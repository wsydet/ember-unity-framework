using System;
using System.Collections.Generic;
using System.Linq;
using Game.Narrative;
using UnityEditor;
using UnityEngine;

namespace Game.UI.Editor
{
    [Serializable]
    public sealed class NovelPlaybackActor
    {
        public bool Enabled;
        public string ResourceKey, InstanceId;
        public NovelPortraitSlot Slot;
        public bool CustomPosition;
        public Vector2 Position = new(.5f, .35f);
    }

    [Serializable]
    public sealed class NovelPlaybackVariable
    {
        public string Id;
        public NovelVariableScope Scope;
        public NovelValue Value;
    }

    /// <summary>Owns only temporary SO copies. Original connections and variable defaults are never written.</summary>
    public sealed class NovelPlaybackStory : INovelResources, IDisposable
    {
        #region 内部参数
        private readonly List<ScriptableObject> _owned = new();
        public NarrativeStorySO Story { get; private set; }
        public NarrativeDialogueSO Dialogue { get; private set; }
        public string DrainCommandId { get; private set; }
        [Serializable] private sealed class Commands { public List<NovelCommand> _commands; }
        [Serializable] private sealed class Variables { public List<NovelVariable> _variables; }
        [Serializable] private sealed class Globals { public List<NovelVariable> _globals; }
        private sealed class Lease<T> : INovelAssetLease<T> where T : UnityEngine.Object
        {
            public bool IsDone => true;
            public T Asset { get; private set; }
            public string Error { get; }
            public Lease(T asset, string path) { Asset = asset; Error = asset ? null : "试播资源不存在或类型错误：" + path; }
            // AssetDatabase/Resources assets are borrowed; do not destroy or globally unload them.
            public void Dispose() { Asset = null; }
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private T Own<T>(T asset) where T : ScriptableObject
        { asset.hideFlags = HideFlags.HideAndDontSave; _owned.Add(asset); return asset; }
        private static void Set(UnityEngine.Object asset, string field, string value)
        { using var so = new SerializedObject(asset); so.FindProperty(field).stringValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Ref(UnityEngine.Object asset, string field, UnityEngine.Object value)
        { using var so = new SerializedObject(asset); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Refs(UnityEngine.Object asset, string field, params UnityEngine.Object[] values)
        {
            using var so = new SerializedObject(asset); var list = so.FindProperty(field); list.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static List<NovelVariable> Merge(IReadOnlyList<NovelVariable> defaults,
            IEnumerable<NovelPlaybackVariable> overrides, NovelVariableScope scope)
        {
            var result = defaults?.Select(v => new NovelVariable(v.Id, v.Value)).ToList() ?? new List<NovelVariable>();
            foreach (var value in overrides ?? Array.Empty<NovelPlaybackVariable>())
            {
                if (value.Scope != scope) continue;
                int index = result.FindIndex(v => v.Id == value.Id);
                if (index < 0 || result[index].Value.Type != value.Value.Type)
                    throw new InvalidOperationException("试播变量不存在或类型已改变：" + value.Id);
                result[index] = new NovelVariable(value.Id, value.Value);
            }
            return result;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        private NovelPlaybackStory() { }

        /// <summary>Continuous audition through the real executor. Internal selections start at their flow entry.</summary>
        public static NovelPlaybackStory ForFlow(NarrativeNodeSO source, NarrativeChapterSO chapter, NarrativeStorySO story,
            IEnumerable<NovelCommand> initialCommands = null, IEnumerable<NovelPlaybackVariable> variables = null)
        {
            var preview = new NovelPlaybackStory();
            try
            {
                if (!source || !chapter || !chapter.Nodes.Contains(source) || (story && !story.Chapters.Contains(chapter)))
                    throw new InvalidOperationException("试播节点必须属于当前剧情章节。");
                var copies = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
                var chapters = story ? story.Chapters.ToArray() : new[] { chapter };
                foreach (var original in chapters.Cast<ScriptableObject>().Concat(chapters.SelectMany(c => c.Nodes)).Distinct())
                    if (original) copies.Add(original, preview.Own(UnityEngine.Object.Instantiate(original)));
                preview.Story = preview.Own(story ? UnityEngine.Object.Instantiate(story) : ScriptableObject.CreateInstance<NarrativeStorySO>());
                foreach (var copy in copies.Values.Append(preview.Story))
                {
                    using var so = new SerializedObject(copy);
                    var property = so.GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue &&
                            copies.TryGetValue(property.objectReferenceValue, out var target)) property.objectReferenceValue = target;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                var copyChapter = (NarrativeChapterSO)copies[chapter];
                var selected = (NarrativeNodeSO)copies[source];
                var nodes = copyChapter.Nodes.Cast<UnityEngine.Object>().ToList();
                string prefix = "preview-" + Guid.NewGuid().ToString("N");
                var setup = preview.Own(ScriptableObject.CreateInstance<NarrativeDialogueSO>());
                Set(setup, "_chapterId", chapter.ChapterId); Set(setup, "_nodeId", prefix + "-setup");
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Commands { _commands = (initialCommands ?? Array.Empty<NovelCommand>()).ToList() }), setup);
                nodes.Add(setup);
                var flow = selected as NarrativeFlowStartSO ?? selected.Flow;
                if (flow)
                {
                    var call = preview.Own(ScriptableObject.CreateInstance<NarrativeFlowCallSO>());
                    var end = preview.Own(ScriptableObject.CreateInstance<NarrativeEndingSO>());
                    Set(call, "_chapterId", chapter.ChapterId); Set(call, "_nodeId", prefix + "-call"); Ref(call, "_callee", flow);
                    Set(end, "_chapterId", chapter.ChapterId); Set(end, "_nodeId", prefix + "-end"); Set(end, "_endingId", prefix);
                    using (var so = new SerializedObject(call))
                    {
                        var names = copyChapter.Nodes.OfType<NarrativeFlowReturnSO>().Where(n => n.Flow == flow).Select(n => n.Result).Distinct().ToArray();
                        var results = so.FindProperty("_results"); results.arraySize = names.Length;
                        for (int i = 0; i < names.Length; i++)
                        {
                            var result = results.GetArrayElementAtIndex(i);
                            result.FindPropertyRelative("_name").stringValue = names[i];
                            result.FindPropertyRelative("_target").objectReferenceValue = end;
                        }
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                    JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Variables { _variables = Merge(flow.Variables, variables, NovelVariableScope.Flow) }), flow);
                    Ref(setup, "_next", call); nodes.Add(call); nodes.Add(end);
                }
                else if (selected is NarrativeReceiverSO receiver)
                {
                    var jump = preview.Own(ScriptableObject.CreateInstance<NarrativeJumpSO>());
                    Set(jump, "_chapterId", chapter.ChapterId); Set(jump, "_nodeId", prefix + "-jump");
                    Set(jump, "_receiverId", receiver.NodeId); Ref(setup, "_next", jump); nodes.Add(jump);
                }
                else Ref(setup, "_next", selected);
                Ref(copyChapter, "_entry", setup); Refs(copyChapter, "_nodes", nodes.ToArray());
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Variables { _variables = Merge(chapter.Variables, variables, NovelVariableScope.Chapter) }), copyChapter);
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Globals { _globals = Merge(story ? story.Globals : null, variables, NovelVariableScope.Global) }), preview.Story);
                Ref(preview.Story, "_entry", copyChapter);
                Refs(preview.Story, "_chapters", chapters.Select(c => copies[c]).ToArray());
                return preview;
            }
            catch { preview.Dispose(); throw; }
        }

        public NovelPlaybackStory(NarrativeDialogueSO source, NarrativeChapterSO chapter, NarrativeStorySO story,
            IEnumerable<NovelCommand> initialCommands = null, IEnumerable<NovelPlaybackVariable> variables = null)
        {
            try
            {
                if (!source || !chapter || !chapter.Nodes.Contains(source))
                    throw new InvalidOperationException("请选择属于当前章节的对话节点。");
                if (story && !story.Chapters.Contains(chapter)) throw new InvalidOperationException("章节不属于当前剧情。");
                string prefix = "preview-" + Guid.NewGuid().ToString("N");
                Story = Own(story ? UnityEngine.Object.Instantiate(story) : ScriptableObject.CreateInstance<NarrativeStorySO>());
                var copyChapter = Own(UnityEngine.Object.Instantiate(chapter));
                Dialogue = Own(UnityEngine.Object.Instantiate(source));
                var ending = Own(ScriptableObject.CreateInstance<NarrativeEndingSO>());
                Set(ending, "_chapterId", chapter.ChapterId); Set(ending, "_nodeId", prefix + "-end"); Set(ending, "_endingId", prefix);
                Ref(Dialogue, "_next", ending);
                var commands = Dialogue.Commands.Select(c => c == null ? null : JsonUtility.FromJson<NovelCommand>(JsonUtility.ToJson(c))).ToList();
                var actionIds = commands.Where(c => c != null && (NovelActorRules.IsAction(c.Kind) || NovelMediaRules.IsAudio(c.Kind)) && !string.IsNullOrWhiteSpace(NovelMediaRules.ActionId(c)))
                    .Select(c => NovelMediaRules.ActionId(c)).Distinct().ToArray();
                DrainCommandId = prefix + "-drain";
                if (actionIds.Length > 0) commands.Add(new NovelCommand(DrainCommandId, NovelCommandKind.WaitActions, waitActions: actionIds));
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Commands { _commands = commands }), Dialogue);
                var setup = Own(ScriptableObject.CreateInstance<NarrativeDialogueSO>());
                Set(setup, "_chapterId", chapter.ChapterId); Set(setup, "_nodeId", prefix + "-setup"); Ref(setup, "_next", Dialogue);
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Commands { _commands = (initialCommands ?? Array.Empty<NovelCommand>()).ToList() }), setup);
                Ref(copyChapter, "_entry", setup);
                if (source.Flow)
                {
                    var flow = Own(UnityEngine.Object.Instantiate(source.Flow));
                    var call = Own(ScriptableObject.CreateInstance<NarrativeFlowCallSO>());
                    var ret = Own(ScriptableObject.CreateInstance<NarrativeFlowReturnSO>());
                    Set(flow, "_nodeId", prefix + "-flow"); Ref(flow, "_flow", null); Ref(flow, "_next", Dialogue);
                    Set(call, "_chapterId", chapter.ChapterId); Set(call, "_nodeId", prefix + "-call"); Ref(call, "_callee", flow);
                    Set(ret, "_chapterId", chapter.ChapterId); Set(ret, "_nodeId", prefix + "-return"); Ref(ret, "_flow", flow);
                    Ref(Dialogue, "_flow", flow); Ref(Dialogue, "_next", ret); Ref(setup, "_next", call);
                    using (var data = new SerializedObject(call))
                    {
                        var results = data.FindProperty("_results"); results.arraySize = 1;
                        results.GetArrayElementAtIndex(0).FindPropertyRelative("_name").stringValue = "完成";
                        results.GetArrayElementAtIndex(0).FindPropertyRelative("_target").objectReferenceValue = ending; data.ApplyModifiedPropertiesWithoutUndo();
                    }
                    JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Variables { _variables = Merge(source.Flow.Variables, variables, NovelVariableScope.Flow) }), flow);
                    Refs(copyChapter, "_nodes", setup, call, flow, Dialogue, ret, ending);
                }
                else Refs(copyChapter, "_nodes", setup, Dialogue, ending);
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Variables { _variables = Merge(chapter.Variables, variables, NovelVariableScope.Chapter) }), copyChapter);
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Globals { _globals = Merge(story ? story.Globals : null, variables, NovelVariableScope.Global) }), Story);
                Ref(Story, "_entry", copyChapter); Refs(Story, "_chapters", copyChapter); Refs(Story, "_exits");
            }
            catch { Dispose(); throw; }
        }
        public INovelAssetLease<T> Load<T>(string path) where T : UnityEngine.Object =>
            new Lease<T>(typeof(T) == typeof(NarrativeStorySO) ? Story as T : LoadAsset<T>(path), path);
        public static T LoadAsset<T>(string path) where T : UnityEngine.Object => string.IsNullOrEmpty(path) ? null :
            path.StartsWith("Assets/", StringComparison.Ordinal) ? AssetDatabase.LoadAssetAtPath<T>(path) : Resources.Load<T>(path);
        public void Dispose()
        {
            for (int i = _owned.Count - 1; i >= 0; i--) if (_owned[i]) UnityEngine.Object.DestroyImmediate(_owned[i]);
            _owned.Clear(); Story = null; Dialogue = null;
        }
        #endregion
    }
}

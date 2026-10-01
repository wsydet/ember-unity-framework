using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ember.Table;
using Game.NovelSave;
using Game.Table.Generated;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Editor
{
    /// <summary>供 AI Skill 调用的编辑器入口；与窗口共用候选解析、变量默认值和正式读档校验。</summary>
    public static class NovelTestSaveAutomation
    {
        #region 内部参数
        [Serializable] public sealed class VariableOverride
        {
            public string Scope, Id, CallId;
            public string String;
            public int Int;
            public bool Bool;
        }
        [Serializable] public sealed class Request
        {
            public string StoryAssetPath, ChapterId, NodeId, TargetChapterId, TargetNodeId, CommandId, OutputRoot;
            public string[] CallIds;
            public List<VariableOverride> Variables = new();
            public int Slot = -1;
            public bool OverwriteExisting;
        }
        [Serializable] private sealed class NodeMatch
        {
            public string StoryAssetPath, StoryName, ChapterId, ChapterName, NodeId, NodeName;
        }
        [Serializable] private sealed class Matches { public List<NodeMatch> Nodes = new(); public bool Truncated; }
        [Serializable] private sealed class Line { public string CommandId, Text; }
        [Serializable] private sealed class Chain { public string[] CallIds; }
        [Serializable] private sealed class Candidate
        {
            public string ChapterId, NodeId, Name, DefaultsJson;
            public int Distance;
            public List<Line> Lines;
            public List<Chain> CallChains;
        }
        [Serializable] private sealed class Preview
        {
            public string StoryAssetPath, ChapterId, NodeId;
            public List<Candidate> Candidates = new();
            public string StateRule = "默认变量，可覆盖；空舞台，不补跑前置步骤；特殊节点只能回退到普通节点。";
        }
        [Serializable] private sealed class Result
        {
            public string Root, IndexPath, PayloadPath, ChapterId, NodeId, CommandId;
            public int Slot;
            public bool FellBack;
        }
        private sealed class Context : IDisposable
        {
            internal readonly EmberTableEngine Engine = new();
            internal NarrativeTableCatalog Catalog;
            internal NarrativeStorySO Asset;
            internal NovelStory Story;
            internal string StoryPath;
            public void Dispose() => Engine.Dispose();
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static void RequireEditor()
        {
            if (!NarrativeEditorAvailability.Visible || !NarrativeGraphModel.IsTemplateActive(true))
                throw new InvalidOperationException("当前项目未启用视觉小说模板或模块。");
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("请停止运行并等待编译完成后生成测试存档。");
        }
        private static Context Load(string path)
        {
            RequireEditor(); var context = new Context();
            try
            {
                context.Asset = AssetDatabase.LoadAssetAtPath<NarrativeStorySO>(path);
                if (!context.Asset) throw new InvalidOperationException("剧情资产不存在：" + path);
                var assets = new List<UnityEngine.Object> { context.Asset };
                foreach (var chapter in context.Asset.Chapters.Where(c => c)) { assets.Add(chapter); assets.AddRange(chapter.Nodes.Where(n => n)); }
                assets.AddRange(context.Asset.CustomSteps.Where(s => s));
                if (assets.Any(EditorUtility.IsDirty)) throw new InvalidOperationException("剧情或自定义脚本资产尚未保存，请先保存再生成。");
                var tables = GameTables.CreateCatalog(); var bytes = new Dictionary<string, byte[]>();
                foreach (var entry in tables.Entries)
                {
                    var asset = Resources.Load<TextAsset>(entry.ResourcePath);
                    if (!asset) throw new InvalidOperationException("缺少导表产物：" + entry.ResourcePath);
                    bytes.Add(entry.TableId, asset.bytes);
                }
                if (!context.Engine.Load(tables, bytes).Succeeded) throw new InvalidOperationException("配表加载失败。");
                context.Catalog = new NarrativeTableCatalog(context.Engine.Database);
                if (!context.Asset.TryReadDefinition(context.Catalog, out context.Story, out var errors))
                    throw new InvalidOperationException(string.Join("\n", errors.Take(5)));
                int start = path.IndexOf("/Resources/", StringComparison.Ordinal);
                if (start >= 0) context.StoryPath = Path.ChangeExtension(path.Substring(start + "/Resources/".Length), null);
                else
                {
                    var library = Resources.Load<NarrativeLibrarySO>(NarrativeLibrarySO.RESOURCE_PATH);
                    if (!library || library.Find(context.Asset.StoryId) != context.Asset) throw new InvalidOperationException("剧情没有可用于实机读档的路径或小说库登记。");
                    context.StoryPath = NarrativeLibrarySO.CURRENT_STORY;
                }
                return context;
            }
            catch { context.Dispose(); throw; }
        }
        private static void Apply(NovelCheckpoint settings, VariableOverride change)
        {
            if (change == null || string.IsNullOrWhiteSpace(change.Id)) throw new InvalidOperationException("测试变量 ID 为空。");
            var variables = change.Scope switch
            {
                "Global" => settings.Globals,
                "Chapter" => settings.Locals,
                "Flow" => (string.IsNullOrEmpty(change.CallId) ? settings.CallStack.LastOrDefault() :
                    settings.CallStack.SingleOrDefault(f => f.CallId == change.CallId))?.Variables,
                _ => throw new InvalidOperationException("变量 Scope 必须是 Global、Chapter 或 Flow。")
            };
            int index = variables?.FindIndex(v => v.Id == change.Id) ?? -1;
            if (index < 0) throw new InvalidOperationException("变量未声明：" + change.Scope + "/" + change.Id);
            var value = variables[index].Value.Type switch
            {
                NovelValueType.Bool => new NovelValue(change.Bool),
                NovelValueType.Int => new NovelValue(change.Int),
                _ => new NovelValue(change.String ?? "")
            };
            variables[index] = new NovelVariable(change.Id, value);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static string Search(string query)
        {
            RequireEditor();
            if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("请输入节点名、ID、台词或自定义脚本名称。");
            var result = new Matches();
            bool Has(string text) => text?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
            foreach (string guid in AssetDatabase.FindAssets("t:NarrativeStorySO", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var story = AssetDatabase.LoadAssetAtPath<NarrativeStorySO>(path);
                foreach (var chapter in story.Chapters.Where(c => c))
                    foreach (var node in chapter.Nodes.Where(n => n))
                    {
                        bool match = Has(node.name) || Has(node.NodeId);
                        if (node is NarrativeDialogueSO dialogue)
                            match |= dialogue.Commands.Any(c => c != null && (Has(c.Text) || Has(c.TextKey) || Has(c.CustomStepId) ||
                                c.Kind == NovelCommandKind.CustomStep && story.CustomSteps.Any(s => s && s.ScriptId == c.CustomStepId && Has(s.DisplayName))));
                        if (!match) continue;
                        if (result.Nodes.Count >= 100) { result.Truncated = true; return JsonUtility.ToJson(result); }
                        result.Nodes.Add(new NodeMatch { StoryAssetPath = path, StoryName = story.DisplayName, ChapterId = chapter.ChapterId,
                            ChapterName = chapter.name, NodeId = node.NodeId, NodeName = node.name });
                    }
            }
            return JsonUtility.ToJson(result);
        }

        public static string Inspect(string storyAssetPath, string chapterId, string nodeId)
        {
            using var context = Load(storyAssetPath);
            var preview = new Preview { StoryAssetPath = storyAssetPath, ChapterId = chapterId, NodeId = nodeId };
            foreach (var target in NovelTestSaveBuilder.FindTargets(context.Story, chapterId, nodeId))
            {
                var chains = NovelTestSaveBuilder.CallChains(target.Chapter, target.Node);
                var node = context.Asset.Chapters.Single(c => c.ChapterId == target.Chapter.Id).Nodes.Single(n => n.NodeId == target.Node.Id);
                preview.Candidates.Add(new Candidate { ChapterId = target.Chapter.Id, NodeId = target.Node.Id, Name = node.name,
                    Distance = target.Distance, CallChains = chains.Select(c => new Chain { CallIds = c }).ToList(),
                    Lines = target.Node.Commands.Where(c => c.Kind == NovelCommandKind.Say).Select(c => new Line { CommandId = c.CommandId, Text = c.Text }).ToList(),
                    DefaultsJson = chains.Count == 0 ? null : JsonUtility.ToJson(NovelTestSaveBuilder.Defaults(context.Story, target, context.StoryPath, chains[0])) });
            }
            return JsonUtility.ToJson(preview);
        }

        public static string Generate(string requestJson)
        {
            var request = JsonUtility.FromJson<Request>(requestJson) ?? throw new ArgumentException("生成请求为空。");
            using var context = Load(request.StoryAssetPath);
            var candidates = NovelTestSaveBuilder.FindTargets(context.Story, request.ChapterId, request.NodeId);
            var target = string.IsNullOrEmpty(request.TargetNodeId) ? candidates.Count == 1 ? candidates[0] : null :
                candidates.SingleOrDefault(c => c.Node.Id == request.TargetNodeId && c.Chapter.Id == request.TargetChapterId);
            if (target == null) throw new InvalidOperationException("没有唯一的普通停留点，请先 Inspect 并明确选择候选节点。");
            var chains = NovelTestSaveBuilder.CallChains(target.Chapter, target.Node);
            var calls = request.CallIds == null || request.CallIds.Length == 0
                ? chains.Count == 1 ? chains[0] : null : request.CallIds;
            if (calls == null || !chains.Any(c => c.SequenceEqual(calls))) throw new InvalidOperationException("请选择 Inspect 返回的完整调用链。");
            var settings = NovelTestSaveBuilder.Defaults(context.Story, target, context.StoryPath, calls);
            foreach (var change in request.Variables ?? new List<VariableOverride>()) Apply(settings, change);
            string command = request.CommandId;
            if (string.IsNullOrEmpty(command) && target.Distance > 0)
                command = target.Node.Commands.LastOrDefault(c => c.Kind == NovelCommandKind.Say)?.CommandId;
            var checkpoint = NovelTestSaveBuilder.Build(context.Story, context.Catalog, settings, command);
            string root = Path.GetFullPath(string.IsNullOrWhiteSpace(request.OutputRoot) ? Path.Combine(Application.persistentDataPath, "VisualNovelSaves") : request.OutputRoot);
            var store = new NovelSaveStore(root);
            if (store.IndexError != null) throw new InvalidOperationException(store.IndexError);
            int slot = request.Slot;
            if (slot == -1) slot = Enumerable.Range(0, 6).Where(i => !store.Slots.Any(s => s.Slot == i)).DefaultIfEmpty(-1).First();
            if (slot < 0 || slot > 5) throw new InvalidOperationException("没有空闲手动槽，请指定导出目录或明确选择要覆盖的手动槽（0–5）。");
            if (store.Slots.Any(s => s.Slot == slot) && !request.OverwriteExisting) throw new InvalidOperationException("目标槽位已有存档，未覆盖。请选择空槽或明确允许覆盖。");
            if (!store.Save(slot, checkpoint, out string error)) throw new IOException(error);
            if (!store.Read(slot, out var read, out error) || !new NarrativeRunner().TryRestore(context.Story, context.Catalog, read, out error))
                throw new IOException("文件已写入，但回读验证失败：" + error);
            var entry = store.Slots.Single(s => s.Slot == slot);
            return JsonUtility.ToJson(new Result { Root = root, IndexPath = Path.Combine(root, "index.json"), PayloadPath = Path.Combine(root, entry.File),
                ChapterId = checkpoint.ChapterId, NodeId = checkpoint.NodeId, CommandId = checkpoint.CommandId, Slot = slot, FellBack = target.Distance > 0 });
        }
        #endregion
    }
}

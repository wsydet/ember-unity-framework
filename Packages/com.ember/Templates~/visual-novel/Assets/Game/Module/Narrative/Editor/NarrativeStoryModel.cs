using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Editor
{
    /// <summary>剧情总览的写入与资产组织。只操作项目 Assets，连线直接写剧情 SO。</summary>
    public static class NarrativeStoryModel
    {
        #region 内部方法
        private static void Require(UnityEngine.Object asset)
        {
            if (!NarrativeGraphModel.IsTemplateActive(true) || !NarrativeGraphModel.CanEdit(asset))
                throw new InvalidOperationException("仅可在 visual-novel 编辑模式修改项目资产。");
        }
        internal static void Folder(string path)
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("..")) throw new ArgumentException("必须位于 Assets 子目录。");
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (parent != "Assets") Folder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        internal static string SafeName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            string result = new string((name ?? "").Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim().Trim('.');
            return string.IsNullOrEmpty(result) ? "Untitled" : result;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static string NodeFolder(NarrativeNodeSO node) => node switch
        {
            NarrativeDialogueSO => "Dialogue", NarrativeChoiceSO => "Choices", NarrativeBranchSO => "Branches",
            NarrativeChapterExitSO => "Exits", _ => "Endings"
        };
        public static string NewNodePath(NarrativeChapterSO chapter, NarrativeNodeSO node, string name)
        {
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(chapter)).Replace('\\', '/') + "/" + NodeFolder(node);
            Folder(folder);
            string prefix = SafeName(chapter.AssetPrefix) + "_";
            name = SafeName(name); if (!name.StartsWith(prefix, StringComparison.Ordinal)) name = prefix + name;
            return AssetDatabase.GenerateUniqueAssetPath(folder + "/" + name + ".asset");
        }
        public static NarrativeStorySO FindStory(NarrativeChapterSO chapter)
        {
            var matches = AssetDatabase.FindAssets("t:NarrativeStorySO", new[] { "Assets" })
                .Select(g => AssetDatabase.LoadAssetAtPath<NarrativeStorySO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s && s.Chapters.Contains(chapter)).ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }
        public static NarrativeStorySO CreateStory(string path)
        {
            NarrativeEditorAvailability.RequireEnabled();
            if (!NarrativeGraphModel.IsTemplateActive(true) || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("当前不可创建剧情");
            Folder(Path.GetDirectoryName(path).Replace('\\', '/'));
            if (AssetDatabase.LoadMainAssetAtPath(path)) throw new InvalidOperationException("目标已存在");
            var story = ScriptableObject.CreateInstance<NarrativeStorySO>(); AssetDatabase.CreateAsset(story, path); return story;
        }
        public static NarrativeChapterSO CreateChapter(NarrativeStorySO story, string folderName)
        {
            Require(story);
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(story)).Replace('\\', '/') + "/Chapters/" + SafeName(folderName);
            Folder(folder); string path = folder + "/Chapter.asset";
            if (AssetDatabase.LoadMainAssetAtPath(path)) throw new InvalidOperationException("该章节目录已存在。");
            var chapter = ScriptableObject.CreateInstance<NarrativeChapterSO>();
            using (var data = new SerializedObject(chapter))
            {
                data.FindProperty("_displayName").stringValue = folderName;
                data.FindProperty("_assetPrefix").stringValue = SafeName(folderName);
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.CreateAsset(chapter, path); AddChapter(story, chapter); return chapter;
        }
        public static void AddChapter(NarrativeStorySO story, NarrativeChapterSO chapter)
        {
            Require(story); Require(chapter);
            if (story.Chapters.Any(c => c && c.ChapterId == chapter.ChapterId)) throw new InvalidOperationException("章节 ID 已登记。");
            if (FindStory(chapter)) throw new InvalidOperationException("章节已属于另一个剧情。");
            using var data = new SerializedObject(story); var list = data.FindProperty("_chapters");
            list.arraySize++; list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = chapter;
            if (!story.Entry) data.FindProperty("_entry").objectReferenceValue = chapter;
            data.ApplyModifiedProperties();
        }
        public static void SyncExits(NarrativeStorySO story)
        {
            Require(story); using var data = new SerializedObject(story); var list = data.FindProperty("_exits");
            foreach (var chapter in story.Chapters.Where(c => c))
                foreach (var exit in chapter.Nodes.OfType<NarrativeChapterExitSO>())
                {
                    if (story.Exits.Any(e => e != null && e.Source == chapter && e.Exit == exit)) continue;
                    int i = list.arraySize++; var item = list.GetArrayElementAtIndex(i);
                    item.FindPropertyRelative("_source").objectReferenceValue = chapter;
                    item.FindPropertyRelative("_exit").objectReferenceValue = exit;
                    item.FindPropertyRelative("_routes").arraySize = 0;
                    item.FindPropertyRelative("_fallback").objectReferenceValue = null;
                }
            data.ApplyModifiedProperties();
        }
        public static void RemoveChapter(NarrativeStorySO story, NarrativeChapterSO chapter)
        {
            Require(story); using var data = new SerializedObject(story);
            var nodes = data.FindProperty("_chapters");
            for (int i = nodes.arraySize - 1; i >= 0; i--)
                if (nodes.GetArrayElementAtIndex(i).objectReferenceValue == chapter)
                { nodes.GetArrayElementAtIndex(i).objectReferenceValue = null; nodes.DeleteArrayElementAtIndex(i); }
            if (story.Entry == chapter) data.FindProperty("_entry").objectReferenceValue = null;
            var links = data.FindProperty("_exits");
            for (int i = links.arraySize - 1; i >= 0; i--)
            {
                var link = links.GetArrayElementAtIndex(i);
                if (link.FindPropertyRelative("_source").objectReferenceValue == chapter) { links.DeleteArrayElementAtIndex(i); continue; }
                if (link.FindPropertyRelative("_fallback").objectReferenceValue == chapter) link.FindPropertyRelative("_fallback").objectReferenceValue = null;
                var routes = link.FindPropertyRelative("_routes");
                for (int j = 0; j < routes.arraySize; j++)
                {
                    var target = routes.GetArrayElementAtIndex(j).FindPropertyRelative("_target");
                    if (target.objectReferenceValue == chapter) target.objectReferenceValue = null;
                }
            }
            data.ApplyModifiedProperties();
        }
        public static void AddRoute(NarrativeStorySO story, int exitIndex)
        {
            Require(story); using var data = new SerializedObject(story);
            var routes = data.FindProperty("_exits").GetArrayElementAtIndex(exitIndex).FindPropertyRelative("_routes");
            var item = routes.GetArrayElementAtIndex(routes.arraySize++);
            item.FindPropertyRelative("_id").stringValue = Guid.NewGuid().ToString("N");
            item.FindPropertyRelative("_text").stringValue = "新路线";
            item.FindPropertyRelative("_target").objectReferenceValue = null;
            item.FindPropertyRelative("_condition").FindPropertyRelative("_predicates").arraySize = 0;
            item.FindPropertyRelative("_condition").FindPropertyRelative("_junction").enumValueIndex = 0;
            data.ApplyModifiedProperties();
        }
        public static string PortPath(int exitIndex, int routeIndex) => "_exits.Array.data[" + exitIndex + "]." +
            (routeIndex < 0 ? "_fallback" : "_routes.Array.data[" + routeIndex + "]._target");
        public static void Connect(NarrativeStorySO story, int exitIndex, int routeIndex, NarrativeChapterSO target)
        {
            Require(story);
            if (exitIndex < 0 || exitIndex >= story.Exits.Count || routeIndex < -1 || routeIndex >= story.Exits[exitIndex].Routes.Count ||
                (target && !story.Chapters.Contains(target))) throw new InvalidOperationException("失效的章节端口或章外目标");
            using var data = new SerializedObject(story);
            data.FindProperty(PortPath(exitIndex, routeIndex)).objectReferenceValue = target; data.ApplyModifiedProperties();
        }
        public static void Save(NarrativeStorySO story)
        {
            Require(story); AssetDatabase.SaveAssetIfDirty(story);
            foreach (var chapter in story.Chapters.Where(c => c)) NarrativeGraphModel.Save(chapter);
            var layout = NarrativeGraphModel.GetLayout(story, false); if (layout) AssetDatabase.SaveAssetIfDirty(layout);
        }
        public static void Move(NarrativeStorySO story, IReadOnlyDictionary<NarrativeChapterSO, Vector2> positions)
        {
            Require(story);
            foreach (var pair in positions) if (!story.Chapters.Contains(pair.Key)) throw new InvalidOperationException("布局包含未登记章节");
            var layout = NarrativeGraphModel.GetLayout(story, true); Undo.RecordObject(layout, "移动章节卡");
            foreach (var pair in positions) layout.SetPosition(pair.Key.ChapterId, pair.Value);
            EditorUtility.SetDirty(layout);
        }
        public static List<(string from, string to)> PreviewOrganization(NarrativeChapterSO chapter, string folder)
        {
            Require(chapter);
            var moves = new List<(string, string)> { (AssetDatabase.GetAssetPath(chapter), folder + "/Chapter.asset") };
            string prefix = SafeName(chapter.AssetPrefix) + "_";
            foreach (var node in chapter.Nodes.Where(n => n).Distinct())
            {
                string name = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(node));
                if (!name.StartsWith(prefix, StringComparison.Ordinal)) name = prefix + name;
                moves.Add((AssetDatabase.GetAssetPath(node), folder + "/" + NodeFolder(node) + "/" + name + ".asset"));
            }
            return moves.Where(m => m.Item1 != m.Item2).ToList();
        }
        public static void ApplyOrganization(IReadOnlyList<(string from, string to)> moves)
        {
            NarrativeEditorAvailability.RequireEnabled();
            if (!NarrativeGraphModel.IsTemplateActive(true) || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("当前不可迁移");
            var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var move in moves)
                if (!move.from.StartsWith("Assets/") || !move.to.StartsWith("Assets/") || move.from.Contains("..") || move.to.Contains("..") ||
                    !destinations.Add(move.to) || !AssetDatabase.LoadMainAssetAtPath(move.from) || AssetDatabase.LoadMainAssetAtPath(move.to))
                    throw new InvalidOperationException("迁移预检失败：" + move.from + " → " + move.to);
            var completed = new List<(string from, string to)>();
            try
            {
                foreach (var move in moves)
                {
                    Folder(Path.GetDirectoryName(move.to).Replace('\\', '/'));
                    string error = AssetDatabase.MoveAsset(move.from, move.to);
                    if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                    completed.Add(move);
                }
            }
            catch
            {
                for (int i = completed.Count - 1; i >= 0; i--) AssetDatabase.MoveAsset(completed[i].to, completed[i].from);
                throw;
            }
        }
        #endregion
    }
}

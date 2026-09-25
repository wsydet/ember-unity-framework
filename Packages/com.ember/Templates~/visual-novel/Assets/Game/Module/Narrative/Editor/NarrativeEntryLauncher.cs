using System;
using System.Linq;
using System.Reflection;
using Ember.Basic;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Editor
{
    /// <summary>独立制作菜单启动新游戏；观察窗口仍然只读。</summary>
    internal static class NarrativeEntryLauncher
    {
        #region 外部方法
        internal static bool CanStart() => NarrativeEditorAvailability.Enabled && EditorApplication.isPlaying &&
            !EditorApplication.isPaused && NarrativeObservation.Current == null &&
            NarrativeGraphModel.IsTemplateActive() &&
            (Selection.activeObject is NarrativeStorySO || Selection.activeObject is NarrativeChapterSO || Selection.activeObject is NarrativeNodeSO);

        internal static void Start()
        {
            if (!CanStart()) return;
            try
            {
                var selected = Selection.activeObject;
                var node = selected as NarrativeNodeSO;
                var chapter = selected as NarrativeChapterSO;
                if (node)
                {
                    var chapters = AssetDatabase.FindAssets("t:NarrativeChapterSO", new[] { "Assets" })
                        .Select(g => AssetDatabase.LoadAssetAtPath<NarrativeChapterSO>(AssetDatabase.GUIDToAssetPath(g)))
                        .Where(c => c && c.Nodes.Contains(node)).ToArray();
                    if (chapters.Length != 1) throw new InvalidOperationException("所选节点必须登记到唯一章节。");
                    chapter = chapters[0];
                }
                var story = selected as NarrativeStorySO;
                if (!story && chapter) story = NarrativeStoryModel.FindStory(chapter);
                if (!story) throw new InvalidOperationException("所选入口没有唯一所属剧情。");
                string path = AssetDatabase.GetAssetPath(story);
                const string marker = "/Resources/";
                int start = path.IndexOf(marker, StringComparison.Ordinal);
                if (start < 0) throw new InvalidOperationException("剧情必须位于 Resources 中。");
                path = path.Substring(start + marker.Length);
                path = path.Substring(0, path.Length - ".asset".Length);
                // Game UI lives in the predefined business assembly, outside the Editor asmdef dependency graph.
                var ui = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Game.UI.NovelSaveUI")).FirstOrDefault(t => t != null);
                var launch = ui?.GetMethod("StartEntryFromEditor", BindingFlags.Static | BindingFlags.NonPublic);
                if (launch == null) throw new InvalidOperationException("正式新游戏入口不可用。");
                launch.Invoke(null, new object[] { new NovelNewGameRequest(path, chapter?.ChapterId, node?.NodeId) });
            }
            catch (Exception ex) { EmberDebug.LogError("Game.Narrative.Editor", (ex.InnerException ?? ex).Message); }
        }
        #endregion
    }
}

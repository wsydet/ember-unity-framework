using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Editor
{
    /// <summary>统一选择新游戏入口；保留已登记小说供旧存档按稳定 ID 恢复。</summary>
    public sealed class NarrativeLibraryWindow : EditorWindow
    {
        #region 内部参数
        private const string ASSET_PATH = "Assets/GameResource/Resources/" + NarrativeLibrarySO.RESOURCE_PATH + ".asset";
        private NarrativeStorySO[] _stories = Array.Empty<NarrativeStorySO>();
        private string _error;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void OnEnable() { titleContent = new GUIContent("当前小说"); minSize = new Vector2(460, 230); Reload(); }
        private void OnFocus() => Reload();
        private void Reload()
        {
            _stories = AssetDatabase.FindAssets("t:NarrativeStorySO", new[] { "Assets/GameResource" })
                .Select(g => AssetDatabase.LoadAssetAtPath<NarrativeStorySO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s).OrderBy(s => s.DisplayName).ToArray();
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("新游戏 · 当前小说", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("选择后，主菜单的新游戏使用这部小说。引用不依赖文件名；重命名或移动资源不会丢失选择。旧存档按剧情 ID 恢复原小说。", MessageType.Info);
            var library = Resources.Load<NarrativeLibrarySO>(NarrativeLibrarySO.RESOURCE_PATH);
            var current = library ? library.Current : null;
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                int index = Array.IndexOf(_stories, current) + 1;
                var labels = new[] { "（请选择）" }.Concat(_stories.Select(s => s.DisplayName + " — " + AssetDatabase.GetAssetPath(s))).ToArray();
                int next = EditorGUILayout.Popup("快速选择", index, labels);
                var chosen = (NarrativeStorySO)EditorGUILayout.ObjectField("小说资源", current, typeof(NarrativeStorySO), false);
                if (next != index && next > 0) chosen = _stories[next - 1];
                if (chosen && chosen != current)
                {
                    try { SetCurrent(chosen); _error = null; }
                    catch (Exception ex) { _error = ex.Message; }
                }
            }
            if (current)
            {
                EditorGUILayout.LabelField("剧情 ID", current.StoryId);
                if (GUILayout.Button("打开小说流程")) AssetDatabase.OpenAsset(current);
                if (GUILayout.Button("定位小说资源")) { Selection.activeObject = current; EditorGUIUtility.PingObject(current); }
            }
            if (!string.IsNullOrEmpty(_error)) EditorGUILayout.HelpBox(_error, MessageType.Error);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Open() => GetWindow<NarrativeLibraryWindow>().Show();

        public static void SetCurrent(NarrativeStorySO story)
        {
            NarrativeEditorAvailability.RequireEnabled();
            if (!NarrativeGraphModel.IsTemplateActive(true) || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("请退出播放模式后选择当前小说。");
            if (!story || !AssetDatabase.GetAssetPath(story).StartsWith("Assets/", StringComparison.Ordinal))
                throw new InvalidOperationException("请选择项目内的小说资源。");
            if (string.IsNullOrWhiteSpace(story.StoryId)) throw new InvalidOperationException("小说缺少稳定 ID。");
            var library = AssetDatabase.LoadAssetAtPath<NarrativeLibrarySO>(ASSET_PATH);
            if (!library)
            {
                NarrativeStoryModel.Folder(System.IO.Path.GetDirectoryName(ASSET_PATH).Replace('\\', '/'));
                library = CreateInstance<NarrativeLibrarySO>();
                AssetDatabase.CreateAsset(library, ASSET_PATH);
            }
            var existing = library.Find(story.StoryId);
            if (existing && existing != story) throw new InvalidOperationException("另一部小说使用相同剧情 ID；请先修复重复 ID。");
            Undo.RecordObject(library, "选择当前小说");
            using var data = new SerializedObject(library);
            data.FindProperty("_current").objectReferenceValue = story;
            if (!library.Stories.Contains(story))
            {
                var list = data.FindProperty("_stories"); list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = story;
            }
            data.ApplyModifiedProperties(); EditorUtility.SetDirty(library); AssetDatabase.SaveAssetIfDirty(library);
        }
        #endregion
    }
}

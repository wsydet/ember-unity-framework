using System;
using System.Linq;
using Sirenix.Utilities.Editor;
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
        [SerializeField] private string _search = "";
        private Vector2 _scroll;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void OnEnable() { titleContent = new GUIContent("当前小说"); minSize = new Vector2(540, 420); Reload(); }
        private void OnFocus() => Reload();
        private void Reload()
        {
            _stories = AssetDatabase.FindAssets("t:NarrativeStorySO", new[] { "Assets/GameResource" })
                .Select(g => AssetDatabase.LoadAssetAtPath<NarrativeStorySO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s).OrderBy(s => s.DisplayName).ToArray();
        }
        private void OnGUI()
        {
            var library = Resources.Load<NarrativeLibrarySO>(NarrativeLibrarySO.RESOURCE_PATH);
            var current = library ? library.Current : null;
            SirenixEditorGUI.Title("新游戏入口", current ? current.DisplayName : "尚未选择小说", TextAlignment.Left, true);
            EditorGUILayout.HelpBox("设置入口后，主菜单的新游戏使用这部小说；旧存档仍按稳定剧情 ID 恢复原小说。", MessageType.Info);
            SirenixEditorGUI.BeginBox("当前小说");
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                var chosen = (NarrativeStorySO)EditorGUILayout.ObjectField("小说资源", current, typeof(NarrativeStorySO), false);
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
            SirenixEditorGUI.EndBox();
            if (!string.IsNullOrEmpty(_error)) EditorGUILayout.HelpBox(_error, MessageType.Error);
            SirenixEditorGUI.Title("项目小说", _stories.Length + " 部 · 按名称或剧情 ID 搜索", TextAlignment.Left, true);
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var story in _stories)
            {
                if (!story || (!string.IsNullOrEmpty(_search) &&
                    (story.DisplayName + " " + story.StoryId).IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0)) continue;
                SirenixEditorGUI.BeginBox(story.DisplayName + (story == current ? " · 当前入口" : ""));
                EditorGUILayout.LabelField("剧情 ID", story.StoryId);
                EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(story), EditorStyles.miniLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("打开流程")) AssetDatabase.OpenAsset(story);
                    using (new EditorGUI.DisabledScope(story == current || EditorApplication.isPlayingOrWillChangePlaymode))
                        if (GUILayout.Button("设为新游戏入口"))
                        {
                            try { SetCurrent(story); _error = null; }
                            catch (Exception ex) { _error = ex.Message; }
                        }
                }
                SirenixEditorGUI.EndBox();
            }
            EditorGUILayout.EndScrollView();
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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    public sealed partial class PlayerSchemeWindow : EditorWindow
    {
        #region 编辑器面板参数
        [SerializeField] private PlayerScheme _selected, _reference;
        [SerializeField] private UnityEngine.Object _art;
        [SerializeField] private int _tab;
        [SerializeField] private bool _ruler = true;
        [SerializeField] private Vector2 _position, _velocity;
        [SerializeField] private bool _initialized;
        [SerializeField] private PlayerFeelProfile _legacyScaleFeel;
        [SerializeField] private PlayerScheme _legacyLabScheme;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private string _message;
        private List<string> _differences;
        private bool _rulerSubscribed;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Open() => OpenTab(1);
        public static void Open(PlayerScheme scheme) => OpenTab(1, scheme);
        public static void OpenTab(int tab, PlayerScheme scheme = null)
        {
            PlayerEditorAccess.Require();
            var window = GetWindow<PlayerSchemeWindow>("玩家开发工具");
            if (scheme != null) window._selected = scheme;
            window._tab = Mathf.Clamp(tab, 0, 1);
            window.minSize = new Vector2(560, 460); window.Show();
        }
        internal void PreserveScaleDraft(PlayerFeelProfile feel, bool ruler)
        { _legacyScaleFeel = feel; _ruler = ruler; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Run(Action action)
        { try { PlayerEditorAccess.Require(); action(); _messageIsError = false; _message = "操作完成。"; } catch (ExitGUIException) { throw; } catch (Exception error) { _messageIsError = true; _message = error.Message; } }
        private void OnEnable()
        {
            titleContent = new GUIContent("玩家开发工具");
            minSize = new Vector2(560, 460);
            if (!_initialized)
            {
                if (!RestoreDraft())
                {
                    _position = PlayerDevelopmentEnvironment.SelectedPosition; _velocity = PlayerDevelopmentEnvironment.SelectedVelocity;
                    if (_selected == null) _selected = PlayerDevelopmentEnvironment.SelectedScheme;
                }
                _initialized = true;
            }
            RestoreCombatDraft();
            EditorApplication.delayCall += IntegrateLegacyWindows;
            UpdateRulerSubscription();
        }
        private void OnDisable()
        {
            CacheDraft();
            if (_combatDraft != null) DestroyImmediate(_combatDraft);
            _combatDraft = null;
            EditorApplication.delayCall -= IntegrateLegacyWindows;
            SceneView.duringSceneGui -= DrawRuler; _rulerSubscribed = false;
            DisposePanels();
        }
        private void UpdateRulerSubscription()
        {
            bool wanted = PlayerEditorAccess.Available && _ruler;
            if (wanted == _rulerSubscribed) return;
            if (wanted) SceneView.duringSceneGui += DrawRuler;
            else SceneView.duringSceneGui -= DrawRuler;
            _rulerSubscribed = wanted;
        }
        private void IntegrateLegacyWindows()
        {
            if (this == null || !PlayerEditorAccess.Available) return;
            foreach (var old in Resources.FindObjectsOfTypeAll<PlayerFeelLabWindow>())
            {
                using var data = new SerializedObject(old);
                _legacyLabScheme = data.FindProperty("_scheme").objectReferenceValue as PlayerScheme;
                _position = data.FindProperty("_position").vector2Value; _velocity = data.FindProperty("_velocity").vector2Value;
                old.Close();
            }
            foreach (var old in Resources.FindObjectsOfTypeAll<PlayerAppearanceWindow>())
            {
                using var data = new SerializedObject(old);
                PreserveScaleDraft(data.FindProperty("_feel").objectReferenceValue as PlayerFeelProfile, data.FindProperty("_ruler").boolValue);
                old.Close();
            }
        }
        private void DrawRuler(SceneView view)
        { if (PlayerEditorAccess.Available) PlayerAppearanceWindow.DrawWorldRuler(view, _ruler); }
        private void OnInspectorUpdate() { UpdateRulerSubscription(); RefreshStatus(); Repaint(); }
        private void Copy(bool blank)
        {
            var source = blank ? PlayerSchemeOwnership.Builtins.FirstOrDefault()
                : _selected != null ? _selected : Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH).Scheme;
            string path = EditorUtility.SaveFilePanelInProject(blank ? "创建空白完整方案" : "独立另存完整方案", blank ? "BlankScheme" : "CustomScheme", "asset", "图和只读素材共享，可变配置独立复制");
            if (!string.IsNullOrEmpty(path)) _selected = PlayerSchemeWorkflow.Copy(source, path, blank);
        }
        internal static void DrawAsset(ScriptableObject asset)
        {
            if (PlayerEditorAccess.DrawUnavailable()) return;
            bool readOnly = PlayerSchemeOwnership.IsReadOnly(asset);
            if (readOnly) EditorGUILayout.HelpBox("内置配置只读，请先复制为自定义。", MessageType.None);
            using (var data = new SerializedObject(asset))
            {
                data.Update(); var p = data.GetIterator(); bool enter = true;
                while (p.NextVisible(enter))
                {
                    enter = false;
                    if (p.propertyType == SerializedPropertyType.Generic && p.hasVisibleChildren)
                    {
                        // 展开是面板状态：只读方案仍可查看数组与嵌套配置。
                        p.isExpanded = EditorGUILayout.Foldout(p.isExpanded, p.displayName, true);
                        enter = p.isExpanded; continue;
                    }
                    using (new EditorGUI.DisabledScope(readOnly || p.name == "m_Script" || asset is PlayerScheme && (p.propertyType == SerializedPropertyType.ObjectReference || p.name == "_ruleMode")
                        || asset is PlayerFeelProfile && p.propertyType == SerializedPropertyType.ObjectReference))
                        EditorGUILayout.PropertyField(p, false);
                }
                data.ApplyModifiedProperties();
            }
        }
        #endregion
    }
}

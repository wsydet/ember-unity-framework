using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ember.Core;
using Ember.UIExtension;
using Ember.UIExtension.Editor;
using Game.Narrative;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Editor
{
    /// <summary>Edits the formal reader prefab; sample content exists only in a separate preview scene.</summary>
    public sealed partial class NovelGameplayLayoutWindow : EditorWindow
    {
        #region 内部参数
        internal const string PrefabPath = "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelReaderPage.prefab";
        private const string ReadingMenuPrefabPath = "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelReadingMenuPage.prefab";
        private static readonly string[] MenuKeys = { "Saves", "QuickSave", "QuickLoad", "MenuSettings", "ReadSkip", "ReturnMenu" };
        private static bool IsMenuKey(string key) => MenuKeys.Contains(key) || key == "MenuPanel";
        private string SourceHash => AssetDatabase.GetAssetDependencyHash(PrefabPath) + ":" + AssetDatabase.GetAssetDependencyHash(ReadingMenuPrefabPath);
        private static readonly string[] Keys = { "Background", "Left", "Center", "Right", "Dialogue", "Speaker", "Body", "Advance", "Choices", "Menu", "Settings", "Saves", "QuickSave", "QuickLoad", "Status", "Auto", "Skip", "History", "HideDialogue", "Speed", "MenuSettings", "ReadSkip", "ReturnMenu", "Shading", "ChoiceTemplate", "MenuPanel" };
        private static readonly string[] Labels = { "背景", "左立绘", "中立绘", "右立绘", "对白框（整体）", "角色姓名", "对白正文", "推进按钮", "选项区域", "阅读菜单入口", "字号按钮", "菜单 · 存档/读档", "菜单 · 快速保存", "菜单 · 快速读取", "状态提示", "自动播放", "已读快进", "历史按钮", "隐藏对话按钮", "阅读倍率", "菜单 · 系统设置", "菜单 · 仅已读快进", "菜单 · 返回主菜单", "对话底板 / 渐变", "选项样式", "阅读菜单外观" };
        private static readonly Vector2[] Resolutions = { new(1920, 1080), new(1920, 1200), new(1440, 1080), new(2520, 1080) };
        [Serializable] private sealed class Identity { public string templateId; }
        [Serializable] private sealed class LayoutRecord
        {
            public string Key;
            public Vector2 Position, Size, Min, Max, Pivot;
            public Vector3 Scale;
            public float Spacing;
            public LayoutRecord(string key, RectTransform rect)
            {
                Key = key; Position = rect.anchoredPosition; Size = rect.sizeDelta; Min = rect.anchorMin;
                Max = rect.anchorMax; Pivot = rect.pivot; Scale = rect.localScale;
                if (rect.TryGetComponent<VerticalLayoutGroup>(out var layout)) Spacing = layout.spacing;
            }
            public void Apply(RectTransform rect)
            {
                rect.anchorMin = Min; rect.anchorMax = Max; rect.pivot = Pivot;
                rect.anchoredPosition = Position; rect.sizeDelta = Size; rect.localScale = Scale;
                if (rect.TryGetComponent<VerticalLayoutGroup>(out var layout)) layout.spacing = Spacing;
            }
        }
        [SerializeField] private List<LayoutRecord> _draft = new();
        [SerializeField] private string _sourceHash;
        [SerializeField] private int _selected = 1, _resolution;
        [SerializeField] private bool _showChoices;
        [SerializeField] private Vector2Int _customResolution = new(1920, 1080);
        [SerializeField] private Sprite _background, _left, _center, _right;
        private GameObject _contents, _previewRoot, _menuContents, _menuPreview;
        private PreviewRenderUtility _preview;
        private readonly Dictionary<string, RectTransform> _targets = new();
        private readonly Dictionary<string, RectTransform> _previewTargets = new();
        private string _message;
        private Vector2 _scroll, _elementScroll;
        private Rect _previewArea;
        private bool _dragging;
        private bool _rendering;
        private int _dragUndo;
        private Vector2 PixelSize => _resolution == Resolutions.Length ? (Vector2)_customResolution : Resolutions[Mathf.Clamp(_resolution, 0, Resolutions.Length - 1)];
        private Vector2 Size
        {
            get
            {
                var pixels = PixelSize;
                var scaler = _contents ? _contents.GetComponent<CanvasScaler>() : null;
                if (!scaler || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) return pixels;
                var ratio = pixels / scaler.referenceResolution;
                float scale = scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Expand ? Mathf.Min(ratio.x, ratio.y)
                    : scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Shrink ? Mathf.Max(ratio.x, ratio.y)
                    : Mathf.Pow(2, Mathf.Lerp(Mathf.Log(ratio.x, 2), Mathf.Log(ratio.y, 2), scaler.matchWidthOrHeight));
                return pixels / Mathf.Max(.001f, scale);
            }
        }
        #endregion

        // --------------------------------------------------------
        #region 生命周期
        private void OnEnable()
        {
            titleContent = new GUIContent("Gameplay 主UI布局"); minSize = new Vector2(1050, 600);
            saveChangesMessage = "Gameplay 布局尚未保存到阅读页 Prefab。";
            _tablesDirty = true;
            Undo.undoRedoPerformed += OnUndo;
            EditorApplication.playModeStateChanged += OnPlayMode;
            Selection.selectionChanged += OnNodeSelection;
            EditorApplication.projectChanged += OnPreviewProjectChanged;
            LoadContents();
        }
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndo;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            Selection.selectionChanged -= OnNodeSelection;
            EditorApplication.projectChanged -= OnPreviewProjectChanged;
            _tableEngine?.Dispose(); _tableEngine = null;
            ReleaseContents();
        }
        private void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode) ReleaseContents();
            if (state == PlayModeStateChange.EnteredEditMode) LoadContents();
        }
        private void OnUndo() { if (_contents) { Changed(); RebuildPreview(); } }
        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (new EditorGUI.DisabledScope(!_contents || !CanEdit(out _)))
                    if (GUILayout.Button("保存布局与外观", EditorStyles.toolbarButton, GUILayout.Width(110))) SaveChanges();
                if (GUILayout.Button("重新载入", EditorStyles.toolbarButton, GUILayout.Width(78)))
                {
                    if (!hasUnsavedChanges || EditorUtility.DisplayDialog("重新载入", "放弃未保存的布局修改？", "放弃并载入", "返回"))
                    { _draft.Clear(); _styles.Clear(); hasUnsavedChanges = false; LoadContents(); }
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("UI 开发中心", EditorStyles.toolbarButton, GUILayout.Width(96))) EUIPrefabManagerWindow.Open();
                if (GUILayout.Button("定位正式 Prefab", EditorStyles.toolbarButton, GUILayout.Width(110)))
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(IsMenuKey(Keys[_selected]) ? ReadingMenuPrefabPath : PrefabPath));
            }
            if (!CanEdit(out var reason)) { EditorGUILayout.HelpBox(reason, MessageType.Info); return; }
            if (!_contents) { EditorGUILayout.HelpBox(_message ?? "请重新载入阅读页。", MessageType.Warning); return; }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(150)))
                {
                    GUILayout.Label("阅读界面元素", EditorStyles.boldLabel);
                    _elementScroll = EditorGUILayout.BeginScrollView(_elementScroll);
                    for (int i = 0; i < Keys.Length; i++)
                        if (GUILayout.Toggle(_selected == i, Labels[i], "Button") && _selected != i) { _selected = i; _detailKey = null; RebuildPreview(); }
                    EditorGUILayout.EndScrollView();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.HelpBox("先选元素，再拖动画面中的橙色框。Ctrl+Z 撤销。", MessageType.None);
                }
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    EditorGUI.BeginChangeCheck();
                    _resolution = EditorGUILayout.Popup("预览分辨率", _resolution, new[] { "1920 × 1080", "1920 × 1200", "1440 × 1080", "2520 × 1080", "自定义" });
                    if (_resolution == Resolutions.Length)
                    {
                        var custom = EditorGUILayout.Vector2IntField("宽 × 高", _customResolution);
                        _customResolution = new Vector2Int(Mathf.Clamp(custom.x, 240, 7680), Mathf.Clamp(custom.y, 240, 7680));
                    }
                    if (!_nodePreview) _showChoices = EditorGUILayout.Toggle("显示示例选项", _showChoices);
                    if (EditorGUI.EndChangeCheck()) RebuildPreview();
                    DrawNodeControls();
                    var available = GUILayoutUtility.GetRect(200, 200, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    DrawPreview(available);
                    GUILayout.Label("画布比例与示例内容仅用于预览；保存不会改变剧情内容。", EditorStyles.miniLabel);
                }
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(290)))
                {
                    _scroll = EditorGUILayout.BeginScrollView(_scroll);
                    DrawProperties();
                    EditorGUILayout.EndScrollView();
                }
            }
            EditorGUILayout.HelpBox(_message ?? (hasUnsavedChanges ? "有未保存的布局修改" : "布局已载入。"), MessageType.None);
        }
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private static bool CanEdit(out string reason)
        {
            reason = null;
            if (EditorApplication.isPlayingOrWillChangePlaymode) reason = "请退出 Play Mode 后编辑布局。";
            else if (((EmberModuleAttribute)Attribute.GetCustomAttribute(typeof(NarrativeModule), typeof(EmberModuleAttribute)))?.Enabled != true)
                reason = "NarrativeModule 未启用。";
            else if (!File.Exists("Assets/Editor/EmberEditingTemplate.json") ||
                JsonUtility.FromJson<Identity>(File.ReadAllText("Assets/Editor/EmberEditingTemplate.json"))?.templateId != "visual-novel")
                reason = "当前加载模板不是 visual-novel，布局编辑已停止。";
            else if (PrefabStageUtility.GetCurrentPrefabStage()?.assetPath == PrefabPath || PrefabStageUtility.GetCurrentPrefabStage()?.assetPath == ReadingMenuPrefabPath)
                reason = "请先保存并关闭阅读页的 Prefab Mode，避免两个编辑入口互相覆盖。";
            return reason == null;
        }
        private static void FindTargets(GameObject root, Dictionary<string, RectTransform> result, GameObject menu)
        {
            result.Clear();
            var binding = root.GetComponent<EUIBinding>();
            if (!binding || EUIBindingEditorUtility.ValidateBinding(binding).HasError)
                throw new InvalidOperationException("阅读页 Binding 校验失败，请在 UI 开发中心修复。");
            foreach (string key in Keys.Where(k => k != "Dialogue" && k != "Shading" && k != "ChoiceTemplate" && !IsMenuKey(k)))
            {
                var entry = Array.Find(binding.Bindings, b => b.Name == key);
                if (!entry.GameObject || !(entry.GameObject.transform is RectTransform rect))
                    throw new InvalidOperationException("缺少真实绑定：" + key);
                result.Add(key, rect);
            }
            result.Add("Dialogue", (RectTransform)result["Speaker"].parent);
            var menuBinding = menu.GetComponent<EUIBinding>();
            if (!menuBinding || EUIBindingEditorUtility.ValidateBinding(menuBinding).HasError)
                throw new InvalidOperationException("阅读菜单 Binding 校验失败。");
            foreach (var key in MenuKeys)
            {
                var entry = Array.Find(menuBinding.Bindings, b => b.Name == (key == "MenuSettings" ? "Settings" : key));
                if (!entry.GameObject || !(entry.GameObject.transform is RectTransform rect))
                    throw new InvalidOperationException("缺少阅读菜单绑定：" + key);
                result.Add(key, rect);
            }
            AddAppearanceTargets(root, menu, result);
        }
        private void LoadContents()
        {
            ReleaseContents();
            if (!CanEdit(out _message)) return;
            try
            {
                string hash = SourceHash;
                if (_draft.Count > 0 && _sourceHash != hash)
                    throw new InvalidOperationException("Prefab 或依赖已被其他入口修改，已停止载入。草稿仍保留；重新载入将放弃草稿。" );
                _contents = PrefabUtility.LoadPrefabContents(PrefabPath);
                _menuContents = _menuContents ? _menuContents : PrefabUtility.LoadPrefabContents(ReadingMenuPrefabPath);
                FindTargets(_contents, _targets, _menuContents);
                foreach (var record in _draft) record.Apply(_targets[record.Key]);
                ApplyStyles();
                _sourceHash = hash; hasUnsavedChanges = _draft.Count > 0;
                const string sample = "Assets/GameResource/Resources/UI/Module/Narrative/Atlas/LastLight/";
                if (!_background) _background = AssetDatabase.LoadAssetAtPath<Sprite>(sample + "Backgrounds/rooftop_dusk.png");
                if (!_center) _center = AssetDatabase.LoadAssetAtPath<Sprite>(sample + "Portraits/lin_evening.png");
                RebuildPreview(); _message = "正式阅读页已载入；示例图片、对白、选项仅用于预览。";
            }
            catch (Exception ex) { ReleaseContents(); _message = ex.Message; }
        }
        private void ReleaseContents()
        {
            if (_preview != null) { _preview.Cleanup(); _preview = null; }
            _previewRoot = _menuPreview = null; _previewTargets.Clear();
            if (_menuContents) { foreach (var component in _menuContents.GetComponentsInChildren<Component>(true)) if (component) Undo.ClearUndo(component); Undo.ClearUndo(_menuContents); PrefabUtility.UnloadPrefabContents(_menuContents); }
            _menuContents = null;
            if (_contents) { foreach (var component in _contents.GetComponentsInChildren<Component>(true)) if (component) Undo.ClearUndo(component); Undo.ClearUndo(_contents); PrefabUtility.UnloadPrefabContents(_contents); }
            _contents = null; _targets.Clear();
        }
        private void Changed()
        {
            _draft = _targets.Select(p => new LayoutRecord(p.Key, p.Value)).ToList();
            CaptureStyles();
            hasUnsavedChanges = true; _message = "布局或外观已修改，尚未保存。";
            SyncPreview(); Repaint();
        }
        private void DrawProperties()
        {
            DrawDetailSelector();
            var rect = _targets[SelectedKey];
            GUILayout.Label(Labels[_selected], EditorStyles.boldLabel);
            EditorGUILayout.LabelField("位置与尺寸（画布单位）", EditorStyles.miniLabel);
            EditorGUI.BeginChangeCheck();
            var position = EditorGUILayout.Vector2Field("位置 X / Y", rect.anchoredPosition);
            var size = EditorGUILayout.Vector2Field("尺寸增量 W / H", rect.sizeDelta);
            var scale = EditorGUILayout.Vector3Field("缩放", rect.localScale);
            var min = EditorGUILayout.Vector2Field("锚点 Min", rect.anchorMin);
            var max = EditorGUILayout.Vector2Field("锚点 Max", rect.anchorMax);
            var pivot = EditorGUILayout.Vector2Field("轴心 Pivot", rect.pivot);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(rect, "调整小说 UI 布局");
                rect.anchorMin = Vector2.Min(min, max); rect.anchorMax = Vector2.Max(min, max);
                rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size;
                rect.localScale = new Vector3(Mathf.Max(.01f, scale.x), Mathf.Max(.01f, scale.y), Mathf.Max(.01f, scale.z));
                if (PrefabUtility.IsPartOfPrefabInstance(rect)) PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
                Changed();
            }
            EditorGUILayout.HelpBox("拉伸锚点下，尺寸增量是相对锚点区域的偏移，并非最终宽高。Y 正方向向上。", MessageType.None);
            if (rect.TryGetComponent<VerticalLayoutGroup>(out var layout))
            {
                EditorGUI.BeginChangeCheck();
                float spacing = EditorGUILayout.FloatField("选项间距", layout.spacing);
                if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(layout, "调整选项间距"); layout.spacing = spacing; Changed(); }
            }
            DrawAppearance(rect);
            GUILayout.Space(18); GUILayout.Label("预览图片（不保存到 Prefab）", EditorStyles.boldLabel);
            if (GUILayout.Button("使用规范示例：黄昏天台 / 林晚"))
            {
                const string sample = "Assets/GameResource/Resources/UI/Module/Narrative/Atlas/LastLight/";
                _background = AssetDatabase.LoadAssetAtPath<Sprite>(sample + "Backgrounds/rooftop_dusk.png");
                _center = AssetDatabase.LoadAssetAtPath<Sprite>(sample + "Portraits/lin_evening.png");
                _left = _right = null; _usePreviewContext = true;
                RebuildPreview();
            }
            EditorGUI.BeginChangeCheck();
            _background = (Sprite)EditorGUILayout.ObjectField("背景", _background, typeof(Sprite), false);
            _left = (Sprite)EditorGUILayout.ObjectField("左立绘", _left, typeof(Sprite), false);
            _center = (Sprite)EditorGUILayout.ObjectField("中立绘", _center, typeof(Sprite), false);
            _right = (Sprite)EditorGUILayout.ObjectField("右立绘", _right, typeof(Sprite), false);
            if (EditorGUI.EndChangeCheck()) RebuildPreview();
        }
        private void RebuildPreview()
        {
            if (_preview != null) _preview.Cleanup();
            _preview = new PreviewRenderUtility();
            // Create the UI under a preview-scene parent, never in the user's open gameplay scene.
            var host = new GameObject("NovelLayoutPreviewHost");
            _preview.AddSingleGO(host);
            _previewRoot = Instantiate(_contents, host.transform, false); _previewRoot.name = "ReaderLayoutPreview";
            _menuPreview = Instantiate(_menuContents, host.transform, false);
            FindTargets(_previewRoot, _previewTargets, _menuPreview);
            // 仅预览克隆合入同一画布；正式菜单仍是独立 EUI Popup。
            _menuPreview.transform.SetParent(_previewRoot.transform, false);
            DestroyImmediate(_menuPreview.GetComponent<GraphicRaycaster>());
            DestroyImmediate(_menuPreview.GetComponent<CanvasScaler>());
            DestroyImmediate(_menuPreview.GetComponent<Canvas>());
            var canvas = _previewRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = _preview.camera;
            if (_previewRoot.TryGetComponent<CanvasScaler>(out var scaler)) scaler.enabled = false;
            // 独立预览相机不使用当前 Game View 的屏幕坐标；仅在预览副本中展示无刘海安全区。
            foreach (var safeArea in host.GetComponentsInChildren<EUISafeArea>(true))
            {
                safeArea.enabled = false;
                var safeRect = (RectTransform)safeArea.transform;
                safeRect.anchorMin = Vector2.zero; safeRect.anchorMax = Vector2.one;
                safeRect.offsetMin = safeRect.offsetMax = Vector2.zero;
            }
            foreach (var animator in host.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var group in host.GetComponentsInChildren<CanvasGroup>(true)) group.alpha = 1;
            bool samples = !_nodePreview || _usePreviewContext;
            SetPicture("Background", samples ? _background : null); SetPicture("Left", samples ? _left : null);
            SetPicture("Center", samples ? _center : null); SetPicture("Right", samples ? _right : null);
            _previewTargets["Speaker"].GetComponent<TMP_Text>().text = "林晚";
            _previewTargets["Body"].GetComponent<TMP_Text>().text = "等这盏灯亮起来，我们就一起回去。\n今天的故事，还没有结束。";
            _previewTargets["Status"].GetComponent<TMP_Text>().text = "";
            var prototype = Array.Find(_previewRoot.GetComponent<EUIBinding>().Bindings, b => b.Name == "ChoiceTemplate").GameObject;
            if ((_showChoices || Keys[_selected] == "ChoiceTemplate") && !_nodePreview)
                for (int i = 0; i < 3; i++)
                {
                    var choice = Instantiate(prototype, _previewTargets["Choices"]); choice.SetActive(true);
                    choice.GetComponentInChildren<TMP_Text>(true).text = "示例选项 " + (i + 1);
                }
            if (_nodePreview) ApplyNodePreview();
            SyncPreview();
        }
        private void SetPicture(string key, Sprite sprite)
        {
            var image = _previewTargets[key].GetComponentInChildren<Image>(true);
            image.sprite = sprite; image.color = sprite ? Color.white : Color.clear;
        }
        private void SyncPreview()
        {
            if (!_previewRoot) return;
            foreach (var pair in _targets) new LayoutRecord(pair.Key, pair.Value).Apply(_previewTargets[pair.Key]);
            var root = (RectTransform)_previewRoot.transform;
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f);
            root.sizeDelta = Size; root.position = Vector3.zero; root.rotation = Quaternion.identity; root.localScale = Vector3.one;
            var menuRoot = (RectTransform)_menuPreview.transform;
            menuRoot.anchorMin = menuRoot.anchorMax = menuRoot.pivot = new Vector2(.5f, .5f);
            menuRoot.sizeDelta = Size; menuRoot.position = new Vector3(0, 0, -1); menuRoot.rotation = Quaternion.identity; menuRoot.localScale = Vector3.one;
            _menuPreview.SetActive(IsMenuKey(Keys[_selected]));
            Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            LayoutRebuilder.ForceRebuildLayoutImmediate(menuRoot);
            foreach (var text in _menuPreview.GetComponentsInChildren<TMP_Text>(true)) text.ForceMeshUpdate();
            foreach (var text in _previewRoot.GetComponentsInChildren<TMP_Text>(true)) text.ForceMeshUpdate();
        }
        private void DrawPreview(Rect available)
        {
            if (_preview == null) return;
            float fit = Mathf.Min((available.width - 16) / Size.x, (available.height - 16) / Size.y);
            var area = _previewArea = new Rect(available.center - Size * fit * .5f, Size * fit);
            if (Event.current.type == EventType.Repaint && !_rendering)
            {
                GUI.DrawTexture(area, RenderPreview(area), ScaleMode.StretchToFill, false);
            }
            DrawHitArea(area, fit);
            var corners = new Vector3[4]; _previewTargets[SelectedKey].GetWorldCorners(corners);
            var bounds = new Rect(area.x + (corners[0].x + Size.x / 2) * fit,
                area.y + (Size.y / 2 - corners[1].y) * fit, (corners[2].x - corners[0].x) * fit, (corners[1].y - corners[0].y) * fit);
            Handles.BeginGUI(); Handles.color = new Color(1, .65f, .15f);
            Handles.DrawAAPolyLine(2, new Vector3(bounds.xMin,bounds.yMin),new Vector3(bounds.xMax,bounds.yMin),
                new Vector3(bounds.xMax,bounds.yMax),new Vector3(bounds.xMin,bounds.yMax),new Vector3(bounds.xMin,bounds.yMin)); Handles.EndGUI();
            GUI.Label(new Rect(bounds.xMin, bounds.yMin - 19, 140, 20), Labels[_selected], EditorStyles.whiteLabel);
            var evt = Event.current;
            int control = GUIUtility.GetControlID(FocusType.Passive);
            if (evt.type == EventType.MouseDown && evt.button == 0 && area.Contains(evt.mousePosition) && bounds.Contains(evt.mousePosition))
            {
                _dragging = true; GUIUtility.hotControl = control; Undo.IncrementCurrentGroup(); _dragUndo = Undo.GetCurrentGroup();
                Undo.RegisterCompleteObjectUndo(_targets[SelectedKey], "拖动小说 UI"); evt.Use();
            }
            if (evt.type == EventType.MouseDrag && _dragging && GUIUtility.hotControl == control)
            {
                var rect = _targets[SelectedKey]; var parentScale = _previewTargets[SelectedKey].parent.lossyScale;
                rect.anchoredPosition += new Vector2(evt.delta.x / fit / parentScale.x, -evt.delta.y / fit / parentScale.y);
                if (PrefabUtility.IsPartOfPrefabInstance(rect)) PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
                Changed(); evt.Use();
            }
            if (evt.type == EventType.MouseUp && _dragging)
            { _dragging = false; GUIUtility.hotControl = 0; Undo.CollapseUndoOperations(_dragUndo); evt.Use(); }
        }
        private Texture RenderPreview(Rect area, bool snapshot = false)
        {
            _rendering = true;
            try
            {
                if (snapshot) _preview.BeginStaticPreview(area); else _preview.BeginPreview(area, GUIStyle.none);
                Texture result = null;
                try
                {
                    var camera = _preview.camera;
                    // URP's Preview camera path excludes uGUI. A disabled Game camera still stays
                    // isolated by PreviewRenderUtility's camera.scene and the unique preview-scene mask.
                    camera.cameraType = CameraType.Game;
                    camera.rect = new Rect(0, 0, 1, 1);
                    camera.orthographic = true; camera.orthographicSize = Size.y / 2;
                    camera.transform.position = new Vector3(0, 0, -10); camera.transform.rotation = Quaternion.identity;
                    camera.nearClipPlane = .1f; camera.farClipPlane = 100;
                    camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.06f, .08f, .11f);
                    _preview.Render(true);
                }
                finally { result = snapshot ? _preview.EndStaticPreview() : _preview.EndPreview(); }
                return result;
            }
            finally { _rendering = false; }
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public static void Open() { if (CanOpen()) GetWindow<NovelGameplayLayoutWindow>().Show(); }
        public static bool CanOpen() => CanEdit(out _);
        public override void SaveChanges()
        {
            if (!_contents || !CanEdit(out _message)) return;
            if (SourceHash != _sourceHash)
            { _message = "Prefab 或依赖已在外部改变，本次拒绝覆盖。请先重新载入。"; return; }
            try
            {
                _menuContents = _menuContents ? _menuContents : PrefabUtility.LoadPrefabContents(ReadingMenuPrefabPath);
                FindTargets(_contents, _targets, _menuContents);
                string folder = ".utmp/visual-novel-ui-layout/" + DateTime.Now.ToString("yyyyMMdd-HHmmssfff");
                Directory.CreateDirectory(folder); File.Copy(PrefabPath, folder + "/EUINovelReaderPage.prefab");
                File.Copy(PrefabPath + ".meta", folder + "/EUINovelReaderPage.prefab.meta");
                File.Copy(ReadingMenuPrefabPath, folder + "/EUINovelReadingMenuPage.prefab");
                File.Copy(ReadingMenuPrefabPath + ".meta", folder + "/EUINovelReadingMenuPage.prefab.meta");
                foreach (var target in _targets.Values)
                    if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                PrefabUtility.SaveAsPrefabAsset(_contents, PrefabPath, out bool success);
                if (!success) throw new IOException("Prefab 保存失败，修改仍保留在窗口中。");
                PrefabUtility.SaveAsPrefabAsset(_menuContents, ReadingMenuPrefabPath, out bool menuSuccess);
                if (!menuSuccess) throw new IOException("阅读菜单保存失败，修改与备份仍保留。");
                var binding = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<EUIBinding>();
                File.WriteAllText(folder + "/binding.json", JsonUtility.ToJson(EUIBindingEditorUtility.GetBindingSnapshot(binding), true));
                _sourceHash = SourceHash; _draft.Clear(); _styles.Clear();
                base.SaveChanges(); _message = "布局已保存到正式阅读页与阅读菜单；备份：" + folder;
            }
            catch (Exception ex) { _message = ex.Message; }
        }
        public override void DiscardChanges() { _draft.Clear(); _styles.Clear(); base.DiscardChanges(); LoadContents(); }
        #endregion
    }
}

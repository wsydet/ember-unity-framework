using System;
using System.Linq;
using Ember.Core.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Ember.LevelAuthoring.Editor
{
    /// <summary>以 Scene 视图为画布。窗口不拥有运行会话，关闭后仍可重新连接。</summary>
    public sealed class EmberLevelWorkbench : EditorWindow
    {
        #region 内部参数
        private EmberLevelDescriptor[] _levels = Array.Empty<EmberLevelDescriptor>();
        [SerializeField] private string _templateScope;
        [SerializeField] private string _extensionScope;
        private bool HasAccess => EmberLevelAccess.Available && _templateScope == (EmberFsmBlueprintAccess.CurrentTemplateId ?? "")
            && EmberExtensionProject.AllowsScope(EmberExtensionProject.LevelAuthoring, _extensionScope);
        private EmberLevelMaterial[] _materials = Array.Empty<EmberLevelMaterial>();
        private EmberLevelDiagnostic[] _diagnostics = Array.Empty<EmberLevelDiagnostic>();
        [SerializeField] private int _levelIndex, _category;
        [SerializeField] private string _search = "", _message = "", _newName = "Level", _sizeId = "custom-size";
        [SerializeField] private Vector2 _scroll, _size = Vector2.one, _testPosition;
        [SerializeField] private Vector2Int _anchor;
        [SerializeField] private string _checkpoint = "";
        [SerializeField] private EmberLevelMaterial _material;
        [SerializeField] private EmberTerrainTile _style;
        [SerializeField] private EmberTerrainPreset _sizePreset;
        [SerializeField] private Tilemap _map;
        [SerializeField] private bool _placing, _erase, _rectangle, _favoritesOnly;
        [SerializeField] private bool _snap = true, _materialPhysics, _allowRotation, _allowScale, _allowMirror;
        [SerializeField] private float _snapStep = .5f, _rotation;
        [SerializeField] private Vector3 _placementScale = Vector3.one;
        [SerializeField] private string _objectAdapter = "";
        private string _objectError = "";
        private bool _messageError;
        private Vector2 _cameraPreview;
        private Vector3Int _begin, _last;
        private int _stroke = -1;
        private UnityEditor.Editor _inspector;
        private EmberLevelDescriptor Level => _levels.Length > 0 ? _levels[Mathf.Clamp(_levelIndex, 0, _levels.Length - 1)] : null;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void OnEnable() { if (string.IsNullOrEmpty(_templateScope)) _templateScope = EmberFsmBlueprintAccess.CurrentTemplateId ?? "";
            if (string.IsNullOrEmpty(_extensionScope)) _extensionScope = EmberExtensionProject.CaptureScope(EmberExtensionProject.LevelAuthoring);
            titleContent = new GUIContent("2D 关卡工作台"); SceneView.duringSceneGui += SceneGUI; Reload(); }
        private void OnDisable() { CancelStroke(); SceneView.duringSceneGui -= SceneGUI; if (_inspector != null) DestroyImmediate(_inspector); }
        private void OnSelectionChange() { if (_inspector != null) DestroyImmediate(_inspector); _inspector = null; _objectError = ""; Repaint(); }
        private void OnGUI()
        {
            if (!HasAccess)
            { CancelStroke(); EditorGUILayout.HelpBox("关卡制作扩展已关闭，或活动模板已变化。资产与草稿保留。", MessageType.Info); if (EmberLevelTrialSession.Active && GUILayout.Button("停止试玩并恢复现场")) EmberLevelTrialSession.Stop(); return; }
            if (!EmberLevelAdapters.All.Any(a => string.IsNullOrEmpty(a.UnavailableReason)))
            { EditorGUILayout.HelpBox("缺少当前模板和模块允许的关卡适配器。请登记项目接线。", MessageType.Warning); return; }
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton)) Reload();
                if (_levels.Length > 0) _levelIndex = EditorGUILayout.Popup(_levelIndex, _levels.Select(l => l.displayName).ToArray());
                if (GUILayout.Button("打开", EditorStyles.toolbarButton)) Run(() => { if (Level != null && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Level.ScenePath); });
                if (GUILayout.Button("保存", EditorStyles.toolbarButton)) Run(() => { EditorSceneManager.SaveOpenScenes(); AssetDatabase.SaveAssets(); });
                if (GUILayout.Button("检查", EditorStyles.toolbarButton)) Run(Validate);
            }
            EditorGUILayout.LabelField(EmberLevelTrialSession.Status, EditorStyles.wordWrappedLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(Level == null || EmberLevelTrialSession.Active || !EmberExtensionProject.IsEnabled(EmberExtensionProject.LevelTrial)))
                {
                    if (GUILayout.Button("入口试玩")) Run(() => EmberLevelTrialSession.Start(Level, EmberLevelStartMode.Entrance, Vector2.zero));
                    if (GUILayout.Button("位置试玩")) Run(() => EmberLevelTrialSession.Start(Level, EmberLevelStartMode.Position, _testPosition));
                    if (GUILayout.Button("检查点试玩")) Run(() => EmberLevelTrialSession.Start(Level, EmberLevelStartMode.Checkpoint, _testPosition, _checkpoint));
                }
                if (GUILayout.Button("停止")) Run(EmberLevelTrialSession.Stop);
                if (GUILayout.Button("重试")) Run(EmberLevelTrialSession.Retry);
            }
            _testPosition = EditorGUILayout.Vector2Field("测试位置（不改出生配置）", _testPosition);
            _checkpoint = EditorGUILayout.TextField("已登记检查点ID", _checkpoint);
            if (!string.IsNullOrEmpty(_objectError)) EditorGUILayout.HelpBox(_objectError, MessageType.Error);
            if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, _messageError ? MessageType.Error : MessageType.Info);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                _newName = EditorGUILayout.TextField("新关名称", _newName);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("从模板新建")) Create(false);
                    if (GUILayout.Button("复制当前关")) Create(true);
                }
                _category = GUILayout.Toolbar(_category, new[] { "地形", "机关", "背景", "装饰" });
                if (_category > 0)
                {
                    var adapters = EmberLevelAdapters.ObjectAdapters.Select(a => a.Id).ToArray();
                    if (_category == 1 && adapters.Length > 0)
                        _objectAdapter = adapters[EditorGUILayout.Popup("机关适配器", Mathf.Max(0, Array.IndexOf(adapters, _objectAdapter)), adapters)];
                    _materialPhysics = EditorGUILayout.Toggle("素材声明物理交互", _materialPhysics);
                    _allowRotation = EditorGUILayout.Toggle("允许旋转", _allowRotation);
                    _allowScale = EditorGUILayout.Toggle("允许缩放", _allowScale);
                    _allowMirror = EditorGUILayout.Toggle("允许镜像", _allowMirror);
                }
                if (_category > 0 && GUILayout.Button("将选中的 Prefab / Sprite 登记为素材")) Run(() =>
                {
                    var prefab = Selection.activeObject as GameObject; var sprite = Selection.activeObject as Sprite;
                    if (prefab != null && !PrefabUtility.IsPartOfPrefabAsset(prefab)) throw new InvalidOperationException("请选择项目中的 Prefab 资产。");
                    if (prefab == null && sprite == null) throw new InvalidOperationException("请先在 Project 中选择 Prefab 或 Sprite。");
                    string path = EditorUtility.SaveFilePanelInProject("登记素材", Selection.activeObject.name + "Material", "asset", "更新时选择原目录项，保持ID");
                    if (string.IsNullOrEmpty(path)) return;
                    var existing = AssetDatabase.LoadAssetAtPath<EmberLevelMaterial>(path); string id = existing != null ? existing.Id : Guid.NewGuid().ToString("N");
                    if (_category == 1 && EmberLevelAdapters.GetObject(_objectAdapter) == null) throw new InvalidOperationException("请先登记并选择支持该机关的编辑适配器。");
                    EmberLevelAssetService.SaveMaterial(path, id, m => m.Configure(id, Selection.activeObject.name, (EmberLevelMaterialKind)_category, prefab, sprite, null, adapterId: _category == 1 ? _objectAdapter : "", rotation: _allowRotation, scale: _allowScale, mirror: _allowMirror, hasPhysics: _materialPhysics)); Reload();
                });
                _search = EditorGUILayout.TextField("搜索素材", _search);
                _favoritesOnly = EditorGUILayout.Toggle("仅收藏", _favoritesOnly);
                foreach (var material in _materials.Where(m => (int)m.Kind == _category &&
                    (m.DisplayName + m.Description).IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0 && (!_favoritesOnly || Favorite(m))))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(Favorite(material) ? "★" : "☆", GUILayout.Width(28))) EditorPrefs.SetBool(Key(material), !Favorite(material));
                        if (GUILayout.Button(new GUIContent(material.DisplayName, AssetPreview.GetMiniThumbnail(material.Prefab != null ? material.Prefab : material.Sprite), material.Description)))
                        { _material = material; _placing = true; EditorPrefs.SetString("Ember.Level.Recent." + Application.dataPath, material.Id); SceneView.RepaintAll(); }
                    }
                }
                if (_material != null) EditorGUILayout.HelpBox(_material.DisplayName + " — " + _material.Description + "\nScene点击放置，Esc取消。", MessageType.Info);
                _placing = EditorGUILayout.Toggle("启用 Scene 放置", _placing);
                if (_category > 0)
                {
                    _snap = EditorGUILayout.Toggle("网格吸附", _snap);
                    _snapStep = EditorGUILayout.FloatField("吸附步长", _snapStep);
                    _rotation = EditorGUILayout.FloatField("放置旋转 Z", _rotation);
                    _placementScale = EditorGUILayout.Vector3Field("放置缩放/镜像", _placementScale);
                }
                if (_category == 0)
                {
                    _map = (Tilemap)EditorGUILayout.ObjectField("目标 Tilemap", _map, typeof(Tilemap), true);
                    _erase = EditorGUILayout.Toggle("擦除", _erase); _rectangle = EditorGUILayout.Toggle("拖拽矩形", _rectangle);
                    if (GUILayout.Button("打开原生 Tile Palette")) Run(() => { if (!EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette")) throw new InvalidOperationException("未安装/启用 Tile Palette 编辑包；可继续使用工作台绘制，不会自动安装依赖。"); });
                    if (GUILayout.Button("重建当前 Tilemap 接缝")) Run(() => { if (_map != null) _map.RefreshAllTiles(); });
                    DrawSize();
                }
                DrawSelection();
            }
            foreach (var diagnostic in _diagnostics)
                if (GUILayout.Button(diagnostic.RuleId + " · " + diagnostic.Message, EditorStyles.helpBox)) { Selection.activeObject = diagnostic.Target; EditorGUIUtility.PingObject(diagnostic.Target); }
            EditorGUILayout.EndScrollView();
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static string Key(EmberLevelMaterial material) => "Ember.Level.Favorite." + Application.dataPath + "." + material.Id;
        private static bool Favorite(EmberLevelMaterial material) => EditorPrefs.GetBool(Key(material));
        private void Run(Action action) { try { action(); _message = ""; _messageError = false; } catch (Exception error) { _message = error.Message; _messageError = true; } Repaint(); }
        private void Reload()
        {
            _levels = EmberLevelAdapters.All.Where(a => string.IsNullOrEmpty(a.UnavailableReason)).SelectMany(a => a.Discover()).ToArray();
            _materials = EmberLevelAssetService.Find<EmberLevelMaterial>();
            string recent = EditorPrefs.GetString("Ember.Level.Recent." + Application.dataPath, "");
            if (_material == null) _material = _materials.FirstOrDefault(m => m.Id == recent);
        }
        private void Validate()
        {
            var list = Level == null ? new System.Collections.Generic.List<EmberLevelDiagnostic>() : EmberLevelAdapters.Get(Level.adapterId).Validate(Level, null).ToList();
            foreach (var duplicate in _materials.GroupBy(m => m.Id).Where(g => g.Count() > 1))
                list.Add(new EmberLevelDiagnostic("material.duplicate", "重复素材ID：" + duplicate.Key, EmberLevelSeverity.Error, duplicate.First()));
            if (_material?.Terrain?.Terrain != null)
                list.AddRange(EmberTerrainEditing.ValidateRules(_material.Terrain.Terrain).Select(m => new EmberLevelDiagnostic("terrain.rules", m, EmberLevelSeverity.Warning, _material.Terrain.Terrain)));
            _diagnostics = list.ToArray(); _message = _diagnostics.Length == 0 ? "本次检查未发现问题。" : "";
        }
        private void Create(bool duplicate)
        {
            Run(() =>
            {
                var adapter = Level != null ? EmberLevelAdapters.Get(Level.adapterId) : EmberLevelAdapters.All.First();
                string folder = EditorUtility.OpenFolderPanel("选择新关目录", Application.dataPath, "");
                if (string.IsNullOrEmpty(folder)) return;
                string relative = FileUtil.GetProjectRelativePath(folder);
                adapter.Create(relative, _newName, duplicate ? Level : null); Reload();
            });
        }
        private void DrawSize()
        {
            EditorGUILayout.Space(); EditorGUILayout.LabelField("制作尺寸预设（宽×高，世界单位）", EditorStyles.boldLabel);
            var selected = (EmberTerrainPreset)EditorGUILayout.ObjectField("更新已有预设", _sizePreset, typeof(EmberTerrainPreset), false);
            if (selected != _sizePreset)
            { _sizePreset = selected; if (selected != null) { _sizeId = selected.Id; _size = selected.Size; _anchor = selected.Anchor; _style = selected.Terrain; } }
            _sizeId = EditorGUILayout.TextField("稳定ID", _sizeId); _size = EditorGUILayout.Vector2Field("尺寸", _size);
            _anchor = EditorGUILayout.Vector2IntField("锚点（格偏移）", _anchor);
            _style = (EmberTerrainTile)EditorGUILayout.ObjectField("地形样式/网格/物理类型", _style, typeof(EmberTerrainTile), false);
            if (_style != null) RunSizePreview();
            if (GUILayout.Button("保存并登记尺寸")) Run(() =>
            {
                string path = _sizePreset != null ? AssetDatabase.GetAssetPath(_sizePreset) : EditorUtility.SaveFilePanelInProject("保存尺寸预设", _sizeId, "asset", "选择项目自有目录");
                if (string.IsNullOrEmpty(path)) return;
                _sizePreset = EmberLevelAssetService.SaveSize(path, _sizeId, _size, _anchor, _style);
                string catalogPath = System.IO.Path.ChangeExtension(path, null) + "Material.asset";
                EmberLevelAssetService.SaveMaterial(catalogPath, _sizeId, m => m.Configure(_sizeId, $"{_size.x}×{_size.y} {_style.Physics}", EmberLevelMaterialKind.Terrain, null, null, _sizePreset)); Reload();
            });
        }
        private void RunSizePreview()
        {
            try { var cell = _map != null ? EmberTerrainEditing.GetWorldCellSize(_map) : _style.CellSize; var count = EmberTerrainEditing.GetCellCount(_size, cell); EditorGUILayout.LabelField($"{(_map != null ? "目标世界格" : "未缩放参考格")} {cell} · 占格 {count.x}×{count.y} · {_style.Physics}"); }
            catch (Exception error) { EditorGUILayout.HelpBox(error.Message, MessageType.Warning); }
        }
        private void DrawSelection()
        {
            var selected = Selection.activeGameObject;
            if (selected == null || !selected.scene.IsValid()) return;
            EditorGUILayout.Space(); EditorGUILayout.LabelField("所选对象：" + selected.name, EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                // 对象/选择变化或 Undo 回滚会使当前 Inspector 与布局失效，下一帧重新绘制。
                if (GUILayout.Button("复制")) { Run(DuplicateSelection); GUIUtility.ExitGUI(); }
                if (GUILayout.Button("删除")) { Run(DeleteSelection); GUIUtility.ExitGUI(); }
                if (GUILayout.Button("锁定")) EmberLevelObjectEditing.SetEditorVisibility(selected, true, true);
                if (GUILayout.Button("解锁")) EmberLevelObjectEditing.SetEditorVisibility(selected, true, false);
                if (GUILayout.Button("隐藏")) EmberLevelObjectEditing.SetEditorVisibility(selected, false, false);
                if (GUILayout.Button("显示全部")) SceneVisibilityManager.instance.ShowAll();
            }
            var renderer = selected.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                EditorGUI.BeginChangeCheck();
                var color = EditorGUILayout.ColorField("色调/透明度", renderer.color);
                var layers = SortingLayer.layers; int index = Array.FindIndex(layers, l => l.id == renderer.sortingLayerID);
                index = EditorGUILayout.Popup("Sorting Layer", Mathf.Max(0, index), layers.Select(l => l.name).ToArray());
                int order = EditorGUILayout.IntField("排序", renderer.sortingOrder);
                bool tiled = EditorGUILayout.Toggle("平铺覆盖", renderer.drawMode == SpriteDrawMode.Tiled);
                var size = EditorGUILayout.Vector2Field("覆盖尺寸", renderer.size);
                if (EditorGUI.EndChangeCheck()) Run(() => EmberLevelObjectEditing.ConfigureRenderer(renderer, color, layers[index].id, order, size, tiled));
                if (_category == 2 && GUILayout.Button("添加基础视差（默认关闭）")) Run(() =>
                { var layer = selected.GetComponent<EmberParallaxLayer>() ?? Undo.AddComponent<EmberParallaxLayer>(selected); Undo.RecordObject(layer, "视差基准"); layer.Configure(null, Vector2.zero, selected.transform.position); });
            }
            if (_inspector == null) _inspector = UnityEditor.Editor.CreateEditor(selected.transform);
            var parallax = selected.GetComponent<EmberParallaxLayer>();
            if (parallax != null)
            {
                EditorGUI.BeginChangeCheck();
                var camera = (Camera)EditorGUILayout.ObjectField("视差参考相机", parallax.ReferenceCamera, typeof(Camera), true);
                var factor = EditorGUILayout.Vector2Field("视差系数", parallax.Factor);
                var baseline = EditorGUILayout.Vector3Field("编辑基准位置", parallax.Baseline);
                if (EditorGUI.EndChangeCheck()) Run(() => { Undo.RecordObject(parallax, "配置视差"); parallax.Configure(camera, factor, baseline); PrefabUtility.RecordPrefabInstancePropertyModifications(parallax); EditorSceneManager.MarkSceneDirty(selected.scene); });
                _cameraPreview = EditorGUILayout.Vector2Field("预览相机位移（不落盘）", _cameraPreview);
                if (GUILayout.Button("重置预览")) _cameraPreview = Vector2.zero;
            }
            _inspector.OnInspectorGUI();
            EditorGUILayout.HelpBox("机关参数与Collider范围使用原生Inspector/Scene手柄；读取实际组件，支持Prefab覆写与Undo。", MessageType.None);
        }
        private void CancelStroke() { if (_stroke >= 0) Undo.RevertAllDownToGroup(_stroke); _stroke = -1; }
        private void DuplicateSelection() => Selection.activeGameObject = EmberLevelObjectEditing.Duplicate(Selection.activeGameObject);
        private void DeleteSelection() => EmberLevelObjectEditing.Delete(Selection.activeGameObject);
        private void DrawSelectedScene()
        {
            var selected = Selection.activeGameObject;
            if (selected != null) EmberLevelAdapters.ResolveObject(selected)?.DrawScene(selected);
        }
        private void SceneGUI(SceneView view)
        {
            if (!HasAccess) { CancelStroke(); return; }
            if (Selection.activeGameObject != null)
            {
                var selected = Selection.activeGameObject;
                try { DrawSelectedScene(); if (_objectError.Length > 0) { _objectError = ""; Repaint(); } }
                catch (Exception error) { if (_objectError != error.Message) { _objectError = error.Message; Repaint(); } Handles.Label(selected.transform.position, error.Message); }
                var layer = selected.GetComponent<EmberParallaxLayer>(); var renderer = selected.GetComponent<SpriteRenderer>();
                if (layer != null && renderer != null)
                {
                    Handles.color = Color.yellow;
                    var delta = layer.Evaluate(_cameraPreview, Vector3.zero) - selected.transform.position;
                    Handles.DrawWireCube(renderer.bounds.center + delta, renderer.bounds.size);
                    Handles.Label(renderer.bounds.center + delta, "视差预览 · 不修改场景");
                    var camera = layer.ReferenceCamera;
                    if (camera != null && camera.orthographic)
                        Handles.DrawWireCube(camera.transform.position + (Vector3)_cameraPreview, new Vector3(camera.orthographicSize * 2 * camera.aspect, camera.orthographicSize * 2, 0));
                }
            }
            if (!_placing || _material == null || EditorApplication.isPlayingOrWillChangePlaymode || !EmberLevelAdapters.All.Any(a => string.IsNullOrEmpty(a.UnavailableReason))) return;
            var ev = Event.current;
            if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Escape) { CancelStroke(); _placing = false; ev.Use(); Repaint(); return; }
            var ray = HandleUtility.GUIPointToWorldRay(ev.mousePosition);
            var normal = _map != null && _material.Kind == EmberLevelMaterialKind.Terrain ? _map.transform.forward : Vector3.forward;
            var plane = new Plane(normal, _map != null ? _map.transform.position : Vector3.zero);
            if (!plane.Raycast(ray, out float distance)) return;
            Vector3 position = ray.GetPoint(distance);
            if (_material.Kind != EmberLevelMaterialKind.Terrain && _snap && float.IsFinite(_snapStep) && _snapStep > 0)
            { position.x = Mathf.Round(position.x / _snapStep) * _snapStep; position.y = Mathf.Round(position.y / _snapStep) * _snapStep; }
            Vector3Int cell = _map != null ? _map.WorldToCell(position) : Vector3Int.zero;
            if (_material.Kind == EmberLevelMaterialKind.Terrain && _map != null && _material.Terrain != null)
            {
                Vector2Int count;
                try { count = EmberTerrainEditing.GetCellCount(_material.Terrain.Size, EmberTerrainEditing.GetWorldCellSize(_map)); }
                catch (Exception error) { Handles.Label(position, error.Message); return; }
                Handles.color = _erase ? Color.red : Color.cyan;
                var origin = cell - new Vector3Int(_material.Terrain.Anchor.x, _material.Terrain.Anchor.y, 0);
                var start = _map.CellToWorld(origin);
                var corners = new[] { start, _map.CellToWorld(origin + new Vector3Int(count.x,0,0)), _map.CellToWorld(origin + new Vector3Int(count.x,count.y,0)), _map.CellToWorld(origin + new Vector3Int(0,count.y,0)), start };
                Handles.DrawAAPolyLine(corners);
                Handles.Label(start, $"{_material.Terrain.Size.x}×{_material.Terrain.Size.y} · {count.x}×{count.y}格");
            }
            else { Handles.color = Color.cyan; Handles.DrawWireDisc(position, normal, .3f); Handles.Label(position, _material.DisplayName); }
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if (ev.button == 0 && !ev.alt && ev.type == EventType.MouseDown)
            {
                Run(() =>
                {
                    if (_material.Kind != EmberLevelMaterialKind.Terrain) { EmberLevelObjectEditing.Place(_material, SceneManager.GetActiveScene(), position, Quaternion.Euler(0, 0, _rotation), _placementScale); return; }
                    Undo.IncrementCurrentGroup(); _stroke = Undo.GetCurrentGroup(); _begin = _last = cell;
                    if (!_rectangle) EmberTerrainEditing.Stamp(_map, cell, _material.Terrain, _erase);
                }); ev.Use();
            }
            if (ev.type == EventType.MouseDrag && _stroke >= 0 && !_rectangle && cell != _last)
            { Run(() => EmberTerrainEditing.Stamp(_map, cell, _material.Terrain, _erase)); _last = cell; ev.Use(); }
            if (ev.type == EventType.MouseUp && _stroke >= 0)
            {
                Run(() => { if (_rectangle) { var min = Vector3Int.Min(_begin, cell); var max = Vector3Int.Max(_begin, cell); EmberTerrainEditing.Paint(_map, new BoundsInt(min, max - min + Vector3Int.one), _material.Terrain.Terrain, _erase); } Undo.CollapseUndoOperations(_stroke); _stroke = -1; }); ev.Use();
            }
            view.Repaint();
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Open()
        {
            EmberExtensionProject.Require(EmberExtensionProject.LevelAuthoring);
            string scope = EmberFsmBlueprintAccess.CurrentTemplateId ?? "";
            var window = Resources.FindObjectsOfTypeAll<EmberLevelWorkbench>().FirstOrDefault(w => w._templateScope == scope && w.HasAccess) ?? CreateInstance<EmberLevelWorkbench>();
            window._templateScope = scope; window.Show();
        }
        #endregion
    }
}

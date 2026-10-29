using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Core.Fsm;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ember.Core.Editor
{
    /// <summary>只读声明画布。复合框使用画布坐标，包含关系只来自真实图。</summary>
    public sealed partial class EmberFsmBlueprintWindow : EditorWindow
    {
        #region 编辑器面板参数
        [SerializeField] private EmberFsmGraph _graph;
        [SerializeField] private string _templateScope;
        [SerializeField] private string _extensionScope;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private BlueprintCanvas _canvas;
        private Label _details, _errors, _layoutNotice;
        private ObjectField _graphField;
        private ScrollView _hierarchy;
        private EmberFsmGraphDescription _description;
        private EmberFsmNodeDescription _selected;
        private readonly Dictionary<string, Node> _nodes = new();
        private readonly Dictionary<string, Vector2> _positions = new();
        private readonly Dictionary<string, Vector2> _sizes = new();
        private readonly Dictionary<string, string> _parents = new();
        private readonly Dictionary<string, EmberFsmNodeDescription> _map = new();
        private readonly HashSet<string> _collapsed = new();
        private bool _legacyLayout;
        private string LayoutKey => "Ember.Fsm.Layout." + Application.dataPath + "."
            + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_graph)) + ".";
        private string CurrentLayoutKey => LayoutKey + "v4.exterior.";
        public EmberFsmGraphDescription Description => _description;
        public int VisibleNodeCount => _nodes.Count;
        public int VisibleEdgeCount => _relations.Count(r => !r.Structural);
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void OnEnable()
        {
            if (string.IsNullOrEmpty(_templateScope)) _templateScope=EmberFsmBlueprintAccess.CurrentTemplateId;
            if (string.IsNullOrEmpty(_extensionScope)) _extensionScope = EmberExtensionProject.CaptureScope(EmberExtensionProject.FsmBlueprint);
            AssemblyReloadEvents.beforeAssemblyReload += PersistLayout;
            AssemblyReloadEvents.beforeAssemblyReload += CaptureExtensionLayoutDraft;
            Undo.undoRedoPerformed += LayoutUndoRedo;
        }
        private void CreateGUI()
        {
            string retainedDraft = _extensionLayoutDraft;
            PersistLayout();
            rootVisualElement.Clear(); _nodes.Clear(); _fullNested = true;
            rootVisualElement.AddToClassList("fsm-blueprint");
            var theme = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.ember/Core/Editor/EmberFsmBlueprint.uss");
            if (theme != null && !rootVisualElement.styleSheets.Contains(theme)) rootVisualElement.styleSheets.Add(theme);
            var toolbar = new Toolbar();
            toolbar.style.flexWrap = Wrap.Wrap;
            toolbar.style.height = StyleKeyword.Auto; toolbar.style.minHeight = 30;
            _graphField = new ObjectField("FSM 图") { objectType = typeof(EmberFsmGraph), value = _graph };
            _graphField.style.width = 300; _graphField.style.minWidth = 300; _graphField.style.maxWidth = 380;
            _graphField.RegisterValueChangedCallback(e => SetGraph(e.newValue as EmberFsmGraph));
            toolbar.Add(_graphField);
            toolbar.Add(new ToolbarButton(RefreshGraph) { text = "刷新声明" });
            toolbar.Add(new ToolbarButton(FitVisible) { text = "适应画布" });
            toolbar.Add(new ToolbarButton(SaveLayout) { text = "保存本机布局" });
            AddNavigationToolbar(toolbar);
            rootVisualElement.Add(toolbar);
            AddLayoutToolbar();
            var parameterToolbar = new IMGUIContainer(() => EmberFsmBlueprintParameters.DrawToolbar(this));
            parameterToolbar.style.flexShrink = 0;
            rootVisualElement.Add(parameterToolbar);
            _breadcrumbs = new Toolbar(); rootVisualElement.Add(_breadcrumbs);

            _layoutNotice = new Label(); rootVisualElement.Add(_layoutNotice);
            _overlapNotice = new Label(); _overlapNotice.style.height = 22; _overlapNotice.style.flexShrink = 0;
            _overlapNotice.style.whiteSpace = WhiteSpace.NoWrap; _overlapNotice.style.overflow = Overflow.Hidden;
            _overlapNotice.style.textOverflow = TextOverflow.Ellipsis;
            _overlapNotice.style.color = new Color(1, .72f, .3f); rootVisualElement.Add(_overlapNotice);
            var body = new VisualElement(); body.style.flexDirection = FlexDirection.Row; body.style.flexGrow = 1;
            body.style.flexBasis = 0; body.style.minHeight = 0;
            _hierarchy = new ScrollView(); _hierarchy.AddToClassList("fsm-hierarchy"); _hierarchy.style.width = 150; _hierarchy.style.flexShrink = 0; body.Add(_hierarchy);
            _canvas = new BlueprintCanvas(OnCanvasMoved, PreviewDragGuides, ClearAlignmentGuides); _canvas.style.flexGrow = 1; body.Add(_canvas);
            var side = new ScrollView(); side.AddToClassList("fsm-inspector"); side.style.width = 380; side.style.flexShrink = 0;
            var parameters = new Foldout { text = "真实配置 / 试跑", value = true };
            parameters.Add(new IMGUIContainer(() => EmberFsmBlueprintParameters.Draw(this)));
            side.Add(parameters);
            _preview = new VisualElement(); side.Add(_preview);
            var declarations = new Foldout { text = "定义、声明与所选关系详情", value = true }; side.Add(declarations);
            _details = new Label(); _details.style.whiteSpace = WhiteSpace.Normal; declarations.Add(_details);
            side.Add(new Button(() => { if (_selected != null) SetRedirectsForNode(_selected.Id, true); }) { text = "展开所选状态的预检声明" });
            side.Add(new Button(NavigateToSelectedTarget) { text = "同画布定位所选关系目标" });
            side.Add(new Button(OpenSelectedRuleSource) { text = "打开所选转换的状态源码" });
            side.Add(new Button(() => Selection.activeObject = _selected?.Definition) { text = "定位定义 SO" });
            side.Add(new Button(() =>
            {
                if (_selected?.Definition != null) AssetDatabase.OpenAsset(MonoScript.FromScriptableObject(_selected.Definition));
            }) { text = "打开定义源码" });
            body.Add(side); rootVisualElement.Add(body);
            _errors = new Label(); _errors.style.whiteSpace = WhiteSpace.Normal; rootVisualElement.Add(_errors);
            LoadLayout(); RefreshGraph();
            _extensionLayoutDraft = retainedDraft; RestoreExtensionLayoutDraft();
            if (!RestoreViewport()) _canvas.schedule.Execute(FitVisible);
            CreateRuntimeView();
            // 长条件树由详情区滚动；不能挤掉方案选择、运行历史和折叠标题。
            foreach (var child in rootVisualElement.Children())
                if (child != body) child.style.flexShrink = 0;
            rootVisualElement.schedule.Execute(RefreshTemplateAccess).Every(500);
            RefreshTemplateAccess();
        }
        private void OnDisable()
        {
            PersistLayout();
            AssemblyReloadEvents.beforeAssemblyReload -= PersistLayout;
            AssemblyReloadEvents.beforeAssemblyReload -= CaptureExtensionLayoutDraft;
            Undo.undoRedoPerformed -= LayoutUndoRedo;
            CaptureExtensionLayoutDraft();
            string retainedDraft = _extensionLayoutDraft;
            CancelLayoutPreview(); _extensionLayoutDraft = retainedDraft; DisposeLayoutUndo();
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void RefreshTemplateAccess()
        {
            bool available=IsTemplateAvailable;
            foreach(var element in rootVisualElement.Children()) element.SetEnabled(available);
            if (available) ResumeExtensionLayoutDraft();
            if (!available)
            {
                SuspendExtensionLayoutDraft(); ObservedSnapshot=null;
                if(_runtimeHistory!=null) _runtimeHistory.Clear();
                if(_runtimeStatus!=null) _runtimeStatus.text="模板或模块已变化，运行观察已停止。";
                if(_errors!=null) _errors.text="当前模板未启用 FSM 蓝图，或窗口属于另一模板。原图与本机布局保留；请从当前模板的入口重新打开。";
            }
        }
        private sealed class BlueprintCanvas : GraphView
        {
            internal BlueprintCanvas(Action<List<GraphElement>> moved, Action preview, Action clear)
            {
                SetupZoom(.1f, 2f);
                this.AddManipulator(new ContentDragger()); this.AddManipulator(new SelectionDragger());
                this.AddManipulator(new RectangleSelector());
                var grid=new GridBackground(); grid.StretchToParentSize(); Insert(0,grid);
                RegisterCallback<MouseDownEvent>(e=>clear(),TrickleDown.TrickleDown);
                RegisterCallback<MouseMoveEvent>(e=> { if(e.pressedButtons==1) schedule.Execute(preview); });
                graphViewChanged = change =>
                {
                    change.edgesToCreate?.Clear(); change.elementsToRemove?.Clear();
                    if (change.movedElements != null) moved(change.movedElements);
                    return change;
                };
            }
            public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter adapter) => new();
            public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) { }
        }

        private static Vector2 InlinePosition(Node node) => new(node.style.left.value.value, node.style.top.value.value);
        private IEnumerable<string> Children(string id) => _parents.Where(p => p.Value == id).Select(p => p.Key);
        private int Depth(string id)
        {
            int depth = 0;
            while (_parents.TryGetValue(id, out id)) depth++;
            return depth;
        }
        private bool Descendant(string child, string ancestor)
        {
            while (_parents.TryGetValue(child, out child)) if (child == ancestor) return true;
            return false;
        }
        private string Representative(string id)
        {
            var result = id;
            while (_parents.TryGetValue(id, out id)) if (_collapsed.Contains(id)) result = id;
            return result;
        }

        private void BuildDisplayForest()
        {
            _map.Clear(); _parents.Clear();
            foreach (var node in _description.Nodes) _map[node.Id] = node;
            foreach (var node in _description.Nodes)
            {
                var visited = new HashSet<string>(); var cursor = node; bool cycle = false;
                while (cursor != null)
                {
                    if (!visited.Add(cursor.Id)) { cycle = true; break; }
                    if (string.IsNullOrEmpty(cursor.ParentId) || !_map.TryGetValue(cursor.ParentId, out cursor)) break;
                }
                // 非法链只在视图中摊开；原图、默认项、绑定及错误不作修补。
                if (!cycle && !string.IsNullOrEmpty(node.ParentId) && _map.ContainsKey(node.ParentId))
                    _parents[node.Id] = node.ParentId;
            }
        }

        private void CapturePositions()
        {
            // 所有控件直接位于同一画布层，绝不把局部坐标再次加到父坐标。
            // inline 是 SetPosition/拖拽当前值，不能读取延迟一帧的 resolved layout。
            var direct = new Dictionary<string, Vector2>();
            foreach (var pair in _nodes)
                if (_positions.TryGetValue(pair.Key, out var old)) direct[pair.Key] = InlinePosition(pair.Value) - old;
            var total = new Dictionary<string, Vector2>();
            foreach (var id in _map.Keys.OrderBy(Depth))
            {
                var delta = direct.TryGetValue(id, out var d) ? d : Vector2.zero;
                if (_fullNested && _parents.TryGetValue(id, out var parent) && total.TryGetValue(parent, out var inherited))
                {
                    // GraphView 多选同时移动父/子时，子已经收到同一偏移，不能叠加两次。
                    bool alreadyMoved = inherited != Vector2.zero && delta == inherited;
                    delta += alreadyMoved ? Vector2.zero : inherited;
                }
                total[id] = delta;
                if (_positions.ContainsKey(id)) _positions[id] += delta;
            }
        }

        private void OnCanvasMoved(List<GraphElement> moved)
        { CancelLayoutPreview(); SnapMovedNodes(moved); CapturePositions(); FitFrames(); ApplyPositions(); RenderPaths(); PersistLayout(); SyncLayoutUndo(); }

        private void PersistLayout()
        {
            if (_canvas == null || _nodes.Count == 0 || _graph == null
                || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_graph))) return;
            SaveLayout();
        }

        private void LoadLayout()
        {
            _positions.Clear(); _collapsed.Clear(); _expandedRedirects.Clear(); _pinnedLayout.Clear(); _legacyLayout = false;
            if (_graph == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_graph))) return;
            bool current = EditorPrefs.GetBool(CurrentLayoutKey + "saved");
            var prefix = current ? CurrentLayoutKey : EditorPrefs.GetBool(LayoutKey + "v3.nested.saved")
                ? LayoutKey + "v3.nested." : EditorPrefs.GetBool(LayoutKey + "v2.canvas.saved") ? LayoutKey + "v2.canvas." : LayoutKey;
            _migrateSpacing = !current;
            _showRedirects = current && EditorPrefs.GetBool(CurrentLayoutKey + "redirects");
            foreach (var node in _graph.Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.Id)) continue;
                var key = prefix + node.Id;
                if (EditorPrefs.HasKey(key + ".x"))
                {
                    var value = new Vector2(EditorPrefs.GetFloat(key + ".x"), EditorPrefs.GetFloat(key + ".y"));
                    if (float.IsFinite(value.x) && float.IsFinite(value.y)) _positions[node.Id] = value;
                    if (!current && _fullNested) _legacyLayout = true;
                }
                if (EditorPrefs.GetBool(key + ".fold")) _collapsed.Add(node.Id);
                if (current && EditorPrefs.GetBool(key + ".redirect")) _expandedRedirects.Add(node.Id);
                if (current && EditorPrefs.GetBool(key + ".pinned")) _pinnedLayout.Add(node.Id);
            }
        }

        private float HeaderHeight(string id)
        {
            if (!_nodes.TryGetValue(id, out var node)) return 150;
            return _headerHeights.TryGetValue(id, out var height) ? height : 160;
        }

        private Vector2 ArrangeMissing(string id, Vector2 origin)
        {
            var children = Children(id).ToArray();
            var header = HeaderHeight(id);
            if (!_positions.ContainsKey(id)) _positions[id] = origin;
            float width = 320, height = header;
            var childOrigin = _positions[id] + new Vector2(40, header + 30);
            foreach (var child in children)
            {
                var size = ArrangeMissing(child, childOrigin);
                childOrigin.x += size.x + 40;
                width = Math.Max(width, childOrigin.x - _positions[id].x);
                height = Math.Max(height, header + size.y + 70);
            }
            _sizes[id] = new Vector2(width, height);
            return _sizes[id];
        }

        private void FitFrames()
        {
            if (!_fullNested) { foreach (var id in _nodes.Keys) _sizes[id] = new Vector2(320, HeaderHeight(id)); return; }
            foreach (var id in _map.Keys.OrderByDescending(Depth))
            {
                _sizes[id] = new Vector2(320, ExteriorHeader(id));
                var children = Children(id).Where(c => _positions.ContainsKey(c)).ToArray();
                if (children.Length == 0 || _collapsed.Contains(id)) continue;
                float x = children.Min(c => Envelope(c).xMin) - 30;
                float y = children.Min(c => Envelope(c).yMin) - ExteriorHeader(id) - 30;
                float right = children.Max(c => Envelope(c).xMax) + 30;
                float bottom = children.Max(c => Envelope(c).yMax) + 30;
                if (LayoutLocked(id)) { x = _positions[id].x; y = _positions[id].y; }
                else _positions[id] = new Vector2(x, y);
                _sizes[id] = new Vector2(Math.Max(320, right - x), Math.Max(ExteriorHeader(id), bottom - y));
            }
        }

        private void ApplyPositions()
        {
            foreach (var pair in _nodes)
            {
                // Node 不开放 Resizable 能力；不能依赖 SetPosition 为不可缩放节点应用尺寸。
                // 显式设置尺寸既供布局使用，也避免同帧边界检查读取 Auto/零宽高。
                pair.Value.style.width = _sizes[pair.Key].x;
                pair.Value.style.height = _sizes[pair.Key].y;
                pair.Value.SetPosition(new Rect(_positions[pair.Key], _sizes[pair.Key]));
                pair.Value.mainContainer.style.width = _sizes[pair.Key].x;
                pair.Value.mainContainer.style.height = _sizes[pair.Key].y;
                pair.Value.mainContainer.style.borderBottomLeftRadius = pair.Value.mainContainer.style.borderBottomRightRadius = 6;
                pair.Value.mainContainer.style.borderTopLeftRadius = pair.Value.mainContainer.style.borderTopRightRadius = 6;
            }
        }

        private void SelectNode(EmberFsmNodeDescription node)
        {
            _selectedRelation = null; HighlightPaths();
            _selected = node;
            ShowExecutionPreview(node);
            _details.text = node.DisplayName + "\nID: " + node.Id + "\n父: " + node.ParentId
                + "\n默认子入口（结构）: " + node.DefaultChildId + "\n定义: " + (node.Definition != null ? node.Definition.name : "缺失")
                + "\n\n" + string.Join("\n", node.Transitions.Where(t => t != null).Select(t =>
                    t.Id + " → " + t.TargetSlot + "\n" + t.Description + "\n模式: " + t.Mode + EmberFsmBlueprintRules.Describe(t)))
                + "\n\n静态重定向槽: " + string.Join(", ", node.RedirectSlots)
                + "\n\n目标绑定:\n" + string.Join("\n", node.Targets.Select(p => p.Key + " → " + p.Value));
        }


        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void OpenSelected() => Open(Selection.activeObject as EmberFsmGraph);
        public static EmberFsmBlueprintWindow Open(EmberFsmGraph graph)
        {
            if(!EmberFsmBlueprintAccess.IsAvailable) throw new InvalidOperationException("当前模板未启用 FSM 蓝图工具。");
            var window = GetWindow<EmberFsmBlueprintWindow>("FSM 蓝图");
            if(!window.IsTemplateAvailable)
            { window=CreateInstance<EmberFsmBlueprintWindow>(); window.titleContent=new GUIContent("FSM 蓝图"); window.Show(); }
            window._templateScope=EmberFsmBlueprintAccess.CurrentTemplateId;
            window.RefreshTemplateAccess();
            window.minSize = new Vector2(900, 500); window.SetGraph(graph); return window;
        }
        public bool IsTemplateAvailable => EmberFsmBlueprintAccess.IsAvailable
            && _templateScope==EmberFsmBlueprintAccess.CurrentTemplateId
            && EmberExtensionProject.AllowsScope(EmberExtensionProject.FsmBlueprint, _extensionScope);
        public void SetGraph(EmberFsmGraph graph)
        {
            if(!IsTemplateAvailable) return;
            bool changed = _graph != graph;
            if (changed) { CancelLayoutPreview(); DisposeLayoutUndo(); }
            if (_graph != graph) { PersistLayout(); _nodes.Clear(); _map.Clear();  _graph = graph;
                _fullNested = true;
                LoadLayout(); }
            _graphField?.SetValueWithoutNotify(graph); RefreshGraph();
            if (changed && _canvas != null && !RestoreViewport()) FitVisible();
        }
        public void SaveLayout()
        {
            if(!IsTemplateAvailable) return;
            if (_graph == null) return;
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_graph)))
            { _errors.text = "请先保存图资产，再按稳定 GUID 保存布局。"; return; }
            CapturePositions(); FitFrames(); ApplyPositions(); RenderPaths();
            foreach (var node in _graph.Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.Id)) continue;
                var key = CurrentLayoutKey + node.Id;
                if (_positions.TryGetValue(node.Id, out var pos))
                { EditorPrefs.SetFloat(key + ".x", pos.x); EditorPrefs.SetFloat(key + ".y", pos.y); }
                EditorPrefs.SetBool(key + ".fold", _collapsed.Contains(node.Id));
                EditorPrefs.SetBool(key + ".redirect", _expandedRedirects.Contains(node.Id));
                EditorPrefs.SetBool(key + ".pinned", _pinnedLayout.Contains(node.Id));
            }
            EditorPrefs.SetBool(CurrentLayoutKey + "saved", true);
            EditorPrefs.SetBool(CurrentLayoutKey + "redirects", _showRedirects);
            SaveViewport(); _legacyLayout = false;
            _layoutNotice.text = "已保存 v4 局部引用布局；旧版/v2/v3 键全部保留，图资产未修改。";
        }
        public void SetCollapsed(string nodeId, bool collapsed)
        {
            if(!IsTemplateAvailable) return;
            CapturePositions(); ApplyPositions();
            if (collapsed) _collapsed.Add(nodeId); else _collapsed.Remove(nodeId);
            RefreshGraph();
            PersistLayout();
        }
        public void RefreshGraph()
        {
            if(!IsTemplateAvailable) return;
            if (_canvas == null) return;
            CancelLayoutPreview();
            CapturePositions();
            foreach (var element in _canvas.graphElements.ToList()) _canvas.RemoveElement(element);
            _nodes.Clear(); _hierarchy.Clear(); _selected = null; ClearRelations();
            _details.text = "选择状态查看执行预览，选择来源/目标引用查看真实声明。"; _preview?.Clear();
            _description = _graph != null ? _graph.Inspect() : null;
            _errors.text = _description == null ? "请选择 FSM 图资产。" : _description.IsValid
                ? "结构校验通过；观察不会评估 Guard。" : string.Join("\n", _description.Errors);
            _layoutNotice.text = _legacyLayout
                ? "外围引用布局从旧嵌套生成新副本并重排留白；保存为 v4.exterior，旧版/v2/v3 原样保留。"
                : "单画布完整嵌套；引用同画布定位，选择仅表示静态关系。";
            if (_description == null) { _map.Clear(); _parents.Clear(); return; }
            BuildDisplayForest();
            BuildBreadcrumbs();
            _errors.text += "\n代码转换声明 " + _description.Nodes.Sum(n => n.Transitions.Count(t => t != null))
                + "；静态重定向声明 " + _description.Nodes.Sum(n => n.RedirectSlots.Count)
                + "；默认子入口 " + _description.Nodes.Count(n => !string.IsNullOrEmpty(n.DefaultChildId))
                + "（结构标记，不计入转换）。";
            BuildLocalRelations();
            foreach (var info in _description.Nodes.OrderBy(n => Depth(n.Id)))
            {
                // 层级列表保留隐藏节点，代理身份也可从这里直接定位。
                _hierarchy.Add(new Button(() => NavigateToState(info.Id)) { text = new string(' ', Depth(info.Id) * 3) + info.DisplayName });
                if (Representative(info.Id) != info.Id) continue;
                bool composite = _description.Nodes.Any(n => n.ParentId == info.Id);
                var node = new Node { title = info.DisplayName + (composite ? " · 复合状态" : "")
                    + (info.Id == _description.EntryId ? " ◆ 图入口" : ""), viewDataKey = info.Id,
                    capabilities = Capabilities.Movable | Capabilities.Selectable, layer = composite ? -100 + Depth(info.Id) : 0 };
                node.AddToClassList("fsm-state");
                node.mainContainer.style.backgroundColor = new Color(.15f,.18f,.22f);
                node.mainContainer.style.borderLeftWidth=node.mainContainer.style.borderRightWidth=node.mainContainer.style.borderTopWidth=node.mainContainer.style.borderBottomWidth=1;
                node.mainContainer.style.borderLeftColor=node.mainContainer.style.borderRightColor=node.mainContainer.style.borderTopColor=node.mainContainer.style.borderBottomColor=new Color(.3f,.36f,.43f);
                node.titleContainer.style.height = 34;
                node.titleContainer.style.borderBottomWidth = 2;
                node.titleContainer.style.borderBottomColor = new Color(.3f,.49f,.65f);
                node.RegisterCallback<MouseDownEvent>(e => { SelectNode(info);  });

                if (composite)
                {

                    node.titleButtonContainer.Add(new Button(() => SetCollapsed(info.Id, !_collapsed.Contains(info.Id)))
                    { text = (_collapsed.Contains(info.Id) ? "▸" : "▾") + " " + _map.Keys.Count(k => Descendant(k, info.Id)) });
                    node.mainContainer.style.flexGrow = 1;
                    node.mainContainer.style.backgroundColor = new Color(.105f, .115f, .13f, .92f);
                    node.style.borderBottomWidth = node.style.borderTopWidth = node.style.borderLeftWidth = node.style.borderRightWidth = 1;
                    node.style.borderBottomColor = node.style.borderTopColor = node.style.borderLeftColor = node.style.borderRightColor = new Color(.25f,.29f,.34f);
                }
                _nodes.Add(info.Id, node); _canvas.AddElement(node);
            }
            foreach (var pair in _nodes) BuildLocalControls(_map[pair.Key], pair.Value);
            var missing = new HashSet<string>(_map.Keys.Where(id => !_positions.ContainsKey(id)));
            float rootX = 0;
            foreach (var id in _map.Keys.Where(id => !_parents.ContainsKey(id)))
                rootX += ArrangeMissing(id, new Vector2(rootX, 0)).x + 100;
            if (_migrateSpacing) { EnsureMigrationSpacing(); _migrateSpacing = false; }
            else PlaceNewNodes(missing);
            FitFrames(); ApplyPositions();
            foreach (var node in _nodes.Values) { node.RefreshExpandedState(); node.RefreshPorts(); }
            RenderPaths(); UpdateFilterNotice(); UpdatePinStyles(); SyncLayoutUndo();
        }
        #endregion
    }
}

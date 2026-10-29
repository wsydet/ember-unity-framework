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
    public sealed partial class EmberFsmBlueprintWindow
    {
        #region 编辑器面板参数
        [SerializeField] private bool _fullNested = true;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private Toolbar _breadcrumbs;
        private Toggle _redirectToggle;
        private bool _showRedirects, _migrateSpacing;
        private readonly HashSet<string> _expandedRedirects = new();
        private readonly Dictionary<string, float> _headerHeights = new();
        private readonly List<Relation> _relations = new();
        private readonly List<VisualElement> _referenceControls = new();
        private string _selectedRelation;

        // 保留 API 查询语义：关系数量按声明去重，绝不按引用数量计数。
        public bool IsFullNested => true;
        public string FocusId => null;
        public int VisibleDeclarationCount => _relations.Count(r => !r.Structural && (!r.Redirect || RedirectVisible(r.Source)));
        public int VisibleRedirectCount => _relations.Count(r => r.Redirect && RedirectVisible(r.Source));
        public int StructuralEntryCount => _relations.Count(r => r.Structural);
        public int SelectedPathSegmentCount => _referenceControls.Count(e => (string)e.userData == _selectedRelation);
        public string SelectedRuleNodeId => _relations.FirstOrDefault(r => r.Key == _selectedRelation && !r.Structural && !r.Redirect)?.Source;
        public string SelectedRuleId => _relations.FirstOrDefault(r => r.Key == _selectedRelation && !r.Structural && !r.Redirect)?.Id;
        public string[] DeclarationKeys => _relations.Where(r => !r.Structural && !r.Redirect).Select(r => r.Key).ToArray();
        public string[] IncomingKeys(string id) => _relations.Where(r => !r.Structural && !r.Redirect && r.Target == id).Select(r => r.Key).ToArray();
        private sealed class Relation
        {
            internal string Key, Source, Target, Id, Slot, Description, Mode;
            internal bool Redirect, Structural;
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private string Display(string id) => id != null && _map.TryGetValue(id, out var n)
            ? (string.IsNullOrWhiteSpace(n.DisplayName) ? n.Id : n.DisplayName) : id ?? "缺失";
        private string StatePath(string id)
        {
            var items = new List<string> { Display(id) };
            while (id != null && _parents.TryGetValue(id, out id)) items.Insert(0, Display(id));
            return string.Join(" / ", items);
        }
        private static string RelationKey(string source, bool redirect, string id)
            => source.Length + ":" + source + (redirect ? ":R:" : ":T:") + id;
        private bool RedirectVisible(string id) => _showRedirects || _expandedRedirects.Contains(id);
        private void AddNavigationToolbar(Toolbar toolbar)
        {
            var snap = new ToolbarToggle { text="网格吸附", value=_snapEnabled };
            snap.RegisterValueChangedCallback(e=>_snapEnabled=e.newValue); toolbar.Add(snap);
            toolbar.Add(new ToolbarButton(()=>{var p=_canvas.contentViewContainer.resolvedStyle.translate; _canvas.UpdateViewTransform(p,Vector3.one);}){text="100%"});
            _redirectToggle = new Toggle("全部预检声明") { value = _showRedirects };
            _redirectToggle.RegisterValueChangedCallback(e => SetShowRedirects(e.newValue)); toolbar.Add(_redirectToggle);
        }
        private void BuildBreadcrumbs()
        { _breadcrumbs.Clear(); _breadcrumbs.Add(new Label("完整嵌套     /     静态声明     ·     点击引用定位，滚轮缩放")); }
        private void SaveViewport()
        {
            var p = _canvas.contentViewContainer.resolvedStyle.translate; var s = _canvas.contentViewContainer.resolvedStyle.scale.value;
            EditorPrefs.SetFloat(CurrentLayoutKey + "panX", p.x); EditorPrefs.SetFloat(CurrentLayoutKey + "panY", p.y);
            EditorPrefs.SetFloat(CurrentLayoutKey + "zoom", s.x);
        }
        private bool RestoreViewport()
        {
            if (!EditorPrefs.HasKey(CurrentLayoutKey + "zoom")) return false;
            float zoom = EditorPrefs.GetFloat(CurrentLayoutKey + "zoom", 1);
            if (!float.IsFinite(zoom) || zoom <= 0) return false;
            _canvas.UpdateViewTransform(new Vector3(EditorPrefs.GetFloat(CurrentLayoutKey + "panX"),
                EditorPrefs.GetFloat(CurrentLayoutKey + "panY"), 0), new Vector3(zoom, zoom, 1)); return true;
        }
        private void ClearRelations()
        { _alignmentGuides.Clear(); _peripherals.Clear(); _relations.Clear(); _headerHeights.Clear(); _referenceControls.Clear(); _selectedRelation = null; }
        private void BuildLocalRelations()
        {
            foreach (var n in _description.Nodes)
            {
                foreach (var t in n.Transitions.Where(t => t != null))
                {
                    n.Targets.TryGetValue(t.TargetSlot ?? "", out var target);
                    _relations.Add(new Relation { Key = RelationKey(n.Id, false, t.Id), Source = n.Id, Target = target,
                        Id = t.Id, Slot = t.TargetSlot, Description = t.Description, Mode = t.Mode.ToString() });
                }
                foreach (var slot in n.RedirectSlots)
                {
                    n.Targets.TryGetValue(slot ?? "", out var target);
                    _relations.Add(new Relation { Key = RelationKey(n.Id, true, slot), Source = n.Id, Target = target,
                        Id = slot, Slot = slot, Redirect = true, Description = "预检重定向 / 可能路径" });
                }
                if (!string.IsNullOrEmpty(n.DefaultChildId)) _relations.Add(new Relation { Key = "S:" + n.Id,
                    Source = n.Id, Target = n.DefaultChildId, Id = "default", Structural = true, Description = "默认子入口（结构）" });
            }
            if (_map.ContainsKey(_description.EntryId ?? "")) _relations.Add(new Relation { Key = "S:entry",
                Source = _description.EntryId, Target = _description.EntryId, Id = "entry", Structural = true, Description = "图入口（结构）" });
        }
        private Label LocalLabel(string text)
        {
            // 结构摘要采用固定单行尺寸；全文通过 tooltip 保留，长标签不会溢出布局测量边界。
            var label = new Label(text) { tooltip = text }; label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.overflow = Overflow.Hidden; label.style.textOverflow = TextOverflow.Ellipsis;
            label.style.minHeight = 22; label.style.unityFontStyleAndWeight = FontStyle.Bold; return label;
        }
        private void BuildLocalControls(EmberFsmNodeDescription info, Node node)
        {
            node.inputContainer.style.display = DisplayStyle.None;
            node.outputContainer.style.display = DisplayStyle.None;
            var exits=Exits(info.Id);
            var inlet=new Label("进入") { name="entry-label", tooltip="直接绑定到本状态的已声明来源；不代表条件已满足" };
            inlet.AddToClassList("fsm-inlet-label");
            inlet.style.position=Position.Absolute; inlet.style.left=14; inlet.style.top=40;
            inlet.style.height=22; node.Add(inlet);
            for(int i=0;i<exits.Length;i++)
            {
                var relation=exits[i];
                var row=new Label(string.IsNullOrWhiteSpace(relation.Description)?relation.Id:relation.Description)
                    { name="declaration-port-row",userData=relation.Key,tooltip=relation.Id+" → "+StatePath(relation.Target)+"\n"+relation.Description };
                row.AddToClassList("fsm-port-row");
                if(relation.Redirect)row.text="◇ 预检 · "+row.text;
                row.style.position=Position.Absolute;row.style.right=16;row.style.top=38+i*ROW;
                row.style.width=210;row.style.height=48;
                row.RegisterCallback<MouseDownEvent>(e=>{SelectRelation(relation);e.StopPropagation();});
                node.Add(row);
            }
            var panel = new VisualElement(); panel.AddToClassList("fsm-structure");
            panel.style.position=Position.Absolute;panel.style.left=12;panel.style.right=12;
            panel.style.top=48+ROW*Math.Max(1,exits.Length);
            foreach (var s in _relations.Where(r => r.Structural && r.Target == info.Id))
                panel.Add(LocalLabel(s.Id == "entry" ? "◆ 图入口" : "◇ 默认：" + Display(s.Source)));
            if (!string.IsNullOrEmpty(info.DefaultChildId)) panel.Add(new Button(() => NavigateToState(info.DefaultChildId))
                { text = "◇ 默认 → " + Display(info.DefaultChildId) });
            if (info.RedirectSlots.Count > 0) panel.Add(new Button(() => SetRedirectsForNode(info.Id,!RedirectVisible(info.Id)))
                { text = "预检 " + info.RedirectSlots.Count + (RedirectVisible(info.Id) ? " · 展开" : " · 过滤") });
            if (_collapsed.Contains(info.Id))
            {
                var hidden=_relations.Where(r=>!r.Structural && Descendant(r.Source,info.Id)).ToArray();
                var fold=new Foldout { text="后代引用 · " + hidden.Length, value=false };
                foreach(var r in hidden) fold.Add(new Button(()=>{ NavigateToState(r.Source); SelectRelation(r); })
                    { text=Display(r.Source)+" · "+r.Id+" → "+Display(r.Target) });
                var scroll=new ScrollView(); scroll.style.maxHeight=90; scroll.Add(fold); panel.Add(scroll);
            }
            node.Add(panel);
            _headerHeights[info.Id]=48+ROW*Math.Max(1,exits.Length)+panel.childCount*26+16
                + (_collapsed.Contains(info.Id)?100:0);
            var anchor=new VisualElement { name="local-entry-anchor" };
            anchor.style.position=Position.Absolute; anchor.style.left=-5; anchor.style.top=45;
            anchor.style.width=10; anchor.style.height=10; anchor.style.borderTopLeftRadius=5;
            anchor.style.borderTopRightRadius=5; anchor.style.borderBottomLeftRadius=5; anchor.style.borderBottomRightRadius=5;
            anchor.style.backgroundColor=new Color(.3f,.7f,1); node.Add(anchor);
            anchor.RegisterCallback<GeometryChangedEvent>(_=>RenderPaths());
        }
        private void EnsureMigrationSpacing()
        {
            // 外围引用需要不同留白。首次迁移保留旧键并重排新副本；后续刷新不重排。
            var roots=_map.Keys.Where(k=>!_parents.ContainsKey(k)).OrderBy(k=>_positions[k].x).ToArray();
            float x=roots.Length==0?0:_positions[roots[0]].x;
            foreach(var id in roots) { var size=PackExterior(id,new Vector2(x,0)); x+=size.x+60; }
        }
        private Vector2 PackExterior(string id,Vector2 origin)
        {
            _positions[id]=origin;
            var children=Children(id).ToArray();
            // 叶兄弟竖排，复合兄弟横排，降低完整嵌套的总宽度。
            bool vertical=children.All(c=>!Children(c).Any());
            var cursor=origin+new Vector2(EXTENT+30,ExteriorHeader(id)+30);
            foreach(var child in children)
            {
                var size=PackExterior(child,cursor);
                if(vertical) cursor.y+=size.y+30; else cursor.x+=size.x+50;
            }
            FitFrames();
            return Envelope(id).size;
        }
        private void UpdateFilterNotice()
        {
            _redirectToggle.SetValueWithoutNotify(_showRedirects);
            _layoutNotice.text += " | 真实状态 " + _map.Count + "；普通声明 " + DeclarationKeys.Length
                + "；预检 " + VisibleRedirectCount + " 展开（其余过滤）。引用不计为状态/转换。";
        }
        private void RenderPaths() { RebuildExterior(); HighlightPaths(); UpdateOverlapNotice(); }
        private void HighlightPaths()
        {
            var relation = _relations.FirstOrDefault(r => r.Key == _selectedRelation);
            foreach (var row in _referenceControls) row.style.backgroundColor = (string)row.userData == _selectedRelation
                ? new Color(.42f,.34f,.12f) : new Color(.16f,.18f,.21f);
            foreach(var path in _peripherals.OfType<EmberFsmBlueprintPath>()) path.SetSelected((string)path.userData==_selectedRelation);
            foreach (var pair in _nodes) pair.Value.titleContainer.style.backgroundColor = relation != null
                && (pair.Key == relation.Source || pair.Key == relation.Target) ? new Color(.42f,.34f,.12f) : new Color(.19f,.23f,.28f);
        }
        private void SelectRelation(Relation r)
        {
            _selectedRelation = r.Key; HighlightPaths();
            if (_preview != null) _preview.style.display = DisplayStyle.None;
            _details.text = "静态声明选择（不是实际执行）\n" + (r.Structural ? "结构入口" : r.Redirect ? "预检重定向 / 可能路径" : "普通转换")
                + "\n来源: " + StatePath(r.Source) + " [" + r.Source + "]\n目标: " + StatePath(r.Target) + " [" + r.Target + "]"
                + "\n声明: " + r.Id + "\n目标槽: " + r.Slot + "\n模式: " + r.Mode + "\n" + r.Description;
            if (!r.Redirect && !r.Structural && _map.TryGetValue(r.Source, out var source))
                _details.text += EmberFsmBlueprintRules.Describe(source.Transitions.FirstOrDefault(t => t.Id == r.Id));
        }
        private void NavigateToSelectedTarget()
        { var r = _relations.FirstOrDefault(r => r.Key == _selectedRelation); if (r != null) NavigateToState(r.Target); }
        private void OpenSelectedRuleSource()
        {
            var relation = _relations.FirstOrDefault(r => r.Key == _selectedRelation);
            if (relation == null || relation.Structural || relation.Redirect || !_map.TryGetValue(relation.Source, out var source)) return;
            var type = source.Transitions.FirstOrDefault(t => t.Id == relation.Id)?.StateType;
            if (type == null) return;
            foreach (var guid in AssetDatabase.FindAssets(type.Name + " t:MonoScript"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type) { AssetDatabase.OpenAsset(script); return; }
            }
        }
        private static Rect NodeRect(Node node) => new(InlinePosition(node), new Vector2(node.style.width.value.value, node.style.height.value.value));
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void FitVisible()
        {
            if(!IsTemplateAvailable) return;
            if (_canvas == null || _nodes.Count == 0) return;
            var bounds = _nodes.Keys.Select(Envelope).ToArray();
            float left = bounds.Min(r => r.xMin) - 35, top = bounds.Min(r => r.yMin) - 35;
            float right = bounds.Max(r => r.xMax) + 35, bottom = bounds.Max(r => r.yMax) + 35;
            float width = _canvas.contentRect.width, height = _canvas.contentRect.height;
            if (!float.IsFinite(width) || width <= 0) width = Math.Max(300, position.width - 530);
            if (!float.IsFinite(height) || height <= 0) height = Math.Max(200, position.height - 170);
            float zoom = Mathf.Clamp(Math.Min(width / Math.Max(1, right-left), height / Math.Max(1, bottom-top)), .1f, 1f);
            _canvas.UpdateViewTransform(new Vector3(width*.5f-(left+right)*.5f*zoom, height*.5f-(top+bottom)*.5f*zoom, 0), new Vector3(zoom,zoom,1));
        }
        public void Navigate(string id) { if (id == null) FitVisible(); else NavigateToState(id); }
        public void SetFullNested(bool enabled) { } // 旧调用保留；新窗口始终单画布，不恢复旧分页。
        public void NavigateToState(string id)
        {
            if(!IsTemplateAvailable) return;
            if (id == null || !_map.ContainsKey(id)) return;
            CapturePositions(); ApplyPositions();
            var parent = id; while (_parents.TryGetValue(parent, out parent)) _collapsed.Remove(parent);
            RefreshGraph(); SelectNode(_map[id]);
            var rect = NodeRect(_nodes[id]); var size = _canvas.contentRect.size;
            // 定位真实状态的局部公共区；不会切换页面或运行状态。
            _canvas.UpdateViewTransform(new Vector3(size.x*.5f-(rect.x+100), 30-rect.y, 0), Vector3.one);
        }
        public void RearrangeLocalLayout()
        { PreviewLayout("all"); }
        public void SetShowRedirects(bool show) { if(!IsTemplateAvailable)return;_showRedirects=show; RefreshGraph(); }
        public void SetRedirectsForNode(string id, bool show)
        { if(!IsTemplateAvailable)return;if(show) _expandedRedirects.Add(id); else _expandedRedirects.Remove(id); RefreshGraph(); }
        public void SelectDeclaration(string source, bool redirect, string id)
        { if(!IsTemplateAvailable)return;var r=_relations.FirstOrDefault(r=>r.Key==RelationKey(source,redirect,id)); if(r!=null) SelectRelation(r); }
        #endregion
    }
}

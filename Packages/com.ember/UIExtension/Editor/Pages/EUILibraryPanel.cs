// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>公共库浏览器；只展示配置，修改仍通过真实资产 Inspector 和公开服务。</summary>
    internal sealed class EUILibraryPanel : IDisposable
    {
        #region 内部参数
        private EUILibrary _library;
        private OdinMenuTree _navigation;
        private PropertyTree _details;
        private EUILibraryEntry _selected;
        private Vector2 _detailScroll;
        private bool _refresh = true, _catalogCurrent;
        private string _selectedId;
        private int _entryCount;
        private string _skin;
        #endregion

        #region 内部方法
        private sealed class EntryDetails
        {
            [ShowInInspector, ReadOnly, LabelText("稳定 ID"), PropertyOrder(0)]
            public string Id { get; }
            [ShowInInspector, ReadOnly, LabelText("Family"), PropertyOrder(1)]
            public string Family { get; }
            [ShowInInspector, ReadOnly, LabelText("Prefab"), AssetsOnly, PropertyOrder(2)]
            public GameObject Prefab { get; }
            [ShowInInspector, ReadOnly, LabelText("资源路径"), PropertyOrder(3)]
            public string Path { get; }
            [ShowInInspector, ReadOnly, LabelText("资源类型"), PropertyOrder(4)]
            public string Kind { get; }
            [ShowInInspector, ReadOnly, LabelText("参考尺寸"), PropertyOrder(5)]
            public Vector2 Size { get; }
            [ShowInInspector, ReadOnly, FoldoutGroup("匹配与覆盖", false, 10), LabelText("允许自动匹配")]
            public bool AutoMatch { get; }
            [ShowInInspector, ReadOnly, FoldoutGroup("匹配与覆盖", 10), LabelText("别名")]
            public string Aliases { get; }
            [ShowInInspector, ReadOnly, FoldoutGroup("匹配与覆盖", 10), LabelText("必要角色")]
            public string Roles { get; }
            [ShowInInspector, ReadOnly, FoldoutGroup("匹配与覆盖", 10), LabelText("根覆盖范围")]
            public string Overrides { get; }
            [ShowInInspector, FoldoutGroup("公开槽位", false, 20), ListDrawerSettings(IsReadOnly=true, ShowPaging=true, NumberOfItemsPerPage=8, ListElementLabelName="Id"), LabelText("槽位")]
            public SlotDetails[] Slots { get; set; }

            public EntryDetails(EUILibraryEntry entry)
            {
                Id=entry.Id; Family=entry.Family; Prefab=entry.Prefab; Size=entry.ReferenceSize;
                Path=AssetDatabase.GetAssetPath(Prefab);
                Kind=Prefab ? PrefabUtility.GetPrefabAssetType(Prefab).ToString() : "缺失";
                AutoMatch=entry.AutoMatch; Aliases=string.Join(", ",entry.Aliases); Roles=string.Join(", ",entry.RequiredRoles); Overrides=string.Join(", ",entry.AllowedOverrides);
                Slots=entry.Slots.Where(s=>s!=null).Select(s=>new SlotDetails(s)).ToArray();
            }
        }

        [HideReferenceObjectPicker]
        private sealed class SlotDetails
        {
            [ShowInInspector, ReadOnly, LabelText("ID")] public string Id { get; }
            [ShowInInspector, ReadOnly, LabelText("角色")] public string Role { get; }
            [ShowInInspector, ReadOnly, LabelText("目标")] public Component Target { get; }
            [ShowInInspector, ReadOnly, LabelText("类型")] public string Type { get; }
            [ShowInInspector, ReadOnly, LabelText("状态")] public string State { get; }
            [ShowInInspector, ReadOnly, LabelText("必需")] public bool Required { get; }
            [ShowInInspector, ReadOnly, LabelText("允许属性")] public string Properties { get; }
            public SlotDetails(EUILibrarySlot slot)
            { Id=slot.Id; Role=slot.Role; Target=slot.Target; Type=slot.ComponentType; State=slot.State; Required=slot.Required; Properties=string.Join(", ",slot.Properties); }
            public override string ToString() => Id + " · " + Role;
        }

        private void PrepareLayout()
        {
            if (Event.current.type != EventType.Layout) return;
            var library=EUILibraryService.LoadDefault();
            if (library!=_library) _refresh=true;
            if (!_refresh) return;
            _refresh=false; _library=library; _navigation=null; _selected=null;
            _details?.Dispose(); _details=null;
            if (!_library) return;
            _entryCount=_library.Entries.Count;
            _skin=_library.SkinId+" / "+_library.SkinVersion;
            _catalogCurrent=EUILibraryService.IsCatalogCurrent(_library);
            _navigation=new OdinMenuTree(false);
            _navigation.Config.DrawSearchToolbar=true;
            _navigation.Config.SearchFunction=item=>
            {
                var term=_navigation.Config.SearchTerm?.Trim();
                if(string.IsNullOrEmpty(term)) return true;
                if(item.Value is not EUILibraryEntry entry) return false;
                return new[] {entry.Id,entry.Family,entry.Prefab?entry.Prefab.name:null}
                    .Concat(entry.Aliases??Array.Empty<string>())
                    .Any(value=>value!=null && value.IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0);
            };
            foreach(var entry in _library.Entries.Where(e=>e!=null).OrderBy(e=>e.Category).ThenBy(e=>e.Id))
                _navigation.Add((string.IsNullOrWhiteSpace(entry.Category)?"未分类":entry.Category)+"/"+entry.Id+" · "+entry.Family,entry);
            var items=_navigation.EnumerateTree().Where(i=>i.Value is EUILibraryEntry).ToArray();
            var selected=items.FirstOrDefault(i=>((EUILibraryEntry)i.Value).Id==_selectedId)??items.FirstOrDefault();
            selected?.Select();
        }

        private void DrawDetails(ref string message)
        {
            // 选择变化只在 Layout 应用，保证 Odin 同一轮 Layout/Repaint 使用相同属性树。
            if(Event.current.type==EventType.Layout)
            {
                var next=_navigation.Selection.FirstOrDefault()?.Value as EUILibraryEntry;
                if(next!=_selected)
                {
                    _details?.Dispose(); _selected=next; _selectedId=next?.Id; _detailScroll=Vector2.zero;
                    _details=next==null?null:PropertyTree.Create(new EntryDetails(next));
                }
            }
            if(_selected==null) { SirenixEditorGUI.InfoMessageBox("在左侧搜索并选择一个组件。"); return; }
            using(new EditorGUILayout.HorizontalScope())
            {
                using(new EditorGUI.DisabledScope(!_selected.Prefab))
                {
                    if(GUILayout.Button("打开 Prefab",GUILayout.Height(26))) AssetDatabase.OpenAsset(_selected.Prefab);
                    if(GUILayout.Button("定位资源",GUILayout.Height(26))) EditorGUIUtility.PingObject(_selected.Prefab);
                    if(GUILayout.Button("查询引用方",GUILayout.Height(26)))
                    {
                        var consumers=EUILibraryService.FindConsumers(_selected,CSharpLogicImplementationData.FindDefault().UIResourceRoot);
                        message=consumers.Length==0?"没有找到引用方。":string.Join("\n",consumers);
                    }
                }
            }
            _detailScroll=EditorGUILayout.BeginScrollView(_detailScroll);
            var preview=_selected.Prefab?AssetPreview.GetAssetPreview(_selected.Prefab):null;
            if(preview)
            {
                var rect=GUILayoutUtility.GetRect(100,140,GUILayout.ExpandWidth(true));
                GUI.DrawTexture(rect,preview,ScaleMode.ScaleToFit);
            }
            _details?.Draw(false);
            EditorGUILayout.EndScrollView();
        }
        #endregion

        #region 外部方法
        internal void RequestRefresh() => _refresh=true;
        internal void Draw(ref string message)
        {
            PrepareLayout();
            if(!_library) { SirenixEditorGUI.InfoMessageBox("项目未部署公共库配置。"); return; }
            EditorGUILayout.LabelField(_skin+" · "+_entryCount+" 项",EditorStyles.miniLabel);
            using(new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField(_library,typeof(EUILibrary),false);
                if(GUILayout.Button("刷新",GUILayout.Width(60))) RequestRefresh();
                if(GUILayout.Button("验证配置 / 槽位",GUILayout.Width(135)))
                {
                    var issues=EUILibraryService.Validate(_library);
                    message=issues.Count==0?"验证通过：没有失效身份或槽位。":string.Join("\n",issues); RequestRefresh();
                }
                if(GUILayout.Button("生成语义目录",GUILayout.Width(110)))
                {
                    try { var catalog=EUILibraryService.GenerateCatalog(_library);message="已生成 "+catalog.entries.Count+" 条目"; }
                    catch(Exception ex) { message=ex.Message; }
                    RequestRefresh();
                }
            }
            if(_catalogCurrent) SirenixEditorGUI.InfoMessageBox("目录与输入一致。详情只读；编辑配置请打开配置资产。");
            else SirenixEditorGUI.WarningMessageBox("目录已过期，自动匹配已阻断。修改配置后请重新生成目录。");
            using(new EditorGUILayout.HorizontalScope())
            {
                using(new EditorGUILayout.VerticalScope(GUILayout.Width(270))) _navigation?.DrawMenuTree();
                using(new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true))) DrawDetails(ref message);
            }
        }
        public void Dispose() { _details?.Dispose(); _details=null; _navigation=null; }
        #endregion
    }
}


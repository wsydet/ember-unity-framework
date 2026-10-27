using System;
using System.Collections.Generic;
using Ember.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>List 的序列化接线及销毁兜底。模板是外部资源所有者提供的 Prefab 引用。</summary>
    public sealed class EUIDynamicListHost : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private GameObject _itemTemplate;
        [SerializeField, Min(0)] private int _cacheCapacity = 20;
        [SerializeField] private string _dataBinding;
        #endregion

        #region 内部参数
        private IDisposable _owner;
        public ScrollRect Scroll => _scroll;
        public RectTransform Content => _scroll ? _scroll.content : null;
        public GameObject ItemTemplate => _itemTemplate;
        public int CacheCapacity => _cacheCapacity;
        public string DataBinding => _dataBinding;
        #endregion

        #region 生命周期
        private void OnDestroy() => _owner?.Dispose();
        #endregion

        #region 外部方法
        public EUIItemList<T> Create<T>(Func<T, string> key, Action<EUIItemLease<T>> bind)
        {
            if (_owner != null) throw new InvalidOperationException("宿主已有动态列表所有者。");
            if (key == null || bind == null) throw new ArgumentNullException("key/bind");
            var issues = ValidateConfiguration();
            if (issues.Count > 0) throw new InvalidOperationException(string.Join("\n", issues));
            if (Content.childCount != 0) throw new InvalidOperationException("动态 List Content 必须为空；固定子逻辑与动态实例不能混合所有权。");
            var list = new EUIItemList<T>(this, key, bind);
            _owner = list;
            return list;
        }

        internal void ReleaseOwner(IDisposable owner) { if (ReferenceEquals(_owner, owner)) _owner = null; }

        public List<string> ValidateConfiguration()
        {
            var issues = new List<string>();
            if (!_scroll || !_scroll.viewport || !_scroll.content) { issues.Add("List 需要 ScrollRect/Viewport/Content 引用。"); return issues; }
            if (!_scroll.transform.IsChildOf(transform) || !_scroll.viewport.IsChildOf(_scroll.transform)
                || _scroll.content.parent != _scroll.viewport) issues.Add("List Viewport/Content 不在宿主滚动层级内。");
            if (!_scroll.viewport.GetComponent<RectMask2D>() && !_scroll.viewport.GetComponent<Mask>()) issues.Add("Viewport 缺少裁剪。");
            var layout = Content.GetComponent<VerticalLayoutGroup>();
            if (!layout || !layout.childControlHeight || layout.childForceExpandHeight) issues.Add("首版 List 要求 VerticalLayoutGroup 控制高度且不强制扩展高度。");
            if (!_scroll.vertical || _scroll.horizontal) issues.Add("首版 List 只支持纵向滚动。");
            var fitter = Content.GetComponent<ContentSizeFitter>();
            var fitterEx = Content.GetComponent<ContentSizeFitterEx>();
            if (fitter && fitterEx) issues.Add("Content 同时存在两个 ContentSizeFitter，会争用尺寸。");
            if ((!fitter || fitter.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
                && (!fitterEx || fitterEx.VerticalFit != ContentSizeFitter.FitMode.PreferredSize)) issues.Add("Content 需要纵向 PreferredSize fitter。");
            if ((fitter && fitter.horizontalFit != ContentSizeFitter.FitMode.Unconstrained)
                || (fitterEx && fitterEx.HorizontalFit != ContentSizeFitter.FitMode.Unconstrained)) issues.Add("Content 横向 fitter 与 stretch 宽度可能形成布局循环。");
            if (_itemTemplate == null || _itemTemplate.scene.IsValid()) issues.Add("ItemTemplate 必须是项目 Prefab 资源，不可引用 Content 中固定实例。");
            else
            {
                var binding = _itemTemplate.GetComponent<EUIBinding>();
                if (!binding || binding.Role != EUIBindingRole.Item) issues.Add("ItemTemplate 根必须为 Item EUIBinding。");
                if (_itemTemplate.GetComponent<ContentSizeFitter>() || _itemTemplate.GetComponent<ContentSizeFitterEx>()) issues.Add("Item 根 fitter 与父布局控制高度冲突；使用 LayoutElement/ILayoutElement 提供高度。");
            }
            if (_cacheCapacity < 0) issues.Add("缓存容量不能小于零。");
            if (string.IsNullOrWhiteSpace(_dataBinding)) issues.Add("缺少数据绑定语义。");
            return issues;
        }
        #endregion
    }
}

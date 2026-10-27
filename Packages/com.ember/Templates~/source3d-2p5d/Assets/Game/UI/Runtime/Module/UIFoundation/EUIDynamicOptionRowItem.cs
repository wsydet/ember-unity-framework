using System;
using Ember.UIExtension;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class EUIOptionRowData
    {
        public string Id, Title;
        public Sprite Icon;
        public bool Selected, Interactable = true;
        public float Progress;
    }

    public partial class EUIDynamicOptionRowItem
    {
        #region 内部参数
        private EUIItemLease<EUIOptionRowData> _lease;
        public int ActiveSubscriptions => _lease != null && _lease.IsCurrent ? _lease.CleanupCount : 0;
        public string CurrentText => ((EUIToggleEx)ControlMap["Toggle"]).Label.text;
        public Sprite CurrentIcon => Icon.sprite;
        public float CurrentProgress => Progress.CurrentValue;
        #endregion
        #region 生命周期
        public override void OnResetDefault()
        {
            base.OnResetDefault();
            _lease?.Dispose(); _lease = null;
            SetContent("", false, true);
            Icon.sprite = null; Icon.enabled = false; Progress.SetProgress(0);
        }
        public override void OnClose() { _lease?.Dispose(); _lease = null; base.OnClose(); }
        public override void OnDispose() { _lease?.Dispose(); _lease = null; base.OnDispose(); }
        #endregion
        #region 外部方法
        public void Bind(EUIItemLease<EUIOptionRowData> lease, Action<EUIOptionRowData, int> selected)
        {
            _lease?.Dispose(); _lease = lease;
            var data = lease.Data;
            SetContent(data.Title, data.Selected, data.Interactable);
            Icon.sprite = data.Icon; Icon.enabled = data.Icon;
            Progress.SetProgress(data.Progress);
            var toggle = (EUIToggleEx)ControlMap["Toggle"];
            UnityAction<bool> handler = value => lease.TryApply(current =>
            {
                if (!toggle.IsInteractable() || !current.Item.IsVisible) return;
                current.Data.Selected = value;
                selected?.Invoke(current.Data, current.Index);
            });
            toggle.onValueChanged.AddListener(handler);
            lease.Track(() => toggle.onValueChanged.RemoveListener(handler));
            LayoutRebuilder.MarkLayoutForRebuild(Item.RectTransform);
        }
        public bool ApplyIcon(EUIItemLease<EUIOptionRowData> lease, Sprite icon)
            => ReferenceEquals(_lease, lease) && lease.TryApply(current => { Icon.sprite = icon; Icon.enabled = icon; });
        #endregion
    }
}

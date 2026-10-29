// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
namespace Game.UI
{
    /// <summary>S3 的中性行接口；实例及逻辑仍由宿主负责，不提供池或容器。</summary>
    public partial class EUIOptionRowItem
    {
        #region 外部方法
        public void SetContent(string title, bool selected, bool interactable)
        { Toggle.Label.text = title; Toggle.SetIsOnWithoutNotify(selected); Toggle.interactable = interactable; Toggle.Refresh(); }
        public bool IsSelected => Toggle.isOn;
        #endregion
    }
}


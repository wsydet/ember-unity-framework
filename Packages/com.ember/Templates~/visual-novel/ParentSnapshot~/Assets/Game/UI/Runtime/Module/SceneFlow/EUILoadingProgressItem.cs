// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using UnityEngine;

namespace Game.UI
{
    /// <summary>加载专用进度条：继承公共进度条资源，拥有填充、百分比和显示状态。</summary>
    public partial class EUILoadingProgressItem
    {
        #region 内部参数

        private int _displayedPercent = -1;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>接收页面已经过准备状态约束的展示进度，不参与加载时序。</summary>
        public void SetProgress(float progress, bool showBar, bool showNumber)
        {
            progress = Mathf.Clamp01(progress);
            Progress.SetValueWithoutNotify(progress);
            int percent = Mathf.FloorToInt(progress * 100f);
            if (_displayedPercent != percent)
            {
                _displayedPercent = percent;
                Value.SetSource(percent.ToString() + "%");
            }
            Track.gameObject.SetActive(showBar);
            Value.gameObject.SetActive(showNumber);
        }

        /// <summary>同时隐藏轨道、填充和数字；不改变页面的加载状态。</summary>
        public void HideVisuals()
        {
            Track.gameObject.SetActive(false);
            Value.gameObject.SetActive(false);
        }

        public override void OnResetDefault()
        {
            base.OnResetDefault();
            _displayedPercent = -1;
            SetProgress(0f, false, false);
        }

        #endregion
    }
}

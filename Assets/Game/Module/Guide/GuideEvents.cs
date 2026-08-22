using System;
using System.Collections.Generic;
using Ember.Core;
using UnityEngine;

namespace Game.Module.Guide
{
    /// <summary>
    /// 引导事件类型 —— 步骤监听并推进状态机的游戏事件。
    ///
    /// 相比 burner 的 30+ 个 SLG 专用事件，本框架只保留通用事件；
    /// 业务可通过 <see cref="GuideModule.NotifyCustom"/> 等入口扩展。
    /// 枚举值写死，避免调整顺序导致已配置资产失效。
    /// </summary>
    public enum GuideEventType
    {
        None = 0,

        /// <summary>UI 页面打开（eventParam: <see cref="GuideEventParamPage"/>）。</summary>
        OnPageShown = 1,

        /// <summary>UI 页面关闭（eventParam: <see cref="GuideEventParamPage"/>）。</summary>
        OnPageHidden = 2,

        /// <summary>UI 按钮被点击（eventParam: <see cref="GuideEventParamClickUI"/>）。</summary>
        OnClickUIButton = 3,

        /// <summary>延时结束（由 Delay 执行器触发）。</summary>
        OnDelayFinish = 4,

        /// <summary>引导遮罩被点击。</summary>
        OnGuideMaskClick = 5,

        /// <summary>自定义事件（eventParam: <see cref="GuideEventParamCustom"/>）。</summary>
        OnCustom = 6,
    }

    /// <summary>
    /// 引导内部事件 Key —— 供 <see cref="Ember.Core.EmberEventBus"/> 使用。
    /// 业务基址 <see cref="EmberBroadcastEvent.Game"/>（10000）起，偏移 1～4。
    /// 页面打开 / 关闭事件来自 <see cref="Ember.UI.EUIObserver"/>，不占用 EventBus key。
    /// </summary>
    public static class GuideEventKey
    {
        /// <summary>延时结束。参数：无。</summary>
        public const int DelayFinish = EmberBroadcastEvent.Game + 1;

        /// <summary>引导遮罩被点击。参数：无。</summary>
        public const int MaskClick = EmberBroadcastEvent.Game + 2;

        /// <summary>UI 按钮被点击。参数：(string pagePath, string ctrlName)。</summary>
        public const int ClickUIButton = EmberBroadcastEvent.Game + 3;

        /// <summary>自定义事件。参数：int key。</summary>
        public const int Custom = EmberBroadcastEvent.Game + 4;
    }

    /// <summary>
    /// 引导事件 —— 一条事件配置（事件类型 + 参数），由 <see cref="GuideStepDefine"/> 引用。
    /// 运行时通过 <see cref="GetEventHandleType"/> 找到对应的处理器类型。
    /// </summary>
    [Serializable]
    public class GuideEvent
    {
        #region 编辑器面板参数

        /// <summary>事件类型。</summary>
        public GuideEventType eventType;

        /// <summary>事件参数（具体类型见各事件说明）。</summary>
        [SerializeReference]
        public object eventParam;

        #endregion

        /// <summary>事件类型 → 处理器类型的映射。</summary>
        private static readonly Dictionary<GuideEventType, Type> HandleTypeMap = new()
        {
            [GuideEventType.OnPageShown]      = typeof(GuideEventHandleOnPageShown),
            [GuideEventType.OnPageHidden]     = typeof(GuideEventHandleOnPageHidden),
            [GuideEventType.OnClickUIButton]  = typeof(GuideEventHandleOnClickUIButton),
            [GuideEventType.OnDelayFinish]    = typeof(GuideEventHandleOnDelayFinish),
            [GuideEventType.OnGuideMaskClick] = typeof(GuideEventHandleOnGuideMaskClick),
            [GuideEventType.OnCustom]         = typeof(GuideEventHandleOnCustom),
        };

        /// <summary>获取指定事件类型对应的处理器类型。</summary>
        public static Type GetEventHandleType(GuideEventType eventType)
            => HandleTypeMap.TryGetValue(eventType, out var t) ? t : null;
    }

    // ============================================================
    // 事件参数类
    // ============================================================

    /// <summary>页面事件参数：按 prefab 路径匹配页面（"*" 或空 = 任意页面）。</summary>
    [Serializable]
    public class GuideEventParamPage
    {
        public string pagePath;
    }

    /// <summary>UI 按钮点击事件参数：按「页面路径 + 控件名」匹配（"*" 或空 = 任意）。</summary>
    [Serializable]
    public class GuideEventParamClickUI
    {
        public string pagePath;
        public string ctrlName;
    }

    /// <summary>自定义事件参数：按 key 匹配。</summary>
    [Serializable]
    public class GuideEventParamCustom
    {
        public int key;
    }
}

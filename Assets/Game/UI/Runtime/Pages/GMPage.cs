/*=============================================================
 * author       : DESKTOP-5SRUU55
 * prefab name  : GMPage
 * page name    : GMPage
 * create date  : 2026/8/18 18:27:57
 * 已迁移至业务层（Game.UI）：框架层程序集不能反向引用 uiextension 增强包
 * 逻辑：GM 调试页 —— 时间缩放控制 + 顶层状态显示 + 增强组件测试
================================================================*/
using Ember.Basic;
using Ember.Core;

using UnityEngine;
using UnityEngine.UI;

using Ember.UIExtension;

namespace Game.UI
{
    public partial class GMPage
    {
        // ── 生命周期钩子（在此文件中填充业务逻辑） ──

        public override void OnInit()
        {
            base.OnInit();

            // GM 面板默认关闭，通过 Btn_GM 打开
            Panel_GM.gameObject.SetActive(false);

            // 按钮点击切换面板显示
            Btn_GM.onClick.AddListener(TogglePanel);

            // 退出按钮（增强组件 EUIButtonEx）：关闭 GM 面板
            if (EUIBtn_Exit != null)
                EUIBtn_Exit.onClick.AddListener(ClosePanel);

            // ── 时间缩放：Toggle 开关时间条显示，Slider 控制缩放倍率 ──

            // 时间条容器（Time 节点）初始隐藏
            _timeBarRoot = Pgb_TimeScale.transform.parent;
            if (_timeBarRoot != null)
                _timeBarRoot.gameObject.SetActive(false);

            // Toggle：控制时间条显示（默认关闭）
            Tgl_Test.isOn = false;
            Tgl_Test.onValueChanged.AddListener(OnTimeBarToggle);

            // Slider：控制 EmberTimeManager.TimeScale（0.1x ~ 3x，默认 1x）
            Pgb_TimeScale.minValue = 0.1f;
            Pgb_TimeScale.maxValue = 3f;
            Pgb_TimeScale.value = 1f;
            Pgb_TimeScale.onValueChanged.AddListener(OnTimeScaleChanged);
            RefreshTimeScaleText(Pgb_TimeScale.value);

            // ── 增强组件测试 ──

            // EUIToggleEx：使用 Label 槽位直接访问文本
            if (EUITgl_Test != null)
            {
                EUITgl_Test.onValueChanged.AddListener(OnEnhancedToggleChanged);
                if (EUITgl_Test.Label != null)
                    EUITgl_Test.Label.text = "增强开关";
            }

            // EUICircleImage：FillPercent 演示进度环
            if (Img_Circle != null)
                Img_Circle.FillPercent = 0.5f;

            // EUIImageEx：帧动画测试（无精灵数组时仅演示颜色）
            if (EUIImg_Test != null)
                EUIImg_Test.color = new Color(0.3f, 0.8f, 0.5f, 1f);

            // 每帧刷新状态机名称
            NeedUpdate = true;
        }

        public override void OnReset()
        {
            // 页面默认关闭
            Panel_GM.gameObject.SetActive(false);
        }

        public override void OnDispose()
        {
            Btn_GM.onClick.RemoveAllListeners();
            if (EUIBtn_Exit != null)
                EUIBtn_Exit.onClick.RemoveAllListeners();
            Tgl_Test.onValueChanged.RemoveListener(OnTimeBarToggle);
            Pgb_TimeScale.onValueChanged.RemoveListener(OnTimeScaleChanged);
            if (EUITgl_Test != null)
                EUITgl_Test.onValueChanged.RemoveListener(OnEnhancedToggleChanged);
            base.OnDispose();
        }

        /// <summary>每帧：刷新顶层状态机状态名</summary>
        public override void OnUpdate()
        {
            RefreshGameStateText();
        }

        // ── 内部参数 ──

        private Transform _timeBarRoot;

        // ── 内部方法 ──

        private void TogglePanel()
        {
            Panel_GM.gameObject.SetActive(!Panel_GM.gameObject.activeSelf);
        }

        private void ClosePanel()
        {
            Panel_GM.gameObject.SetActive(false);
        }

        /// <summary>Toggle 开关：控制时间条（Time 容器）显示，关闭时恢复 TimeScale = 1</summary>
        private void OnTimeBarToggle(bool isOn)
        {
            if (_timeBarRoot != null)
                _timeBarRoot.gameObject.SetActive(isOn);

            if (!isOn)
            {
                // 关闭时间控制：恢复默认倍率，滑块和文本同步
                Pgb_TimeScale.value = 1f;
                ApplyTimeScale(1f);
                RefreshTimeScaleText(1f);
            }

            EmberDebug.LogEvent("GM", $"时间条显示: {isOn}");
        }

        /// <summary>Slider 变化：设置全局时间缩放倍率</summary>
        private void OnTimeScaleChanged(float value)
        {
            ApplyTimeScale(value);
            RefreshTimeScaleText(value);
        }

        /// <summary>
        /// 应用时间缩放：同时设置 UnityEngine.Time.timeScale（影响所有游戏逻辑）
        /// 和 EmberTimeManager.TimeScale（保持框架时间一致）。
        /// </summary>
        private void ApplyTimeScale(float value)
        {
            // Unity 全局时间缩放：影响 Animator / Update / DOTween / 物理等
            UnityEngine.Time.timeScale = value;

            // 框架时间同步（可选，框架内用 EmberTimeManager.DeltaTime 的逻辑同样生效）
            var tm = EmberTimeManager.Instance;
            if (tm != null)
                tm.TimeScale = value;
        }

        /// <summary>刷新倍率文本</summary>
        private void RefreshTimeScaleText(float value)
        {
            if (Txt_TimeScale != null)
                Txt_TimeScale.text = $"TimeScale: {value:F2}x";
        }

        /// <summary>刷新顶层状态机状态名（GameLauncher.Fsm.Current.Name）</summary>
        private void RefreshGameStateText()
        {
            if (Txt_GameState == null) return;

            string stateName = "—";
            var launcher = GameLauncher.Instance;
            if (launcher != null && launcher.Fsm != null && launcher.Fsm.Current != null)
                stateName = launcher.Fsm.Current.Name;

            if (Txt_GameState.text != stateName)
                Txt_GameState.text = stateName;
        }

        /// <summary>增强开关（EUIToggleEx）回调：通过 Label 槽位反馈状态</summary>
        private void OnEnhancedToggleChanged(bool isOn)
        {
            if (EUITgl_Test != null && EUITgl_Test.Label != null)
                EUITgl_Test.Label.text = isOn ? "已开启" : "已关闭";
            EmberDebug.LogEvent("GM", $"增强开关状态: {isOn}");
        }
    }
}

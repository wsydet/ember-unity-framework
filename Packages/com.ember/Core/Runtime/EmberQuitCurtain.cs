// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

using System;
using UnityEngine;
using Ember.Basic;

namespace Ember.Core
{
    /// <summary>
    /// 退出封屏 —— 退出应用前把画面变成不透明黑幕，避免「UI 已销毁、3D 场景还在渲染」的穿帮帧。
    ///
    /// <para><b>要解决的问题：</b><see cref="GameLauncher.Quit"/> 先销毁 Module 与 Manager（UI 页面随之销毁），
    /// 再调用 <c>ApplicationQuitUtil.Quit()</c>；而 Unity 的 <c>Object.Destroy</c> 在「当前 Update 之后、
    /// 渲染之前」生效，PC 等平台的 <c>Application.Quit()</c> 之后还会继续出帧，
    /// 于是「UI 已经没了、只剩 3D 场景」的那一帧会被真的显示出来。</para>
    ///
    /// <para><b>两道封屏：</b></para>
    /// <list type="bullet">
    ///   <item>引擎侧：把 3D 主相机清成纯黑（<see cref="BlackoutCamera"/>），不依赖任何 UI 对象。</item>
    ///   <item>UI 侧：通过 <see cref="CoverRequested"/> 请求 UI 层盖一张不透明全屏遮罩；
    ///   <c>Ember.UI</c> 默认订阅并实现。遮罩不参与 EUI 页面栈，因此框架销毁页面时不会被一起销毁。</item>
    /// </list>
    ///
    /// <para><b>调用时机：</b>由框架负责。<see cref="GameLauncher.Quit"/> 与 <see cref="GameLauncher"/> 的
    /// 应用退出兜底都会先调用 <see cref="Show"/>，业务一般不需要直接调用。
    /// 需要「淡出后再退出」时，业务先播完自己的淡出动画，再调用 <see cref="GameLauncher.Quit"/>。</para>
    /// </summary>
    public static class EmberQuitCurtain
    {
        #region 内部参数

        private const string TAG = LogTags.CoreQuitCurtain;

        /// <summary>本次运行是否已经封屏（幂等标记）。</summary>
        private static bool _shown;

        /// <summary>
        /// 请求 UI 层盖一张不透明全屏遮罩。由 <c>Ember.UI</c> 在 UI 引擎初始化时订阅；
        /// 需要换成品牌图或淡出等自定义表现的项目也可以订阅。
        /// 订阅方必须保证同帧生效，并自行处理重复调用。
        /// </summary>
        public static event Action CoverRequested;

        /// <summary>本次运行是否已经封屏。</summary>
        public static bool IsShown => _shown;

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        /// <summary>
        /// 取框架 3D 主相机。启动器不存在或未挂载主相机时返回 null。
        /// 用 <see cref="GameLauncher.IsValid"/> 判断，避免访问 <c>Instance</c> 触发单例自动创建。
        /// </summary>
        private static Camera MainCamera()
        {
            if (!GameLauncher.IsValid) return null;
            return GameLauncher.Instance.MainCamera;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>
        /// 把一台相机改成纯黑输出：清空渲染层 + Solid Color 黑底。
        /// 传入 null 时无操作。项目若存在额外的 3D 相机（框架只封屏主相机），可在退出时一并调用。
        /// </summary>
        /// <param name="camera">要黑屏的相机</param>
        [NoGC]
        public static void BlackoutCamera(Camera camera)
        {
            if (camera == null) return;

            camera.cullingMask = 0;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
        }

        /// <summary>
        /// 立即封屏（幂等）：3D 主相机转纯黑 + 请求 UI 层盖不透明遮罩。
        /// 同一帧内生效，因此调用之后渲染出的任何一帧都不会再露出 3D 场景。
        /// </summary>
        [HasGC]
        public static void Show()
        {
            if (_shown) return;
            _shown = true;

            BlackoutCamera(MainCamera());

            bool hasCover = CoverRequested != null;
            if (hasCover)
            {
                try { CoverRequested(); }
                catch (Exception ex) { EmberDebug.LogError(TAG, $"UI 封屏遮罩请求失败：{ex.Message}"); }
            }

            EmberDebug.LogShutdown(TAG, hasCover
                ? "退出封屏已生效：3D 主相机转纯黑 + UI 不透明遮罩。"
                : "退出封屏已生效：3D 主相机转纯黑（无 UI 遮罩订阅者）。");
        }

        /// <summary>
        /// 清空封屏标记，供编辑器重新进入 Play Mode 或测试复用。
        /// 不还原相机状态：相机的清屏参数随场景重新加载恢复。
        /// </summary>
        [NoGC]
        public static void Reset()
        {
            _shown = false;
        }

        #endregion
    }
}

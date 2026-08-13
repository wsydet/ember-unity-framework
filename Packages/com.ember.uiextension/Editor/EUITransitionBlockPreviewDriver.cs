// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using DG.Tweening;

using UnityEditor;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// 编辑模式下的方块过渡实时预览驱动器。
    /// 预览在 Runtime 组件（EUITransitionBlock）中通过 Odin [Button] 触发，创建 UpdateType.Manual 的 tween；
    /// 本类位于 Editor 程序集，通过 <see cref="EditorApplication.update"/> 逐帧调用
    /// <see cref="DOTween.ManualUpdate(float, float)"/> 并重绘 Scene 视图，实现不进入播放模式的逐帧预览。
    /// </summary>
    [InitializeOnLoad]
    internal static class EUITransitionBlockPreviewDriver
    {
        #region 内部参数

        private static double _lastTime;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        static EUITransitionBlockPreviewDriver()
        {
            EditorApplication.update += Tick;
            _lastTime = EditorApplication.timeSinceStartup;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static void Tick()
        {
            double now = EditorApplication.timeSinceStartup;

            // 播放模式由 DOTween 自身 Update 接管，这里只管编辑模式；进出播放模式时重置基准避免巨大 dt。
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                _lastTime = now;
                return;
            }

            float dt = (float)(now - _lastTime);
            _lastTime = now;
            dt = Mathf.Clamp(dt, 0f, 0.1f);

            if (DOTween.TotalActiveTweens() <= 0) return;

            DOTween.ManualUpdate(dt, dt);
            SceneView.RepaintAll();
        }

        #endregion
    }
}

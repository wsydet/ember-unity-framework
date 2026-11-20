using Ember.Basic;
using Ember.Core;
using Ember.UI;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace Game.UI
{
    public static partial class EUIAppearanceBootstrap
    {
        private static EmberLifetime _frameRateLifetime;

        private static async UniTask WatchFrameRateAsync(EmberLifetime lifetime)
        {
            try
            {
                while (!lifetime.Cancellation.IsCancellationRequested)
                {
                    await UniTask.Delay(500, ignoreTimeScale: true, cancellationToken: lifetime.Cancellation);
                    if (ReferenceEquals(_frameRateLifetime, lifetime)) UIFrameRatePolicy.CheckRefreshRate();
                }
            }
            catch (OperationCanceledException) when (lifetime.Cancellation.IsCancellationRequested) { }
        }

        /// <summary>UI 启动接线，独立于是否创建 GM；安装器负责幂等与宿主释放。</summary>
        [HasGC]
        public static void InitializeUI()
        {
            var launcher = GameLauncher.Instance;
            EUIBackdrop.DefaultSettings = Resources.Load<EUIBackdropSettings>("Config/UIFoundation/EUIBackdropSettings");
            EUIBackdrop.RequestInstallation(launcher.MainCamera, launcher.UICamera, launcher.UIRoot.transform);
            UIFrameRatePolicy.Initialize();
            // 跟随 GameLauncher 会话和 PlayerLoop，关闭全部界面或暂停游戏时间也继续检查。
            var lifetime = launcher.Lifetime;
            if (lifetime != null && !ReferenceEquals(_frameRateLifetime, lifetime))
            {
                _frameRateLifetime = lifetime;
                WatchFrameRateAsync(lifetime).Forget();
            }
        }
    }
}

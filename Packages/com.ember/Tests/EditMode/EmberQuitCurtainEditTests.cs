// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using Ember.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Ember.UI.Tests
{
    /// <summary>
    /// 退出封屏（<see cref="EmberQuitCurtain"/> + <c>EUIQuitCurtainCover</c>）的 EditMode 契约测试。
    ///
    /// <para>完整的 <c>GameLauncher.Quit()</c> 链路不能在 EditMode 用例里跑：编辑器下它会立即停止
    /// Play Mode。这里验证封屏本身的两条契约：3D 相机转纯黑、UI 侧盖出不透明 Overlay 遮罩，
    /// 以及遮罩必须活过框架清理（<c>Uninstall</c> 只退订、不销毁）。</para>
    /// </summary>
    public class EmberQuitCurtainEditTests
    {
        #region 内部参数

        private Camera _sceneCamera;
        private CameraClearFlags _sceneClearFlags;
        private Color _sceneBackground;
        private int _sceneCullingMask;
        private bool _sceneCameraCaptured;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        [SetUp]
        public void SetUp()
        {
            // 当前打开的场景里可能已存在 GameLauncher（封屏会改主相机），先记录以便还原
            if (GameLauncher.IsValid && GameLauncher.Instance.MainCamera != null)
            {
                _sceneCamera = GameLauncher.Instance.MainCamera;
                _sceneClearFlags = _sceneCamera.clearFlags;
                _sceneBackground = _sceneCamera.backgroundColor;
                _sceneCullingMask = _sceneCamera.cullingMask;
                _sceneCameraCaptured = true;
            }

            EUIQuitCurtainCover.Uninstall();
            EmberQuitCurtain.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            EUIQuitCurtainCover.Uninstall();
            EmberQuitCurtain.Reset();

            if (EUIQuitCurtainCover.Cover != null)
                Object.DestroyImmediate(EUIQuitCurtainCover.Cover);

            if (_sceneCameraCaptured && _sceneCamera != null)
            {
                _sceneCamera.clearFlags = _sceneClearFlags;
                _sceneCamera.backgroundColor = _sceneBackground;
                _sceneCamera.cullingMask = _sceneCullingMask;
            }

            _sceneCamera = null;
            _sceneCameraCaptured = false;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        [Test]
        public void CoverIsOpaqueOverlayAboveEveryLayer()
        {
            EUIQuitCurtainCover.Install();
            EmberQuitCurtain.Show();

            var cover = EUIQuitCurtainCover.Cover;
            Assert.IsNotNull(cover, "封屏遮罩应当在请求后存在");
            Assert.IsTrue(cover.activeSelf, "封屏遮罩应当在请求后可见");

            var canvas = cover.GetComponent<Canvas>();
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode,
                "遮罩必须走 Overlay，才能在所有相机之后渲染，不依赖 UI 相机");
            Assert.Greater(canvas.sortingOrder, 30000, "遮罩必须高于 FreePage（30000 起）");
            Assert.Greater(canvas.sortingOrder, 32500, "遮罩必须高于业务启动黑幕（BootSplash 32500）");

            var image = EUIQuitCurtainCover.CoverImage;
            Assert.IsNotNull(image, "遮罩必须包含满屏黑图");
            Assert.AreEqual(Color.black, image.color, "遮罩必须是不透明纯黑");
            Assert.AreEqual(1f, image.color.a, 0.0001f);
            Assert.IsFalse(image.raycastTarget, "封屏只负责画面，不应拦截输入");

            var rect = image.rectTransform;
            Assert.AreEqual(Vector2.zero, rect.anchorMin, "黑图必须撑满遮罩");
            Assert.AreEqual(Vector2.one, rect.anchorMax, "黑图必须撑满遮罩");
        }

        [Test]
        public void ShowIsIdempotentAndReportsShown()
        {
            EUIQuitCurtainCover.Install();

            EmberQuitCurtain.Show();
            var first = EUIQuitCurtainCover.Cover;

            EmberQuitCurtain.Show();

            Assert.IsTrue(EmberQuitCurtain.IsShown);
            Assert.AreSame(first, EUIQuitCurtainCover.Cover, "重复封屏不应重复创建遮罩");
        }

        [Test]
        public void ShowWithoutCoverSubscriberDoesNotThrow()
        {
            EUIQuitCurtainCover.Uninstall();
            EmberQuitCurtain.Reset();

            Assert.DoesNotThrow(() => EmberQuitCurtain.Show());
            Assert.IsTrue(EmberQuitCurtain.IsShown);
        }

        [Test]
        public void UninstallKeepsCoverAliveForPostCleanupFrames()
        {
            EUIQuitCurtainCover.Install();
            EmberQuitCurtain.Show();

            // 框架清理（EUIViewEngine.Shutdown）会调用 Uninstall；遮罩必须继续存在，
            // 否则「清理完成 → 进程退出」之间的帧会重新露出 3D 场景
            EUIQuitCurtainCover.Uninstall();

            var cover = EUIQuitCurtainCover.Cover;
            Assert.IsNotNull(cover);
            Assert.IsTrue(cover.activeSelf, "退订后遮罩必须仍然可见");
        }

        [Test]
        public void BlackoutCameraClearsRendering()
        {
            var host = new GameObject("QuitCurtainCameraProbe");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.cullingMask = ~0;
                camera.backgroundColor = Color.magenta;

                EmberQuitCurtain.BlackoutCamera(camera);

                Assert.AreEqual(CameraClearFlags.SolidColor, camera.clearFlags);
                Assert.AreEqual(Color.black, camera.backgroundColor);
                Assert.AreEqual(0, camera.cullingMask, "必须清空渲染层，否则 3D 内容仍会画在黑底之上");
            }
            finally
            {
                Object.DestroyImmediate(host);
            }

            Assert.DoesNotThrow(() => EmberQuitCurtain.BlackoutCamera(null));
        }

        #endregion
    }
}

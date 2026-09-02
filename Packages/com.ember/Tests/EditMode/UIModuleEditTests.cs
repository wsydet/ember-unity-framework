// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Ember.UI;
using Ember.UIExtension;
using Ember.UIExtension.Editor;

using NUnit.Framework;

using UniRx;

using UnityEditor;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UI.Tests
{
    /// <summary>
    /// 纯 C# 逻辑的 Edit Mode 测试（不需要 Play Mode）。
    /// </summary>
    public class UIModuleEditTests
    {
        #region EUIPageDef

        [Test]
        public void PageDef_ShouldStorePrefabPath()
        {
            var def = new EUIPageDef("ui/test_page", 100);
            Assert.AreEqual("ui/test_page", def.PrefabPath);
        }

        [Test]
        public void PageDef_ShouldStoreLayer()
        {
            var def = new EUIPageDef("ui/test_page", UILayer.Popup);
            Assert.AreEqual((int)UILayer.Popup, def.Layer);
        }

        [Test]
        public void PageDef_ShouldStorePageType()
        {
            var def = new EUIPageDef("ui/test_page", UILayer.Normal, PageType.MainPage);
            Assert.AreEqual(PageType.MainPage, def.PageType);
        }

        [Test]
        public void PageDef_DefaultPageType_ShouldBeMainPage()
        {
            var def = new EUIPageDef("ui/test_page", 100);
            Assert.AreEqual(PageType.MainPage, def.PageType);
        }

        [Test]
        public void PageDef_FullScreenPopup_ShouldDeriveFullScreenState()
        {
            var def = new EUIPageDef("ui/full_popup", UILayer.Popup, PageType.FullScreenPopup);
            Assert.AreEqual(PageType.FullScreenPopup, def.PageType);
            Assert.IsTrue(def.IsFullScreen);
        }

        [Test]
        public void PageDef_LegacyFullScreenArgument_ShouldNormalizeToFullScreenPopup()
        {
            var def = new EUIPageDef("ui/full_popup", UILayer.Popup, PageType.Popup,
                isFullScreen: true);
            Assert.AreEqual(PageType.FullScreenPopup, def.PageType);
            Assert.IsTrue(def.IsFullScreen);
        }

        [Test]
        public void PageDef_LegacyFullScreenArgument_WithNonPopup_ShouldThrow()
        {
            Assert.Throws<System.ArgumentException>(() =>
                new EUIPageDef("ui/main", UILayer.Normal, PageType.MainPage,
                    isFullScreen: true));
        }

        #endregion

        // --------------------------------------------------------

        #region UILayer Enum

        [Test]
        public void UILayer_Order_Background_Lowest()
        {
            Assert.Less((int)UILayer.Background, (int)UILayer.Normal);
            Assert.Less((int)UILayer.Normal, (int)UILayer.Popup);
            Assert.Less((int)UILayer.Popup, (int)UILayer.TopMost);
        }

        #endregion

        // --------------------------------------------------------

        #region EUIBgMaskPool

        [Test]
        public void BgMask_ShouldRenderOnPopupLayerAndReceiveClicksAfterReuse()
        {
            var root = new GameObject("UIRoot", typeof(RectTransform));
            var cameraObject = new GameObject("UICamera", typeof(Camera));
            var pool = new EUIBgMaskPool(root.transform, cameraObject.GetComponent<Camera>());
            try
            {
                int firstClickCount = 0;
                var mask = pool.Get(2000, () => firstClickCount++, Color.black, layer: 5);

                Assert.AreEqual(5, mask.layer);
                Assert.IsNotNull(mask.GetComponent<GraphicRaycaster>());
                Assert.IsTrue(mask.GetComponent<Image>().raycastTarget);
                Assert.AreEqual(RenderMode.ScreenSpaceCamera, mask.GetComponent<Canvas>().renderMode);
                Assert.AreSame(cameraObject.GetComponent<Camera>(),
                    mask.GetComponent<Canvas>().worldCamera);
                Assert.AreEqual(1999, mask.GetComponent<Canvas>().sortingOrder);

                mask.GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(1, firstClickCount);

                pool.Return(mask);

                int secondClickCount = 0;
                var reused = pool.Get(2500, () => secondClickCount++, Color.white, layer: 6);
                Assert.AreSame(mask, reused);
                Assert.AreEqual(6, reused.layer);
                Assert.AreEqual(2499, reused.GetComponent<Canvas>().sortingOrder);

                reused.GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(1, firstClickCount, "回池时应移除旧点击监听");
                Assert.AreEqual(1, secondClickCount);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
            }
        }

        #endregion

        // --------------------------------------------------------

        #region EUIObserver

        [Test]
        public void Observer_OnPageOpened_ShouldNotify()
        {
            var pageDef = new EUIPageDef("ui/test", UILayer.Normal);
            PageLifecycleEvent? received = null;

            var sub = EUIObserver.OnPageOpened.Subscribe(e => received = e);

            EUIObserver.NotifyOpened(pageDef, "hello");

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(pageDef, received.Value.Page);
            Assert.AreEqual("hello", received.Value.Args);

            sub.Dispose();
        }

        [Test]
        public void Observer_OnPageClosed_ShouldNotify()
        {
            var pageDef = new EUIPageDef("ui/test", UILayer.Popup);
            PageLifecycleEvent? received = null;

            var sub = EUIObserver.OnPageClosed.Subscribe(e => received = e);

            EUIObserver.NotifyClosed(pageDef, null);

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(pageDef, received.Value.Page);

            sub.Dispose();
        }

        [Test]
        public void Observer_OnPagePaused_ShouldNotify()
        {
            var pageDef = new EUIPageDef("ui/test", UILayer.Popup);
            PageLifecycleEvent? received = null;

            var sub = EUIObserver.OnPagePaused.Subscribe(e => received = e);

            EUIObserver.NotifyPaused(pageDef);

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(pageDef, received.Value.Page);
            Assert.IsNull(received.Value.Args); // Pause 不传 args

            sub.Dispose();
        }

        [Test]
        public void Observer_OnPageResumed_ShouldNotify()
        {
            var pageDef = new EUIPageDef("ui/test", UILayer.Popup);
            PageLifecycleEvent? received = null;

            var sub = EUIObserver.OnPageResumed.Subscribe(e => received = e);

            EUIObserver.NotifyResumed(pageDef);

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(pageDef, received.Value.Page);

            sub.Dispose();
        }

        [Test]
        public void Observer_OnAllClosed_ShouldNotify()
        {
            bool received = false;
            var sub = EUIObserver.OnAllClosed.Subscribe(_ => received = true);

            EUIObserver.NotifyAllClosed();

            Assert.IsTrue(received);
            sub.Dispose();
        }

        [Test]
        public void Observer_Unsubscribed_ShouldNotReceive()
        {
            int count = 0;
            var sub = EUIObserver.OnPageOpened.Subscribe(_ => count++);

            EUIObserver.NotifyOpened(new EUIPageDef("a", 1), null);
            Assert.AreEqual(1, count);

            sub.Dispose();

            EUIObserver.NotifyOpened(new EUIPageDef("b", 2), null);
            Assert.AreEqual(1, count); // 不再增长
        }

        [Test]
        public void Observer_WhereFilter_ShouldWork()
        {
            var settingsDef = new EUIPageDef("ui/settings", UILayer.Popup);
            var bagDef = new EUIPageDef("ui/bag", UILayer.Popup);

            PageLifecycleEvent? received = null;

            var sub = EUIObserver.OnPageOpened
                .Where(e => e.Page.PrefabPath == "ui/settings")
                .Subscribe(e => received = e);

            // bag 不应触发
            EUIObserver.NotifyOpened(bagDef, null);
            Assert.IsFalse(received.HasValue);

            // settings 应触发
            EUIObserver.NotifyOpened(settingsDef, null);
            Assert.IsTrue(received.HasValue);

            sub.Dispose();
        }

        #endregion

        // --------------------------------------------------------

        #region EUIEvents

        [Test]
        public void UIEvents_Keys_InCorrectRange()
        {
            Assert.AreEqual(5000, EUIEvents.UIViewEngineReady);
            Assert.AreEqual(5001, EUIEvents.UIViewEngineShutdown);
            Assert.AreEqual(5002, EUIEvents.UIManagerReady);
        }

        #endregion

        // --------------------------------------------------------

        #region UI 过渡模式

        [Test]
        public void Binding_RegularTransition_ShouldKeepExactlyOneMode()
        {
            var go = new GameObject("TransitionBinding");
            try
            {
                var binding = go.AddComponent<EUIBinding>();
                var property = typeof(EUIBinding).GetProperty(
                    "RegularTransition", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(property);

                property.SetValue(binding, EUIBinding.RegularTransitionMode.Animator);
                Assert.IsFalse(binding.UsePresetFade);
                Assert.IsTrue(binding.UseAnimator);
                Assert.IsFalse(binding.UseCustomTransition);
                Assert.IsFalse(binding.UseTransitionBlock);

                property.SetValue(binding, EUIBinding.RegularTransitionMode.CustomCode);
                Assert.IsFalse(binding.UsePresetFade);
                Assert.IsFalse(binding.UseAnimator);
                Assert.IsTrue(binding.UseCustomTransition);
                Assert.IsFalse(binding.UseTransitionBlock);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Binding_TransitionBlock_ShouldPreserveCustomStage()
        {
            var go = new GameObject("BlockTransitionBinding");
            try
            {
                var binding = go.AddComponent<EUIBinding>();
                var serializedBinding = new SerializedObject(binding);
                serializedBinding.FindProperty("usePresetFade").boolValue = true;
                serializedBinding.FindProperty("useTransitionBlock").boolValue = true;
                serializedBinding.FindProperty("useAnimator").boolValue = true;
                serializedBinding.FindProperty("useCustomTransition").boolValue = true;
                serializedBinding.ApplyModifiedPropertiesWithoutUndo();

                var callback = typeof(EUIBinding).GetMethod(
                    "OnTransitionBlockChanged", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(callback);
                callback.Invoke(binding, null);

                Assert.IsTrue(binding.UseTransitionBlock);
                Assert.IsTrue(binding.UseCustomTransition);
                Assert.IsFalse(binding.UsePresetFade);
                Assert.IsFalse(binding.UseAnimator);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Page_AnimatorOnly_ShouldEnterTransitionSequence()
        {
            var go = new GameObject("AnimatorPage");
            try
            {
                var page = new EUIPage(go);
                page.SetTransition(false, false, true, false, 0.3f, 0.2f);

                var property = typeof(EUIPage).GetProperty(
                    "HasTransition", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(property);
                Assert.IsTrue((bool)property.GetValue(page));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Page_RegularLegacyCombination_ShouldNormalizeToCustomOnly()
        {
            var go = new GameObject("LegacyTransitionPage");
            try
            {
                var page = new EUIPage(go);
                page.SetTransition(true, false, true, true, 0.3f, 0.2f);

                Assert.IsFalse(GetPrivateBool(page, "_usePresetFade"));
                Assert.IsFalse(GetPrivateBool(page, "_useAnimator"));
                Assert.IsTrue(GetPrivateBool(page, "_useCustomTransition"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Page_TransitionBlock_ShouldKeepOriginalCustomChain()
        {
            var go = new GameObject("BlockTransitionPage");
            try
            {
                var page = new EUIPage(go);
                page.SetTransition(true, true, true, true, 0.3f, 0.2f);

                Assert.IsTrue(GetPrivateBool(page, "_useTransitionBlock"));
                Assert.IsTrue(GetPrivateBool(page, "_useCustomTransition"));
                Assert.IsFalse(GetPrivateBool(page, "_usePresetFade"));
                Assert.IsFalse(GetPrivateBool(page, "_useAnimator"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Page_CustomTransition_ShouldPrepareVisibleRoot()
        {
            var go = new GameObject("CustomTransitionPage");
            try
            {
                var page = new EUIPage(go);
                page.SetTransition(false, false, false, true, 0.3f, 0.2f);

                var method = typeof(EUIPage).GetMethod(
                    "PrepareShowTransition", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(method);
                method.Invoke(page, null);

                Assert.AreEqual(1f, page.CanvasGroup.alpha);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static bool GetPrivateBool(EUIPage page, string fieldName)
        {
            var field = typeof(EUIPage).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            return (bool)field.GetValue(page);
        }

        #endregion

        // --------------------------------------------------------

        #region 页面可选代码能力

        [Test]
        public void OptionalPageFeatures_Default_ShouldGenerateNoOverrides()
        {
            var go = new GameObject("DefaultPage");
            try
            {
                var binding = go.AddComponent<EUIBinding>();
                ConfigureOptionalPageFeatures(binding, PageType.MainPage, false, false, false);

                Assert.IsEmpty(BuildOptionalPageFeatureMembers(binding));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OptionalPageFeatures_UIUpdate_ShouldGenerateNeedUpdateAndOnUpdate()
        {
            var go = new GameObject("UpdatingPage");
            try
            {
                var binding = go.AddComponent<EUIBinding>();
                ConfigureOptionalPageFeatures(binding, PageType.MainPage, true, true, true);

                string members = BuildOptionalPageFeatureMembers(binding);
                StringAssert.Contains("public override bool NeedUpdate => true;", members);
                StringAssert.Contains("public override void OnUpdate()", members);
                StringAssert.DoesNotContain("AutoCreateClickableMask", members);
                StringAssert.DoesNotContain("OnClickMask", members);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OptionalPageFeatures_Popup_ShouldGenerateSelectedPopupOverrides()
        {
            var go = new GameObject("PopupPage");
            try
            {
                var binding = go.AddComponent<EUIBinding>();
                ConfigureOptionalPageFeatures(binding, PageType.Popup, false, true, true);

                string members = BuildOptionalPageFeatureMembers(binding);
                StringAssert.DoesNotContain("NeedUpdate => true", members);
                StringAssert.Contains("protected override bool AutoCreateClickableMask => true;", members);
                StringAssert.Contains("protected override void OnClickMask()", members);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OptionalPageFeatures_FrameworkUIUpdate_ShouldForwardToUserHook()
        {
            var go = new GameObject("FrameworkUpdatingPage");
            try
            {
                var binding = go.AddComponent<EUIBinding>();
                ConfigureOptionalPageFeatures(binding, PageType.MainPage, true, false, false);

                string members = BuildFrameworkOptionalPageFeatureMembers(binding);
                string hooks = BuildFrameworkOptionalUserHooks(binding);
                StringAssert.Contains("public override bool NeedUpdate => true;", members);
                StringAssert.Contains("public override void OnUpdate()", members);
                StringAssert.Contains("base.OnUpdate();", members);
                StringAssert.Contains("OnUpdateUser();", members);
                StringAssert.Contains("private void OnUpdateUser()", hooks);
                StringAssert.DoesNotContain("/// <summary>每帧更新", members);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OptionalPageFeatures_FrameworkDefault_ShouldOmitUIUpdateHook()
        {
            var go = new GameObject("FrameworkDefaultPage");
            try
            {
                var binding = go.AddComponent<EUIBinding>();
                ConfigureOptionalPageFeatures(binding, PageType.MainPage, false, false, false);

                Assert.IsEmpty(BuildFrameworkOptionalPageFeatureMembers(binding));
                Assert.IsEmpty(BuildFrameworkOptionalUserHooks(binding));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OptionalPageFeatures_FrameworkPopup_ShouldForwardToUserHook()
        {
            var go = new GameObject("FrameworkPopupPage");
            try
            {
                var binding = go.AddComponent<EUIBinding>();
                ConfigureOptionalPageFeatures(binding, PageType.Popup, false, true, true);

                string members = BuildFrameworkOptionalPageFeatureMembers(binding);
                string hooks = BuildFrameworkOptionalUserHooks(binding);
                StringAssert.Contains("protected override bool AutoCreateClickableMask => true;", members);
                StringAssert.Contains("protected override void OnClickMask()", members);
                StringAssert.Contains("OnClickMaskUser();", members);
                StringAssert.Contains("private void OnClickMaskUser()", hooks);
                StringAssert.DoesNotContain("OnUpdateUser", hooks);
                Assert.Less(members.IndexOf("OnClickMaskUser();"),
                    members.IndexOf("base.OnClickMask();"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FrameworkTemplate_ShouldPlaceOptionalMembersInsideManagedBlockAndHooksOutside()
        {
            const string path =
                "Packages/com.ember/UIExtension/Editor/Settings/CSharpCodeTemplate.Framework.tpl";
            string template = File.ReadAllText(path);

            int managedBegin = template.IndexOf("[EmberManaged:begin Lifecycle]");
            int optionalMembers = template.IndexOf("{framework_page_feature_members}");
            int managedEnd = template.IndexOf("[EmberManaged:end]");
            int optionalUserHooks = template.IndexOf("{framework_optional_user_hooks}");

            Assert.GreaterOrEqual(managedBegin, 0);
            Assert.Greater(optionalMembers, managedBegin);
            Assert.Greater(managedEnd, optionalMembers);
            Assert.Greater(optionalUserHooks, managedEnd);
            StringAssert.DoesNotContain("private void OnUpdateUser()", template);
            StringAssert.DoesNotContain("{page_feature_members}", template);
        }

        [Test]
        public void FrameworkSync_ShouldInsertOptionalMembersInsideManagedBlock()
        {
            var lines = new List<string>
            {
                "    public partial class TestPage",
                "    {",
                "        // [EmberManaged:begin Lifecycle]",
                "        public override void OnInit() { }",
                "        // [EmberManaged:end]",
                "    }",
            };
            var blocks = new List<string>
            {
                "        // [EmberOptional:begin UIUpdate]\n"
                + "        public override bool NeedUpdate => true;\n"
                + "        // [EmberOptional:end UIUpdate]\n",
            };

            var method = typeof(EUIBindingCodeGenUtility).GetMethod(
                "InsertFrameworkOptionalPageFeatureBlocks",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(null, new object[] { lines, blocks });

            int managedBegin = lines.FindIndex(line => line.Contains("[EmberManaged:begin Lifecycle]"));
            int optionalMember = lines.FindIndex(line => line.Contains("[EmberOptional:begin UIUpdate]"));
            int managedEnd = lines.FindIndex(line => line.Contains("[EmberManaged:end]"));
            Assert.Greater(optionalMember, managedBegin);
            Assert.Greater(managedEnd, optionalMember);
        }

        [Test]
        public void FrameworkSync_ShouldAddMissingUserHookOnlyOnce()
        {
            var lines = new List<string>
            {
                "    public partial class TestPage",
                "    {",
                "    }",
            };
            var method = typeof(EUIBindingCodeGenUtility).GetMethod(
                "EnsureFrameworkUserHook",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method);

            var args = new object[]
            {
                lines,
                "private void OnUpdateUser",
                "用户逐帧更新钩子。",
                "// 用户逻辑",
            };
            Assert.IsTrue((bool)method.Invoke(null, args));
            Assert.IsFalse((bool)method.Invoke(null, args));
            Assert.AreEqual(1,
                lines.FindAll(line => line.Contains("private void OnUpdateUser()")).Count);
        }

        [Test]
        public void FrameworkSync_ShouldRemoveDefaultOnUpdateUserWhenDisabled()
        {
            var lines = new List<string>
            {
                "    public partial class TestPage",
                "    {",
                "",
                "        /// <summary>用户逐帧更新钩子：框架 OnUpdate 结束时调用。</summary>",
                "        private void OnUpdateUser()",
                "        {",
                "            // 在此编写逐帧业务逻辑",
                "        }",
                "    }",
            };
            var method = typeof(EUIBindingCodeGenUtility).GetMethod(
                "TryRemoveFrameworkUserHook", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method);

            var args = new object[]
            {
                lines,
                "private void OnUpdateUser",
                "// 在此编写逐帧业务逻辑",
                "TestPage",
                false,
                false,
            };
            Assert.IsTrue((bool)method.Invoke(null, args));
            Assert.IsTrue((bool)args[5]);
            Assert.IsFalse(lines.Exists(line => line.Contains("OnUpdateUser")));
        }

        [Test]
        public void FrameworkSync_ShouldProtectCustomOnUpdateUserInNonInteractiveGeneration()
        {
            var lines = new List<string>
            {
                "    public partial class TestPage",
                "    {",
                "        private void OnUpdateUser()",
                "        {",
                "            TickGameplay();",
                "        }",
                "    }",
            };
            var method = typeof(EUIBindingCodeGenUtility).GetMethod(
                "TryRemoveFrameworkUserHook", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method);

            var args = new object[]
            {
                lines,
                "private void OnUpdateUser",
                "// 在此编写逐帧业务逻辑",
                "TestPage",
                false,
                false,
            };
            Assert.IsFalse((bool)method.Invoke(null, args));
            Assert.IsFalse((bool)args[5]);
            Assert.IsTrue(lines.Exists(line => line.Contains("TickGameplay();")));
        }

        private static string BuildOptionalPageFeatureMembers(EUIBinding binding)
        {
            var method = typeof(CSharpLogicImplementationData).GetMethod(
                "BuildOptionalPageFeatureMembers",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            return (string)method.Invoke(null, new object[] { binding });
        }

        private static string BuildFrameworkOptionalPageFeatureMembers(EUIBinding binding)
        {
            return InvokeOptionalPageFeatureBuilder("BuildFrameworkOptionalPageFeatureMembers", binding);
        }

        private static string BuildFrameworkOptionalUserHooks(EUIBinding binding)
        {
            return InvokeOptionalPageFeatureBuilder("BuildFrameworkOptionalUserHooks", binding);
        }

        private static string InvokeOptionalPageFeatureBuilder(string methodName, EUIBinding binding)
        {
            var method = typeof(CSharpLogicImplementationData).GetMethod(
                methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            return (string)method.Invoke(null, new object[] { binding });
        }

        [Test]
        public void Binding_LegacyFullScreenPopup_ShouldMigrateToMutuallyExclusiveType()
        {
            var go = new GameObject("LegacyFullScreenPopup");
            try
            {
                var binding = go.AddComponent<EUIBinding>();
                var legacyField = typeof(EUIBinding).GetField("pageFlags",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(legacyField);
                legacyField.SetValue(binding, PageFlags.Popup | PageFlags.FullScreen);

                binding.OnAfterDeserialize();

                Assert.AreEqual(PageType.FullScreenPopup, binding.PageType);
                Assert.AreEqual(PageFlags.None, legacyField.GetValue(binding));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OptionalPageFeatures_FullScreenPopup_ShouldUsePopupHooks()
        {
            var go = new GameObject("FullScreenPopupPage");
            try
            {
                var binding = go.AddComponent<EUIBinding>();
                ConfigureOptionalPageFeatures(binding, PageType.FullScreenPopup, false, true, true);

                string members = BuildOptionalPageFeatureMembers(binding);
                StringAssert.Contains("AutoCreateClickableMask", members);
                StringAssert.Contains("OnClickMask", members);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void ConfigureOptionalPageFeatures(EUIBinding binding, PageType pageType,
            bool useUIUpdate, bool generateAutoCreateClickableMaskOverride, bool generateOnClickMaskOverride)
        {
            var serializedBinding = new SerializedObject(binding);
            serializedBinding.FindProperty("isPage").boolValue = true;
            serializedBinding.FindProperty("pageType").intValue = (int)pageType;
            serializedBinding.FindProperty("pageFlags").intValue = 0;
            serializedBinding.FindProperty("useUIUpdate").boolValue = useUIUpdate;
            serializedBinding.FindProperty("generateAutoCreateClickableMaskOverride").boolValue =
                generateAutoCreateClickableMaskOverride;
            serializedBinding.FindProperty("generateOnClickMaskOverride").boolValue = generateOnClickMaskOverride;
            serializedBinding.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion

        // --------------------------------------------------------

        #region UI 资源路径

        [Test]
        public void PrefabPath_FrameworkMode_ShouldUseCommonDirectory()
        {
            var go = new GameObject("EUICommonPanel");
            var implementation = ScriptableObject.CreateInstance<CSharpLogicImplementationData>();
            try
            {
                var binding = go.AddComponent<EUIBinding>();
                ConfigureBinding(binding, EUIBinding.CodePathMode.Framework, "Framework", "EUICommonPanel");

                Assert.IsTrue(implementation.TryResolvePrefabPath(binding, out var path, out var error), error);
                Assert.AreEqual(
                    "Assets/GameResource/Resources/UI/Common/Prefabs/EUICommonPanel.prefab",
                    path);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(implementation);
            }
        }

        [Test]
        public void PrefabPath_BusinessMode_ShouldUseFirstClassPathSegmentAsModule()
        {
            var go = new GameObject("InventoryPanel");
            var implementation = ScriptableObject.CreateInstance<CSharpLogicImplementationData>();
            try
            {
                var binding = go.AddComponent<EUIBinding>();
                ConfigureBinding(binding, EUIBinding.CodePathMode.Business, "Inventory/Page", "InventoryPanel");

                Assert.IsTrue(implementation.TryResolvePrefabPath(binding, out var path, out var error), error);
                Assert.AreEqual(
                    "Assets/GameResource/Resources/UI/Module/Inventory/Prefabs/InventoryPanel.prefab",
                    path);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(implementation);
            }
        }

        [Test]
        public void PrefabPath_BusinessModeWithoutModule_ShouldFail()
        {
            var go = new GameObject("InvalidPanel");
            var implementation = ScriptableObject.CreateInstance<CSharpLogicImplementationData>();
            try
            {
                var binding = go.AddComponent<EUIBinding>();
                ConfigureBinding(binding, EUIBinding.CodePathMode.Business, string.Empty, "InvalidPanel");

                Assert.IsFalse(implementation.TryResolvePrefabPath(binding, out _, out var error));
                StringAssert.Contains("模块名", error);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(implementation);
            }
        }

        [Test]
        public void ResourcesPath_FullAssetPath_ShouldStripPrefixAndExtension()
        {
            Assert.AreEqual(
                "UI/Common/Prefabs/EUICommonPanel",
                DefaultUIResourceProvider.ToResourcesPath(
                    "Assets/GameResource/Resources/UI/Common/Prefabs/EUICommonPanel.prefab"));
        }

        [Test]
        public void ResourcesPath_RelativeWindowsPath_ShouldNormalizeAndStripExtension()
        {
            Assert.AreEqual(
                "UI/Module/Inventory/Prefabs/InventoryPanel",
                DefaultUIResourceProvider.ToResourcesPath(
                    "UI\\Module\\Inventory\\Prefabs\\InventoryPanel.prefab"));
        }

        private static void ConfigureBinding(
            EUIBinding binding,
            EUIBinding.CodePathMode pathMode,
            string classPath,
            string className)
        {
            var serializedBinding = new SerializedObject(binding);
            serializedBinding.FindProperty("codePathMode").enumValueIndex = (int)pathMode;
            serializedBinding.FindProperty("classPath").stringValue = classPath;
            serializedBinding.FindProperty("className").stringValue = className;
            serializedBinding.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion
    }
}

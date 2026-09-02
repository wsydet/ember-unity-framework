// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Ember.UI;
using Ember.UIExtension;
using Ember.UIExtension.Editor;

using NUnit.Framework;

using UnityEngine;

namespace Ember.UI.Tests
{
    /// <summary>UI 开发中心的纯逻辑 Edit Mode 测试。</summary>
    public class EUIDevelopmentCenterEditTests
    {
        #region 创建计划

        [Test]
        public void CreationPlan_ValidBusinessRequest_ShouldResolvePathsWithoutWriting()
        {
            var suffix = Guid.NewGuid().ToString("N");
            var prefabName = $"DevCenter{suffix}Panel";
            var className = $"DevCenter{suffix}Page";
            var pageName = $"DevCenter{suffix}PageDef";
            var settings = EUIBindingSettingData.LoadExistingSettings();
            Assert.IsNotNull(settings, "测试项目应提供现有 EUIBinding 设置资产。");
            var implementation = settings.LogicImplementations
                .OfType<CSharpLogicImplementationData>()
                .FirstOrDefault(item => item);
            Assert.IsNotNull(implementation, "测试项目应配置 C# 逻辑实现。");

            var expectedPrefab =
                $"{implementation.UIResourceRoot}/Module/DevCenterTests/Prefabs/{prefabName}.prefab";
            var expectedLogic =
                $"{settings.BusinessCodeRoot}/DevCenterTests/Page/{className}.cs";
            var expectedBinding =
                $"{settings.BusinessCodeRoot}/DevCenterTests/Page/{className}.Binding.cs";
            var expectedSettings =
                $"{settings.BusinessCodeRoot}/DevCenterTests/Page/{className}Settings.cs";
            var pageDefFullPath = EUIPrefabCatalogService.ToFullPath(implementation.PageDefFile);
            Assert.IsTrue(File.Exists(pageDefFullPath));
            var pageDefBefore = File.ReadAllText(pageDefFullPath);

            Assert.IsFalse(File.Exists(EUIPrefabCatalogService.ToFullPath(expectedPrefab)));
            Assert.IsFalse(File.Exists(EUIPrefabCatalogService.ToFullPath(expectedLogic)));
            Assert.IsFalse(File.Exists(EUIPrefabCatalogService.ToFullPath(expectedBinding)));
            Assert.IsFalse(File.Exists(EUIPrefabCatalogService.ToFullPath(expectedSettings)));

            var request = new EUICreationRequest
            {
                CodePathMode = EUIBinding.CodePathMode.Business,
                PrefabName = $"  {prefabName}.prefab  ",
                PageName = $"  {pageName}  ",
                ClassPath = @"  DevCenterTests\Page  ",
                ClassName = $"  {className}  ",
                PageType = PageType.Popup,
                GenerateCustomSettings = true,
            };

            Assert.IsTrue(EUICreationService.TryBuildPlan(request, out var plan, out var result),
                result.Error);
            Assert.IsTrue(plan.IsValid);
            Assert.IsTrue(result.Success);
            Assert.AreEqual("DevCenterTests/Page", plan.Request.ClassPath);
            Assert.AreEqual(prefabName, plan.Request.PrefabName);
            Assert.AreEqual(pageName, plan.Request.PageName);
            Assert.AreEqual(className, plan.Request.ClassName);
            Assert.AreEqual(expectedPrefab, plan.PrefabPath);
            Assert.AreEqual(expectedLogic, plan.LogicScriptPath);
            Assert.AreEqual(expectedBinding, plan.BindingScriptPath);
            Assert.AreEqual(expectedSettings, plan.SettingsScriptPath);
            Assert.AreEqual(implementation.PageDefFile, plan.PageDefFile);

            Assert.AreEqual(pageDefBefore, File.ReadAllText(pageDefFullPath));
            Assert.IsFalse(File.Exists(EUIPrefabCatalogService.ToFullPath(plan.PrefabPath)));
            Assert.IsFalse(File.Exists(EUIPrefabCatalogService.ToFullPath(plan.LogicScriptPath)));
            Assert.IsFalse(File.Exists(EUIPrefabCatalogService.ToFullPath(plan.BindingScriptPath)));
            Assert.IsFalse(File.Exists(EUIPrefabCatalogService.ToFullPath(plan.SettingsScriptPath)));
        }

        [Test]
        public void CreationPlan_PathTraversal_ShouldFailBeforeResolvingTargets()
        {
            var request = new EUICreationRequest
            {
                PrefabName = "TraversalPanel",
                PageName = "TraversalPage",
                ClassPath = "Inventory/../Page",
                ClassName = "TraversalPage",
            };

            Assert.IsFalse(EUICreationService.TryBuildPlan(request, out var plan, out var result));
            Assert.IsFalse(plan.IsValid);
            Assert.IsFalse(result.Success);
            StringAssert.Contains("穿越目录", result.Error);
            Assert.IsNull(plan.PrefabPath);
            Assert.IsNull(plan.LogicScriptPath);
        }

        [Test]
        public void CreationPlan_ExistingGameUIClass_ShouldFailWithoutWriting()
        {
            var suffix = Guid.NewGuid().ToString("N");
            var request = new EUICreationRequest
            {
                CodePathMode = EUIBinding.CodePathMode.Business,
                PrefabName = $"ExistingClass{suffix}Panel",
                PageName = $"ExistingClass{suffix}Page",
                ClassPath = "DevCenterTests/Page",
                ClassName = "EUIMainPage",
                PageType = PageType.MainPage,
            };

            Assert.IsFalse(EUICreationService.TryBuildPlan(request, out var plan, out var result));
            Assert.IsFalse(plan.IsValid);
            Assert.IsFalse(result.Success);
            StringAssert.Contains("Game.UI 命名空间中已存在同名类 EUIMainPage", result.Error);
            Assert.IsNull(plan.PrefabPath);
            Assert.IsNull(plan.LogicScriptPath);
        }

        [Test]
        public void CreationPlan_ClassPathContainingCs_ShouldKeepBindingTargetAligned()
        {
            var suffix = Guid.NewGuid().ToString("N");
            var request = new EUICreationRequest
            {
                CodePathMode = EUIBinding.CodePathMode.Business,
                PrefabName = $"DotPath{suffix}Panel",
                PageName = $"DotPath{suffix}Page",
                ClassPath = "Dev.cs/Page",
                ClassName = $"DotPath{suffix}Page",
                PageType = PageType.MainPage,
            };

            Assert.IsTrue(EUICreationService.TryBuildPlan(request, out var plan, out var result),
                result.Error);
            StringAssert.EndsWith(
                $"/Dev.cs/Page/DotPath{suffix}Page.cs", plan.LogicScriptPath);
            StringAssert.EndsWith(
                $"/Dev.cs/Page/DotPath{suffix}Page.Binding.cs", plan.BindingScriptPath);
        }

        [Test]
        public void CreationPlan_ReservedWindowsClassName_ShouldFailBeforeWriting()
        {
            var request = new EUICreationRequest
            {
                PrefabName = "ReservedNamePanel",
                PageName = "ReservedNamePage",
                ClassPath = "DevCenterTests/Page",
                ClassName = "CON",
            };

            Assert.IsFalse(EUICreationService.TryBuildPlan(request, out var plan, out var result));
            StringAssert.Contains("Windows 保留", result.Error);
            Assert.IsNull(plan.LogicScriptPath);
        }

        [Test]
        public void CreationPlan_ReservedWindowsPageName_ShouldRemainValidIdentifier()
        {
            var suffix = Guid.NewGuid().ToString("N");
            var request = new EUICreationRequest
            {
                PrefabName = $"ReservedPageName{suffix}Panel",
                PageName = "CON",
                ClassPath = "DevCenterTests/Page",
                ClassName = $"ReservedPageName{suffix}Page",
            };

            Assert.IsTrue(EUICreationService.TryBuildPlan(request, out var plan, out var result),
                result.Error);
            Assert.IsTrue(plan.IsValid);
        }

        #endregion

        // --------------------------------------------------------

        #region 路径与排序

        [TestCase(PageType.Background, (int)UILayer.Background)]
        [TestCase(PageType.MainPage, (int)UILayer.Normal)]
        [TestCase(PageType.Popup, (int)UILayer.Popup)]
        [TestCase(PageType.FullScreenPopup, (int)UILayer.Popup)]
        [TestCase(PageType.TopMost, (int)UILayer.TopMost)]
        [TestCase(PageType.SubPage, (int)UILayer.Normal)]
        [TestCase(PageType.FreePage, 30000)]
        [TestCase(PageType.Overlay, -1)]
        public void DefaultSortingOrder_ShouldMatchPageType(PageType pageType, int expected)
        {
            Assert.AreEqual(expected, EUIBindingEditorUtility.GetDefaultSortingOrder(pageType));
        }

        [Test]
        public void AssetPathHelpers_ShouldNormalizeAndKeepRootBoundaries()
        {
            Assert.AreEqual("Assets/Game/UI",
                EUIPrefabCatalogService.NormalizeAssetPath(@"  Assets\Game\UI\  "));
            Assert.IsNull(EUIPrefabCatalogService.NormalizeAssetPath("   "));
            Assert.AreEqual("Assets/Game/UI/GamePages.cs",
                EUIPrefabCatalogService.ResolveFrameworkPageDefFile(
                    @"Assets\Game\UI\GamePages.User.cs"));
            Assert.IsNull(EUIPrefabCatalogService.ResolveFrameworkPageDefFile(
                "Assets/Game/UI/OtherPages.cs"));

            Assert.IsTrue(EUIPrefabMaintenanceService.IsUnderRoot(
                "Assets/Game/UI/Runtime/Test.cs", "Assets/Game/UI"));
            Assert.IsTrue(EUIPrefabMaintenanceService.IsUnderRoot(
                "assets/game/ui", "Assets/Game/UI"));
            Assert.IsFalse(EUIPrefabMaintenanceService.IsUnderRoot(
                "Assets/Game/UIBackup/Test.cs", "Assets/Game/UI"));
            Assert.IsFalse(EUIPrefabMaintenanceService.IsUnderRoot(
                "Packages/com.ember/Test.cs", "Packages/com.ember"));
            Assert.IsNull(EUIPrefabCatalogService.ToFullPath(
                "Packages/com.ember/Templates~/base/Test.cs"));
        }

        [Test]
        public void FindStalePageDefs_ShouldIgnoreCommentedDefinitions()
        {
            var suffix = Guid.NewGuid().ToString("N");
            var file = Path.GetTempFileName();
            var missingPrefab = $"Assets/__EmberMissing/{suffix}.prefab";
            var actualName = $"设置页{suffix}";
            try
            {
                File.WriteAllText(file,
                    "/*\n"
                    + $"public static readonly EUIPageDef Commented{suffix} = new(\"{missingPrefab}\", UILayer.Normal, PageType.MainPage);\n"
                    + "*/\n"
                    + $"var regular = \"EUIPageDef String{suffix} = new(\\\"{missingPrefab}\\\")\";\n"
                    + "var raw = \"\"\"\n"
                    + $"public static readonly EUIPageDef Raw{suffix} = new(\"{missingPrefab}\", UILayer.Normal, PageType.MainPage);\n"
                    + "\"\"\";\n"
                    + $"public static readonly EUIPageDef {actualName} = new(\"{missingPrefab}\", UILayer.Normal, PageType.MainPage);\n");

                var stale = CSharpLogicImplementationData.FindStalePageDefsPublic(file);

                Assert.AreEqual(1, stale.Count);
                Assert.AreEqual(actualName, stale[0].Name);
            }
            finally
            {
                File.Delete(file);
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 创建配置

        [Test]
        public void ApplyCreationConfig_FullScreenPopup_ShouldApplyAllSelectedOptions()
        {
            var gameObject = new GameObject("CreationPopup");
            try
            {
                var binding = gameObject.AddComponent<EUIBinding>();
                var maskColor = new Color(0.1f, 0.2f, 0.3f, 0.4f);
                var request = new EUICreationRequest
                {
                    CodePathMode = EUIBinding.CodePathMode.Framework,
                    PrefabName = "CreationPopupPanel",
                    PageName = "CreationPopupPage",
                    ClassPath = "UI/Page",
                    ClassName = "CreationPopupPage",
                    PageType = PageType.FullScreenPopup,
                    UseUIUpdate = true,
                    UseMask = true,
                    MaskColor = maskColor,
                    ClickMaskToClose = true,
                    TransitionMode = EUIBinding.RegularTransitionMode.Animator,
                    FadeInTime = 0.45f,
                    FadeOutTime = 0.25f,
                    GenerateAutoCreateClickableMaskOverride = true,
                    GenerateOnClickMaskOverride = true,
                    GenerateCustomSettings = true,
                };

                EUIBindingEditorUtility.ApplyCreationConfig(binding, request);

                Assert.AreEqual(request.CodePathMode, binding.PathMode);
                Assert.AreEqual(request.PrefabName, binding.PrefabName);
                Assert.AreEqual(request.PageName, binding.PageName);
                Assert.AreEqual(request.ClassPath, binding.ClassPath);
                Assert.AreEqual(request.ClassName, binding.ClassName);
                Assert.IsTrue(binding.IsPage);
                Assert.AreEqual(PageType.FullScreenPopup, binding.PageType);
                Assert.IsFalse(binding.NoCodeGeneration);
                Assert.IsTrue(binding.GenerateCustomSettings);
                Assert.IsTrue(binding.UseUIUpdate);
                Assert.IsTrue(binding.UseMask);
                Assert.AreEqual(maskColor, binding.MaskColor);
                Assert.IsTrue(binding.ClickMaskToClose);
                Assert.IsTrue(binding.GenerateAutoCreateClickableMaskOverride);
                Assert.IsTrue(binding.GenerateOnClickMaskOverride);
                Assert.IsFalse(binding.UsePresetFade);
                Assert.IsFalse(binding.UseTransitionBlock);
                Assert.IsTrue(binding.UseAnimator);
                Assert.IsFalse(binding.UseCustomTransition);
                Assert.AreEqual(0.45f, binding.FadeInTime);
                Assert.AreEqual(0.25f, binding.FadeOutTime);
                Assert.IsEmpty(binding.Bindings);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ApplyCreationConfig_NonPopup_ShouldClearPopupOnlyOptions()
        {
            var gameObject = new GameObject("CreationMainPage");
            try
            {
                var binding = gameObject.AddComponent<EUIBinding>();
                var request = new EUICreationRequest
                {
                    PageType = PageType.MainPage,
                    UseMask = true,
                    ClickMaskToClose = true,
                    GenerateAutoCreateClickableMaskOverride = true,
                    GenerateOnClickMaskOverride = true,
                    TransitionMode = EUIBinding.RegularTransitionMode.CustomCode,
                };

                EUIBindingEditorUtility.ApplyCreationConfig(binding, request);

                Assert.IsFalse(binding.UseMask);
                Assert.IsFalse(binding.ClickMaskToClose);
                Assert.IsFalse(binding.GenerateAutoCreateClickableMaskOverride);
                Assert.IsFalse(binding.GenerateOnClickMaskOverride);
                Assert.IsFalse(binding.UsePresetFade);
                Assert.IsFalse(binding.UseAnimator);
                Assert.IsTrue(binding.UseCustomTransition);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CreateCustomSettings_WithGlobalSimpleNameCollision_ShouldUseGeneratedNamespaceType()
        {
            var gameObject = new GameObject("SettingsResolutionProbe");
            try
            {
                var binding = gameObject.AddComponent<EUIBinding>();
                EUIBindingEditorUtility.ApplyCreationConfig(binding, new EUICreationRequest
                {
                    ClassName = "EUISettingsResolutionProbe",
                    GenerateCustomSettings = true,
                });

                var createSettings = typeof(EUIBindingCodeGenUtility).GetMethod(
                    "TryCreateCustomSettingsAfterCompile",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.IsNotNull(createSettings);
                var arguments = new object[] { binding, null };
                Assert.IsTrue((bool)createSettings.Invoke(null, arguments), arguments[1] as string);
                Assert.IsInstanceOf<Game.UI.EUISettingsResolutionProbeSettings>(
                    binding.PageSettings);
                Assert.IsNotInstanceOf<global::EUISettingsResolutionProbeSettings>(
                    binding.PageSettings);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 运行时交互

        [TestCase(false, false, false, false, TestName = "CompleteShow_None_RestoresAnimatorInteraction")]
        [TestCase(false, false, false, true, TestName = "CompleteShow_Custom_RestoresAnimatorInteraction")]
        [TestCase(false, true, false, true, TestName = "CompleteShow_TransitionBlock_RestoresAnimatorInteraction")]
        [TestCase(true, false, false, false, TestName = "CompleteShow_PresetFade_RestoresAnimatorInteraction")]
        [TestCase(false, false, true, false, TestName = "CompleteShow_Animator_RestoresAnimatorInteraction")]
        public void CompleteShow_ShouldRestoreAnimatorCanvasGroupInteraction(
            bool usePreset, bool useBlock, bool useAnimator, bool useCustom)
        {
            var root = new GameObject("RuntimePage", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasGroup));
            try
            {
                var animatorObject = new GameObject("Animator", typeof(RectTransform),
                    typeof(Animator), typeof(CanvasGroup));
                animatorObject.transform.SetParent(root.transform, false);
                var animatorGroup = animatorObject.GetComponent<CanvasGroup>();
                animatorGroup.blocksRaycasts = false;
                animatorGroup.interactable = false;

                var page = new EUIPage(root);
                page.SetTransition(usePreset, useBlock, useAnimator, useCustom,
                    inTime: 0f, outTime: 0f);
                var completeShow = typeof(EUIPage).GetMethod("CompleteShow",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.IsNotNull(completeShow);
                completeShow.Invoke(page, null);

                Assert.IsTrue(animatorGroup.blocksRaycasts);
                Assert.IsTrue(animatorGroup.interactable);
                Assert.IsTrue(root.GetComponent<CanvasGroup>().blocksRaycasts);
                Assert.IsTrue(root.GetComponent<CanvasGroup>().interactable);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CompleteShow_Background_ShouldRemainNonBlocking()
        {
            var root = new GameObject("RuntimeBackground", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasGroup));
            try
            {
                var animatorObject = new GameObject("Animator", typeof(RectTransform),
                    typeof(Animator), typeof(CanvasGroup));
                animatorObject.transform.SetParent(root.transform, false);
                var animatorGroup = animatorObject.GetComponent<CanvasGroup>();
                animatorGroup.blocksRaycasts = false;

                var page = new EUIPage(root)
                {
                    EUIPageDef = new EUIPageDef("Assets/Test.prefab", UILayer.Background,
                        PageType.Background),
                };
                page.SetTransition(usePreset: true, useBlock: false, useAnimator: false,
                    useCustom: false, inTime: 0f, outTime: 0f);
                var completeShow = typeof(EUIPage).GetMethod("CompleteShow",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.IsNotNull(completeShow);
                completeShow.Invoke(page, null);

                Assert.IsFalse(animatorGroup.blocksRaycasts);
                Assert.IsTrue(animatorGroup.interactable);
                Assert.IsFalse(root.GetComponent<CanvasGroup>().blocksRaycasts);
                Assert.IsTrue(root.GetComponent<CanvasGroup>().interactable);

                page.OnPause();
                page.OnResume();
                Assert.IsFalse(root.GetComponent<CanvasGroup>().blocksRaycasts);

                page.HideViewOnly();
                page.RestoreViewOnly();
                Assert.IsFalse(root.GetComponent<CanvasGroup>().blocksRaycasts);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PrepareShowTransition_LoadingBlock_ShouldBlockLowerPagesWithoutInteraction()
        {
            var root = new GameObject("RuntimeLoading", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasGroup));
            try
            {
                var animatorObject = new GameObject("Animator", typeof(RectTransform),
                    typeof(Animator), typeof(CanvasGroup));
                animatorObject.transform.SetParent(root.transform, false);
                var animatorGroup = animatorObject.GetComponent<CanvasGroup>();
                animatorGroup.blocksRaycasts = false;
                animatorGroup.interactable = true;

                var page = new EUIPage(root)
                {
                    EUIPageDef = new EUIPageDef("Assets/Loading.prefab", UILayer.TopMost,
                        PageType.TopMost),
                };
                page.SetTransition(usePreset: false, useBlock: true, useAnimator: false,
                    useCustom: true, inTime: 0.3f, outTime: 0.2f);
                var prepareShow = typeof(EUIPage).GetMethod("PrepareShowTransition",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.IsNotNull(prepareShow);
                prepareShow.Invoke(page, null);

                var rootGroup = root.GetComponent<CanvasGroup>();
                Assert.IsTrue(rootGroup.blocksRaycasts);
                Assert.IsFalse(rootGroup.interactable);
                Assert.IsTrue(animatorGroup.blocksRaycasts);
                Assert.IsFalse(animatorGroup.interactable);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 目录健康状态

        [Test]
        public void CatalogHealth_NoCodeGeneration_ShouldIgnoreGeneratedArtifacts()
        {
            var entry = new EUIPrefabCatalogEntry
            {
                IsPage = true,
                NoCodeGeneration = true,
                GenerateCustomSettings = true,
                LogicScriptExists = false,
                BindingScriptExists = false,
                SettingsScriptExists = false,
                PageDefOk = false,
                MissingScriptCount = 0,
                NullBindingCount = 0,
            };

            Assert.IsTrue(entry.IsHealthy);

            entry.MissingScriptCount = 1;
            Assert.IsFalse(entry.IsHealthy, "NoCodeGeneration 仍不能忽略 Missing Script。");
            entry.MissingScriptCount = 0;
            entry.NullBindingCount = 1;
            Assert.IsFalse(entry.IsHealthy, "NoCodeGeneration 仍不能忽略空绑定引用。");
        }

        [Test]
        public void CatalogHealth_CodeGeneration_ShouldRequireScriptsSettingsAndPageDef()
        {
            var entry = new EUIPrefabCatalogEntry
            {
                IsPage = true,
                GenerateCustomSettings = true,
                LogicScriptExists = true,
                BindingScriptExists = true,
                SettingsScriptExists = true,
                PageDefOk = true,
            };

            Assert.IsTrue(entry.IsHealthy);
            entry.SettingsScriptExists = false;
            Assert.IsFalse(entry.IsHealthy);
            entry.SettingsScriptExists = true;
            entry.PageDefOk = false;
            Assert.IsFalse(entry.IsHealthy);
        }

        [Test]
        public void DeletePlan_NoCodeGeneration_ShouldNeverIncludeScripts()
        {
            var snapshot = EUIPrefabCatalogService.Scan();
            Assert.IsTrue(snapshot.IsConfigured, snapshot.Error);
            Assert.IsNotEmpty(snapshot.Entries);
            var existing = snapshot.Entries[0];
            var entry = new EUIPrefabCatalogEntry
            {
                PrefabPath = existing.PrefabPath,
                IsPage = false,
                NoCodeGeneration = true,
                LogicScriptPath = existing.LogicScriptPath,
                BindingScriptPath = existing.BindingScriptPath,
                SettingsScriptPath = existing.SettingsScriptPath,
                GenerateCustomSettings = true,
            };

            var plan = EUIPrefabMaintenanceService.BuildDeletePlan(snapshot, entry);

            Assert.IsTrue(plan.CanExecute, string.Join("\n", plan.Errors));
            CollectionAssert.AreEqual(new[] { existing.PrefabPath }, plan.AssetPaths);
        }

        [Test]
        public void EmptyLeafCheck_ShouldProtectSafeAreaSemanticNodes()
        {
            var root = new GameObject("Root");
            try
            {
                var ordinaryLeaf = new GameObject("OrdinaryLeaf");
                ordinaryLeaf.transform.SetParent(root.transform, false);
                Assert.IsTrue(EUIPrefabCatalogService.IsDeletableEmptyLeaf(
                    root, ordinaryLeaf.transform));

                var safeArea = new GameObject("EUISafeArea", typeof(RectTransform), typeof(EUISafeArea));
                safeArea.transform.SetParent(root.transform, false);
                var semanticLeaf = new GameObject("Center", typeof(RectTransform));
                semanticLeaf.transform.SetParent(safeArea.transform, false);
                Assert.IsFalse(EUIPrefabCatalogService.IsDeletableEmptyLeaf(
                    root, semanticLeaf.transform));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        #endregion
    }
}

/// <summary>用于验证 Settings 解析不会误选全局命名空间中的同名类型。</summary>
[Serializable]
public sealed class EUISettingsResolutionProbeSettings
{
}

namespace Game.UI
{
    /// <summary>代表 UI 代码生成器的预期 Settings 命名空间。</summary>
    [Serializable]
    public sealed class EUISettingsResolutionProbeSettings
    {
    }
}

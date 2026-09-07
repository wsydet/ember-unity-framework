# Odin 面板源码清单

最后扫描：2026-09-07。以下是项目维护的 Odin 使用点，来源为当前源码中的 `Sirenix.OdinInspector` 引用，不代表每一项面板已经实测。第三方包、模板快照和 UniTask 自带编辑器不在此清单内。

风格规范见 [Odin 使用注意事项](odin-usage-notes.md)。普通纯 C# Manager 没有独立 Unity Inspector，不应仅为“统一面板”添加无效属性。原 `OdinInspectorDemo.cs` 已不存在，参考当前 GameLauncher、EmberBaseSO、EUIBinding 和 EmberSceneMapping。

| 源文件 | 所属区域 |
|---|---|
| [AnimationProperty.cs](../../Packages/com.ember/UIExtension/Runtime/Behaviour/AnimationProperty.cs) | UIExtension / Runtime |
| [EUIBinding.cs](../../Packages/com.ember/UIExtension/Runtime/EUIBinding.cs) | UIExtension / Runtime |
| [EUIBindingListDrawer.cs](../../Packages/com.ember/UIExtension/Editor/EUIBindingListDrawer.cs) | UIExtension / Editor |
| [EUIBlockCurves.cs](../../Packages/com.ember/UIExtension/Runtime/Components/EUIBlockCurves.cs) | UIExtension / Runtime |
| [EUIButtonEx.cs](../../Packages/com.ember/UIExtension/Runtime/UIExt/EUIButtonEx.cs) | UIExtension / Runtime |
| [EUICircleImage.cs](../../Packages/com.ember/UIExtension/Runtime/Behaviour/EUICircleImage.cs) | UIExtension / Runtime |
| [EUIEventTriggerListener.cs](../../Packages/com.ember/UIExtension/Runtime/UIExt/EUIEventTriggerListener.cs) | UIExtension / Runtime |
| [EUIGradient.cs](../../Packages/com.ember/UIExtension/Runtime/UIExt/EUIGradient.cs) | UIExtension / Runtime |
| [EUIGraphicAnimation.cs](../../Packages/com.ember/UIExtension/Runtime/Behaviour/EUIGraphicAnimation.cs) | UIExtension / Runtime |
| [EUIImageEx.cs](../../Packages/com.ember/UIExtension/Runtime/UIExt/EUIImageEx.cs) | UIExtension / Runtime |
| [EUIMeshOrder.cs](../../Packages/com.ember/UIExtension/Runtime/UIExt/EUIMeshOrder.cs) | UIExtension / Runtime |
| [EUIRoundedImageModifier.cs](../../Packages/com.ember/UIExtension/Runtime/Behaviour/EUIRoundedImageModifier.cs) | UIExtension / Runtime |
| [EUISafeArea.cs](../../Packages/com.ember/UIExtension/Runtime/SafeArea/EUISafeArea.cs) | UIExtension / Runtime |
| [EUIToggleEx.cs](../../Packages/com.ember/UIExtension/Runtime/UIExt/EUIToggleEx.cs) | UIExtension / Runtime |
| [EUITransitionBlock.cs](../../Packages/com.ember/UIExtension/Runtime/Components/EUITransitionBlock.cs) | UIExtension / Runtime |
| [EmberBaseSO.cs](../../Packages/com.ember/Basic/Runtime/Base/EmberBaseSO.cs) | Basic / Runtime |
| [EmberDebugConfigEditor.cs](../../Packages/com.ember/Basic/Editor/EmberDebugConfigEditor.cs) | Basic / Editor |
| [EmberDebugConfigSO.cs](../../Packages/com.ember/Basic/Runtime/Debug/EmberDebugConfigSO.cs) | Basic / Runtime |
| [EmberSceneField.cs](../../Packages/com.ember/Core/Runtime/EmberSceneField.cs) | Core / Runtime |
| [EmberSceneMapping.cs](../../Packages/com.ember/Core/Editor/EmberSceneMapping.cs) | Core / Editor |
| [EmberUPMManager.cs](../../Packages/com.ember/UPMManager/Editor/EmberUPMManager.cs) | UPMManager / Editor |
| [GameLauncher.cs](../../Packages/com.ember/Core/Runtime/GameLauncher.cs) | Core / Runtime |
| [OdinIntegrationTest.cs](../../Packages/com.ember/FrameworkTools/Editor/OdinIntegrationTest.cs) | FrameworkTools / Editor |
| [RelativeCanvasOrder.cs](../../Packages/com.ember/UIExtension/Runtime/UIExt/RelativeCanvasOrder.cs) | UIExtension / Runtime |
| [EUIBootSplash.cs](../../Assets/Game/UI/EUIBootSplash.cs) | 业务 / UI |
| [PlayerControlBounds.cs](../../Packages/com.ember/Templates~/source3d-2p5d/Assets/Game/Module/PlayerControl/PlayerControlBounds.cs) | source3d-2p5d 模板 / Module |
| [PlayerControlSettings.cs](../../Packages/com.ember/Templates~/source3d-2p5d/Assets/Game/Module/PlayerControl/PlayerControlSettings.cs) | source3d-2p5d 模板 / Module |
| [PlayerControlSceneBinding.cs](../../Packages/com.ember/Templates~/source3d-2p5d/Assets/Game/Module/PlayerControl/PlayerControlSceneBinding.cs) | source3d-2p5d 模板 / Module |
| [SceneUIObject.cs](../../Packages/com.ember/Templates~/source3d-2p5d/Assets/Game/Module/SceneUI/SceneUIObject.cs) | source3d-2p5d 模板 / Module |

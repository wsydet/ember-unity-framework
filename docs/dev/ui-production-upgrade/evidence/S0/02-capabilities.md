# S0 有效能力与正式入口

源码读取与本轮已加载程序集反射共同核验，见 [validation.json](validation.json)。存在有效类型不等于全部特性已经行为验收。

| 能力 | 当前事实与来源 | 后续边界 |
| --- | --- | --- |
| 文本 | UIExtension/Runtime/UIExt/TMPEx.cs，有效 TextMeshProUGUI 派生；字体皮肤、文本源、多语言已有实现 | S1 复用，不再造 TMP 包装 |
| 按钮 | EUIButtonEx.cs，有效 Button 派生；Label、状态节点、附加图形、IEUIExposedChildProvider | 自定义 EnableState 与 interactable 不能混为一谈 |
| Image | EUIImageEx.cs，有效 Image 派生 | S2 检查实际拉伸与 Sprite 设置 |
| Toggle | EUIToggleEx.cs / EUIToggleGroupEx.cs，有效；on/off/disabled 与 Label | 状态行为需 S1/S2 专项验收 |
| 进度 | Components/EUIProgressBar.cs 是有效逻辑包装，支持 Slider 或 fill Image | 不等于公共 Progress Prefab 已交付 |
| Binding | EUIBinding.cs 有角色、模式、框架标记、绑定、页面类型、基类 GUID | 已有 Editor 快照不包含全部契约字段，S1 要补齐而非视作完整导出 |
| 固定嵌套 | EUIBindingBridge.PopulateControlMap 的 UILogic 路径创建子逻辑并关联父级 | S1 验证与工厂路径的互斥所有权 |
| 动态 Item | EUIItemFactory.TryCreate(instance, out item, out error) 已有效；EUIItem 有 BeginUse/Show/Hide/ResetForPool/Dispose | 工厂不实例化/销毁 GameObject，实例归宿主；S3 完成容器 |
| 安全区 | EUISafeArea.cs 与现有嵌套 Prefab 有效 | 保留 Animator/EUISafeArea/Center 等现有定位 |
| UIContainer、TabLoader、PagePreloader、TMPMarquee | 对应文件仅注释参考，程序集没有这些类型 | 不能作为可调用能力，不整批启用 |
| 其他参考文件 | AdvancedText、AdvancedTextImage、HrefText、ImageFont、MergedImage、UIParticleOrder 等同样没有有效类型声明 | 按具体阶段需求逐项取舍 |

以上相对源码位置均基于 `Packages/com.ember/UIExtension/Runtime`；EUIItem 位于 `Packages/com.ember/UI/Runtime/EUIItem.cs`。

正式制作菜单为 `Ember/UI/UI 开发中心`（EUIPrefabManagerWindow）。项目模板菜单为 `Ember/项目中心`（EmberSetupWindow）。可复用 API：

- `EUICreationService.TryBuildPlan` / `Create`：预检与创建；当前请求 PrefabName 默认 `NewPanel`，S1 必须修正。
- `EUIBindingEditorUtility.GetBindingSnapshot`、`SetPageInfo`、`SetBindings`、`CollectBindings`、`SetBaseBinding`、`ValidateBinding`。
- `EUIBindingEditorUtility.GenerateOrUpdatePageDef`：注册维护。
- `EUIBindingCodeGenUtility.TryRegenerateCode`：正式再生成；保留用户区，不手改 Binding 文件。
- `EmberProjectSetup.GetEditingTemplate`、`GetTemplates`、`SaveTemplate`：身份、模板和普通保存。最后一项本轮未调用。

本轮执行了六个正式页面的 `ValidateBinding`，全部 Issues=[]、HasError=false。未运行 Unity Test Runner；S0 使用真实 Play 导航与资产校验。后续可按改动选取 `EUIDevelopmentCenterEditTests`、`EUIDeletionProtectionEditTests`、`EUICatalogLayoutEditTests`、`EUIModuleTemplateEditTests` 及 `EUITransitionBlockPreviewTests`，不能据本轮盘点宣称这些测试已通过。

Burner 本轮未进一步扫描；S0 所需现场已经由 Ember 自身源码、资产和运行确认。未修改 Burner，也未拷贝其产品内容。

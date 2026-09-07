# UI 与 UIExtension 源码阅读路线

> 更新：2026-09-07。读取当前 `Packages/com.ember`，旧 Burner 迁移步骤已经结束。

先按 [UI 开发参考](../user/UI开发参考.md) 创建一个 Page 和一个 Item，再按以下路线阅读。

| 顺序 | 源码 | 关注点 |
|---|---|---|
| 1 | `UI/Runtime/EUIEnums.cs`、`EUIPageDef.cs`、`IEUIView.cs` | 页面类型、层级与生命周期契约 |
| 2 | `UI/Runtime/EUILogic.cs`、`EUIPage.cs`、`EUIItem.cs` | 业务逻辑、页面包装和宿主 Item 的差别 |
| 3 | `UI/Runtime/EUIManager.cs`、`EUIPageContext.cs` | 打开/关闭入口、主页面与弹窗栈、状态所有权 |
| 4 | `UI/Runtime/EUIViewEngine.cs` | 资源加载、分帧队列、过渡、遮罩与 Update |
| 5 | `UIExtension/Runtime/EUIBinding.cs`、`EUIBindingBridge.cs`、`EUIItemFactory.cs` | Binding 到 Logic、Page/Item 的桥接 |
| 6 | `UIExtension/Editor/Settings/CSharpLogicImplementationData.cs`、`Editor/Pages/EUIBindingEditorUtility.cs` | 生成路径、Framework/Business 所有权、Binding 和可选用户钩子 |
| 7 | `UIExtension/Editor/Pages/EUICreationService.cs`、`EUIPrefabCatalogService.cs`、`EUIModuleTemplateService.cs` | UI 开发中心的预检、创建、维护和模块目录同步 |
| 8 | `SceneUI/Runtime/Integration/EmberSceneUIModuleBase.cs`、`EUIItemSceneUIView.cs` | 实际业务 Module 如何持有页面与池化 Item |

表中路径相对于 `Packages/com.ember`；按需查看 `UIExtension/Runtime/UIExt`、`Behaviour`、`Components` 和 `SafeArea`。
部分历史高级组件源码仍可能是注释或未激活实现，文件存在不表示已可用。

每一步都跟踪一次真实调用：生成控件字段 → 打开 → 暂停/恢复 → 关闭 → 再打开 → 销毁。
特别检查事件清理、异步取消、主页面所属状态、Item 回池与 Dispose 的区别。
API 细节见 [EUI 参考](eui-reference.md)，回归见 [绑定测试](uiextension-test-plan.md)。

# UI — 页面与宿主 UI 单元

源码：[UI/Runtime](../../UI/Runtime)、[UIExtension](../../UIExtension)。
`EUIManager` 是业务入口，`EUIViewEngine` 执行资源加载、排序、过渡和生命周期。两者均为
`EmberSingleton` + `IEmberManager`，不是挂在预制体上的 MonoBehaviour。

## 打开与关闭页面

先用 UI 开发中心创建 Page、生成 Binding 和 `EUIPageDef`，业务使用生成的 `GamePages` 常量：

```csharp
EUIManager.Instance.ShowMainPage(GamePages.EUIMainPage);
// 在拥有该页面的状态退出时关闭
EUIManager.Instance.ClosePageByDef(GamePages.EUIMainPage);
```

| 页面角色 | 入口与行为 |
|---|---|
| MainPage | `ShowMainPage` 入主页面栈，暂停旧页；状态退出负责关闭自己拥有的页面 |
| Popup / FullScreenPopup | `ShowPopup`；后者同时隐藏下层，遮罩由配置决定 |
| TopMost | `ShowTopMost`，用于 Loading 等 |
| SubPage | `ShowSubPage(def, parentPage, ...)`；父关子关 |
| Overlay / FreePage | `ShowOverlay` / `ShowFreePage`，固定排序值 |
| Background | `SetBackground` / `SetBackgroundAsync`，单槽位且不拦射线 |

设置页由状态机 Push/Pop 管理，按 [UI 开发参考](../../../../docs/user/UI开发参考.md) 的状态路由打开，避免只关闭页面却未退出状态。

其他入口：`PreloadPage`、`ClosePage(page, returnValue)`、`CloseTopPopup`、`CloseAllPopups`、
`GetReturnValue`、`HidePageViewOnly` / `ShowPageViewOnly`。预加载完成回调仍需处理，不保证调用瞬间完成。

`EUIPageDef` 构造参数为 `prefabPath, layer, pageType`，可选 `overlaySortingOrder`、`freePageSortingOrder`。
UILayer 当前为 Background=0、Normal=1000、Popup=2000、TopMost=25000；实际栈排序由 PageContext 管理。

## Page、Logic、Binding

- `EUIPage` 是普通 C# 页面包装，持有实例与页面状态；`IEUIView` 描述该层契约。
- `EUILogic` 是业务逻辑基类；生成的 `.Binding.cs` 实现强类型控件绑定，不能手工改。
- Page 预制体根挂 `EUIBinding`，桥接层创建 Logic 并填充 `ControlMap`，无需挂一个实现 IEUIView 的业务 MonoBehaviour。
- 生命周期包括 OnBeginLoad、OnBind、OnResetDefault、OnInit、OnOpen、OnShow、OnHide、OnClose、OnReset、OnDispose。
- 普通过渡选择无、预设渐变、Animator、自定义 UniTask 之一；自定义覆写 `OnCustomEnter/OnCustomExit`。
- Framework 模式业务增量写到 `XxxUser` 钩子；使用 UIUpdate 由 Binding 配置并重新生成。

## Item

`EUIBindingRole.Item` 由宿主页面/列表/业务模块持有，不生成 PageDef，不进入 GamePages，不通过 EUIManager 打开。
`EUIItemFactory` 根据实例根 Binding 创建 Item 与 Logic。Item 逻辑的 `Page` 为 null，`Item` 指向宿主包装。
Item 没有独立 Canvas/页面排序，复用宿主的渲染环境；临时隐藏与回池、最终 Dispose 是不同生命周期。
SceneUI 的气泡就是 Item，见 [SceneUI 接入](../scene-ui/README.md)。

开发仓库还提供 [完整 UI 使用规范](../../../../docs/user/UI开发参考.md) 与 [EUI API 参考](../../../../docs/dev/eui-reference.md)。

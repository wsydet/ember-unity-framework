# Ember 与 Burner UI 的设计取舍

最后核对：2026-09-07。Burner 信息来自 2026-08-11 的本地研究记录，未重新核验外部项目；Ember 列以当前工作区源码为依据，不表示所有功能已经在 Unity 实测。

## 当前对应关系

| 关注点 | Burner 研究记录 | 当前 Ember |
|---|---|---|
| 分层 | GameUIManager → BurnerUIManager | `EUIManager` 路由与栈 → `EUIViewEngine` 页面加载、更新、过渡；均为纯 C# Manager |
| 页面/逻辑 | GamePage / GameUILogic / GameUIBinding | `EUIPage` / `EUILogic` / `EUIBinding`；业务继承逻辑类 |
| 定义 | PageDefineDictionary | 开发中心生成/同步 `GamePages` 的 `EUIPageDef` |
| 类型 | PageFlags 组合 | 互斥 `PageType`：Background、MainPage、Popup、FullScreenPopup、TopMost、SubPage、Overlay、FreePage |
| 排序 | 主栈、弹窗、子页、固定排序页 | 主栈与 Popup 分组、TopMost、SubPage 相对排序、Overlay/FreePage 独立排序均有实现 |
| 可见性 | Canvas、射线开关或 planeDistance | 页面渲染开关与交互状态统一管理；全屏遮挡、恢复下层、仅隐藏 View 有不同入口 |
| 预加载 | DoPreload | `PreloadPage`、`OnPreload`；业务 Init/Open 延后到实际打开 |
| 过渡 | 协程/Animator 钩子 | `EUILogic` 生命周期 + 过渡 Handler；普通页面四种模式，Loading 使用专用阶段流程 |
| 订阅清理 | AddEvent / RemoveAllEvents | `TrackDisposable` 与 `EmberEventGroup` |
| 适配 | CanvasScaler、SafeArea | 当前适配与 SafeArea 已实现，入口及验证见 UI 文档 |
| 调试 | 栈/队列/计时 | UI 开发中心和当前调试 API；能力以对应源码为准 |
| 场景气泡 | HUD 等业务扩展 | 独立 SceneUI 引擎 + 可选业务 Module + EUI Item |

旧对比中的 `EUIPageRouter`、`EUIManager : EmberMonoSingleton`、页面子类协程动画，以及“SubPage/Overlay 排序、预加载、SafeArea 未实现”等结论已不适用。

## 保留的演进方向

- 多组 MainPage 栈、复杂分帧初始化、页面音乐、业务状态监听与全局点击统计，应以实际项目需求决定是否扩展。
- 背景模糊、截图、粒子播放托管和场景 Volume/雾效控制仍需独立设计，不能把 Burner 的业务能力直接视为 Ember 缺陷。
- `ResourcesProvider` 当前同步加载资源；UI 层的异步回调/UniTask 过渡不等于资源加载已经完全异步。
- 不再用无测试依据的“覆盖率 100%”衡量对齐程度。每个功能须分别确认源码、配置和运行时验收。

## 维护入口

- [UI 开发参考](../user/UI开发参考.md)：业务开发规范。
- [EUI API 与生命周期](eui-reference.md)：当前入口、控件与回调。
- [框架进度](framework-progress.md)：统一维护未完成事项。
- [框架测试清单](framework-test-checklist.md)：回归入口。
- [Burner 架构历史研究](burner-architecture.md)：保留设计来源。

# UI — 用户界面管理

## 概述

基于层级栈的界面管理系统。每个界面实现 `IEUIView` 接口获得完整生命周期，
通过 `EUIPageDef` 注册页面元数据，由 `EUIManager` 管理多层级 Push/Pop。

## 文件清单

| 角色 | 路径 |
|------|------|
| 主逻辑入口 | `Runtime/EUIManager.cs` |
| 核心接口 | `Runtime/IEUIView.cs` |
| 页面定义 | `Runtime/EUIPageDef.cs` |

## 依赖

| 依赖 | 类型 | 说明 |
|------|------|------|
| `Ember.Core` | 框架模块 | EmberMonoSingleton |
| `Ember.Resource` | 框架模块 | EmberResourceManager（异步加载预制体） |

## 公开 API

### EUIManager — 界面管理器

继承 EmberMonoSingleton。每个层级独立维护界面栈。

| 方法 | 说明 |
|------|------|
| `Push(EUIPageDef page, object args)` | 加载预制体 → 实例化 → OnOpen → 入栈。暂停原栈顶 |
| `Pop(int layer)` | 关闭栈顶：OnClose → Destroy → 恢复新栈顶 |
| `CloseAll(int layer)` | 关闭指定层级所有界面 |
| `CloseAll()` | 关闭所有层级所有界面 |
| `GetTopView(int layer) → IEUIView` | 获取栈顶界面 |
| `GetCount(int layer) → int` | 界面数量 |
| `HasView(int layer) → bool` | 是否有界面 |

### IEUIView — 界面生命周期

| 方法 | 说明 |
|------|------|
| `OnOpen(object args)` | 首次打开（预制体实例化后），绑定控件、注册事件 |
| `OnClose()` | 关闭时调用，注销事件、释放引用 |
| `OnPause()` | 被其他界面 Push 覆盖时 |
| `OnResume()` | 覆盖界面 Pop 后恢复时 |

### EUIPageDef — 页面定义

| 成员 | 说明 |
|------|------|
| `PrefabPath` | 预制体资源路径 |
| `Layer` | 所属层级值 |

### UILayer — 层级枚举

| 值 | 数值 | 说明 |
|----|------|------|
| Background | 0 | 背景 |
| Normal | 100 | 普通界面 |
| Popup | 200 | 弹窗 |
| TopMost | 300 | 顶层（Loading 等） |

## 主流程

**Push：** `Push(page, args)` → 初始化检查 → EnsureLayerRoot → PauseTopView → ResourceManager.LoadAssetAsync → Instantiate → GetComponent<IEUIView> → 无组件则 Destroy → stack.Push → OnOpen

**Pop：** `Pop(layer)` → TryPop → OnClose → DestroyView → ResumeTopView

## 约束与陷阱

| 类别 | 说明 |
|------|------|
| 异步 Push | Push 走 ResourceManager 异步加载预制体，连续快速 Push 需自行防重入 |
| 预制体要求 | 预制体根节点必须有实现 IEUIView 的 MonoBehaviour 组件 |
| PrefabPath | null 时构造函数抛 ArgumentNullException |

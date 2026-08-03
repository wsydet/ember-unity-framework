# Core.Editor — 编辑器工具

## 概述

Core 模块的编辑器工具集：自动同步 Build Settings 场景列表、Debug 配置 SO 自动创建、
场景快速打开器、Odin 面板。

## 文件清单

| 角色 | 路径 |
|------|------|
| Build Settings 场景同步 + Play Mode 场景管理 | `FrameworkSceneBootstrapper.cs` |
| Debug 配置 SO 自动创建 | `Debug/EmberDebugConfigCreator.cs` |
| Debug 配置 Odin 面板 | `Debug/EmberDebugConfigEditor.cs` |

## 公开 API

### FrameworkSceneBootstrapper — Build Settings 场景同步

`[InitializeOnLoad]` 静态类，编译完成时自动同步。

- FrameworkScene 强制排在 Build Settings 首位
- `Assets/Game/Scenes/` 下所有 .unity 文件自动加入
- Scenes 文件夹文件变更时自动触发
- 进入 Play Mode 前保存并关闭多余场景，退出后恢复
- 菜单：`Ember/跳转到 FrameworkScene (Ctrl+Shift+F)`

### EmberDebugConfigCreator — Debug 配置自动创建

`[InitializeOnLoad]`，启动时检查 `EmberDebugConfig.asset` 是否存在，不存在则自动创建并预填框架标签。

### EmberDebugConfigEditor — Debug 配置 Odin 面板

继承 OdinEditor，在面板顶部添加三个批量操作按钮：全部开启、全部关闭、清理空项。

---

此外，Core.Editor 命名空间下还有以下跨模块共享的类型（文件位于 `Assets/Ember/Editor/`）：

### EmberSceneMapping（SO） — 状态↔场景映射表

| 成员 | 说明 |
|------|------|
| `frameworkScene` | FrameworkScene 引用（SceneAsset） |
| `entries` | 状态→场景条目列表 |
| `PopulateFromStates()` | 扫描所有 EmberGameState 子类，自动匹配同名场景 |
| `SyncNewStates()` | 同步新增状态（保留已有手动赋值） |

### EmberSceneQuickOpener — 快速场景打开器

EditorWindow，菜单 `Ember/快速打开场景`。主场景互斥选择 + 叠加场景多选 + 一键打开。

### FrameworkSceneToolbarButton — Toolbar 按钮

- 左侧：指示/跳转 FrameworkScene（`MainToolbarElement`）
- 右侧：快速打开场景按钮
- 菜单：`Ember/跳转到 FrameworkScene (Ctrl+Shift+F)`

### OdinIntegrationTest — Odin 集成测试

菜单 `Ember/Test/Run Odin Integration Test`。检测 Odin Inspector 程序集、关键类型、属性可用性。

## 约束与陷阱

| 类别 | 说明 |
|------|------|
| Build Settings | 场景同步仅写入 EditorBuildSettings，不修改 .asset 文件 |
| Play Mode | 进入/退出 Play Mode 时自动保存/恢复场景列表（通过 SessionState） |
| SO 自动创建 | DebugConfig 和 SceneMapping 的 SO 会在首次编译后自动创建，无需手动操作 |

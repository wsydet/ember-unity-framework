# Editor — 编辑器工具集

## 概述

Ember 框架的顶层编辑器工具：状态↔场景映射表、快速场景打开器、Toolbar 按钮、Odin 集成测试。

## 文件清单

| 角色 | 路径 |
|------|------|
| 状态↔场景映射 SO | `EmberSceneMapping.cs` |
| 映射 SO 自动创建 | `EmberSceneMappingCreator.cs` |
| 快速场景打开器 | `EmberSceneQuickOpener.cs` |
| Toolbar 按钮 | `FrameworkSceneToolbarButton.cs` |
| Odin 集成测试 | `OdinIntegrationTest.cs` |

## 依赖

| 依赖 | 类型 | 说明 |
|------|------|------|
| `Ember.Core` | 框架模块 | EmberBaseSO、EmberSceneField、EmberGameState、EmberDebug 等 |
| `Sirenix.OdinInspector` | 第三方 | Odin 面板属性、OdinEditor |
| `UnityEditor` | 引擎 | EditorWindow、EditorBuildSettings、AssetDatabase 等 |

## 公开 API

### EmberSceneMapping — 状态↔场景映射表

`[CreateAssetMenu]` ScriptableObject（继承 EmberBaseSO）。自动创建路径：`Assets/Ember/Editor/Resources/EmberSceneMapping.asset`

| 成员 | 说明 |
|------|------|
| `frameworkScene` | FrameworkScene 引用（SceneAsset） |
| `entries` | `List<StateSceneEntry>` 状态→场景条目 |
| `PopulateFromStates()` | 扫描所有 EmberGameState 子类，自动匹配同名场景 |
| `SyncNewStates()` | 同步新增状态（保留已有手动赋值） |

### EmberSceneQuickOpener — 快速场景打开器

EditorWindow。菜单 `Ember/快速打开场景`。主场景互斥选择（Toolbar）+ 叠加场景多选（Toggle）+ 一键打开。

### FrameworkSceneToolbarButton — Toolbar 按钮

`[InitializeOnLoad]` 静态类。

- 左侧 `MainToolbarElement`：指示当前是否在 FrameworkScene + 点击跳转
- 右侧 `MainToolbarElement`：快速打开场景按钮
- 菜单：`Ember/跳转到 FrameworkScene (Ctrl+Shift+F)`

### OdinIntegrationTest — Odin 集成测试

菜单 `Ember/Test/Run Odin Integration Test`。检测：Sirenix 程序集加载、关键类型解析、OdinMenuEditorWindow 可用性、属性使用验证。

所有测试日志通过 EmberDebug 输出。

## 主流程

**场景映射自动创建：** 编译完成 → `EmberSceneMappingCreator` 检查 SO 是否存在 → 不存在则创建 + PopulateFromStates → 存在则 SyncNewStates

**快速打开场景：** 选择主场景 + 勾选叠加场景 → 打开 FrameworkScene(Single) → Additive 加载主场景和叠加场景

## 约束与陷阱

| 类别 | 说明 |
|------|------|
| SO 位置 | EmberSceneMapping.asset 自动创建在 `Assets/Ember/Editor/Resources/` |
| 场景匹配规则 | 状态名去掉 "State" 后缀 + "Scene" 后缀 = 场景名（如 GameplayState → GameplayScene） |
| InitState | 在场景映射表中跳过（无用户场景） |

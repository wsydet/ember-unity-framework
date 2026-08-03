# Debug — 增强日志系统

## 概述

EmberDebug 提供带彩色标签、按类过滤、全局开关、来源定位的增强日志。
两级分层标签（Parent.Child），子标签继承父级颜色。
通过 EmberDebugConfigSO 在 Inspector 中可视化操作。

## 文件清单

| 角色 | 路径 |
|------|------|
| 增强日志 | `EmberDebug.cs` |
| 日志配置 SO | `EmberDebugConfigSO.cs` |
| 标签常量 + 颜色 + 级别颜色 | `EmberLogPresets.cs` |

## 公开 API

### EmberDebug — 日志工具

静态类，带 [DebuggerStepThrough] 避免调试时步入。每条日志附 (at path:line)。

| 方法 | 颜色 | 说明 |
|------|------|------|
| `Log(tag, msg)` | 白色 | 常规信息 |
| `LogInit(tag, msg)` | 绿色 | 系统初始化、组件注册 |
| `LogEvent(tag, msg)` | 紫色 | 事件播报、状态切换 |
| `LogCleanup(tag, msg)` | 灰色 | 资源释放、模块卸载 |
| `LogShutdown(tag, msg)` | 淡紫色 | 框架退出、最终清理 |
| `LogWarning(tag, msg)` | 橙色 | 警告（受 GlobalOpen 和标签过滤影响） |
| `LogError(tag, msg)` | 红色 | 错误（始终输出，不受过滤控制） |
| `LogException(tag, ex)` | 红色 | 异常（始终输出） |

**过滤控制：**

| 方法 | 说明 |
|------|------|
| `Enable(tag)` | 开启指定标签日志 |
| `Disable(tag)` | 关闭指定标签日志 |
| `SetColor(tag, Color)` | 设置标签专属颜色 |
| `IsEnabled(tag) → bool` | 标签是否允许打印 |
| `GlobalOpen` (属性) | 全局开关。关闭后所有非 Error 日志静默 |
| `LoadConfig()` | 从 Resources 加载 SO 配置。首次 Log 时自动延迟加载 |

### LogTags — 标签常量表

两级分层：父标签可一键关闭所有子标签。

| 父标签 | 子标签 |
|--------|--------|
| `EmberCore` | EventBus, ServiceLocator, Singleton, ObjectPool, ManagerCollector, UpdateManager, StateMachine, GameLauncher, CameraManager |
| `EmberResource` | Manager, Provider |
| `EmberUI` | Manager |
| `EmberScene` | Manager |
| `EmberAudio` | Manager |
| `EmberInput` | Manager |
| `Game` | — |

### LogTagColors — 标签颜色

| 标签 | 颜色 |
|------|------|
| EmberCore | 绿色 (0.25, 0.85, 0.40) |
| EmberResource | 蓝色 (0.42, 0.65, 0.85) |
| EmberUI | 橙色 (0.90, 0.55, 0.30) |
| EmberScene | 紫色 (0.65, 0.50, 0.85) |
| EmberAudio | 金色 (0.90, 0.75, 0.25) |
| EmberInput | 青色 (0.45, 0.80, 0.80) |
| Game | 粉色 (0.80, 0.40, 0.60) |

子标签自动继承父标签颜色。非预定义标签通过 hash 生成稳定颜色。

### EmberDebugConfigSO — 日志配置

继承 EmberBaseSO，存放在 `Assets/Ember/Core/Runtime/Resources/EmberDebugConfig.asset`。

| 字段 | 说明 |
|------|------|
| `globalOpen` | 全局开关 |
| `autoCollect` | 新标签自动收集到 SO |
| `frameworkEntries` | 框架标签列表（颜色预锁不可改） |
| `userEntries` | 用户标签列表（自由修改） |

| 方法 | 说明 |
|------|------|
| `GetOrCreate(className)` | 查找或创建标签条目 |
| `TryGet(className, out entry)` | 查找标签条目 |
| `EnableAll()` | 开启所有标签 |
| `DisableAll()` | 关闭所有标签 |
| `CleanEmpty()` | 清理空项 |
| `PopulateFrameworkEntries()` | 预填充框架标签（SO 创建时调用一次） |

## 主流程

**日志输出：** `Log(tag, msg)` → `CanLog(tag)` → 全局开关检查 → IsEnabled(从SO读或回退缓存) → 父级检查 → FormatMsg(颜色+文件定位) → `Debug.Log`

**自动收集：** 首次使用时 GetOrCreate → hash颜色 → 编辑模式下若 autoCollect，自动写入 SO

## 约束与陷阱

| 类别 | 说明 |
|------|------|
| 延迟加载 | 首次调用 Log 时自动从 Resources 加载配置，无需手动 LoadConfig |
| 父标签过滤 | 父标签关闭 → 所有子标签都静默 |
| 代码禁用持久化 | Disable/Enable 在编辑模式下同步更新 SO（EditorUtility.SetDirty），运行时不持久化 |
| Error 不受控 | LogError/LogException 始终输出，不受 GlobalOpen 和标签过滤影响 |

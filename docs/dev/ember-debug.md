# EmberDebug 日志系统

> 框架统一日志工具，替代裸 `Debug.Log`。
> 特性：彩色标签 + 消息级别分色 + 两级标签过滤 + SO 可视化配置 + Console 双击跳转。

---

## 一、快速开始

```csharp
using Ember.Core;

// 1. 定义标签（用 LogTags 中的常量）
private const string TAG = LogTags.CoreServiceLocator;

// 2. 打日志
EmberDebug.Log(TAG, "普通消息");
EmberDebug.LogInit(TAG, "初始化完成");
EmberDebug.LogEvent(TAG, "事件播报");
EmberDebug.LogCleanup(TAG, "清理资源");
EmberDebug.LogWarning(TAG, "异常但可恢复");
EmberDebug.LogError(TAG, "错误");

// 3. 过滤控制
EmberDebug.Disable(LogTags.Audio);       // 关掉所有音频日志
EmberDebug.Disable(LogTags.CoreEventBus); // 只关 EventBus 日志
EmberDebug.GlobalOpen = false;           // 全关（Error 除外）
```

---

## 二、消息级别 & 颜色

| 方法 | 文字颜色 | Console 背景 | 用途 | 受全局开关影响 |
|------|----------|-------------|------|--------------|
| `Log` | 白色 | 无 | 普通信息 | ✅ |
| `LogInit` | 绿色 | 无 | 初始化/注册/启动 | ✅ |
| `LogEvent` | 紫色 | 无 | EventBus 派发/订阅 | ✅ |
| `LogCleanup` | 灰色 | 无 | 销毁/卸载/清理 | ✅ |
| `LogWarning` | 白色 | 黄色 | 异常但可恢复 | ✅ |
| `LogError` | 白色 | 红色 | 错误 | ❌（始终输出） |
| `LogException` | 白色 | 红色 | 异常 | ❌ |

每个方法都有两个重载——带 `Object context` 的参数双击可直接选中 GameObject。

---

## 三、标签系统

### 两级分层

标签用 `.` 分隔父级和子级：

```
Core                 ← 父（绿色 🔒）
├── Core.EventBus       ← 子（绿色 🔒，继承父颜色）
├── Core.ServiceLocator
├── Core.ManagerCollector
├── Core.UpdateManager
├── Core.StateMachine
├── Core.Singleton
└── Core.ObjectPool

Audio                ← 父（金色 🔒）
└── Audio.Manager

UI                   ← 父（橙色 🔒）
└── UI.Manager

Scene                ← 父（紫色 🔒）
└── Scene.Manager

Resource             ← 父（蓝色 🔒）
├── Resource.Manager
└── Resource.Provider

Input                ← 父（青色 🔒）
└── Input.Manager

Game                 ← 父（粉色 🔒）
```

### 级联开关

```csharp
EmberDebug.Disable(LogTags.Core);              // 父标签关闭 → EventBus + ServiceLocator + ... 全静默
EmberDebug.Disable(LogTags.CoreEventBus);      // 只关 EventBus，其他 Core 日志正常
EmberDebug.Enable(LogTags.Core);               // 重新开启父标签
```

### 所有预定义标签

在 [EmberLogPresets.cs](../../Assets/Ember/Core/Runtime/EmberLogPresets.cs) 的 `LogTags` 类中定义。

---

## 四、SO 可视化配置

编辑器下自动创建 `Assets/Ember/Core/Runtime/Resources/EmberDebugConfig.asset`，Inspector 面板中可：

- 全局开关：一键静默所有非 Error 日志
- 自动收集：新类首次调用日志时自动加入列表
- 批量操作：全部开启 / 全部关闭 / 清理空项
- 逐类控制：开关 + 颜色选择（预定义标签 🔒 锁定颜色）
- 父子缩进：子标签 `└─` 缩进展示

---

## 五、从 Debug.Log 迁移

```csharp
// ❌ 旧的
Debug.Log("[Ember] Resource system initialized.");
Debug.LogWarning("[Ember] Mixer not found.");
Debug.LogError("[Ember] Load failed.");

// ✅ 新的
EmberDebug.LogInit(LogTags.ResourceManager, "Resource system initialized.");
EmberDebug.LogWarning(LogTags.AudioManager, "Mixer not found.");
EmberDebug.LogError(LogTags.ResourceProvider, "Load failed.");
```

区别：
- 标签替代 `[Ember]` 前缀 → 彩色 + 可按模块过滤
- 消息文本不带模块前缀（标签已标识）
- 每条日志自动附 `(at 文件:行号)` → Console 双击跳转

---

## 六、颜色定义

所有颜色在 [EmberLogPresets.cs](../../Assets/Ember/Core/Runtime/EmberLogPresets.cs) 中集中定义：

| 类 | 作用 |
|----|------|
| `LogTags` | 标签常量定义 |
| `LogTagColors` | 预定义标签的专属颜色（不可被 SO 修改） |
| `LogColors` | 消息级别文字颜色 |

调整颜色只需改这个文件。

---

## 七、注意事项

- `CallerFilePath` 和 `CallerLineNumber` 自动捕获调用位置，不需要手动传
- 打包后 SO 正常随包（在 `Runtime/Resources/` 下）
- `GlobalOpen = false` 时 `LogError` / `LogException` 不受影响，始终输出
- 标签颜色不会重复——子标签继承父级预定义颜色，用户自定义标签用 hash 生成

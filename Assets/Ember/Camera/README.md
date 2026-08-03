# Camera — 相机管理

## 概述

框架级相机基础设施。支持 Cinemachine 虚拟相机注册/切换、强制霸占堆栈（Override Stack）
多重嵌套、锁定模式。自动配置 CinemachineBrain 的过渡曲线。

## 文件清单

| 角色 | 路径 |
|------|------|
| 主逻辑入口 | `Runtime/EmberCameraManager.cs` |

## 依赖

| 依赖 | 类型 | 说明 |
|------|------|------|
| `Ember.Core` | 框架模块 | EmberSingleton、IEmberManager、EmberDebug、GameLauncher |
| `Unity.Cinemachine` | 外部包 | CinemachineCamera、CinemachineBrain、CinemachineBlenderSettings |

## 公开 API

### EmberCameraManager — 相机管理器

继承 EmberSingleton，实现 IEmberManager。[EmberInitOrder(Default)]。

**相机注册：**

| 方法 | 说明 |
|------|------|
| `Register(string key, CinemachineCamera vcam)` | 注册虚拟相机，自动禁用 GameObject |
| `Unregister(string key)` | 注销 |
| `IsRegistered(string key) → bool` | 是否已注册 |
| `GetCamera(string key) → CinemachineCamera` | 获取已注册相机 |

**普通切换：**

| 方法 | 说明 |
|------|------|
| `Switch(string key, bool force)` | 切换到指定相机。锁定/霸占模式下会被拦截（除非 force=true） |

**强制霸占堆栈（Override Stack）：**

| 方法 | 说明 |
|------|------|
| `PushOverride(string key, CinemachineCamera localCamera)` | 压入霸占栈顶。阻止普通切换。支持多重嵌套 |
| `PopOverride(string fallbackKey)` | 弹出栈顶。无霸占者时恢复正常模式 |
| `RemoveOverride(string key) → bool` | 精准移除某个霸占条目（不限栈顶） |

**锁定：**

| 方法 | 说明 |
|------|------|
| `Lock()` | 锁定相机，拒绝一切普通切换（霸占不受影响） |
| `Unlock()` | 解除锁定 |

**属性：**

| 属性 | 说明 |
|------|------|
| `ActiveCamera` | 当前活跃的虚拟相机 |
| `IsOverrideMode` | 是否处于霸占模式 |
| `OverrideStackCount` | 霸占栈层数 |
| `IsLocked` | 是否已锁定 |
| `UICamera` / `MainCamera` | 由 GameLauncher 注入的相机引用 |
| `Brain` | MainCamera 上的 CinemachineBrain |
| `blenderSettings` | Cinemachine 转场配置（可拖入 Inspector） |
| `OnCameraSwitched` (事件) | 相机切换后触发 |

## 主流程

**初始化：** `IEmberManager.Init()` → 从 GameLauncher 获取 UICamera/MainCamera → 获取 Brain → 应用 BlenderSettings

**切换：** `Switch(key)` → 锁定/霸占检查 → 查找注册表 → `ActivateCamera` → 禁用旧相机 → 启用新相机 → OnCameraSwitched 事件

**霸占：** `PushOverride(key)` → 查找/使用本地相机 → 压入栈 → ActivateCamera
`PopOverride(fallback)` → 弹出栈顶 → 有剩余霸占者则切换 → 无则恢复正常

**堆栈场景：** 玩家自由移动(Normal) → 进入对话(PushOverride "Dialogue") → 对话中播Timeline(PushOverride "Cutscene") → Timeline结束(PopOverride) → 对话结束(PopOverride) → 恢复正常

## 约束与陷阱

| 类别 | 说明 |
|------|------|
| Cinemachine 依赖 | 需要 Unity.Cinemachine 包 |
| 霸占嵌套 | 支持多重嵌套，Push/Pop 必须配对 |
| 锁定 vs 霸占 | Lock 拒绝普通 Switch，但不影响 PushOverride/PopOverride |

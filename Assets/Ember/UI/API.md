# 模块名称：用户界面管理（UI）

---

## 1. 快速上手

```csharp
// 1. 静态注册页面（放在一个静态类中集中管理）
public static class GamePages
{
    public static readonly PageDef MainMenu = new("ui/main_menu", UILayer.Normal);
    public static readonly PageDef Settings = new("ui/settings",  UILayer.Popup);
}

// 2. 打开页面
EmberUIManager.Instance.Push(GamePages.Settings, args: null);

// 3. 关闭页面
EmberUIManager.Instance.Pop(UILayer.Popup);
```

---

## 2. 模块概述

UI 模块提供基于层级栈的界面管理系统。每个界面实现 `IUIView` 接口获得完整生命周期
（OnOpen / OnPause / OnResume / OnClose），通过 `PageDef` 静态注册页面元数据，
由 `EmberUIManager` 统一管理多层级界面栈的 Push/Pop 操作和预制体异步加载。

---

## 3. 依赖关系

| 依赖 | 类型 | 说明 |
|------|------|------|
| `Ember.Core` | 框架模块 | EmberMonoSingleton（单例基类） |
| `Ember.Resource` | 框架模块 | EmberResourceManager（异步加载 UI 预制体） |
| `UnityEngine` | 引擎 | MonoBehaviour、GameObject、Instantiate、Destroy 等 |

---

## 4. 文件清单

| 角色 | 路径 |
|------|------|
| 主逻辑入口 | `Assets/Ember/UI/Runtime/EmberUIManager.cs` |
| 核心接口 | `Assets/Ember/UI/Runtime/IUIView.cs` |
| 页面定义数据类 | `Assets/Ember/UI/Runtime/PageDef.cs` |
| 视图/表现层 | 无（预制体由业务层提供） |
| 编辑器扩展 | 无 |

---

## 5. 公开 API

### 5.1 入口类型

| 类型 | 职责 | 获取方式 |
|------|------|----------|
| `EmberUIManager` | UI 界面栈管理，Push/Pop 操作入口 | `EmberUIManager.Instance`（MonoSingleton） |
| `IUIView` | UI 界面生命周期接口 | 由业务层 MonoBehaviour 实现，挂载在预制体上 |
| `PageDef` | 页面元数据（预制体路径 + 层级） | `new PageDef("path", UILayer.Normal)` |
| `UILayer` | 层级预设枚举（Background / Normal / Popup / TopMost） | 直接使用枚举值或自定义 int |

### 5.2 核心方法

#### EmberUIManager — 界面管理器

| 方法签名 | 说明 |
|----------|------|
| `Push(PageDef page, object args = null)` | 异步加载预制体并压入指定层级栈顶。暂停原栈顶 → 加载 → 实例化 → OnOpen → 入栈 |
| `Pop(int layer)` | 关闭指定层级栈顶界面：OnClose → 弹出 → Destroy → 恢复新栈顶 |
| `Pop(UILayer layer)` | Pop 的枚举重载 |
| `CloseAll(int layer)` | 关闭指定层级所有界面 |
| `CloseAll(UILayer layer)` | CloseAll 的枚举重载 |
| `CloseAll()` | 关闭所有层级的所有界面 |
| `GetTopView(int layer)` | 获取指定层级栈顶界面，空栈返回 null |
| `GetCount(int layer)` | 获取指定层级的界面数量 |
| `HasView(int layer)` | 指定层级是否有界面在显示中 |

#### IUIView — 界面生命周期接口

| 方法签名 | 说明 |
|----------|------|
| `OnOpen(object args)` | 界面首次打开时调用（预制体实例化后），在此绑定控件、注册事件 |
| `OnClose()` | 界面被关闭时调用，注销事件、释放引用；之后 GameObject 被销毁 |
| `OnPause()` | 另一个界面被 Push 到此界面上方时调用，暂停动画/计时器 |
| `OnResume()` | 上方界面被 Pop 后，此界面重新回到栈顶时调用，刷新数据/恢复动画 |

#### PageDef — 页面定义

| 方法签名 | 说明 |
|----------|------|
| `PageDef(string prefabPath, int layer)` | 构造，prefabPath 为 null 时抛出 ArgumentNullException |
| `PageDef(string prefabPath, UILayer layer)` | 构造重载，接受 UILayer 枚举 |
| `PrefabPath` (属性) | 预制体资源路径 |
| `Layer` (属性) | 所属层级值 |

#### UILayer — 层级枚举

| 值 | 数值 | 说明 |
|----|------|------|
| `Background` | 0 | 背景层 |
| `Normal` | 100 | 普通界面层 |
| `Popup` | 200 | 弹窗层 |
| `TopMost` | 300 | 顶层（Loading、全局提示等） |

---

## 6. 主流程

**流程一：Push（打开界面）**
`[外部] Push(page, args)` → `[_initialized 检查]` → `[page 判空]` → `EnsureLayerRoot(layer)` → `PauseTopView(layer)` → `EmberResourceManager.LoadAssetAsync<GameObject>(prefabPath, callback)` → `[回调: prefab 判空]` → `Instantiate(prefab, layerRoot)` → `GetComponent<IUIView>()` → `[无 IUIView: Destroy + LogError]` → `stack.Push(view)` → `view.OnOpen(args)`

**流程二：Pop（关闭栈顶界面）**
`[外部] Pop(layer)` → `TryPop(layer, out view)` → `[空栈: return]` → `view.OnClose()` → `DestroyView(view)` → `ResumeTopView(layer)` → `[新栈顶.OnResume()]`

**流程三：CloseAll（批量关闭）**
`[外部] CloseAll(layer)` → `[stack 不存在: return]` → `while Pop → OnClose → DestroyView`
`[外部] CloseAll()` → `foreach layer in _stacks → CloseAll(layer)`

---

## 7. 修改影响范围

- **调整 Push 流程（如增加加载动画、Loading 遮罩）** → 改 `EmberUIManager.Push()`
- **新增界面生命周期事件（如 OnPreOpen、OnPostClose）** → 在 `IUIView` 添加方法，同步更新 `EmberUIManager.Push/Pop` 中的调用点
- **新增层级（如在 Normal 和 Popup 之间插入 Banner 层）** → 在 `UILayer` 枚举添加新值即可，不影响现有逻辑
- **替换预制体加载方式（如直接引用而非路径加载）** → 改 `PageDef` 和 `Push` 中的加载逻辑
- **新增查询 API（如按类型查找界面）** → 在 `EmberUIManager` 添加新方法

---

## 8. 约束与已知陷阱

| 类别 | 说明 |
|------|------|
| 初始化顺序 | `Push` 依赖 `EmberResourceManager` 已完成初始化，否则预制体加载失败回调 null + LogError |
| 生命周期 | `EmberUIManager` 继承 `EmberMonoSingleton`，挂载 `DontDestroyOnLoad`。Push 是**异步**的（走 ResourceManager 加载预制体），Pop/CloseAll 是**同步**的 |
| 数据边界 | `PageDef.PrefabPath` 为 null 时构造函数抛 `ArgumentNullException`。Push 时若预制体无 `IUIView` 组件，已实例化的 GameObject 会被立即 Destroy + LogError。每层界面栈无深度限制 |
| 线程安全 | 所有操作仅限主线程 |
| 已知问题 | 无 `[Obsolete]`、`TODO`、`FIXME` 标记。Push 是异步回调模式，连续快速 Push 同一个界面可能在第一个回调完成前第二次调用已执行，需业务层自行防重入 |

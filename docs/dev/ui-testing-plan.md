# UI 模块集成测试计划

> 目标：用 Init → Main → GamePlay 三个顶层状态 + 四个 UI 页面验证 EUIManager + EUIPageRouter + UIBinding + StateMachine 完整链路。

---

## 测试场景

```
MainScene 启动 → 开屏动画 → MainMenu 自动打开
    ↓ 点击"设置"按钮
Settings (Popup) 弹出，MainMenu 被遮挡（OnPause）
    ↓ 点击"关闭"
Settings 关闭，MainMenu 恢复（OnResume）
    ↓ 点击"开始游戏"
MainState ──→ GameplayState（场景切换 + Loading 页面）
    ↓ 加载完成
InGameUI 打开（GameplayScene）
    ↓ 点击"返回主界面"
GameplayState ──→ MainState（场景切换 + Loading 页面）
    ↓ 加载完成
MainMenu 重新打开（回到大厅）
```

---

## 需要新建的文件

```
Assets/Game/
├── State/
│   ├── GameMainState.cs             ← MainState 子类
│   └── GameGameplayState.cs         ← GameplayState 子类（新增）
├── UI/
│   ├── Prefabs/
│   │   ├── MainMenu.prefab          ← MainMenu 预制体
│   │   ├── Settings.prefab          ← Settings 预制体
│   │   └── InGameUI.prefab          ← InGameUI 预制体（新增）
│   ├── Generated/
│   │   ├── UIMainMenu.bindings.cs   ← 代码生成（自动）
│   │   ├── UIMainMenu.cs            ← 骨架 + 手写逻辑
│   │   ├── UISettings.bindings.cs   ← 代码生成（自动）
│   │   ├── UISettings.cs            ← 骨架 + 手写逻辑
│   │   ├── UIInGame.bindings.cs     ← 代码生成（自动，新增）
│   │   └── UIInGame.cs              ← 骨架 + 手写逻辑（新增）
│   └── GamePages.cs                 ← 页面注册表（EUIPageDef）
└── Scenes/
    ├── FrameworkScene.unity         ← Init → Main 的启动场景
    ├── MainScene.unity              ← 主界面场景
    └── GameplayScene.unity          ← 玩法场景（新增）
```

---

## 步骤

### 1. 创建预制体

**MainMenu.prefab** 结构（预制体上只有视觉组件 + EmberUIBinding）：

```
MainMenu (Canvas + CanvasScaler + CanvasGroup + EmberUIBinding)  ← 无 EUIPage!
├── BgImage (Image)
├── TitleText (TextMeshProUGUI)
├── BtnSettings (Button + Image)
│   └── Text (TextMeshProUGUI) "设置"
├── BtnStart (Button + Image)
│   └── Text (TextMeshProUGUI) "开始游戏"
└── VersionText (TextMeshProUGUI)
```

**Settings.prefab** 结构（同上）：

```
Settings (Canvas + CanvasScaler + CanvasGroup + EmberUIBinding)  ← 无 EUIPage!
├── BgMask (Image, 半透明黑)
├── Panel (Image, 居中面板)
│   ├── TitleText (TextMeshProUGUI) "设置"
│   ├── ToggleSound (Toggle)
│   ├── SliderVolume (Slider)
│   ├── BtnClose (Button)
│   │   └── Text (TextMeshProUGUI) "关闭"
│   └── BtnLogout (Button)
│       └── Text (TextMeshProUGUI) "退出登录"
└──
```

**InGameUI.prefab** 结构（游戏内 UI，只有一个返回按钮）：

```
InGameUI (Canvas + CanvasScaler + CanvasGroup + EmberUIBinding)  ← 无 EUIPage!
├── BgImage (Image, 游戏背景)
├── TitleText (TextMeshProUGUI) "游戏中"
├── BtnBack (Button + Image)
│   └── Text (TextMeshProUGUI) "返回主界面"
└── InfoText (TextMeshProUGUI) "这是游戏内 UI 界面"
```

> **架构关键**：预制体上**没有 MonoBehaviour 页面类**。`EUIPage` 是运行时创建的纯 C# 包装类，`EUILogic` 是生成的纯 C# 逻辑类（对标 Burner `GamePage` + `GameUILogic`）。预制体上只有 `EmberUIBinding` 一个自定义脚本。

### 2. 配置 EmberUIBinding

在 MainMenu 预制体的 Inspector 中：
- 是否为 Page: ✅
- 页面名: `MainMenu`
- 页面类型 PageFlags: `MainPage`
- 逻辑类: `Game/UI/UIMainMenu`（类名自动填 `UIMainMenu`）
- 点"自动收集绑定" → 自动填充 BtnSettings / BtnStart / TitleText 等

在 Settings 预制体的 Inspector 中：
- 页面名: `Settings`
- 页面类型: `Popup`
- 逻辑类: `Game/UI/UISettings`

在 InGameUI 预制体的 Inspector 中：
- 页面名: `InGameUI`
- 页面类型: `MainPage`
- 逻辑类: `Game/UI/UIInGame`

### 3. 生成代码

每个预制体点"生成"。产出：
- `UIMainMenu.bindings.cs` — 字段 + OnBind（`ControlMap["name"] as Type` 模式）
- `UIMainMenu.cs` — 生命周期骨架（OnInit/OnShow/OnHide/OnClose/OnDispose，仅首次生成）
- 自动更新 `GamePages.cs` 中的 EUIPageDef

> 生成的代码继承 `EUILogic`（纯 C#），非 MonoBehaviour。

### 4. 手写页面逻辑

**UIMainMenu.cs**（自动生成的骨架 + 手写业务逻辑，继承 EUILogic 纯 C# 类）：

```csharp
public partial class UIMainMenu  // 注意：继承 EUILogic，非 MonoBehaviour！
{
    public override void OnInit()
    {
        // ControlMap 已填充，字段已绑定
        _BtnSettings.onClick.AddListener(() =>
            EUIPageRouter.Instance.ShowPopup(GamePages.Settings));

        _BtnStart.onClick.AddListener(() =>
            EmberDebug.Log(LogTags.EmberUI, "开始游戏"));
    }

    public override void OnPause()
    {
        EmberDebug.Log(LogTags.EmberUI, "MainMenu 被遮挡");
    }

    public override void OnResume()
    {
        EmberDebug.Log(LogTags.EmberUI, "MainMenu 恢复可见");
    }

    public override void OnDispose()
    {
        // 清理事件监听
    }
}
```

**UISettings.cs**（生成的骨架 + 手写逻辑）：

```csharp
public partial class UISettings  // EUILogic 子类
{
    public override void OnInit()
    {
        _BtnClose.onClick.AddListener(() =>
            EUIPageRouter.Instance.ClosePage(Page));

        _BtnLogout.onClick.AddListener(() =>
            EmberDebug.Log(LogTags.EmberUI, "退出登录"));
    }

    public override void OnDispose()
    {
        EmberDebug.Log(LogTags.EmberUI, "Settings 清理");
    }
}
```

> **注意**：`Page` 属性来自 `EUILogic`，是当前页面的 `EUIPage` 包装引用。关闭页面用 `EUIPageRouter.Instance.ClosePage(Page)`。

**UIInGame.cs**（生成的骨架 + 手写逻辑）：

```csharp
public partial class UIInGame  // EUILogic 子类
{
    public override void OnInit()
    {
        _BtnBack.onClick.AddListener(() =>
        {
            // 返回主界面：通过状态机 TransitionTo<MainState>
            GameLauncher.Instance.Fsm.TransitionTo<MainState>();
        });
    }

    public override void OnDispose()
    {
        EmberDebug.Log(LogTags.EmberUI, "InGameUI 清理");
    }
}
```

> **关键**："返回主界面"不走 `EUIPageRouter.ClosePage()`，而是通过状态机 `TransitionTo<MainState>()` 触发场景切换 + Loading 页面。

### 5. 注册页面

**GamePages.cs**：

```csharp
public static class GamePages
{
    // Normal 层
    public static readonly EUIPageDef MainMenu = new("ui/main_menu",  UILayer.Normal, PageType.MainPage);
    public static readonly EUIPageDef InGameUI = new("ui/in_game_ui", UILayer.Normal, PageType.MainPage);
    // Popup 层
    public static readonly EUIPageDef Settings = new("ui/settings",   UILayer.Popup,  PageType.Popup);
    // TopMost 层
    public static readonly EUIPageDef EUILoadingPage = new("ui/ember_loading", UILayer.TopMost, PageType.TopMost);
}
```

### 6. 状态机配置

**GameMainState.cs**（开屏动画结束 → 打开首页）：

```csharp
public class GameMainState : MainState
{
    protected override void OnMainEnter(object args)
    {
        base.OnMainEnter(args);
        EUIPageRouter.DefaultLoadingPageDef = GamePages.EUILoadingPage;
    }

    protected override void OnOpeningAnimationEnd()
    {
        EUIPageRouter.Instance.ShowMainPage(GamePages.MainMenu);
    }
}
```

**GameGameplayState.cs**（进入玩法 → 打开 InGameUI）：

```csharp
public class GameGameplayState : GameplayState
{
    protected override void OnGameplayEnter(object args)
    {
        base.OnGameplayEnter(args);
        EUIPageRouter.Instance.ShowMainPage(GamePages.InGameUI);
    }

    protected override void OnGameplayExit()
    {
        EUIPageRouter.Instance.CloseAllPopups();
        base.OnGameplayExit();
    }
}
```

**UIMainMenu.OnInit()** 中"开始游戏"按钮：

```csharp
_BtnStart.onClick.AddListener(() =>
    GameLauncher.Instance.Fsm.TransitionTo<GameplayState>());
```

> **场景配置**：需在 Build Settings 中添加 `MainScene` 和 `GameplayScene`。

### 7. 运行测试

进入 Play Mode，验证：

| 步骤 | 预期行为 | 验证点 |
|------|---------|--------|
| 启动 | MainMenu 自动打开 | EUIManager.OpenPage 正常 |
| 点击"设置" | Settings 以 Popup 形式弹出，MainMenu 被 OnPause | EUIPageRouter.ShowPopup |
| 看控制台 | 出现 "MainMenu 被遮挡" 日志 | OnPause 回调正常 |
| BG Mask | Settings 下方出现半透明黑色遮罩 | EUIBgMaskPool 正常 |
| 点击遮罩 | Settings 关闭，MainMenu 恢复 | BG Mask 点击关闭 |
| 按 Escape | Settings 关闭，MainMenu 恢复 | TryEscapeKeyClose 正常 |
| 看控制台 | 出现 "MainMenu 恢复可见" + "Settings 清理" 日志 | OnResume + Cleanup |
| **点击"开始游戏"** | **触发 TransitionTo《GameplayState》，Loading 页面出现** | **状态机场景切换 + Loading** |
| **加载完成** | **InGameUI 打开，显示"游戏中"** | **GameGameplayState.OnGameplayEnter** |
| **点击"返回主界面"** | **触发 TransitionTo《MainState》，Loading 页面出现** | **状态机返回大厅** |
| **加载完成** | **MainMenu 重新打开（全新实例）** | **MainState.OnOpeningAnimationEnd** |
| UIObserver | OnPageOpened/OnPageClosed 事件正常推送 | 事件链路 |

---

## 测试矩阵

| 被测模块 | 测试内容 | 如何验证 |
|----------|---------|---------|
| **EUIManager** | Push/Pop 页面，Canvas 层创建 | Play 后 Hierarchy 中出现 UI_Layer_100/UI_Layer_200 |
| **EUIPageRouter** | ShowMainPage / ShowPopup / ClosePage 路由 | 页面正确打开/关闭 |
| **EUIPage** | Init → PlayShow → OnPause → OnResume → PlayHide → Cleanup | 控制台日志顺序正确 |
| **EUIPageContext** | MainPage 栈 + Popup 列表 + SortingOrder | Popup 的 sortingOrder > MainPage |
| **EUIBgMaskPool** | 自动创建/回收遮罩 | Popup 出现时 Hierarchy 有 EmberBgMask |
| **EUIObserver** | 生命周期事件播报 | Subscribe 回调收到事件 |
| **EmberUIBinding** | 字段生成 + Find + GetComponent | _btnSettings / _btnClose 不为 null |
| **UIBinding 代码生成** | .bindings.cs 正确生成 | 文件存在且编译通过 |
| **EUIPageTransition（预设+自定义）** | 预设渐入渐出 + OnCustomEnter/OnCustomExit 动画链 | Loading 页面进度条组单独渐显/渐隐 |
| **EmberStateMachine** | Init→Main→Gameplay→Main 完整链路 | 状态切换日志 + 场景加载/卸载 |
| **EmberSceneManager** | 异步加载/卸载 + Loading 页面自动显示 | TransitionTo《GameplayState》时 Loading 出现 |
| **EmberSceneManager 假进度** | 比例映射 + 平滑收尾 | Loading 进度条 0→60%→平滑至 100% |

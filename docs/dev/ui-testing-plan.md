# UI 模块集成测试计划

> 目标：用两个真实 UI 页面验证 EmberUIManager + EmberUIPageRouter + UIBinding 完整链路。

---

## 测试场景

```
MainScene 启动 → MainMenu 自动打开
    ↓ 点击"设置"按钮
Settings (Popup) 弹出，MainMenu 被遮挡（OnPause）
    ↓ 点击"关闭" / 点击遮罩 / 按 Escape
Settings 关闭（PlayHide → Cleanup），MainMenu 恢复（OnResume）
```

---

## 需要新建的文件

```
Assets/Game/UI/
├── Prefabs/
│   ├── MainMenu.prefab              ← MainMenu 预制体
│   └── Settings.prefab              ← Settings 预制体
├── Generated/
│   ├── UIMainMenu.bindings.cs       ← 代码生成（自动）
│   ├── UIMainMenu.cs                ← 代码生成骨架（自动）+ 手写逻辑
│   ├── UISettings.bindings.cs       ← 代码生成（自动）
│   └── UISettings.cs                ← 骨架 + 手写逻辑
└── GamePages.cs                     ← 页面注册表（PageDef）
```

---

## 步骤

### 1. 创建预制体

**MainMenu.prefab** 结构：

```
MainMenu (Canvas + CanvasGroup + EmberPage + EmberUIBinding)
├── BgImage (Image)
├── TitleText (TextMeshProUGUI)
├── BtnSettings (Button + Image)
│   └── Text (TextMeshProUGUI) "设置"
├── BtnStart (Button + Image)
│   └── Text (TextMeshProUGUI) "开始游戏"
└── VersionText (TextMeshProUGUI)
```

**Settings.prefab** 结构：

```
Settings (Canvas + CanvasGroup + EmberPage + EmberUIBinding)
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

### 2. 配置 EmberUIBinding

在 MainMenu 预制体的 Inspector 中：
- NamespaceName: `Game.UI`
- ClassName: `UIMainMenu`
- OutputDirectory: `Game/UI/Generated`
- 点"自动收集所有子节点" → 自动填充 BtnSettings / BtnStart / TitleText

在 Settings 预制体的 Inspector 中：
- NamespaceName: `Game.UI`
- ClassName: `UISettings`
- OutputDirectory: `Game/UI/Generated`

### 3. 生成代码

每个预制体点"生成代码"。产出：
- `UIMainMenu.bindings.cs` — 字段 + OnBind
- `UIMainMenu.cs` — 生命周期骨架（首次）
- `UISettings.bindings.cs` — 字段 + OnBind
- `UISettings.cs` — 生命周期骨架（首次）

### 4. 手写页面逻辑

**UIMainMenu.cs**（在生成的骨架上补充）：

```csharp
public partial class UIMainMenu
{
    protected override void OnInitialize(object args)
    {
        _btnSettings.OnClick.AddListener(() =>
            EmberUIPageRouter.Instance.ShowPopup(GamePages.Settings));

        _btnStart.OnClick.AddListener(() =>
            EmberDebug.Log(LogTags.EmberUI, "开始游戏"));
    }

    protected override void OnPaused()
    {
        EmberDebug.Log(LogTags.EmberUI, "MainMenu 被遮挡");
    }

    protected override void OnResumed()
    {
        EmberDebug.Log(LogTags.EmberUI, "MainMenu 恢复可见");
    }

    protected override void OnCleanup()
    {
        // 清理事件
    }
}
```

**UISettings.cs**（在生成的骨架上补充）：

```csharp
public partial class UISettings
{
    protected override void OnInitialize(object args)
    {
        _btnClose.onClick.AddListener(() =>
            EmberUIPageRouter.Instance.ClosePage(this));
    }

    protected override bool OnEscapeKey()
    {
        EmberUIPageRouter.Instance.ClosePage(this);  // 按返回键关闭
        return true;  // 已处理，阻止冒泡
    }

    protected override void OnCleanup()
    {
        EmberDebug.Log(LogTags.EmberUI, "Settings 清理");
    }
}
```

### 5. 注册页面

**GamePages.cs**：

```csharp
public static class GamePages
{
    public static readonly PageDef MainMenu = new("ui/main_menu", UILayer.Normal, PageType.MainPage);
    public static readonly PageDef Settings = new("ui/settings", UILayer.Popup,  PageType.Popup);
}
```

### 6. 配置 MainState

在 `MainState.OnMainEnter` 中：

```csharp
protected override void OnMainEnter(object args)
{
    EmberUIPageRouter.Instance.ShowMainPage(GamePages.MainMenu);
}
```

### 7. 运行测试

进入 Play Mode，验证：

| 步骤 | 预期行为 | 验证点 |
|------|---------|--------|
| 启动 | MainMenu 自动打开 | EmberUIManager.OpenPage 正常 |
| 点击"设置" | Settings 以 Popup 形式弹出，MainMenu 被 OnPause | EmberUIPageRouter.ShowPopup |
| 看控制台 | 出现 "MainMenu 被遮挡" 日志 | OnPause 回调正常 |
| BG Mask | Settings 下方出现半透明黑色遮罩 | EmberBgMaskPool 正常 |
| 点击遮罩 | Settings 关闭，MainMenu 恢复 | BG Mask 点击关闭 |
| 按 Escape | Settings 关闭，MainMenu 恢复 | TryEscapeKeyClose 正常 |
| 看控制台 | 出现 "MainMenu 恢复可见" + "Settings 清理" 日志 | OnResume + Cleanup |
| UIObserver | OnPageOpened/OnPageClosed 事件正常推送 | UniRx Subject 链路 |

---

## 测试矩阵

| 被测模块 | 测试内容 | 如何验证 |
|----------|---------|---------|
| **EmberUIManager** | Push/Pop 页面，Canvas 层创建 | Play 后 Hierarchy 中出现 UI_Layer_100/UI_Layer_200 |
| **EmberUIPageRouter** | ShowMainPage / ShowPopup / ClosePage 路由 | 页面正确打开/关闭 |
| **EmberPage** | Init → PlayShow → OnPause → OnResume → PlayHide → Cleanup | 控制台日志顺序正确 |
| **EmberPageContext** | MainPage 栈 + Popup 列表 + SortingOrder | Popup 的 sortingOrder > MainPage |
| **EmberBgMaskPool** | 自动创建/回收遮罩 | Popup 出现时 Hierarchy 有 EmberBgMask |
| **EmberUIObserver** | 生命周期事件播报 | Subscribe 回调收到事件 |
| **EmberUIBinding** | 字段生成 + Find + GetComponent | _btnSettings / _btnClose 不为 null |
| **UIBinding 代码生成** | .bindings.cs 正确生成 | 文件存在且编译通过 |

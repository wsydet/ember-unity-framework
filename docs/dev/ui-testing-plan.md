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

**MainMenu.prefab** 结构（预制体上只有视觉组件 + EmberUIBinding）：

```
MainMenu (Canvas + CanvasScaler + CanvasGroup + EmberUIBinding)  ← 无 EmberPage!
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
Settings (Canvas + CanvasScaler + CanvasGroup + EmberUIBinding)  ← 无 EmberPage!
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

> **架构关键**：预制体上**没有 MonoBehaviour 页面类**。`EmberPage` 是运行时创建的纯 C# 包装类，`EmberUILogic` 是生成的纯 C# 逻辑类（对标 Burner `GamePage` + `GameUILogic`）。预制体上只有 `EmberUIBinding` 一个自定义脚本。

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

### 3. 生成代码

每个预制体点"生成"。产出：
- `UIMainMenu.bindings.cs` — 字段 + OnBind（`ControlMap["name"] as Type` 模式）
- `UIMainMenu.cs` — 生命周期骨架（OnInit/OnShow/OnHide/OnClose/OnDispose，仅首次生成）
- 自动更新 `GamePages.cs` 中的 PageDef

> 生成的代码继承 `EmberUILogic`（纯 C#），非 MonoBehaviour。

### 4. 手写页面逻辑

**UIMainMenu.cs**（自动生成的骨架 + 手写业务逻辑，继承 EmberUILogic 纯 C# 类）：

```csharp
public partial class UIMainMenu  // 注意：继承 EmberUILogic，非 MonoBehaviour！
{
    public override void OnInit()
    {
        // ControlMap 已填充，字段已绑定
        _BtnSettings.onClick.AddListener(() =>
            EmberUIPageRouter.Instance.ShowPopup(GamePages.Settings));

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
public partial class UISettings  // EmberUILogic 子类
{
    public override void OnInit()
    {
        _BtnClose.onClick.AddListener(() =>
            EmberUIPageRouter.Instance.ClosePage(Page));

        _BtnLogout.onClick.AddListener(() =>
            EmberDebug.Log(LogTags.EmberUI, "退出登录"));
    }

    public override void OnDispose()
    {
        EmberDebug.Log(LogTags.EmberUI, "Settings 清理");
    }
}
```

> **注意**：`Page` 属性来自 `EmberUILogic`，是当前页面的 `EmberPage` 包装引用。关闭页面用 `EmberUIPageRouter.Instance.ClosePage(Page)`。

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

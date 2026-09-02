# UI 功能逻辑备份（重新生成代码用）

> 状态：✅ 已使用完成 —— 2026-08-31「删除 UI → 新位置重建 → 重新生成 → 补全功能」全流程已走完，6 页面功能逻辑已按本文档补回 dev（补全时保留新骨架「页面配置」区：NeedUpdate / Popup 遮罩双钩子；GMPage/EUILoadingPage 的 NeedUpdate 改为静态 `=> true` 开启，删除原动态赋值）。**用户确认模板保存成功后即可删除本文档。**
> 创建：2026-08-31
> 背景：用户计划调整 EUIBinding 代码生成位置与预制体存放位置；过程中会使用「Ember/UI/UI 预制体管理器 → 删除 UI」整体删除页面（预制体 + .cs + .Binding.cs + Settings.cs + GamePages 条目），重新生成后按本文档补功能。
> 关联：CLAUDE.md（编码规范）、docs/dev/template-upgrade-system.md（两级标记）、upm-migration-plan.md

---

## 〇、「删除 UI」删除范围（已核对源码 EUIPrefabManagerWindow.DeleteUI）

删除以下全部资产（不可恢复，含 .meta）：

1. 预制体本体（.prefab）
2. 逻辑脚本（.cs，仅当存在）
3. 绑定脚本（.Binding.cs）
4. 自定义参数脚本（XxxSettings.cs，仅当 GenerateCustomSettings）
5. 两个 GamePages 文件（GamePages.cs + GamePages.User.cs）中该页面的**失效** EUIPageDef 条目（预制体删除后条目失效 → 复用 CleanStalePageDefs 清理，含注释行）

**不碰**：场景文件（.unity）——场景内对已删预制体的实例会变 Missing Prefab，需在删除前自行处理（用户方案：主界面保留非预制体 UI）。

**不受影响**：EUIBootSplash / CustomBootSplash / EUIDefaultMainAnimation（非 EUIBinding 页面，不出现在管理器列表）。

---

## 一、重新生成后骨架的变化（2026-08-31 起）

新生成的 .cs 骨架自带「页面配置」区（用户区，可自行修改）：

- `public override bool NeedUpdate => false;`（+用法说明）
- **仅 Popup**：`AutoCreateClickableMask` + `OnClickMask` 两个 override（+用法说明）

补全功能时**保留这些新成员**，只把下述功能逻辑填回 Lifecycle 块内与用户区。

---

## 二、页面功能逻辑记录（补全内容 = 下述代码块）

### 1. EUIMainPage（EUIMainPanel，MainPage）

**控件绑定**（prefab 层级路径）：`Btn_Start`（m_Btn_Start）、`Btn_Settings`（m_Btn_Settings）

**功能逻辑**：

```csharp
private const string TAG = LogTags.Game + "." + nameof(EUIMainPage);

public override void OnInit()
{
    base.OnInit();

    Btn_Start.onClick.AddListener(() =>
        GameLauncher.Instance.Fsm.TransitionTo<GameplayState>());

    Btn_Settings.onClick.AddListener(() =>
        GameLauncher.Instance.Fsm.TransitionTo<SettingsState>(SettingsContext.Main));

    OnInitUser();
}

public override void OnPause()
{
    base.OnPause();
    EmberDebug.Log(TAG, "EUIMainPage 被遮挡");
}

public override void OnResume()
{
    base.OnResume();
    EmberDebug.Log(TAG, "EUIMainPage 恢复可见");
}

public override void OnDispose()
{
    Btn_Start.onClick.RemoveAllListeners();
    Btn_Settings.onClick.RemoveAllListeners();
    base.OnDispose();
    OnDisposeUser();
}
```

### 2. EUIGamePlayPage（EUIGamePlayPanel，MainPage）

**控件绑定**：`Btn_Back`（m_Btn_Back）

**功能逻辑**：

```csharp
private const string TAG = LogTags.Game + "." + nameof(EUIGamePlayPage);

public override void OnInit()
{
    base.OnInit();

    Btn_Back.onClick.AddListener(() =>
        GameLauncher.Instance.Fsm.TransitionTo<MainState>());

    OnInitUser();
}

public override void OnDispose()
{
    EmberDebug.Log(TAG, "EUIGamePlayPage 清理");
    Btn_Back.onClick.RemoveAllListeners();
    base.OnDispose();
    OnDisposeUser();
}
```

### 3. EUISettingPage（EUISettingPanel，Popup）

**控件绑定**：`Btn_Close`（Panel/m_Btn_Close）、`Txt_NowScene`（Panel/m_Txt_NowScene）

**功能逻辑**：

```csharp
private const string TAG = LogTags.Game + "." + nameof(EUISettingPage);

public override void OnInit()
{
    base.OnInit();

    Btn_Close.onClick.AddListener(() =>
        GameLauncher.Instance.Fsm.Pop());

    OnInitUser();
}

public override void OnOpen(object param)
{
    base.OnOpen(param);

    var context = param is SettingsContext ctx ? ctx : SettingsContext.Main;
    Txt_NowScene.text = context switch
    {
        SettingsContext.Main     => "当前场景：主界面",
        SettingsContext.Gameplay => "当前场景：玩法中",
        _                        => "当前场景：未知",
    };

    OnOpenUser(param);
}

public override void OnDispose()
{
    EmberDebug.Log(TAG, "EUISettingPage 清理");
    Btn_Close.onClick.RemoveAllListeners();
    base.OnDispose();
    OnDisposeUser();
}
```

> 注意：本页为 Popup → 新骨架自带 `AutoCreateClickableMask`/`OnClickMask`，补全后遮罩点击默认关闭行为与原来一致（此前是框架硬编码关闭）。

### 4. GMPage（GMPanel，FreePage / TopMost / sortingOrder 30000）

**控件绑定**（prefab 层级路径）：

| 字段 | 路径 | 类型 |
|---|---|---|
| Btn_GM | m_Btn_GM | Button |
| Panel_GM | m_Panel_GM | Component |
| Pgb_TimeScale | m_Panel_GM/Infos/Time/m_Pgb_TimeScale | Slider |
| Txt_TimeScale | m_Panel_GM/Infos/Time/m_Txt_TimeScale | TMP_Text |
| Txt_GameState | m_Panel_GM/Infos/顶层状态/m_Txt_GameState | TMP_Text |
| Tgl_Test | m_Panel_GM/Infos/开关/m_Tgl_Test | Toggle |
| Scr_Test | m_Panel_GM/Infos/ScrollRect/m_Scr_Test | ScrollRect |
| Img_Test | m_Panel_GM/Infos/Image/m_Img_Test | Image |
| Raw_Test | m_Panel_GM/Infos/RawIamge/m_Raw_Test | RawImage |
| EUIBtn_Exit | m_Panel_GM/Buttons/m_EUIBtn_Exit | EUIButtonEx |
| EUITgl_Test | m_Panel_GM/m_EUITgl_Test | EUIToggleEx |
| EUIImg_Test | m_Panel_GM/m_EUIImg_Test | EUIImageEx |
| Img_Circle | m_Panel_GM/m_Img_Circle | EUICircleImage |

**功能逻辑**（Lifecycle 块内，`[EmberManaged:begin/end Lifecycle]` 之间）：

```csharp
public override void OnInit()
{
    base.OnInit();

    // GM 面板默认关闭，通过 Btn_GM 打开
    Panel_GM.gameObject.SetActive(false);

    // 按钮点击切换面板显示
    Btn_GM.onClick.AddListener(TogglePanel);

    // 退出按钮（增强组件 EUIButtonEx）：关闭 GM 面板
    if (EUIBtn_Exit != null)
        EUIBtn_Exit.onClick.AddListener(ClosePanel);

    // ── 时间缩放：Toggle 开关时间条显示，Slider 控制缩放倍率 ──

    // 时间条容器（Time 节点）初始隐藏
    _timeBarRoot = Pgb_TimeScale.transform.parent;
    if (_timeBarRoot != null)
        _timeBarRoot.gameObject.SetActive(false);

    // Toggle：控制时间条显示（默认关闭）
    Tgl_Test.isOn = false;
    Tgl_Test.onValueChanged.AddListener(OnTimeBarToggle);

    // Slider：控制 EmberTimeManager.TimeScale（0.1x ~ 3x，默认 1x）
    Pgb_TimeScale.minValue = 0.1f;
    Pgb_TimeScale.maxValue = 3f;
    Pgb_TimeScale.value = 1f;
    Pgb_TimeScale.onValueChanged.AddListener(OnTimeScaleChanged);
    RefreshTimeScaleText(Pgb_TimeScale.value);

    // ── 增强组件测试 ──

    // EUIToggleEx：使用 Label 槽位直接访问文本
    if (EUITgl_Test != null)
    {
        EUITgl_Test.onValueChanged.AddListener(OnEnhancedToggleChanged);
        if (EUITgl_Test.Label != null)
            EUITgl_Test.Label.text = "增强开关";
    }

    // EUICircleImage：FillPercent 演示进度环
    if (Img_Circle != null)
        Img_Circle.FillPercent = 0.5f;

    // EUIImageEx：帧动画测试（无精灵数组时仅演示颜色）
    if (EUIImg_Test != null)
        EUIImg_Test.color = new Color(0.3f, 0.8f, 0.5f, 1f);

    // 每帧刷新状态机名称
    NeedUpdate = true;

    OnInitUser();
}

public override void OnResetDefault()
{
    // 页面默认关闭
    Panel_GM.gameObject.SetActive(false);
}

public override void OnDispose()
{
    Btn_GM.onClick.RemoveAllListeners();
    if (EUIBtn_Exit != null)
        EUIBtn_Exit.onClick.RemoveAllListeners();
    Tgl_Test.onValueChanged.RemoveListener(OnTimeBarToggle);
    Pgb_TimeScale.onValueChanged.RemoveListener(OnTimeScaleChanged);
    if (EUITgl_Test != null)
        EUITgl_Test.onValueChanged.RemoveListener(OnEnhancedToggleChanged);
    base.OnDispose();
    OnDisposeUser();
}

/// <summary>每帧：刷新顶层状态机状态名</summary>
public override void OnUpdate()
{
    RefreshGameStateText();
}

// ── 内部参数 ──

private Transform _timeBarRoot;

// ── 内部方法 ──

private void TogglePanel()
{
    Panel_GM.gameObject.SetActive(!Panel_GM.gameObject.activeSelf);
}

private void ClosePanel()
{
    Panel_GM.gameObject.SetActive(false);
}

/// <summary>Toggle 开关：控制时间条（Time 容器）显示，关闭时恢复 TimeScale = 1</summary>
private void OnTimeBarToggle(bool isOn)
{
    if (_timeBarRoot != null)
        _timeBarRoot.gameObject.SetActive(isOn);

    if (!isOn)
    {
        // 关闭时间控制：恢复默认倍率，滑块和文本同步
        Pgb_TimeScale.value = 1f;
        ApplyTimeScale(1f);
        RefreshTimeScaleText(1f);
    }

    EmberDebug.LogEvent("GM", $"时间条显示: {isOn}");
}

/// <summary>Slider 变化：设置全局时间缩放倍率</summary>
private void OnTimeScaleChanged(float value)
{
    ApplyTimeScale(value);
    RefreshTimeScaleText(value);
}

/// <summary>
/// 应用时间缩放：同时设置 UnityEngine.Time.timeScale（影响所有游戏逻辑）
/// 和 EmberTimeManager.TimeScale（保持框架时间一致）。
/// </summary>
private void ApplyTimeScale(float value)
{
    // Unity 全局时间缩放：影响 Animator / Update / DOTween / 物理等
    UnityEngine.Time.timeScale = value;

    // 框架时间同步（可选，框架内用 EmberTimeManager.DeltaTime 的逻辑同样生效）
    var tm = EmberTimeManager.Instance;
    if (tm != null)
        tm.TimeScale = value;
}

/// <summary>刷新倍率文本</summary>
private void RefreshTimeScaleText(float value)
{
    if (Txt_TimeScale != null)
        Txt_TimeScale.text = $"TimeScale: {value:F2}x";
}

/// <summary>刷新顶层状态机状态名（GameLauncher.Fsm.Current.Name）</summary>
private void RefreshGameStateText()
{
    if (Txt_GameState == null) return;

    string stateName = "—";
    var launcher = GameLauncher.Instance;
    if (launcher != null && launcher.Fsm != null && launcher.Fsm.Current != null)
        stateName = launcher.Fsm.Current.Name;

    if (Txt_GameState.text != stateName)
        Txt_GameState.text = stateName;
}

/// <summary>增强开关（EUIToggleEx）回调：通过 Label 槽位反馈状态</summary>
private void OnEnhancedToggleChanged(bool isOn)
{
    if (EUITgl_Test != null && EUITgl_Test.Label != null)
        EUITgl_Test.Label.text = isOn ? "已开启" : "已关闭";
    EmberDebug.LogEvent("GM", $"增强开关状态: {isOn}");
}
```

> GMPage 的 usings：`Ember.Basic`、`Ember.Core`、`UnityEngine`、`UnityEngine.UI`、`Ember.UIExtension`。
> 注意：新骨架自带 `public override bool NeedUpdate => false;`（页面配置区）。GMPage 采用**动态开关**方式（OnInit 里 `NeedUpdate = true`）——两种方式不能并存（get-only override 下不能 set）。补全时二选一：
> ① 保留骨架的 `NeedUpdate => false` 改为 `=> true`，删掉 OnInit 里的 `NeedUpdate = true;`；
> ② 或删掉骨架的 override，保留 OnInit 动态赋值。**推荐 ①**（与 burner 风格一致）。

### 5. EUILoadingPage（EUILoadingPanel，TopMost）

**控件绑定**（prefab 层级路径）：`Cg_Progress`（m_Cg_Progress）、`Img_ProgressBar`（m_Cg_Progress/Pos/m_Img_ProgressBar）、`Txt_ProgressNum`（m_Cg_Progress/Pos/m_Txt_ProgressNum）、`TransitionBlock`（m_TransitionBlock）

**功能逻辑**（完整 Lifecycle 块内容，含假进度两阶段 + 自定义过渡动画）：

```csharp
// ── 内部参数（class 级私有字段）──
private float _fastElapsed;
private float _tailElapsed;
private float _displayProgress;
private bool _inTailPhase;
private bool _fakeComplete;
private bool _fadeInDone; // 渐入完成前不开始计时

public override void OnInit()
{
    base.OnInit();
    _settings = CustomSettings as EUILoadingPageSettings;
    NeedUpdate = true;
    // 顶层状态机的快速转场判定必须在 OnShow 之前生效：
    // 拦截器在页面打开完成后才送达 SkipFakeProgress，若等那时，OnShow 已按开关把进度条显示出来了
    if (EmberStateMachine.QuickSceneLoad)
        SkipFakeProgress = true;
    // 只有假进度模式才在 Init 阶段重置进度显示（快速模式不激活进度条/数字，避免闪现）
    if (UseFakeProgress)
        ApplySettings();
    else
        HideProgressVisuals();
    OnInitUser();
}

public override void OnShow()
{
    base.OnShow();

    if (UseFakeProgress)
    {
        _fastElapsed = 0f;
        _tailElapsed = 0f;
        _displayProgress = 0f;
        _inTailPhase = false;
        _fakeComplete = false;
        _fadeInDone = false;
        ApplySettings();
    }
    else
    {
        // 快速/无进度显示模式：不跑假进度，隐藏进度条与数字
        HideProgressVisuals();
    }

    EmberEventBus.OnNext(EUIEvents.LoadingFadeInStart);
    OnShowUser();
}

public override void OnHide()
{
    base.OnHide();
    NeedUpdate = false;
    OnHideUser();
}

public override void OnUpdate()
{
    // 无假进度模式（显式快速转场 / 未勾选任何进度显示 / 无配置）：只隐藏进度显示，
    // 就绪由 IsTransitionReady 的真实加载判定
    if (!UseFakeProgress)
    {
        HideProgressVisuals();
        return;
    }

    // 渐入完成前不开始计时（进度条在此期间不可见）
    if (_fakeComplete || !_fadeInDone) return;

    var sceneMgr = EmberSceneManager.Instance;
    bool realDone = sceneMgr != null && !sceneMgr.IsLoading;

    if (!_inTailPhase)
    {
        // Phase 1: 快充阶段 → 阈值
        _fastElapsed += Time.deltaTime;
        float fastT = Mathf.Clamp01(_fastElapsed / _settings.fastFillDuration);
        _displayProgress = fastT * _settings.fastFillThreshold;

        if (fastT >= 1f)
        {
            if (realDone)
            {
                // 真实加载已完成 → 进入收尾
                _inTailPhase = true;
                _tailElapsed = 0f;
            }
            else
            {
                // 真实加载未完成 → 卡在阈值等待
                _displayProgress = _settings.fastFillThreshold;
            }
        }
    }

    if (_inTailPhase)
    {
        // Phase 2: 收尾阶段 → 当前进度平滑到 100%
        _tailElapsed += Time.deltaTime;
        float tailT = Mathf.Clamp01(_tailElapsed / _settings.tailDuration);
        _displayProgress = Mathf.Lerp(_settings.fastFillThreshold, 1f, tailT);

        if (tailT >= 1f)
        {
            _displayProgress = 1f;
            _fakeComplete = true;
        }
    }

    SetProgress(_displayProgress);
}

public override void OnResetDefault()
{
    base.OnResetDefault();
    _fastElapsed = 0f;
    _tailElapsed = 0f;
    _displayProgress = 0f;
    _inTailPhase = false;
    _fakeComplete = false;
    SkipFakeProgress = false; // 每次打开默认回到假进度模式，快速转场由业务侧显式开启
    SetProgress(0f);
    // 恢复默认：进度条/数字彻底关闭；打开时由 OnShow 按模式决定是否重新激活，
    // 关闭时保证复用/下次打开从干净状态开始（杜绝任何残留闪现）
    HideProgressVisuals();
    TransitionEffect?.HideAllImmediate();
}

// ── 外部方法 ──

/// <summary>
/// 手动设置加载进度（0-1），更新进度条和数字。
/// 如果不手动调用，OnUpdate 通过 timer 驱动假进度 0→1，不依赖真实加载进度。
/// </summary>
public void SetProgress(float progress)
{
    progress = Mathf.Clamp01(progress);

    if (_settings != null)
    {
        if (_settings.useProgressBar && Img_ProgressBar != null)
        {
            Img_ProgressBar.fillAmount = progress;
            Img_ProgressBar.gameObject.SetActive(true);
        }
        else if (Img_ProgressBar != null)
        {
            Img_ProgressBar.gameObject.SetActive(false);
        }

        if (_settings.useProgressNumber && Txt_ProgressNum != null)
        {
            Txt_ProgressNum.text = $"{(int)(progress * 100)}%";
            Txt_ProgressNum.gameObject.SetActive(true);
        }
        else if (Txt_ProgressNum != null)
        {
            Txt_ProgressNum.gameObject.SetActive(false);
        }
    }
}

/// <summary>假进度是否已完成</summary>
public bool IsFakeComplete => _fakeComplete;

/// <inheritdoc/>
public override bool IsTransitionReady
{
    get
    {
        // 无假进度（快速转场 / 未勾选进度显示 / 无配置）：真实场景加载完成即就绪
        if (!UseFakeProgress)
        {
            var sceneMgr = EmberSceneManager.Instance;
            return sceneMgr == null || !sceneMgr.IsLoading;
        }
        return _fakeComplete;
    }
}

/// <summary>
/// 是否需要假进度：未显式跳过，且进度显示（进度条/数字）至少勾选其一，且配置存在。
/// 快速转场（SkipFakeProgress）或未勾选任何进度显示时自动回退真实加载模式。
/// </summary>
private bool UseFakeProgress =>
    !SkipFakeProgress
    && _settings != null
    && (_settings.useProgressBar || _settings.useProgressNumber);

/// <summary>隐藏进度条与进度数字（快速转场模式）</summary>
private void HideProgressVisuals()
{
    if (Img_ProgressBar != null)
        Img_ProgressBar.gameObject.SetActive(false);
    if (Txt_ProgressNum != null)
        Txt_ProgressNum.gameObject.SetActive(false);
}

/// <summary>
/// 方块过渡效果（将绑定字段 Component 转换为 IEUITransitionEffect）。
/// 绑定代码生成器只能产出 Component 类型，这里做接口转换。
/// </summary>
private IEUITransitionEffect TransitionEffect => TransitionBlock as IEUITransitionEffect;

// ── 内部方法 ──

private void ApplySettings()
{
    if (_settings == null) return;
    SetProgress(0f);

    // 初始化进度条组透明度（自定义动画接管前先隐藏）
    if (Cg_Progress != null)
        Cg_Progress.alpha = 0f;
}

// ── 自定义过渡动画 ──

/// <summary>
/// 过渡动画进入阶段（Custom 槽）。方块扫入已由预设槽（EUITransitionBlock）完成，
/// 这里仅负责进度条组渐显。
/// </summary>
public override async UniTask OnCustomEnter()
{
    if (_settings == null) return;

    // 进度条组渐显
    if (Cg_Progress == null) return;
    var duration = _settings.customEnterDuration;
    if (duration <= 0f) { Cg_Progress.alpha = 1f; return; }

    float elapsed = 0f;
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        Cg_Progress.alpha = Mathf.Clamp01(elapsed / duration);
        await UniTask.Yield(PlayerLoopTiming.Update);
    }
    Cg_Progress.alpha = 1f;
    _fadeInDone = true;
    EmberEventBus.OnNext(EUIEvents.LoadingFadeInComplete);
}

/// <summary>
/// 过渡动画退出阶段（Custom 槽）。这里仅负责进度条组渐隐；
/// 方块扫出由预设槽（EUITransitionBlock）在自定义之后播放。
/// </summary>
public override async UniTask OnCustomExit()
{
    if (_settings == null) return;

    var exitDuration = _settings.customExitDuration;
    EmberEventBus.OnNext(EUIEvents.LoadingFadeOutStart, exitDuration);

    // 进度条组渐隐
    if (Cg_Progress != null && exitDuration > 0f)
    {
        float elapsed = 0f;
        var startAlpha = Cg_Progress.alpha;
        while (elapsed < exitDuration)
        {
            elapsed += Time.deltaTime;
            Cg_Progress.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / exitDuration);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }

    if (Cg_Progress != null) Cg_Progress.alpha = 0f;
    EmberEventBus.OnNext(EUIEvents.LoadingFadeOutComplete);
}
```

**自定义参数类 EUILoadingPageSettings**（GenerateCustomSettings，重新生成时若勾选会自动生成，无需手写；字段与当前一致）：

```csharp
[Serializable]
public class EUILoadingPageSettings
{
    [Header("进度显示")]
    public bool useProgressBar = true;
    public bool useProgressNumber = true;
    [Header("假进度")]
    [Range(0.5f, 10f)] public float fastFillDuration = 1.5f;
    [Range(0.3f, 0.9f)] public float fastFillThreshold = 0.6f;
    [Range(0.3f, 3f)]  public float tailDuration = 1f;
    [Header("自定义过渡动画")]
    [Range(0f, 3f)]    public float customEnterDuration = 0.3f;
    [Range(0f, 3f)]    public float customExitDuration = 0.2f;
}
```

> EUILoadingPage 的 usings：`Ember.Basic`、`Ember.Core`、`Ember.UI`、`UnityEngine`、`Cysharp.Threading.Tasks`（UniTask）。
> 注意：`NeedUpdate` 同 GMPage——骨架自带 `NeedUpdate => false`，补全时改为 `=> true` 并删掉 OnInit/OnHide 里的动态赋值（推荐）；或删掉 override 保留动态赋值。
> `SmoothTailDuration => 0f` 新骨架不含，需手动补。

### 6. EUIBackgroundPage（EUIBackgroundPanel，Background）

**无功能逻辑**（纯骨架，无绑定条目）。重新生成后无需补全。

---

## 三、页面注册表（GamePages.cs）当前条目

> 重新生成时 codegen 自动写回（框架模式 → GamePages.cs / 用户模式 → GamePages.User.cs），一般无需手动补。但若调整了预制体位置，`new("...")` 路径会按新位置更新，需同步确认。

```csharp
public static readonly EUIPageDef EUIBackgroundPage = new("Assets/Game/UI/Runtime/Prefabs/EUIBackgroundPanel.prefab", UILayer.Background, PageType.Background);
public static readonly EUIPageDef EUIMainPage       = new("Assets/Game/UI/Runtime/Prefabs/EUIMainPanel.prefab", UILayer.Normal, PageType.MainPage);
public static readonly EUIPageDef EUIGamePlayPage   = new("Assets/Game/UI/Runtime/Prefabs/EUIGamePlayPanel.prefab", UILayer.Normal, PageType.MainPage);
public static readonly EUIPageDef EUISettingPage    = new("Assets/Game/UI/Runtime/Prefabs/EUISettingPanel.prefab", UILayer.Popup, PageType.Popup);
public static readonly EUIPageDef EUILoadingPage    = new("Assets/Game/UI/Runtime/Prefabs/EUILoadingPanel.prefab", UILayer.TopMost, PageType.TopMost);
public static readonly EUIPageDef GMPage            = new("Assets/Game/UI/Runtime/Prefabs/GMPanel.prefab", UILayer.TopMost, PageType.FreePage, freePageSortingOrder: 30000);
```

## 四、当前生成路径配置（调整位置的参照）

- 代码生成根：`EmberCSharpImplementation.asset.codePath = Assets/Game/UI/Runtime`（各页面 binding.CodePath 可单独覆盖）
- **UI 资源根（2026-09-01 调整）**：`Assets/GameResource/Resources/UI`。框架模式路由到 `Common/Prefabs`；用户模式从 `classPath` 首段取模块名，路由到 `Module/<模块>/Prefabs`。
- 旧预制体位置（迁移前）：`Assets/Game/UI/Runtime/Prefabs/`
- 页面注册表：`pageDefFile = Assets/Game/UI/GamePages.User.cs`（框架模式自动改写为 GamePages.cs）
- 基类：`Ember.UI.EUILogic`
- 业务新页面 classPath 约定：`<模块>/Page/<类名>`、`<模块>/Component/<类名>`（框架演示页保持现状目录）

## 五、补全操作指引（生成完代码后）

1. 编译无报错后，按第二节逐个页面补功能逻辑（保留新「页面配置」区成员）
2. 控件绑定名必须与第二节清单一致（prefab 重建后重新做 EUIBinding 条目时用同名）——不一致则补全代码编译报错，按报错对齐名称
3. 补全后提醒：**等编译完全结束后**再 Play 验证：主菜单→设置→返回 / 进入玩法→返回 / GM 页时间缩放 / 场景切换 Loading 假进度 / 遮罩点击
4. 验证通过后由用户在「模板编辑器」手动保存模板（助手不执行 sync-scaffold.ps1）

## 六、风险清单（操作前注意）

| 风险 | 对策 |
|---|---|
| 场景内对已删预制体的实例变 Missing Prefab | 删除前先处理场景实例（用户方案：主界面保留非预制体 UI）；MainScene/GameplayScene/SettingsScene/FrameworkScene 逐一确认 |
| 误删 EUIBootSplash 等 | 不受影响（非 EUIBinding 页面） |
| GMPage/EUILoadingPage 功能重、绑定多 | 建议最后处理或保留不删，重新生成实验用 1-2 个页面验证流程即可 |
| 补全后与模板镜像不一致 | 补全验证后由用户在模板编辑器保存 base |
| 操作不可恢复 | **开始前先 git 提交干净状态**（git 由用户执行） |

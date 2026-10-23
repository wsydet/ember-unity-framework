# Loading 会话：公开契约与 Windward 接入

2026-10-23，随 Ember 0.21.4 交付。旧版 0.21.3 不包含本页新增遮幕 API。Windward 的 Ember 0.21.3 / Unity 6000.5.4f1 / platformer-2d 0.11.1 已签收基线保持不变；后续经独立授权，通过 UPM Manager 升级框架至 0.21.4，再按本页接入并人工验收。无需为本功能重新部署模板。

## 三层职责

| 层 | 实现 | 职责 |
|---|---|---|
| 动画 | EUITransitionBlock、IEUIControlledTransitionEffect | 颜色、网格、方向、曲线、进入/退出；不认识场景或恢复事务 |
| 通用会话 | EUICurtainSession、EUIManager.Curtain | 页面所有权、覆盖渲染屏障、保持、揭幕、暂停、取消、释放 |
| 业务适配 | EUIManager.SceneLoading 或消费项目编排 | 决定覆盖下做什么、何时揭幕；场景适配在此登记 HoldPresentation |

BeginCurtain 打开的就是传入的正式 Loading 页面，不另建第二层遮罩。场景自动流程使用相同接口；其他业务无须创建场景请求。

EUILogic.TransitionOperation 保留为旧进度展示的兼容输入，由场景适配赋值。已有生成页面可以读取其准备结果；通用会话不认识该请求，不依赖场景事件、进度计时或 WhenInteractive 来决定揭幕。

## 公开 API

命名空间 Ember.UI，全部在 Unity 主线程调用；页面必须为 PageType.TopMost。

| API | 语义 |
|---|---|
| EUIManager.BeginCurtain(pageDef, args, options, cancellationToken) | 立即返回独立句柄，进入期间即可暂停 |
| EUIManager.CoverAsync(...) | Begin + 等待 Covered 的便利形式 |
| session.Covered | 进入结束、跨过 Unity EndOfFrame 渲染和下一帧后成功；此后才可阻塞主线程 |
| session.SetPaused(bool) / IsPaused | 冻结/继续本次受控方块动画 |
| session.RevealAsync(cancellation) | 显式揭幕，等待退出与页面 Cleanup/活动列表清理 |
| session.RevealFinished | 可提前订阅的同一次揭幕结果 |
| session.State | Covering → Covered → Revealing → Revealed；异常释放为 Released |
| session.Cancel / Dispose / DisposeAsync | 立即终止并清理本次页面；不播放补偿退出。DisposeAsync 返回 UniTask |

EUICurtainOptions 在创建时复制：

| 属性 | 默认 | 说明 |
|---|---|---|
| EnterDuration / ExitDuration | -1 / -1 | 分别负值沿用组件设置，0 立即采样终态，拒绝 NaN/Infinity |
| UseScaledTime | false | 默认非缩放时间，内部 timeScale=0 不阻塞 |
| InitiallyPaused | false | 接入用户暂停订阅之前就冻结 |
| SkipFakeProgress | false | ResetDefault 后、Init/Show 前注入，跳过进度显示及假计时，避免首帧闪现 |
| SkipCustomTransition | false | 跳过自定义 Enter/Exit 槽，例如进度组淡入淡出；方块仍播放 |

暂停/时长作用于 IEUIControlledTransitionEffect，本次内置实现为 EUITransitionBlock。会话按受控接口直接发现组件，不要求它额外实现旧 IEUITransitionHandler。旧自定义 UniTask 动画、Animator、第三方过渡仍遵循自身时钟和取消契约，不承诺可被自动冻结。Windward 无进度方案设置两个 Skip 选项，只使用受控方块，不启动旧进度动画任务。普通自动场景 Loading 默认保留已有进度淡入淡出与非缩放时间。

## 非场景工作示例

下面 userPause 和业务方法由项目实现，框架 API 可直接使用：

```csharp
var loading = EUIManager.Instance.BeginCurtain(
    GamePages.EUILoadingPage,
    options: new EUICurtainOptions {
        EnterDuration = 0.75f, ExitDuration = 0.6666667f,
        InitiallyPaused = userPause.IsPaused,
        SkipFakeProgress = true, SkipCustomTransition = true
    }, cancellationToken: lifetime);
var pauseBinding = userPause.Subscribe(loading.SetPaused);
try
{
    await loading.Covered;
    var candidate = await PrepareBusinessAsync(lifetime);
    lifetime.ThrowIfCancellationRequested();
    CommitBusiness(candidate);
    await StabilizeCommittedBusinessAsync();
    await WaitForRevealPermissionAsync();
    await loading.RevealAsync();
}
finally
{
    pauseBinding.Dispose();
    await loading.DisposeAsync();
}
```

此例要求项目已为提交失败设计稳定回退/错误界面。需要保持黑幕修复时，在内层 catch 中完成恢复或错误界面切换，再离开外层 finally。Dispose 只释放表现，不会使业务状态自动恢复正确。

Covered 保证正常渲染 PlayerLoop 的时序，不证明任意自定义素材确实不透明。页面须铺满视口，覆盖终态 alpha/scale/位置正确，其他 UI 不应盖在上方。编辑模式或无画面的批处理不具备屏幕覆盖保证。

## 生命周期与取消

- Manager 同时只接受一个会话；并发 Begin/Cover 抛 InvalidOperationException，不抢占、不隐式排队。串行需求由业务排队。
- Covered 后无限保持。Covered 前 Reveal 抛 InvalidOperationException，原会话仍可继续；重复 Reveal 共享第一次任务，后续令牌不替换首次令牌。
- Begin/Cover 令牌只控制取得覆盖。取得后该令牌迟到取消不会自动揭开已提交业务。业务必须继续管理自己的取消和 finally 清理。
- Reveal 首次令牌取消立即放弃本次会话。正常成功 State 为 Revealed，取消/释放为 Released；Dispose/Cancel 幂等。
- Cancel/Dispose 移除所属 TopMost、活动/关闭追踪，隐藏视图、停止方块/定向 Tween、调度销毁。Unity 原生对象 Destroy 在帧末回收；不冒充同步物理销毁。
- 通过公共 ClosePage/ClosePageByDef 关闭会话页面视为取消；应使用 RevealAsync 完成正常揭幕。清理回调抛错仍继续移除页面、结束等待，并以故障结果报告。资源回调每次获取只接受一次，迟到获取只归还自己的引用。
- 页面在进入/保持/退出时被销毁，或 Manager 销毁，相关等待会终止。动画异常使任务失败。旧操作、旧取消或迟到回调不能释放新会话。
- 未完成的等待在取消时为 UniTaskStatus.Canceled；该项目 UniTask.AsTask 的适配可能表现为带 OperationCanceledException 的 Faulted Task，不以 Task.IsCanceled 单独判定。
- 会话不取得 PlayerModule.Pause、输入锁或业务 CTS。项目负责释放这些所有权；失败路径也不能遗留内部暂停。
- 不可逆提交后，场景适配不再用请求令牌中断收尾，仍报告实际 CommitFailed/CleanupFailed。手动业务自行完成稳定收尾后再揭幕，取消不代表回滚。

## 场景兼容与完成阶段

RunWithLoadingAsync、默认场景 Loading、RequestInPlace 保留自动闭幕 → 操作 → 展示收尾 → 揭幕。RunLoadingPage 使用会话等待 presenter 就绪。全局 Loading 事件保留通知用途，不再作为跨请求完成凭证。

外部会话已 Covered 时，场景适配复用它并登记展示持有，内部只执行请求工作，外部决定揭幕。尚在 Covering/Revealing 时不接受新场景请求。

| 边界 | 含义 | 是否等待外部遮幕 |
|---|---|---|
| operation.WhenPrepared | 必要准备成功 | 否 |
| operation.WhenRunning | 进入/原位提交生命周期成功，不保证旧场景清理已结束 | 否 |
| operation.Completion | 本请求执行及场景清理完成；自动拥有 Loading 时也包含其退出 | 外部持有时不等 |
| loading.RevealAsync / RevealFinished | 揭幕和页面清理完成 | 是 |
| operation.WhenInteractive / CanInteract | 请求、清理、真正展示拥有者均成功 | 是 |

阶段在失败时也会结束，必须检查 EmberTransitionResult.Status。玩家动作许可仍可比 WhenInteractive 更晚。

受支持的顺序：外层 Covered → 内层 RecoverAsync 等 Completion → 释放内部 Pause → 业务触发 Reveal → WhenInteractive。禁止在决定 Reveal 的内部逻辑先等待 WhenInteractive。现有阶段接口足以编排，本次没有更改它们或伪造提前成功。

## Windward 正式接续步骤

1. 等包含本契约的正式版本，再按单独授权升级；当前版本号未变不代表消费包已补齐。无需重新部署平台模板。
2. 在 WindwardLife 原单次死亡会话中取得 Loading 句柄，保留死亡去重、计数、音效、一次镜头冲量、挑战区/安全区计时。约死亡 0.53333336 秒开始 0.75 秒闭幕；等待 Covered，且达到原 1.3333334 秒 placement 门槛后再调用 PlayerCheckpointRecovery.RecoverAsync。闭幕较慢时延后恢复，不能提前世界复位。
3. 保留 WorldResetScope → PlayerModule.RestoreCheckpoint → RequestInPlace<GameplayState>。不加载或卸载关卡。内层 Completion 返回时外部仍 Covered，因此恢复函数可以释放自己的 Pause。返回 false 时处理 LastError，结束项目恢复暂停和输入所有权，不能继续当作成功复生。
4. 用独立的用户暂停信号接 SetPaused。不要将 timeScale=0 或恢复事务引发的所有 PausePresentation 通知都解释为用户暂停；否则会重新引入等待。创建前用户暂停通过 InitiallyPaused 传入，结束时注销订阅。
5. 原复生继续拥有 0.5 秒隐藏 + 0.53333336 秒动作、保护及输入清理。原遮幕 DeathElapsed=1.5333333 的展开门槛，相对 placement 约为 0.2 秒：可由恢复成功后的复生时钟发出揭幕许可，较慢恢复先保持。此处等待的是复生的“允许揭幕”信号，不是整个 WindwardLife.Recover（后者还等复生退出）。0.6666667 秒揭幕与复生并行，控制恢复仍由 RebirthExited 的成功条件决定。
6. 死亡会话设置 SkipFakeProgress=true、SkipCustomTransition=true；正式 EUI 配置同时关闭进度条/数字。参数通过 EUI 开发中心调整/生成，不修改 Binding、EmberManaged 块、私有字段或 PackageCache。死亡专属外观可用项目专用页面定义复用相同 Loading 逻辑和方块，不覆盖普通加载的默认样式。
7. 新接线通过增量人工验收后再去掉旧 EUIWindwardRebirthPage 的遮幕显示；保留复生动作与音频业务。

### WindwardLife 的具体修改点

以下按只读核对的 `WindwardLife`、`WindwardFlowModule`、`GameSettingsState`、`PlayerCheckpointRecovery` 编写，供升级后实施；本轮没有写入 Windward，也没有声称它已在 Windward 编译/人工验收。通用示例中的 userPause、rebirth 信号不是现有项目 API；下面不依赖那些占位方法。

在 WindwardLife 增加字段；保留现有 OnDeath 的去重、计数、声音和冲量，只替换 Recover 的编排。页面定义可先使用现有 `Game.UI.GamePages.EUILoadingPage`，最终在 EUI 开发中心建立死亡专属定义并换入，避免普通加载共用死亡风格。

```csharp
private EUICurtainSession _deathCurtain;
public void SetUserPaused(bool paused) => _deathCurtain?.SetPaused(paused);

private async UniTaskVoid Recover(PlayerModule module, CancellationToken cancellation)
{
    EUICurtainSession curtain = null;
    var flow = Flow; // 退出会话可能清空 Flow；捕获本次拥有者。
    try
    {
        // DeathElapsed 仍由 WindwardFlowModule.FixedUpdate -> AdvancePresentation 推进。
        await UniTask.WaitUntil(() => DeathElapsed >= 0.53333336f,
            cancellationToken: cancellation);
        curtain = EUIManager.Instance.BeginCurtain(Game.UI.GamePages.EUILoadingPage,
            options: new EUICurtainOptions {
                EnterDuration = 0.75f, ExitDuration = 0.6666667f,
                UseScaledTime = false, InitiallyPaused = flow.UserPaused,
                SkipFakeProgress = true, SkipCustomTransition = true
            }, cancellationToken: cancellation);
        _deathCurtain = curtain;
        await curtain.Covered;
        await UniTask.WaitUntil(() => DeathElapsed >= _beforePlacement,
            cancellationToken: cancellation);
        cancellation.ThrowIfCancellationRequested();

        IsWorldRecovery = true;
        RebirthPending = _player.ObservationGraph.Nodes.Any(n => n.Id == "windward-rebirth");
        try
        {
            bool restored = await Recovery.RecoverAsync(module, GameLauncher.Instance.Fsm,
                WorldResetReason2D.Death, cancellation);
            if (!restored) throw new InvalidOperationException(Recovery.LastError);
        }
        finally { IsWorldRecovery = false; }
        cancellation.ThrowIfCancellationRequested();
        RefreshRegion();

        // Recovery 已等到内层 Completion，并释放成功事务的 Pause。
        // 只等复生时钟的展开门槛；不能等待整个 Recover 或 WhenInteractive。
        if (RebirthPending)
        {
            await UniTask.WaitUntil(() => RebirthElapsed >= 0.2f || !RebirthPending ||
                _rebirthAborted || RecoverySessionEnded, cancellationToken: cancellation);
            if (_rebirthAborted || RecoverySessionEnded)
                throw new InvalidOperationException("复生阶段被中断或玩家已停止。");
        }
        var reveal = curtain.RevealAsync(cancellation);
        if (RebirthPending)
        {
            await UniTask.WaitUntil(() => !RebirthPending || _rebirthAborted || RecoverySessionEnded,
                cancellationToken: cancellation);
            if (_rebirthAborted || RecoverySessionEnded)
                throw new InvalidOperationException("复生阶段被中断或玩家已停止。");
        }
        await reveal;
        _dying = false; CompletedRecoveries++;
        flow.Clock.SetPhase(WindwardRunPhase.Playing, Time.realtimeSinceStartupAsDouble);
        await flow.SaveAsync();
    }
    catch (Exception error)
    {
        // 生命周期取消由 ReleaseSession/宿主退出负责；没有承诺回滚已复位世界。
        if (!SessionReleased)
        {
            LastError = error.ToString();
            flow.Clock.SetPhase(WindwardRunPhase.Faulted, Time.realtimeSinceStartupAsDouble);
            // 采用明确的终止策略：不在半恢复状态恢复输入，不伪装成功复生。
            // RecoverAsync 返回 false 会保留 Pause；禁用组件调用它现有 OnDisable，
            // 该路径 StopSession 并释放 _pause。没有调用私有 ReleasePause。
            var recovery = Recovery;
            recovery.enabled = false;
            module.StopSession();
            // 项目错误提示/回主菜单应接在此 Faulted 路径；不可保存失败快照。
        }
    }
    finally
    {
        curtain?.Dispose();
        if (ReferenceEquals(_deathCurtain, curtain)) _deathCurtain = null;
    }
}
```

同时在 `WindwardLife.ReleaseSession()` 的幂等保护之后、取消 `_sessionLifetime` 之前增加：

```csharp
_deathCurtain?.Dispose();
_deathCurtain = null;
```

保留 `WindwardRebirthState` 的 HiddenSeconds=0.5、AnimationSeconds=0.53333336、保护及 Action.End；上述代码没有解除玩家动作锁。揭幕从复生约 0.2 秒开始，正常在约 0.8667 秒结束，动作在约 1.0333 秒结束；实际进入/固定步会有一帧量化误差。慢恢复只延长 Covered 保持，不提前推进复生。若将来另调揭幕更慢，动作许可仍由复生状态决定。

### 真正的用户暂停接线

在 `WindwardFlowModule` 新增以下**项目方法**，不是框架已有 API：

```csharp
public bool UserPaused { get; private set; }
public void SetUserPaused(bool paused)
{
    UserPaused = paused;
    Active?.SetUserPaused(paused);
    Clock.SetUserPaused(paused, Time.realtimeSinceStartupAsDouble);
}
```

`ResetModuleData` 重置 UserPaused=false；`Bind` 中 actor.Bind(this) 后调用 actor.SetUserPaused(UserPaused)。该模块 Update 原有音频 PausePresentation 调用保留，但 Clock.SetUserPaused 的条件改为 UserPaused，移除 `PlayerModule.State == Paused && !IsWorldRecovery` 的猜测：用户可能在内部恢复暂停期间打开设置。

在 `GameSettingsState` 增加 `Game.WindwardFlow.WindwardFlowModule _pausedFlow`；OnSettingsEnter 在取得 PlayerModule.Pause 之前执行：

```csharp
if (EmberModuleCollector.Instance.TryGetModule<Game.WindwardFlow.WindwardFlowModule>(out var flow))
{
    _pausedFlow = flow;
    flow.SetUserPaused(true);
}
```

OnSettingsExit 在原 `_worldPause.Dispose()` 之后执行：

```csharp
_pausedFlow?.SetUserPaused(false);
_pausedFlow = null;
```

当前设置 Push/Pop 是单一用户暂停拥有者；若后续增加其他独立暂停来源，项目要将 bool 升级为拥有者集合/计数后汇总给 SetUserPaused。不得把 `WindwardLife.PausePresentation` 直接接到遮幕，它也接收事务内部暂停。

旧页面移除时，删除 WindwardLife.Bind 内 ShowTopMost(WindwardRebirth) 接线及对应页面可用性门槛，不再等待 PresentationReady；保留 Flow.FixedUpdate 的 AdvancePresentation、原死亡/复生 FSM 动作与声音。修改生成文件时仅改用户逻辑区，页面声明/绑定通过正式 EUI 生成流程更新。先完成人工接入验收，再删除已无引用的旧页面资产。

Windward 后续验收：真实接触死亡仅一层遮幕；慢恢复持续覆盖；不卸载同场景；进入/退出各用户暂停 0.15 秒后连续播放；内部暂停不死锁；重复死亡、退出/取消不留页面；计数/声音/冲量一次；隐藏、动作、控制时长保持；挑战区死亡复生计时、安全区停表；普通出口自动 Loading 正常。框架测试通过不等于这批人工接入已通过。

## 参数预览

正式 Loading Prefab 的临时 contents/克隆，未保存或更改默认 Prefab：1920×1080，固定列数3得到3×3；Lines 水平分组；进入 TopLeft，退出组件默认反向；进入0.75、退出0.6666667、错开0.08秒；纯黑；X/Y Scale及Alpha恒1；Rotation/YPosition为0；XPosition=EaseInOut(0,3,1,0)。

同行三块拼成横向条带，实帧可形成分层擦入/擦出。方块边长640，网格总高1920，16:9裁切后可见条带约220/640/220高，不是原版等高条带逐像素复刻。其他视口比例须重新预览。试验参数未写成所有项目默认外观。

EditorPlayEnterPreview / EditorPlayExitPreview 自行初始化 RectTransform/网格；EditorPreviewStop 清理池与 Tween。Inspector 按钮、PrefabUtility.LoadPrefabContents 都可直接使用，不需反射 Awake；finally 卸载 contents。

## 验证范围

验证包含真实 EUI 页面、真实方块、真实当前生成 Loading 进度逻辑，以及可控场景端口的事务集成。后者不是 Windward 实景死亡或真实磁盘加载测试。当前 Assets 与 base、platformer-2d 封存的 Loading 逻辑 SHA256 一致，本次没有模板初始接线改动、保存、Bump 或封存改动。

开发仓库实施证据：docs/dev/loading-curtain-delivery-20261023.md；实帧、采样及作业记录：docs/dev/evidence/loading-curtain-20261023/。这些开发证据不随 UPM 包分发。

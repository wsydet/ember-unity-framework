# 通用层级状态机

实现位于 `Core/Runtime/Fsm/`，命名空间 `Ember.Core.Fsm`，程序集 `Ember.Core.Runtime`。
`EmberFsm<TContext>` 是由宿主持有、显式驱动的普通 C# 对象。现有 `EmberStateMachine`
继续管理 Init/Main/Gameplay、场景转场和覆盖状态。本功能不增加全局 Manager。

## 资产与实例

采用 SO 配置 + C# 运行对象：具体 SO 继承 `EmberFsmStateDefinition<TContext, TState>`，
其 `CreateState()` 通过普通构造函数返回新的 `TState`，无需反射、类型名字符串或克隆 SO。
具体、非泛型派生类可以使用 `CreateAssetMenu`；开放泛型基类自身不是可创建的项目资产。

宿主只提供 `EmberFsmGraph` 与上下文。构造状态机时校验并保存节点、父子关系、目标绑定和转换声明
的只读结构快照；首次 `Start()` 才为每个节点创建对象，并调用一次 `Initialize`。之后进入退出复用
这些对象。同一 SO 可供多宿主、多节点引用；工厂必须每次返回新对象，内核会拒绝复用已有对象的工厂。
SO 参数也应当作只读配置，建议在工厂中复制到运行对象，运行时不向资产保存计时器、上下文或行为字段。
结构快照不深复制 SO 参数。修改资产后需重新装配新状态机才能可靠应用全部配置变化。

`TContext` 为引用类型，并必须与各定义的泛型上下文类型**相同**；实际上下文对象可以是它的派生类。
例如定义使用 `ActorContext` 时，宿主创建 `EmberFsm<ActorContext>`，即使传入 `PlayerContext` 对象。

`EmberFsmNode.Id` 是图内稳定的序列化字符串。新节点默认生成 GUID，也可由资产生产工具分配稳定 ID；
显示名称、类型和 SO 引用均不决定身份。Unity Inspector 复制数组元素会复制 ID，复制后需分配新 ID
并明确调整引用；启动前校验拒绝重复 ID，不会悄悄重写连线。图内 ID 可在其他图中重复。
父关系以单个 `ParentId` 表达，每节点至多一个父节点；每个有孩子的节点必须指定默认**直接**子节点。

`graph.Inspect()` 可在编辑态无角色时读取结构和 `Errors`。指定 `Inspect(typeof(MyContext))` 可附加类型检查。
检查涵盖入口、重复/空 ID、定义缺失、上下文及声明类型、非法父节点、层级环、默认子节点、
重复/缺失/未声明的绑定、不存在的目标及重复转换 ID。无效图在状态机构造时抛出 `ArgumentException`，
并且不执行工厂或生命周期。声明 getter 抛异常会成为校验错误。

## 声明与请求

行为类持有 `static readonly EmberFsmTransition<TContext, TState>`，定义 SO 的 `GetTransitions()`
返回这些相同句柄。描述包含转换 ID、目标槽、说明、模式及可选的强类型 Guard。节点将目标槽绑定到
具体节点 ID。多个声明可以共用同一目标槽，不同节点可以给同一行为的槽绑定不同目标。

`activation.Request(handle)` 标记来源为 `State`；宿主的 `fsm.Request(activation, handle)` 标记为
`External`。二者都要求来源节点在本次激活中有效，并且传入该节点的原始声明对象。
仅创建一个 ID 相同的新描述不能冒充声明。没有绕过声明、Guard 或预检的外部强制跳转 API。
事件处理也使用相同入口，内核不负责事件总线或主动轮询 Guard。

`Enter(EmberFsmActivation)` 每次得到不同的激活序号。延迟操作、异步续体或订阅闭包必须在发起时
捕获该值。退出再进入后，旧值仍返回 `StaleActivation`。不要在延迟回调中读取便利基类的最新
`Activation` 或重新调用 `GetActivation`，那会主动申请一个新的有效凭证。
所有控制调用必须回到创建状态机的线程；后台线程调用会抛 `InvalidOperationException`。

请求按“凭证 → 原始声明 → 绑定 → Guard → 进入预检 → 退出/进入”同步执行。
返回值为 `EmberFsmResult`：`Changed` 表示已完成操作，`NoChange` 表示路径相同；
`GuardRejected`、`EntryRejected`、`InvalidTarget`、`UndeclaredTransition`、`StaleActivation`、
`RedirectCycle`、`RedirectLimit`、`Reentrant`、`NotRunning`、`Paused`、`Faulted`、`Disposed`
分别指出拒绝或故障原因。没有请求队列。

普通请求保留公共祖先；从子到父退出，从父到子进入。以父节点为目标时沿默认子节点解析到叶子，
明确指定叶子时直接进入它的祖先路径。目标为已激活祖先时，保留祖先并转入其默认子路径；
最终路径相同则不执行预检、进入或退出（请求 Guard 仍校验一次）。
声明的 `ReenterTargetBranch` 模式从目标节点本身起退出重进整个分支，重置该分支激活时间。
重定向后，该模式作用于最终目标分支。不自动恢复上次子节点。

```text
Alive                       Idle → Move：退出 Idle；进入 Move
  Ground                    Move → Jump：退出 Move、Ground；进入 Air、Jump
    Idle / Move             Fall → Dead：退出 Fall、Air、Alive；进入 Dead
  Air
    Jump / Fall
Dead
```

父状态也能使用自己的声明发起转换。声明来源节点与当时活动叶子可以不同。

## 请求位置与统一尝试

转换可以通过可选的 `sites` 参数声明 `EmberFsmRequestSite<TContext,TState>`。每个位置包含 ID、
正常触发阶段、所在段、段内 Order、完整只读条件、继续策略和可选提交动作。同一节点的同阶段同段
不能出现相同 Order；同一转换内的位置 ID 不可重复。位置条件与旧 Guard 不能同时声明。
这仍是代码声明，不增加规则资产、自动调度或全图轮询。

在原生命周期位置调用 `Activation.AttemptGroup("decision")`，默认选择 FixedUpdate 阶段；
Update 或事件段需显式传入对应阶段。内核只执行该节点、该段的声明，按 Order 升序尝试。
调用方必须检查返回值的 `ShouldStop`，为 true 时结束当前回调。不同段之间仍可执行原有计时或运动。
阶段只描述通常调用位置，诊断的 ActualPhase 使用真实派发阶段；事务外独立请求记作 External。

单独尝试使用 `activation.Attempt(handle, siteId)` 或 `fsm.Attempt(activation, handle, siteId)`。
原 Request 共用同一内核：未迁移声明保留旧 Guard；单位置声明可省略位置，多位置缺失时返回
`RequestSiteRequired`，未知位置返回 `InvalidRequestSite`，均不查询条件。只尝试指定位置，
不会补跑同段更高优先位置或推进计时。

每次有效尝试仅查询一次完整条件，false 返回 GuardRejected；true 后仍独立执行 CanEnter 与重定向。
`EmberFsmAttemptResult.Matched` 区分条件匹配与转换成功。UntilChanged 对 GuardRejected、
EntryRejected、NoChange 继续；UntilMatched 在条件匹配后即停止当前回调，即使目标普通拒绝。
Changed、故障或协议错误都停止调用链。已迁移请求/Attempt 的协议错误终止本轮后续子派发并原样
返回宿主，普通协议错误不自动 Faulted。事务内嵌套请求返回 Reentrant，不污染外层事务。

最终路径完全预检且确需改变后，执行该位置的 commit，随后旧路径 Exit、新路径 Enter。
NoChange、普通拒绝或重定向协议错误不执行 commit。commit 异常记录 Commit 阶段并沿原故障
协议清理；已提交资源不自动退款。既有 Enter 的消费仍留在 Enter，不能在 commit 重复扣费。
Stop/Dispose 不查询位置条件或执行 commit。

`EmberFsmDiagnostic.Attempt` 保存该次位置、派发关联、激活代数、是否查询及结果、请求目标、
预检/重定向经过、提交开始/完成、转换前路径。诊断本体保留最终目标、结果、阶段、异常与转换后
路径。短路的位置为 Skipped，SkipReason 说明原因；其他未进入的段/节点不生成虚假 false。
历史继续按 historyCapacity 有界保留，关闭后不回填；读取蓝图/观察不会重评条件。来源关联校验
包含位置元信息，编辑后的新说明不能映射到旧冻结实例。

蓝图所选转换展示所有位置与继续/提交说明，可打开对应状态源码。运行区将活动路径与详细宿主
指标分开；历史显示采集事实，详细指标可折叠滚动。位置仍属于原转换，多个位置不会增添图边。

## 进入预检与故障

`CanEnter()` 只对需要新进入的节点按父到子调用，并在任何旧状态退出**之前**完成。
它返回 `EmberFsmEntryDecision.Allow`、`Reject` 或 `Redirect(slot)`。
重定向槽必须由定义的 `GetRedirectSlots()` 预先声明并在该预检节点上绑定，不能返回任意节点 ID。
因此蓝图可以枚举可能的重定向目标。重定向重新解析并预检最终路径，检测目标循环，并受构造参数
`redirectLimit`（默认 16 次重定向）限制；中间节点不进入。返回最终真实结果，`LastOperation.TargetId`
记录最终解析到的目标。拒绝/无效重定向/成环/超限不退出旧路径。

Guard 和预检应只做判断，不申请激活资源、不修改生命周期状态。重定向可能让相同祖先的预检执行多次。
声明读取、工厂、初始化、Guard、预检、Enter、Exit、Pause、Resume、Dispose 和诊断通知期间，
内核拒绝嵌套控制，返回 `Reentrant`。嵌套 `graph.Inspect()` 抛异常并形成外层声明错误。
`Dispose()` 遇到重入会抛出明确异常；在可能重入的调用点用 `TryDispose()` 检查结果。

回调异常进入 `Faulted`，停止调度并记录首个 `LastFault`（节点、阶段、原异常及堆栈、故障时路径）。
随后从子到父尝试退出剩余活动节点，并按创建顺序的逆序释放所有已创建对象。
已经尝试过的 Exit 不重试；部分执行的 Enter 也会尝试 Exit；部分 Initialize 的对象仍会 Dispose。
每个清理回调至多尝试一次，清理异常计入 `CleanupFaultCount`，不覆盖首个故障。
这不保证回滚任意业务副作用，也无法中断不返回的用户回调。故障实例不能重新 Start，宿主应最终 Dispose
并根据业务需要重新装配。工厂抛出之前自己申请的资源需要工厂自行清理。

`Exit` 负责本次激活资源（订阅、任务取消、临时效果），`Dispose` 负责对象全寿命资源。
业务应使用 `try/finally` 保证自己的释放逻辑，并允许 Dispose 在部分初始化/激活后执行。
正常 `Stop()` 只退出活动路径，保留对象及初始化结果，允许再次 `Start()`。
`TryDispose()` / `Dispose()` 最终释放全部对象，之后不能启动。最终释放中发生异常时，
`TryDispose` 返回 `Faulted`，状态仍为 `Disposed`，错误保存在 `LastFault`。

## 驱动、时间和暂停

宿主显式调用 `Update(deltaTime)` 和 `FixedUpdate(fixedDeltaTime)`，二者均按父到子调度。
转换成功后结束本轮路径派发，旧子节点和新分支都不再收到本轮 Tick。
内核不能中断正在执行的用户方法，因此成功切换后应及时 `return`。
嵌套驱动返回 `Reentrant`，负数、NaN 和无穷时间在运行态被拒绝。

`GetElapsedTime(nodeId)` 使用**逻辑 Update 时间域**（double 累计）。每次 Update 在回调前给
整条原活动路径增加宿主传入的时间；FixedUpdate 不重复累计。新进入节点从 0 开始，公共祖先继续累计。
退出节点保留最后持续时间供查询，下次进入清零；业务字段由 Enter 自行重置。缩放时间由宿主传入，
不读取 `Time.time`、Animator 或物理状态。

Pause、Resume 通知均为**父到子**。暂停整条路径，停止 Tick 和计时，拒绝普通转换，允许 Stop。
宿主自行暂停物理、动画、音效等系统；内核不假定它们存在。

## 诊断

`InstanceId` 区分宿主；`Description` 按节点提供显示名称、定义资产、层级、转换句柄及槽绑定。
`GetActivePath()` / `CurrentLeafId` / `GetElapsedTime()` 提供当前信息；`LastOperation` 提供最近一次
有记录的控制操作，`LastFault` 始终保存首个运行故障。Tick 的返回结果应由宿主检查，普通 Tick 不写历史。
`Diagnostic` 事件与 `GetHistory()` 返回不可变路径快照，包含实例 ID、序号、请求来源、声明来源节点、
转换 ID、解析目标、当前叶子和完整路径。故障记录还包含节点、阶段与原始异常。

历史容量由构造参数固定（默认 64，0 关闭历史），超出时淘汰最旧项。
`DiagnosticsEnabled = false` 关闭事件和历史追加，仍保留必要的 LastOperation/LastFault，不改变生命周期。
重新开启不会回填，已经存在的历史继续保留。观察者异常被隔离、计入 `ObserverFaultCount`，不使运行故障；
观察者不能在通知事务中切换。观察工具只读快照，不重新执行 Guard，也不向 SO 写入运行信息。
重入等在控制入口立即拒绝的调用不递归发送诊断，以免观察者制造通知循环。
不要用可关闭的诊断历史作为连招等玩法依据。

## 最小资产驱动示例

以下脚本由项目拥有，放在项目业务目录；具体 SO 与 MonoBehaviour 使用同名文件。
示例不需要控制器逐个创建或注册状态。

```csharp
using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

public sealed class LampContext
{
    public bool Enabled;
    public int PulseCount;
}

public sealed class LampState : EmberFsmState<LampContext>
{
    public static readonly EmberFsmTransition<LampContext, LampState> Next =
        new EmberFsmTransition<LampContext, LampState>("next", "next", (state, ctx) => ctx.Enabled,
            description: "启用后，到时进入下一节点");
    private readonly float _duration;
    private float _elapsed;
    public LampState(float duration) { _duration = duration; }
    public override void Enter(EmberFsmActivation activation)
    {
        base.Enter(activation);
        _elapsed = 0; // 明确重置本次业务数据
    }
    public override void Update(float deltaTime)
    {
        _elapsed += deltaTime;
        if (_elapsed >= _duration && Activation.Attempt(Next).ShouldStop)
            return; // 成功、故障或协议终止后不继续本次回调
        Context.PulseCount++;
    }
    // 若订阅事件：在 Enter 中捕获 var token = activation，回调使用 token.Request(Next)，Exit 取消订阅。
}

// LampDefinition.cs：创建两份此类型资产即可拥有不同参数，不新增行为类型。
[CreateAssetMenu(menuName = "Game/FSM/Lamp")]
public sealed class LampDefinition : EmberFsmStateDefinition<LampContext, LampState>
{
    [SerializeField] private float _duration = 1;
    public override LampState CreateState() => new LampState(_duration);
    public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new[] { LampState.Next };
}

public sealed class LampRootState : EmberFsmState<LampContext> { }

// LampRootDefinition.cs：父节点具有自己的对象和生命周期，不要求 LampState 继承 LampRootState。
[CreateAssetMenu(menuName = "Game/FSM/Lamp Root")]
public sealed class LampRootDefinition : EmberFsmStateDefinition<LampContext, LampRootState>
{
    public override LampRootState CreateState() => new LampRootState();
    public override IReadOnlyList<EmberFsmTransition> GetTransitions() => System.Array.Empty<EmberFsmTransition>();
}

// LampHost.cs
public sealed class LampHost : MonoBehaviour
{
    [SerializeField] private EmberFsmGraph _graph;
    private EmberFsm<LampContext> _fsm;
    private void Awake() { _fsm = new EmberFsm<LampContext>(_graph, new LampContext { Enabled = true }); }
    private void OnEnable() { Check(_fsm.Start()); }
    private void Update() { Check(_fsm.Update(Time.deltaTime)); }
    private void FixedUpdate() { Check(_fsm.FixedUpdate(Time.fixedDeltaTime)); }
    private void OnDisable() { _fsm?.Stop(); }
    private void OnDestroy() { _fsm?.Dispose(); }
    private void Check(EmberFsmResult result)
    {
        if (result == EmberFsmResult.Faulted)
        {
            // 此处由业务记录 _fsm.LastFault 并处理宿主故障。
            enabled = false;
        }
    }
}
```

在 Project 中创建 Root 定义资产、两份 Lamp 定义资产（例如 Duration 为 1、3），以及
`Create → Ember → Core → FSM Graph` 图资产。使用默认 Inspector 配置以下内容；示例 ID 可用新节点的
GUID 替代，所有引用保持一致即可。显示名称任意且允许重复。

| 节点 ID | 定义资产 | Parent Id | Default Child Id | Bindings |
| --- | --- | --- | --- | --- |
| `root` | Root | 空 | `short` | 空 |
| `short` | Duration=1 的 Lamp | `root` | 空 | Slot=`next`, Target Id=`long` |
| `long` | Duration=3 的 Lamp | `root` | 空 | Slot=`next`, Target Id=`short` |

图的 Entry Id 填 `root`，宿主 Inspector 引用该图即可。
运行时进入 root → short，随后 short/long 按各自参数切换，root 保持激活。
同一图分配给多个宿主会分别创建对象。停用宿主执行 Stop，再启用从图入口重新 Start；销毁时最终 Dispose。

## 附加条件表达式

`EmberConditionExpression` 以稳定节点 ID、kind、参数和子 ID 表达一棵原生序列化树。`EmberConditionCatalog<T>` 提供 true/and/or/not 和业务登记的强类型叶查询；`Compile` 只校验与捕获参数，不访问运行上下文。编译拒绝未知种类、重复/空 ID、环或共享子节点、未连接节点、错误参数类型/数量、非法数值及超过32层/256节点的树。AND/OR至少两项，NOT恰好一项；计划与编辑资产分离。

`EmberFsmRuleBinding<T>` 使用原始转换句柄、源节点和请求位置绑定计划，`EmberFsmRulePlan<T>.Validate` 核对真实图、上下文和唯一键，不能用同 ID 的新句柄替代。基础许可通过后才执行附加查询，二者为 AND；没有绑定的位置保持原行为。计划没有输入消费、资源扣减、效果创建或自动 Tick 能力。业务负责资产所有权、版本、允许编辑的入口、冻结时机和叶查询纯度，公共内核不认识玩家。

条件计划有序短路；诊断分别记录值、事实和未求值原因，记录关闭不为展示重跑查询。编辑器可用公共 `EmberConditionEditor` 绘制树，但资产的 Undo、校验、保存与可用性由业务适配负责；绘制不得隐式迁移或保存。

## 当前蓝图布局与编辑边界

蓝图读取真实声明与运行观察。完整嵌套画布上的来源/目标引用只是定位代理，不能增加真实状态或重接运行图。公共参数适配与条件树适配留在各业务 Editor 程序集中。

布局工具提供“仅修复重叠／整理选中分支／整理全图”，先生成候选、移动清单和约束冲突，再由用户显式应用或取消。固定复合节点同时保护后代；分支包含固定节点时不能整体拖动。预览不改现有坐标，原图/坐标/折叠等条件变化会使候选失效；不能在打开窗口、重绘、切方案或 Play 时自动整理。

应用、拖动和固定使用本机布局状态及 Undo，布局独立于图 SO、规则、运行配置和快照。Undo/Redo恢复坐标/固定状态并重新持久化本机布局；不因此清空用户其他 Undo。只读窗口不得驱动FSM、重新评估条件或修改源图。历史 v1/v2/v3 记录描述当时迁移，不应据此让新的工具回退到旧视图。

以下测试数字均为各日期的历史结果，不表示本文本轮维护重跑了 Unity 测试；新增运行/窗口改动须按实际修改重新验证。

## 2026-10-08 内核历史验证边界

包内 `Core/Tests/EditMode/Ember.Core.Fsm.Tests.asmdef` 提供 EditMode 测试，测试夹具不进入业务程序集。
覆盖多实例、层级路径、时间、父级派发短路、重定向、旧激活凭证、重入、故障清理、静态声明及诊断一致性。
在 Unity Test Runner 的 EditMode 中选择 `Ember.Core.Fsm.Tests` 执行。

2026-10-08：Unity MCP 恢复连接后，确认当前工程为 `ember-unity-framework`，Unity 版本
`6000.5.4f1`，编译和域重载均已结束，Console 错误查询为 0 条。
通过 MCP 执行 `Ember.Core.Fsm.Tests` EditMode 测试：**52 个通过、0 个失败、0 个跳过**，
测试执行耗时约 1.42 秒，结果为 Passed。测试任务 ID：`22430b3caf9e4687a975221077e3ef28`。
本次验证范围为新增 FSM 测试程序集；未执行全项目回归、PlayMode 或实际模板场景验收。

同日源码复核后，通过 Unity MCP 独立复跑同一程序集：**Passed，52 个通过、0 个失败、0 个跳过**，
测试执行耗时 1.4384339 秒，Console 错误查询为 0 条。任务 ID：`5cf3086780c64253a9e4bd84a04131e4`。
复核覆盖 5 个内核运行时脚本、测试夹具及程序集边界；本轮未修改运行时代码或测试。

该批用例通过内存中创建的 SO 与 `Configure` 组图，尚未覆盖真实 SO/图资产的保存重载、域重载后的配置保持、
Inspector 装配操作及 PlayMode 宿主启停。共享配置测试验证运行对象、更新次数与计时隔离，
不包含输入消费、动作变速或平台跳跃手感。旧游戏流程状态机源码未修改，但尚未执行 Init、Main、Gameplay 全流程回归。
接入正式模板前需补充资产与场景验证；当前实现属于本地未发布内容。

当时首版不提供并行区域、历史子状态恢复、任意动态目标、转换排队、自定义 Inspector、蓝图视图、
模板接线或游戏玩法。Update 正常派发不主动分配，但用户回调、转换路径解析、诊断快照及故障处理可能分配，
因此公开驱动接口保守标为 `HasGC`，本次没有性能测量结论。

## 2026-10-10 蓝图历史实施记录

2026-10-10：R07.1 新增 Core/Editor/EmberFsmBlueprintWindow（UI Toolkit / GraphView）静态声明视图，节点和转换来自 Inspect，布局使用本机 EditorPrefs 的图 GUID + 节点 ID。禁止图形重接和任意控制，不读取 Guard；运行实例适配留待 R07.2。新增三项编辑器观察无副作用测试，测试程序集现引用 Core.Editor。首次编译修正 Capabilities.None 后，MCP 等待期间连接失败，最终编译/55 项测试/窗口实际显示尚未验证，不能视为已验收或已发布。

R07.1 续验：2026-10-10 通过 MCP 确认编译结束、Console error/warning 0；Ember.Core.Fsm.Tests 55/55 EditMode 通过（job 7026dc5555ce40389dccfabce6a03efc）。随后正式 PlayerGraph 窗口调用无详情失败，停止自动验证。测试通过不代表已完成正式窗口显示、布局重载及域重载验收，蓝图仍待完成此部分验证。

R07.1 布局修正：同帧刷新不再读取尚未完成布局的 GetPosition，而读取节点 inline left/top。新增回归测试后预期 56 项；MCP 编译等待期间失败，最终结果尚未验证。

R07.1 已验收（2026-10-10）：编译完成、Console error/warning 0；56/56 EditMode 通过（job 0ec18ae8ce8c47f2a08a36d708cfbc0b）。正式图 7 节点/17 边、折叠、坐标保存重开、真实域重载保持通过，图未改脏；用户确认实际显示正常。布局以项目路径/图 GUID/节点 ID 保存在本机 EditorPrefs，窗口不改运行图。当前只读静态声明，运行实例/时间/历史视图留待 R07.2；尚未发布框架版本。

R07.1a（2026-10-10，未验收）：根据真实父子关系显示嵌套复合框，父框仍有自己的声明和端口；空间包含替代层级边，默认入口不计入转换。折叠代理逐条保留真实来源/目标，不能图形重接。所有节点使用画布坐标，父框移动传播后代；布局写本机 v2.canvas 命名空间，旧键只读迁移并保留。首轮编译通过，61 项测试 60 通过/1 框包含失败（job 4f9098abf5d144338a543e941448d128）。后续正式图窗口调用失败，停止 MCP；静态补上显式 inline 尺寸与跨层多选修正后，最终编译/61 项回归、窗口和域重载均待续验。未发布。

R07.1a 可读性续修（2026-10-10）：窗口拆出 Navigation partial 和静态正交 Routing。总览/聚焦与完整嵌套各自使用 v3 布局键，旧版/v2 不覆盖；默认过滤预检声明，提供数量、单状态展开与全局开关。图/默认入口是结构关系；代码声明的出口/指定入口/路径段不成为新状态或新转换，选择高亮不表示运行事实。父状态公共转换、跨层真实目标、源码/SO 定位与观察无副作用保持。最终代码通过 Unity 编译与 63/63 EditMode（job c246ff9cd7d7461eb5d81466b86ff494）。中间真实窗口截图发现裁切/缩放后已调整，最终截图查询未找到窗口实例，按规则停止；完整可视、正式布局重开和显式域重载尚待验收，未发布。

R07.1a 最终验收（2026-10-10，方案 19.39）：Unity MCP 编译与 64/64 EditMode 通过。正式窗口总览、Ground/Air 聚焦和完整嵌套、鼠标拖动/路径选择、折叠、跨视图布局保存重开、旧版/v2 只读迁移及真实域重载均完成验证；用户确认可读性并提供最终真实总览截图。新增 FitVisible 使用当前 inline 范围，避免同帧适应读取旧边界；PlayerGraph 未改脏，序列化内容不变。各模式独立 v3 布局，旧键保留。历史失败/中间结果继续保留，以本条为当前验收结论；仅总览取得有效最终截图，其他视图依据实际交互和用户四视图反馈验收。停在 R07.1a，R07.2 运行观察和 R07.3 参数编辑尚未实施；未 Bump 或发布。


## 运行观察（R07.2，2026-10-10）

当前蓝图主界面为 R07.1b 单画布完整嵌套，局部来源/目标引用不增加真实状态。R07.2 在此基础上新增实例观察；上述历史阶段限制保留为历史记录，以此处为当前能力。

宿主可通过 `EmberFsmObservation.Capture(fsm)` 捕获只读的 InstanceId、状态、活动路径、各节点逻辑时间、有界历史、最后操作与首故障。Capture 有分配，适合诊断采样，不作为玩法热路径；它不会驱动状态、读取 Guard 或改变诊断开关。结构描述仍引用原定义资产，运行数据不写入 SO。

业务 Editor 适配通过 `EmberFsmObservationSources.Register(key, provider)` 提供宿主、原装配图和快照，卸载适配可调用 Remove；内核不自动全局注册运行器。窗口每 200ms 读取，必须选择具体 GUID 实例；同图同名宿主也不会共用状态。实例结束后清除高亮和历史，不自动选中其他宿主。platformer 的适配只在 Play 中枚举有效场景的当前已交接玩家会话。

绿色边框是完整活动路径，亮绿色 ● 为叶子，各层时间来自 GetElapsedTime；暂停不由编辑器补时。绿线标记最近已记录成功转换，黄线仅表示静态选择；提示区分声明目标、最终解析目标与叶子。历史显示已有拒绝/故障，关闭诊断不回填，未记录不等于未发生。不能据此查询未执行的条件或回放全部中间重定向。域重载使原 C# 实例失效，重新试跑后需要明确选择新实例。

最终通过 Unity MCP 编译、71/71 EditMode 和正式白盒路径/跳跃/暂停/退出清理验证。普通保存 platformer-2d 0.2.1，未发布框架版本；实际试跑中另记录一次启动器会话失效，原因未确认，详见方案 19.42。R07.3 参数编辑与 Undo/Redo 尚未实施。


### 配置编辑适配（R07.3）

`EmberFsmBlueprintParameters.Register` 由业务 Editor 程序集登记右侧参数界面和顶部工具栏；窗口提供当前图、所选真实节点及所选配置引用。通用编辑器不根据状态名称推断玩家参数，也不创建状态、评估 Guard 或接管运行。

`DrawProperties` 仅编辑适配方明确开放的序列化字段，使用 `SerializedObject.ApplyModifiedProperties` 接入 Unity Undo/Redo。业务适配负责校验、配置权威、显式资产保存以及应用时机。蓝图布局仍存于本机 EditorPrefs，与 SO 参数保存分离。platformer 的应用入口重新启动隔离白盒会话，参数在请求接受时冻结；当前运行实例不会因为编辑共享 SO 自动更新。

该历史批次 Core FSM/蓝图及 platformer 参数测试合计 75/75 EditMode 通过；实际白盒验证未保存参数应用、运行实例隔离、重新试跑、显式保存、窗口重开和真实域重载。此结果仅覆盖该批改动，不替代后续 E01/E02、布局及完整方案验证，也不要求消费项目持有开发仓库的临时方案。

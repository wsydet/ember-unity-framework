# 玩家转换规则与状态扩展

## 代码规则（E01）

基础移动图有21节点、18叶子、56条普通转换、57个请求位置、14个预检重定向槽及3个默认子入口；“通用攻击模板1”另有独立31节点图；节点身份以真实图读取为准，数量不是扩展上限。basic-drive → ground/air 构成公共运动层；地面四阶段与空中上升/下落/墙面动作分别共享父级决策。冲刺、下穿、挂边/翻越、落地及翻滚是 basic-drive 下的互斥动作分支，不假装是 air/ground 的子节点。新增状态保留旧 ID、目标槽和用户手动坐标，不通过重装基础图抹掉扩展。

PlayerRule.Fixed 创建声明请求位置：转换 ID、site ID、阶段、segment、顺序、条件、继续策略及必要提交动作。状态在正确时机调用 Activation.AttemptGroup，ShouldStop 为真时停止该处后续运动/计时。系统不自动扫描全图求值，也不靠动画转换驱动 FSM。

每次真正尝试只求一次完整代码条件；目标 CanEnter 独立预检。全部目标预检通过后才执行退出前 Commit，再 Exit/Enter。普通落地完整条件包括动作优先关系，外部请求同样遵守。以下三项固定写入属于 Commit：air 落地取消墙窗口、挂边松手 ReleaseInput、翻滚撞墙停止标记。预检拒绝不写入；已有 Enter/Begin 的能力、输入和效果提交仍在那里，不复制到 Commit。

翻滚 finished 在计时前 wall 与计时后 elapsed 两个位置尝试，保持同一转换 ID；外部请求必须选择 site，不能猜结束原因。GuardRejected、EntryRejected、NoChange 按 UntilChanged/UntilMatched 继续；协议错误立即中止本次派发并由宿主报告，不当作普通条件不满足，也不自动标记 Faulted。进入提交区后回调异常会故障停止并清理，已提交资源不自动返还。

完整公共接口以 `Packages/com.ember/Core/Runtime/Fsm/EmberFsmRequestSite.cs`、`EmberFsm.Attempts.cs` 及包内 FSM 说明为准。旧未迁移调用保持兼容，不据此给新玩家状态增加绕过规则的入口。

### 当前请求顺序与运动时机

以下对照 `Assets/Game/Module/Player/States/` 的真实声明与调用位置。全部主动请求处于 FixedUpdate；默认 segment 为 `decision`、site 为 `fixed`，Order 按表中先后以10递增。**C** 表示 UntilChanged，普通条件/进入拒绝与 NoChange 可继续；**M** 表示 UntilMatched，条件匹配后即使进入拒绝或 NoChange 也停止此位置的后续工作。两者遇协议错误或故障均停止；不能把 `Matched` 当作 `Changed`。所有模式默认 PreserveAncestors，表内 **R** 才是 ReenterTargetBranch。

| 源节点 | 按实际顺序尝试的转换 ID、许可与策略 | 本地工作及特殊位置 |
| --- | --- | --- |
| basic-drive | hard-land：HardPending C；rolling：Rolling.CanStart C；land-pause：NormalPending 且不能起跳/下穿/地面冲刺 C | 先作三项决策；未停止且未被 Landing/Rolling/Dash/Ledge 接管时才计算常规水平速度和朝向 |
| ground | drop-through C → jump C → ground-dash C → air-dash C → leave-ground：地面探测消失 M | 前四项各用原 CanStart；父级已计算水平运动 |
| air | drop-through C → buffer-or-coyote-jump C/R → head-jump C/R → wall-jump C/R → air-jump C/R → ground-dash C → air-dash C → land：探测接地且 Y速度<=0 M → ledge-hang C → wall-slide C | 使用原 CanStart、Hang.TryCandidate；land 成功提交才取消墙窗口；E02仅补充其中三个许可 |
| idle | move M → accelerate M → decelerate M | 按 GroundMotionState 精确匹配 |
| accelerate | idle M → move M → decelerate M | 同上 |
| move | idle M → accelerate M → decelerate M | 同上 |
| decelerate | idle M → move M → accelerate M | 同上 |
| jump | apex：Y速度<=0 M | 未停止后递减响应窗口、应用水平响应，再处理短跳释放；Pause/Exit 清除本次高度控制 |
| air-jump / wall-jump / head-jump | 各自 apex：Y速度<=0 M | 起跳资源及速度提交在 Enter；墙跳的控制窗口可跨入 fall，不能在 apex 时误清 |
| fall | 无主动转换 | CanEnter 上升时经 rising 槽重定向；不是每步主动请求 jump |
| wall-slide | leave-wall：!CanContinueWallSlide M | 持续许可独立于 E02 入场阈值；Exit.EndSlide 保留冲刺所需离墙信息 |
| ground-dash / air-dash | finish-ground：结束且有支撑 M → finish-air：结束且无支撑 M | **先 Dash.Advance，再查询**；Begin/End 与动作效果的 Enter/Exit 保持原顺序 |
| drop-through | clear-ground：已离开且有支撑 M → clear-air：已离开且无支撑 M | Begin 申请本宿主碰撞豁免；Exit/Dispose 清理，不全局恢复别人的豁免 |
| ledge-hang | release：Failed 或 ShouldRelease M → climb：CanClimb M | release 最终预检通过才执行 ReleaseInput；Hang.Exit 保留翻越候选，Dispose 才释放对象期资源 |
| ledge-climb | finish-or-blocked：Failed 或 Completed M | 转 fall，由常规支撑判断选择后继；运动推进在 Controller 后续 Ledge.Apply |
| land | before-motion：lost-support M → drop C → jump C → dash C；elapsed：finished M | 先前段，再 Landing.Advance，再 elapsed，未停止才 Hold；finished 的 site=elapsed、Order=10 |
| hard-land | before-motion：lost-support M；elapsed：finished M | Definition只声明这两条；没有普通落地的取消动作，计时/保持顺序同上 |
| rolling | before-motion：lost-support M → drop C → jump C → dash C → finished/wall M；elapsed：finished/elapsed M | 两次 finished 共用转换句柄；wall 为前段 Order=50，elapsed 为后段 Order=10，中间 Rolling.Advance，未停止才 Apply |

14个重定向槽由 ground 的 fall、四个地面叶子各三个其它阶段、fall 的 rising 构成。它们是 CanEnter 的装配预检路径，不计入56条普通请求，也不能合并成额外自动轮询。默认子入口由图指定，初始化/恢复不重放历史 FSM 对象。

Controller 每步先采样 Sensors、落地事实、JumpWindow及落地资源补充、HeadJump、Wall、Ledge 输入，再 BeginStep/竖直事实；随后派发 FSM。成功通过宿主 Check 后才依次做 Wall.Apply、Ledge.Apply、VerticalMotion.Apply、Motor.ExecuteStep、动画同步及落地记录。EndPhysicsStep 在 finally 中执行：拒绝零消费不意味着缓冲输入永久保留，故障也不能绕过自然步末清理。普通协议错误由 Check 报告，不能继续这一轮运动提交。

Enter/Begin 拥有输入消费、次数/CD、起跳/冲刺/翻越速度及动作效果；CanEnter 与规则条件只能查询。普通 Jump 的恢复上升入口只恢复运动，不重放按键高度控制、不消费输入或补二段跳次数。Stop/Dispose 不执行退出规则或提交动作；清理由生命周期负责。成功转换同步短路后续子派发，过期激活、错误句柄、错误 site 及回调重入均不得生成第二条旁路。

## 有限附加条件（E02）

只允许以下稳定键：源节点 air，转换 air-jump / wall-slide / ledge-hang，请求位置 fixed。最终许可恒为“完整代码条件 AND 方案附加条件”。目标、模式、阶段、顺序、继续策略、提交和结束/清理保持只读。额外条件只限制该转换入场，不是能力的全局门禁，也不是动作持续条件；墙滑进入后不因速度不再满足入场阈值而立即退出。

| 积木 | 参数与语义 |
| --- | --- |
| 恒真 | 无额外限制，不绕过代码许可 |
| 输入轴阈值 | X/Y、绝对值、< / <= / > / >=、归一化阈值；有符号[-1,1]，绝对值[0,1] |
| 输入事实 | 含缓冲的跳跃请求、本步新按跳跃、按住跳跃、本步冲刺请求；查询不 Sample/Consume |
| 支撑事实 | 地面探测、有效支撑、地面/宽限起跳机会，三者含义不同 |
| 速度阈值 | 当前 Motor X/Y速度、绝对值及比较，单位u/s；Y正向上，不能用未提交指令冒充当前速度 |
| 能力资源可用 | Ability/Skill类别及稳定ID；必须存在于本方案装配；关闭/未开放/次数/CD不足为false |
| AND / OR / NOT | 有序短路，AND/OR至少两项，NOT恰好一项 |

数值有限；空根、未知kind/参数/版本、非法枚举、缺子项、重复参数、环或错误引用明确拒绝，不默认为真。删除最后条件用显式“重置为无额外限制”。没有随机、计时记忆、反射属性路径、新探测、事件订阅或动作执行积木。

表达式是稳定节点 ID、kind、明确类型参数及有序孩子引用的原生序列化节点表；不能用 CLR 类型名或编辑器属性树作为保存协议。当前公共编译器限制深度32、节点256，拒绝共享子节点和未连接节点。每次请求独立求值，实际访问的节点各一次；重复事实叶也不跨请求缓存。JumpHeld 沿当前按键视图，其余输入事实沿 Adapter，不能声称全部同帧锁存。能力查询只说明资源可用，不隐含输入或几何许可，不创建 UseAttempt。

PlayerSchemeRules 为方案独占子资产，schema v1，并记录 Owner。旧 LegacyCode 模式继续代码规则且不携带 Rules；Configured 模式必须有完整合法的三条规则。显式等价迁移为自定义方案建立三个恒真根，支持 Undo，仍需明确保存；打开、绘制、捕获、Play 不创建或迁移资产。内置先复制。普通 Copy 保留条件，新建空白方案重置恒真并关闭可选机制/能力。

编辑入口在玩家配置的动作/机制区域及同一蓝图详情中。明确选择编辑 Scheme，观察实例另选，不让选运行对象自动切换写入目标。基础许可、附加树和只读提交分区显示，错误定位到树节点。树修改支持 Undo/Redo；保存与应用到下次试跑分开，非法草稿不得由方案保存/准备通过。

规则模式、整个树及归属纳入 Copy/Compare/Restore，副本重映射 Owner，不能把副本资产身份不同误报为参数不同。规则子资产必须位于所属方案文件中；运行归属校验不依赖 AssetDatabase。未知版本保留诊断数据并拒绝，不自动降级。迁移和子资产结构维护仅非Play显式执行，Play中的条件编辑属于下次会话草稿；原生Unity保存可能写入非法草稿，因此准备阶段仍独立校验。绘制、getter、OnValidate和关闭重开不得隐式保存/迁移，重载前必须先处置未保存草稿。

准备时校验归属/版本/白名单/树/能力ID并编译不可变计划，经 PlayerSchemeCapture 交给候选。原转换句柄仍为授权依据，不以同名字符串伪造声明。先基础条件，成功再附加树；基础拒绝或短路项不求值。CanEnter 不重复评估附加树。附加false返回GuardRejected，求值异常按Guard故障停止并清理。

同 Gameplay 恢复继续使用原计划，改资产后新会话才生效。规则不写玩家快照，不增加存档schema；跨会话读档按新捕获配置及原兼容规则准备，不能承诺旧档携带旧规则。

## 观察与验证

静态展示只描述声明与编辑条件，不能执行 Guard。运行展示只读真实尝试记录：基础/附加拒绝、目标预检、未评估、短路与提交阶段应分开。记录关闭时不额外求值。编辑树与冻结内容不同后停止把旧记录映射到新树，仍可查看旧捕获描述。布局独立保存在本机，不写规则或图资产。

例如 wall-slide 附加 Y速度<=-5 u/s，应在远离挂点的墙段验证：原墙滑许可通过且下降达到阈值才进入，之后由原持续条件维持；Hang附加拒绝仍可按原顺序尝试WallSlide。验证应包括暂停/恢复、改源后旧会话、新会话、缺配置、错误所有权、资产重载和真正窗口表现，不能只跑关键词检查。

## 新状态接入

按[新状态 Skill](../TemplateSkills/ember-platformer-add-state/SKILL.md)先确定行为与共享影响。完整交付包括 C#状态/Definition、声明与图绑定、必要能力/参数/输入接线、完整动画槽、捕获/复制/校验和保存恢复策略；不能只交空生命周期脚手架。没有次数/CD语义时不强造能力，独立Buff不强塞为互斥状态。

需要新增可变Definition参数、扩展规则schema/可编辑范围、改变模块所有权或持久化时，先明确源码依据、选项与兼容影响，再实施依赖部分。现有 E02 不等于任意新动作无代码编辑器。

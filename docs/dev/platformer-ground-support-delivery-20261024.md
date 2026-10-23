# platformer-2d 地面支撑修复交付（2026-10-24）

> 后续状态：用户已授权封存与准备框架新版本。通过正式 Bump 封存为 platformer-2d **0.11.2**，hash为 `986ceff5bf74695b8c82a33347a48d7d`，Framework **0.21.5** 本地准备中，尚未提交或远端发布。见[发布准备记录](release-0.21.5.md)。下文保留首次修复交付时的版本和验证历史。

## 归属、基线与交付边界

本改动判定为 **模板升级**：维护源在 `Assets/Game/Module/Entity`、`Assets/Game/Module/Player`，随后通过 `Ember.Core.Editor.EmberProjectSetup.SaveTemplate` 普通保存到 platformer-2d。没有修改 com.ember 通用运行时或 base 父模板；不是 Windward 产品业务补丁。

基线为 Ember Framework 0.21.4 / platformer-2d 0.11.1 / Unity 6000.5.4f1，模板原封存 hash `22dd884949be5e2697a2606d5da00c46`。父模板 base 0.8.3 / `1270ce6dfcae3c01d050c09ecfd60337` 未变。模板元数据 `frameworkVersion=0.21.0` 是原有兼容声明，不是当前包版本。

这是已修复、已验证并普通保存的候选内容，**尚未 Bump、重新封存或发布**。普通保存与 Bump/发布依 CLAUDE.md 分别授权执行，当前包与模板版本号仍是 0.21.4 / 0.11.1；旧版号不能作为包含修复的证明。最终实时 hash 见下方保存记录；`versionedContentHash` 仍是上述旧封存值。

只读取 Windward 接续与证据，没有写入 Windward、覆盖定制、修改 PackageCache、升级、重新部署、Git 提交或推送。S5 不在此签收，用户原台面人工复验仍待消费接入后进行。

## 原始证据与范围

读取 `C:/Users/wuyu/My/Windward/docs/implementation/S5-ground-support-framework-handoff-20261024.md`、`evidence/s5-support-controlled-before-bound-20261024.tsv`、`evidence/s5-support-window-repro-20261024.json`、`evidence/s5-jump-native-20261024-0141.tsv`。

- 有效受控 before-bound 记录包含 35 个 sensor=true / support=false 样本。原台面顶端 Y 约 -5.250029 与 -5.25，移动/减速时解算出约 +0.000052～+0.000122 u/s；原窗口严格 worldY<=0 耗尽宽限。
- before 和 before-focused 未正确绑定新增设备，不能证明移动复现；before-bound 临时绑定后有效，设备列表、焦点策略与测试键盘均由原诊断恢复。
- 人工 nativeSpace/A/D 固定 deviceId=1，不覆盖所有输入源；只用公开请求计数和状态判断请求进入，不能据此断言未按键。
- `s5-first-platform-support-20261024.tsv` 未生成，未作为证据。问题恢复目标120后仍在，不能归因于先前30帧残留。
- move/accelerate 均有成功跳跃样本，idle 也会失败；修复目标是统一支撑契约。

在框架原加载程序集（MVID `64600bc4-247b-462e-b682-9f5300f86a95`）通过 MCP 执行原最小窗口：初次 Observe(true,0,.02)，再五次 Observe(true,.000104725361,.02)。后五步支撑全 false，CanJump 为 true/true/true/false/false，重现原缺陷。原五个运行时文件备份保存在同目录 evidence 的 `.before` 文件。

## 修复与公开契约

详见 [地面支撑契约](../../Assets/Game/Documentation/platformer-2d/ground-support-contract.md)。

- 新增只读 `EntityGroundContact2D`：Collider、Point、Normal、ActorVelocity、SurfaceVelocity、HasContact、SeparationSpeed。传感器保留原几何探测语义，另采样真实启用接触、过滤法线/层/碰撞豁免/单向穿越，并排除瞬移后远处的旧接触缓存。
- 新增 `PlayerJumpWindow.Observe(EntityGroundContact2D,float,bool,bool)`；Controller 每物理步唯一消费，地空状态、资源补充、落地和承载复用结果。只在附近但未接触不会新授地面机会。
- `dot(actorVelocity-surfaceVelocity,normal) <= .001 u/s` 仅容忍真实接触的法向解算噪声；.02秒步长对应 .00002 单位，不是距离扩张或世界Y容差。切向上坡和载具上升由相对法向速度解释。
- 普通跳及既有其他跳跃入口、向上外部运动和动作指令消费机会。消费时仍有支撑/接触则等待观察到接触离开，才可因后续接触重新授予。显式上升小于容差也消费，旧接触不能反复补资源。
- 挂边/下穿暂时抑制与主动起跳锁分离；载具使用上一解算步速度，落地冲击采用最后提交步的相对法向速度。恢复快照后等待真实接触建立基线，不补回存档已耗资源。
- 新增只读 Contact、SupportReason、AwaitingSeparation；观察面板显示本步原因、法线、分离速度和锁。原 bool 重载保留保守原语义，正式 Controller 不再使用它；仅替换窗口文件不能得到完整修复。

没有修改 PlayerFeel、跳跃高度/持跳、移动惯性、全局重力、coyote/缓冲时长、正式场景/Prefab/碰撞体或 Windward 风公式。

## 实际文件

以下相对路径均在维护源 `Assets/Game/`，普通保存生成相同模板路径：

| 范围 | 文件 |
| --- | --- |
| 接触事实 | `Module/Entity/EntityGroundContact2D.cs`（新增及 meta）、`EntitySensors2D.cs` |
| 统一消费 | `Module/Player/PlayerJumpWindow.cs`、`PlayerController.cs`、`PlayerLandingMotion.cs`、`PlayerSupportMotion.cs` |
| 主动上升 | `Module/Player/PlayerExternalMotion.cs`、`PlayerActionState.cs` |
| FSM/诊断 | `Module/Player/States/PlayerGroundState.cs`、`PlayerAirState.cs`、`Module/Player/Editor/PlayerFsmObservationSource.cs` |
| 新专项 | `Module/Player/Tests/PlayerGroundSupportTests.cs`（新增及 meta） |
| 受影响夹具 | 同目录 `PlayerStateTests.cs`、`PlayerGroundPhaseTests.cs`、`PlayerRuntimeCandidateTests.cs`、`PlayerJumpRuleTests.cs`、`PlayerVerticalMotionTests.cs`、`PlayerAnimationTests.cs` |
| 消费说明 | `Documentation/platformer-2d/ground-support-contract.md`（新增及 meta）、`README.md`链接 |

旧测试中“放到探测范围就算落地”的前提改为先建立物理接触；不推进输入/控制器计时的准备函数最多模拟5步并断言接触成功，原缓冲到期、同叶重入、短跳、资源数量与恢复断言保留。帧率专项每个真实渲染帧只调用一次 Tick，按累计时间执行固定步；减速场景等待正式减速阶段后按键。

工作区原本已有大量模板加载差异，见 [初始状态](evidence/ground-support-20261024/workspace-before.txt)。保存同时保留原有 glossary.md/meta 和 README 术语链接，未将其声称为本修复新产物。完整保存前差异见 [源差异](evidence/ground-support-20261024/final-source-delta.txt)；其中编辑/部署记录和开发用目录由正式保存流程按既有规则剥离。

## 增量验证与失败链

只运行与本契约直接相关的 PlayMode 用例，没有全量 S3/S4 重跑。最终选定 **76项通过，0失败、0跳过**，原始结果与100步接触轨迹见 [10-final-selected.json](evidence/ground-support-20261024/10-final-selected.json)。包括：

- .000104725361 u/s 正向事实持续10步，超过原.08秒；100步真实 Composite 覆盖站立、加速、move、减速、左右换向，并增加明显斜面对照。
- 2单位等价精度 Composite 上，左右移动与松键减速分别起跳，120/30目标各4组；每组一次消费、一次进入 jump，确实离地。
- 主动起跳仍在旧接触、微小显式弹射、真正离台超时、上升穿越单向平台、未接触探测、连续支撑不补资源。
- 落地/缓冲优先级、恢复资源、承载平移/下降/离台继承/禁用/瞬移/单向下穿、挂边、动作交接，以及受影响跳跃时序和动画重入。

保留首次失败和复测，不删除不利结果：

| 证据编号 | 结果与处理 |
| --- | --- |
| 00 | 新文件未被 scripts 刷新导入，发现0项；改用 all 刷新主动编译；不是通过证据 |
| 01 | 初版 Composite 创建顺序不对，没有正向物理解算；无效夹具，不能算复现 |
| 02 | 5/6，旧 Land 夹具只等待 FSM idle；改为等真实支撑 |
| 03 | 7/7，新增初步回归；其初版帧驱动物理记录不用于最终实际帧率结论 |
| 04→05 | 25/28→31/31，定位瞬移后旧接触缓存，加入当前距离排除并复测 |
| 06→07 | 43/45→54/54，修正恢复测试物理前提及短跳以峰值验证离地；采用真实渲染帧输入采样 |
| 08→09→10 | 8/13→6/21→76/76，修正旧测试探测等于接触的前提；首次只模拟一步不足以建立接触，改为有界准备并保留断言 |
| 11 | 仅补查目标帧率：前7组已通过，第8组在按键前仍是move，夹具过早假设已减速；失败已保存，改为等待实际减速状态 |
| 12 | 修正渲染/物理阶段等待后，单独复测帧率矩阵1项（8组）全部通过；运行时实现与10批次相同 |

编号 JSON 全部位于 [证据目录](evidence/ground-support-20261024/)。最后一处仅测试时序改动后的专项结果另见保存记录。

等价几何并非原 Windward 场景复制：Unity生成2单位Composite后的实际顶点 Y 为 -0.0000583 与0（输入微差经生成后约翻倍）；宽40的连续运动夹具约0.0006093差、法线x约-1.523e-5，世界向上解算约7.617e-5 u/s而相对法向残差约1e-8。因此结论是同数量级精度几何回归，不能声称已在原首台验收。

最终76项批次实测平均FPS如下。均为约0.4秒短窗口平均，不是稳定帧率认证；每组跳跃次数为1。

| 目标 | 右移 | 右减速 | 左移 | 左减速 |
| --- | ---: | ---: | ---: | ---: |
| 120 | 118.70 | 118.77 | 117.69 | 66.06 |
| 30 | 28.27 | 29.85 | 29.96 | 30.13 |

07批次同类120目标四组均约119FPS通过；11补查120目标四组为119.59/120.35/119.14/118.82且全部通过，但11整体因最后30目标减速夹具前提失败，不能标为全批通过。

## 编译、保存与后续消费

每轮C#修改通过绑定的 `ember-unity-framework@b23e9b768e85ed30` 主动请求 MCP refresh/compile，确认编译完成、域重载和 Console；不使用 Windward 编辑器验证。76项批次运行时程序集 MVID 为 `6a464912-5218-4752-a2b0-edefe6f05392`。编译状态快照见 [compile-and-reload.json](evidence/ground-support-20261024/compile-and-reload.json)。

后续消费顺序：显式封存并由正式框架版本发布承载 → Windward 对话核对实际包解析及旧部署原稿 → 对新模板/旧模板/本地定制做三方差异审查 → 经正式工具增量接入 → 相关物理/风/弹射专项 → 用户原首台人工复验。不要覆盖整个目录，不要直接改私有字段或每帧补机会。

未覆盖：Windward 实际风交互、产品场景与定制动作的接入运行、构建后不同设备长时帧率、原首台人工手感签收；未扩大载具全矩阵或其他无关已通过用例。外部或自定义代码直接写刚体上升、绕开正式外部运动/动作入口时，不受显式消费锁的完整保证，应按公开契约接入。

正式 `SaveTemplate` 最终保存1484个文件，逐文件SHA256对照维护源为0差异，资源/meta/GUID已由保存入口校验。最终 `contentHash=986ceff5bf74695b8c82a33347a48d7d`，编辑记录一致；版本仍0.11.1，`versionedContentHash=22dd884949be5e2697a2606d5da00c46`。base无本轮改动。Git diff --check通过，未执行提交或推送。

最后测试夹具改动后再次主动编译，编译完成时间戳1792780595941，域重载完成1792780601421；最终非编译/非域重载等待、Console错误0，见 [final-editor-state.json](evidence/ground-support-20261024/final-editor-state.json)。没有因仅测试时序修正而重跑其余75项。

最后专项 [12-render-rate-final.json](evidence/ground-support-20261024/12-render-rate-final.json) 的8组均一次起跳：

| 目标 | 右移实际均值 | 右减速实际均值 | 左移实际均值 | 左减速实际均值 |
| --- | ---: | ---: | ---: | ---: |
| 120 | 97.24 | 117.58 | 114.66 | 118.52 |
| 30 | 28.35 | 29.87 | 18.82 | 30.00 |

短窗口受编辑器调度影响有慢帧，以上是实测均值；保留10、11、12各批原始记录，不能只摘取接近目标的结果声称稳定帧率。测试恢复原targetFrameRate/vSync，输入设备和动作资产限制由夹具清理。

# topdown-2d 阶段实施记录

2026-10-28。实施依据：[已授权方案](topdown-2d-template-plan.md)。本记录承接实际实施与本轮证据，不替代模板使用文档。

## 当前状态

| 阶段 | 状态 | 本轮结果 |
| --- | --- | --- |
| T0 基线与复用审查 | 完成 | 核对正式身份、父链及实际 hash；保护 2,962 个文件；核对最小移植闭包、目录、状态及快照范围；主动编译并确认域重载 |
| T1 模板骨架 | 完成 | 正式启动/退出/重入、3 项生命周期测试及可见白盒验证通过，见本轮记录 |
| T2 移动与角色方案 | 完成 | 最新 8 项 PlayMode、2 项 Editor 通过；正式运行验证移动、朝向与暂停，设备/人工手感仍待后续签收 |
| T3 基础战斗 | 准备实施 | T1/T2 已退出；下一批选择性移植 Entity 原语并适配俯视战斗 |
| T4 保存与恢复 | 未开始 | 等待可玩会话与独立快照模型 |
| T5 制作与扩展 | 未开始 | 两关、玩家工具、关卡适配、EUI、扩展与使用文档待实现 |
| T6 封存与消费验证 | 未开始 | 保存、Bump、提交、发布及消费升级分别遵守授权；未获授权的交付步骤不能执行 |

## T0 实测基线

- 仓库：`C:/Users/wuyu/My/ember-unity-framework`；HEAD `b04da0de17617a1a38685b0d2aebb557b05405b7`，暂存区无改动。
- Unity：`6000.5.4f1`；MCP 已固定 `ember-unity-framework@b23e9b768e85ed30`。另一个 Windward 实例不参与本任务。
- 包：`com.ember 0.24.3`。正式编辑记录为 `platformer-2d 0.13.0`，loadedAt `2026-10-27 17:01:05`。
- base：`0.10.0`，contentHash/versionedContentHash/实际目录 hash 均为 `61cbe49aa797b1604f293b9756674566`。
- platformer-2d：`0.13.0`，contentHash/versionedContentHash/实际目录 hash 均为 `ff217ca17005f39519049c46a83c138b`；父为 base `0.10.0`，parentContentHash 与上述 base 一致。
- 项目中心 `ValidateTemplateGraph()`：空问题列表。父模板可作为正式派生起点，无需修补 metadata。
- 已读根规则、公共/平台字典、架构、目录隔离、编码、API 速查、Unity 验证、蓝图登记、改动分流，以及方案引用的输入、SceneFlow、平台配置/战斗/关卡与 Entity 源码。源文件身份清单见 [T0 源码清单](topdown-2d-evidence/t0-source-inventory.json)。

### 已有改动来源与保护

开始时 Git 显示 53 个已跟踪变更、1,209 个未跟踪文件。它们全部视为先于本任务的成果，不是 topdown 改动：

| 既有内容 | 来源证据及处理 |
| --- | --- |
| UI 页面、Binding、公共控件库、Prefab 名称迁移、图集、字体与生成代码 | UIFoundation/base 文档、实际文件和 Git 差异；整套保护，不作为新模板来源，不替它们执行保存或修正 |
| B03 Tile/字体回归夹具 | `docs/implementation/b03-player-exit-font-20261028.md` 与活动 Development/EmberRegression；保护并排除新模板 |
| FSM 观察与布局工具、测试、公共规则/速查文档 | 包及 docs 中既有差异；只读保留，不能夹进本任务提交 |
| 渲染、质量、时间、UnityConnect、解决方案配置 | 既有设置差异；保护，不借本任务统一重写 |
| art/ui、既有诊断报告及证据 | 工作区未跟踪内容；备份已有文件，不清理 |
| 模板 Skill 发现副本 | `.agents` 及正式安装记录；保护，由项目中心正常同步处理，不复制到新模板维护源 |

Git 比较与模板比较基准不同。项目中心 `CompareEditingTemplate("platformer-2d")` 实测 **38 项差异、0 错误、0 警告**，全部为 `Game/Development/EmberRegression` 新增夹具（含 meta）。比较指纹为 `d99851b28bd48d6e80fd773b4c7f0843`。因此不能把 Git 中的 UI 差异直接认定为模板未保存，也不能把它们加入新模板。

保护目录（本机，Library 被 Git 忽略，不属于模板产物）：

`Library/EmberBackups/topdown-2d-t0-20261028/`

- `workspace-before.zip`：2,962 文件，209.77 MiB；SHA256 `377A988A6ABA724209D18937E0C9946AE7BF452A725FE2A881F7FA327D34A08E`。
- `manifest.json`：每文件路径、长度与 SHA256；已从 ZIP 逐个解压到内存计算 hash，并再次比较源文件，**2,962/2,962 一致**。
- 范围：五个受管根及其根 meta、Assets/Editor、.agents、UserSettings、Library/EmberAISkills，以及所有既有已跟踪变更/未跟踪文件；包含原始字节、GUID、身份与布局。
- `git-status-before.txt`、`head.txt`、`unstaged.patch`、`staged.patch`、`absent-paths.json`：记录原差异、暂存边界及原有删除。补丁不替代 ZIP 中的原始字节。
- `center-comparison.txt`：项目中心完整差异报告。
- 场景实测只有 FrameworkScene，`isDirty=false`；项目 ScriptableObject dirty 列表为空。没有正在播放/试玩。
- 未发现打开的 PlayerSchemeWindow 或 FSM 蓝图窗口；Player SessionState 草稿为空，另存为 `player-window-session-draft.json`（null）。现有一个 FallbackEditorWindow 保持原状，不擅自修复或关闭。
- 平台白盒 EditorPrefs 和相关本机偏好单独导出；不修改它们。

### 恢复路径

1. 若需回到平台工作，先保护当时新增的 topdown Assets 与新草稿，避免恢复覆盖之后的成果。
2. 通过项目中心正式加载 `platformer-2d`，保留场景和 Skill 同步人工确认，不手改身份或复制快照替代加载。
3. 对比保护包 manifest，只从包恢复原工作区独有/修改文件，保留原 meta；参照 `absent-paths.json` 恢复原有删除状态。B03 夹具属于必须恢复的独有内容。恢复前逐项比较较新改动；不使用全仓库 reset、clean 或 stash 弹出覆盖。
4. 校对 .agents 安装所有权、UserSettings 和本机偏好；Player 草稿在本次基线为空。恢复后主动请求 Unity 编译、确认本轮域重载、读 Console，再验平台相关链路。
5. ZIP 为本机恢复材料，不能随模板分发。不能删除 Library 后再指望从它恢复；最终交付前应由维护者保留恢复包到长期备份位置。

## T0 最终复用与排除清单

本批归属 **topdown-2d 模板**，不包含 UnityFarm 产品业务。没有计划中的公共 API 提取，不扩大 base 或新增 PlayerManager；若实施发现公共缺口，单独记明归属和受影响方后修复。

| 能力 | 决策、来源与依赖闭包 | 要保留/新增的验证 |
| --- | --- | --- |
| 正式启动、生命周期、SceneFlow | 直接继承实际 base：GameInit/Main/Gameplay、GameSceneScope、参与者、Prepare/Start/Dispose、原位替换；不继承平台 GameGameplayState 接线 | 新游戏、退出重入、取消、部分准备失败、候选独立、旧结果晚释放 |
| 输入与存储 | 直接继承 base 的 GameInput 和 PlayerData；两 Module 实际默认 Enabled=false，必须在新活动 Assets 显式启用；存储目录独立，试跑另隔离 | Ready/epoch、最高优先级 Deny、恢复按住不重发、Drain/失败保留 |
| Powers | 选择性移植 `Entity/Powers` 7 个 C#：AbilityDefinition、SkillDefinition、PowerConfig、Contracts、Host、Runtime、Snapshot；依赖 System/UnityEngine/Ember.Basic，不依赖 Player/FSM/输入 | EntityPowerHostTests 的宿主隔离、尝试一次提交、CD/次数/开放/销毁契约，另加俯视动作接线测试 |
| Effects | 选择性移植 `Entity/Effects` 6 个 C#：Binding、Container、Contracts、Definition、Rules、Targets，以及 Entity2D 归属根；无自主 Tick | 独立句柄、Owned/Timed、打断释放、暂停、可恢复效果、绑定输出清理与双宿主 |
| 生命/伤害 | 移植 EntityHealth；EntityHitPayload、Contracts、Batch 适配为向量旋转及俯视宿主。Batch 注册根/世代失效、稳定键顺序保留；不保留平台 Actor/Motor 的隐式要求 | 敌我过滤、多 Hurtbox/窗口去重、同批换血、无敌顺序、目标失效 |
| 运动/朝向/受击 | 新增俯视向量 Motor/Facing/Reaction 接线。平台 EntityMotor2D、Facing2D、ReactionMotion 仅作算法参考，不直接移植运行实现 | 平面加减速、模拟摇杆力度、斜向、碰撞扫掠、任意朝向击退及暂停零速度 |
| 命中查询 | 适配 PlayerHitQuery 的原生 Collider Overlap + Cast + 终点 Overlap 流程；替换左右镜像为统一方向旋转，运行/预览/刀光共享变换 | 八向/任意方向、移动扫掠、短窗口跨步；不声称任意旋转/形变连续碰撞 |
| 玩家方案、FSM、会话/恢复 | 流程参考平台 Prepare/Start/Stop/Replacement、配置冻结和独立宿主；新增俯视 Context、Definitions、图、SchemeCapture、完整快照与 codec；不复制整个 Player | 冻结 A/编辑 B、预检不消费、旧回调无效、配置与快照严格匹配 |
| 相机 | 适配 CameraParticipant 的正交 WorldSpace Follow/注册/提交通知契约，独立 Game.PlayerCamera.Runtime；Player 不引用 Cinemachine | 候选不激活镜头、提交后绑定、恢复重置历史、旧释放不解绑新玩家 |
| 关卡 | 直接复用 Ember.LevelAuthoring 编辑引擎与 TrialSession；新增 IEmberLevelAdapter、关卡身份/出生校验及 Init 前试跑注入 | 两关复用、Undo/Redo、测试位置/入口、停止恢复编辑现场、试跑存档隔离 |
| UI | 继承 base 公共页面；必要 HUD/死亡恢复/暂停/保存反馈由 EUI 开发中心制作/绑定/生成 | 页面与玩法控制所有权、恢复换会话退订、错误可见、布局实际渲染 |

不移植：跳跃、地空/墙面、Sensors/支撑/承载、单向平台/下穿、土狼时间、平台补次数、挂边、踩头、弹射/移动平台、PlatformerSnapshot、整套平台工具/Skill、平台专属美术及任何永夜产品内容。新增 Scheme、图、Prefab、配置使用独立 GUID；不复制原 meta。兄弟模板只读源码通过上述恢复包/已保存快照回查，不建立运行依赖。

## T0 固定实现边界

- 目录与程序集：`Game/Module/Entity`（Game.Entity.Runtime，仅 Basic）、`Player`（Game.Player.Runtime，引用 SceneFlow/Input/PlayerData/Core/Entity/UniTask）、`PlayerCamera`（独立 Cinemachine 适配）、`LevelAuthoring`（运行关卡声明及独立 Editor 适配）。Editor 与测试独立 asmdef。UI 为 `Game/UI/Runtime/Module/Player` 等中心生成路径；配置 `GameResource/Resources/Config/Player`，玩家/场景资源按既有类别组织。
- 图：根分支 `Alive` / `Dead`；Alive 下 `Locomotion(Idle/Move)`、`Action(Attack/Dash/项目示例动作)`、`Hit`。独立 SO 声明，运行对象归宿主；Buff/世界进度不成为互斥状态。
- 输入：Move 单位圆限制但保留力度；鼠标世界平面瞄准/手柄死区，空输入保留朝向；按钮 Update→Fixed 锁存且限制立即清理。移动与瞄准可分离，攻击进入锁向，冲刺方向来源配置明确。
- Tick：PlayerModule 为会话唯一驱动；同一物理步先推进角色/敌人意图和运动、收集候选、稳定结算，避免各组件无序 Update。游戏秒冷却/效果只由一个更新点推进。
- 快照：独立 TopdownSnapshot/codec v1，场景/关卡与出生身份、方案身份与配置签名、XY 位置/朝向、HP、能力开放/次数/CD、独立可恢复 Timed 效果，以及一个真实持久互动对象的世界片段。玩家与世界一次接受、一起写入、候选准备/联合提交；不使用 PlatformerSnapshot 版本/地空字段。
- 保存只接受稳定 Locomotion；Attack/Dash/Hit/Dead/无恢复协议动作或 Owned 效果给出拒绝原因。不保存任意 FSM 叶子/Animator 时间。接受时冻结；失败不更新恢复目标或退出当前会话。死亡重试、载入、换关分别声明原因及世界策略。
- 工具统一检查 `IsTemplateActive("topdown-2d")` 与模块开关；菜单、窗口、所有实际写入/试玩操作一致；FSM 蓝图显式 ownerTemplateId/enabled。

## 本轮验证证据与未覆盖范围

T0 主动调用 Unity MCP `refresh_unity(compile=request, scope=all)`，返回 compile_started=true。后续同实例状态：

- last_compile_started_unix_ms `1793190238178`；last_compile_finished_unix_ms `1793190241005`。
- last_domain_reload_after_unix_ms `1793190247983`；is_compiling=false、is_domain_reload_pending=false、is_updating=false、ready_for_tools=true。
- 已加载 Core.Editor MVID `3bf654a8-fbab-46a5-b162-fca51b02e68f`；SceneFlow `a19878fa-2f5b-40ee-ae12-e1fe9af22d2a`；Entity `5593f221-6ff4-4f7d-874a-9e5956fd280d`。
- Console：0 error；1 条 MCP WebSocket 初始化 warning（完整堆栈来自 MCP transport），连接已恢复并成功完成后续查询。不把它写成零警告。
- 实际父/平台目录 hash 与 metadata 相等，父链图无问题；保护包逐文件 hash 检查通过。T0 未改 C#/asmdef，没有运行玩法测试或长时/全量测试。
- 新模板尚未实际创建/运行，因此 TD01—TD10、键鼠人工体验、真实手柄、分辨率与 Windows Player 均未验收。现有平台历史通过数不用于本轮 topdown 验收。

下一步：项目中心「模板开发 → 新建模板」，ID `topdown-2d`、名称 `2D 俯视`、派生父模板 `base`，执行正常「创建并加载」确认及 Skill 同步预览。切换后先核对编辑身份、parentVersion/parentContentHash/ParentSnapshot，再在活动 Assets 开始 T1；不使用「将当前编辑副本另存为」。

### T1 创建请求

已通过 MCP 打开正式 `EmberSetupWindow.ShowTemplateDevelopment()`，填入上述表单，并在 EditorApplication.delayCall 调用项目中心面板自身的 `CreateNewTemplate(true)`。保留该方法的「创建并加载」确认、LoadTemplate 内 Skill 同步预览及场景确认。不是直接调用低层 API 静默替换，也没有直接写 Templates~。此请求只代表已发起，必须等用户处理确认后再核对实际结果。

等待模态确认期间不重复提交创建、切换、刷新或编译请求。用户确认后先查磁盘活动身份、目标 metadata 与编辑器状态；若取消、创建失败或仅创建未加载，应按真实结果续接，不能擅自视为切换成功。

### T1 首次加载失败与恢复

用户已确认创建并加载及 Skill 同步。重新核对发现：模板已成功创建（preview 0.1.0、直接父 base 0.10.0，hash 与 base 相同），但正式活动身份仍为 platformer-2d。中心 `_lastResult` 为“创建模板失败：模板事务失败，且自动回滚未能完整恢复”。Console 完整堆栈定位到 `Directory.Move → EmberTemplateTransaction.MovePath → Rollback`，拒绝访问 `Assets/Game.ember-backup~`。回滚异常遮住了最初提交异常，暂不能据此断言原始失败点或锁持有者。

现场：`Assets/Game` 不存在，`Assets/Game.ember-backup~` 有完整 1,438 文件，其他根与原身份仍在。未清理事务备份或用模板内容补齐缺失。先将备份逐文件对照 T0，1,438/1,438 相同、无多余文件；校验源/目标绝对路径均在仓库内、目标不存在后，使用 PowerShell 原生 Move-Item 将原目录恢复到 `Assets/Game`。随后业务根、Assets/Editor 和 .agents 共 2,063 文件与 T0 全部一致。证据：[恢复校验](topdown-2d-evidence/t1-failed-load-restored.json)。

这只是原工作区恢复，不是新模板加载，不修改编辑身份、模板快照或 hash。恢复后已通过 MCP 主动请求编译；在确认编译/域重载后，重新从项目中心加载已存在的 topdown-2d，继续保留正常模态确认。不得重复“创建”同 ID，也不能把失败当成俯视实现完成。

恢复后本轮编译：started `1793190744871`、finished `1793190748727`、domain reload after `1793190754440`；随后编译/域重载/导入均 idle、ready=true，Console error=0。已加载原 Player 程序集 MVID `c3590bdd-cab6-4748-9364-3bb05b8967fa`。新模板 Assets 与 ParentSnapshot 实际 hash 均为 `61cbe49aa797b1604f293b9756674566`，父链无问题。

已再经正式面板 `LoadTemplateIntoProject(topdown-2d)` 发起加载，等用户处理正常加载及 Skill 同步确认；请求结果待核对。没有重建模板。

### T1 第二次失败与事务诊断修正

第二次用户确认后仍加载失败，现场再次仅留下 Game.ember-backup~，活动身份仍为平台。已再次按 1,438 个 SHA256 核对并原位恢复 Game，没有丢弃草稿。停止无变化重试。

新增必要的框架诊断修正（本项归属框架，不提升玩法职责）：`EmberTemplateTransaction.CommitPreparedTargets` 在提交和回滚均失败时，使用 AggregateException 保留两者；MovePath 的 IOException 附带源/目标路径。以前回滚异常遮住原始提交异常，无法判断真正失败的目标。没有改事务落位/回滚顺序、权限、备份清理或使用复制降级。

新增定向回归 `CommitPreparedTargets_RollbackFailureShouldPreserveOriginalErrorAndBackups`：在第二目标尚未落位时注入外部冲突，证明原始异常保留、其他目标仍恢复、冲突目标原备份仍在。加上已有正常提交及三处父同步故障回滚，共 **5/5 EditMode 通过，0 失败/跳过，1.566 秒**；job `1d7cddbeeabc4ee383eddafa5f55c75f`。未运行全量测试。

该批主动编译 started `1793191010162`、finished `1793191017107`、reload after `1793191022166`；idle/ready 确认，Console error=0；Core.Editor MVID `9adad6a8-7382-44dc-a47d-cfdf032148ba`。对既有 Resources 的同进程目录改名/恢复诊断往返完成，无异常、无残留，不能据此断言 Game 或其他目标也可移动。

已发起带新诊断的第三次正式加载，保留正常人工确认。尚未认定故障根因，也尚未开始俯视运行实现；若仍失败，依据首次提交和回滚的完整异常处理，不继续盲重试。

### T1 加载成功与实现

第三次正常确认后正式活动记录为 `topdown-2d 0.1.0`，loadedAt `2026-10-28 20:40:47`，受管 Assets 到位，无残留事务备份。此次成功不证明前两次 Windows 目录移动错误已修复；框架改动只改善双异常诊断。

已在活动 Assets 新增 Game.Player.Runtime、Game.PlayerCamera.Runtime、独立 Editor 与 PlayModeTests 程序集。PlayerSceneParticipant 按 Scope 准备独立候选，PlayerModule 在 Gameplay 激活后交接；准备不运行、不开放输入，候选可跨 Module Reset 保留，旧 Dispose 不能停止新会话。角色是零重力 Kinematic Rigidbody2D，尚无 T2 运动逻辑。相机独立消费成功交接通知，Player 无 Cinemachine 引用。

显式启用继承的 GameInputModule/PlayerDataModule。新增 Topdown 输入资产（键鼠及手柄接线），正式存储使用 TopdownProgress / ember.topdown2d.progress。通过活动 Editor 制作入口生成独立 Player Prefab、占位 Sprite、五面白盒墙、正交 Follow 相机及 Gameplay Scope 接线；仅修改活动 Assets，未保存模板或 Bump。

生命周期 PlayMode 测试 **3/3 通过、0 失败/跳过，0.318 秒**，job `f45a7a8aa7e6443684829495561aa98e`：准备不启动/跨 Reset、取消与旧结果隔离、后续参与者启动失败清理。没有运行全量或兄弟模板测试。

主动编译 started `1793191604135`、finished `1793191607982`、reload after `1793191614097`；ready=true、Console error=0。Player Runtime MVID `5cbb57e3-099e-4b99-93e1-5142950ecd89`，Editor MVID `7bdb0f78-1364-43a6-b26d-60b5ee966050`。中途 InputSystem AddAction 命名参数错误已按实际已安装源码改为 expectedControlLayout，并重新编译确认；失败批次不算通过。

正式实播：Framework 正常到 Main，Input/Storage Ready、无玩家会话；请求2进入 Gameplay 后 running/simulated/input=true、过渡结束、Topdown Camera Follow=Player；请求3返回 Main 后 HasSession=false、gameplay input=false；请求4再次进入后运行正常、SessionVersion=3。Console error/warning=0。

实际截图 `t1-gameplay.png` 显示继承的 Gameplay 页面 BgImage 遮住白盒，因此该图是未通过的视觉证据，不能据上述状态值宣称画面完成。已使用 ember-eui-build 技能，备份原 Prefab/meta/生成文件到 t1-ui-before，经 PrefabUtility 修改真实页面，将 BgImage/TitleText 关闭、既有两个按钮放至右上角；通过 EUIBindingCodeGenUtility.TryRegenerateCode 正式重新生成，保留真实 Binding。正在完成该批主动编译和无遮挡画面复查。

### T1 退出证据

UI 生成后主动编译 started `1793191781290`、finished `1793191785824`、reload after `1793191793535`，Console error/warning=0。Binding 实际仍为原有两个按钮，回执 `t1-gameplay-ui-binding.json`。首次回执序列化遇 Unity Color 自引用，资源修改/生成已成功；只重写回执为普通字段，未重复执行制作。

去除遮挡后的画面仍无白盒，检查发现占位 PNG 没有 Single Sprite 子资产，渲染引用为 null。已修正制作器，显式 spriteImportMode=Single，生成器在 Sprite 缺失时拒绝继续；修复本轮新建 Prefab/场景引用，并将世界相机改为 SolidColor。修复批编译 started `1793191917025`、finished `1793191919935`、reload after `1793191926581`，Editor MVID `17102412-72db-45e4-92c6-8720dab01f30`，Console error=0；资源另经 MCP 刷新。

再次实际从 Framework→Main→Gameplay：会话/物理/玩法输入=true、过渡已结束、missingSprites=0、Console error/warning=0。[T1 可见白盒](topdown-2d-evidence/t1-gameplay-visible.png) 已查看，蓝色玩家、黄色方向、墙体与右上菜单清晰显示。之后停止 Play，回到 Framework 编辑现场。T1 完成；此前遮挡和缺 Sprite 截图保留为失败证据，不用作验收。

## T2 实现与本轮验收

- 方案 PlayerScheme 捕获运动、瞄准、碰撞与方向动画映射，独立复制图及 Definition；原源配置后续编辑不影响运行宿主。默认图为 Alive → Locomotion → Idle/Move。PlayerSchemeWorkflow.Copy 创建独立 ID、图和 Definition，Sprite 美术仍显式共享。
- Move 单位圆限制且保留模拟量；连续方向独立于表现的 1/4/8 向序列映射。鼠标经已绑定玩法相机射线与 XY 平面求交，手柄有死区，无有效输入保持朝向；Movement 模式明确跟随运动。
- BoxCollider 原生位置 Cast、最多四次滑动、每物理步只提交一次 MovePosition。全程零重力，无地面/跳跃/平台恢复逻辑。PlayerModule 是唯一 Update/FixedUpdate 驱动。
- 按钮跨 Update/Fixed 锁存，禁止立即清空。暂停租约各自释放；首次暂停冻结 FSM/刚体/游戏时间，最后释放恢复，清除旧速度/请求。Settings 覆盖接入同一暂停所有权。
- 新增稳定动作 ID→方向 Sprite 序列映射。缺少必要映射拒绝准备；只有明确启用 AllowPrimitivePresentation 才可回退占位。默认 Idle/Move 已显式配置映射，尚无正式角色美术。Scene/Prefab 的 Sprite 变换补齐 Z scale=1，避免 Vector2 隐式转换产生退化矩阵。

本轮失败保留：首跑 job 2d766db01c9b4649afa1d4a7722e6c7c 为 4/7；InputTestFixture.Set/Press 在 UnityTest 仅排队，修正夹具显式 InputSystem.Update 后 job df153bcfbf734555b97fad91e71ca9c9 为 7/7。后续动画接线复跑 job 17b673cf36ec4bf9a503bebb856ada34 在 TearDown 遇 Runner SetEnumeratorPC 空引用并连带场景重名；按既有平台夹具改为真实 yield 包装、唯一场景名及部分 Setup 安全清理。不能将旧通过数替代当前验证。

最新相关 PlayMode **8/8 通过，0 失败/跳过，3.681 秒**，job 289cda90480e49f09c476526e3896658。包括原三项生命周期与模拟力度/斜向/独立朝向、四向实体墙和窄口、暂停零漂移/嵌套释放、输入禁止/恢复按住、两个宿主配置隔离。

Editor **2/2 通过，0 失败/跳过，4.514 秒**，job 727a9a29f93f4edd96f2686b1664c1c9；验证复制的身份/图/定义独立与重载，以及缺映射拒绝/显式回退。

编译与域重载证据：T2 首批 started 1793192239599 / finished 1793192246069 / reload 1793192251839；映射与复制批 started 1793192639991 / finished 1793192646960 / reload 1793192653319，Runtime MVID 75c82f00-6517-4421-b079-bac5ec0a96d7；最终夹具修复批 started 1793192774415 / finished 1793192776213 / reload 1793192780607。各批均确认 idle/ready 后读取 Console error=0。没有全量或兄弟模板重测。

正式实播仍经 Framework→Main→Gameplay。初次新建模拟键鼠未被 PlayerInput 配对，位置未变及瞄准不匹配的回执保留为失败，不能称为玩法验收。改用实际已配对输入进行有界回放，不手动额外 Tick：t2-live-paired-motion.json 记录 (0,0)→(0.8613,0.8613)，FSM=move；另一轮右移实际位置 (1.46,0)。单次鼠标事件会被后续本机硬件更新覆盖，因此用 600ms onBeforeUpdate 有界回放后自动退订，t2-live-held-aim.json 的实际/预期向量均 (-0.37139,0.92848)，误差 0。该证据是自动回放，不是人工手感或真实手柄签收。

暂停实播 t2-live-pause.json：frozen=true、drift=0、resumed=true、paused=false；Console error/warning=0。[移动后可见玩家](topdown-2d-evidence/t2-live-body-check.png) 已查看，位置与相机跟随改变、玩家及方向标记可见。停止 Play 后恢复 Framework 编辑场景。人工体验、真实手柄、分辨率与 Player 构建仍未覆盖，留至 T6。

## T3 当前实施与证据

- 已选择性移植 Entity2D、EntityHealth、Powers 7 文件、Effects 6 文件及 HitPayload/Contracts/Batch，均未复制原 meta。平台 Motor/Facing/ReactionMotion、Player 整目录、地空状态与恢复未移植。T2 新写的平面扫掠运动下沉到本模板 Game.Entity.Runtime，保留本轮 GUID，改为独立刚体/形状/掩码参数，不引入 Player 依赖。
- EntityHit 采用方向向量，将攻击本地前/侧向旋转为世界击退；EntityPlaneFrame 同时用于命中查询、刀光和预览。新增普通值命中构造入口。Entity 原语测试四类 **40/40 PlayMode 通过，0 失败/跳过，0.470 秒**，job 8d5d21fcaa664348b48a4391b7a1b134。
- 玩家图已扩展为 Alive(Locomotion[Idle/Move], Action[Attack/Dash], Hit) 与独立 Dead。动作进入预检只查询资源，Enter 才提交；近战锁向，冲刺方向来源和是否无敌显式配置；Owned 冲刺效果在结束/打断/释放时撤销。
- 所有转换已接入框架 E01 RequestSite/AttemptGroup，明确段落、顺序、条件与提交描述。参考平台的完整状态机和蓝图流程，未移植平台状态语义。新增 topdown.player 观察源，显式 ownerTemplateId=topdown-2d、模块 Enabled 条件；Controller 提供只读观察快照，源图与冻结图通过 ObservationOrigin 关联，不由窗口额外 Tick/判断 Guard/消费输入。
- PlayerCombatScene 为本会话唯一战斗所有者，统一推进、收集和稳定结算；原生旋转 Box Overlap/Cast/终点 Overlap，按攻击实例/窗口/Entity 去重。简单追击敌人有发现、追击、0.45 秒预告、出手、恢复、受击和死亡；场景声明组件无 Update。
- 已在活动 Assets 装配独立 attack/dash Skill、9 节点图、六种动作映射、玩家刀光、Chaser 与 TrainingTarget Prefab 及正式 Gameplay 样例。战斗关闭时不注册战斗对象，移动接线仍可运行。新增只读 CombatActorView 供诊断/业务读取。

编译证据：原语批 started 1793193426934 / finished 1793193429790 / reload 1793193435867；战斗/蓝图/样例批 started 1793194248746 / finished 1793194256252 / reload 1793194262779，Runtime MVID 4ddb88c2-fc53-40e0-8df9-f639c54811d7；新增战斗测试批 started 1793194473229 / finished 1793194478423 / reload 1793194485087。均确认 idle/ready 后读取 Console error=0，样例资产另经 MCP 刷新。

新增 6 项战斗用例与相关 T1/T2 组成 14 项首跑 job 534f90892b714667b6aa9948c4af5b94：**13 通过、1 失败**。失败为八向循环中 InputSystem 缓存未处理值异常（日志未忽略），不是通过证据；正在将夹具限定到自有键盘/手柄并改用完整 GamepadState 事件后复验。尚未达到 T3 退出条件。

### T3 退出验证

夹具故障的后续记录：限定自有设备后的 job 5967bb6ff8e34e49be384a3e79fc48bc 在 TearDown 访问已还原的输入设备；已重排为先释放业务/订阅、最后 base.TearDown。job 4b9d6ddc3a314232a3e81ba319acde98 仍为 13/14；定位到测试 Steps 在 LastFixedUpdate 续接点调用 Update，跨阶段读取 Dynamic 输入缓存。修正调用阶段后单独八向用例 de60855f2b384bb2834e5cbc4a2897ae 通过。期间曾尝试单宿主连续攻击用例，因第二击未触发而失败（44f89cc1f2e64391b5a33c6f9807c961），未计为通过；八向用例恢复独立会话形式，覆盖重点仍为方向几何与去重。

最终相关玩家 PlayMode **14/14 通过、0 失败/跳过，6.143 秒**，job 0ea21e6794704730887493b00bc74a9a。方案 Editor 2/2 通过，job 88446395f4cd4707954e9490a38c7d38（结果查询时编辑器域重载导致一次暂时断线，恢复后读取原 job 成功，没有重复发起）。最终夹具批编译 started 1793195323798 / finished 1793195325748 / reload 1793195330145，Console error=0。

正式 Framework→Main→Gameplay 实播：默认追击敌人经过 windup/recovery 并造成玩家受击；蓝图通过公开 Open 打开实际源图，9 节点、运行路径 alive/hit，随后观测 dead，HP=0、gameplay input=false；回执 t3-live-death-blueprint.json，截图 t3-live-combat.png（死亡后画面）。读取观察没有额外 Tick/Guard 调用。

实播输入第一轮在玩家已经死亡后注入，t3-live-input-combat.json 不通过；第二轮转场前按住 Shift 被输入恢复保护抑制，t3-live-input-combat-success.json 仅证明 attack/slash=true，文件名不表示全部通过。最终改为可交互后先中立再新按下，t3-live-dash-melee.json 记录近战/冲刺/刀光和目标扣血，详见回执实际数值。均为实际配对键鼠有界回放，无手动额外 Tick；回调均自动退订，结束后停止 Play。人工体验与真实手柄仍未签收。

## T4 当前实施与证据

新增独立 TopdownSnapshot v1 / ember.topdown2d.progress codec：场景/关卡/出生/方案身份、位置与连续朝向、生命装配、能力开放/次数/CD、可恢复 Timed 效果，以及世界角色 HP/位置和持久交互布尔片段。二进制严格边界、版本、数量、有限数、重复身份及尾随/截断校验；无平台快照依赖。敌人恢复时动作重新从 idle 开始，未承诺敌人任意动作相位恢复。

即时保存只接受 idle/move 且无待提交受击；先同步冻结玩家与世界，再交存储异步写盘。Owned/未支持效果由 CaptureRestorableTimed 明确拒绝。检查点交互由 E 新按下驱动，世界值与检查点由会话持有；无独立 MonoBehaviour Update。重试明确读取检查点基线的世界和能力，玩家回到检查点恢复 HP、清除临时计时效果；读档则恢复所选档案，二者不混同。

恢复候选不改旧身体、世界颜色、碰撞排除或效果；所有表现绑定推迟到提交。SceneScope 替换准备独立 Runtime/Combat/World，预检身份/HP/能力集合/实体墙重叠；Start 前预检与启动 FSM 后才释放旧会话。提交后意外异常按故障状态处理，不声称任意业务回滚。退出提供存储 Drain，失败保留当前会话并恢复请求入口；失败写入须显式确认，不能静默抹除。

相关 PlayMode 新增六项并含直接受影响的 T1—T3，首跑42dc50c15a0143abaa3183163e50cdf7 为19/20；恢复位置在 simulated=false 设置后重新启用被覆盖，调整为启用后落位。最新 **20/20 通过，0 失败/跳过，9.022 秒**，job12ded9fb790344e5bd898d1470787da4。包含完整 codec、坏数据、冻结保存、独立候选释放/替换、坏方案/坏槽位保护旧会话与好档、动作拒绝，以及足够时长后的同宿主两次攻击各提交一次。T3 曾试验单宿主八向用例用20步等待不足攻击时长，该失败不代表连击已完成；新重复攻击用例按实际 fixedDeltaTime 计算完整等待。

T4 首批编译1793196008004→1793196015721，reload1793196023466；测试访问 internal helper 报错已改为测试自有 TimedSpec，修复编译1793196145559→1793196148024，reload1793196155319；落位/重叠修正1793196231950→1793196234870，reload1793196240048。各成功批均主动请求编译、ready后读Console error=0。正式 Gameplay 添加唯一 TopdownLevelIdentity 与 Checkpoint Prefab，修改通过 Editor API 并刷新，未手写YAML。

实播首轮保存成功，交互世界值为true；原位恢复误用不同格式的 Fsm.CurrentScenePath 比较，走入跨场景准备后因旧Scope已占用而拒绝。t4-live-save-restore.json 与 t4-recovery-diagnostic.json 为失败证据：SessionVersion保持1、旧HP/位置/世界保留；不能计为恢复通过。已改为比较真实玩家 scene.path，继续编译与正式链路复验。T4 尚未签收。

### T4 正式链路复验

修正同场景路径判断后，实播 t4-live-save-restore-v2.json：保存成功、恢复 Completed、SessionVersion 1→3，位置(0,-2)、HP4、持久交互true，与同一档案一致。回执采集时恢复仍持有显露期暂停（WhenInteractive 与 Completion 完成先后不同），所有权随后自动释放，不能把当时 paused=true 解释成暂停租约泄漏。

继续验证退出发现 PreparedPlayerSession.Validate 把本会话的存储 Draining 当失效，导致退出等待期间停止玩家。修正为已启动会话允许同世代 Draining，准备候选仍必须 Ready；新增 DrainRetainsThePausedRunningSessionUntilAcceptedWritesComplete。与冻结/恢复回归共 **2/2 通过，0失败/跳过，0.624秒**，job222e89bddcf44cd89b81e2107da13705。修复编译1793196546637→1793196548897，reload1793196553999；Console error=0。

最终正式从 Main 读取既有好档进入Gameplay→原位检查点重试→Drain返回Main，回执 t4-live-continue-retry-exit-final.json；loaded/retry=Completed、世界交互true、HP恢复最大值6、exit=true、最终HasSession=false、GameMainState、TimeScale=1。读取Console error/warning=0，停止Play回到Framework编辑现场。T4已完成本轮开发端退出条件；完整正式按钮UI仍属T5，消费部署与人工体验仍未验收。

## T5 当前实施

已使用 ember-eui-build 流程从 EUI 中心公开 API 创建业务 FreePage EUITopdownHudPage，保存真实控件/Binding后 TryRegenerateCode；主菜单精确追加 Continue/Progress 两个绑定，保留原有框架标记，生成后才实现用户逻辑。新增存档、保存退出、读档、检查点重试、暂停与写入失败确认按钮和生命/动作显示；返回菜单用户钩子接 Drain。制作前主菜单 Prefab/meta 备份至 t5-ui-before；生成回执 t5-eui-creation.json。首次 namespace 编译错误和 PageName 标识符预检拒绝已纠正，未把预检拒绝当成创建成功。

玩家开发窗口已提供正式身份/模块条件菜单、精确模板旧窗口失效、源方案编辑/校验/独立复制、FSM蓝图入口与参数接线、EntityPlaneFrame 方向命中预览。新增俯视 IEmberLevelAdapter、对象适配和独立试玩桥：Framework→Main开屏结束后请求Gameplay，开发存档在 Library/TopdownTrials/<request>/Progress，停止先 ShutdownFrameworkAsync 等待所有者/I/O，再退出并由公共 TrialSession 恢复编辑现场。尚未实播试玩，不作为通过证据。

两个关卡已制作：Starter训练场与独立GUID/关卡ID的TopdownArena（不同出生位置、障碍与战斗布局）；检查点/敌人/沙袋/实体障碍登记为工作台素材。首次复制场景后 original.Scheme 被场景卸载释放，导致后续定义 Configure 拒绝；保留已复制场景和身份，通过读取资产路径后重新加载修正适配器，并显式 CompleteArena 续接本轮新资产，未覆盖既有场景或手改YAML。

扩展接线新增：PlayerStateDefinition/PlayerState允许继承，提供受 Context 生命周期回收的额外输入按钮与有唯一拥有者的 PlayerActionControl；运动仍由原 PlayerModule 统一物理提交。独立 Game.Player.Examples 程序集正在制作 Focus 样例：扩展 Locomotion 的一个入口段，复用基础移动/攻击/冲刺，新增 Focus Definition/State、F/手柄北键输入、Skill、动画、0.5秒结束、打断/Owned效果与运动释放；不复制核心循环。独立方案将用于Arena，默认Starter保持基础9节点。T5仍在实施与验证。

T5 增量验证补记：Focus 首跑因前次制作预检异常后未标记方案 dirty，磁盘缺 focus 映射而失败；补充真实映射并保存。后续用例第二次进入在 CD 未完成时错误期待成功，改为查询实际 Update 时钟的资源可用性，单项550fa7bffaec4df8a654524ab47ba478 **1/1通过，5.597秒**。关卡复制测试先暴露 Runner 无有效场景设置与返回资产被卸载两处问题：生产入口现在拒绝无法恢复的未保存现场，创建完成后在恢复场景之后重新加载结果；测试显式安排已保存编辑场景。最终 dfbfb06031ad4a9aa832ef3ad213af62 **4/4 Editor通过，8.422秒**，包括两关校验、独立复制/重载与源不变、放置复制/删除UndoRedo、蓝图owner/禁用条件。

本轮新增产品接口并补回归：CaptureState只读值、owner动作请求、交互解析器、生命写入前规则；规则接收稳定目标事实并返回最终命中或拒绝，非事后补血。PlayMode相关59ef2ccce87e4e52b952922742ae51a1 **24/24通过，14.006秒**（包含T1—T4、Focus及接口新增）。请求中 HitBatchTests 名称没有匹配实际 EntityHitBatchTests，不能声称该批覆盖Entity；另行正确过滤 ddc9932da7544853bc2519b3e0106208 **4/4命中原语通过，0.045秒**。

发现非试玩从Arena存档继续也必须选择其角色方案，因此增加中性的 IPlayerLevelConfiguration，场景身份明确引用自身关卡定义，Player按场景声明捕获方案；Player不反向依赖LevelAuthoring。两个正式场景已通过Unity API绑定并验证 errors=[]。该批主动编译1793198904198→1793198908079，reload1793198916558，Console error=0。最新C#改动后仍需实际试玩与最终相关复验，尚未宣称T5完成。

### T5 实播与退出证据

首次正式试玩被公共规则拒绝：本项目原 EditorSettings 为 enterPlayModeOptionsEnabled=true / flags=1（禁域重载）。记录后临时清除 DisableDomainReload，保留原值待验证结束恢复。第一次实际启动的 Main 动画回调在旧转场仍收尾时发起请求，被 Busy 拒绝；已等待本 FSM 空闲后再请求，并避免首轮自动试玩排队显示主菜单页面。编译中身份矩阵误调用 internal 测试 helper 报错，改为仅测试反射调用原纯判定方法，不扩大公共 API、不伪造磁盘身份。

竞技场正式试玩 request6076486f77f6422fa28b94c9212d4d82 已可交互，源场景TopdownArena、独立方案10节点、出生(-6,-4)，开发存档独立目录见 t5-trial-ready.json。蓝图实际10节点并高亮alive/locomotion/idle。配对键盘F有界回放 t5-live-focus.json：focus=true、ended=true、effects=0。

MCP manage_camera 的合成截图在启动期强制递归 PlayerLoop，产生工具自身 ScreenshotUtility.cs:197 的错误；失败黑图 t5-arena-hud.png 和完整错误回执保留。未修改插件缓存，改用 Unity 原生 ScreenCapture.CaptureScreenshot 的帧末捕获；t5-arena-hud-ready.png 已查看，HUD、地图、按钮及角色可见。保存工具错误后清理 Console，再续验 runtime error/warning=0，不把旧工具错误隐藏成未发生。

通过真实 EUI Binding 的 Button.onClick 回调实播（不是人工鼠标签收）：暂停/继续、保存、读档、重试、保存退出、主菜单继续均成功，回执 t5-live-ui-chain.json；暂停drift=0、读档/重试HP6、继续仍为竞技场10节点，开发写盘前后正式存档文件SHA完全一致。该轮交互请求尚未经后续业务Update就被暂停清空，保存的是false世界值；未称它证明激活。随后等真实交互处理完成，再按保存/读档按钮，t5-world-true-roundtrip.json saved/restored均true，SessionVersion7→9。源方案快照字节留作后续非试玩恢复验证。

停止使用公共 TrialSession.Stop→所属 ShutdownFrameworkAsync；t5-trial-after.json 确认Play=false、Active=false、bridge=false，Framework单场景和isActive/isLoaded均与before一致。正式UI/工具/扩展/两关内容已实现；正继续T6相关最终检查、第二关试玩的视图/选择恢复、设备/构建及交付授权边界核对。尚未普通保存模板、Bump、提交或发布。

## T6 最终相关验证与边界

本轮审查补齐了两个实际契约点：受击使用本击 Braking 且硬直结束清除反应速度；退休接收器拒绝旧引用继续伤害。HUD读取前以BeginProgressRead取得独立暂停/进度租约，阻止竞争退出；读取后再核对界面代次、SessionVersion和当前状态，旧回调不重新进入。新增租约旧释放不解除新租约的回归。

最终代码批主动编译1793201037195→1793201040925，reload1793201048542，确认ready并读Console error=0。最终相关PlayMode **31/31通过、0失败/跳过、17.629秒**，joba9041221b9ba4790a7d61697dacc03d7，含T1—T5生命周期/输入/物理/战斗/快照/恢复/接口/扩展示例及直接受影响EntityHitBatch。Editor **7/7通过**，jobc8540abc013e4dbbae07228c63563d53，含新增topdown纯身份矩阵18种组合，未替代实际切换。框架诊断的正常提交、双异常和三处故障回滚最终定向复验 **5/5通过、1.372秒**，job4f16ba1b013b46a897c30dad3797085c。没有全仓全量重测。

训练场位置试玩第一轮(0,-4)读取太晚，已被敌人击退，不能把当时坐标当初始落位通过；保留t6-starter-position-ready.json。再次从远离敌人的(-9,-6)位置启动，t6-starter-position-safe-ready.json准确匹配且HP6。停止后t6-starter-trial-restored.json：Framework场景/选择恢复，pivotError=rotationError=sizeError=0，2D/正交模式一致。临时域重载设置恢复enabled=true/flags=1，ProjectSettings/EditorSettings.asset无Git差异。

非试玩恢复开发端验证字节，t6-nontrial-arena-restore.json：Completed、trial=false、Arena场景、独立10节点方案、input=true、位置(-6,-4)，证明普通模式不依赖试玩桥选择正确方案。已留在普通模式竞技场供必要人工体验；此时不进行场景切换或代码修改打断体验。自动EUI链路/输入回放不签收手感。

项目中心只读CompareEditingTemplate和完整Scan均 **0错误、0警告、329文件差异**；回执t6-center-comparison/validation。受保护非Assets882文件仅Unity再生成slnx不同，所选6个Package源基线哈希全部一致；2962文件保护归档的SHA再次一致。模板差异文件hash清单单列，不将整个Git平台→俯视的工作区差异当本任务提交，暂存为空。

Windows构建预检：Unity6000.5.4f1、StandaloneWindows64支持=true、Mono2x、5个启用场景；尚未执行可能耗时较长的Player构建。普通保存、Bump、提交、发布和消费部署均未执行。审阅包docs/dev/topdown-2d-delivery-review.md承接具体差异、验证和独立授权边界。T6仍有人工/设备/实际构建及封存消费部分，不标记整个模板交付完成。

### T6 资源身份收尾与人工体验重新就绪

GUID审计发现PlayerGraph、PlayerPowers沿用原平台草稿同路径meta（t6-new-resource-guid-collisions.json），与新配置/图独立身份要求不符。停止Play后使用AssetDatabase创建克隆、SerializedObject重接PlayerScheme、删除本轮旧资产并移回原路径；未手写YAML/meta，未修改模板存储。修正前3份资源及meta保留Library/EmberBackups/topdown-2d-t6-resource-identity。新Graph GUID=f256d44e60e6cf64c8a8cbc4d9bb279f，Powers GUID=bb6a6882345b436439562312ba8a22d9，方案Capture仍为9节点。原脚本/程序集GUID未借此批改写，审计结论限定为新配置/Prefab身份。

Unity MCP刷新assets后Console错误/警告0；相关关卡/方案EditMode复验job7cd9a2f869e34e27b052a2e3a5fe8cce **7/7通过，18.007秒**。t6-new-resource-guid-final.json检查43个非base继承的asset/prefab meta，原平台草稿GUID复用为0。重新执行中心只读CompareEditingTemplate，仍329差异、0错误/警告，并更新活动文件hash清单。没有C#/程序集改动，本批不新增编译结论；此前最终31项覆盖的代码未改变。

用户对先前人工体验请求回复“我每看到播放模式啊”：当时为修正GUID已停止Play，不能记为人工通过。现重新由Framework→Main进入普通Arena（非试玩桥，不写正式存档），打开Game视图，t6-nontrial-arena-after-guid.json记录Completed、playing=true、trial=false、10节点、(-6,-4)；原生截图t6-human-arena-ready.png已查看，Console错误/警告0。保持当前Play供用户体验，期间仅整理文档/只读比较；完整Scan要求Edit模式，待人工体验结束再复验。

### T6 人工通过后的最终Edit检查

用户随后明确反馈“可以操作，体验通过”，Editor竞技场人工体验记为通过，证据t6-human-validation.json；不扩展成真实手柄、分辨率矩阵或Player验收。通过GameLauncher.ShutdownFrameworkAsync正常等待转场、所有者和I/O结束后停止Play。Edit模式重跑中心完整Scan，最终329差异、0错误、0警告，已更新t6-center-validation.json/.txt；Console错误/警告0。enterPlayModeOptions仍DisableDomainReload，与T0设置一致，暂存为空。本批仅记录与只读检查，未修改C#/程序集，不重复宣称新编译或全量测试。

已授权的模板实现、相关测试和Editor运行验证至此完成。当前资源仍只在活动Assets；下一步的项目中心普通保存与Windows64首次构建分别等待明确授权。普通保存不会Bump/提交/发布；构建仅拟输出Library/EmberValidationBuilds/topdown-2d/Windows64/EmberTopdown.exe，预计数分钟至十余分钟，尚未执行。干净消费工程验证需在模板正式保存和适当部署授权之后开展，T6交付仍未全部完成。

## T6 用户追加：攻击归属武器（2026-10-29）

用户要求攻击状态绑定武器、参考元气骑士的扩展方式，并明确选择“近战武器＋可移动射击的远程武器”。此前人工通过仅对应旧玩家Attack实现，不作为此批新行为签收。变更前活动实现保护于Library/EmberBackups/topdown-2d-before-weapon/active-implementation.zip，SHA256=37ACC89F0FF44CBB31B163439C4E5E3E7E6F0F9A7AE01E021772850AC41505F5。

实际新增Player/Weapons内的WeaponDefinition/Spec、WeaponContext、WeaponRuntime和WeaponLoadout；每把武器独立Graph/Definition/Powers、fire资源、攻击方向、命中窗口/去重。通过IWeaponActor隔离角色上下文，默认Module仍为唯一驱动，武器不直接写刚体或结算。玩家移除Attack节点、攻击参数和attack Skill，默认8节点、Focus方案9节点；默认两把武器各4节点ready/windup/active/recovery，近战取得移动/朝向租约，手枪不锁移动。手枪为单次按键的即时射线示例，先墙体截断再选最近敌人，同距离按稳定ID；没有声称完成实体弹丸、背包、换弹、自动连射等系统。

Q/左肩键切换，或TryEquipWeapon(id)；仅稳定可控状态允许，切换取消旧动作/残留命中、不退款、不重置每把武器资源。角色受击/死亡/冲刺和扩展动作抢占武器控制，武器不能抢占其他角色/项目租约。补查发现Focus会与挥剑争用唯一控制，因此新增明确抢占规则和组合回归，避免在Focus.Enter故障。运行观察分别列出角色与武器实例；工具支持武器参数/图，方案复制深复制各武器Graph、Definition、Powers/Skill并分配新ID。确认旧角色攻击四份资产无引用后经AssetDatabase删除；Prefab挂点改为Weapon。

快照升级为未发布开发格式v2：完整冻结当前装备ID、按装配顺序的各武器开放/次数/CD；恢复前检查集合/身份，候选失败不影响旧玩家。只在角色idle/move、所有武器ready时保存，恢复从ready开始；v1明确DataVersionUnsupported，不自动迁移、补齐或覆盖。

首轮编译因新增复制测试缺少Game.Entity.Runtime引用失败，补齐Editor测试asmdef后主动编译通过。后续行为批编译1793203294936→1793203298055/reload1793203306413；补齐Focus抢占后的最终编译1793204119574→1793204123368/reload1793204131508，Player MVID=e190c566-51b8-4697-99a9-fa82959e43e6，Console代码错误0。每批均由Unity MCP显式请求，未以自动刷新代替。

相关PlayMode首次37/37通过（ba86c6ecb8fc4b40845a1341f7d6f41e，20.339秒），补充codec后38/38（63d658fca2b04bfdbe01d2ef8c765bd5，19.368秒）；Focus组合修正后最终 **39/39、0失败/跳过、20.482秒**，jobbc4872875c0a4be6bc4a883d3b78bbe3。过滤仍是PlayerLifecycleTests、TopdownCodecTests与直接受影响EntityHitBatchTests。Editor方案/关卡 **8/8、0失败/跳过、27.154秒**，job9e1a692342584112a5ad94c8c39e155e，含武器配置独立复制及角色无攻击资源校验。未做全仓全量测试。

实播经公共关卡试玩Session request c3f0def782244aaa99a2dc099431a3d0启动Arena。t6-weapons-live-chain.json：武器蓝图4节点实际active、暂停drift0、EUI保存/换武器/读取/保存退出/继续成功，恢复手枪；开发根独立，正式存档前后文件SHA一致。首次模拟手柄添加后未与Input Host配对，movingShot字段实际delta0，不能记为边走边射成功；随后用InputUser正式配对测试设备，t6-weapons-paired-moving-shot.json：玩家move、武器active、向上delta0.978、朝右，截图已查看。测试设备已释放，未更改生产输入配对策略，不把模拟回放扩展为真实手柄/热插拔验收。

通过公共Stop等待运行与I/O后恢复Framework场景/选择，并恢复EditorSettings原enabled=true/flags1；t6-weapons-trial-restored.json。发现HUD新增武器行与进度提示间距不足，经PrefabUtility修改正式EUI资源、公开TryRegenerateCode生成；Binding仅更新时间注释改变，用户Update/进度逻辑保留，仍显式请求编译。资产及生成文件备份位于HUDLayoutBefore。新布局实播截图已查看。

最终普通Framework/Main流程读取隔离验证字节进入Arena（不写正式存档），配对键盘F验证挥剑→Focus抢占成功且会话存活，再装备手枪；t6-weapons-nontrial-ready.json：playing=true/trial=false、玩家9节点、武器4+4、输入开放，Console错误/警告0。现保持此普通Play供新增武器人工体验，尚未收到本批人工签收。

最终中心Scan和CompareEditingTemplate均0错误/0警告、369文件差异，t6-weapons-final-compile-and-center.json及中心报告已更新。55个非base继承配置/Prefab meta的原平台草稿GUID复用0；882保护文件仍仅Unity生成slnx不同，T0原归档SHA再次一致。活动文件hash清单更新，暂存为空。所有武器改动落活动Assets，未普通保存/Bump/提交/发布/消费部署，Windows长时间构建也未执行；后续仍按独立授权边界推进。

### 武器人工交接被编译取消：重新建立最终编译边界

用户反馈切回Unity时触发编译，Play被取消。本批人工体验未完成，不能记为通过。查询MCP确认已回Edit、额外编译1793204567391→1793204570848/reload1793204577816已结束；106个相关源码/asmdef与交接清单SHA均一致，Player MVID仍e190c566-51b8-4697-99a9-fa82959e43e6，未检出代码编译失败。额外编译触发源尚未锁定，不能直接归因于用户操作或某个文档导入。

此前显式编译在最终39项测试之前，之后还整理了Assets文档，交接没有建立“所有测试/资源/文档落盘→最终刷新及显式编译→完成域重载/Console检查→启动Play”的最后边界。现执行该顺序；不新增业务代码、不用旧测试结果替代本次交接编译。之后停止Assets写入，恢复普通Arena供体验；最终编译和重新就绪证据分别记录t6-weapons-handoff-compile-state.json及t6-weapons-handoff-ready.json。

### 武器版本人工通过与保存/构建授权边界

全部落盘后通过MCP强制刷新并主动编译：1793204812524→1793204814417，reload1793204821897，程序集MVID与已验证代码一致。重载时MCP自身短暂WebSocket警告单独保留；正常启动后Console错误/警告0。没有在此后写Assets，普通Arena手枪装备、输入有效、持续稳定，handoff-ready记录compiling/updating=false。

用户最新反馈“没问题，继续吧”，武器拆分版本Editor人工体验确认通过，证据t6-weapons-human-validation.json。正常ShutdownFrameworkAsync等待运行/I/O结束后停止Play；Edit下再次完整Scan和CompareEditingTemplate均369文件差异、0错误/警告，Console错误/警告0。没有新C#/程序集改动，不重复测试或以旧通过替代此轮交接编译。

已完成可在现有实现授权下执行的开发端收尾。下一操作分别请求项目中心普通保存topdown-2d，以及预计数分钟至十余分钟的Windows64构建/启动冒烟授权；不将“继续”或实现授权扩大为Bump、提交、框架发布或消费部署。具体369项差异、保护路径与验证结果已整理到交付审阅文档，等待对具体操作作出确认。

### 项目中心普通保存完成，构建按用户选择暂缓

用户明确答复“授权普通保存”及“暂不构建”。仅执行普通保存，不进行Bump、提交、发布、构建/Player冒烟或消费部署。执行前复核活动topdown-2d 0.1.0身份、369差异/369文件SHA与审阅清单一致、受检资产dirty=0、Editor空闲。现有存储模板（含ParentSnapshot）与编辑记录备份到Library/EmberBackups/topdown-2d-before-first-save，stored-template-before.zip SHA256=46D95918444E6F1B1AC3F7F6198498B9405A0560DA49DEA39797C3FF087AB0E4。

调用项目中心公开正式入口EmberProjectSetup.SaveTemplate，保留原显示名/描述，共复制1050文件（包含base继承内容）；未手写模板文件、版本或hash。保存后version=0.1.0、channel=preview、parentId=base、parentVersion=0.10.0、parentContentHash/frameworkVersion/versionedContentHash均保持；新contentHash=5ef1d9ae0042f8852d4b34566d7cd8bc，原versionedContentHash=61cbe49aa797b1604f293b9756674566，仍未封存。正式回执t6-center-save-receipt.json。

通过MCP刷新资源后，中心CompareEditingTemplate为0差异/0错误/0警告，完整Scan为0差异/0错误/1警告，唯一警告是“版本封存：模板有已保存但未封存到版本的内容”。不以清警告为由越权Bump。当前Console错误/警告0；保存后369个活动文件SHA全部未变，未修改C#/程序集，不额外重跑测试或声称新增编译。ParentSnapshot逐文件复验698/698一致、文件数一致，证据t6-save-parent-snapshot-audit.json。

当前模板存储已包含武器拆分版实现，已不再是创建时base空内容；普通保存不等于本轮内容已封存/发布。用户暂不构建，因此TD10的Windows Player和干净消费验收仍未完成，T6保持部分完成。授权的普通保存操作完成，无Git暂存/提交。

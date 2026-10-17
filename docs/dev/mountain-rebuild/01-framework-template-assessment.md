# Mountain 重制：框架与 2D 平台跳跃模板补充评估

> 实施更新（2026-10-17）：T01—T05/T07公共代码及接合已落盘并通过Unity回归，通过项目中心普通保存；T06复用既有能力，T08未启动。实际API、版本/hash、测试证据和已补齐的两关/UI验收与剩余产品验收见 [公共能力实施记录](03-public-capability-progress.md)。未Bump、提交、推送或发布。下文原扫描基线及拟议接口保留为历史需求，不等同于最新源码或已发布能力。

> 扫描日期：2026-10-16。用途：交给另一个对话评审并逐项实施公共能力补充。本文只是一份实施建议，不代表已修改、测试或发布任何能力。
> 状态：2026-10-17已确认Q01—Q08，并完成两份文档的一致性复核。静态源码事实、拟议接口和实施阶段验收分别标注；当前没有遗留的八项产品决策待答。配套项目实施步骤见 [02-new-project-implementation.md](02-new-project-implementation.md)。

## 1. 接手时先读

用户要在**一个新项目**中，以 Ember **platformer-2d 模板**重制 Mountain，并尽量复用老美术。不能以老项目直接升级 Unity、整体搬运脚本或复制工程代替重制。

本轮授权仅为扫描、分析和两份文档。不修改 Mountain、框架运行代码、模板、场景、资源、包配置，也不创建新 Unity 项目。后续对话必须根据其收到的实施授权推进；本文没有授予提交、发布或消费升级权限。

三层责任边界：

| 层级 | 判定条件 | 本项目的例子 |
| --- | --- | --- |
| 框架 | 已有通用 API 的错误，或与具体玩法无关的公共基础能力问题 | 只有独立复现的包内状态/场景、输入、资源等公共接口错误才进入包；本次尚无已证实项 |
| 2D 平台跳跃模板 | 不带 Mountain 语义、其他平台游戏可直接复用的玩家/关卡初始能力 | 外部运动来源、移动平台承载、通用弹射、检查点与关卡复位协议 |
| 独立项目 | Mountain 的玩法规则、数值、美术、关卡、内容和存档含义 | 挥旗造风、强弱风叠加、蒲公英方向规则、贴纸旗面、三关流程、结尾对比 |

旧项目的缺陷不自动成为框架缺陷；模板已经提供的能力也不应重复开发。

交接时，本文件用于落实公共能力缺口，配套文件用于新项目实现。先核对T01—T08的实际交付状态，再按配套文档第7节的阶段依赖表安排工作；T任务是待实施清单，不是已经交付的接口。产品决定以配套文档[第12节已确认决策清单](02-new-project-implementation.md#12-已确认决策清单)为准，不因发现旧实现与新决定不同而退回旧产品行为。

## 2. 可复查的基线

| 项目 | 2026-10-16扫描基线，后续实施需复核 |
| --- | --- |
| 老项目 | `C:\Users\wuyu\My\Mountain`；Unity Version Control 的 `.plastic` 工作区；未发现可用 Git 远程地址 |
| 老 Unity | `2021.3.23f1c1`；固定步长 `0.016666668`，约 60 Hz |
| 框架仓库 | `C:\Users\wuyu\My\ember-unity-framework`；远程 `https://github.com/wsydet/ember-unity-framework.git` |
| Git HEAD | `79fe3889b744361b5481fb9a90a50a7531c5baa6`；工作区已有大量改动，不能把 HEAD 当成当前 Assets 的完整内容 |
| 框架/宿主 Unity | `com.ember 0.19.2` / `6000.5.4f1` |
| 活动模板 | `platformer-2d 0.4.1`，preview，声明框架兼容版本 `0.19.0` |
| 父模板 | `base 0.8.3`，父内容 hash `1270ce6dfcae3c01d050c09ecfd60337` |
| 模板内容 hash | `373437cccbe9f4ba6e93359da2c8b2fd`；contentHash 与 versionedContentHash 一致 |
| 活动内容核对 | 五个受管目录与 platformer-2d 已保存 Assets 快照共 1,317 个文件逐字节相同，包括 `.meta` |

这里的对照对象是当前已经加载到 `Assets` 的平台模板。已有未提交内容必须保留，不能为执行本文而清理、回退或重新部署模板。

后续实现前读取 [CLAUDE.md](../../../CLAUDE.md)、[仓库架构](../../../Packages/com.ember/Documentation~/maintenance/repository-architecture.md)、[业务目录](../../../Packages/com.ember/Documentation~/maintenance/business-directory-layout.md)、[改动分流](../../../Packages/com.ember/Documentation~/maintenance/unityfarm-change-routing.md) 和 [Unity 验证规则](../../../Packages/com.ember/Documentation~/maintenance/unity-validation.md)。Mountain 不是 UnityFarm；这里复用的是同一套包/模板/产品归属规则。

## 3. 老项目实际范围

五个启用场景依次为主菜单、序章、森林、山顶、结尾。另有禁用的 SampleScene 与不在构建列表中的 DemoLevel，不默认计入复刻范围。

44 个自有 C# 脚本显示，旧项目主要闭环是移动与跳跃 → 挥旗形成风场 → 影响玩家及机关 → 收集贴纸 → 在旗杆编辑旗面 → 检查点复活 → 过关 → 显示本次与上次旗面及死亡/时间统计。新项目的旗面入口已由Q04改为进入安全区即可编辑，旧旗杆触发仅是来源事实。没有发现要求复刻敌人 AI、攻击伤害或装备成长的证据。名为 `Mantis` 的预制体不能仅凭名字判定为敌人。

主要原始依据：

- [PlayerControl.cs](C:/Users/wuyu/My/Mountain/Assets/Script/PlayerControl.cs)：移动、跳跃、墙跳、挥旗、风速、机关附着；源码为旧编码，读文件时 UTF-8 失败应尝试 GB18030。
- [WindArea.cs](C:/Users/wuyu/My/Mountain/Assets/Script/WindArea.cs)、[MoveMachineControl.cs](C:/Users/wuyu/My/Mountain/Assets/Script/Machine/MoveMachineControl.cs)、[CloudControl.cs](C:/Users/wuyu/My/Mountain/Assets/Script/Machine/CloudControl.cs)：风场与载具。
- [MashroomControl.cs](C:/Users/wuyu/My/Mountain/Assets/Script/Machine/MashroomControl.cs)、[DandelionControl.cs](C:/Users/wuyu/My/Mountain/Assets/Script/Machine/DandelionControl.cs)、[BrokenStone.cs](C:/Users/wuyu/My/Mountain/Assets/Script/Machine/BrokenStone.cs)：弹射与塌陷平台。
- [RebirthControl.cs](C:/Users/wuyu/My/Mountain/Assets/Script/RebirthControl.cs)、[LevelManager.cs](C:/Users/wuyu/My/Mountain/Assets/Script/LevelManager.cs)：死亡、检查点、局部世界复位。
- [StickerManager.cs](C:/Users/wuyu/My/Mountain/Assets/Script/Inventory/StickerManager.cs)、[InteractionManager.cs](C:/Users/wuyu/My/Mountain/Assets/Script/InteractionManager.cs)、[FlagManager.cs](C:/Users/wuyu/My/Mountain/Assets/Script/FlagManager.cs)：独特收集和旗面编辑。

## 4. 已有能力与实际缺口

| 需求 | 模板/框架现状 | 处理 |
| --- | --- | --- |
| 基础地面/空中移动、跳跃 | 已有 Move/Jump/Input Feel、土狼时间、输入缓冲、短跳与顶点重力 | 按Q02复用并调参；原版恒速上升与模板轨迹不同本身不构成缺陷，仅原解法仍无法成立时评估T08 |
| 墙滑/墙跳 | 已有通用状态 | 复用基础；原版首次纵向墙跳和离墙帧宽限需对照验证 |
| FSM、能力、效果 | PlayerModule 统一驱动，Entity2D 归属，Powers 与 ActionEffects 分离 | 不搬运老大脚本；只在必要时增加叶子状态/效果 |
| 外部风/持续弹射 | MotionControl 为内部协作契约；Locomotion 与 Motor 每帧写速度 | 需要模板扩展契约；独立脚本 AddForce/写刚体不能保证有效 |
| 移动平台 | 传感器有地面/单向平台，未见支撑物位移与速度承载完整链路 | T02 |
| 弹射蘑菇 | HeadBounce 是带条件的踩头跳，触发与向量语义不同 | T03；不能直接冒充弹射机关 |
| 死亡与恢复 | 已有玩家生命策略、检查点快照恢复 | 世界机关复位与持久收集状态不是当前 Recover 的职责，T04 |
| 多关卡 | 现有普通 GameplayState 以 `GameplayScene` 为目标，PlayerConfig 有场景校验 | T05；保留校验，扩展入口数据 |
| 保存 | base 继承的 PlayerDataModule 有泛型快照/codec、排队和 drain；平台模板的 PlatformerSnapshot v3 保存玩家 | 复用基础，组合项目数据；不是新增通用存储 Manager |
| 相机 | Cinemachine 3 适配器及白盒玩家跟随已存在 | 正式场景需接线；分区触发按 T06 决定是否通用化 |
| UI/音频/输入 | 框架已有；玩家 Action Map 有 Attack=J | EUI 重建旗面；Mountain 的 Act=J 要明确解绑攻击 |
| 攻击/敌人 | 模板有战斗方案与演示，不是 Mountain 需求 | 不作为本轮补齐事项；PlayerHitScene 的 nonLethal 演示不能当敌人系统 |

模板依据：[架构](../../../Assets/Game/Documentation/platformer-2d/PlayerArchitecture.md)、[工作流](../../../Assets/Game/Documentation/platformer-2d/PlayerWorkflow.md)、[规则](../../../Assets/Game/Documentation/platformer-2d/PlayerRules.md)。文档与源码不一致时，以本次实际源码为准，并记录差异。

### 4.1 实际维护源定位

| 证据/扩展点 | 当前文件 | 已确认约束 |
| --- | --- | --- |
| 玩家驱动、保存、暂停、恢复 | [PlayerModule](../../../Assets/Game/Module/Player/PlayerModule.cs) | Save 冻结玩家快照；Recover 走当前世界的原位事务；Pause 会影响 Time.timeScale |
| 运动职责 | [PlayerMotionControl](../../../Assets/Game/Module/Player/PlayerMotionControl.cs)、[PlayerContext](../../../Assets/Game/Module/Player/PlayerContext.cs) | MotionControl 为 internal，单 owner；不能从独立模块任意控制内部通道 |
| 刚体提交 | [EntityMotor2D](../../../Assets/Game/Module/Entity/EntityMotor2D.cs)、[Locomotion](../../../Assets/Game/Module/Player/States/PlayerLocomotionState.cs) | 按模板驱动顺序合成后提交，不靠多个 FixedUpdate 的执行顺序竞争 |
| 入口/恢复策略 | [PlayerSessionContracts](../../../Assets/Game/Module/Player/PlayerSessionContracts.cs)、[PlayerLifePolicies](../../../Assets/Game/Module/Player/PlayerLifePolicies.cs) | 当前 NewGame/FromSlot/ForTrial；位置 provider 只解析，不改世界 |
| 玩家存档 | [PlatformerSnapshot](../../../Assets/Game/Module/Player/PlatformerSnapshot.cs) | 当前 v3；并不代表当前互斥动作或完整世界可恢复 |
| base 存储服务 | [PlayerDataModule](../../../Assets/Game/Module/PlayerData/PlayerDataModule.cs) | `[EmberModule(ModulePhase.Global, Enabled = true)]`；位置在 Assets，并非包内 Manager |
| 游戏场景准备 | [GameGameplayState](../../../Assets/Game/State/GameGameplayState.cs)、[GameSceneScope](../../../Assets/Game/Module/SceneFlow/GameSceneScope.cs) | 一名玩家、显式scope；参与者按请求准备/提交/清理 |
| 包内场景默认值 | [GameplayState](../../../Packages/com.ember/Core/Runtime/State/GameplayState.cs)、[转场实现](../../../Packages/com.ember/Core/Runtime/State/EmberStateMachine.Transitions.cs) | 默认 ScenePath 可由子类覆盖；默认值本身不是框架缺陷 |
| 跟随适配 | [PlayerCameraParticipant](../../../Assets/Game/Module/PlayerCamera/PlayerCameraParticipant.cs) | 正交、WorldSpace Follow、无旋转Aim、会话提交后绑定 |
| 通用镜头/音频 | [EmberCameraManager](../../../Packages/com.ember/Camera/Runtime/EmberCameraManager.cs)、[EmberAudioManager](../../../Packages/com.ember/Audio/Runtime/EmberAudioManager.cs) | 已有注册/切换/覆盖与BGM/SFX接口，不另造全局服务 |

PlayerData 若出现与平台玩法无关的模块缺陷，应先判断是否需要修 base 维护源并同步子模板；不能仅因通用就移到 `Packages/com.ember` 运行层。本次没有提出这样的缺陷修复。

## 5. 框架补充结论

**本次静态扫描没有证实需要立即修改 `Packages/com.ember` 的缺陷。** 这不等于框架经过全面测试。当前发现首先落在模板与产品之间的玩法扩展边界。

后续如怀疑公共 API 有问题，先提供不依赖 Mountain 类和资源的最小复现，记录期望、实际、调用栈与包版本，再确定是否为框架缺陷。不要为了项目方便把 WindwardWind、贴纸、关卡表或旗面数据收入包。

## 6. 模板补充任务单

下面的 T 编号是**拟议任务**，其中提到的接口名只说明职责，不代表当前代码已有这些 API。每项先做最小白盒验证，再合入模板。P0 表示重制的关键前置，P1 表示可在第一段核心闭环之后完成，P2 表示按复用价值决定。

### T01 / P0：外部运动来源与动作接管契约

问题：模板内部 Locomotion、Wall/Ledge、Vertical、Correction 和 Motor 都拥有明确写入顺序。Mountain 需要环境水平速度、短暂反冲和持续弹射，不能让额外 MonoBehaviour 在 Update/FixedUpdate 争抢刚体。

1. 阅读当前 `PlayerMotionControl`、`PlayerContext`、`PlayerController`、`PlayerLocomotionState`、`EntityMotor2D`，画出一次固定步的唯一写入链路。
2. 在模板中定义最小的按玩家实例注册/释放的外部运动来源契约，区分附加环境速度、基础速度替换、有限时间强制上升、输入锁定。不把“强风/弱风/旗”写入通用命名。
3. 明确每个来源的 owner、有效期、优先级、叠加方式、退出清理，以及暂停、死亡、场景卸载、复活时的行为。脚本禁用和对象销毁也必须释放。
4. 保留单一 Motor 提交。区分角色自身速度、环境速度、支撑物位移；避免 T02 平台速度和风速被计算两次。
5. 提供通用调试视图/日志，能追踪当前来源、合成结果与覆盖原因。不要开放无限制的公共字段写入。
6. 在白盒中做两个外部来源重叠、删除其中一个、暂停恢复、换场景的验证；再用简单恒定水平来源证明无需 Mountain 代码也可使用。

完成条件：原有地面/空中/墙/冲刺等方案没有多余速度残留；来源释放不依赖一次必然到来的 OnTriggerExit；外部项目可通过明确接口接入而不改内部私有成员。

Q04已明确按区域划分挑战区和安全区：进入安全区即可编辑旗面并暂停通关计时，世界继续运行。T01需要覆盖通用的“单角色交互锁”：只在实际编辑时取得该玩家的输入/运动职责，释放时只解除自己的锁；不修改Time.timeScale、不停止场景机关，也不回滚期间自然结束的效果。区域安全分类、编辑权限和RunTimer规则属于Windward项目，不写入通用接口。编辑资格不再要求靠近特定旗杆；停表依据区域归属，不依赖编辑面板是否打开。现有PlayerModule.Pause涉及全局时间缩放，不能作为这条交互路径的替代。

### T02 / P0：移动支撑物承载

1. 基于 T01，建立支撑物识别、相对位移、平台速度、离开时速度继承规则。先支持本项目实际使用的二维平移，旋转平台不作为本轮默认范围。
2. 处理接触点/脚底探测、边缘离开、顶面落地、平台下降、快速上升顶到天花板，以及单向平台穿越。
3. 角色被承载时仍可走跳；显式区分风施加于玩家与平台，不能把旧代码的“平台速度减 windDiff”当成公共公式。
4. 以通用移动平台测试横移、升降、暂停、瞬移复位、禁用/销毁；处理瞬移时的承载失效，防止玩家被带到旧坐标。
5. 供项目机关复用支撑物适配器；Cloud/Mantis 的寿命、计数、动画与回家条件仍归项目。

完成条件：载具静止/运动/换向不抖动，连续跳落不重复累加速度；复活后无悬空支撑引用；不同固定步设置下行为按秒计算。

### T03 / P0：通用弹射与临时竖直运动

1. 设计 `LaunchRequest` 一类的数据请求（拟议名），含方向/速度、持续时长、起点与结束策略、可打断规则、来源标识。
2. 用 T01 实现限时恒速上升或初速度模式，明确撞顶、落地、受新弹射、死亡时如何结束。不要把老 `rJumpFrames` 全局字段暴露给机关。
3. 接受一个通用可弹射对象契约，使玩家和移动载具各有适配实现；不要求所有载具成为 PlayerController。
4. 机关只发请求，音效、动画、镜头冲量由事件或表现组件承担。触发去重和冷却由配置决定。
5. 用上/左/右发射、连续两个弹射源、撞顶、空中挥旗被弹射打断进行白盒验收。

Mountain 的蒲公英距离分段和左右不对称倍率由项目配置决定，在提交弹射请求前换算成实际速度与持续时间；接受弹射后，由项目实现挥旗能力/风场刷新。Q05已确认保留旧规则与数值：机器右侧水平0.5倍、竖直0.8倍，左侧水平0.7倍、竖直0.5倍。模板只执行通用请求，不内置这些倍率，也不自动将其对称化。

### T04 / P1：检查点与死亡后的世界复位协议

现有 Recover 恢复玩家快照，不会自动重置场景。已有 `IPlayerDeathHandler`、`IPlayerRecoveryPositionProvider` 可接管策略，但位置解析接口不能偷偷承担世界突变。

1. 定义通用 CheckpointId 与 Spawn/RecoveryPose 的解析入口，避免把某个场景物体的瞬时引用直接保存到磁盘。
2. 增加场景复位参与者及顺序：关闭输入 → 停止运动来源 → 复位机关 → 安置玩家 → 重置相机 → 恢复输入。任一步失败必须停在可诊断状态。
3. 参与者声明“保留/恢复本关初始/恢复检查点记录”策略；模板只提供协议，不规定贴纸、风场、云应选哪种。
4. 区分死亡复活、读档、重新开始一轮、离开关卡；不能统一清空全部世界状态。
5. 模板给出危险区域、检查点、通用复位平台的最小示例，与 PlayerModule 生命周期连通。

完成条件：复活后无旧风/支撑物/动作 owner；玩家被复位一次；收集持久状态可由项目明确保留；暂停、复活动画期间再次触发危险区不重复计数。

### T05 / P0：多关卡入口与组合进度集成点

1. 读取 GameGameplayState / GameplayState / PlayerEntryRequest / PlayerConfig 的实际入口和校验链路，明确目标 SceneKey/LevelId 由谁解析。
2. 为模板提供可配置的关卡入口数据；PlayerConfig 场景匹配改为验证本次入口对应配置，而不是删除检查。白盒和旧单场景默认入口必须继续可用。
3. 保留 GameSceneScope 的准备、提交、启动与清理事务；场景切换中只接受一次入口操作。
4. 设计玩家快照与项目世界数据的组合/恢复集成点，复用 PlayerDataModule 的泛型 codec、排队与 drain。不要把 Mountain 的关卡、贴纸字段加入 PlatformerSnapshot。
5. 新项目自己的进度格式有schemaVersion，区分“可恢复的检查点基线”和“已经提交的项目进度”。Q03已确认检查点续玩：组合存档可复用已验证的检查点基线并冻结最新收集/旗面数据，不要求即时采集当前玩家快照。挥旗、弹射、死亡和旗面半放置等瞬时状态不写入续玩数据，保持现有玩家快照的合法性校验。
6. 用两个最小场景验证新游戏 → 过关 → 保存后返回菜单 → 检查点续玩；加入空中拾取后保存、旗面确认放置后保存的用例。保存失败时不得先卸载场景或清空进度；错误场景键、缺少Player/Scope、恢复失败均有清晰结果。

2026-10-17确认的项目存档范围为单自动存档、检查点续玩、保留已保存的收集与旗面进度、不导入旧项目存档。当前一轮的保存槽由项目指定；上次完成旗面是独立历史记录。自动保存触发、贴纸事务、历史记录更新都属于Windward项目，模板只提供安全的组合/恢复接口，不硬编码槽位数量或贴纸字段。

Q06进一步确认：长按R保存后回菜单并保留本轮进度，主菜单的“重新开始”单独重置本轮，保留设置和上次完成记录。模板应支持保存完成后离场、失败保留现场及明确的新一轮入口；按键、清档范围、覆盖确认、挑战区计时和死亡统计均由项目决定，不扩为框架Manager或模板强制规则。复用PlayerDataModule现有排队/drain时，项目还需按runId区分旧轮次写入，避免旧任务在新一轮初始化后覆盖新进度。

模板接口必须支持检查点后发生的新业务进度与旧检查点基线组合：例如角色挥旗时拾到贴纸，保存后重启应回检查点并保留该贴纸，不尝试还原挥旗瞬间。若沿用PlatformerSnapshot，需从合法检查点取得或构造符合校验的恢复基线；不能在任意时刻强行Capture再绕过校验，也不能把单独两个文件的写入宣称为一次原子进度保存。

需要特别验证：`RequestInPlace` 明确用于同一场景恢复，不能用来换关。普通转场在请求时读取目标 ScenePath，并检查同状态重入；T05 应明确目标配置冻结时机与重入路径。推荐先评估模板子类覆盖 ScenePath、配置化关卡状态或明确的关卡流程协调，保留框架已有事务。只有证明这些扩展点不能表达需求，才另立框架提案，不能先修改包默认场景字符串。

当前 PlatformerSnapshot 为 v3，包含玩家位置/速度/朝向/能力次数与冷却/可恢复计时效果/HP；不包含贴纸、关卡机关或动作精确阶段。不能宣称现有 Save 已实现整个 Mountain 存档。

区域关卡接合：LevelId标识整关，RegionId标识关内区域，CheckpointId标识续玩位置，三者分开。实现Q04前先检查并复用现有区域归属/进入离开检测；若缺少通用检测，在模板场景业务层补最小协议，并与T06相机区域协调，避免各系统各判一套边界。安全区不承载通关挑战/卡点，以及进入安全区开放旗面编辑并停表的规则，均为项目配置和业务，不放入框架Manager，也不意味着安全区自动成为检查点。

### T06 / P2：分区相机适配

已有白盒相机适配可复用；正式 GameplayScene 的 Scope 当前仅挂 PlayerSceneParticipant。先在新项目按现有工作流配置跟随，不因此修改 EmberCameraManager。

如多关卡都需要通用区间相机，模板可增加区域进入/退出适配器：保存明确优先级、定义重叠与退出回退规则、支持复活重选区域，接入现有相机注册生命周期。Mountain 的房间边界、镜头参数、冲量幅度归项目。

验收必须覆盖加载首帧、死亡传送、区域重叠、旗面 UI、场景卸载。Cinemachine 2 的组件和序列化不能直接拿到 Cinemachine 3 上继续用。

### T07 / P1：通用塌陷平台

老 BrokenStone 是接触启动倒计时，离开后可提前消失，恢复前检测占位。可作为模板可选机关实现：稳定 ID、按秒计时、占位恢复、暂停/重置、动画表现分离。旧参数、碎裂图和是否可恢复是项目配置。

该能力按用户确认的分类归模板。可以先在模板白盒做最小组件，森林与山顶再验证复用；不要把它长期留作Mountain专属补丁，也不必为这一种平台建立庞大机关框架。

### T08 / 条件项：原有关卡解法所需的跳跃与墙跳能力

原版起跳后的若干固定帧持续指定 Y 速度，再进入重力段，包含一次纵向墙跳和离墙宽限。2026-10-17用户已确认Q02：保留核心玩法和原有关卡解法，基础手感使用模板优化，不要求逐帧复刻。旧轨迹与数值用于解释关卡需求，不作为逐点一致的验收标准。

优先用模板现有移动、跳跃、输入缓冲和墙跳配置调出稳定手感，再测试旧关卡的关键路线与操作顺序。只有在调参后，原解法仍因通用能力缺失而无法成立时，才启动T08；必须附具体关卡位置、输入步骤、期望落点与现有能力不足的证据。可选补充包括通用的上升阶段或墙跳宽限，但不预先安排严格复刻旧固定帧算法。挥旗回弹与旗跳仍是项目专属状态。

验收关注原路线可达、原核心技巧有效、操作反馈稳定；不得通过新增二段跳/冲刺或改掉关键关卡布局绕过待验证问题。本项目没有需求的额外模板能力继续关闭。Q05已单独确认保留旧风叠加与蒲公英不对称规则、数值，将特殊差异配置化并修复实现问题；这不改变Q02优化基础手感的方向。

## 7. 实施顺序与出口

1. 先复核版本与工作区，建立不触碰现有草稿的实施范围记录。
2. 先完成T01，并核实T05的关卡入口部分；T02、T03随后接入T01。T07可独立推进，在新项目S3塌陷平台接入前交付。T04在S4之前交付；T05的组合保存/恢复部分依赖T04检查点基线，需在S5的保存验收前完成接合验证。不能用“T05整体优先”倒置检查点数据依赖。
3. T06先配置再评估通用化；通用区域归属归T05，必要的区域相机扩展在S4/S7前交付。T08仅在模板调参后仍无法支持原有关卡解法、且证实缺少通用能力时启动，单纯轨迹差异不构成启动理由。分阶段供新项目消费的具体门槛见配套文档第7节，不要求S0等待全部T任务完成。
4. 每项在模板 `Assets` 维护源实现并进行 Unity 验证，模板说明更新到 `Assets/Game/Documentation/platformer-2d/`。
5. 完成后通过 `Ember/项目中心` 的正式保存/Bump 操作封存；**不要手改 `Templates~`、`ParentSnapshot~`、版本/hash 或脚本复制替代中心操作**。
6. 保存、Bump、提交、发布、消费项目升级分别记录状态。消费升级使用 `Ember/UPM Manager`，不修 PackageCache。
7. 交给新项目对话的出口必须包括：实际版本、已完成 T 编号、真实接口及调用示例、测试结果、遗留限制。新项目文档中的拟议接口须替换为实际接口后再编码。

### 7.1 每项的维护范围和回退

| 任务 | 预计维护源，实际文件名由实施设计确认 | 兼容/回退约束 |
| --- | --- | --- |
| T01 | `Assets/Game/Module/Player/`、`Module/Entity/`，对应白盒与文档 | 默认无外部来源时结果与原方案一致；扩展实例释放后可回到原路径 |
| T02 | `Module/Entity/` 的支撑物能力、Player适配、通用白盒 | 支撑物检测可选；不改变静态地面/单向平台既有含义 |
| T03 | 通用机关业务目录与实体请求协议、Player状态/配置 | 无请求时不改变玩家状态图；旧配置有明确默认值 |
| T04 | `Module/SceneFlow/`、Player生命流程、通用关卡复位组件 | 原玩家恢复保留；世界复位由显式策略选择，不默认全场重置 |
| T05 | `Game/State/`、SceneFlow、Player入口/配置/存档接合点 | 保留单场景模板入口、试跑入口和现有v3读取；不能静默把读失败变成新游戏 |
| T06 | `Module/PlayerCamera/`或独立相机区域业务模块、白盒场景 | 未装区域组件时原跟随照常；离场恢复注册和覆盖栈 |
| T07 | 独立可选平台机关模块/资源 | 不让所有项目强制启用；无组件的场景行为不变 |
| T08 | 必要时修改Player Feel/状态阶段/图/编辑器配置 | 先调参并验证原关卡解法；确需新增参数时默认关闭、保持模板原方案行为；参数或策略改动后重启玩家试跑会话，使冻结方案生效 |

回退应撤销本项维护源及其新配置/图变更，保留先前方案和GUID。禁止用重新部署模板覆盖当前工作区作为快速回退。若修改触及base继承内容，要按项目中心父子模板流程处理差异，不能直接手改父快照。

### 7.2 哪些内容不要进入补齐清单

玩家11张序列图的切片元数据异常、贴纸定义与关卡投放范围、旧List序列化风险、输入旧API、Unity2021/Cinemachine2到新版本的资产重建，均属于旧项目资料或独立项目迁移工作。Q07已确认保留13项贴纸定义和11个拾取点：原ID不重排，不补ID2，ID0/3保留定义与素材但不投放或赠送；序章1个、森林10个、山顶0个。此内容范围不构成框架/模板缺陷，也不要求模板增加连续ID校验或补齐未投放内容。没有运行复现的异常不能写成框架已确认bug。

Q08已确认：在新项目用原图重新切片并逐动作验收，缺失的额外风场声暂不播放，保留已有挥旗声。历史meta和缺失音频不再作为实施前置。切片、Sprite重绑定和静音配置均属独立项目；模板仅需正常接受新动画/可选音效，不为旧素材缺失增加专属恢复系统，也不修改Mountain源文件。

## 8. 跨任务验收矩阵

| 场景 | 通过标准 |
| --- | --- |
| 默认方案回归 | 原有移动/跳跃/墙/可选能力不因扩展默认开启而变化 |
| 多来源叠加 | 风、平台、弹射之间组合唯一；对象销毁后不残留贡献 |
| 生命周期 | 暂停、恢复、死亡、退出菜单、换关后 owner 与引用归零或按协议重建 |
| 单角色交互锁 | 锁定交互玩家时，风场寿命、移动平台、云与其他世界更新继续；释放交互锁不回滚世界，也不解除其他owner的控制 |
| 区域进入/离开 | 普通跨区、重生、读档与换关都得到正确区域归属；项目据此开放/关闭编辑权限和停表，不依赖相机混合完成或UI开关 |
| 两关切换 | 只有新关玩家接受输入，旧相机/机关不再更新 |
| 保存合法性 | 非法瞬时玩家快照被拒绝；检查点基线与已提交项目进度可以独立于当前动作组合保存；已接受数据不随之后动作改变 |
| 检查点续玩 | 空中拾取/旗面确认放置后保存，重启回检查点并保留成果；瞬时风场与机关按复位策略重建 |
| 保存后离场与新一轮 | 保存成功后才返回菜单；失败保留可重试现场；重开前处理旧轮次待写任务，旧存档不能覆盖新轮次，项目指定的历史/设置数据不被统一清除 |
| 项目依赖方向 | 模板和包均不引用 Windward 项目命名空间、Mountain源资产 GUID 或贴纸表结构 |
| 验证证据 | 本轮编译、白盒实测、源/快照差异记录分别存档；历史通过结果不能替代 |

## 9. 已确认决策与接力边界

统一决策与执行映射由 [新项目文档第 12 节](02-new-project-implementation.md#12-已确认决策清单)维护，Q01—Q08全部已确认。下面是框架/模板执行者需要知道的摘要；具体项目规格、阶段与验收以配套文档正文为准。

2026-10-17已确认工程名Windward、目录 `C:\Users\wuyu\My\Windward`、独立Git、Windows 64位、键盘＋手柄及验证顺序，中文展示名暂定《风径》；具体结论以配套文档为准。模板补充验收应支持两种输入设备，独立项目的旗面操作也纳入手柄验收范围。

同日确认Q02：保留核心玩法和原有关卡解法，以模板优化基础手感，不要求逐帧复刻。T08按此结论缩为有实测证据才启动的条件项；新项目以原路线和技巧成立作为验收重点。

同日确认Q03：单自动存档、检查点续玩，保留已保存的收集和旗面进度，不导入旧存档。T04/T05需要支持按检查点重建世界并应用已提交进度；不增加任意动作中断点恢复需求。

同日确认Q04：关内按区域划分，进入安全区即可编辑且暂停通关计时，离开后恢复符合运行状态的计时；安全区不承载通关挑战/卡点，安全区停留及编辑期间世界继续运行。停表覆盖整个安全区停留时间，关掉编辑面板但仍在安全区也不计时。T01需提供通用单角色交互锁，实际编辑时才锁玩家；安全区与旗面业务留在独立项目，不采用全局暂停或必须靠近旗杆的限制。此处约束针对安全区/编辑，不取消正式暂停菜单自身的暂停行为。

同日确认Q05：保留旧风场合成规则、数值和蒲公英对机器的左右倍率差异，将接收对象与方向差异配置化；修复重叠来源错误清空、重复触发及退出/销毁清理等实现问题。风场矩阵和机关配置归独立项目；模板提供通用外部运动与弹射能力。不能把特殊差异本身判作缺陷而归一化，也不能仅据旧项目实现问题认定框架缺陷；基础移动手感仍遵循Q02。

同日确认Q06：本轮跨关累计挑战时间，挑战区游玩和死亡/复活动画计时，安全区、主菜单/暂停/设置、加载转场和结尾不计时；到达安全区立即停表，最终通关判定时冻结成绩。死亡在唯一死亡事务被接受时计一次，不等动画结束。长按R约1秒保存后回菜单，继续游戏仍从检查点恢复；“重新开始”通过独立入口确认后重置本轮检查点、收集、背包、旗面和统计，保留设置及上次完成记录。具体RunTimer和数据事务步骤见配套文档5.6/5.7，T04/T05只承接通用生命周期与保存能力。

同日确认Q07：保留现有13项定义和11个拾取点，原ID不重排，缺号及未投放内容暂不补齐。独立项目保留ID0/3的定义和资源，但不加入初始背包或新增获取途径；不创建ID2，维持序章1、森林10、山顶0的分布，并逐点验证可达、拾取及存档去重。如显示收集完成度，以正式投放的11个目标为准，不以13项定义数为分母；这不额外要求增加收集进度UI。

同日确认Q08：原图在新项目重新切片并逐动作验收，缺失风场声暂不播放，已有挥旗声继续复用。执行步骤与来源见配套文档6.1/6.3；A18/A19分别验收11张序列图对应动作和两类声音的区分。静音是已接受的首版差异，不等待旧音频补全，也不悄悄替换为其他声音。

本轮交付仅为两份文档，未修改项目代码、资产或包配置；审计备份和检查报告只在忽略的`.utmp`目录。2026-10-17复核了8项决定的正文/步骤/验收对应关系、本地链接及标题锚点；旧项目6项来源摘要与扫描基线一致。没有启动Unity或改写Mountain，不宣称编译、运行、性能或视觉还原验收已经通过。后续仍须取得T任务真实交付与A01—A19实测证据；中文展示名暂定及Git远程地址未指定不阻止本地实施。

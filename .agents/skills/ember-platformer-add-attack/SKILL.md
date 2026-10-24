---
name: ember-platformer-add-attack
description: 为 Ember 平台玩家新增或连接攻击招式，接入能力、收刀、动画和命中窗口，并验证完整战斗方案。普通攻击变体复用现有状态；仅调已有数值或替换图片分别交给手感与动画技能。
---

# 平台玩家攻击接入

读取项目 AGENTS.md、CLAUDE.md 及适用规范。Unity 实时核对 Application.dataPath，调用 `Ember.Core.Editor.EmberProjectSetup.GetTemplateSkillExecutionBlockReason("ember-platformer-add-attack")`；非空时停止业务写入并说明项目中心修复入口。清单版本只是下限，还须确认正式平台模板体系、Player 模块启用及下述真实 API 存在。没有 Unity 连接只做可证实的分析，遵守项目首次失败即停止自动验证的规则。

## 目标与候选

区分窗口编辑方案、下次白盒方案、项目默认与运行冻结会话。保留草稿、场景、稳定节点 ID、GUID、已有布局。内置只读方案先用 `Game.Player.Editor.PlayerSchemeStructureWorkflow.CopyIndependent(source, schemePath, graphPath, controllerPath)` 创建完整独立副本，三个目标须不存在；不要通过复制 YAML 或解除只读标记修改内置资源。

继承用户已确认的玩法与授权，只补会影响结果的缺失设计：地面/空中/通用、站桩/移动、普通/停顿连接、冲刺指定/续接、能力和资源、取消开放、收刀、伤害反应。第四刀、重斩、循环连接通常是段配置与连接，先复用通用攻击状态。只有现有动作模型不能表达的独立行为才转 `ember-platformer-add-state`，不要每刀新建 C# 状态。

先查现有序列帧/Clip，再按需求提供外部素材或 AI 生成选择。列出动作覆盖、帧数、帧率、尺寸、枢轴、朝向、许可及缺项；外部许可查真实来源，AI 不冒充现成授权素材。未选定素材不擅自定为正式角色资源；可先完成配置与隔离验证。独立刀光可复用不同偏移与时序，不承担命中。

## 编辑入口与约束

先核对这些类当前源码，字段与示例数值以目标项目为准：

- `PlayerAttackProfile` / `PlayerAttackSegment`：总开关、段 ID/开关、AbilityId、环境、时长/速度、各取消门、运动、RecoveryId、Windows、Visual。
- `PlayerCombo`：GroundEntry/AirEntry/DashEntry；Connections 的 Source/Normal/Delayed/DelayedEnabled。停顿开关关闭可保留原特殊收刀拓扑，不能因此删节点或免除绑定校验。
- `PlayerHitWindow`：起止、单次采样、重试策略、层/队伍过滤、Payload、形状关键帧；Box/Circle/Polygon 保持在段时钟坐标中。持续区间左闭右开；合法多形状和多窗口不能靠扩大身体碰撞体替代。
- `PlayerAttackVisual`：Enabled/Frames/OpensAt/Fps/Scale/Angle/Offset，独立表现读取冻结攻击时钟；身体闪烁不能改刀光 alpha。

使用 `PlayerCombatExampleWorkflow.ApplyConfiguration(scheme, attack, combo, newClips)` 提交完整候选。newClips 为新增攻击节点 ID→AnimationClip，例如 `combat.attack.fourth`；已有段只改参数时传空字典，走保留图和 Controller 的路径。结构变化走联合预检/Undo事务，保留生命与冲刺根装配。失败不绕过 FSM、动画键或所有权校验。该入口不自动保存、不切默认。

已有完整方案可在 `Ember/Player/玩家开发工具` 的“攻击与命中”读取独立候选，配置、选择新增动画并联合应用。源变化应核对冲突，不能用重新读取覆盖用户尚未处理的草稿。原生命中编辑通过 `PlayerHitShapePreview.Open(feel, segment, window, frame, shape)` 进入独立预览阶段，原生 Edit Collider 只改临时形状，显式应用才改配置草稿；不要自动关闭已有 Prefab/预览阶段。

首次建立示例可用 `InstallThreeHit(scheme, first, second, third)`，仅允许尚无攻击配置/basic-attack 的方案，Clip须明确选择；不要对现有招式重复安装。需要冲刺出招时用 `EnableDashFollowup`；生命/反应用 `InstallLife`。它们是独立装配入口，不表示新增攻击必须开启生命、冲刺、HUD或额外能力。受击无敌要求显式效果容器，HUD开启时按项目 EUI 工作流创建/绑定/生成。

维持一节点一动画槽和独占覆盖 Key。可选收刀片段缺省时合法回退 Idle；攻击 Clip 必须存在。动画只写 Sprite 子树，不用 Animator 自动转换或事件驱动玩法。动作速度同时影响逻辑窗口与视觉，不因帧长静默修改确认的动作时长。局部替换动画转 `ember-platformer-replace-animation`，仅调已装配攻击数值转 `ember-platformer-tune-feel`。

## 验证与交付

先完整方案静态校验，再通过 Unity 编译、逻辑/PlayMode、正常主流程与白盒实际同驱动验证，分别记录证据：

- 新段起手/接续、取消边界、按住不连发、最后一刀不补旧起手；关闭/缺资源零消费；普通与停顿意图不漂移。
- 左右方向、脚底、命中窗口/账本/过滤、伤害与反应、刀光移除不改战斗、暂停冻结和清理。
- 复制不改源、冻结A后编辑B不影响当前/恢复候选、多实例隔离；原ID/布局、Undo/Redo与保存重载。
- 有生命时验证无敌、死亡确认和恢复；正式 Main→新游戏/继续与白盒使用同一准备/输入/FSM/命中流程。隔离夹具不能替代实际入口。

观察使用玩家蓝图/开发工具的现有按需诊断，不常态每帧刷日志；记录方案、输入、固定步长、配置与实际结果。测试变体留隔离配置，不把正式三连击图扩成所有机制演示。

交付资产路径、图/配置差异、素材来源及参数，明确实现完成、验证通过、未执行和阻塞。必需表现/命中/实际验证缺失不能称新增攻击完成。普通保存、下次试跑、设默认、模板保存、Bump、Git提交/发布分别遵守已有授权，不因本技能扩大权限。

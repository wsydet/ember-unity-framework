# 玩家攻击、受击与恢复

本说明属于 platformer-2d 及合法派生模板。入口为 **Ember → Player → 玩家开发工具 → 玩家配置 → 攻击与命中**。使用前确认 Player 模块启用。内置“通用攻击模板1”与“空洞骑士风格”“蔚蓝风格”同级，只读；它从空洞手感独立复制，使用自己的图与 Controller，不会自动成为项目默认。

## 在 Unity 中直接体验

仅体验内置方案不需要复制、保存或设为默认。以下白盒方式会更新本机的下次白盒选择，不改变 PlayerConfig 的项目默认。

1. 在非 Play 状态打开 **Ember → Player → 玩家开发工具**，将窗口的编辑方案选择为内置 **通用攻击模板1**。已有攻击候选草稿无需重新读取、应用或丢弃；不要点击“重新读取当前方案（替换此攻击候选）”。
2. 切到“手感白盒”，将“Gameplay 目标”选择为“白盒场景 · 独立开发库”。出生位置可设为 (0, 2)、速度 (0, 0)，在示例地面上方落地。若要之后恢复自己的白盒设置，先记下原方案、位置、速度及目标。
3. 点击“应用当前白盒设置并播放”，待正常进入 Main 后点击“新游戏”。不要直接打开并播放孤立 Gameplay 场景。若 Unity 提示处理场景修改，先自行保留修改，不丢弃已有内容。
4. 点击 Game 视图取得输入焦点。A/D 左右移动，空格跳跃，左 Shift 冲刺，J 攻击；手柄对应左摇杆、南键、右肩键、西键。连续分次按 J，在接续窗口内连接三刀；一直按住 J 不会自动连击。
5. 向左接近 x=-2 的沙袋，面朝沙袋测试三连击与左右刀光。三刀均命中且无其他攻击时沙袋 HP 从 30 变为 23；默认不显示玩家血条，不能把没有血条当作生命未启用。
6. 继续向左到 x=-8 附近接触陷阱，观察受击、身体闪烁和短暂无敌。持续接触可死亡，随后点击“恢复”，观察玩家恢复和世界保留。
7. 在同一次 Play 内，等角色回到可保存的普通移动/待机阶段，再使用保存返回及继续检查流程。停止 Play 后下一次试跑使用新的隔离存档库，不用于保留正式进度。

结束后停止播放即可。没有必要点击“设为项目默认”或保存内置资产；原有配置及攻击候选草稿应保留，白盒设置可按步骤2记录恢复。

**正式场景体验的区别：**“播放正式场景（项目默认）”和高级区“正式流程验收（新隔离存档库）”使用项目默认，不会因为窗口选中了通用攻击模板1就改用它。现有指定方案正式体验入口是 Editor API `PlayerDevelopmentEnvironment.PlayFormalWithScheme(scheme)`，尚无对应的专用面板按钮。该 API 只为本次 Play 指定方案并隔离存档，不改项目默认和白盒偏好；可由 Unity MCP 调用，然后仍从 Main → 新游戏进入。普通手动体验优先使用上述已有白盒按钮。

### 已实现范围与验证边界

正式示例包含从空洞手感独立复制的完整方案、三连击、收刀与接续、锁向、原生命中窗口、沙袋、六帧刀光、陷阱、生命/受击/无敌/死亡恢复。编辑工具包含攻击候选联合提交、命中预览、独立结构复制，以及添加攻击 Skill。停顿派生、两种冲刺出招、空冲惯性和第四攻击扩展已用隔离配置验证，不是正式示例中额外制作的招式。

T03 已验证当前平台模板入口、无关 base 模板菜单隐藏与 Skill 限制，以及 Player 模块关闭/重新启用时的操作保护、测试候选保留和原窗口恢复。合法派生、缺失/无效身份、消费项目身份及实际跨模板旧窗口草稿恢复仍未验证，当前项目不为此切换。六帧刀光已在正式 GameplayScene 以临时 0.1 时间倍率完成左右方向的真实渲染捕获与逐帧检查。故障验证覆盖候选创建/进入失败，以及第一刀运行中实例状态回调抛错后的会话、输入、物理、动画与刀光清理和重复停止幂等；未破坏正式图资产。以上边界不写作全部验收通过；消费项目升级和发行构建不在本次交付范围。

## 编辑完整方案

1. 选择内置方案，创建可写副本。只改数值可以复制完整配置；添加动作拓扑时用结构独立复制，保留节点 ID、布局和独立 Controller。Editor API 为 `PlayerSchemeStructureWorkflow.CopyIndependent(source, schemePath, graphPath, controllerPath)`，三个目标必须不存在。
2. 在“攻击与命中”读取攻击候选。候选与资产、正在运行的冻结方案分离；重新读取会替换该候选，先处理自己的草稿。
3. 编辑段 ID、开关、AbilityId、环境、时间、移动/取消窗口、命中窗口和刀光。普通新招式复用 `PlayerAttackStateDefinition`，每段对应独立节点，不必复制状态类。
4. 编辑 Combo 连接候选，新增段提供对应 AnimationClip。点击联合校验并应用；缺动画、悬空连接、错误能力标识或不合法形状会拒绝。提交支持 Undo/Redo，但不会自动保存或设默认。
5. 在已应用时间轴下打开原生命中编辑。独立预览显示角色姿态和原生 Collider；可看左右朝向及逻辑秒，显式应用才改配置草稿。退出预览不保存正式资产。已有 Prefab/预览阶段不会被自动关闭。
6. 保存完整方案；结构变化还需保存对应图和 Controller。通过正常 FrameworkScene → Main → 新游戏/继续试跑，不能直接启动孤立 Gameplay 场景代替准备链。

批处理和 Skill 使用 `PlayerCombatExampleWorkflow.ApplyConfiguration(scheme, attack, combo, newClips)`，`newClips` 只提供新增攻击节点 ID 到 Clip 的映射，例如 `combat.attack.fourth`。只改参数且拓扑匹配时传空映射，图及 Controller 保持原对象。已有生命和冲刺装配会保留。

## 独立开关与时间规则

| 配置 | 作用 |
| --- | --- |
| `Feel.Attack.Enabled` / 段 `Enabled` | 总攻击与单招式启用；关闭攻击不关闭生命和受击 |
| 段 `AbilityId` | 指向 Powers 的能力身份；装配、开放和次数分别决定资格；Ability 本身不提供冷却 |
| 段时间、`Speed`、取消门 | 逻辑秒驱动本体、收刀、接续及刀光；动画结束不替代窗口截止 |
| `Combo` | 地空起手、普通连接、停顿派生和冲刺起手配置 |
| `Life.Enabled` / `ShowHealth` | 生命与可选血条独立；血条要求生命，关闭血条仍扣血/死亡 |
| `Reactions` | 位移/硬直、受击动画、伤害无敌、反应保护和身体闪烁 |
| `Recovery` | 恢复生命策略和位置来源；普通继续读取保存 HP，恢复按策略处理 |

每次新攻击锁定方向；下一刀读取最新输入。按住攻击不会自动刷新请求，末刀本体旧缓存不会补第一刀，收刀中须新按起手。普通请求不会等待成停顿派生；冲刺续接只携带普通下一段。冲刺指定攻击与续接普通段是两种配置策略。

命中支持 Box、Circle、Polygon、形状关键帧和多个窗口。默认每窗口按实体去重，不按 Hurtbox 数量重复扣血；不同窗口有独立机会，仍尊重无敌。短窗口跨步会采样，平移扫掠不等于任意旋转形变的连续精确碰撞。刀光仅为表现，不承担命中，不跟随身体闪烁 alpha。

## 正式示例与资源

方案：`Assets/GameResource/Resources/Config/Player/BuiltinSchemes/通用攻击模板1.asset`；图：`Assets/GameResource/Resources/Config/Player/GeneralAttack1Graph.asset`；Controller：`Assets/GameResource/Player/GeneralAttack1.controller`。

| 示例参数 | 第一刀 | 第二刀 | 第三刀 |
| --- | --- | --- | --- |
| 段 ID | first | second | third |
| 本体时长（秒） | 0.45 | 0.50 | 0.60 |
| 命中窗口（秒） | 0.10～0.25 | 0.10～0.25 | 0.10～0.25 |
| 伤害 | 2 | 2 | 3 |
| 击退/击飞向量 | (2, 1) | (2, 1) | (5, 5) |
| 硬直（秒） | 0.15 | 0.15 | 0.35 |

窗口为 Box，偏移 (3.2, 0.2)、大小 (5.6, 4)，正向最远边界距玩家原点 6 单位。刀光缩放 3.2、偏移 (2.4, 0.2)，在 0.1 秒开始、24 fps、六帧；它是 AI 生成透明序列图，来源和导入约定见 `Assets/GameResource/Player/Combat/SlashSheet-source.md`。空冲退出速度倍率为 0.25。这些是体验确认后的示例值，真实资产优先。

方块身体使用 Animator 播放 `.anim` 中的 Sprite 子节点缩放和位移曲线，动作由玩家 FSM 选择。受击后的基础动作引用 `Assets/GameResource/Player/Combat/FeelFix/` 下的片段，明确将 Sprite 局部位置归零，避免短受击打断后残留抬起位置。刀光由独立表现会话按攻击逻辑时钟播放序列帧，不承担命中检测。

示例生命 10，受击无敌 0.6 秒，身体闪烁最低 alpha 0.3、8 Hz，默认不显示血条。两套场景均有 `CombatExample`：沙袋初始 HP30、非致死，位置 (-2, 0.8)；陷阱位置 (-8, 0.25)，接触伤害2、尝试间隔0.25秒，受目标无敌约束。动画、刀光、沙袋与陷阱资源位于 `Assets/GameResource/Player/Combat/`。正式仅呈现三连击，停顿/冲刺变体由隔离测试配置覆盖。

隔离配置由测试创建并在结束后清理，不是另一套需要部署的正式方案：

| 变体 | 测试源码（相对于 `Assets/Game/Module/Player/`） |
| --- | --- |
| 三连击、动画回退、锁向及取消 | `Tests/PlayerThreeHitTests.cs`，`WithThreeHit` |
| 停顿派生、能力关闭/恢复与提示 | `Tests/PlayerDelayedAttackTests.cs`，`InstallDelayedTest` |
| 两种冲刺出招、普通续接、空冲惯性 | `Tests/PlayerCombatDashTests.cs`，`InstallCombatDashTest` |
| 受击、生命策略和世界保留 | `Tests/PlayerReactionTests.cs`、`Tests/PlayerLifeModuleTests.cs` |
| 短窗口、形状、过滤和同批结算 | `Tests/PlayerHitDetectionTests.cs`、`Tests/PlayerCombatPhysicsBaselineTests.cs` |
| 新增第四段、生命/冲刺保留、Undo/Redo/重载 | `Editor/Tests/PlayerSchemeStructureWorkflowTests.cs` |

运行测试使用实际测试类名；例如前三个运行测试文件属于 partial `PlayerStateTests`，不能把文件名直接当类名筛选。自动物理夹具请求 0.02 秒/30 fps 与 0.01 秒/120 fps 两组条件；目标帧率设置不等于测得的显示刷新率。正式入口和白盒仍须分别实播，测试夹具不能替代主流程验收。

可选血条采用 EUI 的 `EUIPlayerHealthItem`，位于 `Resources/UI/Module/Player/Prefabs/`，由 Gameplay 页面绑定当前会话并在换玩家时释放旧订阅。不要手改生成 Binding。

## 死亡、保存与恢复

默认死亡进入 `life.dead`，Gameplay 提示点击“恢复”。恢复经现有场景状态机原位联合提交新玩家；候选准备成功前不会先传送旧角色，不隐式重置沙袋或世界。攻击、收刀、冲刺机会、受击等无法完整快照还原的临时阶段会在底层保存捕获处拒绝，不能绕过 UI 强存。

自定义死亡实现 `IPlayerDeathHandler`，以 `HandlerId` 对应 `Life.CustomDeathHandler`；自定义位置实现 `IPlayerRecoveryPositionProvider`，以 `ProviderId` 对应 `Recovery.CustomPositionProvider`。`ResolvePosition(PlatformerSnapshot)` 只解析候选位置，不移动当前角色或重置世界。缺处理方、错误版本、能力装配不兼容必须明确拒绝；旧 v2 生命数据不能静默猜测，可走显式迁移接口。

当前编辑、下次试跑、项目默认、运行冻结 A 是不同身份。修改资产 B 不改变当前及恢复候选 A，下一新会话才使用新配置。Editor 的 `PlayerDevelopmentEnvironment.PlayFormalWithScheme(scheme)` 仅对本次正式场景验收注入指定方案并隔离存档，不改变项目默认或白盒偏好；普通 `Play(true)` 仍验收项目默认。

## 诊断与限制

- 不出招：检查攻击开关、段环境/Ability、输入通道及请求版本/截止；不要只看 Clip 是否播放。
- 联合提交失败：查看首个字段错误；检查 Combo、图归属、独占动画键及新增 Clip，不删除旧节点规避校验。
- 不掉血或不受击：分开核对伤害分类、队伍/层、账本、伤害免疫与反应保护；霸体并不必然免伤。
- 恢复失败：检查自定义处理方、快照版本/装配及准备错误，保留当前会话，不先改位置或 HP。
- 窗口不可用：检查正式活动模板和 Player 启用状态。旧窗口保留草稿，不通过改模板记录或解除内置只读绕过。
- 低渲染帧率可能跳过可见序列帧，但逻辑窗口按游戏时间推进。暂停冻结逻辑和表现，恢复不补发旧输入。

使用 [攻击接入 Skill](../TemplateSkills/ember-platformer-add-attack/SKILL.md) 完成材料选择、配置、图/动画接入与验证；新增独立行为用状态 Skill。正式验收还需结合当前项目的实际主流程、输入设备与碰撞环境，测试通过不等于任意消费项目已升级或发行构建已验收。

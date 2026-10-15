# 参数与依赖

路径均相对于真实项目根；按当前 `PlayerSchemeWorkflow.Configuration` 和 SerializedObject 读取实际值，不使用本文示例作为默认值。`u` 表示世界单位，`s` 表示游戏秒；值必须有限。Inspector 的 Min/Range 是编辑提示，最终边界以完整 `PlayerSchemeWorkflow.Validate`、Feel.Capture、PlayerSchemeValidation 和对应配置 Capture 为准。

## 资产路由

| 组 | 实际资产/源码 | 继承与作用 |
|---|---|---|
| 运动 | scheme.Feel.Movement / PlayerMoveFeel.cs | 地面与空中水平、冲刺、下穿、落地、翻滚、挂边几何 |
| 跳跃 | scheme.Feel.Jump / PlayerJumpFeel.cs | 普通跳参数、短跳、竖直组合、墙面、二段跳、踩头 |
| 输入窗口 | scheme.Feel.Input / PlayerInputFeel.cs | 缓冲与土狼，不等于悬停或起跳响应 |
| 能力 | scheme.Powers 的 AbilitySlots/SkillSlots | slot.Enabled、definition.DefaultOpen、次数/CD分开；按稳定 ID 找槽，不固定数组下标 |
| 效果 | scheme.EnableEffects + ActionEffects | 动作效果唯一来源；不是图中 Definition，来源绑定/动作持有/独立定时按现行所有权 |
| 规则 | scheme.RuleMode / Rules | 三个 air 入口附加查询条件可限制进入；旧模式保持，不绕过基础许可 |
| 表现 | scheme.Animation、Controller/Clip、玩家 Prefab | 与物理参数分离，调手感不自动改帧长、碰撞尺寸或摄像机 |

动作可用至少分三层：装配启用；新游戏默认开放或当前进度开放；运行时次数/CD、输入、支撑/几何、基础规则与附加规则许可。编辑器中“启用”不能证明当前角色能立即进入；旧存档恢复的进度不会自动取新的 DefaultOpen/补满次数。

## 水平运动与输入窗口

| 字段（Movement 内） | 单位/范围 | 条件与说明 |
|---|---|---|
| `_speed` | u/s，≥0 | 基础地面目标速度；关闭独立空中时也用于空中 |
| `_accelerated` | bool | 关闭即直接达到目标速度；开启需图含 Accelerate/Decelerate |
| `_acceleration/_braking/_turning` | u/s²，≥0 | 起步/同向、松键或超速、反向三条分支；零可能无法加速/停止 |
| `_independentAir` | bool | 关闭继承整套地面水平配置，不只继承速度 |
| `_airSpeed` | u/s，≥0 | 独立空中开启时有效 |
| `_airAccelerated` | bool | 独立空中下的渐变开关；关闭即时响应 |
| `_airAcceleration/_airBraking/_airTurning` | u/s²，≥0 | 独立空中渐变时有效 |
| `_enableJumpResponse/_jumpResponseDuration` | bool / s，≥0 | 有效起跳早期且有水平输入时直接响应；不替代缓冲/土狼 |

Input 的 `_enableJumpBuffer/_jumpBufferDuration`、`_enableCoyote/_coyoteDuration` 单位 s、≥0；两时长即使关闭也必须有效。关闭缓冲仍保留首次物理决策；土狼只给起跳资格，边缘悬停另有开关。

水平计算在 `PlayerFeelRuntime.CalculateHorizontalSpeed`：按当前支撑选择地面/空中配置，夹住输入[-1,1]生成目标，按起步/刹车/反向分支 MoveTowards。估算从静止到 v 的时间 v/a、松键停止时间 v/b、距离 v²/(2b)，仅适用于正加速度、恒定输入、无碰撞/接管的连续近似；实际固定步有离散误差。即时模式不要靠增大未生效的 a/b 调响应。

## 普通跳与竖直组合

| Jump 字段 | 单位/范围 | 条件 |
|---|---|---|
| `_mode` | HeightAndApexTime / Direct | 两种输入模式互斥，不混改非当前模式数值声称生效 |
| `_height/_apexTime` | u / s，均>0 | 高度/到顶模式，需要纯竖直向下世界重力 |
| `_launchSpeed/_gravityScale` | u/s >0 / 倍率≥0 | 直接模式；零重力不能估算有限到顶和滞空 |
| `_enableShortJump/_shortJumpMode` | bool / 两种策略 | 仅本次有效普通起跳松键触发 |
| `_releaseSpeed` | u/s，>0且≤有效起跳速度 | ReleaseSpeedLimit 策略 |
| `_minimumHeight/_releaseMultiplier` | u≥0 / [0,1] | MinimumHeightVelocityReduction；碰顶或已结束上升不强求最低高度，削速一次 |
| `_enableApexGravity/_apexSpeed/_apexMultiplier` | bool / u/s≥0 / (0,1] | 绝对竖直速度在阈值内使用顶点倍率，优先于下落倍率，不相乘 |
| `_enableFallGravity/_fallMultiplier` | bool / >0 | 下降且不在有效顶点区间生效 |
| `_enableFallLimit/_fallLimit` | bool / u/s >0 | 最大向下速度，结算包含本物理步重力增量 |
| `_enableEdgeHover/_edgeHoverDuration` | bool / s≥0 | 自然离地窗口；起跳/落地取消，不等于土狼时间 |

基础高度/到顶模式由源码换算 `launch=2h/t`、`gravityScale=(2h/t²)/(-Physics2D.gravity.y)`。直接模式基础估算 `t=v/g`、`h=v²/(2g)`，g 是向下重力加速度正值。都未计短跳、顶点/下落重力、悬停、限速、碰撞和离散步；EstimatedHeight/EstimatedApexTime 名称中的 Estimated 必须保留。完整滞空不能把 JumpDuration 翻倍当实测。

面板中的“下落加速／重力修正”就是 `_enableFallGravity/_fallMultiplier`。倍率大于 1 加快下降，1 不修正，小于 1 减缓下降；有效顶点区间优先，下落限速仍在最后生效，不保证所有落地高度下下降时间都短于上升时间。

## 碰撞补正

Movement 内 `_jumpCorrection`、`_groundDashCorrection`、`_airDashCorrection`、`_stepCorrection` 分别控制跳跃顶角、地面冲刺、空中冲刺和普通移动跨台阶。每组 `_enabled` 默认 false；启用后参数冻结到会话。不开新状态，不复制能力资源。

| 组内字段 | 单位 / 边界 | 作用 |
|---|---|---|
| `_maximumDistance` | u，>0 | 沿补正轴的最大距离 |
| `_increment` | u，>0；最大距离 / 步长 ≤64 | 从近到远搜索，最终一级覆盖最大距离 |
| `_lookAhead` | u，≥0 | 至少检查此距离，实际检查不小于本步前进距离 |
| `_skin` | u，>0 且小于最大距离 | 路径与目的空间安全间隙 |
| `_direction` | 右/上优先、左/下优先、仅正向、仅负向、输入优先 | 同距离候选顺序；只用于跳跃和空冲，地冲及跨台阶固定向上 |
| `_obstacleLayers` | 非空 LayerMask | 可触发补正的障碍；其他碰撞层仍参加安全检查 |
| `_supportDistance` | u，>0 | 地冲及跨台阶验证本步前进后的落脚支撑距离 |

跳跃补正覆盖普通跳、空中跳、墙跳和踩头反弹的上升阶段。单物理步最多补正一次，保留原速度、重力和动作资源。目的空间采用全部刚体实体碰撞体的保守包围盒，位移路径扫掠全部碰撞体；只对静止且未使用 Effector 的实体障碍触发，移动或单向平台不会触发补正。保守空间检查可能拒绝凹形/复杂几何中实际可走的间隙，不能承诺任意形状绕障。

## 可选动作

| 动作 | 数值位置/范围 | 必须同时核对 |
|---|---|---|
| 二段跳 | Jump `_airJumpSpeed` >0 u/s；`_airJumpHorizontalMode` PreserveVelocity/ FacingSpeed；后者 `_airJumpHorizontalSpeed` ≥0 u/s | Ability、图节点、air-jump 附加规则；独立于普通 Jump，不应用短跳。只在进入步处理水平策略，之后常规空中控制；不能配置地面支撑补充次数 |
| 墙滑/墙跳 | Jump `_wallSlideSpeed` ≥0；快速值≥普通值；`_wallJumpX/Y` >0，均u/s；`_wallProtection/_wallReturnDuration` ≥0 s；`_wallReturnMultiplier` (0,1] | 两个 Ability、墙几何与输入；快速开关 `_enableWallFastSlide` 关闭时继承普通速度；回墙开关 `_wallReturnEnabled`；wall-slide 附加规则 |
| 地冲/空冲 | Movement `_groundDash/_airDash`：`_duration` >0 s，`_speedMode` Constant/Linear；`_speed/_startSpeed/_endSpeed` ≥0 u/s | 对应 Skill 与 Dash 输入、图节点；各速度字段均需合法。当前是水平冲刺，不能承诺八方向；退出不按动画帧数决定 |
| 下穿 | Movement `_dropThroughSpeed` >0 u/s | Ability、实际单向平台及向下+新按跳跃；不是穿透所有碰撞 |
| 挂边/翻越 | Movement `_ledge`：`_layers`非空；`_reach/_verticalReach/_gripInset/_skin` >0 u；`_riseDuration/_inwardDuration` >0 s | 两个 Ability，翻越依赖挂边；静止实体墙顶及空间/路径验证，ledge-hang 附加规则；不承诺动态平台 |
| 普通/重落地 | Movement `_landing._normalEnabled/_hardEnabled`；相应 `_normalDuration/_hardDuration` >0 s、`_hardSpeed` >0 u/s | 图节点，真实接触前下降速度；普通可被更高优先动作抢占，重落地有不可取消停顿 |
| 翻滚 | Movement `_rolling._enabled`；`_speed` >0 u/s、`_duration` >0 s | 真实落地且水平输入的自动衔接；不是新增翻滚按键，不自动提供无敌 |
| 踩头反弹 | Jump `_headJumpSpeed` >0 u/s | Ability、真实目标几何与图 HeadJump；不当作普通短跳 |

## 能力、效果、表现

源码 `Assets/Game/Module/Entity/Powers/`：槽 `_enabled` 控制装配；Definition `_defaultOpen` 为新游戏初始开放；`_limitedUses` 与正整数 `_maximumUses` 控制次数；Skill `_cooldownSeconds` 为有限非负秒数。保留稳定 `_id`，不通过换 ID 规避进度兼容。Player 专用 Definition 还有补充策略，按实际类读取；不要为二段跳开放地面补充等非法组合。

效果参数先读 `PlayerActionEffects` 及引用的 EntityEffectDefinition，按实际动作槽、层/外观配置和持续策略展开。本 Skill 没有通用“开无敌”开关：效果容器、动作接线及规则许可必须完整校验。行为消费仍归已有 Enter/提交点，条件树仅查询。

Prefab Collider/Sensors 的尺寸、探测距离、物理层与视觉层级，Animation 的片段/Override，PlayerCameraParticipant/Cinemachine 的跟随、偏移、阻尼都不由 Feel 覆盖。用户确实要求这些变化时先读取其独立接线与影响，不能把“更飘”自动解释为改碰撞体或镜头。

## 从描述到候选

- 更利落：保持速度和跳高基线，先查看当前是否渐变。已渐变时可将起步/刹车/转向提高约20%–40%作为首轮范围，并估算响应时间缩短；可能损失惯性感。即时模式已经无加速等待，改未生效数值无用，应询问或检查输入窗口、落地停顿、动画/镜头是否才是症结。
- 更轻飘：先说明候选针对哪个阶段。例如保持基础高度，把到顶时间提高10%–20%，相应降低起跳速度与基础重力；代价是上升更慢、整段节奏改变。若只想顶点停留，考虑已有顶点重力而保留基础轨迹，必须实测非线性区间。下降缓慢可能来自倍率/限速，不能无条件同时改三者。
- 空中更可控：先查是否继承地面；需要独立调节时显式展示开启独立空中和对应速度/三类加速度的变化。提高空中刹车会减少松键漂移，提高反向加速度可能改变墙跳保护后衔接；有起跳直接响应时初段测量应单独解释。

百分比仅是首轮经验范围，不是强制配方。始终输出真实原值、作用开关、候选和副作用；相关基础值为零或当前机制关闭时重新推导，不机械乘比例。风格名只描述预期节奏，不代表原作参数来源。

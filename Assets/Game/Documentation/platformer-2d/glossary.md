# 2D 平台跳跃术语字典

归属：`platformer-2d` 及合法派生模板。先读 [Ember通用术语](../../../../Packages/com.ember/Documentation~/maintenance/glossary.md)。本页只补玩法含义，不改变框架术语或产品规则；源码/正式方案配置为准。

| 简化用语／别称 | 在本模板中的含义 | 边界与来源 |
|---|---|---|
| 2d平台跳跃、平台模板、平台跳跃模板 | 模板 ID `platformer-2d` | 不是 Windward 产品；活动身份以项目中心记录为准 |
| 玩家、玩家模块 | PlayerController 组合角色能力，PlayerModule 唯一驱动 Update/FixedUpdate | 不新增 PlayerManager，不由观察窗口重复 Tick |
| 实体、Entity2D | 可组合运动/检测/朝向/能力/效果的归属根 | 不是所有实体必须具备玩家FSM/刚体 |
| 完整方案、Scheme、玩家方案 | PlayerScheme 的图、Feel、Powers、输入、动画、动作效果与规则装配 | 不是仅一份手感数值；复制时核对共享图/美术引用 |
| 手感、Feel | Movement/Jump/Input 等运动与输入参数 | 资源次数、冷却、解锁归 Powers；调参不等于增加状态 |
| 玩家配置、PlayerConfig | 正式 Scheme 引用及 Scene/Spawn 环境配置 | 编辑窗口选中项不等于项目默认 |
| 当前编辑方案 | 玩家开发工具正在编辑的源配置 | 不自动应用到正在运行的会话 |
| 冻结A、运行方案 | Gameplay准备时捕获的本次配置闭包 | 编辑B后原位恢复仍用A；新Gameplay/开发重启才捕获B；美术对象引用不保证字节隔离 |
| 白盒、手感白盒 | 通过开发设置从正式 FrameworkScene 流程进入的可控验证场景 | 不直接打开白盒绕过准备；不是全路线或发行构建验收 |
| 开发重启 | 工具按正式生命周期重建开发会话以应用新配置 | 不等于只重置观察窗口或修改旧会话冻结数据 |
| 状态、节点、Definition | 玩家FSM行为和声明配置 | 图布局只影响编辑展示，不应因调参重建身份 |
| 能力、Power | 解锁、次数、冷却等可选业务能力 | 不与互斥动作状态混为一体 |
| 动作接管、Action、Lease | PlayerActionState及项目动作的有界控制所有权 | 不绕过唯一驱动；结束要释放本次效果/运动来源 |
| 输入清理、控制恢复 | 清除旧输入窗口、由动作/会话资格重新允许操作 | 不能简单绑定到遮幕退出；复生可比WhenInteractive更晚 |
| 地面支持、支撑机会 | 本步物理检测支持与跳跃窗口 | 初次支撑建立基线不等于新落地奖励 |
| 普通跳、二段跳、墙跳 | 已有不同来源的跳跃策略 | 不将墙跳/弹射曲线塞进普通跳，按来源及owner/代次隔离 |
| 顶角补正、跨台阶 | Movement中的可选碰撞补正 | 不是新状态或能力；默认关闭，受空间扫掠限制 |
| 外部运动、承载、弹射 | 独立来源/所有权的附加运动与机关交互 | 不直接覆盖玩家唯一运动结算；参见 ExternalMotion 和 launch-policy |
| 检查点、Checkpoint | 稳定玩家快照与明确世界复位基线 | 不保存任意FSM叶子/Animator时间；不能把内存检查点当完整磁盘存档 |
| 世界复位、WorldResetScope | 同场景参与者按原因恢复各自基线 | 不代表卸载重载关卡；产品决定哪些对象应恢复 |
| 原位交接 | RequestInPlace准备候选、同步提交、替换会话 | 保留Gameplay模块/场景/冻结A，旧回调不影响新会话 |
| 存档、保存进度 | PlayerData/业务存储协议持久化可恢复内容 | 与保存方案资产、模板保存、Bump不同；动作中可能按契约拒绝 |
| 玩家相机、PlayerCamera | 消费Player会话的独立相机适配模块 | Player不依赖Cinemachine具体类型 |
| 死亡/复生表现 | 模板能力上由项目动作决定的表现编排 | Windward的0.5+0.53333336、计时区、声音/冲量不是所有平台项目默认值 |

依据：[玩家架构](PlayerArchitecture.md)、[玩家工作流](PlayerWorkflow.md)、[检查点接入](CheckpointIntegration.md)、[项目动作](ProjectActions.md)、[外部运动](ExternalMotion.md)、[战斗](PlayerCombat.md)。

维护源在本目录；通过项目中心普通保存才能进入模板，Bump/发布/消费升级分别处理。新增其他玩法字典时放对应模板目录，不让无关模板读取本页作为其规则。

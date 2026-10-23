# 平台跳跃模板

归属 `platformer-2d` 及合法派生模板。统一入口是 **Ember → Player → 玩家开发工具**，窗口内保留“手感白盒／玩家配置”两个页签；旧 Tools/Ember/Player 菜单已撤下。非所属活动模板隐藏入口，Player 模块关闭时保留草稿并禁用操作。

## 使用与扩展

- [地面支撑事实与跳跃机会](ground-support-contract.md)：真实接触、法线相对速度、主动起跳消费锁、统一消费者及消费合并边界。

- [平台跳跃术语字典](glossary.md)：简化用语、方案与动作会话、恢复与控制的职责边界；配合框架通用字典阅读。

- [载具自身碰撞与本步承载速度](carrier-motion-contract.md)：可选自身切向碰撞约束、弹射到期与乘员净空的固定步顺序及消费迁移边界。

- [可选弹射尾段契约](launch-tail-contract.md)：保持结束后直接接管真实自身速度，独立上升／低速区／下降规则、慢速许可生命周期及快照边界；默认关闭。
- [弹射水平控制与墙跳资格](launch-policy.md)：已通过模板 Player 回归的可选竖直保持、水平输入及接受时资格刷新契约，随 0.9.0 交付。
- [墙跳与单次飞行策略](wall-flight-policy.md)：可选直上资格、反向转换、独立墙跳曲线及按来源/owner/代次隔离的下降。
- [普通跳独立竖直策略](ordinary-jump-policy.md)：恒速保持、松键、按住顶点、独立上升／下降和特殊动作隔离。

- [项目动作接管](ProjectActions.md)：真实 FSM 动作、会话/激活凭证、部分释放、零重力、弹射取消、配置冻结与最小消费程序集。
- [外部运动与塌陷平台](ExternalMotion.md)：实例来源、局部交互锁、真实物理验证及消费边界。
- [公共机关与检查点接入](CheckpointIntegration.md)：平移支撑、弹射、世界复位、冻结关卡入口、组合进度和区域归属。
- [两关正式链路验收](LevelAcceptance.md)：独立A/B场景、正式加载和UI、组合保存续玩及相机加载/恢复/卸载。
- [玩家工作流](PlayerWorkflow.md)：部署、选择/复制完整方案、调参、白盒、相机、换图和保存。
- [玩家架构与所有权](PlayerArchitecture.md)：唯一驱动、冻结 A、输入/能力/效果、暂停及完整快照恢复。
- [转换规则与状态扩展](PlayerRules.md)：E01 请求位置和提交点，E02 三个入口的有限附加条件。
- [手感参数与依赖](../TemplateSkills/ember-platformer-tune-feel/references/parameters.md)：按动作/机制定位真实字段与单位。
- [预设测量基线](../../../GameResource/Resources/Config/Player/Feel/README.md)：历史调校条件与实测结果，实际参数以方案资产为准。

从 FrameworkScene 正常 Play → Main → 新游戏/继续进入 Gameplay。白盒通过开发设置选择，复用正式玩家和驱动；每次 Play 使用独立开发库，不直接运行白盒场景绕过准备。当前编辑方案、下次白盒方案、项目默认和运行冻结方案是不同对象，编辑不追溯改变当前会话。

基础移动/跳跃/下落、地空水平控制、二段跳、墙面、水平冲刺、下穿、挂边翻越、落地/翻滚和踩头已有实现。基础移动图为21节点，数量不是扩展限制；图身份、配置来源和布局不得因调参被重建。攻击、命中、生命与恢复见[战斗工作流](PlayerCombat.md)。两套移动方案仍为21节点，独立“通用攻击模板1”为31节点。移动平台挂边、精确动作存档及任意无代码动作编辑不属于现有范围。

下落加速通过下落重力倍率配置，顶点区间优先。碰撞补正提供默认关闭的跳跃顶角、地冲、空冲和普通移动跨台阶配置；白盒提供真实位置尾迹与 X–Y 运动轨迹图，入口及限制见[玩家工作流](PlayerWorkflow.md)。

## 模板专属 Skill

- [手感调整](../TemplateSkills/ember-platformer-tune-feel/SKILL.md)
- [序列帧替换](../TemplateSkills/ember-platformer-replace-animation/SKILL.md)
- [新状态接入](../TemplateSkills/ember-platformer-add-state/SKILL.md)
- [攻击接入](../TemplateSkills/ember-platformer-add-attack/SKILL.md)

四个 Skill 的唯一维护源在 `Assets/Game/Documentation/TemplateSkills/`，schema v2清单归属本模板；通过正式模板预览/同步维护发现副本，不进入框架公共技能清单或个人全局目录。执行前检查正式模板身份、兼容条件和真实模块/API；只发现 Skill 不代表所需业务已部署。

## 维护边界

本文档及业务资源在项目 Assets 编辑，通过正式模板流程普通保存；Bump、提交、发布和消费升级分别处理。禁止手改 Templates~、ParentSnapshot~ 或 hash。复制方案仍可能共享图、Definition和美术，修改前核对实际引用者；保留内存草稿、Undo/Redo与当前手动布局。

旧阶段过程及测试数字已从操作首页移出：当前行为以上述正式说明和源码为准，开发仓库交付记录保留历史覆盖与未执行边界。测试夹具、源码依赖检查、编译通过分别不能代替真实窗口、人工手感、消费部署或发行构建验收。

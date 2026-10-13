# 平台跳跃模板

归属 `platformer-2d` 及合法派生模板。统一入口是 **Ember → Player → 玩家开发工具**，窗口内保留“手感白盒／玩家配置”两个页签；旧 Tools/Ember/Player 菜单已撤下。非所属活动模板隐藏入口，Player 模块关闭时保留草稿并禁用操作。

## 使用与扩展

- [玩家工作流](PlayerWorkflow.md)：部署、选择/复制完整方案、调参、白盒、相机、换图和保存。
- [玩家架构与所有权](PlayerArchitecture.md)：唯一驱动、冻结 A、输入/能力/效果、暂停及完整快照恢复。
- [转换规则与状态扩展](PlayerRules.md)：E01 请求位置和提交点，E02 三个入口的有限附加条件。
- [手感参数与依赖](../TemplateSkills/ember-platformer-tune-feel/references/parameters.md)：按动作/机制定位真实字段与单位。
- [预设测量基线](../../../GameResource/Resources/Config/Player/Feel/README.md)：历史调校条件与实测结果，实际参数以方案资产为准。

从 FrameworkScene 正常 Play → Main → 新游戏/继续进入 Gameplay。白盒通过开发设置选择，复用正式玩家和驱动；每次 Play 使用独立开发库，不直接运行白盒场景绕过准备。当前编辑方案、下次白盒方案、项目默认和运行冻结方案是不同对象，编辑不追溯改变当前会话。

基础移动/跳跃/下落、地空水平控制、二段跳、墙面、水平冲刺、下穿、挂边翻越、落地/翻滚和踩头已有实现。当前图21节点，数量不是扩展限制；图身份、配置来源和布局不得因调参被重建。完整战斗、移动平台挂边、精确动作存档及任意无代码动作编辑不属于现有范围。

## 模板专属 Skill

- [手感调整](../TemplateSkills/ember-platformer-tune-feel/SKILL.md)
- [序列帧替换](../TemplateSkills/ember-platformer-replace-animation/SKILL.md)
- [新状态接入](../TemplateSkills/ember-platformer-add-state/SKILL.md)

三个 Skill 的唯一维护源在 `Assets/Game/Documentation/TemplateSkills/`，schema v2清单归属本模板；通过正式模板预览/同步维护发现副本，不进入框架公共技能清单或个人全局目录。执行前检查正式模板身份、兼容条件和真实模块/API；只发现 Skill 不代表所需业务已部署。

## 维护边界

本文档及业务资源在项目 Assets 编辑，通过正式模板流程普通保存；Bump、提交、发布和消费升级分别处理。禁止手改 Templates~、ParentSnapshot~ 或 hash。复制方案仍可能共享图、Definition和美术，修改前核对实际引用者；保留内存草稿、Undo/Redo与当前手动布局。

旧阶段过程及测试数字已从操作首页移出：当前行为以上述正式说明和源码为准，开发仓库交付记录保留历史覆盖与未执行边界。测试夹具、源码依赖检查、编译通过分别不能代替真实窗口、人工手感、消费部署或发行构建验收。

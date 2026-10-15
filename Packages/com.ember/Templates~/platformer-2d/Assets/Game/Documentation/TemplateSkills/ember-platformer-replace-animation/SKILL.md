---
name: ember-platformer-replace-animation
description: 为 Ember 平台玩家导入序列帧或 spritesheet，按真实状态映射替换全部或部分动画，并验证朝向、脚底、循环、暂停及效果兼容。用于换角色图片和动画；不因换图改碰撞体、玩法状态或动作时长。
---

# 玩家序列帧替换

读取项目 AGENTS.md、CLAUDE.md 及适用规范，确认真实 Unity 项目。调用 `Ember.Core.Editor.EmberProjectSetup.GetTemplateSkillExecutionBlockReason("ember-platformer-replace-animation")`；返回非空时停止业务写入并说明修复入口。无 Unity 连接只做可证实的素材与接线分析，不宣称导入或验证成功。

核对 `EmberProjectSetup.IsTemplateActive("platformer-2d")`、PlayerModule 的实际启用状态及 `PlayerSchemeWorkflow`、`PlayerAnimationProfile/View/Session` 能力。合法派生模板按正式有效身份判断，不能凭残留资产或版本号猜测能力。通过 `Ember/Player/玩家开发工具` 确定目标完整方案，保留未保存草稿、窗口与图布局。

## 素材与映射

从方案真实图和 Animation.Slots 生成状态清单，区分父状态后备、叶子、启用与本次替换范围，不硬编码21节点。先展示“替换／保留／缺失／需用户选择”的映射，再创建资源；仅读取相关细节时打开 [素材与导入](references/artwork.md)。

核对帧顺序、帧尺寸/切片、帧率或时长、循环、默认朝向、脚底/轴心、PPU与显示尺寸。能从素材可靠取得的事实直接读取；仅对影响结果的缺失信息询问。部分替换明确保留其他映射；缺帧不能静默删状态、关闭能力或杜撰动作素材，可请用户选择保留占位或明确复用。

## 接入与验收

已有攻击的帧、Clip、刀光素材和表现映射由本技能替换；新增招式/连接交给 `ember-platformer-add-attack`。保持命中几何、窗口和逻辑时长，速度与冻结动作时钟同步；刀光独立于身体闪烁，不能以换图改变伤害。

按 [动画接入协议](references/integration.md) 使用 Unity 资产、导入与动画 API。内置方案先 `PlayerSchemeWorkflow.Copy` 完整独立复制，已有自定义按用户意图修改或另存并检查共享引用。保持基础图、稳定节点 ID、原动画键和未替换内容；不重跑一次性 `PlayerAnimationAssets.Install`。

沿用 Visual → Animation → Sprite。动画只写 Sprite 子树的 Sprite 引用或局部姿态，不写玩家根、碰撞/传感器、朝向外层或效果 alpha，不使用 AnimationEvent 驱动玩法。图片尺寸不自动改变物理尺寸；视觉比例有意调整时说明对象和依据。

复用完整方案校验、只读保护、Undo及显式保存；保存、选择下次白盒、重启、设默认分别按授权处理，不自动设默认。当前会话保持冻结A，源资源改动后使用新会话验证。

实际试跑左右朝向、脚底、循环/末帧、暂停、同叶重入、相关动作与效果合成、多实例及保存重载。新片段先有可靠的映射与曲线校验，再以运行画面验收；不能用缩略图或编译代替播放验证。交付替换/保留/复用/缺失表、资产路径、输入素材约定、已执行验证及限制。

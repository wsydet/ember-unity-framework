# Ember Framework 0.14.3 发布说明

2026-09-23。框架 0.14.3 携带正式封存的 visual-novel 0.7.2 / preview。

## 修复

Gameplay 主UI布局的 CanOpen → CanEdit 原来只读 Assets/Editor/EmberEditingTemplate.json，且只接受 visual-novel 精确 ID。消费项目只有正式活动部署记录，因而被错误禁用，派生模板也被拒绝。

在 Ember.Core.Editor 的 EmberProjectSetup.Identity.cs 提取 IsTemplateActive(templateId)：embedded 项目只读编辑记录，消费项目只读活动部署记录，沿模板父链识别派生模板；缺失身份、未知模板、断链、无法命中的循环和重复 ID 拒绝。不会伪造记录，也不会从另一环境的残留记录回退。

模板的 NovelGameplayLayoutWindow 与 NarrativeGraphModel 共用判断。Game.UI.Editor 新增 Ember.Core.Editor 引用，避免与已依赖 UI 的 Game.Narrative.Editor 循环引用。剧情图保留项目变化缓存失效与写入前刷新。布局保留 Play/切换 Play、NarrativeModule.Enabled、阅读页与阅读菜单 Prefab Mode 的限制。NarrativeEntryLauncher 未修改：非 Play、暂停、已有会话、未选中剧情/章节/节点均应禁用。

## 正式保存与验证

- 在已加载的 visual-novel 编辑副本修改，通过 Unity MCP SaveTemplate 保存 820 个文件，再 BumpTemplateVersion("visual-novel", 2) 封存 0.7.2；没有直接编辑快照或 hash。
- contentHash 与 versionedContentHash 均为 e81241729c7de881dae7a5b6a4c98fa1。base 0.6.4、source3d-2p5d 0.3.7 及 ParentSnapshot 不变。
- 模板 frameworkVersion 沿用父模板 0.14.1 的 major.minor 兼容声明；本次新调用依赖 0.14.3 API，迁移时必须先升级框架。
- Unity 6000.5.4f1 经 MCP 刷新编译、域重载完成，Console 无错误。
- 24/24 EditMode 回归通过：15 项框架身份用例，4 项剧情/布局身份与两个 Prefab Mode 冲突用例，3 项模块菜单可用性用例，2 项布局外观与草稿回归。覆盖开发、部署、两级派生、非小说、跨环境残留记录、缺失/异常继承。
- 模板保存期间排除既有字体缓存变化，备份后在 finally 中逐字节恢复，SHA256 一致。发布只提交修复相关路径。
- 未修改或升级 CMH；实际消费项目升级后的菜单、Play 与保存验收仍待执行。本次未执行完整项目测试集。

## CMH 升级步骤

框架升级不会自动更新 Assets 中已经部署的模板脚本，因此还需要应用模板修复。

1. 提交或备份 CMH，在 Ember/UPM Manager 选择 v0.14.3 并升级；不要手改 manifest/lock 或 PackageCache。
2. 对已有定制工程，优先做最小差异迁移：以新包 Templates~/visual-novel/Assets 为只读参考，在项目 Assets/Game/UI/Editor/Game.UI.Editor.asmdef 增加 Ember.Core.Editor 引用；将 Narrative/NovelGameplayLayoutWindow.cs 中仅检查编辑 JSON 的分支改为 Ember.Core.Editor.EmberProjectSetup.IsTemplateActive("visual-novel")，删除不再使用的 Identity 嵌套类。保留其余布局、样式和保护逻辑，不整文件覆盖定制。
3. 为保持共用实现，同时合入 Assets/Game/Module/Narrative/Editor/NarrativeGraphModel.cs 的身份判断差异。若保留模板测试，同步 Tests/NarrativeTemplateIdentityTests.cs 与 Tests/Game.Narrative.Tests.asmdef 的 Ember.Core.Editor 引用。用户文档变化可按需同步。上述路径均相对模板 Assets 或项目 Assets。
4. 人工移植修复不等于完整部署 0.7.2：保持原部署版本/hash 和模板技能安装记录，不手改身份记录，不创建 EmberEditingTemplate.json。
5. 如需正式整套部署 0.7.2，应先在备份/独立工作副本中比较所有受管目录及技能差异，再在 Ember/项目中心执行完整重新部署，最后逐项合回产品定制。完整部署替换 Game、Resources、Ember/Editor、Settings、GameResource；不能直接在未备份的 CMH 上覆盖。“补齐缺失”不更新已有脚本，不能解决此问题；包含模板技能的版本变化还受正式部署协议约束。
6. 编译后确认：非 Play 且模块启用时布局菜单可用；打开阅读页或阅读菜单 Prefab Mode 时禁用；退出后恢复。Play 模式布局不可编辑；从所选入口开始新游戏仍遵守原条件。验证自定义 Prefab、布局、剧情和资源未被覆盖。

派生模板应在开发端通过父级更新预览/同步吸收 0.7.2，保留派生修改并封存，再走消费端迁移/部署流程。不要只把活动 ID 改成 visual-novel。

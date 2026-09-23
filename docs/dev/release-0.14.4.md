# Ember Framework 0.14.4 发布说明

2026-09-23。本次归类为框架升级：修复模板技能更新被迫要求完整业务部署的耦合。visual-novel 保持正式封存的 0.7.2；base 0.6.4、source3d-2p5d 0.3.7 及其 hash、父快照均不变。没有直接修改模板快照、安装副本或 CMH。

## 行为

消费项目原先必须让当前包内模板与业务部署 version/hash 完全相同才能同步技能，包内技能一更新就被要求完整重新部署。现在通过项目中心“模板专属 AI Skill”显式预览和更新，业务部署版本保持原值，技能取自当前包中同一活动模板的已封存快照。

更新范围只有 Assets/Game/Documentation/TemplateSkills（含根 .meta）、明确归属当前模板的 .agents/skills/<id> 和 .agents/ember-ai-skills.json。业务剧情、表、图片、Prefab、场景、代码及 EmberDeployedTemplates.json 不参加技能事务。

schema v3 的 templateBaseline 分别记录业务部署与技能源的版本/hash，并记录完整技能源树的文件、目录和根 meta 指纹。旧 v1/v2 可读并迁移；旧记录没有完整源指纹时，差异必须先确认备份。JsonUtility 的空对象与空字符串按未设置值规范化。无技能的旧模板保持原有空操作语义。

最低模板版本按实际业务版本判断；不满足时阻断，不能通过只升级技能宣称业务已升级。发现副本和本地源修改需要明确备份；预览后任一绑定内容变化拒绝写入。备份放在 .utmp/ember-ai-skills，源、根 meta、发现副本和技能记录复用现有 CommitPreparedTargets 异常回滚。GUID 冲突检查只豁免本次替换的技能源范围，保护其他业务资产。完整部署不会重写技能正文中的示例版本字符串。

## 验证

- Unity 6000.5.4f1 / Unity MCP：154/154 项 EditMode 测试通过，0 失败、0 跳过，测试运行 12.45 秒。job：41af9d10886744c5952884b886635447。
- 覆盖 EmberTemplateSkills、TemplateDeployment、TemplateTransaction、TemplateSyncPlan、TemplateMetadata、AISkillInstaller、AISkillDownload。临时消费夹具验证业务和身份逐字节保留、重复更新、旧记录迁移、取消不写入、明确备份、最低业务版本、指纹失效、四个技能事务阶段故障回滚、GUID 冲突与完整部署重建基线。
- 首轮暴露 JsonUtility 空对象和无技能模板回归，已修复后重跑上述测试。最初使用 test_names 传 fixture 未启动，改为 group_names 后运行；不把该次初始化当成测试通过。
- MCP 刷新请求成功；最后一次 Console 查询 ping 未响应，按仓库规则停止追加编译查询。尽管测试执行成功，本次未完成最终 Unity 编译验证；请在 Unity 中手动触发编译。如果仍有报错，请将首条编译错误及完整堆栈发回任务。
- 静态发布检查通过：通用 skill bundle 9 项 / 24 文件；共享二进制及 autocrlf 两种检出；共享字体配置；版本、release/manifest 和模板声明一致。
- 未在 CMH 操作，也未执行消费项目窗口点击、真实图片导入/烘焙或全项目测试。图像确认页面仍是 0.14.2 正式交付实现，本次没有改它的模板正文。

## CMH 如何使用

1. 在 Ember/UPM Manager 升级至 v0.14.4，编译完成后，在 Ember/项目中心完成你计划的最后一次完整部署（visual-novel 0.7.2）。此次完整部署仍替换五个受管目录；按你说明，CMH 没有需要保留的本地改动，本任务没有检查它。
2. 以后仅更新 skill：先升级框架包，再打开“模板专属 AI Skill”→“预览当前模板技能更新（保留业务内容）”→“更新技能”。有技能本地修改时选择备份后更新或取消。重载 AI 会话后使用新技能。
3. 已有带 hash 的正式部署记录也可直接使用独立技能更新；0.14.4 本身不要求为了迁移技能协议再完整部署。
4. 框架/业务功能升级若需要改 Assets 脚本、配表结构或 Prefab，仍需单独评估业务迁移。此修复保证技能专用入口保留业务内容，不表示将来点击“完整重新部署”也会保护定制内容。通用消费端无损业务合并向导尚未实现。

模板技能作者继续在正式加载的 Assets 中编辑，经 SaveTemplate → Bump 封存后发布新框架 tag；此次仅改框架更新器，因而无需对未变模板再 Save/Bump。

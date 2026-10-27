# 保护本地修改与升级

## 操作前

在项目中心核对正式编辑/部署身份、当前模板版本、包版本及内容差异。备份现有文件而非只记 Git HEAD，包含未跟踪内容和 .meta；保留五个受管目录 `Assets/Game`、`Assets/Resources`、`Assets/Ember/Editor`、`Assets/Settings`、`Assets/GameResource`，以及有关的 Assets/Editor、ProjectSettings 和未保存场景。记录删除状态，不批量恢复切换模板产生的删除。

消费工程保留部署记录及 `ProjectSettings/EmberTemplateBaseline~/`，用于旧模板、新模板、本地内容的三方比较。美术替换也先保护原 Prefab、配置、生成文件和素材；一次只应用明确的新请求。

## 框架维护工程

确认编辑身份为 ui-game，在项目中心预览差异、执行资源/谱系校验，再普通保存。保存更新内容，不自动升级版本或完成封存；Bump 只在明确发布范围下执行。父 base 更新先预览父级同步，逐项合并本模板差异，不能自动覆盖其他派生模板。禁止直接修改 Templates~、ParentSnapshot~、身份、版本或 hash。

## 消费工程

1. 核对真实交付包包含 ui-game、所需工具和匹配技能；没有实际发布来源时不填写假 tag。
2. 使用 **Ember/UPM Manager** 安装/升级包，不直接编辑 manifest/lock 或 PackageCache。包升级不会自动更新已经部署的 Assets。
3. 项目中心选择当前 ui-game，预览可用更新。补丁增量按中心的旧基线/新模板/本地差异选择；冲突逐项保留产品内容。跨 minor 或需要全量替换时，先完成可恢复备份，再按中心明确的迁移/替换流程处理，之后三方合并产品代码、配置与资源。
4. 不把产品代码写回安装包。Framework 页使用用户扩展点；Business 页可通过公开中心生成。恢复公共库引用和本地字体映射后检查所有绑定。
5. 主动请求 Unity 编译并确认本轮编译和域重载完成；从 FrameworkScene 验证一局、暂停、重开、返回、动态行释放和目标设备适配，再记录新的部署基线。

首次新工程直接部署 ui-game，不必先部署 base。已有工程不得把“补缺”误当无损完整覆盖。需要详细冲突恢复时使用正式安装的 ember-template-conflict-recovery 技能及实际包规范。

## 发布准备边界

本地保存、模板 Bump、框架提交/tag/发布、消费部署是独立步骤。公共技能源维护于框架仓库 `.agents/skills`；AISkills~ bundle 由正式工具从真实已提交 sourceCommit 生成，不手写或伪造。发布前核对模板内容已封存、技能与包兼容、依赖声明和实际消费再生成能力。

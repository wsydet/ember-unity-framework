# 项目自有 AI Skill 来源

版本：**0.25.1**。本协议随该版源码交付，发行引用为 `v0.25.1`；既有 `v0.25.0` 不包含本协议。推送状态以远端 tag 为准。

项目业务工作流由消费项目维护，不进入框架 bundle 或模板快照。框架仅提供来源、所有权、兼容门禁及发现副本事务；不解释关卡连接、端口、能力、解锁、路线或主题。模板技能源仍遵守[模板 Skill 协议](../maintenance/template-ai-skills.md)。

## 维护源与清单

唯一项目维护源为 `ProjectSettings/EmberProjectSkills/`，在正式模板加载/部署的 Assets 替换范围外。项目版本控制应收录此目录；备份和发现副本不能代替维护源。

```text
ProjectSettings/EmberProjectSkills/
  catalog.json
  project-create-level/
    SKILL.md
    references/...
```

```json
{
  "schemaVersion": 3,
  "skills": [
    {
      "id": "project-create-level",
      "projectId": "my-project",
      "version": "1.0.0",
      "templateId": "my-template",
      "minimumTemplateVersion": "1.0.0",
      "maximumTemplateVersion": "1.9.9",
      "minimumFrameworkVersion": "0.25.1",
      "requiredExtensions": ["ember.level.authoring"],
      "requiredModuleScope": "my-project.level-authoring"
    }
  ]
}
```

模板和 Skill 版本仅示意；本协议最低框架版本为 0.25.1，其他新 API 按其实际最低版本声明。`version` 是该项目 Skill 的声明版本，与包/模板版本独立；即使没有升版，源文件字节变化也会使旧副本失效。`projectId` 为稳定来源所有者，不从目录前缀推断。改变所有者须先处理旧来源，不能接管同名安装。

`templateId` **精确匹配**正式活动编辑/部署模板 ID；模板兼容范围含上下限，按实际业务版本判断。派生模板使用自己的正式 ID 和版本声明，不能冒充父模板。完整绑定还包括活动模板 contentHash 与 editing/deployed 模式；独立更新模板技能源不把项目 Skill 绑定到更高业务版本。

扩展依赖绑定当前有效契约版本/hash，模块范围复用 `EmberExtensionDocuments.RegisterModuleScope(id, ownerTemplateId, enabled)`。项目 Editor 接线负责在域重载后重新登记模块。制作 Skill 可依赖 `ember.level.authoring`；包含试玩操作的 Skill 还应声明 `ember.level.trial`，无需依赖房间图或蓝图。业务 Module 与框架基础设施不互相代替。

ID 与模板、通用和未管理技能共用同一个发现命名空间。同名冲突报错，即使字节一致也不接管。正文继续使用 UTF-8 无 BOM 的合法 `SKILL.md` 文件头；目录、大小、符号链接及路径限制复用原安装器。

## 预览、同步与退出发现

项目中心现有模板 Skill 窗口同时显示“项目自有”条目。`PreviewCurrentTemplateSkills()` / `SyncCurrentTemplateSkills(preview, allowBackup)` 统一预览、复核与提交；模板生命周期及扩展配置也沿用同一个 `EmberTemplateTransaction`，没有第二套事务。项目清单、源码和目录结构加入预览指纹，提交前变化即拒绝。

同步只写受管发现副本及安装记录，不更新项目维护源。模板的独立更新仍可更新其专用 Assets 源，不会覆盖 ProjectSettings 项目源。只读包内模板浏览不能授予执行身份。

安装记录使用 schema v4（此前未使用项目技能时仍可写 v3；迁入 v4 后不自动降级），新增 `ownerKind: project`、`projectId`、声明 `revision`、`sourceCatalogHash`，并复用逐文件 SHA256、目录、模板绑定、扩展绑定和模块条件。catalog 的任意字节变化保守地使该项目来源下已安装条目失效。旧 v1/v2/v3 可读；不支持 v4 的旧管理器应拒绝写入，不能降级覆盖记录。

关闭扩展或模块、身份/版本/hash 不匹配、源删除/变更，都会收回项目受管发现副本；修改副本先完整备份至 `.utmp/ember-ai-skills/<transaction>/backup/`。自动生命周期只收回失效副本，不自动安装或采用新正文。有效新内容须显式预览/同步。域重载后重新检查；清单损坏或正式身份缺失时保守收回项目受管副本，源文件与制作草稿保留。

若文件系统或安装记录损坏导致无法安全确认所有权/备份，操作报错并保留原内容，不能声称已完成退出发现。修复后重新同步。已加载的 AI 会话不能即时撤回，因此必须执行下述 guard。

## 执行前复核与兼容退出

每份项目 `SKILL.md` 应要求先调用：

```csharp
Ember.Core.Editor.EmberProjectSetup.GetTemplateSkillExecutionBlockReason("project-create-level");
```

虽然保留历史方法名，此入口同时核验项目所有权、声明版本、完整源/副本指纹、正式模板模式和业务基线、框架/API、扩展版本/hash 及模块条件。返回非空或校验抛错均停止业务写入；没有 Unity 连接时不执行未经验证的写操作。它是开发防误用约定，不是任意代码安全沙箱。

UPM 安装器原有签名保留；低层新增重载接受当前框架版本/API 能力。项目调用方应使用 Core 入口，由其提供真实身份，不能手填版本伪造启用结果。UPM 不依赖 Core、Player 或产品类型。

撤回功能前先停止业务执行，保留项目源、发现副本修改及安装记录备份；通过匹配版本的项目中心撤出项目受管条目，再降级框架。不要把项目正文移动到公共 bundle，也不要直接删其他所有者目录。提交、发行、消费升级均另行授权。

本轮实际验证及未覆盖范围见[职责边界实施记录](level-authoring-boundary-plan.md#9-框架侧实施记录2026-10-30)。

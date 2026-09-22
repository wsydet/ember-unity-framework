# AI Skill 独立安装与更新

**本轮未发布扩展**：技能分为随框架附带的框架级 Skill、模板自有 Skill 和模板继承的 Skill。框架级继续使用本文独立更新器，首次安装还会从包内生成 bundle 自动补入；已有副本不随包升级静默覆盖。模板技能由项目中心按正式模板身份同步，继承技能随子模板快照封存。完整约定、兼容边界、备份与验证状态见 [框架与模板 AI Skill](template-ai-skills.md)。下文 0.13.0/0.13.1 描述的是已发布的原通用更新系统，不表示新的模板能力已经发布。

此功能从框架 **0.13.0** 开始提供。消费项目先通过 UPM Manager 升级并完成编译，再使用技能更新入口；当前固定标签 `v0.13.1` 提供 9 个技能。已有 0.13.0 更新器也可选择此标签检查技能，无需为纯技能更新强制升级框架。

## 使用入口

打开 `Ember/UPM Manager`，找到 **AI Skill · 独立安装与更新**：

1. 填写框架仓库的分支或标签，默认 `main`。需要可重复的团队基线时使用固定标签。
2. 点击 **检查 AI Skill 更新**。窗口显示下载进度，支持取消，120 秒超时；使用本机 Git 和已有仓库凭据。
3. 按技能查看“未安装”“有可更新内容”“内容与所选版本一致”“存在本地修改”或“已有项目副本”。
4. 点击安装或更新。本地修改和未管理副本需要明确选择 **备份并覆盖**。不兼容的技能会说明缺少的框架版本或 API，阻止安装。
5. 重新加载 AI 会话，使其重新发现项目技能。安装技能本身不会执行技能中的脚本或生成项目资源。

通用技能安装只修改当前项目的 `.agents/skills/<id>/` 和 `.agents/ember-ai-skills.json`，不修改个人技能目录、其他项目技能、`Assets`、UPM manifest/lock 或模板。模板部署/加载另由项目中心协调业务目录及模板技能的同一事务。


## 0.13.1 技能目录

| 技能 | 用途 |
|---|---|
| `ember-eui-build` | Ember EUI 制作 |
| `ember-commit-review` | Ember 提交审查 |
| `ember-region-organizer` | Ember 代码分块 |
| `ember-solution-design` | Ember 方案评估 |
| `ember-package-scan` | Ember 包清单同步 |
| `ember-generate-doc` | Ember API 文档 |
| `ember-odin-inspector` | Ember Odin 面板检查 |
| `ember-doc-maintenance` | Ember 文档维护 |
| `ember-odin-capture-style` | Ember Odin 风格记录 |

每个技能可单独安装，无须安装其他技能。Odin 两项在执行时检查项目 Odin 条件；当前面板不做 Odin 专用安装阻断。EUI 仍检查公开生成 API。文档维护需要 Python 3 及 rg 或 Git；下载和安装不会自动执行审计脚本。

API 文档模板和 Odin 检查依据随各自技能分发。项目自己的文档、提交规范与风格记录优先；缺少开发仓库 docs/dev 不阻止使用。文档审计默认 consumer 模式保护所有依赖包，framework 模式仅用于明确的框架源码维护。

## 来源、版本与备份

- **框架级唯一维护源**：框架仓库 `.agents/skills/`。消费者持有安装副本，不应分别维护一套同名通用技能。包内 `AISkills~/` 是脚本生成的发布副本，不是第二维护源。
- **发布目录**：`.agents/skills/catalog.json`。只有列入目录的技能可通过面板安装。当前发布 9 个消费项目可用技能，见下表；插件迁移与空白模板不在目录内。
- **独立更新**：Git 浅克隆、partial clone 和非 cone 稀疏检出只展开 `.agents/skills/`，不检出框架源码、模板和游戏资产。支持过滤的服务器只传输所需 blobs；若服务器忽略过滤，Git 可能额外传输对象，但安装范围不变。
- **固定来源**：检查时解析分支/标签的提交 SHA，安装使用同一缓存快照，期间远程分支推进不改变待安装内容。
- **安装记录**：`.agents/ember-ai-skills.json` 保存仓库、请求的分支/标签、实际 SHA、UTC 安装时间及各文件 SHA256。应与项目技能一起提交。
- **备份**：替换前把完整旧目录移动到 `.utmp/ember-ai-skills/<时间与随机ID>/backup/<id>/`。旧版本中已删除的文件不会残留到新版，个人添加的文件则保留在备份中。
- **失败处理**：下载失败不触碰安装目录。安装前后复查内容；写入记录失败时恢复原目录。失败的暂存内容保留在同一事务目录供检查，不自动重试覆盖。
- **恢复旧版**：关闭更新操作后，从显示的备份目录恢复整个技能，再重新检查；它会作为本地变化显示，不会被静默覆盖。备份位于被项目忽略的 `.utmp`，需要长期归档时另行保存。

取消或关闭窗口会结束当前 Git 请求，清理独占下载缓存；已安装技能不受影响。框架开发仓库中已有维护目录时，面板只检查，不允许覆盖技能源文件。

## EUI 技能与公开生成接口

新技能要求 `eui-regenerate-v1`：公开的
`EUIBindingCodeGenUtility.TryRegenerateCode(EUIBinding binding, out string error)`。
面板在保持零框架/Odin 程序集引用的条件下只读检查该 API；版本号符合最低要求不代表 API 已存在。

API 接收已保存 Prefab 资源上的 EUIBinding，调用原统一生成流程，保留用户代码、消费端 Framework 模式限制和包内资源只读保护。它不弹确认框、不创建 Prefab、不主动 Refresh。调用方在批处理结束后统一刷新；成功返回不代表编译成功。

技能里的 `assets/EmberEuiSkillAdapter.cs` 是可按需使用的模板，不是自动安装到 Assets 的代码。UnityFarm 既有 `Assets/Game/Module/FarmM1/Editor/EmberEuiSkillAdapter.cs` 及其调用方保持原样，仍使用保留的旧内部生成入口。更新技能不会自动删掉或替换这些业务工具；以后迁移适配器时需同时检查旧 `CheckGenerator` 调用并避免同名类重复。

技能更新不等于框架升级。缺少新 API 时先通过 UPM Manager 升级框架并完成编译，再安装新技能；禁止手改消费端 manifest/lock 或 PackageCache 来绕过。

## 维护与验证

维护源只提交 `.agents/skills`；本地 `.claude/skills` 兼容 junction 不重复跟踪。维护一个技能时更新源目录及相关接口说明；新增可供消费端使用的技能时在 catalog 中声明 id、显示名、描述、最低框架版本及需要的能力。catalog schemaVersion 当前为 1，未知能力会被拒绝。

回归覆盖目录范围、固定 tag 下载、取消/超时/重试、首次安装、清理旧文件、本地修改与未管理副本、预览后变化、替换失败恢复和源码保护。Unity EditMode 测试与实际 IMGUI/消费端验收需在 Unity 中完成，不能用 Git 或 JSON 静态检查替代。

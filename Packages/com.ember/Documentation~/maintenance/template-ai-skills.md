# 框架与模板 AI Skill

0.14.4 将消费端技能更新与业务模板部署分开。模板技能源仍由作者正式保存和封存；消费者可独立更新兼容技能，无需重新部署业务目录。验证范围见 [0.14.4 发布说明](../../../../docs/dev/release-0.14.4.md)。

## 分类与职责

| 类别 | 唯一维护源 | 分发与启用 |
|---|---|---|
| 框架级（原通用 Skill） | 框架仓库 `.agents/skills/`，schema v1 catalog | 构建为包内 `AISkills~/`；安装框架并完成编译后自动补入缺失的首次安装。UPM Manager 继续提供独立 Git 版本检查与更新 |
| 模板自有 Skill | 正式加载后的项目 `Assets/Game/Documentation/TemplateSkills/` | 经 SaveTemplate → Bump 封存到模板；对应模板部署/加载时安装发现副本；消费端之后可显式独立更新 |
| 继承的 Skill | 派生模板内物化的父模板基线，作者仍在 Assets 编辑 | 属于模板 Skill，随派生模板部署或独立技能更新；作者通过已有 O/N/C 父子同步更新，不在消费端读取最新父模板 |

例如 base 有两个 Skill，child-a 与 child-b 均继承这两个；两个子模板还能各有自己的 Skill。部署 child-b 不需要先部署 base。切换 child-a → child-b 后，两份继承技能的有效所有者改为 child-b，child-a 专属技能移出发现目录。

UPM Manager 保持无 Core/Odin 程序集依赖。Core.Editor 引用 UPMManager.Editor 暴露的安装器预览/暂存接口，统一在既有 `EmberTemplateTransaction` 中提交业务目录、身份记录、技能目录及技能安装记录。没有另一个安装器、全局 Manager 或运行时模块。UPM 面板通过可选反射入口打开项目中心模板技能窗口。

这是分发与发现隔离，不是保密或安全权限。包内模板源文件可以被手工读取。AI 客户端可能需要重新加载会话才重新发现技能；已经加载到会话里的旧内容不能即时撤回。

## 框架随包分发

框架维护者修改 `.agents/skills` 并提交技能源后，运行：

```powershell
./scripts/build-ai-skill-bundle.ps1
./scripts/build-ai-skill-bundle.ps1 -Check
```

脚本仅打包 catalog 列出的通用技能。`Packages/com.ember/AISkills~/bundle.json` 保存源提交 SHA 与每个文件 SHA256；`skills/` 为生成副本，禁止在此独立维护正文。源提交可早于包含生成产物的发布提交。检查模式同时检查源、输出、额外文件及指纹；发布前需纳入检查。

包内 `.gitattributes` 对 `AISkills~/skills/**` 禁用换行转换与 filter，保证 Git/UPM 检出不会把已记录的字节指纹改变。模板快照沿用既有 `Templates~/**` 字节保护。

编辑器启动/包安装后的 delayCall 使用原 `EmberAISkillInstaller.Install` 安装包内基线，不依赖网络。只有“无同名目录且无安装记录”的技能自动安装。已有技能、本地修改、未管理同名内容、曾删除但仍有记录的技能均保留，由 UPM Manager 显式检查和更新。新框架包增加的新框架技能可以首次补入；包升级不会自动覆盖已有技能。框架维护仓库的 `.agents/skills/catalog.json` 源目录受保护，不执行自动覆盖。

自动安装失败或兼容条件不足时在 UPM Manager 展示原因。每个通用技能沿用原单技能事务；一组通用技能不是一个整体事务，已成功安装的条目保留。临时错误可在下一次编辑器启动重试，或通过独立更新入口手动安装。个人全局技能目录不参与本机制。

## 模板技能目录与清单

```text
Assets/Game/Documentation/TemplateSkills/
  catalog.json
  ember-example-workflow/
    SKILL.md
    references/...
```

该源目录不是 `.agents/skills`，仅靠源文件存在不启用技能。目录和文件的 Unity `.meta` 必须正常生成并随模板保存；模板 hash、封存 hash 和 ParentSnapshot 自然覆盖它们。可执行 C# 示例应保存为 `.cs.txt` 等模板文件，避免源目录中的示例被 Unity 当成项目代码编译。

最小清单示例（示意版本不代表已发布能力）：

```json
{
  "schemaVersion": 2,
  "skills": [
    {
      "id": "ember-example-workflow",
      "displayName": "示例工作流",
      "description": "示例用途",
      "templateId": "example",
      "minimumFrameworkVersion": "0.13.2",
      "minimumTemplateVersion": "0.1.0"
    }
  ]
}
```

`templateId` 标记正文的来源模板；有效启用归属是安装记录中的当前完整模板，而不是这个标签。继承文件保留来源标签，不必重命名为每个子模板。物化到子模板即纳入该子模板版本和 hash，不动态查询祖先目录。标签不是访问控制凭证。

`minimumTemplateVersion` 在消费端比较**实际业务部署版本**，包括派生模板自己的业务版本；不能用新版包的技能源版本冒充业务已升级。开发端比较当前编辑模板版本。不比较来源标签对应模板的最新版。父子版本是独立序列：派生作者必须审查继承约束；较低版本的新派生模板可在编辑源清单中声明其已验证的适用下限，然后保存/Bump。不要以父版本较高为理由直接绕过检查。实际发布时 `minimumFrameworkVersion` 必须填写包含所需 API 和模板技能协议的正式框架版本，独立升级协议由 0.14.4 提供。

技能 ID 在一个最终清单内全局唯一，区分来源不能绕过 ID 冲突；大小写冲突、路径越界、设备名、符号链接/junction、重复 ID、缺少 SKILL.md 或 frontmatter 名称不一致均拒绝。同 ID 不能并存两份继承/子模板正文：有意定制时修改继承条目，用三方冲突处理记录决策；另一个独立技能需新 ID。

未声明技能的旧模板可以完全没有源目录。已有源目录却没有有效 catalog 属于错误。schema v2 允许 `skills: []` 表示显式空集合。未知 catalog/安装记录版本、未知能力和未知所有权模式拒绝；普通未知说明字段由 JsonUtility 忽略，不能用它们承载强制条件，新增约束必须升级 schema。通用下载目录只接受 schema v1，并拒绝模板归属字段；模板 schema v2 不会通过通用 Git 下载器进入项目。

## 作者工作流

1. 在项目中心正式加载模板，以 `Assets/Editor/EmberEditingTemplate.json` 建立编辑身份。只建同名目录不能代替正式记录。
2. 在上述 Assets 源目录新增或编辑清单及正文。在“模板专属 AI Skill”窗口预览当前编辑模板并显式同步发现副本，测试草稿。
3. 修改发现副本不等于修改维护源。先将需要保留的更改手工合回 Assets 源文件，再保存模板；冲突内容保留在备份，不反向自动合并。
4. 正式 SaveTemplate 会校验清单并保存源文件，只更新 `contentHash`；显式 Bump 才更新 `versionedContentHash`。保存/Bump 不自动改发现副本，完成后重新预览同步。
5. 父模板修改后，通过已有父子三方计划同步。清单的并发变化按整文件冲突解决，必须保留所需父/子条目；技能正文与 `.meta` 继续作为现有模板单元处理，不另建文本合并器。
6. 父同步本身仅修改模板存储。开发面板原有的“同步当前编辑模板后重载”会走正式 LoadTemplate，因此会显示技能差异并同步；取消/失败时存储同步已经提交，旧编辑副本可能过期，应按提示重新加载。两步不是一个跨存储与项目的大事务。

“另存为新模板”会改变正式编辑身份，因此发现副本与新身份一起提交；删除仍持有项目技能所有权的模板会阻断，应先正式加载另一个模板。不得直接改 `Templates~`、`ParentSnapshot~` 或 metadata hash。visual-novel 已包含四项正式 Skill；本次框架升级不改模板内容，visual-novel 保持 0.7.2。

## 消费者工作流与独立技能基线

1. 通过 `Ember/UPM Manager` 升级到 v0.14.4 或更新版本，等待编译完成。禁止直接编辑消费项目 manifest、lock 或 PackageCache。
2. 打开项目中心的“模板专属 AI Skill”，选择“预览当前模板技能更新（保留业务内容）”。预览分别显示实际业务版本与包内技能源版本、文件变化和兼容阻断。
3. 检查后点击“更新技能”；发现本地技能改动时，明确选择备份后更新或取消。升级包、打开窗口和预览本身均不覆盖文件。
4. 完成后重新加载 AI 会话，使客户端发现新技能。

独立更新仅写入 `Assets/Game/Documentation/TemplateSkills` 及其根 `.meta`、明确归属当前模板的 `.agents/skills/<id>` 和 `.agents/ember-ai-skills.json`。剧情、配表、图片、Prefab、场景、业务代码和 `EmberDeployedTemplates.json` 保持不变。只读取同一个活动模板在当前包中已封存的快照；不从 main 或最新父模板拼接。

独立更新不会宣称业务已经升级到新模板版本。技能的最低业务版本、最低框架版本或 API 能力不满足时阻断；先有针对性地迁移依赖的业务功能，再选择正式部署策略。不存在通用的消费业务无损合并保证。

首次部署、完整重新部署、切换仍按原模板生命周期处理五个目录与身份记录；**完整重新部署仍会覆盖业务内容**。补缺只增加缺失文件，不能作为业务升级或独立技能更新入口。未部署模板只能看只读预览，不能单独启用其技能。框架 embedded 环境继续以正式编辑身份同步开发发现副本。

安装记录升级到 schema v3，保留通用技能和每项模板技能的所有权、来源、模式及文件指纹。`templateBaseline` 分别记录正式业务 `businessVersion/businessContentHash`、技能源 `sourceVersion/sourceContentHash` 和完整专用源目录（含 catalog、目录和 `.meta`）的字节指纹。完整部署重建业务/技能源基线；独立更新只推进技能源基线，执行前仍核对业务身份。

旧 schema v1/v2 可读，写入模板技能时迁移。v2 尚无整个源目录指纹：源目录与新包有差异时保守要求完整备份，不能推定它没有本地修改。旧业务记录缺少 hash、来源所有权不匹配或未知 schema 时阻断，不伪造基线。通用 v1 技能不会因 ID 前缀被接管。旧更新器不认识 v3 时拒绝写入，不应降级使用。

## 冲突、备份与恢复

只管理正式当前身份对应、且有明确模板所有权的技能。其他模板所有权与当前正式记录不一致时阻断并要求恢复记录，不删除其他模板条目。同名通用技能、手工目录或通用维护源 ID 一律阻断，不提供模板流程中的强制接管。先备份并移走冲突目录或给新技能换 ID。

旧模板受管技能有本地修改（包括新增/删除文件）时，可取消切换，或明确选择“备份并同步”。旧副本完整保存到 `.utmp/ember-ai-skills/<id>/backup/<skill-id>`，安装记录备份在同级；确认后旧技能从自动发现目录移出，不能保留为发现目录内的 `.bak` 技能。通用、手工、个人全局技能均不删除。

独立更新还备份完整源目录到 backup/.template-source，根 meta 到 backup/.template-source.meta。

预览和暂存后复核完整来源文件及根 meta、catalog、本地源目录、发现副本、安装记录及正式身份记录；任意变化要求重新预览。完整部署将模板目录、身份记录和技能加入同一 `CommitPreparedTargets`；独立更新只将专用源目录、根 meta、发现副本和技能记录加入该事务，异常时逆序恢复。补缺也先暂存，再提交。Unity 场景注册/场景映射更新是提交后的现有编辑器操作，不属于文件事务；若它们失败，已提交的模板与技能身份仍应保持一致，修复后重试场景设置。

恢复时先停止模板操作，保留 `.utmp` 与错误信息。普通同步备份用于取回本地修改；若要回到旧模板，应使用匹配的框架包与正式部署/加载流程，再将需要的个人修改合回。不要只恢复旧技能目录而保留新模板身份。若自动回滚也失败，按报错保留所有 `*.ember-backup~`，一起检查五个业务目录、正式身份记录及技能记录后恢复。现有事务保证可捕获异常的回滚，不承诺断电、进程强杀或磁盘损坏后的自动恢复。备份位于忽略目录，需要长期保存时另行归档。

## 技能执行前置检查

每个模板 SKILL.md 都应要求：执行前确认这是实际 Unity 项目；调用
`Ember.Core.Editor.EmberProjectSetup.GetTemplateSkillExecutionBlockReason("<skill-id>")`。
返回非空原因时停止业务写操作，请用户通过项目中心修复。没有 Unity API 连接时应只读检查正式身份及安装记录；schema v3 按 templateBaseline 核对业务身份和技能源身份，不能直接要求两者版本相等，并避免继续不确定的业务写入；不可按 Assets 目录名称猜测身份。

框架级技能不要求某个模板身份；继承技能验证的是当前派生模板的有效归属，而不是强行要求项目当前是 base。此约定用于避免会话残留导致误操作，不是对任意手工代码的安全沙箱。

## 验证与发布

相关隔离测试位于 `EmberTemplateSkillsEditTests` 与 `EmberAISkillInstallerEditTests`。覆盖部署/切换/编辑身份、继承、保存/Bump、独立更新保留业务、旧状态迁移、兼容下限、明确备份、预览变化和事务各阶段回滚。每个版本实际执行结果与尚未验收的边界记录在对应发布说明，不沿用历史通过数。

模板正文发生变化时仍须作者正式 Load → 编辑 Assets → Save → Bump；仅框架安装器变化无需伪造模板 Bump。发布框架时检查通用技能 bundle，更新 package、CHANGELOG、release/manifest，提交并发布新 tag。消费项目之后通过技能专用入口更新，不能为了 skill 升级要求整模板覆盖。

# 正式交接 r7：本地交付完成，消费端尚未验证

2026-11-12（Asia/Shanghai）用户明确接受文件 symlink 场景“环境受限、待验证，不记通过”，不再阻塞本地收束；授权直接正式归档、安全清理本任务临时资料，收束后发布新版本。AC06 的该场景仍待验证，不降格为通过；本轮性质仍为实施自检。

完成日期：2026-11-12。正式归档入口为 [README.md](README.md)，正式消费端提示词为 [consumer-validation-prompt.md](consumer-validation-prompt.md)。源码与上一轮测试所验证的16文件指纹一致。原文中的“未完成/阻塞收束”及“未授权发布”是2026-11-11历史状态，已由本节用户新决定取代；测试结果和补测要求不变。下方完整保留实施事实与历史修订，逐项AC中的文件symlink仍为待验证（用户接受的非阻断限制）。

---

# 当前有效交接 r6：已实施并定向验证，AC06 文件 symlink 待验证

更新：2026-11-11，Asia/Shanghai。任务 `20261111-patch-preview-long-paths`；本节优先于下方 r1–r5 历史。

- 当前状态：**未完成，保留任务目录、原草稿备份和本轮证据；未正式归档、未清理**。不是未实施，也不能称全部本地 AC 通过。
- 有效需求 requirements.md r1、方案 design.md r2 不变。详细实施、逐项 R01–R08 / AC01–AC08、偏差、失败修复及恢复步骤见 [implementation.md](implementation.md)。同对话结果仅为实施自检。
- 用户方式保持方案/实施两阶段；必要本地事项通过后由本实施对话直接正式归档到 `docs/tasks/20261111-patch-preview-long-paths/`，无须独立验收、消费端回传或回入口。没有修改通用流程/skill；没有重新请求实施审批。
- 已完成实际修复：共享长路径门面迁入 UPMManager（GUID保留），补 Core 各 partial/UPM/defaults IO 缺口；严格补丁版本头与原 best-effort 分离；owned lease/精确安全清理、主次异常与已提交结果语义；事务重入/未知备份保护、全部回滚错误及恢复路径；新增定向回归。
- 基线仍为分支 `codex/local-snapshot-20261111-162819`、HEAD `16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2`；UPM 实测 Embedded 0.25.2，解析至本仓库 Packages/com.ember；正式 topdown-2d 0.1.2 未变。MCP 已重新枚举并核对 projectRoot，只操作框架实例，没有沿用旧身份假定或操作 Windward。
- 历史未决编译查询后发现 CS1501 缺 Encoding 重载，已修。之后 4 轮主动 MCP 编译均有完成/域重载及新程序集加载证据；最终 Console error/warning=0。[最终编译状态](evidence/compile4state.json)、[MVID](evidence/compile4loaded.json)。最后 C# 修改后已再次主动编译。
- 测试：首轮 72/41 passed/29 failed/2 skipped（46.6209秒）用于揭示并修复 lease 共享等问题；复跑 74/73 passed/0 failed/1 skipped（57.0378秒）；最后修正真正短树夹具后只补跑受影响 3 例，3/3 passed（3.5140秒）。完整具名过滤和各例 output 见 evidence；不相加宣称77个独立case，不称全量通过。
- 实测 WindowsEditor 6000.5.4f1，CLR4.0.30319.42000，Windows NT10.0.26200.0，NTFS，LongPathsEnabled=0。源meta227/live235普通读成功；保留原Temp/GUID结构的Old/Incoming/Local=289/294/291成功；真正短树=249/254/251普通读成功且计划/hash等价；备份后缀272提交/回滚通过。
- **唯一环境阻断：AC06 文件 symlink 场景**，`Owned_LinksAreRejectedWithoutFollowingTargets("file")` 因 Win32 1314 skipped。目录 junction、真实ACL拒绝、并发/嵌套、归属标记/占用/残留/清理等已通过；不能用它们或静态检查代填文件 symlink 通过。UNC只有格式验证、实网未测；其他OS未测。
- 源码未提交身份：[source-identity.json](evidence/source-identity.json)，16个文件聚合SHA256 `84084d51b877aacabf644720ab149b07d7a2b5f4d241112436e64fad91e6ce75`。[改前实际工作区备份](evidence/implementation-start/)，不是HEAD替代草稿。两份用户字体及既有规则/skill/流程等其他改动保留；没有提交/发版/推送、消费升级、真实部署/快照修改或全量/长时测试。
- 已准备可独立使用的 [consumer-validation-prompt.md](consumer-validation-prompt.md)，完整嵌入背景、未发布身份/逐文件指纹、接入需另行授权、当前消费包核对、普通读取前提、只读指纹保护与结果格式；当前为未完成交付草稿，消费端尚未验证。

恢复：在具备文件 symlink 创建权限的 WindowsEditor 环境核对同一工程根/UPM/代码指纹，MCP主动编译并确认加载后，只补跑上述文件链接参数case（约1秒；方法过滤若带出其余3个case应如实记录）。确认实际执行通过及目标字节未变后更新AC06；再直接正式归档所有有效资料与证据/备份，核对独立可读/链接、完成日期和精确任务目录所有权后才清理。本次不改系统全局策略或提权来制造通过，不要求用户重新审批原有实施范围。

---

以下为历史 r5 及更早交接，未决描述不覆盖本节。

# 当前有效交接 r5：方案完成，待实施

更新：2026-11-11，Asia/Shanghai。任务 ID：20261111-patch-preview-long-paths。此节及 design.md r2 优先于下方 r1–r4 历史；历史中的“实施中”“独立验收/回入口”等不再表示当前状态。

## 有效资料与下一步

- 当前阶段：方案已完成；下一阶段：实施、本地定向验证、直接正式归档。本地修复未完成，AC01–AC08 运行验收仍待实施，不把本阶段静态审查记为运行通过。
- requirements.md r1 的 R01–R08、AC01–AC08 完整有效，未改需求或降低标准。
- 有效方案：design.md r2，含源码链路、取舍、草稿审查、准确改动范围、顺序和逐项 AC 映射。入口 r1 方案已替代。
- implementation.md 仍仅记录入口误提前实施的未验证事实；不代表本轮实施成果。入口335字符探针不作为修复验收证据，历史第二次编译/域重载仍未核对。
- 仅两个有效阶段提示词：prompts/02-design.md（本阶段已完成）→ prompts/03-implementation.md（已按 r2 更新，可直接接手）。prompts/04-acceptance.md 仅历史，不执行。
- 用户最新专属方式：实施完成必要本地验证后直接正式归档，附消费端验证提示词；不另开独立验收、不等待消费端回传、不要求回入口，不修改公共规则/流程/skill。实施自检如实标为自检。
- 正式归档：docs/tasks/20261111-patch-preview-long-paths/；实施通过后承接有效需求、决策、实际交付、逐项结果和必要证据，再安全清理精确本任务目录。

## 本轮实际核对及成果

- UPM manifest=file:com.ember，lock=embedded；实际包 Packages/com.ember，version=0.25.2 未发布工作区；Unity ProjectVersion=6000.5.4f1 (d550df8bd089)。已从实际位置读取公共工作流/交接模板。
- 分支 codex/local-snapshot-20261111-162819；HEAD 16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2。正式编辑记录 topdown-2d 0.1.2；不切换/保存/Bump。
- 实际 git status：未暂存四个 Core 草稿（ProjectSetup.cs、ProjectSetup.Patch.cs、InheritanceEngine.cs、Transaction.cs）及 PatchEditTests；未跟踪 FileSystem/TemporaryDirectory 及各自 meta。另有既有 SKILL.md、AGENTS.md、ai-skills.md、ai-workflow.md、glossary.md 改动和两份用户字体，全部保留。本阶段只写任务文档；接手必须重新核对，不假定历史清单或 MCP 实例 ID 有效。
- 独立源码审查后采用统一模板文件系统边界，保留原 Temp/GUID 前缀。短前缀不能消除长路径边界，故不作为修复。
- 草稿必要修正：Core alias 不跨 partial；AISkills、UPM 安装器的 FileInfo/SHA256/枚举、扩展默认配置读取仍有普通 IO 缺口。适配迁到 UPMManager.Editor，沿现有 Core→UPM 依赖共享，避免循环依赖。
- owned scope 创建/标记失败也须报告残留；不得扫描整个共享 Temp；补独占归属、精确边界和链接检查。预览清理失败可报错，应用已提交后的清理失败必须明确已提交及残留，不能误报未提交。版本头严格失败仅用于补丁，避免无关 best-effort 行为改变。
- 本阶段未修改/回滚产品代码、测试、资源、公共文档或 skill；未执行 Unity MCP、运行时探针、编译、测试；未操作 Windward。设计提议不等于已实施。

## 授权传递和实施入口

用户已授权：当前框架实际源码的本地修复、直接相关增量测试、Unity MCP 主动编译，以及本地完成后正式归档和安全清理本任务临时资料；无需再次请求相同范围批准。必要测试失败不得以直接归档替代验证。

未授权：提交、发版、推送、消费升级；Windward/其他消费项目 PackageCache、消费源资源、真实部署基线和模板快照修改；全量或预计长时间测试。真实部署/模板保存/Bump 不在本任务内，应用/回滚只在隔离临时项目验证。

下一对话直接使用 prompts/03-implementation.md。先读本节、requirements.md、design.md r2、implementation.md，核对实际 Git/UPM/Unity 目标，然后接续草稿并补缺口。必要本地事项通过后直接完成归档，最终交付正式记录及 consumer-validation-prompt.md；消费端未测如实记录，不等待其回传。本阶段到此结束，不在本对话继续实施。

---

以下为历史交接原文（保留过程事实，不恢复旧状态）：
# 20261111-patch-preview-long-paths：增量预览 Windows 长路径

## 当前有效状态（r4，2026-11-11，优先于下方历史记录）

用户最新明确要求：提供当前长路径问题的方案、实施两份提示词；实施阶段完成必要本地验证后直接正式归档收束，不等待消费端回传，不另开独立验收或回入口，不修改通用 skill。授权边界与 R01–R08、AC01–AC08 不变。当前入口只完成交接，方案与实施均未完成；现有 design.md 和实现均为待审查草稿。

有效启动文件：prompts/02-design.md → prompts/03-implementation.md。方案先完成，实施再接手。原 prompts/04-acceptance.md 只留历史，不再执行。需求仍用 requirements.md；其中旧“独立验收另一个对话承接”已由本任务例外替代。下方 r1/r2 保留历史事实，不作为当前阶段或收束方式依据。

## 身份、阶段与有效资料
- r1 / 2026-11-11 / Asia/Shanghai。
- 入口：本次用户调用 ember-solution-design 并要求排查与本地修复的对话；临时目录所有者为本任务，目录下文档和 evidence 均属本任务。
- 正式归档建议：docs/tasks/20261111-patch-preview-long-paths/，独立验收后回入口核对。
- 当前连续推进需求、方案和实施自检；用户完整实施授权及公共 ai-workflow.md 的“已有完整授权允许连续推进”条款适用。独立验收未进行。
- 需求：requirements.md r1；方案：design.md；实施与证据后续更新。状态：实施中。

## 环境与实际基线
- 仓库：ember-unity-framework；分支 codex/local-snapshot-20261111-162819；HEAD 16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2。
- 初始未暂存：Packages/com.ember/SharedAssets/Fonts/NotoSansSymbols2/NotoSansSymbols2-Regular SDF.asset、Packages/com.ember/SharedAssets/Fonts/钉钉进步体/DingTalk-JinBuTi SDF.asset；均非本任务，不修改或还原。
- embedded Packages/com.ember，package.json 0.25.2，UPM manifest/lock 均 file:com.ember。
- Unity 6000.5.4f1，WindowsEditor；HKLM SYSTEM/CurrentControlSet/Control/FileSystem LongPathsEnabled=0。
- 正式编辑身份 Assets/Editor/EmberEditingTemplate.json：topdown-2d 0.1.2；本任务公共框架修复不切换或保存模板；无消费升级/模板基线变更。
- Unity MCP 目标仅 ember-unity-framework@b23e9b768e85ed30；Windward 同时在线但不操作。

## 授权与下一步
- 已授权实际源码检查、本地实现、直接相关测试、Unity MCP 主动编译。无须再次实施审批。
- 未授权：发版、推送、升级 Windward、PackageCache 修改；未授权全量或长时测试。不提交。
- 三阶段提示词见 prompts/02-design.md、03-implementation.md、04-acceptance.md；提前生成不代表前置完成。
- 连续实施自检后更新 implementation.md 与证据，再由另一对话执行独立验收；必要验收未完成则不标整个工作流完成、不清理本任务资料。

## 用户纠正后的当前状态（r2）

2026-11-11 用户指出入口应分阶段交给其他对话，代理确认偏离 ember-solution-design 阶段职责，立即停止继续代码修改和测试。此前“连续实施”是代理误解，不是用户明确改变分阶段方式；以本节为准。

- 当前阶段回到需求交接；方案草稿与部分实现已存在，不能视作方案阶段完成或实施完成。
- 没有创建、派发或联系其他对话。按技能由用户依次使用三个提示词；只有明确要求创建/派发才操作。
- 已修改 Core/Editor 的 EmberProjectSetup.cs、EmberProjectSetup.Patch.cs、EmberTemplateTransaction.cs、EmberTemplateInheritanceEngine.cs，新增 EmberTemplateFileSystem.cs、EmberTemplateTemporaryDirectory.cs；Tests/EditMode/EmberTemplatePatchEditTests.cs 新增相关用例。全部为未验证草稿，不提交，不擅自回滚。
- 已主动提交两次 Unity MCP 编译请求：首次 scope=scripts 未导入新增文件，Console 报 CS0234 找不到 EmberTemplateFileSystem；随后 scope=all 的 force refresh 已成功送达并触发导入/编译。第二次编译和域重载结果尚未核对，不能宣称编译通过。用户纠正后不继续调用 Unity。
- 未运行任何回归测试；当前没有行为验收通过结论。先前 335 字符运行时探针仅证明当前运行时普通读取失败、扩展路径 IO 可用，不代表修复完成。
- 后续方案对话应先审查需求与草稿取舍，再更新有效方案；实施对话接手草稿，先核对第二次编译实际状态及源码，再修复、定向验证。独立验收必须另一个对话。

## 本任务专属工作方式变更（r3，2026-11-11）

用户澄清：省略独立验收和等待消费端回传仅适用于当前长路径问题，不修改通用 skill 或公共工作流。通用规则已恢复到本轮简化前，保留此前获授权的“整体任务授权不改变阶段边界”说明。

本任务仍交接方案、实施两份提示词；方案由后续方案对话承接，实施对话完成本地修复、Unity MCP 必要编译与定向自检后直接归档收束，按实际交付生成消费端验证提示词，不等消费端回传，也不用再回入口。消费端未测如实记录，不伪称已验收。原 prompts/04-acceptance.md 保留为历史，不再是本任务必经阶段。此例外不授权入口继续实施，不改变不发版、不推送、不升级 Windward、不修改 PackageCache 的边界。

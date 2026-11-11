接续任务 20261111-patch-preview-long-paths：Ember 增量预览 Windows 暂存长路径修复。本对话负责实施、必要本地定向验证及直接正式归档收束。方案阶段已完成，按 design.md r2 执行，不重新要求实施审批。

工作区：C:/Users/wuyu/My/ember-unity-framework
任务目录：.utmp/ember-workflow/20261111-patch-preview-long-paths/
正式归档：docs/tasks/20261111-patch-preview-long-paths/

先读 AGENTS.md、CLAUDE.md，通过 UPM 实际解析位置完整读 Documentation~/maintenance/ai-workflow.md、ai-handoff-template.md，再读 handoff.md 最新有效 r5、requirements.md r1、design.md r2、implementation.md 的未完成事实。先核对当前 Git/分支/HEAD、UPM、正式模板身份和实际源码，不沿用旧实例 ID。design r2 是有效方案；入口实现/测试草稿仍未完成，不能因存在就视作通过。

本任务用户明确只用方案、实施两阶段。你完成必要本地修复、编译和定向验证后在本对话直接归档；不另开独立验收、不等消费端回传、不要求回入口、不修改通用 skill/流程/规则。已有授权包含本地实施和相关验证，无须再次审批；不因例外降低 R01–R08、AC01–AC08 标准。同对话检查记实施自检。

实施前完整读编码规范、docs/dev/ember-api-reference.md、unity-validation.md。先备份要改的实际工作区内容到任务 evidence，不能用 HEAD 代替未提交草稿。保留两份用户字体，以及现有 skill/AGENTS/流程/术语等其他改动，不回滚。历史首次 scripts 编译报新文件未导入的 CS0234；第二次 force/all compile=request 已送达但结果未知，先查询实际状态，不重复结果不明请求。最终改完后仍主动请求本轮编译，确认本轮编译/域重载及新程序集加载，再读 Console、运行测试。

按 design.md r2 的文件表和步骤落实，尤其不能漏掉方案审查发现的缺口：
1. 保留项目 Temp/原 GUID 前缀；采用统一长路径 IO。迁移草稿 EmberTemplateFileSystem 到 UPMManager/Editor（保留 meta GUID，Core 不留重复定义），沿 Core→UPM 现有依赖共享，不能新增反向引用。前缀仅最终 IO 边界，DOS/UNC、枚举还原/边界检查和非 Windows 透传；不进入 AssetDatabase、计划、相对路径、持久记录或外部工具参数。
2. 接续 Core 的预览复制/meta/hash/规范化/三方比较、应用和共享事务 IO；补 AISkills partial（alias 不跨文件）、UPM 安装器可达 Snapshot/Hash/FileInfo/Prepare/校验、扩展默认配置读取。只改实际可达边界，不全仓库替换；不改变 MD5/SHA256、CRLF/版本头、排序/大小写、GUID、stale-plan、冲突和事务顺序语义。版本头严格 IO 失败用于补丁路径，其他 best-effort 调用保留原约定并明确诊断。
3. 修正 owned scope：创建/owner 标记失败在报告范围，未建立归属不递归删；独占凭据、精确本次 root、父链/子树链接检查，不递归检查整个共享 Temp。保留并发/嵌套/中断残留，不扫旧目录。首要异常对象/堆栈/具体路径保留，次级清理失败附诊断及残留警告。预览成功但清理失败应明确失败；应用已经提交后清理失败应报告已提交及残留，不能使调用者误以为未提交。备份与回滚失败保留所有相关错误/恢复路径，不删除未知备份。
4. 按 design 第7节逐项补齐回归，修正现有测试观察依赖被测 alias、单组件长度及仅断言 live 235 的缺陷。内部注入点限测试，不能暴露新的业务公共回调。隔离临时项目可模拟部署/基线/模板，不对真实工程试应用。

验证必须按 R01–R08、AC01–AC08 逐项记录：
- WindowsEditor、LongPathsEnabled=0 为主要运行环境；核对记录实际 Unity/.NET、平台/文件系统、源 .asset/.meta 普通读取成功且<=235，旧预览结构暂存>260。多层合法组件，完整 BuildTemplatePatchPlan 成功并得到正确差异/冲突；335字符历史探针和其他系统结果不替代本轮验证。
- 比较预览前后 source/live/部署记录/旧基线的原始字节及目录/GUID，不能仅用归一化 hash 证明只读。短树等内容应有相同 hash/计划；计划/JSON/枚举不得泄漏特殊前缀。
- 覆盖成功/异常清理、复制/读失败、owner建立失败、锁文件次级清理失败、无主异常预览清理失败和已提交应用清理失败；实际并发与嵌套、标记损坏/缺失、占用、权限/链接、模拟中断残留不扫删。无法运行的安全场景如实待验证，不能记通过。
- 长路径补丁提交、至少一个目标落位后注入失败回滚、另设长事务目标使备份后缀>260（与<=235预览样例分开）、原始内容/GUID/记录/旧基线恢复；补深层 Skill 文件应用以覆盖 UPM 可达 IO。回跑直接相关短路径用例。
- 只运行新增及直接受影响方法；发起前列具体过滤与估时。建议 PatchEditTests 相关方法、TransactionEditTests 的复制/meta/commit/rollback/link 相关方法、文件系统/owned scope 新用例和实际涉及的 UPM 方法。不得默认整个程序集/全量，不拆批规避长时授权。UNC 无共享时只记格式验证，实网未测。
- Unity MCP 是唯一自动编译入口，仅操作重新确认根目录为 ember-unity-framework 的实例；不操作 Windward。断连严格遵守 unity-validation.md：先检查待编译改动/未决请求，无法送达立即请用户手动触发编译，之后每30秒重试最多10次，不用 BatchMode/dotnet/日志轮询替代。请求成功/空Console/idle均不能单独证明本轮程序集已加载。

更新 implementation.md、handoff.md 和必要 evidence，记录实际改动、方案偏差、编译请求/本轮完成/Console、具名测试过滤与 passed/failed/skipped、逐项 AC 自检结果、已知限制。必要本地验证仍失败/受阻时保持未完成、保留资料，明确恢复步骤，不用“直接收束”跳过验证。

本地必要事项通过后无需再询问收束：
1. 生成独立可用的 consumer-validation-prompt.md：问题背景、实际未发布改动身份/版本/可核对指纹、接入需另有授权、当前消费项目实际包解析核对、源普通读取前提和路径长度、只读预览/指纹保护/预期结果/结果格式。不能默认 Windward 已升级、不能隐含 PackageCache 修改或真实应用授权，不仅链接框架临时目录。
2. 将有效需求、方案决策、真实交付、逐项本地 AC 结果、证据和限制、消费端提示词归入正式目录。明确“本地交付完成；消费端尚未验证”，不冒充独立验收，不等消费端回传。
3. 核对正式文档与证据可读取、链接有效且不依赖临时目录后，记录完成日期；核对精确任务目录绝对路径、ID、所有权、无 symlink/junction/未知内容，仅删除已归档承接的本任务临时资料，保留其他任务和无承接备份。归档失败不标完成、不清理。
4. 最终说明根因/取舍、改动范围、真实环境及编译/测试结果与限制、正式归档和消费端提示词链接，本对话直接结束本地任务。

授权限制：只本地修复和直接相关验证；不提交、发版、推送、升级 Windward，不修改任何消费项目 PackageCache、消费源资源、真实部署基线或模板快照；不运行全量或预计长时间测试。本任务例外不修改公共规则/skill，不再等待独立验收或回入口。


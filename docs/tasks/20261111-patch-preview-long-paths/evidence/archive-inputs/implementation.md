# 实施记录 r1：修复与定向自检已执行，AC06 文件 symlink 场景受环境限制

更新日期：2026-11-11，Asia/Shanghai。当前状态：**未完成，保留任务资料；不正式归档、不清理**。本轮为实施自检，不是独立验收。用户明确仅方案、实施两阶段，后续满足必要本地条件即可由本对话直接正式归档，不需要新验收对话或回入口，不等待消费端回传。

## 实际基线与保护

- 根目录 `C:/Users/wuyu/My/ember-unity-framework`；分支 `codex/local-snapshot-20261111-162819`；HEAD `16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2`，未提交、未发版、未推送。
- UPM 经 Unity PackageInfo 实际核对：Embedded、0.25.2，resolvedPath 为本仓库 `Packages/com.ember`。不是发布的新版本；改动身份见 [source-identity.json](evidence/source-identity.json)，16 个交付文件原始 SHA256 聚合为 `84084d51b877aacabf644720ab149b07d7a2b5f4d241112436e64fad91e6ce75`。
- 正式编辑身份 `Assets/Editor/EmberEditingTemplate.json`：topdown-2d 0.1.2，contentHash `d83863b16bda8c2655f21d6aa3d55693`。未切换/保存/Bump/部署模板；测试中的部署、应用、恢复和基线均位于自建隔离临时项目。
- 改前实际草稿备份：[implementation-start](evidence/implementation-start/)。包含原四个 Core 草稿、原新增 FileSystem/TemporaryDirectory 和 meta、原 Patch 测试及本轮将改的其他源码；不是 HEAD 替代备份。初始/最终 Git 状态均有 evidence。
- 未触碰两份用户字体、既有 skill、AGENTS、公共流程/术语等其他改动；未操作 Windward、任何消费项目 PackageCache、真实部署基线或模板快照。

## 实际交付与方案偏差

依 design.md r2 落地；准确链路见 [IO 自检](evidence/io-audit.md)，工作区补丁见 [tracked diff](evidence/delivery-tracked.diff)（不包含未跟踪新文件正文；新文件按 manifest 指纹核对工作区）。

| 范围 | 实际变化 |
|---|---|
| UPMManager/Editor/EmberTemplateFileSystem + meta | 从 Core 迁移，原 GUID 保留；DOS/UNC 转换、设备/尾空格尾点拒绝、枚举边界与普通路径恢复；统一 File/Directory/流/FileInfo、raw MD5、原异常 Data 诊断；补 Encoding 重载 |
| Core/Editor/EmberProjectSetup.cs、.Patch.cs、.AISkills.cs | 每个 partial 独立 alias；补丁严格版本头 IO；其他原 best-effort 保留并警告；预览/应用 owned scope；提交成功后清理失败保持 count 并明确 warning |
| Core/Editor/InheritanceEngine、Transaction | meta/hash/复制/移动/枚举/属性/清理统一边界；逐目标累积回滚错误和恢复路径；拒绝同进程提交重入及未知备份；不改算法、GUID、stale-plan、冲突和事务顺序 |
| Core/Editor/TemporaryDirectory | 父链与本次子树安全检查，独占 owner lease，创建/owner 写入失败报告，未归属不递归删；保留主错误/次级清理诊断；不扫描共享 Temp、不认领旧残留 |
| UPM Installer、Templates | Within/Snapshot/Hash/Length/ReadCatalog/目录/Prepare/校验的可达 IO；未重构无关 Install 分支 |
| ExtensionProject、Identity | 暂存 defaults 的存在性/读与时间戳边界适配，缓存键和解析契约保持 |
| PatchEditTests、新 FileSystemEditTests | 独立原始字节/目录观察器，合法多层长路径、真正短树等价、深层 Skill、owner/并发/权限/junction、清理和事务回归；内部实例注入点仅用于测试，无公开业务回调 |

实施发现并修正：持有读写 lease 时，另开 owner 读取流须以 `FileShare.ReadWrite` 允许已存在的写句柄；原 lease 自身的 `FileShare.Read` 仍阻止其他 writer/delete。首轮测试稳定揭示并修正该共享冲突，没有因此缩短/移走暂存根。

验证实现偏差：Unity Mono 的 Directory.GetAccessControl 未实现，权限夹具改用隐藏的 Windows PowerShell 子进程，仅设置并最终恢复自有临时目录 ACL；符号链接创建 Win32 1314，目录用实际 junction 补测，文件 symlink 仍跳过。未调整系统策略、开发者模式或提升权限。最后修正短树夹具，改为独立短根，显式验证所有短 stage <260，不能只改项目目录名。

## 编译与 Console

历史未决请求先查询：编辑器不在编译，Console 实际为 CS1501（WriteAllLines 缺 Encoding overload），FileSystem 尚未加载；未重复历史未知请求。

本轮所有自动编译只走 Unity MCP，重新枚举并固定框架实例后核对 projectRoot；未操作另一个 Windward 实例。每批修改主动 compile=request/force/all，共 4 次；连接在域重载时暂不响应均按 30 秒恢复查询，首个恢复查询即成功，无重复未决请求，无 BatchMode/dotnet/日志轮询。

最终请求及完成证据：[request](evidence/compile4request.json)、[state](evidence/compile4state.json)、[loaded assemblies](evidence/compile4loaded.json)、[编译后 Console](evidence/compile4console.json)。最终编译启动 21:38:10.372、完成 21:38:14.376、域重载完成 21:38:19.825（Asia/Shanghai；以 evidence epoch 为准）；isCompiling/isUpdating=false。新程序集实际加载：

- Core `f7a9767c-d956-4fb9-8a5d-f7263cf3c16f`；UPM `18af4670-80b2-431c-9dd7-e7a77b9af1f2`。
- Tests 最终 `e2ddb411-d417-4bb6-8a96-e7a5fe7a3af7`（较上一轮变化）；新门面与 hooks 曾通过反射确认。
- 最终编译后及最后定向测试后 Console error/warning 均 0 条：[最终 Console](evidence/consolefinal.json)。故障测试期间的预期 warning 在对应用例 output 中保留，不用末次空 Console 抹去历史故障。

## 定向测试真实结果

发起前已说明具体过滤及估时；[完整具名过滤](evidence/test-filters.json)。仅 4 个类中的具名新用例或直接相关方法，非整个程序集；没有扫描全模板来源、没有全量/长时测试。

| 请求 | 过滤与估时 | 实际结果 | 证据 |
|---|---|---|---|
| 643457abbd6349a182b1f58fce65610b | Patch、FS/owned、Transaction、UPM 具名方法，30–90 秒 | 72 total / 41 passed / 29 failed / 2 skipped，46.6209 秒；揭示 lease 共享冲突、ACL API/链接环境问题和一处路径分隔断言问题，均未记通过 | [首轮结果](evidence/test1final.json) |
| 20ca5bffa0bb4eea864828e70533985a | 同相关范围，加无变化预览与已提交应用清理失败，50–90 秒 | 74 total / 73 passed / 0 failed / 1 skipped，57.0378 秒；唯一跳过是实际文件 symlink，Win32 1314 | [复跑结果](evidence/test2final.json) |
| 42192328b8214559a28eb358aa0383ce | 最后改动的 FullPreviewPreservesInputs（2 case）与 ShortEquivalentPlanAndHashesMatch，约 5 秒 | 3 total / 3 passed / 0 failed / 0 skipped，3.5140 秒；覆盖最终测试源码 | [最终定向结果](evidence/test3final.json) |

最后 3 例替代上轮同名方法结果，不把两轮相加宣称 77 个独立用例。最终覆盖 74 个独立 case，73 通过、1 待验证。

实测环境：Unity 6000.5.4f1 WindowsEditor；CLR 4.0.30319.42000；Microsoft Windows NT 10.0.26200.0；NTFS；注册表 LongPathsEnabled=0（主集成用例显式断言）。源 .asset/.meta 普通 IO 可读且 meta 227；live meta 235；原结构 Old/Incoming/Local meta=289/294/291。真正短树 Old/Incoming/Local meta=249/254/251，均普通读取成功。长事务目标=258、备份=272。335 字符历史探针未作为本轮验收证据。

## R01–R08 / AC01–AC08 实施自检

| 需求 / AC | 当前结果 | 实际证据与边界 |
|---|---|---|
| R01 / AC01 | 通过（源码自检） | [IO audit](evidence/io-audit.md) 覆盖创建、复制/meta/hash/三方、应用与共享事务，以及 UPM/defaults 原遗漏；运行链路见新集成测试 |
| R02 / AC02 | 通过（本机） | 完整 BuildTemplatePatchPlan 长路径成功；普通源/live 前提及所有真实长度、平台、策略关闭见 test3 output；保留 Temp/完整GUID 原结构 |
| R03 / AC03 | 通过（Windows 本地及 UNC 格式） | 枚举还原与边界拒绝、DOS/UNC/设备/点段、raw MD5、长短 hash/计划等价、版本头/CRLF/GUID 与 JSON 无前缀；UNC 实网、其他 OS 未运行 |
| R04 / AC04 | 通过（隔离夹具） | 原始 byte+目录 fingerprint 覆盖 source/live/记录/旧基线及 meta/GUID；无变化、父变更、冲突/选择、stale-plan、成功清理；未写真实工程业务资源 |
| R05 / AC05 | 通过（所列故障） | 复制中途、严格版本头读、meta三方读、owner创建/写失败、锁文件主异常+次级清理诊断、无主异常预览清理失败、实际应用已提交后清理失败返回/警告；原异常对象/堆栈/路径与恢复错误保留 |
| R06 / AC06 | **待验证，不能标通过** | 实际并发/嵌套、缺失/损坏凭据、占用、独占 lease、权限、父/root/子目录 junction、共享 Temp 无关 junction、中断残留保留已通过；**文件 symlink** 因创建权限 Win32 1314 未运行；不以 junction 或静态检查替代 |
| R07 / AC07 | 通过（隔离夹具） | 长路径提交、至少首目标落位后注入回滚、原字节/GUID/基线/记录恢复；272 字符备份、未知备份拒绝、备份清理失败/回滚错误；深层 Skill 走 UPM 可达链路；直接相关短事务/父同步回归 |
| R08 / AC08 | 编译与定向验证记录通过；整体受 AC06 限制 | MCP 编译/域重载/MVID/Console证据完整，具名结果与 skip 如实记录；未执行提交/发布/消费升级/全量测试等未授权动作 |

## 剩余事项与恢复步骤

1. 在具备文件 symlink 创建权限的 WindowsEditor 环境重开同一工作区；不要求或暗中修改当前机器全局开发者策略，不将目录 junction 当作文件链接替代。核对实际 root、UPM 和 source-identity；有源码差异则重新评估受影响验证。
2. 通过 Unity MCP 主动编译并确认本轮域重载及新程序集加载，读 Console。只补跑 `Ember.UI.Tests.EmberTemplateFileSystemEditTests.Owned_LinksAreRejectedWithoutFollowingTargets("file")`（约 1 秒），核对场景确实执行而非 skipped，并确保链接指向的目标原始字节不变；具名过滤可用方法名选该参数组时需如实记录其余 3 个 case 的结果。
3. AC06 未通过前不标本地交付完成。成功后在当前实施对话直接完成正式归档；归档承接本目录需求/方案决策、实际交付、逐项结果、证据/备份及消费端提示词，核对链接后按精确所有权规则清理任务临时资料，无须独立验收或回入口。

已准备 [消费端只读验证提示词草稿](consumer-validation-prompt.md)，含实际未发布身份及全部文件指纹，可独立使用；当前未交付为“本地完成”。消费端尚未验证，也不等待其回传作为此处恢复前提。

---

以下保留入口阶段历史未完成草稿，不替代上述本轮事实。

# 未完成实施草稿的交接

2026-11-11：入口对话误提前实施，用户纠正后停止。此文件仅记录工作区事实，不表示实施阶段通过。

已有草稿：模板局部 IO 适配、DOS/UNC 原生路径转换与枚举恢复；预览/应用临时目录归属及异常清理；共享事务 IO 接入；版本头 IO 不吞异常；新增长路径/冲突/回滚/清理测试。详见实际 diff，需正式方案审查。

验证：WindowsEditor 6000.5.4f1，Environment.Version=4.0.30319.42000，LongPathsEnabled=0，335 字符文件的普通 File.ReadAllText 抛 DirectoryNotFoundException；扩展路径创建、读、枚举、删除成功。该探针已自行清理。

编译：首次仅 scripts 请求暴露新文件未导入的 CS0234；第二次 force/all refresh+compile=request 已送达。用户纠正后未确认第二次编译、域重载或 Console。测试全部未运行。不得将草稿作为已完成交付。

保留用户原有字体资源改动，没有修改 Windward、PackageCache、源模板快照，没有提交、推送或发版。

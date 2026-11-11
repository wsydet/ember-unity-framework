# 增量预览 Windows 长路径修复方案 r2（有效）

日期：2026-11-11，Asia/Shanghai。状态：方案完成，待实施；取代入口 design.md r1。依据 requirements.md r1 的 R01–R08 / AC01–AC08，全部保留，不降低标准。本文件是实施依据，不是已完成修复或运行验证结论。

## 1. 阶段、归属与核对基线

本任务为框架公共 Editor IO 修复，非 Windward 产品业务、非模板升级。用户明确只用方案、实施两阶段；实施完成必要本地验证后直接正式归档，并附消费端验证提示词。不另开独立验收、不等待消费端回传、不回入口，也不改通用流程或 skill。

本阶段只读核对 AGENTS.md、CLAUDE.md、UPM manifest/lock、实际 embedded 包 Packages/com.ember 内 ai-workflow.md / ai-handoff-template.md、架构、通用及 topdown-2d 术语、unity-validation.md；未调用 Unity、未编译/测试/探针，未操作 Windward。

- 当前分支 codex/local-snapshot-20261111-162819，HEAD 16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2。
- manifest 的 com.ember=file:com.ember，lock source=embedded；package.json=0.25.2（工作区有未发布草稿）。ProjectVersion=6000.5.4f1 (d550df8bd089)。
- 正式编辑记录 Assets/Editor/EmberEditingTemplate.json：topdown-2d 0.1.2。本任务不保存、切换、Bump、部署模板。
- Git 当前未暂存：Core/Editor 的 EmberProjectSetup.cs、EmberProjectSetup.Patch.cs、EmberTemplateInheritanceEngine.cs、EmberTemplateTransaction.cs；Tests/EditMode/EmberTemplatePatchEditTests.cs。未跟踪：EmberTemplateFileSystem.cs、EmberTemplateTemporaryDirectory.cs 及各自 meta。以上仅为既有草稿。
- 同时有 .agents/skills/ember-solution-design/SKILL.md、AGENTS.md、maintenance/ai-skills.md、ai-workflow.md、glossary.md 的既有改动；本阶段不修改或还原，实施也不得借本任务修改。两份用户字体 NotoSansSymbols2-Regular SDF.asset、DingTalk-JinBuTi SDF.asset 必须保留。
- LongPathsEnabled=0、Environment.Version=4.0.30319.42000 及 335 字符探针来自入口记录；本阶段未重新实测。历史第二次编译/域重载未确认、测试未运行；不得沿用旧 MCP 实例 ID，实施重新核对。

## 2. 独立源码核对与根因链（R01）

对照当前源码和 git diff 的原实现，不能仅据入口摘要推断：

| 环节 | 实际入口/调用与风险 | 设计落点 |
|---|---|---|
| 预览前复核 | Patch.cs 的 ReadPatchDeploymentData、ValidateDeploymentBaseline、PatchProjectHash；InheritanceEngine.CaptureDirectory 会安全检查、枚举、读 meta、读 bytes/hash | 三方源、旧基线及本地都经过同一 IO 边界；存在性判断不能把权限/路径错误误当缺失 |
| 暂存创建 | BuildTemplatePatchPlan 使用 Temp/EmberPatchPreview-32位GUID~/Old、Incoming、Local；相较 Assets 前缀增长约 56 字符 | 保留现有隔离目录形式；创建、归属、清理独立管理 |
| 暂存复制 | CopyPatchAssets → Transaction.CopyDirectory → 链接检查、递归目录/文件枚举、创建、File.Copy、MaterializeFolderMetadataDirectories.ReadLines | 复制两端、属性和 folderAsset meta 物化一起适配 |
| 规范化 | CopyPatchAssets 遍历暂存文件，RewriteVersionMarker 读写 bytes，仅暂存改写头为 0.0.0 | IO 失败必须抛出；不能以 false 当无变化继续生成差异 |
| 三方差异 | ValidateCaseInsensitivePathUniqueness → BuildThreeWayChanges → CaptureDirectory → ReadMetaProperties.ReadLines、ComputeTemplateFileContentHash(ReadAllBytes)、ComputeTreeHash | 恢复普通路径后才求相对路径；保留 ordinal 排序、文本 CRLF 归一化、MD5 及冲突规则 |
| 预览结束 | 原 finally/TryCleanTransactionPath 静默吞清理失败；草稿改用 owned scope | 无论成功、复制失败、读取失败、校验失败都进入清理/残留报告 |
| 补丁应用 | CommitTemplatePatch 暂存 Assets，ApplyPatchChanges、GUID 校验、PreviewSkills/ConfirmLifecycleSkills、AddDeploymentBaseline/Record、CommitPreparedTargets | 包括深层技能文件和扩展默认配置的暂存读取，不能只修 .asset 测例 |
| 事务 | CommitPreparedTargets 先移动所有 destination 到 .ember-backup~，再移动 staged；失败逆序 Rollback，成功清备份 | 同一适配覆盖 Copy/Move/Delete/属性、raw MD5 复核；不改目标顺序、备份后缀或恢复策略 |
| 共享调用者 | CommitTemplateDeployment（AISkills partial）、RestoreTemplateDeploymentBaseline、ApplyParentSync 复用上述实现 | 只修直接相关 IO/清理；在隔离夹具验证，不执行真实部署或父模板同步 |

用户报告源 Assets 最长 235、旧 Local 最长 289、meta 存在却 ReadLines 抛 DirectoryNotFoundException。源码支持“暂存增长暴露普通 IO 长路径边界”的解释；335 字符探针仅证实一个运行时的部分原语，不是完整链路证据。源可读也不保证旧基线、暂存、备份路径可读。

## 3. 取舍和支持边界（R02、R03）

| 候选 | 效果与代价 | 决策 |
|---|---|---|
| 仅改成短名/短 GUID/短工程路径 | 降低概率，但深层源、基线与备份仍可超限；缩短 GUID还削弱隔离 | 不作为修复，不宣称消除边界 |
| 工程外短暂存/盘符映射 | 可能缩短；引入不同卷移动、权限、归属与映射生命周期问题，仍有长路径 | 放弃，保持项目 Temp 和同卷事务预期 |
| 系统策略或依赖用户改名 | 不覆盖 LongPathsEnabled=0，且涉及全局配置 | 放弃，不修改注册表/manifest |
| 模板调用链统一适配扩展路径 IO | 覆盖创建、读写、枚举、复制、移动、删除；业务路径仍普通形式 | 采用；保留原暂存前缀，避免把“变短”混同修复 |

Windows 本地盘普通绝对路径转换为 `\\?\C:\...`；UNC 转换为 `\\?\UNC\server\share\...`。先在逻辑层规范化绝对路径与分隔符，处理点段，再转换。拒绝设备命名空间如 `\\.\`、非 DOS/UNC 扩展设备路径；可识别自身生成的扩展 DOS/UNC 路径并幂等，但公开业务入口不接受任意设备路径。相对路径先按原调用约定解析，不能把扩展前缀直接加在相对路径上。不得借此接受原本不合法的尾空格/尾点等别名、越界路径或改变大小写/Unicode。非 Windows 原样透传，保留原有大小写策略。

前缀只在最终 System.IO/FileStream/FileInfo 访问处出现。枚举输出恢复调用方普通 root 的路径形式；验证原生枚举项确在 nativeRoot 的分隔边界内再映射，不能盲目 Substring。Directory.GetParent 不返回带前缀 DirectoryInfo 给业务层；纯路径父级计算使用普通逻辑路径。计划字段、RelativePath、部署记录、日志中的业务路径、AssetDatabase 调用和外部工具参数不携带前缀。提供局部 raw MD5 文件入口或就在最终 GetMD5File 参数边界转换；不改 CryptographyUtils 全局算法。

微软文档说明扩展路径可绕过部分传统 MAX_PATH 限制，但受组件长度、总长、API 和文件系统限制；LongPathsEnabled 策略还依赖应用声明，不能把策略开关当普遍保证。参考：[Microsoft Maximum Path Length Limitation](https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation)（本阶段核对）。

目标是当前本地 WindowsEditor 6000.5.4f1、LongPathsEnabled=0 的模板受管文件 IO；包声明 unity=6000.0 不等于所有 6000.x/Mono/.NET 行为已验证。Environment.Version 不能完整标识 Mono 实现，实施记录平台、Unity 完整版本、系统/卷和实际 API 结果。组件长度仍取决于卷（通常 255），扩展总长也有限（约 32767），权限、非法名称、只读、占用、网络卷限制仍生效。UNC 需做转换与枚举映射定向测试；未有实际网络共享时明确实网未验证，不许伪造通过。不扩展 Unity AssetDatabase 本身或 UnityYAMLMerge 进程的长路径协议，不宣称所有合法磁盘路径都能被 Unity 导入。

## 4. 文件系统适配和准确改动范围

草稿适配目前位于 Core.Editor，不能直接让 UPMManager.Editor 引用它：asmdef 现有方向是 Core.Editor → UPMManager.Editor，反向会循环。正式方案把纯 System.IO 适配移至 `Packages/com.ember/UPMManager/Editor/EmberTemplateFileSystem.cs`，namespace Ember.UPMManager.Editor；保留原 meta GUID，删除 Core 下重复定义。跨程序集仅公开必需文件系统门面，NativePath 保持尽可能小的可见范围；业务调用者使用门面，不自行传播 native 字符串。无 UnityEngine/AssetDatabase 依赖，无新 runtime API，不改 asmdef 依赖方向。

| 文件（均相对 Packages/com.ember） | 允许的必要修改 |
|---|---|
| UPMManager/Editor/EmberTemplateFileSystem.cs + meta（由草稿迁移） | 规范化、原生转换/枚举还原，文件/目录原语；补 Copy 默认参数/OpenRead/文件大小/GetLastWriteTimeUtc 等实际所需方法；保留 Encoding、FileShare、覆盖语义与惰性 ReadLines 错误传播 |
| Core/Editor/EmberTemplateTemporaryDirectory.cs + meta | 仅负责项目 Temp 的操作 scope、归属、异常和清理；不将通用 CleanPath 变成“有标记才能删”的 API |
| Core/Editor/EmberProjectSetup.cs、.Patch.cs、.AISkills.cs | 接入上述边界及预览/应用 owned scope；各 partial 的 using alias 只在自身文件有效，AISkills 目前仍为普通 IO，必须补齐；严格版本头失败传播只用于补丁暂存，见下节 |
| Core/Editor/EmberTemplateInheritanceEngine.cs、EmberTemplateTransaction.cs | 全部相关属性/枚举/meta/hash/复制/移动/清理接入，补残留诊断；不改 fingerprint、GUID、三方、stale-plan 或事务目标规则 |
| UPMManager/Editor/EmberAISkillInstaller.cs、.Templates.cs | 仅改模板预览/准备/校验可达的 IO：Within 的属性、Snapshot/Hash/EnumerateFiles、FileInfo.Length、SnapshotDirectories、ReadCatalog、PrepareTemplateTargets 等；公用函数复用适配，无改变安装/权限/确认/指纹契约。不要全文件 alias 导致无关安装分支顺带重构 |
| Core/Editor/EmberExtensionProject.cs、.Identity.cs | 仅 SourceIdentities 读取暂存 defaults 的可达读/时间戳边界，保留缓存 key 和配置解析；不改变扩展启用语义 |
| Tests/EditMode/EmberTemplatePatchEditTests.cs、EmberTemplateTransactionEditTests.cs；必要新增 EmberTemplateFileSystemEditTests.cs + meta | 定向集成、事务、安全/转换测试；仅新增需要的测试接口，不暴露产品公共测试回调 |
| 本任务目录与 docs/tasks/20261111-patch-preview-long-paths/ | 实施记录、逐项结果、证据和正式归档/消费验证提示词 |

其他文件、字体、公共工作流/skill、Templates~、ParentSnapshot~、真实部署记录/基线与 Windward 均不在修改范围。实施发现新的必需可达 IO 文件，先记录具体调用和 AC 关联再最小扩展，不做全仓库替换。这里的模板/基线写入验证只针对夹具临时项目。

## 5. 暂存生命周期、错误和安全（R05、R06、R07）

1. 逻辑 root 固定为规范化 projectRoot/Temp 下 operation-32位随机GUID~，operation 为内部允许值（Preview/Apply），不用用户任意路径字符串拼接。校验父链，禁止 reparse point；不递归扫描整个共享 Temp，不因另一操作残留/链接阻断本次。只检查自身 root 树。
2. 检查不存在后创建并不能单独证明独占。在新 root 以 CreateNew 建立 token 归属标记，立即保持独占的 owner/lease 文件句柄，写入完成后才执行 action；同名占用、标记创建/写入失败立即终止。创建到标记完成也置于错误报告范围；尚无完整归属凭据时不得递归删除，必须报告本次尝试路径和可能残留。GUID 和 CreateNew 防偶然碰撞，不宣称抵抗可写同用户恶意进程的所有竞态。
3. 清理前确认精确 parent、完整目录名/token、root 和所有祖先/子项无链接，owner 凭据匹配。释放句柄与删除有序安排，不能锁住自己导致每次清理失败。缺标记、权限不足、链接替换或归属不明时停删并报告。安全预检失败不得跟随链接递归；通用 CleanPath 保留链接检查，调用方负责目标归属。
4. 不扩大 ACL、不提升权限、不去系统 Temp 根创建公共可写目录；继承项目目录的权限，失败给路径及异常。静态检查无法消除检查到使用之间的竞态；面向可信本地项目/合作进程，不提供对抗恶意并发替换的沙箱保证。
5. 预览并发/嵌套各自 root、token/lease，清理仅自身；中断/域重载/崩溃留下可诊断残留，下次不全局扫描认领或清理。不自动清历史 Windward 残留。并发应用不因临时 scope 隔离就变成事务隔离：保留备份已存在即拒绝的约束，目标验证/移动之间再核对，独占 claim 或项目级重入拒绝应覆盖同进程并发写入，不能覆盖、删除未知备份；不承诺跨恶意进程原子事务。
6. action 失败时 `throw;` 保留首要异常对象/堆栈，附操作、普通路径（复制/移动包含源与目标）；惰性 ReadLines 的枚举时异常同样附 meta 路径。次级清理失败写入结构化 Data/诊断及 warning，明确残留位置和原因，不能取代首要异常，也不能因记录诊断本身失败而掩盖原错误。事务现有提交+回滚失败 Aggregate 链保留两者；补全部恢复失败目标/备份路径，不只保留首项而丢恢复状态。
7. 无主异常的只读预览若清理失败，抛带路径和 InnerException 的清理错误，不返回“无残留成功”。应用已 CommitPreparedTargets 成功后清理失败则报告“已提交，残留待处理”的 warning/结果诊断，保留返回的提交结果，不能因 Run finally 抛异常使调用者误以为没提交而重试。提交成功后的备份清理同样明确报告，保留备份不冒称回滚。提交前失败可抛异常并清理自己的 stage。
8. 共享旧暂存调用（Deploy/Baseline/ParentSync）无需强行都迁移为项目 Temp scope；保持位置和生命周期，使用统一 IO、链接检查及明确残留报告。所有自动清理仅限本次创建/事务 state 记录的对象，不能按后缀全盘删除。

## 6. 既有草稿审查结论

| 草稿 | 采用/放弃与必要修正 |
|---|---|
| FileSystem DOS/UNC 转换及枚举还原 | 采用方向，重做程序集落点；补原生前缀输入检查、root 边界校验、DirectoryInfo/FileInfo/流旁路和存在性错误处理。禁止把一次 ReadAllText 探针当所有原语通过 |
| Core 四文件 using alias | 采用有限 IO 接入；不得认为 alias 跨 partial 生效；补 AISkills/UPM/扩展默认配置的可达调用。复核各 overload 原签名 |
| TemporaryDirectory.Run | 采用 owned scope；放弃创建/标记在 try 外、扫描整个 Temp、安全凭据只短暂打开、所有成功清理失败一律抛出的现状，按第5节补齐 |
| CleanPath 增加 ValidateDirectorySafety | 采用拒绝链接的原则；不把“有安全检查”等同“已校验归属”；避免误删其他事务的目标 |
| RewriteVersionMarker 移除全局 catch | 不直接采用全局语义改变。补丁规范化/应用使用严格模式，IO 错误失败退出；其他原先 best-effort 调用保留约定并显式诊断。可加严格内部入口或参数，不能把合法二进制/无标记当错误 |
| CryptographyUtils.GetMD5File(NativePath(...)) | 采用最终 IO 转换的必要性；封装到共享门面更清晰，不改 raw MD5 为模板归一化 hash |
| 新增完整预览/冲突/回滚/异常/嵌套测试 | 采用场景，不能直接认定覆盖完整。当前全测试类 alias 让独立观察也依赖被测适配；235 仅断言 live meta，未明确 source/旧基线所有前提；单长文件名可能碰组件上限，改多层目录并记录实际长度。fixture Fingerprint 仍需完整原始字节/目录/GUID观察 |
| 测试 previewStaged 回调 | 可保留 internal 默认 null 的单一注入点，限定测试用途；补复制中途/归属创建失败、成功路径清理失败和实际并发，不以嵌套替代并发 |

## 7. 实施顺序与定向验收映射

先完整读编码规范和 API 速查，保留当前所有未提交修改；备份要改的实际工作区文件到本任务 evidence（包括原草稿，不能只保存 HEAD）。先核对历史第二次编译的实际状态，不重复结果不明请求；最终改完后仍需本轮新的主动编译。

按顺序：共享门面及程序集迁移 → 原语/路径静态审查 → Core/UPM/扩展相关 IO 接入 → owned scope/错误与事务清理 → 补夹具和定向用例 → MCP 编译/域重载与 Console → 分组相关 EditMode 测试 → 逐项自检记录 → 正式归档。测试预计仅秒级到短时；执行前公布具体过滤与估时，无法可靠估计或预计长时则停止扩大并按授权处理，不能拆批绕过。

| AC | 实施检查/用例和证据要求 |
|---|---|
| AC01 | 保存最终调用链/IO 清单及相关 diff，说明原始失败路径和草稿缺口；不能只看异常堆栈 |
| AC02 | WindowsEditor、LongPathsEnabled=0 实际读取确认；构造 .asset/.meta 源及 live <=235 且普通 System.IO.ReadAllText/ReadAllBytes 可读，源树/旧基线本身可访问；多层合法组件，旧 stage Local meta >260，记录 Old/Incoming/Local 真实长度。完整 BuildTemplatePatchPlan 经过全部链路成功；保留旧结构以确保未因缩短掩盖。对 CreateDirectory/Copy/ReadLines/ReadAllBytes/枚举/属性/Move/Delete 的真实运行覆盖来自集成与事务；335 探针不算 |
| AC03 | 用固定预期内容/GUID/相对路径验证 diff；与等内容短树比较 hash/冲突计划，覆盖 CRLF 和版本头；计划、持久 JSON、枚举、Unity 参数无特殊前缀。DOS/UNC 转换/根/点段/空值/设备拒绝及非 Windows 透传；已前缀幂等和枚举 root 边界。网络卷不可用时仅格式验证，不冒称实网 |
| AC04 | 无变化、父变更、双方冲突、KeepChild/AcceptParent、过期计划拒绝；预览前后原始 byte 指纹覆盖 source/live/旧基线/部署记录、目录 meta 与 GUID。不能仅用归一化 hash 判断只读；成功后本次 stage 消失 |
| AC05 | 注入复制中途/版本头读取/三方读取错误；核对首要对象/InnerException/堆栈及具体路径。Windows 锁文件造成清理失败：有主异常时保留主异常+warning/Data+残留；无主异常预览抛清理错误；提交后清理失败报告已提交。owner 创建/写失败不能无声残留。释放测试锁后夹具只清自己的残留 |
| AC06 | 嵌套和实际并发 owned scope 保留对方；标记缺失/损坏、同名占用、无权限、root/父级/子项链接（含文件链接）拒绝并不越界；无权限或链接创建环境受限要如实记录受阻，不代填通过。模拟中断残留下一次操作不清旧目录；共享 Temp 无关链接/残留不触发全局扫描。临时 root 精确边界与诊断断言 |
| AC07 | 在临时项目长路径应用提交、至少一个目标落位后的注入失败、目录/文件/.meta 备份恢复；用原始指纹确认 live/记录/旧基线/GUID 恢复、无未知删除，另设合法的长事务目标夹具使备份后缀路径 >260（不强求同一个 <=235 源路径样例满足这点）。补备份清理失败/回滚失败路径报告，并回跑相关短路径事务用例。带深层模板技能文件的应用验证 UPM Snapshot/Hash/准备缺口，不能仅验证不含 skill 的 .asset |
| AC08 | 唯一自动编译入口 Unity MCP，重新枚举并核对框架工程根；主动 compile=request、确认本轮编译/域重载与新程序集已加载后读 Console，再运行具名相关测试。记录环境、请求/完成、过滤、passed/failed/skipped 和证据。不得把旧空 Console 或 idle 当证据；不操作 Windward、不提交/发版/推送/改真实快照或 PackageCache |

建议过滤：EmberTemplatePatchEditTests 的新增用例和原相关预览/冲突/stale-plan 用例；EmberTemplateTransactionEditTests 中复制/meta/commit/rollback/link 的相关方法；新增文件系统/owned scope 用例；UPM 安装器只选直接受改方法的用例。以实际 method 列表写入 implementation.md，不默认跑整个程序集。其他系统结果只能补充，不替代 Windows/策略关闭结果。测试跳过、编译失败、必需运行环境缺失均保持相应 AC 待验证，不能标本地修复完成。

MCP 断连按 unity-validation.md：先查待编译变更/是否已有未决请求；无法送达时立即提示用户手动触发编译，之后每30秒恢复查询最多10次，不重复未决操作，不用 BatchMode/dotnet/日志轮询代替。

## 8. 完成、回退与正式归档

实施只做本地交付，已有授权无需再审批。不提交、发版、推送、升级 Windward、改消费源资源/PackageCache/真实基线或模板快照，不全量/长时测试。失败时在本任务隔离夹具恢复，实际工作区的代码回退以任务备份逐项处理，不用 git reset/checkout 覆盖用户修改；消费端无需迁移持久格式，旧计划仍按原指纹失效规则处理。

必要本地验证通过后，实施对话直接将有效需求/方案、真实改动、逐项 AC 自检、证据与限制归入 docs/tasks/20261111-patch-preview-long-paths/，生成 consumer-validation-prompt.md。该提示词独立可用，写明未发布及实际改动身份、接入须另有授权、普通读取前提、只读完整预览步骤/预期/源指纹保护、结果格式；不能默认 Windward 已升级或授权真实应用。明确“本地交付完成；消费端尚未验证”，不等待回传、不伪称独立验收。

正式文档/证据能独立读取且不依赖临时目录后才标完成；精确核对任务目录绝对路径、ID、归属、无 symlink/junction 和未知文件，仅清已归档本任务临时资料。必要本地验证或归档失败则保留未完成状态和证据，不因两阶段例外跳过要求。


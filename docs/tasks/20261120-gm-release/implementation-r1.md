# R1：维护完成，发布准备受目录句柄阻塞

2026-11-20，Asia/Shanghai。沿用完整模式、原实施聊天与主聊天授权。**本轮正常回交，但R1目标未全部完成，不能派最终发布。** 未commit/tag/push；用户发布授权继续有效，不需重复审批。唯一实际发布阻塞为R1-B01；bundle直接检查的换行限制另列CHECK-N01。

## 逐项状态

| AC | 本轮结果 | 证据/剩余工作 |
| --- | --- | --- |
| MA01 | 通过 | GMCenter四个region恢复内部参数/生命周期/内部方法/外部方法；去掉region标题后的源码与进场一致，无运行改动 |
| MA02 | 通过 | Apply/Inspect共用唯一运行回执写入器；目录`.utmp/ember-ui/gm-control-center/`，UTC时间+GUID+操作名文件；公开ReceiptDirectory/LastReceiptPath，窗口制作/检查结果显示路径。连续两次Inspect九目标绑定相同、路径不同；未调用Apply重做视觉 |
| MA03 | 通过 | 主动MCP编译、域重载、已加载新MVID及最终Console空；final-compile-request/editor-final/final-loaded-state.json |
| TP01 | 通过 | 正式SaveTemplate保留完整base描述，915文件；显式minor Bump到0.12.0；正式DeclareFrameworkVersion到0.27.0，活动记录一致；未重新加载覆盖Assets |
| TP02 | **部分/阻塞** | 正式脚本Check后升包0.27.0，一次实际Client.Resolve后PackageInfo更新；base+lane同步完成，五派生待完成。三个正式事务被目录Move拒绝并回滚，另外两个启动融合尚未进行 |
| RC01 | **部分** | 新CHANGELOG Unreleased段、docs/dev/release-0.27.0.md真实草稿已写；release_metadata.py预览按预期拒绝旧兼容声明，没有执行--write或造假JSON；当前版本指针/Dependencies待谱系完成后更新，历史发布不变 |
| RC02 | **部分** | 160条明确路径已选择性暂存；shared-binaries和shared-font-config通过；bundle直接-Check失败限制与替代溯源检查见CHECK-N01。尚非完整可发布候选 |
| PR01 | 通过 | 原字体字节恢复且不暂存，ProjectSettings未改，始终base编辑态，未加载/切换/绕过弹窗，旧任务证据未覆盖，消费未升级 |
| PB01 | 后续阶段 | 受当前阶段边界及R1-B01阻塞，未commit/tag/push；必须准备完成→主聊天独立验收→P1 |

## R1-B01：Unity持有派生模板目录句柄，正式事务无法移动原Assets目录

已成功：base0.12.0，hash `cad30f499213dc7c946e32ae45dfffc7`；lane-defense-2d0.1.6，hash `1d718d86736e081a29fc142ab62f194c`，父base0.12.0/兼容0.27.0。**续接不重复Bump这两个已封存结果。**

source3d-2p5d、ui-game、visual-novel在`ApplyParentSync`的正式事务将`Assets`移到`Assets.ember-backup~`时抛IOException/AccessDenied；不是CanApply冲突未决，也不是MCP断连。失败后旧metadata与磁盘hash一致，未留staging/backup目录；未直接修快照。platformer/topdown尚未执行同步或加载，避免在当前目录事务已阻塞时继续扩大现场。

一次有界只读NtQuerySystemInformation/GetFinalPathNameByHandle诊断，限定已由MCP核对的Unity PID **28440**（工程/版本6000.5.4f1一致），扫描3997个该进程句柄、87个disk句柄，约0.045秒，发现：

| 句柄 | Unity持有的目录（仓库相对路径） |
| --- | --- |
| 9480 | Packages/com.ember/Templates~/visual-novel/Assets/GameResource/Resources/Config/Narrative/LastLight/Chapters |
| 10732 | Packages/com.ember/Templates~/source3d-2p5d/Assets/GameResource/Resources/UI/Module/SceneFlow/Prefabs |
| 12348 | Packages/com.ember/Templates~/ui-game/Assets/Game/Module/PlayerData |
| 13584 | Packages/com.ember/Templates~/source3d-2p5d/Assets/Game |

均为disk/directory句柄、GrantedAccess1048705。没有终止进程、强制关闭句柄或扫描无关文件内容。随后通过反射确认当前Unity公开API存在`AssetDatabase.ReleaseCachedFileHandles()`，正式调用并GC/WaitForPendingFinalizers；只复查上述四个已知句柄，仍有效且仍指同一目录。因此没有在条件不变时继续重试。

此前只读检查：目录无ReadOnly、ACL未见差异；两个失败目录全树DELETE访问探针无失败；Restart Manager在普通用户环境查询3713个相关文件未报告占用者（它未能解释目录锁）；暂停自动刷新后正式重试仍失败，finally已AllowAutoRefresh。GC放在一次事务前也无效。上述不是“没有锁”的结论，后续Unity目录句柄诊断才提供具体占用者。

曾尝试将一次ui-game事务排到delayCall，编辑器迟迟未执行。已根据唯一动态模块`data-000002B804553190`明确移除该回调，取消数1；之后才执行其它事务。最终本轮MCPDynamicCode pendingCallbacks=0，不存在重启后迟到重复Bump。

**建议人工恢复：** 用户保留Unity未保存内容后，关闭并重新打开此工程，以释放上述Unity进程的原生目录句柄；恢复MCP后先核对项目、活动base0.12.0、两个已成功封存结果及磁盘hash，确认旧进程句柄已消失，再从fresh plan继续正式父同步。若重启后立刻重现，保留新的占用路径/来源，交维护处理；不要为发布手写快照或强关内核句柄。

关键证据：`sync-*-error*.json/txt`、`unity-directory-handle-query.json`、`known-handles-after-cache-release.json`、`restart-manager-lock-query-unsandboxed.json`、`template-existing-file-differences.json`、`templates-final-validation.json`。

## 六派生审阅与续接选择

六个`plan-*.json`及`parent-sync-review.json`已完整落盘。Binding冲突仅生成时间戳差异，可AcceptParent；文件夹.meta/小说字体GUID及自有设置保持KeepChild，不覆盖小说皮肤。lane正式结果已保存；其它三个失败事务选择记录/原始plan保留。

platformer与topdown的GameInit冲突不能只KeepChild或AcceptParent：前者漏InitializeUI，后者丢白盒/玩家配置、试玩存储及过场接线。已在任务证据`prepared-project-merges/<id>/GameInitState.cs`准备各加一行InitializeUI的可审阅候选，**未写入快照/当前base项目**。恢复后必须正式Load到Assets→保留子业务并补接线→主动MCP编译→Save/Bump；加载前告知目标和Skill/场景/未保存确认弹窗，保留人工确认，最终正式返回base。自有设置页和小说Loading/字体皮肤保留为模板覆盖，本轮不强行套base新布局；GM新增代码/资产及启动接线需要完整继承。

七个模板通过正式资产/.meta/GUID校验，actualHash=contentHash=versionedContentHash，父快照actualHash=各自parentContentHash。`ValidateTemplateGraph()`当前返回[]，**该结构检查不等于都追上最新父模板**：五个仍指base0.11.1/兼容0.26.0，正是release_metadata.py拒绝生成的原因。不得用空graph冒充TP02通过。

## CHECK-N01：bundle字节换行检查限制

技能源和bundle未改，记录sourceCommit仍`fcc06dcb65404036eb2c6908fcd68379f3306239`。正式`build-ai-skill-bundle.ps1 -Check`直接执行报`Bundle source differs`。独立核对34个文件：现有bundle原始字节全部匹配manifest；HEAD源与记录sourceCommit源逐字节一致；checkout/HEAD/sourceCommit/bundle全部在仅CRLF→LF归一化后文本相等。源码不存在语义差异，未重建bundle或伪造sourceCommit。

为尝试直接执行正式-Check，曾将28个源文本暂时对齐现有bundle原字节；脚本因这些换行在Git中被视作dirty而正确拒绝，未绕过该保护。finally恢复全部28个进场原字节，`.agents/skills`无Git改动。保留`bundle-check-canonical.txt`、备份及Python程序。此轮**没有把正式-Check写成通过**；字节/文本溯源证据在`bundle-provenance-audit.json`和`bundle-newline-equivalence.json`，满足release参考中要求的换行差异核对条件，主聊天续接时保留该限定，无需重建无变化技能源。

## 版本、编译、暂存与保护

包由upm-bump-version -Check预览后正式0.26.0→0.27.0。PackageInfo滞后时一次实际Resolve；首次探针误把void返回赋给var，仅探针编译失败未送达Resolve，纠正后只送达一次。最终业务MVID `26a2edc8-4c5e-45c5-bcd0-710c80cf6f0e`，Editor MVID `58e99bc9-23f3-49b3-8881-4dbd816761db`，主动编译/域重载完成，最终Console0。过程中有MCP WebSocket重连警告，不宣称全程零警告。没有dotnet/BatchMode或全量/长时测试。

候选160路径见`candidate-paths.json`/NUL pathspec；仅两维护相关源码、base文档、base/lane正式封存、活动记录、包版本、CHANGELOG及新发布草稿。初始index为空，未清空无关暂存。已通过`git diff --cached --check`、shared-binaries `--revision : --checkout`、shared-font-config `--revision :`；未commit/tag/push。由于R1未完成，这些暂存是**部分候选**，不可直接发布。

字体DefaultLightSans进场及最终SHA256均`14C9718D55D832593C689643CD752A277E0915FE418BB71F8F85C883653C0E4D`，只留未暂存原修改。正式Save会复制原字节，没有内置字体规范化；故本轮已备份原缓存，Save期间暂用HEAD干净源字体，之后精确恢复。没有把缓存收进模板或源提交。ProjectSettings/QualitySettings和EditorSettings保持原字节；当前base0.12.0/FrameworkScene，非Play/非编译/非更新，原DisableDomainReload保持。未改偏好或GameView；未切模板，无用户草稿被覆盖。

下一步由主聊天处理R1-B01人工恢复并派续接R2，完成剩余正式同步/两启动合并、release_metadata预览+write及当前指针、候选增量检查，然后安排独立验收，最后才派P1。任务不归档、不标完成。所有本页简写证据位于[evidence/r1/](../../../.utmp/ember-task-archives/20261120-gm-release/materials/evidence/r1/)。

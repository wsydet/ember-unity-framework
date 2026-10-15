# 0.19.0 发布推进记录

## 续跑验证完成（2026-10-15）

此前阻塞已解除。Unity MCP 返回框架 0.19.0、非编译、scriptCompilationFailed=false、Console 0 errors。三个派生模板逐一正式查询均 Synced，实际 hash、封存 hash 和父快照一致；ValidateTemplateGraph 无问题、编辑记录未过期。没有重复 Bump、同步或加载模板。证据位于 platformer-combat-evidence/2026-10-15-release。

发布定向 EditMode job b3b24dd64544476faef44f0246128b43：2/2 Passed、0 失败/跳过，5.7387787 秒；SavedLayoutCopiesWithoutOverwritingOrPlacingNewNodes 与 CombatBuiltinHasIndependentStructureAndCompleteThreeHitPresentation 通过。测试后默认配置和玩家窗口仍与基线一致，Options=1/Enabled=true、FrameworkScene dirty=false。其他本批分阶段验证继续按原证据范围引用，T03 剩余延期决定不变。

已更新 0.19.0 CHANGELOG、发布说明、release/manifest 与当前版本指针。下方候选与阻塞描述保留为历史。发布仅纳入包内交付、说明及选定验证证据；工作区保护用补丁/旧源码/资源备份继续留在本地，不批量提交开发副本或字体运行缓存。

候选 index 的共享二进制（含 autocrlf 两种检出）与字体配置检查通过；审阅发布声明时补齐新增 ember-platformer-add-attack 的模板技能清单。工作区 Assets、字体缓存、发现副本没有进入暂存区。发布 tag 与远端结果以 Git 中的 v0.19.0 和 main 指向为准。

2026-10-15，用户已授权保存并发布新版框架。当前为发布准备状态，尚未提交、创建 tag 或推送；不得将本文件视为远端发布证据。

## 范围与版本

框架新增公开 CopySavedLayout 编辑器接口，依仓库向后兼容新能力的 minor 规则选择 0.19.0。platformer-2d 在开发期间已升至 0.4.0，本次封存最新补验修复为 0.4.1。与上一发布 0.3.1 相比是模板 minor 升级，消费项目不能当作普通 patch 覆盖。默认方案和用户草稿保留，T03 剩余项延期真实项目。

当前 main/HEAD 与远端 main、v0.18.0 peeled commit 均为 e3aad088de167dd9ef4835869d32a2eeaec55023；远端无 v0.19.0。原暂存区为空。工作区大量 Assets/发现目录变更属于当前平台开发副本，字体 asset 是运行缓存；这些不随发布批量暂存。状态基线保留于 .utmp/release-0.19.0-before-status.txt。

## 已执行

- 版本脚本 -Check 后将 package.json 更新至 0.19.0，通过 PackageManager.Client.Resolve 确认 Unity 识别 0.19.0。
- 正式 SaveTemplate(platformer-2d) 返回 1317 文件，补充模板描述，随后 patch Bump 到 0.4.1。contentHash/versionedContentHash 同为 373437cccbe9f4ba6e93359da2c8b2fd。没有加载或切换模板。
- 根 base 通过 DeclareFrameworkVersion 声明 0.19.0；三个派生父同步预览均 CanApply=true、HasConflicts=false、WillChangeChildContent=false。
- 批量执行三次 ApplyParentSync 时 MCP 返回 success=false/message=null/data=null（约 30 秒），按规则立即停止后续 Unity 调用。之后只读磁盘显示四模板兼容声明均为 0.19.0，各内容 hash/封存 hash 一致、原父级版本和内容 hash 不变；编辑记录为 platformer-2d 0.4.1。磁盘结果不能代替正式完成返回及同步后实时校验。
- 保存前实时基线：原仓库、非 Play、Options=1、默认 PlayerDefaultScheme GUID a2d7100fc47f54b4282b5be389d735b8，玩家窗口为空。失败后未继续查询，不能声称已做保存后实时草稿检查。
- 攻击验证沿用本批开发的实际证据：P9 600 PlayMode、114 Editor、47 存储；后续调校 8 项及最终故障 1 项/回滚 2 项；左右六帧真实渲染。各阶段重叠不累加为独立测试总数，本次发布准备未重跑。

## 续跑入口与剩余门槛

静态补充：共享二进制 HEAD/checkout 及 autocrlf 两种检出通过；字体配置通过。公共 bundle 在 .utmp/release-0.19.0-check/skills 的隔离验证副本检查通过（11 项/32 文件）；28 份源与 bundle 的差异已逐一证明仅 CRLF/LF，bundle 原始字节符合 manifest，仅在隔离副本对齐后执行官方 -Check，原仓库源和 bundle 未改。隔离副本有本地验证用提交，不是框架发布提交。release/manifest-0.19.0.json 已由正式脚本生成，status 明确为发布候选；恢复后应根据实际完成结果修订声明与说明，再更新 CHANGELOG/当前指针和候选 index。

1. 重连 Unity 后先确认 0.19.0、非 Play、Options=1、默认和草稿；核对各模板实际 hash、父快照、父指针、编辑记录与正式父同步状态。磁盘已推进，不重复 Bump/同步，不重载项目。
2. 补齐编译结果和适配当前变更的必要回归。此前本批已通过的证据可复用，不能用历史 0.18.0 发布测试替代。
3. 公共 Skill 源未修改；原目录检查被模板发现副本的变更阻挡，应在干净只读验证副本按原脚本验证 bundle 和 sourceCommit，不删除用户文件或伪造 bundle。共享二进制/字体检查与候选 index 检查须完成。
4. 审阅并选择性暂存：框架布局复制接口及 meta、platformer-2d 快照、四模板 metadata、包版本、CHANGELOG、发布声明、当前版本指针及本批说明/证据。Assets 开发副本、发现副本、字体缓存和本机测试产物不混入。
5. 完成检查后提交、annotated v0.19.0、仅推送 main 与该 tag，ls-remote 核对两者指向。发布授权已经给出，无需再询问。

本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；如仍有报错，请提供首条错误及完整堆栈。T03 剩余项、真实消费迁移、发行构建仍未验证；不因版本准备而将 P9 全项放行。

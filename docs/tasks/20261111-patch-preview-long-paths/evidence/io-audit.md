# 本轮 IO 链路与边界自检

2026-11-11；实施自检，非独立验收。有效方案 design.md r2，R01–R08 不变。

| 可达链路 | 最终 IO 与保护 |
|---|---|
| BuildTemplatePatchPlan → ReadPatchDeploymentData/ValidateDeploymentBaseline/PatchProjectHash | 普通逻辑路径；File Exists/ReadAllText 及 CaptureDirectory 全部通过 UPM 文件系统门面 |
| Temp/EmberPatchPreview-完整GUID~/Old、Incoming、Local → CopyPatchAssets | 保留原前缀；CopyDirectory 的属性/枚举/目录创建/Copy/meta ReadLines 均适配；目录与文件链接检查 |
| 暂存版本头 → RewriteVersionMarkerStrict | 字节读写严格传播；无标记、二进制、不需改写仍按原规则返回 false；其他旧 RewriteVersionMarker 调用保留 best-effort 并 warning |
| CaptureDirectory/ReadMetaProperties/ComputeTemplateFileContentHash/三方比较 | 适配 ReadLines/ReadAllBytes/属性/枚举；枚举先按 native root 分隔边界校验，再还原普通路径；MD5、CRLF、版本头、GUID、排序与冲突逻辑不改 |
| CommitTemplatePatch → ApplyPatchChanges/GUID复核/PrepareSkills/AddDeploymentBaseline/Record | Core 各 partial 自己声明 alias；技能 Snapshot/SHA256/Length/目录/Prepare/校验与暂存 defaults 读取、时间戳均适配 |
| CommitPreparedTargets/ParentSync/部署/基线恢复 | Copy/Move/Delete/属性统一边界，raw MD5 门面保持原算法；原备份后缀/目标顺序与逆序回滚不改；同进程提交重入拒绝；备份已存在拒绝且不清未知备份 |
| 异常与回滚 | IO 异常对象原样 throw，Data 附普通路径及操作；Move 附两端；回滚逐目标累积 AggregateException，保留每个目标与备份路径；主异常与清理次异常分开 |
| Owned scope | project/Temp 父链检查，无共享 Temp 全树扫描；GUID+CreateNew owner，持有 lease 到清理前校验；完整归属未建立不递归删；只清本次精确 root，保留占用/并发/中断残留 |
| 清理结果 | 预览成功但清理失败抛 IOException；已有主异常附 Data 和 warning；应用已提交后 warning 明确“已提交”并返回原 count；已提交备份清理失败保留备份并 warning |

FileSystem 从 Core 移至 UPMManager/Editor，meta GUID 仍为 46d8056309f9abf4c8afc65b92f83c91；Core 无重复定义。asmdef 均未改，依赖仍 Core→UPM。

特殊前缀仅在门面内部最终 System.IO/FileInfo/FileStream 边界；NativePath 为 internal。业务计划、持久记录、相对路径与 AssetDatabase/外部工具参数未接入特殊前缀。非 Windows 原样透传，当前机器仅实测 Windows。UNC 只做格式/映射校验，无网络共享实测。

链接检查面向可信本地项目及合作进程；检查与使用之间仍存在竞态，不提供抵御同用户恶意替换的沙箱保证。文件 symlink 创建权限不可用（Win32 1314），该实际安全场景仍待验证；目录 junction、父链/root/子目录、安全拒绝、共享 Temp 无关链接及真实权限拒绝已实测。不能把目录 junction 通过替代文件 symlink 通过。

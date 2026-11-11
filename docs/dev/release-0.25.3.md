# Ember 0.25.3

2026-11-12。基于 v0.25.2 的 Windows 模板增量预览/事务 IO 修复补丁。发布目标为 origin/main 与 annotated `v0.25.3`；实际发布提交以远端 tag 的 peeled commit 为准。

## 交付与取舍

保留项目 Temp/完整 GUID 暂存结构，在最终 System.IO 边界统一处理 Windows 长路径；门面位于 UPMManager.Editor，沿 Core→UPM 现有依赖共享。补齐预览复制、meta、hash、版本头、三方比较、应用/共享事务、深层 Skill 准备校验和扩展 defaults 读取。特殊前缀不进入计划、相对路径、记录、AssetDatabase 或外部工具参数；hash、GUID、冲突、stale-plan 和事务顺序语义不变。

补强 owner lease、精确清理边界和链接拒绝；未建立归属不递归删，不扫描历史残留。首要错误保留，次级清理错误附诊断；预览清理失败明确失败，已提交应用清理失败明确报告已提交及残留，避免调用者重试；回滚错误保留所有恢复路径，拒绝未知备份和同进程提交重入。

正式成果与逐项自检见[任务归档](../tasks/20261111-patch-preview-long-paths/README.md)，消费端另见[只读验证提示词](../tasks/20261111-patch-preview-long-paths/consumer-validation-prompt.md)。

## 验证与限制

同一实施任务通过 Unity MCP 主动编译、域重载与新程序集 MVID 验证，最终 Console 无错误/警告。定向74个独立case：73通过、0失败、1跳过；最终修改的3例另补跑3/3通过，不与前组相加。WindowsEditor6000.5.4f1、CLR4.0.30319.42000、NTFS、LongPathsEnabled=0：源/live meta227/235普通读取，暂存Old/Incoming/Local289/294/291完整预览成功；短树249/254/251普通读取并与长树计划/hash一致；272字符事务备份提交及落位后故障回滚通过。深层Skill、owner/实际并发嵌套、ACL拒绝、目录junction、主次异常及已提交后清理失败场景已覆盖。

**文件 symlink 场景仍环境受限、待验证，未记通过**（Win32 1314）。用户2026-11-12接受此限制，不阻塞本地收束与发布；补测方法与步骤留在正式归档。UNC只做格式验证，实网、其他OS/Unity版本、全量玩法/Player构建及消费项目尚未验证。本轮是实施/发布自检，不是独立验收。

## 发布候选检查

- 从远端main/v0.25.2的提交 `4eaf344167d679583e17c9f683ef8aea2d5bc95e` 建立干净候选；16个修复文件逐项匹配实施测试源码，差异仅Git文本CRLF处理。原分支的大量本地快照、字体缓存、规则/流程/技能改动均未纳入。
- 发布阶段再次经MCP主动编译，finished=`1794418442729`，domain_reload_after=`1794418462114`；实际PackageInfo和框架API均为0.25.3，已加载程序集与上轮验证实现一致，非编译/非更新，Console错误/警告均0。
- 七模板实际/封存hash及六份父快照/指针重新只读核对一致，图校验无问题；候选模板与v0.25.2逐文件不变，未保存或重复Bump。
- staged index共享二进制及core.autocrlf=false/true干净检出检查通过；字体配置的候选工作区/index检查通过；技能bundle检查12技能/34文件通过。技能源与bundle不变，保留真实sourceCommit，不重建或纳入其他技能草稿。
- 57项依赖仅com.ember标签变化；其他依赖、第三方与可选包目标、模板谱系及bundle sourceCommit逐项相同。全部69份原临时任务文件的原始字节已在正式归档及Git index中逐项校验；任务局部attributes保留证据/备份原始换行，不改公共规则。
- 正式链接检查无临时目录依赖；原任务精确临时目录已清理。提交前空白检查通过；实际源码Git blob指纹与文本LF核对说明已嵌入正式消费端提示词。

## 模板、依赖与接入

七个模板内容、封存版本、父快照/指针及0.25.0兼容声明不变；第三方依赖和12项公共技能bundle保持v0.25.2内容。本版发布声明与manifest仅推进框架tag；发布检查另记归档evidence。

已有消费项目经另行授权后通过 Ember/UPM Manager 升级至 v0.25.3，不手改 manifest/lock，不修改 PackageCache。确认实际解析路径/版本、编译及修复源码后，仅做只读预览并核对源/live/记录/旧基线原始字节与GUID。框架包升级不授权真实模板应用，不自动升级 Windward 或其他消费项目。

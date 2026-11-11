# 20261111-patch-preview-long-paths：Windows 暂存长路径修复

**本地交付完成；消费端尚未验证。** 完成日期：2026-11-12，Asia/Shanghai。仅方案、实施两阶段，同对话检查为实施自检，不冒充独立验收。

用户2026-11-12明确接受文件 symlink 创建权限受限（Win32 1314）的待验证项，允许直接收束并随后发布；该项仍未通过。目录 junction、安全权限拒绝、实际并发/嵌套及其他相关故障验证已通过。UNC仅格式验证，实网、其他OS及消费项目未测。

根因是可普通读取的源文件在项目Temp/完整GUID暂存副本中超过MAX_PATH。保留原目录结构，在UPM共享文件系统的最终IO边界支持Windows长路径，补齐Core/AISkills/UPM/defaults可达IO；保持hash/GUID/冲突/stale-plan/事务顺序；补owned scope与主次异常、已提交后清理失败的明确语义。

## 正式资料

- [有效需求及收束决定](requirements.md)、[方案r2及决策](design.md)。
- [实际交付、逐项R/AC自检与编译测试](implementation.md)、[正式交接及历史](handoff.md)。
- [消费端只读验证提示词](consumer-validation-prompt.md)：含独立背景、源码指纹、接入需另有授权、UPM核对、只读保护及回报格式。
- [IO链路自检](evidence/io-audit.md)、[源码身份](evidence/source-identity.json)、[最终原始源码快照](evidence/delivery-source/)、[修改前真实工作区备份](evidence/implementation-start/)。
- [具名过滤](evidence/test-filters.json)、[复跑74例](evidence/test2final.json)、[最终3例补跑](evidence/test3final.json)、[编译/域重载](evidence/compile4state.json)、[实际程序集MVID](evidence/compile4loaded.json)、[Console](evidence/consolefinal.json)。

验证环境：Unity6000.5.4f1 WindowsEditor，CLR4.0.30319.42000，Windows NT10.0.26200.0，NTFS，LongPathsEnabled=0。74个独立case最终73通过/0失败/1待验证；最后修改的3例补跑3/3通过，不能相加称77例。源/live meta227/235普通读取成功，Old/Incoming/Local暂存289/294/291；短树249/254/251且计划/hash一致；事务备份272提交与回滚通过。

## 已接受限制与补测

AC06文件symlink场景仍“环境受限、待验证”，不是通过。后续具备文件符号链接创建权限的WindowsEditor环境核对工程根、UPM、源码身份；通过Unity MCP主动编译、确认本轮加载及Console，再仅运行`Ember.UI.Tests.EmberTemplateFileSystemEditTests.Owned_LinksAreRejectedWithoutFollowingTargets("file")`（约1秒）。确认实际执行、未skipped，目标字节不变；记录结果。方法级过滤若包含其他3个参数case须如实记录。不得用junction或静态检查代替该场景，不为测试暗改全局策略。

## 归档与发布边界

原实施基线：`16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2`本地快照；修复16文件聚合SHA256为`84084d51b877aacabf644720ab149b07d7a2b5f4d241112436e64fad91e6ce75`。发布候选从v0.25.2建立，拟为v0.25.3，仅纳入本任务；模板、依赖、技能bundle及其他本地改动不进入发布。最终发布身份及检查另见[发布说明](../../dev/release-0.25.3.md)（发布准备阶段建立）。

原始阶段记录、提示词和evidence脚本保留为历史；它们含原临时路径的文字描述不是当前依赖或执行入口，正式记录及证据均在本目录独立承接。原始文档字节见evidence/archive-inputs。清理只针对已完整承接的本任务精确临时目录；检查结果见evidence/archive-manifest.json和evidence/cleanup.json。其他任务与备份保留。

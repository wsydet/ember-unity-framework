# Ember 增量预览 Windows 暂存长路径：消费端只读验证提示词（未完成交付草稿）

记录日期：2026-11-11，Asia/Shanghai。任务 `20261111-patch-preview-long-paths`。框架实施自检现有 74 个独立用例中 73 通过、1 个文件 symlink 安全场景因 Win32 1314 待验证；不能称本地交付完成或独立验收完成。消费端尚未验证。本提示词已包含必要背景和实际身份，复制到其他对话也不依赖框架临时目录。

请验证当前消费项目的 Ember 增量预览长路径问题。本次仅做只读核对与完整预览；**接入未发布框架改动、升级包、真实应用补丁、恢复/改写部署基线、覆盖源资源均须另外有用户明确授权**。此文本不授予这些操作，不允许修改 PackageCache。不要假设 Windward 或任何其他工程已升级，不默认目标工程名字或 MCP 实例 ID。

## 背景与实际未发布身份

用户曾报告：Windows LongPathsEnabled=0，源路径降到 235 字符，但项目 Temp/EmberPatchPreview-完整GUID~/Local 暂存达到 289，普通 meta ReadLines 在 MAX_PATH 边界失败。保留原 Temp/GUID 前缀，修复在最终 IO 边界统一使用 Windows 扩展路径；枚举恢复普通路径，不能让前缀进入计划/JSON/AssetDatabase/外部工具参数。预览源、live、旧部署基线、部署记录须保持原始字节、目录及 GUID 不变。

可接入的实际源码是框架开发仓库 `ember-unity-framework` 的本地工作区，UPM 包 `com.ember` / package.json `0.25.2`，**未提交、未发布**；不是已发布 0.25.2 tag 的承诺。基底分支 `codex/local-snapshot-20261111-162819`，HEAD `16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2`。下方 16 个文件的原始 SHA256 标识本次工作区；不能只凭版本号/HEAD 当作已接入。Core 不再定义 EmberTemplateFileSystem，位置为 UPMManager/Editor，meta GUID `46d8056309f9abf4c8afc65b92f83c91`，依赖仍 Core→UPM。

文件聚合 SHA256：`84084d51b877aacabf644720ab149b07d7a2b5f4d241112436e64fad91e6ce75`。算法：以下相对包路径按 ordinal 排序，每行 `path<TAB>raw-file-sha256<LF>`，UTF-8 无 BOM 拼接后 SHA256。逐文件指纹表在文末。行尾转换后指纹不同也不能无说明地判为相同源码，应核对实际差异和重新确认身份。

本地已实测：Unity 6000.5.4f1 WindowsEditor，CLR 4.0.30319.42000，Windows NT 10.0.26200.0，NTFS，LongPathsEnabled=0。源 meta=227、live meta=235 普通读取成功；完整预览 Old/Incoming/Local meta=289/294/291 成功；等内容短树=249/254/251 普通读取成功且 hash/计划等价；事务目标258/备份272提交和落位后回滚通过。深层 Skill、扩展 defaults、原字节只读、owner/并发/目录 junction/权限/清理异常已验证。文件 symlink、UNC 实网、其他 OS/Unity 版本未实际通过；不外推为普遍支持。

## 执行前提与授权边界

1. 先读目标工程自身 AGENTS.md/CLAUDE.md，按 UPM 实际 PackageInfo.resolvedPath 读取包内 unity-validation.md。重新列出 Unity MCP 实例，只绑定用户指定消费工程，并核对返回 projectRoot。不得顺手操作其他实例。
2. 记录 Git/未提交状态、Unity 完整版本、Windows/CLR、卷格式、注册表 LongPathsEnabled（只读，不改策略），以及正式活动部署模板 ID/版本/旧基线身份。不能按场景或残留目录猜模板。
3. 核对当前 UPM 来源、resolvedPath、package.json 版本及上述源码逐文件指纹。如果尚未接入此改动，报告“当前包不含该工作区修复，需要另行授权接入”，停止依赖修复的结论。不得编辑缓存、自动改 manifest、覆盖包或以老 0.25.2 号代替指纹核对。
4. 若已另行获授权并完成接入，必须通过 Unity MCP 主动编译，确认本轮编译/域重载和新程序集 MVID/门面已加载，再读 Console。请求成功/idle/空 Console 单独不足。断连遵循 30 秒、最多10次恢复，不重复未决请求，不用 BatchMode/dotnet/日志轮询替代。
5. 只读预览前确保没有未保存场景/dirty 资源；存在时停止并让用户在本验证外自行决定如何处理，不能为运行预览自动保存/覆盖草稿。核对待预览模板是已封存且同 ID、同 major.minor、patch 向前；条件不满足记录前置不满足，不手改版本/hash/基线绕过。

## 只读预览与指纹保护

1. 使用原有长路径复现树；列出源与 live `.asset` 及配对 `.meta` 的普通绝对路径和字符长度，确认均 <=235，并实际用不带特殊前缀的 System.IO.ReadAllBytes/ReadAllText 成功。组件应是多层合法名字，不以超长单文件名制造另一类失败。若源本身普通读取失败，按前置不满足记录，不能将其当作本修复失败或改名测试通过。
2. 在预览前建立独立原始快照：源模板受管资源、live受管资源、全部目录与目录 meta/GUID、部署记录、旧基线文件及 metadata。指纹必须来自原始字节与目录清单；不能只用会归一化换行/版本头的模板 hash。保护其他本地文件，并记录 Temp 当前操作与残留，不扫删历史残留。
3. 用正式项目中心只读“增量预览”（或经授权的只读 BuildTemplatePatchPlan 诊断）运行完整预览；不得点应用/完整部署。记录旧结构 project/Temp/EmberPatchPreview-32位GUID~/Old、Incoming、Local 的逻辑路径长度，确保复现路径的暂存确实 >260。不要缩短前缀/工程路径来掩盖问题。不要把扩展前缀传给 AssetDatabase。
4. 预期完整计划生成成功，差异/冲突仍与旧基线、新模板、本地三方内容对应；GUID、hash、排序、版本头和 CRLF 语义保持。对确有本地修改的文件保留冲突，不能为“成功”消除冲突。计划/日志中的业务路径和 JSON 不应含特殊前缀；枚举输出为普通路径。
5. 预览结束后重新取同样的原始字节和目录快照并精确比较，源/live/记录/旧基线/GUID 均不变；仅本次预览 stage 清理成功。其他并发操作、中断残留与历史失败目录不能被清掉。若预览/清理异常，保存首要异常对象类型、完整堆栈、具体普通路径、Data/InnerException、残留路径及次级清理诊断，不自动删未知残留。
6. 不在消费项目试应用/故障回滚或修改源来制造差异。需要新增夹具、接入不同包、应用验证或处理残留，均先在本轮已有授权之外另行取得明确授权。

## 回报格式

请回报：工程根与正式模板身份；UPM来源/版本/解析路径和逐文件指纹匹配结果；Unity/CLR/Windows/文件系统/策略；源/live asset+meta 的普通读取结果及长度；Old/Incoming/Local暂存长度；完整预览成功/失败、实际差异/冲突；预览前后原字节/目录/GUID/记录/基线比较结果；stage清理和旧残留保护；Console及编译/域重载/MVID证据；首要/次级异常及残留；明确 passed/failed/pending/not-applicable。缺少前置、跳过或未运行不能记通过。消费端回报是补充验证，不意味着本提示词授权了真实应用，也不补造框架未通过的文件 symlink 安全结果。

## 实际源码逐文件指纹

以下路径均相对实际 `com.ember` 包根。

| 包内路径 | 原始 SHA256 |
|---|---|
| `Core/Editor/EmberExtensionProject.Identity.cs` | `e008ca359bc0318537609170acec8634f81d7ec7b9b6db00983db6c4f3b18701` |
| `Core/Editor/EmberExtensionProject.cs` | `653f2419511d83f08b27730140167308df35fcdb767d8da908f24f6093838eff` |
| `Core/Editor/EmberProjectSetup.AISkills.cs` | `f4e280b0324667091fca0a73667b931cea15616b771c4df2d32fa74f6447246b` |
| `Core/Editor/EmberProjectSetup.Patch.cs` | `871da8f36392068cf79514950d7611b1d3aaffd69fcc26c7743fa1034b6ea7d8` |
| `Core/Editor/EmberProjectSetup.cs` | `1a547ee716a1f49525d24fb6db8e4168ac0d447a4ec82be2875091b546630c89` |
| `Core/Editor/EmberTemplateInheritanceEngine.cs` | `8f2e33d37c4d033d6fe3d06c765e027224d67c848566bfc14689c9d6aa129159` |
| `Core/Editor/EmberTemplateTemporaryDirectory.cs` | `6422b6daf53275e14c368718b9dcc8ec9346aa036756f0aaca8994800253f41c` |
| `Core/Editor/EmberTemplateTemporaryDirectory.cs.meta` | `ec30619fbcc7ab845ce5ac596630c9656d364c783b22dc8cd337a9fdb54117fa` |
| `Core/Editor/EmberTemplateTransaction.cs` | `9aa20a6ffb146f9b254087a2ec16fe5e7f1bd32ca83ffd20e1e0f5bd69a7f74a` |
| `Tests/EditMode/EmberTemplateFileSystemEditTests.cs` | `6ed183fde588ff67e3e045bab0f262c450cef2fd0e1c36e76e520da306909b84` |
| `Tests/EditMode/EmberTemplateFileSystemEditTests.cs.meta` | `493ca94917bc3c7c2aa01245c97c63429cd23707eca31db751ab18ec549d92a9` |
| `Tests/EditMode/EmberTemplatePatchEditTests.cs` | `4ded0d89902a44dce2534b4241ec0a74a1cf5bcf444da370a0969a69ac8b18af` |
| `UPMManager/Editor/EmberAISkillInstaller.Templates.cs` | `fca7da1b39979a8f5c0f5c909c84b6cd12f0e6ac66c3410f01a1adf81efbace6` |
| `UPMManager/Editor/EmberAISkillInstaller.cs` | `41b0ee2fdf8fe83a8580561bfef0fb0cdb18b3f3548e67663aa0c190e511b7f8` |
| `UPMManager/Editor/EmberTemplateFileSystem.cs` | `89718fb2ef2ee90e9b92743f0ccec02bea506af63212ad7836e1740f0a1c0de3` |
| `UPMManager/Editor/EmberTemplateFileSystem.cs.meta` | `692f695fce5f5812447cd346e2953f17d51b95b299ee2f8f2adad48150d830c3` |

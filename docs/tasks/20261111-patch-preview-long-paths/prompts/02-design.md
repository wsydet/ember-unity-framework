接续任务 20261111-patch-preview-long-paths：Ember 增量预览 Windows 暂存长路径修复。本对话只负责方案，不实施。

工作区：C:/Users/wuyu/My/ember-unity-framework
任务目录：.utmp/ember-workflow/20261111-patch-preview-long-paths/

先读 AGENTS.md、CLAUDE.md，通过 UPM 实际解析位置读取 Ember 的 Documentation~/maintenance/ai-workflow.md、ai-handoff-template.md，再读本任务 handoff.md 最新有效状态、requirements.md、implementation.md 的未完成事实。先独立核对需求及实际源码，再审查 design.md 和现有代码草稿，不能把草稿当作已批准方案或已完成修复。

本任务专属工作方式由用户明确指定，优先于公共流程默认值：只用方案、实施两份阶段提示词。实施对话完成本地修复与必要验证后直接正式归档收束，附消费端验证提示词；不另开独立验收、不等待消费端回传、不要求再回入口。不要修改通用 skill、公共流程或仓库规则来实现这个例外。

问题背景：Windward 位于 C:/Users/wuyu/My/Windward，用户报告安装 Ember 0.25.2、LongPathsEnabled=0。源 Assets 最长路径已缩至 235；旧 Temp/EmberPatchPreview-<32位GUID>~/Local 增加约 56 字符，残留最长达到 289；meta ReadLines 曾在路径存在时抛 DirectoryNotFoundException。以框架当前源码为准，不操作 Windward。入口已实测当前 Unity 6000.5.4f1 WindowsEditor 在 LongPathsEnabled=0 下普通读取 335 字符文件失败，扩展路径 IO 成功；这只是探针，不是修复验收证据。

方案须覆盖：
1. 暂存创建、CopyPatchAssets、EmberTemplateTransaction.CopyDirectory、文件枚举、meta 读取、内容哈希、三方差异、失败清理，以及复用这些实现的补丁应用、备份和回滚。
2. 比较缩短暂存与统一长路径兼容方案，明确 Windows/Unity/.NET 支持边界及需要的本地运行时验证；仅缩短前缀不能被描述为消除长路径边界。
3. Windows 特殊前缀仅在文件系统访问边界使用，不进入 AssetDatabase、相对路径、计划或持久记录，不改变 hash、路径比较、GUID、只读预览和冲突语义。
4. 首要异常与具体路径保留；清理失败不覆盖首要异常、不静默隐藏残留。临时目录归属、隔离、权限、链接检查和安全清理边界，包含并发/中断情形。
5. 定向回归计划：源 .asset/.meta 可普通读取、旧预览暂存后超过 260；完整预览差异正确、源内容及 GUID 不变、成功/异常清理、短路径、应用/事务回滚。优先本地 Windows 且 LongPathsEnabled=0，不能以其他系统结果替代。

工作区已有入口误提前生成的实现和测试草稿，详见 handoff.md/implementation.md；只读评估其必要性与缺陷，不回滚或修改产品代码，也不执行运行时探针、编译或测试。两份字体资源改动原本属于用户，保留；接手时重新核对实际 Git 状态，不依赖历史文件清单或旧实例 ID。

产出：写入有效 design.md，记录采用/放弃的草稿及原因、准确改动范围、实施步骤与验收映射；更新 handoff.md 当前状态和 prompts/03-implementation.md，使实施可直接按已有授权执行。保留需求 R01–R08、AC01–AC08，不降低标准。方案完成后交付实施提示词链接并结束本阶段，不自行跨到实施。

授权限制：仅本地修复与直接相关验证；不提交、发版、推送、升级 Windward、修改任何消费项目 PackageCache、消费源资源、真实部署基线或模板快照；不运行全量或预计长时间测试。已有实施授权应传递给实施对话，无须再次审批。

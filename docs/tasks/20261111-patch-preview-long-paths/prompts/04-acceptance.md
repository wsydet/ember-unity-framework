接续任务 20261111-patch-preview-long-paths，本对话负责独立验收。
工作区 C:/Users/wuyu/My/ember-unity-framework，任务目录 .utmp/ember-workflow/20261111-patch-preview-long-paths/。
先完整读取 AGENTS.md、CLAUDE.md、实际 UPM 解析包内 Documentation~/maintenance/ai-workflow.md，以及该任务 handoff.md；读取 requirements.md、实际代码和测试，再读 design.md、implementation.md 与 evidence。
核对当前工作区与有效基线。范围：Windows 增量预览暂存增长超限及共享补丁事务 IO；保留 GUID/hash/三方比较/只读语义，不能改消费源资源、模板快照或基线。已有授权为本地实现与相关验证；不发版、推送、提交、升级 Windward 或改 PackageCache，不运行全量或长时测试。
前置：方案阶段需有效需求；实施需可执行方案；独立验收需实施与当前真实证据。缺失不得推定完成；独立验收必须另一个对话，本入口自检不冒充验收。
本轮完成：独立逐项验证 AC01–AC08，写 acceptance.md、问题复现与 handoff.md；失败回实施，满足后回入口归档。保留两份非本任务字体资源改动及其他工作区内容。测试前读 unity-validation.md，通过 Unity MCP 只操作框架实例，主动请求编译并确认本轮域重载后读取 Console。完成更新同一任务记录和下一阶段提示词。

当前补充：入口曾误提前实施，用户已要求恢复分阶段；先读 handoff.md r2 和 implementation.md 的未完成状态。现有 design.md 与代码均为待审查草稿，不把入口提前操作视作阶段完成。

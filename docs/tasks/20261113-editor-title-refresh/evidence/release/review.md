# 0.25.5 发布前复审

2026-11-13，用户追加授权：再审查本轮标题刷新修复，确认后提交、推送、发布；保护其他修改，Windward 不升级。

完整复审了 EditorMainWindowTitle、EmberTitleSnapshot/EmberTitleProcess、全部 8 个测试 case、生产与测试 asmdef、meta/GUID、solution 新引用，以及前轮编译/测试/周期采样证据。没有发现本次需要阻止发布的代码问题，未改动已验证的生产 C#。

- 主线程刷新仅调度 Task、接收已完成结果并格式化标题；descriptor 复用标题，无 Git 启动或同步管道读取。内部 API 与 native fallback 不重复查询。
- 单次在途、合并 dirty 请求、项目/来源代次推进和 Dispose 后拒绝发布均闭合；旧请求结束前不会再启动下一请求。
- 进程退出、stdout/stderr EOF 共享期限，后台有界回收；取消前/中/启动竞争均有终止信号和取消复查。错误不逐帧输出或忙循环。
- 成功空分支显示 HEAD，失败显示 ? 并保留诊断。最近 Git 根同时识别目录和文件，子工程/盘符根保留格式。
- embedded 与 consumer 显示读取分别来自正式编辑/活动部署文件；缺失或损坏不跨模式回退，不向资格判断提供缓存结果。
- Basic.Editor 不依赖 Core.Editor 或 UPMManager.Editor，未增加反向引用。其他 Runtime、门禁、模板和产品实现不在本次范围。
- 手动/Play/projectChanged/周期 Hook 保留；域重载/退出取消并解绑本类事件。Windows 原生路径保持，macOS 后台执行保留已有平台限制。
- 8 项定向测试与 16.107 秒自然周期证据仍匹配源码清单。发布版本将再次经对应 MCP 请求编译、确认已加载 0.25.5 并复跑同一类 8 项，不扩大为全量。
- 已知验证限制保留：macOS、原生窗口实际标题视觉、真实项目/UPM 来源切换和消费端四条件未实测；它们不被写为通过。正式 worktree .git 文件格式由 separate-git-dir 夹具覆盖，未创建新工作区。

发布基线采用远端 main/annotated v0.25.4 peeled commit c5a2fe90b45ea5343a66f086c2663d35c6f6eac6；当前开发分支 16cff3d 的 WIP 历史不能直接推送为 main。复用已经存在且干净的发布 checkout，单独建立 codex/release-0.25.5，选择性导入 10 个标题源码/meta/solution 文件、当前任务文档和必要发布资料。

候选范围审计额外发现主工作区 solution 相对 v0.25.4 含早先 WIP 项目引用增删/排序；已将候选 solution 改为发布基线加本轮 Basic.Editor.Tests 一行，主工作区原 solution 字节不变。其余 9 个源码/meta/asmdef 字节与最终验证清单一致。比较包内 3591 个 C#/asmdef 后，仅运行 Editor 中早先两份规则同步实现/测试与候选不同，已确认不提供标题/缓存 API，未收入发布。不是标题实现缺陷，没有为此修改已验证的生产 C#。

发布审计脚本初次读取含中文路径的保留清单时因 Windows 默认 GBK 解码失败，已显式改为 UTF-8 后通过；该问题只影响取证脚本，不属于 Unity 编译或产品测试。259 份既有文件原始 SHA256 保持；另外 6 份仅按授权更新包版本或发布指针，其他文案保留。

其他工作区原始路径及 SHA256 已在 preservation-before.json 保存；提交前后检查不被覆盖。模板、技能 bundle、三方依赖沿用 v0.25.4；不重建技能、不修改版本/hash/父快照，也不升级 Windward。最终提交范围与远端身份由本目录其他证据承接。

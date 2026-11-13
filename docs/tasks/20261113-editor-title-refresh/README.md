# 窗口标题周期刷新：第二批框架 Editor 修复

2026-11-13（上海时间），任务 `20261113-editor-title-refresh`。接续 Windward 性能任务，按用户本轮指令在当前对话实施并自检，不创建工作区、子代理或新对话。独立验收及消费端复测仍由后续阶段承接。**实施阶段最初交付为本地修改；用户随后授权复审、提交、推送并发布 0.25.5，进度见文末。整体性能问题保持开放。**

## 基线与范围

- 当前 checkout：`codex/local-snapshot-20261111-162819`，HEAD `16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2`。包自报 0.25.4、UPM source=Embedded，含较多既有未提交及未跟踪改动；未将工作区标识描述为本轮已发布版本。
- 框架 Unity：6000.5.4f1，MCP `ember-unity-framework@b23e9b768e85ed30`。Windward 实例未用于执行本轮修改、编译或测试。
- 正式编辑身份为 `topdown-2d 0.1.2`。本轮不修改正式身份、扩展绑定、模板内容/hash，显示缓存不提供任何资格授权。
- [起始状态](evidence/status-before.txt)保留首批缓存、模板事务、文档、字体与发布记录等现有修改。本轮目标文件开始时无本地差异，另保留修改前副本。Unity 因新增测试程序集自动在根 `.slnx` 加入一行项目引用，该生成差异列入本轮交付。
- 来源为 Windward，归属为框架 Basic.Editor 修复；不包含 Runtime、房间驻留或产品侧补丁，不适用 UnityFarm 来源分流。

来源：[Windward 原性能回执](../../../../Windward/docs/design/planning/editor-performance-20261113/implementation.md)、[0.25.4 消费复测](../../../../Windward/docs/design/planning/editor-performance-20261113/post-upgrade/README.md)。已读取该目录的 baseline-summary、title-refresh-hotspot、slowest-frame-diagnostics，保留原样本及其限制。约 149–171ms 的标题刷新为既有热点；旧 0.25.3 也有约 121–126ms 命中，不能定性为已证实的 0.25.4 回归。嵌套 BuildMainWindowTitle 耗时不与外层相加。

## 实现与边界

1. `EditorMainWindowTitle.RefreshTitlePeriodically` 保留 5 秒周期，只调度后台刷新并接收已完成快照；主线程没有 Git 启动、输出读取或进程退出等待。`ApplyTitle` 从快照构建一次完整标题；Unity descriptor 只复制 `_currentTitle`，原生 fallback 使用同一字符串。
2. `EmberTitleSnapshot` 持有单次在途 Task。周期和手动请求合并；项目/来源变化及失效推进代次并取消旧请求，旧请求实际结束前不再启动下一请求。迟到结果不得覆盖新项目/新代次，Dispose 后 Tick 不再发布。失败显示 `?`，错误保存在 `LastQueryError`，下个周期重试，不逐帧重试或刷 Console。
3. 后台冻结工程路径与包来源，查找最近的 `.git` 目录或文件，读取选定正式身份文件，再执行 `git branch --show-current`。成功空输出显示 `HEAD`，非零退出、无 Git、缺失可执行文件、超时或取消显示 `?` 并保留错误。子目录工程、`.git` 文件及文件系统根路径格式都有直接断言。
4. embedded 只读取 `EmberEditingTemplate.json.templateId`；所有 consumer 来源只读取 `EmberDeployedTemplates.json.activeTemplateId`。缺失或损坏不回退到另一身份文件。后台只读取 JSON 文本，主线程发布时解析显示字段；不调用 Core 资格缓存或修改门禁。
5. `EmberTitleProcess` 同时排空 stdout/stderr，保留内容有上限。退出与两路 EOF 共享 2 秒期限；超时/取消终止进程，后台最多再等待 250ms 回收，最终释放 Process。域重载/退出取消会立即发终止信号，主线程不等待进程。返回值错误可诊断，正常失败不会形成未观察异常或忙循环。
6. 保留手动菜单、Play、projectChanged、内部 API Hook 及失败后的原生 fallback。域重载/退出解绑本类订阅并取消请求。Windows 保留原 SetWindowText 路径；macOS osascript 改为单次在途后台有界调用，不再在主线程 WaitForExit，仍受原 AppleScript 权限和平台限制。

程序集方向已核对：Basic.Editor 仅引用 Basic.Runtime/TMP/UI，Core.Editor 引用 UPMManager.Editor/Core.Runtime/Basic.Runtime/Odin；Basic 没有 Core 或 UPM Editor 引用。Core 的 `EmberEditorMetadataCache` 不可被 Basic 反向消费。UPM 的下载/更新查询类为内部业务专用类，启动方式也不满足本次后台启动要求，因此不增加反向引用或扩大通用重构；独立的小型显示缓存和进程执行器留在 Basic.Editor，未变更已有生产程序集依赖。

## 验证索引

定向范围仅为 `Ember.Basic.Editor.Tests.EmberTitleSnapshotTests`，位于新增 `Ember.Basic.Editor.Tests` 程序集，8 个 NUnit test cases。覆盖 descriptor 连续 100 次回调不增请求、单次在途与合并、项目/同项目失效代次、Dispose 后迟到结果、身份模式/缺失/损坏、标题字段及盘符根、真实隔离 Git 分支切换/detached/子目录/`.git` 文件、无 Git/失败/超时/取消、两路大量输出排空和显示长度上限。

`.git` 文件夹具使用临时 `clone --separate-git-dir`，不创建当前仓库工作区或 worktree；它覆盖 `.git` 文件的查询边界，不声称执行了真实消费工程/worktree 切换。测试只在系统 Temp 随机目录写入隔离 Git 和身份文件，结束后清理，不改当前工程 Git、身份或用户存档。

首轮 6/8，通过后发现两项夹具假设错误：当前 Unity descriptor 没有无参构造函数；clone 后保留分支名。已按实际构造函数创建 descriptor，并显式在 clone 内 detached 再断言，保留[首轮失败结果](evidence/testResult1.json)。第二轮 8/8、0 skipped、用例耗时约 4.89 秒，见[第二轮结果](evidence/testResult2.json)。最后的盘符根子目录修正后另请求编译并在同一范围复跑，以最终证据为准。

编译仅经框架 MCP 主动请求；每轮核对对应 compile_finished/domain_reload_after、非编译/非更新、实际程序集 MVID，再读 Console。域重载中短暂断连按 30 秒间隔恢复，未重放结果不明的请求，未用 BatchMode/dotnet/Editor.log 替代。最终编译、测试与自然周期采样结果见本节下方最终证据补记。

Windows 原生 fallback 已直接调用，同一标题传入且请求计数不变，见[调用证据](evidence/nativeFallback.json)；Mono 的 Process.MainWindowTitle 返回不可用值，因此未据此声称验证了原生窗口实际显示。macOS 未运行；真实项目/UPM 来源切换、退出 Editor、消费端 Play 与人工视觉验收未执行。实际域重载及加载新代码已由 MCP 编译证据覆盖，迟到/退出所有权由确定性测试和运行中取消诊断覆盖。

### 最终证据补记（上海 14:44）

- 最后[编译请求](evidence/compileRequest4.json)、[编译/域重载状态](evidence/compileState4.json)、[加载新程序集](evidence/compileLoaded4.json)确认本轮代码已加载，scriptCompilationFailed=false，非编译/非更新。Basic.Editor MVID `a846df9e-3b58-41c7-b056-bcd9561e3b31`，Tests MVID `e020f8de-fe41-4d99-ab14-ab7db6110da8`。
- 最终[测试结果](evidence/testResult3.json)：**8/8 通过，0 failed，0 skipped，用例耗时 5.595 秒**。本轮未运行全量或长期测试。
- [16.107 秒自然周期 CPU 采样](evidence/periodic-profile.json)、[统计](evidence/periodic-summary.json)及[采样程序文本](evidence/capture.cs.txt)：148 个框架编辑器帧，148 个 RefreshTitlePeriodically 样本，主线程回调最大 **0.3191ms**、p95 **0.0047ms**，>1ms 为 0；请求计数 13→16，覆盖 **3 次周期查询调度**。BuildMainWindowTitle 嵌套样本最大 **0.0475ms**，不与外层相加。录制期间没有 MCP 调用。p95 包括无到期工作的 update 回调，不能作为单次到期刷新 p95；最大值同时覆盖实际调度/发布工作。
- 采样为当前框架工程、EditMode、编辑器后台的有限验证，148 帧足以观察三个周期；没有将它包装成 Windward 1200+ CPU 帧、4K 前台四条件或游戏 FPS 对比。证据与源码/重复回调测试共同支持：已移除命中的周期主线程同步 Git 长任务；消费端仍须按原环境复测。
- [运行中取消诊断](evidence/live-cancellation.json)：启动后台长等待子进程后 250ms 发出取消，约 **278.14ms** 返回 `git cancelled`；执行 continuation 在线程 432，发起的 Editor 主线程为 1。该结果属于盘符根修正前的相同进程执行器，后续仅修改标题路径格式化及其断言，执行器源文件未变。
- [最终现场](evidence/finalState.json)：FrameworkScene、无 dirty、非 Play/非编译/非更新、title 正确、LastQueryError=null，Profiler/Driver/profileEditor/deep/CPU 均恢复 false；正式 topdown 身份仍可识别，原未签收的 trial 扩展仍安全拒绝。
- [最终 Console](evidence/finalConsole.json)：0 error，2 条 MCP WebSocket 断连警告，未清空或归因为标题修复。它们来自编译/域重载期间连接关闭；每批均最终恢复并确认编译完成。
- [本轮实际差异](evidence/task-only.diff)、[源码指纹](evidence/source-manifest.json)和[收尾状态](evidence/status-after.txt)。差异仅包含标题实现、标题缓存/执行器、新测试及 meta、Unity 自动生成的 `.slnx` 引用；既有首批和其他任务差异未纳入。
- 收尾复核 10 份源码/程序集/元文件及生成 solution 的 SHA256 均与清单一致；[27 项文档链接检查](evidence/link-check.json)无缺失。已按授权在 Windward 原 `implementation.md` 末尾追加[第二批回执](consumer-receipt-append.md)，仅追加文档链接及验证边界。

## 消费接续与残余问题

详见[消费接续](consumer-handoff.md)。本轮不提交、推送、发布，不更改模板快照/hash，不升级 Windward，不改 PackageCache、manifest/lock 或复制产品补丁。正式版本发布后才经 Ember/UPM Manager 接入；如扩展内容 hash 变化，先提供真实预览并独立授权绑定，再按原四条件有限复测。

上述提交/发布限制为实施阶段原授权；用户后续明确授权复审后提交、推送和发布新版。Windward 不升级及模板/绑定边界继续有效。

普通入口打开项目中心的 46.74ms inclusive 重绘样本（其中 GC.Collect 约 21.76ms）继续记录为独立 GUI/分配尾帧问题；二者不相加，本轮不实施窗口重构。没有 GPU 数据，不据此判断 GPU 瓶颈。首次身份/扩展缓存方案与门禁保持原职责，整体性能任务继续开放。

## 0.25.5 发布复审与候选（2026-11-13）

用户明确授权再审查本轮修复，确认后提交、推送和发布；保护其他已有改动，Windward 暂不升级。[复审记录](evidence/release/review.md)未发现阻止发布的问题，生产 C# 未再修改。[0.25.5 发布说明](../../dev/release-0.25.5.md)。

发布版本经 MCP [主动编译](evidence/release/releaseCompileRequest.json)、[编译/域重载完成](evidence/release/releaseCompileState.json)、[解析版本与新代码加载](evidence/release/releaseLoaded.json)，PackageInfo 与框架 API 均为 0.25.5；[定向测试](evidence/release/releaseTestResult.json)8/8、0 skipped、4.9006 秒。Basic.Editor MVID 保持 a846df9e-3b58-41c7-b056-bcd9561e3b31，与确定性编译未改源码相符；完成事件和域重载后的加载结果共同证明本轮验证，未靠 MVID 必须变化推断。

[实际模板与谱系](evidence/release/releaseTemplates.json)七项内容/封存 hash 和六份父快照/指针全部一致，graphIssues=[]；[Console](evidence/release/releaseConsole.json)为 0 编译错误和两条 MCP WebSocket 断连警告。未执行全量、长期测试或消费升级。

从远端 v0.25.4 的 c5a2fe90b45ea5343a66f086c2663d35c6f6eac6 复用既有干净发布 checkout，分支 codex/release-0.25.5；仅导入 10 个本轮源码/meta/solution 文件、本任务记录及必要发布资料。原开发分支不 reset、不切换，其他改动不暂存进该发布提交。候选审计和二进制/字体/bundle 检查结果由本目录 evidence/release 保存，最终远端身份在发布完成后另行核对。

[候选审计](evidence/release/candidate-audit.json)确认 9 个标题源码/meta/asmdef 与已验证字节一致；solution 仅从 v0.25.4 增加标题测试项目引用，排除了主工作区较早 WIP 的其他自动生成差异。3591 个包内 C#/asmdef 对照仅有两份既有规则同步文件不同，它们不提供标题或缓存 API，未收入候选。259 份既有文件 SHA256 不变，另 6 份为本轮明确更新的版本/发布指针。

候选 staged [共享二进制](evidence/release/binaries-staged.txt)、[字体配置](evidence/release/fonts-staged.txt)、[12 技能/34 文件 bundle](evidence/release/bundle-staged.txt)全部通过；二进制同时覆盖 core.autocrlf=false/true 干净检出。仅静态完整性，不替代 Unity 字体渲染或 Play 测试，本轮也未变字体或重建 bundle。

# 构建调度与原生 Assert 复核

日期：2026-10-17。承接[Odin路径修正](06-odin-build-path-fix.md)中的6条Unity原生Assert。本轮修改归属platformer-2d的Editor验收工具；没有新增Ember运行时能力、修改MCP包或Odin补丁，也没有屏蔽任何诊断。

## 证据与因果边界

历史诊断为GetApplication().MayUpdate及includeAutoRepaint条件断言，发生于MCP execute_code同步调用PlayerColdAcceptance.Build/BuildPipeline.BuildPlayer期间。当前Console已无这些历史条目，不能冒充本轮重新取得历史完整堆栈。

只读核对当前MCP源码：TransportCommandDispatcher同时从EditorApplication.update与UnitySynchronizationContext.Post的Pump处理命令。因而“主线程执行”不能保证处于相同编辑器阶段。MCP自己的BuildRunner.ScheduleOnNextUpdate也先返回请求，再从一次性update回调构建。

本轮三组同场景、同define、StandaloneWindows64、Mono、BuildOptions.None对照：

| 对照 | 结果 | Assert证据 |
|---|---|---|
| 修改前原Build入口，在execute_code中同步执行 | Succeeded，0错误、6警告；GUID `3a1653d89b3e444398bf9d3b2347a237` | 构建期间日志订阅未收到Assert/Error/Exception，结束后Console错误查询0 |
| 修改后一次性Editor update执行 | Succeeded，0错误、0警告；GUID `fd7c701cf3af407da13cbdfdfb5ad701` | build-diagnostics.txt为asserts=0 errors=0，Console错误/警告查询0 |
| 显式SynchronizationContext.Post执行同配置BuildPlayer | Succeeded，0错误、0警告 | 独立diagnostics.txt为asserts=0；entry-stack.txt确认经过UnitySynchronizationContext.Exec/ExecuteTasks |

因此，本轮**未复现历史6条Assert，不能证实同步上下文是唯一根因，也不能宣称已修复Unity内部断言本身**。已修正的是验收工具直接依赖MCP请求执行时机的问题，新调度路径实测无Assert。警告数量受增量构建缓存影响；本轮0警告不代表已修复此前内容/可选资源警告。本轮没有修改资源或放宽运行断言来消除诊断。

## 实际实现

维护源：`Assets/Game/State/Editor/PlayerColdAcceptance.cs`。

- Build保持原签名，检查条件后排队并立即返回；下一次EditorApplication.update执行前先退订，防止构建内部泵消息重入。
- 拒绝Queued/Building期间的重复请求、已有Player构建、编译和导入状态；执行时再次验证模板、模块、Play及场景保存状态。
- 异常通过EmberDebug.LogError报告，保留失败状态和报告；没有吞掉异常使其看起来成功。
- 构建目录写入build-report.txt、build-entry-stack.txt及build-diagnostics.txt。后者通过Application.logMessageReceived记录Warning/Error/Assert/Exception原文与堆栈，计数Assert和Error/Exception；finally退订，不改变Unity日志设置。
- 已实测入口返回Queued，同一请求内第二次调用被拒绝；实际构建入口堆栈仅经ExecuteOnUpdate→EditorApplication.Internal_CallUpdateFunctions，不经过MCP请求栈。

调用示例与完成判定维护于模板[LevelAcceptance](../../../Assets/Game/Documentation/platformer-2d/LevelAcceptance.md)。Build返回不等于完成；等BuildStatus终态、报告及诊断文件后再运行Player。排队期间被域重载打断不视为成功，应就绪后重新发起。

## 新构建两进程实测

两进程使用上述scheduled构建，共用新的隔离磁盘库，先退出save进程再启动resume：

| 阶段 | 时间 | PID | boot | 断言 | 退出码 |
|---|---|---:|---|---:|---:|
| save | 15:16:25 | 33808 | `07d42de0f59546228f808159d692f417` | 16/16 | 0 |
| resume | 15:16:57 | 13508 | `9b39be8dd26c418882dacc3d2857fcb7` | 19/19 | 0 |

两份报告均PASSED、editor=false、development=false、errors=[]，buildGuid一致。覆盖正式Main→A→B→组合保存退出、B关磁盘合法检查点恢复、项目/世界在玩家启动前恢复、死亡恢复一次提交及相机绑定/重置/Brain输出/卸载。运行器和断言未变。没有实现Windward业务，也未验证IL2CPP或其他平台。

证据根：`.utmp/build-scheduling-20261017/`。baseline、scheduled、context-probe分别存放三组对照；scheduled/evidence保存报告、Player日志及相机PNG。

- report-save.json SHA256：`45272ae2d129a0923370c52d8981b87fb7e99d58f49e5f4053cbebc6f2046976`。
- report-resume.json SHA256：`5b01d1cf793687597a75444193d38c05a58325d05c6ea3f67246836c35183032`。

构建自动改写的ProjectSettings批处理配置、URP预筛选配置及两个Odin NoEditor .meta已按本轮基线恢复，并保留构建后备份。Odin维护源仍只有前轮DLL/PDB及Maintenance~改动；未操作PackageCache。

## 后续定位边界

若再次出现MayUpdate断言，请保留本入口输出的build-entry-stack.txt、build-diagnostics.txt及当时Console完整堆栈，核对是否仍从Editor update执行及是否存在模态窗口/导入活动。本轮没有可稳定复现的触发条件，不能将“未复现”替换成确定根因。

## 正式保存状态

2026-10-17 15:19:57通过项目中心SaveTemplate普通保存1395文件，15:20:04复查0错误、0差异、1条已保存未封存警告；Console错误/警告查询0。未Bump、提交、推送或发布。

- 框架实际0.19.2，platformer-2d仍0.4.1，声明frameworkVersion仍0.19.0，父base仍0.8.3。
- contentHash及编辑记录：`35505b28730529dd56ab61a475994056`。
- versionedContentHash：`373437cccbe9f4ba6e93359da2c8b2fd`，parentContentHash：`1270ce6dfcae3c01d050c09ecfd60337`，均未改变。
- Odin仍通过前轮兄弟仓库本地源消费，版本4.0.2，未发布修正版；本轮未改其维护代码。

模板内容来自Assets维护源，经正式保存生成快照。当前工程可用新Build入口；其他消费项目不会自动升级，后续仍需各自正常的模板接入与第三方依赖接线。

后续完成CleanBuildCache非增量发行构建：0错误、18条历史内容警告复现、0 Assert，两进程16/16及19/19通过。详见[非增量构建复核](08-clean-build-acceptance.md)。本节此前的零警告仅为增量构建观察，不代表这些警告已修复；最新保存状态见08。

# 非增量发行构建复核

日期：2026-10-17。承接[构建调度验收](07-build-scheduling-acceptance.md)，专门排除增量Player构建缓存对警告观察的影响。不删除Library、不重新部署模板，不修改或屏蔽诊断。

## 入口与非增量证据

Assets维护源`Game/State/Editor/PlayerColdAcceptance.cs`新增`Build(string outputDirectory, bool cleanBuild)`重载；原单参数入口仍使用普通构建。两者保持一次性EditorApplication.update调度。cleanBuild=true传入BuildOptions.CleanBuildCache，并输出BuildReport各步骤消息到build-messages.txt。

实际调用：`PlayerColdAcceptance.Build(absoluteFreshOutputDirectory, true)`，输出根`.utmp/clean-build-20261017/player`。构建入口堆栈确认ExecuteOnUpdate→EditorApplication.Internal_CallUpdateFunctions。报告原始options数值128，与当前Unity的CleanBuildCache数值相等；后端Mono2x、Development=false，耗时2分15.833秒。GUID为`5d5b769d6ab947e186ab1a3325231dfc`。

注意：本Unity版本的summary.options.ToString()同时显示若干历史枚举名称（包括Il2CPP）；不能据该字符串断言换了后端。独立实测PlayerSettings后端为Mono2x，产物有MonoBleedingEdge及Managed程序集，原始选项值确为128。核对结果另存build-verification.txt。这里是Unity提供的非增量Player构建，不是全部资产重新导入。

## 构建及诊断结果

BuildReport：**Succeeded，0错误、18警告**。原始诊断记录：**asserts=0 errors=0**，记录18条Warning；结束后Console错误查询0。没有清空或关闭诊断。

| 历史诊断 | 本次结果 |
|---|---|
| UniRx过时Object查找API | 3条复现（CS0618） |
| EmberSceneField及EUITransitionBlock未使用字段 | 3条复现（CS0414） |
| Plaque向量截断、ShowTextInstance整数除/模、NodeBlurEffect逗号表达式 | 10条Shader警告复现 |
| Feel可选PostProcessing Profile/Volume缺失脚本 | 2条复现 |
| 当时MCP WebSocket断线/重连 | 本次未出现 |
| 历史6条MayUpdate/includeAutoRepaint原生Assert | 本次未复现 |
| Odin PluginImporter路径错误 | 本次未复现，沿用已修正本地维护源 |

此前18条内容警告全部仍在，本轮没有修改这些代码/资源，不能把上轮增量构建0警告当作已修复。新调度入口在非增量构建下未出现Assert，增加了该路径的验证证据；仍不能倒推历史6条Assert的确切原因，07中的因果边界继续成立。

证据包括build-report.txt、build-verification.txt、build-entry-stack.txt、build-diagnostics.txt、build-messages.txt；均保存在上述输出根。BuildReport的“Compile scripts”和“Packaging assets”警告原文可直接逐项核对。

## 两进程验收

同一非增量产物使用全新隔离存档库player/evidence，保存进程退出后才启动续玩进程；未改运行器或放宽断言。

| 阶段 | 时间 | PID | boot | 断言 | 退出码 |
|---|---|---:|---|---:|---:|
| save | 15:26:29 | 127008 | `b93ff9bd40054202b3f651665f5bc153` | 16/16 | 0 |
| resume | 15:27:08 | 72100 | `e86ec9c9dc884d2b9a8bc79a902acb02` | 19/19 | 0 |

两报告均PASSED、editor=false、development=false、errors=[]、GUID与本次构建一致。覆盖正式Main→A→B→组合保存退出；重启进程后从磁盘恢复B关合法检查点、项目和世界状态；死亡恢复一次提交，以及相机加载、恢复绑定、Brain输出和卸载。

- report-save.json SHA256：`0d84f8c073bd61ce74d5d4e91c2362a23a3cfa239c413c9d69a19101dcc034f0`。
- report-resume.json SHA256：`af761dc177b16a1b1a14d340fd3ba4426a6092b3eea7987ad5be6561a1f14945`。

本轮范围仍为Windows64 Mono发行验收，未测试IL2CPP、其他平台或Windward专属玩法。没有把同进程恢复算作跨进程续玩。

## 维护与保存

新增重载及报告输出只修改Assets中的验收脚本和LevelAcceptance使用说明，通过项目中心于15:27:48普通保存1395文件。当前contentHash与编辑记录均为`b0d2b8954453f5eefeb8f094854c24eb`；保存后项目校验文本在`.utmp/clean-build-20261017/project-validation-after-save.txt`。

15:28:19保存后校验为0错误、0差异、1条已保存未版本封存提示，未执行提示中的版本封存操作。

框架0.19.2、platformer-2d 0.4.1、声明frameworkVersion 0.19.0、父base 0.8.3均保持；versionedContentHash仍`373437cccbe9f4ba6e93359da2c8b2fd`、parentContentHash仍`1270ce6dfcae3c01d050c09ecfd60337`。没有Bump、提交、推送或发布。

保留原有修改；仅将本轮构建自动产生的URP预筛选及两个Odin NoEditor导入器meta恢复至基线，项目设置hash与本轮开始一致。Odin修正版仍通过兄弟仓库本地路径消费，未改其补丁、未直接修改PackageCache。

后续已按正确维护源修正上述18条警告，并重新完成CleanBuildCache构建（0错误、0警告、0 Assert）及两进程验收；详见[警告修正交付](09-build-warning-fixes.md)。本页18条复现结果保留为修正前对照。

# 编辑器正式身份与扩展查询缓存：方案 B 实施回执

2026-11-13（上海时间），任务 `20261113-editor-identity-cache`。本轮完成框架 Editor 实现、对应 Unity 编译/域重载和直接相关验证。**实施阶段交付时尚未发布；Windward 尚未接入，整体低帧率问题仍待消费端同环境复测。发布记录见文末。**

来源：[Windward 方案](evidence/consumer-reference/design.md.txt)、[原实施回执](evidence/consumer-reference/implementation.md.txt)、[框架修复提示词](evidence/consumer-reference/framework-fix-prompt.md.txt)。用户已采用 B，本轮没有重复方案审批，也没有调用方案入口重开任务。

## 环境、范围与既有工作

- 使用现有框架 checkout：分支 `codex/local-snapshot-20261111-162819`，HEAD `16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2`，工作区包自报 0.25.3；这些标识不代表本修复已发布。
- Unity 6000.5.4f1，对应 MCP 实例 `ember-unity-framework@b23e9b768e85ed30`；未借用 Windward 实例验证框架。
- 正式 embedded 编辑身份为 `topdown-2d 0.1.2`。既有项目配置和 Assets 默认组合仍绑定 `0.1.1`，因此当前项目实际门禁拒绝这些可选扩展。未改写配置、重新绑定 hash 或切换模板。
- 保留原有扩展长路径 IO、模板事务、补丁预览、工作流文档、字体和发行草稿等修改。修改前副本位于 [evidence/before](evidence/before)，[已有差异](evidence/preexisting.diff)与[起始状态](evidence/status-before.txt)分别保留。任务外 16 份已跟踪差异与起始内容一致，见[保护核对](evidence/preexisting-preservation.json)。该核对不扩称对所有既有未跟踪文件建立了起始指纹。
- 实施阶段未暂存、提交、推送、tag、发布、保存/Bump/部署模板、修改 Templates~/ParentSnapshot~、版本/hash、消费端 PackageCache 或 manifest/lock。验证临时夹具均已清理，任务证据保留供审查。

## 实际实现

[仅本任务差异](evidence/task-only.diff)以任务开始时的真实文件为基线，包含 13 份已有 C# 文件的增量与 3 份新增 C# 及其 meta，共 19 个源码/元文件；不把原有长路径或事务修复算成本次成果。[最终源码指纹](evidence/source-manifest.json)用于核对本轮验证对应的内容。

1. `EmberEditorMetadataCache` 保存包身份、已解析 JSON、模板元数据和正式身份代次。资格查询仅持有模板 ID/版本和合法完整父链，embedded 只读编辑记录，consumer 只读活动部署记录；断链、循环、重复和无效身份拒绝资格。`GetTemplates` 返回深副本；显式模板操作仍即时读取元数据，不以展示快照替代操作检查。
2. `EmberExtensionProject.Cache` 在同一代次内复用配置和稳定绑定检查。`All`、定义依赖、解析结果集合均只读，`Load` 深复制配置、enabled 数组和各 binding。`CheckCompatibility` 按当前调用/同步检查批次求值，模块 `Func<bool>` 继续实时求值；不把动态条件长期冻结为 true。
3. 导入/projectChanged、事务提交及回滚、Save、定义注册/注销、包路径/版本/来源、模板记录/父链和内容输入变化联动失效，包括 `ContentHashes`。模块/scope/适配器登记和释放立即清除当前批次条件结果；旧登记实例不能注销新定义。
4. 同时保留文件通知和已知输入时间戳检测。当前 Unity 的 `FileSystemWatcher` 诊断没有收到变化事件，因此不能只依赖通知。周期约每 0.5 秒将冻结的已知路径列表交后台 stat；主线程只接收结果，不周期枚举源码树、读源码或重算 hash。已知目录时间戳也覆盖新增/删除文件和目录；缺失的部署记录、模板 metadata 和源码根同样登记，防止首次创建或恢复后一直复用失败快照。后台不调用 Unity API；异常收回缓存，查询拒绝旧 true。定向用例在 1.1 秒观察窗口确认配置及源码变化。此时间依赖编辑器正常调度，不是硬实时保证。
5. 条件菜单刷新、项目中心绘制、适配器列表以及试玩 Tick 复用同步检查批次。菜单执行、写操作、Start/Retry 和受控文档读取前重新核对当前输入；只读 Scene GUI 门禁同步复核身份/配置，源码失效由后台合并检测。同步完整源码 stat 保留在实际操作边界，有一次性成本。
6. Tick 的失效判断仍在 Play 早退之前，继续调用原 StopCurrent/owner 清理与 TryRestore 路径。Start 在保存依赖之后再次检查资格和适配器身份。未修改 Runtime、游戏业务、存档或模板 Assets。

## 编译与行为证据

最后一批 C# 通过 MCP 显式请求编译：[请求](evidence/compile6request.json)、[编译/域重载状态](evidence/compile6state.json)、[新程序集加载](evidence/compile6loaded.json)、[随后 Console](evidence/compile6console.json)。状态记录包含本轮 compile_started、compile_finished 和 domain_reload_after，非编译/非更新/无待重载；新后台 probe 字段确已加载。

- Core Editor MVID：`4b66644d-1921-49e0-a818-c1a3557aafba`，与起始 `f7a9767c-d956-4fb9-8a5d-f7263cf3c16f` 不同。
- Level Editor MVID：`f2959b0b-51e2-4412-83d8-eb61c33bd61d`；UI Tests MVID：`5c8cea56-d3d4-4776-acdb-bf6fa2b4eb8b`。
- 域重载期间若状态请求未送达，按 30 秒间隔重连同一实例；已提交的编译请求没有盲目重放。最终成功恢复并完成验证，未用 Editor.log、BatchMode 或 dotnet 替代。
- 最终 Console：error/warning 过滤下 0 条，未清空 Console，见[最终 Console](evidence/delivery-console.json)。首轮类型名冲突已修正；中间有两条既有测试序列化 warning，最终轮未再次产生。

定向 EditMode 过滤仅为以下四类，共 **33/33 通过、0 skipped，测试用例耗时 11.01 秒**，不含 runner 启停时间，见[最终测试原始结果](evidence/tests5.json)：

- `Ember.UI.Tests.EmberEditorIdentityCacheEditTests`（8 项）：同代次不重读/不重扫/不重算、深副本/只读集合、配置缺失后创建/外部改变/损坏/删除、动态兼容条件、定义注册/注销、签收配置/开关/身份与绑定、断链/循环、scope 禁用/Dispose、事务立即失效、源码 hash 变化；其中测试方法含参数或组合断言，以原始结果列出的 test case 为准。
- `Ember.UI.Tests.EmberTemplateIdentityEditTests`（15 项）：embedded/consumer、合法派生、缺失、未知、循环和重复身份。
- `Ember.Core.Fsm.Tests.EmberExtensionTests`（6 项）：依赖、兼容回调异常、未知/循环、关闭预览与基础设施。
- `Ember.LevelAuthoring.Tests.EmberLevelTrialProtocolTests`（4 项）：冻结配置/发现换绑、旧 owner、停止异常、失效 Tick 沿当前 owner 停止及恢复排队。

新增缓存类实际含 8 个测试方法；测试总数以原始报告为准。最早 31 项运行有 1 项源码失效失败，证据保留在 [tests1](evidence/tests1.json)。随后修正失效检测，并在已签收的内存预览测量中发现同步 stat 过慢，进一步移到后台；最终 33 项是在该最终代码上重跑的结果，未用中间通过结果代替最终验证。

另在 runner 外对已保存且无 dirty 的真实编辑现场做协议夹具恢复检查：[owner 恢复证据](evidence/owner-final-source.json)。旧请求 stops=0，失效当前 owner stops=1，恢复后 Active=false、恢复所有权=false，FrameworkScene 路径与 Selection 保持一致。没有进入 Play，没有替代产品 adapter 的实际启动/清理验收。

## 性能与缓存命中

此处为框架 Editor API 微基准，不是 Windward 游戏 FPS 或最终 Trial Tick/窗口 Profiler 验收。

| 最终工作区预热门禁/展示路径 | 次数 | p50 ms | p95 ms |
|---|---:|---:|---:|
| CurrentTemplateId | 1000 | 0.0001 | 0.0006 |
| IsTemplateActive | 1000 | 0.0001 | 0.0002 |
| IsEnabled | 1000 | 0.0124 | 0.0176 |
| 三项门禁检查批次 | 1000 | 0.0136 | 0.0180 |
| GetTemplates 深副本 | 1000 | 0.0017 | 0.0030 |
| ConditionalMenu.Refresh | 50 | 0.0147 | 0.0186 |

[最终微基准](evidence/microbenchmark-final-source.json)及[程序](evidence/microbenchmark.cs.txt)：预热后 reads=244、scans=27、builds=23、hashes=6，测量结束完全相同；冷重建单独记录 9.26ms。这些绝对累计数包含本轮测试，关注的是区间差值为零。项目当前配置版本不匹配，因此该表可选扩展走拒绝路径。

为覆盖启用态，另用 `CreateConfiguration` 生成**仅在内存中的待审阅绑定**并构建实际 prepared definitions，未 Save/Commit：[启用态预览与微基准](evidence/accepted-preview-final-source.json)。6 个扩展启用、Problems 为空，1000 次 prepared resolution 的 p50=0.0144ms、p95=0.0479ms，reads/scans/builds/hashes 均未增加。这验证了稳定绑定后的求解层，不等同于已修改真实项目配置或跑过启用态产品试玩。

完整已知源码同步 stat 的最终 p95=77.87ms，保留在操作边界；中间受环境影响曾测到约 140ms，正因此移出周期主线程路径。它不是稳态每秒成本，也不应被隐去。[后台版周期主线程检查](evidence/window-final-source.json)强制到期检查 100 次，p50=0.0889ms、p95=0.1646ms、max=0.299ms。项目中心测试窗口持续约 3.09 秒、793 次 update/repaint 请求，首次额外读取 1 份部署元数据，模板枚举、解析构建和 hash 重算增量均为 0；没有将 repaint 请求数视为实际绘制帧数或 FPS。诊断窗口已关闭。

## 消费接续、限制与待批准步骤

见[消费端接续说明](consumer-handoff.md)。当前不改变 Windward 基线结论：普通/试玩入口、中心开/关的四条件仍需正式升级后同环境前台复测。原始 inclusive 样本不相加，不从 API 微基准换算 FPS。

尚未覆盖：真实包来源切换/UPM 升级、实际模板切换/部署、真实启用态 adapter 的 Play/Retry/退出、Windward 玩家邻房/死亡/继续以及前台四条件和 GPU。实际模板切换可能覆盖草稿，故保留边界。文件变化检测基于通知和路径/时间戳/长度，不声明能检测恶意保留全部文件元数据的内容替换。

提交、推送、框架发布/tag、Windward UPM Manager 升级和必要的内容 hash 重新绑定分别等待明确授权。升级不自动重新部署模板；新的配置绑定先展示正式预览差异，再独立授权。框架工程目前 0.1.1→0.1.2 的旧绑定不匹配也仅报告，未擅自修复。

最终现场为 FrameworkScene、无 dirty、无 Play、无活动试玩、无恢复 owner、无编译/导入，见[最终现场](evidence/delivery-environment.json)。本任务没有自动清理其证据目录，也没有把消费端性能问题标成已修复。


## 0.25.4 发布候选核验（2026-11-13）

用户已明确授权发布。复用既有干净发布工作树，从远端 v0.25.3 整理本批修复，保留当前开发分支和其他任务改动。发布轮实际 PackageInfo/API 为 0.25.4，显式编译 finished=1794543522283、domain_reload_after=1794543529915，新程序集及 Console 核对完成。相同四类定向用例在 0.25.4 下重新运行 33/33 通过，0 skipped，约 12.99 秒；未扩大到全量测试。

七模板实际/封存 hash 与六份父快照/指针全部匹配，graph issues=0。候选 staged 共享二进制及 autocrlf=false/true 干净检出通过，字体策略通过，PowerShell 7 下技能 bundle 检查 12 技能/34 文件通过；旧 Windows PowerShell 5 的 GBK JSON 误解码失败单独保留，不记通过。模板、依赖和 bundle 不变；没有保存/Bump/部署模板或升级 Windward。

[发布说明](../../dev/release-0.25.4.md)、[发布轮测试](evidence/release/tests-0.25.4.json)、[实际模板检查](evidence/release/templates-actual.json)、[编译及加载](evidence/release/compile-final-loaded.json)、[二进制](evidence/release/binaries-staged.json)、[字体](evidence/release/fonts-staged.json)、[技能 bundle](evidence/release/bundle-staged.json)。正式发布身份以远端 main 与 annotated v0.25.4 的 peeled commit 为准，发布完成后另记远端证明。

# 编辑器性能：有限基线与框架交接回执

2026-11-13，上海时间。任务接续原方案 B。**当前只完成有限基线和框架交接，问题尚未修复。** 用户在本轮明确调整分工：“如果需要修改框架，请给我对应的提示词我去修改框架，然后消费端更新”。因此未在框架落盘实现，未进行框架编译、测试、发布或消费升级。后续使用[框架修复提示词](framework-fix-prompt.md)。

## 实际基线与保护

- Windward：Unity 6000.5.4f1，Ember 0.25.3，manifest 指向 v0.25.3，实际包 `Library/PackageCache/com.ember@941a945e9797`；正式身份 platformer-2d 0.13.2，扩展 Resolve problems=0。Git HEAD `0a45f7ac5a041bd97adbc04a57e647271147d65f`，已有大量修改。
- 框架开发仓库确认在 `C:/Users/wuyu/My/ember-unity-framework`，包自报 0.25.3，Git HEAD `16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2`，已有未提交的扩展、模板事务、长路径 IO 等修改。只读核对，不认为这些改动是本轮实现。实际状态保存在[框架差异清单](framework-workspace-status.txt)，后续框架对话必须重新核对。
- 本轮开始重新查询到 FrameworkScene 编辑模式、无活动试玩、未编译／未导入、Profiler/profileEditor 均关闭、项目中心未打开；未依据上一对话现场启动。
- 读取两工程根规则与其公共工作流、Unity 验证和改动归属约束，使用 unity-mcp-orchestrator 执行本轮 Unity 操作。现有 MCP 仅连接 Windward，未把该实例当成框架源码的编译验证入口。
- 普通入口采用 `PlayerDevelopmentEnvironment.Play(true)` 创建全新隔离库，通过实际主菜单重新开始按钮及确认按钮进入现行序章；trialActive=false。随后通过正常框架 Shutdown 完成清理再退出 Play。
- 正式试玩入口调用与关卡窗口一致的 `WindwardRoomGraphWindow.Start`，目标 `Assets/Game/Levels/Windward/Chapters/PrologueChapter.asset`，preset 为当前 `PrologueChapterTrial.asset`、abilityMode=Profile、authoringReview=false、includeNeighbors=true。trialActive=true，使用另一个全新隔离库。没有调用旧 Adjacent_Ready 诊断预设或历史 Ready 自动验收入口。
- 两入口均在 `wwr-prologue-e918f1e550fd598f`，玩家稳定位置约 (-18.5, -13.11002)。普通隔离库 `Library/PlayerFormalAcceptance/31f14caff1d5477fba7ecffeae8cb2fc`，试玩隔离库 `Library/PlayerFormalAcceptance/e59522057d764f908d51bdb059cea532`。未读取／覆盖用户正式存档。

## 采样方法与实际限制

每种入口对照项目中心关闭／打开，保持现行关卡和位置。先采集未开启 Profiler 的游戏 delta 与帧计数，再启用 ProfilerDriver、profileEditor 和 CPU 区域记录 Editor CPU，Deep Profile 关闭。每段以约 10—12 秒且 300／320 个游戏观察帧为目标，低帧率上限约 30 秒；实际 callback 执行有少量超出边界。四条件有效记录的两阶段总时长约 141 秒，操作与诊断在有限 P0 范围内，没有扩大全图或长测。

`Profiler.enabled` 单独设 true 不足以获得 Editor CPU 数据，随后明确设置 `ProfilerDriver.enabled` 才取得有效帧。初次结果 `ordinary-closed.json` 的 frames=[]、4 字节 raw，以及最早一次 Vector3 JSON normalized 循环引用异常，均为采样器失败，不作为性能证据。修正为坐标数组后重新采集；没有修改项目 C#、程序集或资源。

有效 CPU 通过 `GetRawFrameDataView` 记录帧时长和样本名／inclusive 耗时／样本索引／直接子项数。首份有效关闭中心样本还保留完整 `.raw`；后续以 JSON 原始 CPU 样本为准，避免重复产生大型 raw。后续 JSON 保留大于等于 1ms 的样本及 Trial Tick、ConditionalMenu、项目中心、Physics2D 和 GC.Collect 指定样本；不是所有 marker 的完整树。每帧同 marker 的最大 inclusive 样本单独汇总，不叠加嵌套时间。第一条游戏 delta 起点早于采样窗口，汇总明确排除它。

现场渲染尺寸读数 3840×2160，Game 窗口约 933×543（编辑器坐标），targetFrameRate=-1、vSyncCount=0、timeScale=1。采样时的[窗口与环境](sampling-environment.json)已保存；未改画质、分辨率、FixedUpdate 或限帧。

**前台与样本数限制不能省略：**

- 普通关闭／打开中心的两个阶段均记录 Application.isFocused=true，Game 为编辑器内部聚焦窗口。
- 试玩关闭中心：未录制阶段 124/124 观察点应用在前台；录制阶段只有 44/132 在前台，其余在后台。
- 试玩打开中心：两阶段均 Application.isFocused=false，虽然内部 focusedWindow 仍是 Game。此项是后台诊断，不符合完整的前台对照条件。不得据此认定打开窗口反而更快。
- 普通打开中心录制有 300 个游戏观察点，但 CPU 帧只返回 298 个（停止边界差异）。试玩关闭中心达到约 30 秒上限时只有 130 个 CPU 帧；未为凑 300 帧延长采样。P0 的“四项均至少 300 有效前台帧”没有完全达到；本次交付是有明确缺口的有限基线。
- 试玩关闭中心中的 3252.49ms 整帧，`TransportCommandDispatcher.ProcessQueue` 占 3076.68ms，为本轮 MCP 环境查询开销。保留原始统计，同时汇总明确标识 dispatcher>10ms 的观察开销帧并单列排除后的统计；不能将 3.25 秒归因于框架 Tick。
- GPU 数据未取得；GC 仅观察 CPU marker，不包含完整分配字节统计；渲染样本保留在原始 JSON，未完成独立渲染线程／GPU 分析。没有做邻房、死亡、保存继续或全地图验证，这些属于修复后的相关回归。

## 结果与可用结论

下表为录制时 Editor CPU 整帧；试玩关闭中心一项排除上述 1 个明确的长 MCP 观察开销帧，其余无需此过滤。完整原值、过滤、焦点、游戏 delta 和逐 marker 统计见[统计结果](baseline-summary.json)，通过[汇总脚本](analyze-baseline.py)由原始 JSON 生成。

|入口／项目中心|CPU 帧数|p50 ms|p95 ms|p99 ms|说明|
|---|---:|---:|---:|---:|---|
|普通／关闭|1081|7.30|9.85|86.06|周期长停顿，不能只看中位数|
|普通／打开|298|10.75|179.31|341.82|录制开销明显，前台样本|
|正式试玩／关闭|129（原130）|148.66|513.18|684.35|录制阶段焦点混合；已排除1个长MCP帧|
|正式试玩／打开|318|44.83|120.23|192.91|后台诊断，不与上项作受控快慢排序|

直接命中的框架热点：

- 普通关闭中心，条件菜单 Update 单次最大 427.60ms，10 帧超过 50ms；对应 EditorLoop 最大 438.14ms。Trial Tick p95 约 0.0012ms，确认这一入口没有活动试玩热检查。PlayerLoop p95 2.52ms、最大 4.50ms。
- 普通打开中心，条件菜单最大 277.64ms，项目中心样本最大 227.33ms，窗口相关 67 帧超过 50ms。PlayerLoop p95 4.70ms。
- 正式试玩关闭中心，Trial Tick p50 124.11ms、p95 217.29ms，130/130 原始帧 Tick 均超过 50ms；PlayerLoop p95 6.01ms。剔除 MCP 长帧不会消除逐帧 Tick 热点。
- 正式试玩打开中心的后台样本，Trial Tick p50 32.54ms、p95 47.76ms；条件菜单最大 109.94ms、项目中心最大 70.17ms。仍支持公共路径存在开销，不用于硬件前台 FPS 承诺。

未开启 Profiler 的游戏帧计数率分别约 131.08、71.77、4.16、17.45 帧／秒；最后一项为后台。录制后的对应帧率与这些数据分开保存在 summary，不能混为优化前后对比。波动、焦点和文件缓存均未控制到可签署最终性能验收的程度。

当前证据支持优先在框架修复共同身份／扩展查询，且普通入口也受条件菜单影响。没有证据支持先改 Windward 玩家碰撞层、房间驻留或一跳集合；没有将这些项目候选提前实施。也没有将 CPU 样本或早先 API 微基准相加推算 FPS。

## 收尾、证据和后续

试玩使用当前 requestId 调用原 `EmberLevelTrialSession.Stop(requestId)`，不解绑回调；等待 owner 清理和编辑现场恢复。11:23:39（上海时间）[最终现场](final-state.json)：FrameworkScene、playing=false、trial=false、cleaning=false、compiling=false、updating=false，场景无 dirty；项目中心关闭。Profiler、ProfilerDriver、profileEditor、Deep Profile 和 CPU 区域均恢复初始 false。

[最终 Console](final-console.json)在 error/warning 过滤下返回 0 error、2 条 Unity Android ADB server 冲突警告。保留原文，未清空 Console，未把这些环境警告当成本问题根因。

本轮落盘仅为本任务文档、采样程序文本、分析脚本与证据；未改业务／框架 C#、PackageCache、模板快照、manifest/lock、配置绑定或资源，因此未触发项目重编译，也不宣称本轮编译通过。`execute_code` 的内存编译是临时诊断，不是项目编译证据。记录末段保存的[输入指纹](consumer-input-fingerprints.json)与[收尾复核](consumer-inputs-unchanged.json)六项均一致；这是该时间段的定向核对，不扩称全仓保护哈希证明。[工作区状态](windward-workspace-status.txt)保留已有修改背景。

主要原始证据：[普通关闭](ordinary-closed-valid.json)、[普通打开](ordinary-open.json)、[试玩关闭](trial-closed.json)、[试玩打开](trial-open.json)、[采样代码文本](capture-bounded.cs.txt)。采样文本保存最后一个条件的版本；普通条件原始 JSON 另有其实际时长和目标计数，不能把最后版参数追溯套到全部样本。

下一步由用户在框架工程粘贴[完整修复提示词](framework-fix-prompt.md)。框架实现、对应 MCP 编译和定向验证完成后，再按各自授权推进发布以及 Windward 的 `Ember/UPM Manager` 升级；必要的内容 hash 绑定先展示正式预览。消费升级后需同环境补齐前台四条件前后复测与直接相关功能验证。此前不将本方案或整个低帧率问题标为已修复。

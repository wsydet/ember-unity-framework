# 增强缓冲进度条独立验收 A1

- 任务：20261117-buffered-progress-bar；日期/时区：2026-11-17，Asia/Shanghai。
- 性质：实施对话之外的独立验收。先读 requirements.md R1 与当前 Runtime，再读 D1/H3/S1 和原始证据；R01–R08、AC01–AC10 未改变。
- 结论：**需返工，BPP-A01（P2）未关闭。AC01、AC08 失败，其余八项在下述支持边界内通过。整体任务未完成，不归档或清理任务目录。**
- 本轮未修产品，仅创建并移除验收专用短测试源码，新增本目录验收证据和记录。没有提交、推送、版本修改、发布、模板操作、新对话或跨对话消息。

## 基线与证据性质

复用原 checkout：codex/local-snapshot-20261111-162819 / 16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2。Unity MCP 确认本工程，6000.5.4f1；实际 UPM 路径为本仓库 Packages/com.ember，embedded 0.25.5。Assets/Editor/EmberEditingTemplate.json 仍为 topdown-2d 0.1.2 / d83863b16bda8c2655f21d6aa3d55693；已读通用与 topdown 字典，base 无 glossary.md。

开始核对 implementation-owned-final.json：12/12 指纹匹配。实施 182 现有文件基线仅三份授权导航文档变化，implementation-backup 的其他实际字节均匹配。入口 52 文件实际只有 runtime-extensions.md 发生预期导航变化；H3/S1 所称“唯一 API 速查变化”不准确，原文保留，不据此推定产品被改。Git 状态按同一 --porcelain=v1 模式与 workspace-final.txt 完全一致；首次差异来自本轮 --untracked-files=all 与原报告的目录折叠方式，不是新增产品差异。见 evidence/acceptance-protection-start.json、acceptance-workspace-same-mode.json、acceptance-workspace-comparison.json。

本轮预先完整备份 192 文件、65,619,052 字节，包括全部 34 个 SharedAssets/Fonts 内部文件、相邻 Fonts.meta、项目 FontSkins 配置/生成项及交付/保护源码。逐字节复核见 acceptance-protection-final.json，变化 0、新共享字体文件 0。备份在 evidence/acceptance-backup。未使用 HEAD 覆盖任何资源。

保护边界：TMP Settings.asset 及 meta 未进入运行前 192 文件实际字节快照；结束时另存当前副本，不冒充初始备份。二者正常受跟踪、非 skip-worktree，开始/结束 Git 均 clean，当前字节逐一等于 HEAD；项目 FontSkins 配置在运行前实际备份内。见 acceptance-config-boundary.json。没有切换全局字体皮肤/语言、修改 TMP_Settings、共享字体/材质/翻译源，也没有全局 SaveAssets/UnloadUnusedAssets。后续返工测试预检应显式包含这些配置文件。

中心制作的依据是独立审阅实施期原始 create/regenerate、保存 Prefab/Binding/用户类快照、编译及 FormalPage=true 运行轨迹，结合本轮未变产品指纹和当前 Bridge 实测。**本轮未重新中心创建 Page**；本轮四 PlayMode 方法和新 PNG 均 FormalPage=false，不能称再次中心运行。旧中心快照不是本轮新生成资产。

## 本轮实际验证

| 范围 | 本轮结果与独立判断依据 |
| --- | --- |
| 定向 EditMode | 明确过滤 11 个方法，11/11、0 失败/跳过，约 .964 秒。连续下降/同值、Fade 退休、对应效果复位、数值/时间、真实文字、极大 Slider、主目标销毁、重绑、引用冲突、零轨道和 Bridge/旧包装。具体 test_names 在 evidence/acceptance-edit.json。不是完整类或程序集。 |
| 交付短 PlayMode | 仅 EUIBufferedProgressBarPlayTests，4/4、0 失败/跳过，方法总计约 2.078 秒：六组合真实 Canvas、scaled/unscaled 暂停、隐藏/禁用/目标销毁/重用、单实例 TMPEx 本地化刷新。evidence/acceptance-play.json。 |
| 独立状态探针 | 缺白引用的纯慢尾 1→.8→.6；缺慢引用的 Shrink/Fade 中点及隐藏恢复；六组合 100→0 终态均通过。evidence/acceptance-independent-state-final.cs.txt / .json。 |
| 独立文字探针 | 普通 TextMeshProUGUI、inactive 预配置独立 TTF 字体、fr-FR 下固定 InvariantCulture；1.125 的两位输出 1.13；精度 0–4、上下钳制、Custom 参数/坏格式、None、极大 finite、非法上限及恢复均通过。只临时改变并恢复当前线程文化。acceptance-text-probe.cs.txt / acceptance-text-result.json。 |
| 独立 Slider 网格 | 使用实际 Image.OnPopulateMesh/VertexHelper 的顶点，四方向方向匹配的 Filled 图形通过；方向不匹配却 Diagnostic=null，复现 BPP-A01。acceptance-slider-result-final.json、acceptance-slider-matched.json 及配套 .cs.txt。 |
| 旧路径/lease | 按实际源码契约重放三个旧 Item 的 0/.5/1、文字/布局/GUID，均符合；真实 never-active lease.Track(Clear) 幂等，新 owner=40 不被已释放旧 lease 再次 Dispose 清除，新 lease 释放后归零。acceptance-legacy-and-lease.json / .tsv / .cs.txt。 |
| 原生选择 | 本轮调用三个 SameNode 参数情景；Slider/Image/EUIImageEx 加 companion 前后选择一致，任意范围 Slider 的旧绝对 Value 语义保持。acceptance-native-selection.json。这是断言探针，不额外虚增 NUnit 结果数量。 |
| 临时 Play 探针 | 两短方法首次 2/2 通过：实际 Destroy(component) 后存活借入白图隐藏/恢复基础色、Play Undo 恢复时钟。补充真实数据/主条断言后，仅重跑 Undo 1/1 通过；不将重复运行计作额外覆盖。源码/原始结果在 acceptance-play-probe*.cs.txt / *result.json。 |

本轮全部 Unity 行为验证都针对已确认加载的程序集。初始主动编译 started=1794852369161、finished=1794852371618、reload=1794852377495；临时探针新增和补充断言各主动编译并记录 MVID；删除探针后主动编译 started=1794852957631、finished=1794852964066、reload=1794852969561。原始请求/Console/事件/加载证据见 acceptance-compile.json、acceptance-probe-compile.json、acceptance-probe2-compile.json、acceptance-cleanup-compile.json。

MCP 过程偏差如实保留：两次 state ping 未答后，同批后续只读 Console/API 检查先于独立计时 30 秒等待恢复，未严格遵守首次失败后的查询间隔；没有重复编译未知请求、未使用日志/BatchMode/dotnet 替代。后续编译确认改为等待 30 秒再查询。不能把这段历史写成每次完全合规的 30 秒恢复。各轮测试前已有实际新程序集/Console 证据，最终事件与卸载确认齐全。

## 可视检查

本轮 evidence/acceptance-visual 保存实际 1000×640 PNG 和 six-frames.json。五次实际时刻 0 / .195539 / .310460 / .681590 / 1.082488 秒；六行依次为 slow 关的 Disabled/Shrink/Fade、slow 开的 Disabled/Shrink/Fade。

独立查看 .19/.68 图并对全部五图逐行重新取像素：主条 x100–579 均为绿色 (28,184,128)，每行 480 个连续主区像素未被白覆盖；文字可见为 60%。.1955 秒 Shrink 白尾收至约 x727，Fade 保持损失矩形且露出背景/橙慢尾；.31046 秒白图已退；.68159 秒三条慢尾仍可见；1.08249 秒损失区全部回到背景 (22,28,34)。独立脚本对五时刻×六行的主区、白条退休和终态无残影断言通过，保留逐段 RGB，见 check_pixels.py / independent-pixel-assertions.json。此结论仅限本地自动渲染，不是人工、设备或构建签收。

## 逐 AC 结论

| AC | 结论 | 支持、依据与边界 |
| --- | --- | --- |
| AC01 | **失败** | normalized/数值上限 API、真实主条、正式强类型 Page 历史证据、有效四方向 Slider/Filled Image 可用；但 Slider 内部 Filled 图形方向未纳入校验，可能几何与白条矛盾且不报错，BPP-A01。不得写“所有文档接受的 Slider 配置均通过”。 |
| AC02 | 通过 | 无白引用纯慢尾的实际 fillAmount、独立线性中点与终态通过；当前尾端续接/同值不重启及真实帧 scaled/unscaled 暂停通过。 |
| AC03 | 通过 | 无慢引用 Shrink/Fade 外部白矩形/alpha 中点、结束隐藏恢复基础色通过；连续 hold 合并、Fade 已淡区退休及新损失替代由定向用例/源码复核支持。限有效一致轨道，BPP-A01 错误配置另列。 |
| AC04 | 通过 | 六组合的实际 H/LTR 同屏像素、真实文字、独立时钟及终态通过；另有六组合归零。四方向有效 Filled 配置的网格与白矩形一致。方向矛盾配置的拒绝缺口归 AC01/08，不能用此通过结论掩盖。 |
| AC05 | 通过 | 可选/None、三类格式、0–4 精度、InvariantCulture/AwayFromZero、坏格式回退、实时真实值、TMPEx 空 Key SetSource/非空 Key 跳过及实例刷新通过；无共享字体新增差异。未新增全局皮肤/语言集成签收，配置备份边界已明确。 |
| AC06 | 通过 | 连续 100→80→50、同帧/同值、回血/上限/输入模式变化及对应效果配置复位符合源码与外部断言；实际 Play Undo 恢复后真实值 60、主条 .6 保留、白图有界结束。没有异步回调/全局更新任务。EditMode Undo 疑点 BPP-A02 已被更准确的 Play 验证排除。 |
| AC07 | 通过 | Sync/Clear、inactive 首次输入、隐藏/禁用/重显、真实 never-active lease 和新 owner 清理、主目标丢失及组件 Destroy 后独立存活白图退休通过；借入资源不销毁。CanvasGroup 透明度由 owner 显式 Sync 的文档合同清楚。 |
| AC08 | **失败** | 输入/时间 finite 边界、非法 maximum、极大 Slider、零轨道/缺失/别名/主目标销毁及恢复等定向通过；但是不兼容 Slider Filled 方向被默默接受，实际图形不再表达声明方向，缺少必须的配置诊断，BPP-A01。 |
| AC09 | 通过 | 审阅真实中心预检/创建/保存/重生成、Bar0–5 强类型字段、编译与 FormalPage=true 轨迹后独立确认链路；本轮实测 Bridge 对象身份、三种原生选择、旧包装和三个旧 Item。受保护源码/布局/GUID 不变。没有声称本轮重新中心运行。 |
| AC10 | 通过（交付证据项） | 有本轮定向结果、实际 Canvas PNG/独立像素判断、各源码批次主动编译/程序集/Console、唯一随包手册及失败历史。产品是否整体可验收仍受 AC01/08 失败约束；配置补存及恢复查询间隔偏差未隐藏，文档需随 BPP-A01 修复补清内部 Filled 条件。 |

不适用项：人工输入、设备、Player 构建、全工程测试、全局语言/字体皮肤切换、模板切换/部署均未执行，也不计作本轮通过。radial、交互 Slider、外部双写、相对斜切及竞争布局仍按 D1 排除，不能悄悄扩大或缩小范围。

## 返工问题与排除项

### BPP-A01：P2，Slider 内部 Filled 方向未验证，开放

- 受影响：R01/R05/R06/R07，AC01/AC08；AC04 的错误配置保护相关，但已验证六组合不作废。
- 位置：Packages/com.ember/UIExtension/Runtime/Behaviour/EUIBufferedProgressBar.cs，TargetIssue 的 Slider 分支（188–195 行）；ApplyVisuals 使用该分支的 slider.direction 驱动 White，而原生 Slider.UpdateVisuals 对 Filled 只赋 fillAmount。
- 预期：主/慢 Slider 的实际填充方向与声明方向一致。内部 Filled 图形不兼容时须明确诊断并停止相应反馈写入，保留规范化数据/文字；不可把损失图放进真实主区。保留合法四方向、Simple/Sliced stretch 路径和旧 ProgressBar 行为。
- 复现：建立 400×40 Slider，非交互、min=0/max=1、有独立 fillRect；fillRect 的 Image 为有实际 Sprite 的 Filled Horizontal/origin=0。Slider.direction=RightToLeft，White 为其完整轨道直接子 Simple Image，专用且 raycastTarget=false。Slow 关，100/100 immediate → 60/100。
- 实际：Diagnostic=null、slider.value=.6；真实网格 x=-200..40，对应左侧 [0,.6]；白矩形 anchors=[0,.4]，与主区重叠，方向矛盾。BTT/TTB 也被接受而仍产生横向主网格。evidence/acceptance-slider-probe.cs.txt 可重放，结果在 acceptance-slider-result-final.json。
- 对照：把 Filled method/origin 按 Slider 方向正确配置，四方向主网格与白图分离，acceptance-slider-matched.json；因此不要求取消四方向支持，而是补齐兼容校验/诊断和说明。
- 定向修复：在 companion 校验实际 fillRect Image 的 Filled method/origin（及相关几何条件），Inspector 显示诊断，补实际网格/像素级用例，手册明确 Slider 内部 Filled 前置。不要改旧 EUIProgressBar、Bridge/生成器或作者共享资源来迁就测试。修复后主动 MCP 编译，再复验失败配置及四方向合法路径、六组合和受影响文字/数据保持即可，不要求全量。

### BPP-A02：Undo 疑点，不成立，保留排查历史

EditMode 直接执行序列化/Undo 的探针观察到旧十秒 hold 未重置（acceptance-undo-result.json）。这未覆盖真实 Play Inspector/Unity 生命周期，不能据此判产品失败。随后真实 Play 中同样序列化 apply + 对应 setter 复位 + Undo 后，延迟恢复 .08、真实值 60/normalized=.6/主条=.6 保留，.4 秒后白条结束；补充断言后的 1/1 明确通过。没有为此修改产品，不能把初次疑点当尚存缺陷。

其他失败历史：实施首轮四个精确 float 断言失败、ShowViewOnly 编译失败、DynamicRow 固定文字探针误判均审阅并保留。验收自身的 JSON Vector2 序列化环错误保留在 acceptance-probe-serialization-failure.json（夹具已在 finally 清理）；EditMode Destroy(component) 的探针失败保留在 acceptance-independent-state.json，不作运行时结论，实际 Play 销毁测试通过。没有删失败记录或迁就探针改旧业务。

## 清理与下一步

只移除了本轮新增 EUIBufferedProgressAcceptanceProbe.cs 及其确证 .meta，源码/指纹/结果已保留，见 acceptance-probe-cleanup.json。清理编译确认 ProbeLoaded=false、PageLoaded=false、Runtime MVID 不变、Play MVID 恢复交付值；FrameworkScene dirty=false、timeScale=1、临时条实例 0、非 Play/编译/更新、Console 错误 0。最终保护字节不变。

将 BPP-A01 和 prompts/05-rework.md 交回实施定向修复，之后另一阶段定向复验；本对话不向其他对话发送消息、不自动创建对话。问题关闭前不能提示“可以收束归档”。满足必要项后再回入口处理本任务目录 .utmp/ember-workflow/20261117-buffered-progress-bar/，正式归档候选仍为 docs/tasks/20261117-buffered-progress-bar/。

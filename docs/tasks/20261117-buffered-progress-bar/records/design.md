# 增强缓冲进度条设计 D1

- 任务：20261117-buffered-progress-bar；方案对话：01a1e56a-0298-74c0-ba05-156773a5f69c。
- 日期：2026-11-17，Asia/Shanghai；有效需求：[requirements.md R1](requirements.md)，R01–R08 / AC01–AC10 均保留。
- 状态：方案完成，具备实施条件；产品、资源、测试未改，未运行探针/编译/测试。本文的接口、默认值与策略是方案决定，不冒充用户原话或已实现结果。

## 1. 基线、归属与兼容决定

UPM manifest 的 com.ember=file:com.ember，lock 的 source=embedded；物理包为 Packages/com.ember，工作区版本 0.25.5，Unity 6000.5.4f1。Git 为 codex/local-snapshot-20261111-162819 / 16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2，无暂存差异。正式编辑记录为 topdown-2d 0.1.2 / d83863b16bda8c2655f21d6aa3d55693，父 base 字典当前不存在，topdown 字典已读。本轮静态复核入口 source-baseline.json 的 52 文件，全部匹配，含 34 个共享字体源文件；完整 Git 状态与入口一致，见 evidence/design-baseline.json。本轮没有 Unity 运行时 UPM 探针；实施通过 MCP 再核对目标实例及解析路径。

归属为框架公共 UI 能力，不属于 UnityFarm 来源、base 业务或玩法模板，不新增 Manager/Module/第三方依赖。无版本修改、模板保存/Bump/部署、提交或发布。所有既有未提交差异都是保护基线，尤其 UI runtime extensions 和四个 SDF 的当前字节；不得从 HEAD 回退。

**D01：新增 EUIBufferedProgressBar : MonoBehaviour companion，组合原生 Slider/Image。** 沿用 Ember.UIExtension 的 EUI 命名惯例、私有生命周期、SerializeField 和 Runtime/Editor 分离。保留 EUIProgressBar 的类、字段和 Value 行为，不修正其旧 Slider min/max 语义，以免暗改调用方。

旧 EUIProgressBar 是普通 C# EUIComponent，Initialize 为 internal，Value 非 virtual。Bridge 的 ProgressBar 实际返回 Slider，生成器也产出 Slider 字段。单继承它既没有 Unity 时钟/Inspector，也不会令中心生成新字段，new 隐藏 Value 还会造成两套数值语义。因此不做无法正式绑定的包装子类，不扩张 EUIComponent 初始化 API，不为复用少量赋值而引入新通用适配层。复用的是已有 Unity Slider/Image 和 companion 接入机制；新组件独占所配置显示目标，游戏数据仍由调用方持有。

**D02：正式接入走既有 Extension。** 标记 `[EUIExtension(typeof(EUIBufferedProgressBar), AutoDetect=false)]`。在中心绑定下拉框显式选 EUIBufferedProgressBar；生成字段为 Ember.UIExtension.EUIBufferedProgressBar。现有 GetExtensionFullTypeName → Extension Bridge 的精确组件解析已经具备能力，预计无需修改 WidgetTypes、Bridge 或生成器。不得把 ProgressBar 枚举改映射到新组件，也不得让 companion 自动抢走 Slider/Image/EUIImageEx。验证后若发现真实接入缺陷，只做最小修复并记录偏差。

## 2. 对外接口和数值合同

以下是首版合同，命名可因实际 API 冲突等价调整，语义不可省略：

| API | 合同 |
| --- | --- |
| SetNormalized(float value, bool immediate=false) | 进入 normalized 模式；规范化后 CurrentValue=n、MaxValue=1、NormalizedValue=n；切换输入模式时立即同步 |
| SetValue(float current, float maximum, bool immediate=false) | 进入当前/最大值模式，minimum 固定为 0；有效上限 >0，当前钳制到 [0,max]，n=current/max |
| SyncImmediate() | 保留真实数值和文字，取消两类效果，慢尾同步 n，白条隐藏并还原基础 alpha |
| Clear() | 可复用，取消效果并设真实 (0,1)、文字更新到 0；回池/新 owner 先调用 |
| ConfigureTargets(Slider mainSlider, Image mainImage, Slider slowSlider, Image slowImage, Image whiteImage, RectTransform whiteTrack, TMP_Text progressText) | 配置/更换借入引用，重绑前清退旧白条；同槽只允许一种主目标，换引用立即同步。支持首次 inactive 调用 |
| SlowBufferEnabled / WhiteMode | white 枚举 Disabled、Shrink、Fade；变化只重置受影响效果，重复相同配置不重启 |
| SlowDelay / SlowDuration / WhiteDelay / WhiteDuration / UseUnscaledTime | 独立时钟参数；值变化时对应效果立即同步，时钟类型变化同步两类效果 |
| TextMode / TextPrecision / CustomTextFormat / ProgressText | None、Percent、CurrentOverMax、Custom；配置变化立即更新文字，不重启动画 |
| RefreshText() | 按当前有效数值与配置重刷借入文字；例如作者已显式移除 TMPEx Key 后恢复数值显示 |
| CurrentValue / MaxValue / NormalizedValue / IsAnimating / Diagnostic | 只读观测；数据读数从组件规范化后的真实输入取得，不从尾条反推 |

输入全部先做有限性检查，NaN 或任意 Infinity 的 current/normalized 按 0；finite normalized 钳制 [0,1]。maximum<=0 或非有限时有效 current=max=n=0，文字 CurrentOverMax 为 0 / 0；无异常、无除零，下一次有效范围可恢复。使用 double 中间运算处理 current/max 和 Slider min/max 差，避免有限 float 差溢出，写入前检查有限性。规范化后的 pair、输入模式完全一致才是同值；不以较宽 epsilon 忽略微小真实变化。动画终点用钳制后的精确目标赋值结束。

Slider 是显示适配器，不是业务范围：要求有限 min<max、wholeNumbers=false、非交互展示，无需改原 min/max。写值为 lerp(min,max,n)，通过 SetValueWithoutNotify，读 authored 值也按 normalized 解释。四方向 LeftToRight、RightToLeft、BottomToTop、TopToBottom 支持。wholeNumbers=true、无 fillRect、非法范围提示配置问题且不写该目标；不偷偷修改作者 Slider 设置。外部继续写同一 Slider、拖动或 Animator 同时驱动不在所有权合同内；不订阅 onValueChanged 把显示变化当新业务输入。

Image 主条/慢尾支持 Type.Filled 的 Horizontal/Vertical 和各自两个 fillOrigin，preserveAspect=false。首版增强组件不支持 radial、Simple 主条或 Tiled 主条；Inspector/Diagnostic 显式告知并停止写无效目标。**旧 EUIProgressBar 的支持行为保持原状**。新组件不是对所有 Image 的替换，文档必须列出这一支持矩阵。Slow 的方向/起点须与主条一致，主/慢的完整轨道可视边界须对齐；允许 Canvas 整体缩放/旋转，轨道内部同向、无相对旋转/斜切、无布局控制器竞争。主 Slider 的 fillRect.parent 应代表完整填充轨道；Filled 图形与 stretch 填充均按该实际轨道对齐。非法主目标不妨碍真实数据/文字更新，但不可据此声称主条视觉通过。

## 3. 三层引用、几何与绘制顺序

主条、慢尾均可选 Slider 或 Image 两者之一；主条必需，慢尾仅 SlowBufferEnabled 时需要。白条为专用 Image（Simple 或 Sliced、preserveAspect=false、raycastTarget=false）和完整 whiteTrack。whiteImage 必须是 whiteTrack 直接子节点，其矩形由组件专有驱动；不能是主/慢 Slider fillRect、主/慢 Image，不能交给 LayoutGroup/Animator。无需新 Shader、网格组件或运行时生成层。文字是借入的 TMP_Text（支持 TextMeshProUGUI/TMPEx）。

绘制顺序由 Prefab 作者设置为 Background → Slow → White → Main → Text。若有子 Canvas 不得通过 overrideSorting 破坏顺序。组件不每帧搬动 sibling，也不重写作者颜色。Inspector 核对同轨道/同向、明显引用复用和排序问题；无法自动证明的复杂 Canvas 布局在文档列前置并由像素证据验证。

设真实值 n，慢尾端点 b，白尾端点 w，白条强度 a，基础白色为 authorColor（RGB 可自定）。主条填充 [0,n]；慢尾填充 [0,b]，由主条遮住 [0,n]，可见区间是 [n,b]；白条**真正矩形区间** [n,w]，不是从零填充后仅依赖遮挡掩盖。水平 LTR anchorMin.x=n、anchorMax.x=w；RTL 为 [1-w,1-n]；竖向 BTT 为 y=[n,w]，TTB 为 [1-w,1-n]。另一轴 stretch [0,1]，offsetMin/Max=0，保留合法 pivot。whiteTrack 应与完整主轨道边界重合，使用锚点可随尺寸变化，无需逐帧测像素。零宽/高轨道不生成非法值，窗口恢复后锚点自然恢复。

白条的 alpha=authorColor.a*a，仅写实例 Image.color，不写共享 Material/CanvasGroup；authorColor 由首次有效绑定捕获，动画中不重复从乘后值捕获。显式重绑同一引用也先恢复再捕获。白条结束或关闭：w=n、a=0、Image.enabled=false，同时 Image.color 恢复 authorColor，下一次受击重新设置 a。组件独占白条 enabled/矩形/color；换引用、Clear/Disable/Destroy 清退旧目标，不留下半透明。主条和文字 alpha 不受白条改变。

引用冲突须先验证再写：同一 Slider/Image、共享 fillRect/Graphic、whiteTrack 等于 whiteImage rect 或非法祖先关系均不能让两个效果写同一对象。可选层缺失/销毁只停该效果，主目标丢失停止视觉效果且保留真实数据/文字；每种诊断状态变化最多警告一次，不每帧刷日志。关掉效果时不要求相应引用存在。无效白条不要触碰主/慢引用。共享资源永远借入，不 Destroy。

## 4. 时间、连续输入与状态转移

**D03：一个实例 LateUpdate 驱动两套独立有限动画，无 Tween、协程、async 完成回调或全局订阅。** 运行时持有 n、b、w、a、各自 delayRemaining/elapsed/start、initialized 与绑定缓存；它们不参与序列化。独立状态 Idle / Holding / Animating。LateUpdate 采用有限非负 dt；一帧跨 delay 时消耗剩余 dt 推进动画，不用循环补帧。duration<=0 在 hold 结束瞬间完成；delay=duration=0 在输入调用返回前终态。有限大 dt 直接完成，非法 dt 按 0。效果参数 finite 负值归 0、非有限归 0；有限正参数允许到 3600 秒，超过钳到该上限，避免误填导致无限等待。

默认 SlowBufferEnabled=true、WhiteMode=Shrink；SlowDelay=0.35s、SlowDuration=0.65s，WhiteDelay=0.08s、WhiteDuration=0.22s；UseUnscaledTime=true。线性进度（有限时长、便于调参和验证），白条先快速反馈，慢尾后续追上；无曲线编辑器或多种速度模型。TimeScale=0 默认仍播放，可切 scaled 后暂停；切换时同步防止时钟跳变。这是方案常规选择。

每次**下降**，主条/真实文字在 API 内立刻写 n；b 从当前已显示位置开始，确保 >=旧真实值，重启慢延迟，之后 lerp(start,n,t)。Shrink 的 w 从当前显示尾端（至少旧真实值）开始，白区从新 n 到 w，重启白延迟，之后 w=lerp(start,n,t)，a=1。二次下降绝不从历史满值重置 b/w；连续受击延长各自 hold，停止受击后有界结束。

Fade 在 hold 时 w 固定、a=1；进入淡出后 w 固定、a=1-t，结束隐藏。**D04：Fade 再次受击时，已开始淡出的旧白区立即退休，新白区只表示 [新n,旧真实n] 并重置 a=1；不会将旧已淡区恢复为不透明。** 若仍在 hold（尚未淡出），可以把连续同批损失合并为 [新n,现有w]，重启 hold。淡出模式只拥有一个反馈层，不保留每次伤害独立队列；新受击替换正在淡出的反馈是明确策略。完成后再受击也只能用旧真实值，不能复活上次 w。

| 触发 | 明确结果 |
| --- | --- |
| 首次 API / 初始化 | 第一次输入始终立即同步，不制造起始受击；从未输入时显示 0/1。Awake/OnEnable 不覆盖先前 inactive 输入 |
| inactive 或组件 disabled 时 Set | 保存最新真实 pair 和文字，所有层立即同步；不排队动画。重新启用不重播 |
| 同帧 100→80→50 | 顺序处理，每次主条立刻更新，最终 n=.5；没有 Tick 时 hold 合并，尾端保持 1，计时从最后一次不同下降算 |
| 同值反复 Set | 不重置计时、alpha 或起点；若 immediate=true 仍执行同步。文字配置变化使用专门配置入口 |
| 100→80，尾端已到 .9，再→50 | 慢/收缩白尾从各自当前显示端点继续，各自重启延迟，不回到 1 |
| 任意增加/回血/回满 | 立即同步全部层到新真实值并隐藏白条，取消全部旧损失；无回血缓动。包含 n 增加但 current 不增的情况 |
| 最大值改变 / 输入模式改变 | 原子接受新 pair，立即同步所有层；即使 n 下降也不伪造受击。相同 maximum 下才按下降/增加判断 |
| current 改变但规范 n 相同 | 更新真实文字，不制造损失，不重启效果；有效 pair 相同则完全无操作 |
| Slow 开关改变 | b=n，慢动画清空；关闭不留尾，打开只对未来下降工作，不改变白条 |
| White Disabled/Shrink/Fade 切换 | 清空白条并恢复 alpha，只对未来下降工作，不影响慢尾；重复模式不重启 |
| 修改对应 delay/duration / 重绑目标 | 对应效果同步；目标重绑同步全部；旧白引用先清退 |
| SyncImmediate / immediate Set | 取消两效果，b=w=n，白条隐藏并恢复色；所有时钟归零 |
| OnDisable/隐藏 GameObject | 保留真实 pair/文字，效果同步，不累计离屏时长；再次显示没有旧伤害尾条 |
| Clear / 回池 | 真实重置到 0/1；白条退休、计时归零。新 owner 在显示前 SetValue(...,true) |
| OnDestroy / 目标销毁 | 幂等清退白条与状态，不订阅、不留更新任务；借入目标/字体/材质不销毁 |

仅 CanvasGroup.alpha=0、Graphic.enabled=false 或被遮挡不是 owner 生命周期；默认时钟继续。业务希望结束时显式 SyncImmediate；页面 OnHide 调 SyncImmediate，动态 Item Bind 时 `lease.Track(bar.Clear)`，涵盖 never-active 回池。不能假定 OnDisable 总会收到，也不自动扫描页面/池。组件自己的 disabled 属性变化按上表处理。

### 六组合的准确显示与终态

下表均从 100→60，最终主条 .6、真实文字 60%（或60/100）；Tslow=.35+.65=1s，Twhite=.08+.22=.30s。中途两效果无需互相等待。

| Slow | White | 可见过程 | 终态 |
| --- | --- | --- | --- |
| 关 | 关 | 只有主条立刻 .6 | 无尾区，无白图 |
| 开 | 关 | 慢尾保留 1，.35s 后 .65s 内收至 .6 | b=.6 |
| 关 | Shrink | [.6,1] 白区 hold .08s 后尾端收至 .6 | 白图隐藏、基础色还原 |
| 关 | Fade | [.6,1] 白区 hold .08s 后原区 alpha 降到0 | 白图隐藏、基础色还原 |
| 开 | Shrink | 白区盖住损失区域的慢色；白区先收，随后可见慢尾，两个端点独立 | Twhite 后仅剩慢尾，Tslow 后无尾 |
| 开 | Fade | 白区先淡，逐渐露出其下的慢色；主条始终真实 | Twhite 后仅剩慢尾，Tslow 后无尾 |

## 5. 进度文字合同

TextMode 默认 Percent、TextPrecision 默认 0，范围钳制 0–4；未配文字或 None 时不更新任何文本。Percent 输出 round(n*100,precision) 加 `%`，CurrentOverMax 输出有效 current / max，固定小数位、InvariantCulture、四舍五入 AwayFromZero。normalized 模式下 CurrentOverMax 为 n / 1，不猜测业务上限。读取仍为真实输入，不读取 b/w；显示精度不改变几何。

CustomTextFormat 使用 .NET composite format：{0}=current、{1}=maximum、{2}=normalized、{3}=percent（0–100）。例 `{0:0.0} / {1:0.0}`、`{3:0}%`。内置格式使用 TextPrecision；Custom 的格式项自己控制精度。空/纯空白、FormatException、越界参数索引或异常格式均回退内置 Percent；不把格式错误抛到游戏循环。每种失败只告警一次。字符串仅在真实 pair/文字设置/引用变化时重算，不随尾条每帧格式化；不要声称全部 setter 无 GC。

TMPEx 数字目标要求 Key 为空；组件不清 Key、不写全局本地化源/表。Key 非空时 Diagnostic 提示并跳过该文本，避免与语言刷新争写；作者显式清空后 RefreshText 即可。Key 为空时使用 SetSource 更新本实例的运行时原文，使语言刷新保留数字。普通 TMP_Text 写 text。组件不设置字体、字号、材质、皮肤或翻译源，不新增全局语言订阅；自定义格式的语言切换由页面重新赋配置并 RefreshText。Clear 把仍配置的动态文本更新为 0，移除引用仅放弃所有权，不恢复历史业务数字。

## 6. Inspector、文件与实施顺序

Inspector 使用清楚的引用、慢尾、白条、文字、时间分组，显示受选模式控制的字段及缺失/冲突/不支持方向诊断；不新增 Odin 全项目整理任务。Editor 只在 Ember.UIExtension.Editor 内。提供只读真实值/动画状态和 Play 模式“立即同步”，编辑期不自动运行/保存动画，不借 OnValidate 修改共享资源；Undo/多选正确。参数校验必须有 Runtime 路径，不能只靠 Inspector Min。

| 路径 | 计划变化 |
| --- | --- |
| Packages/com.ember/UIExtension/Runtime/Behaviour/EUIBufferedProgressBar.cs + .meta | companion、序列化引用/参数、数据与有限状态、公开 API |
| 同目录 EUIBufferedProgressContracts.cs + .meta（按必要性拆） | white/text 枚举及紧密相关结构；避免扩大 public API |
| Packages/com.ember/UIExtension/Editor/UIExt/EUIBufferedProgressBarEditor.cs + .meta | 独立 Inspector / 配置诊断 |
| Packages/com.ember/Tests/EditMode/EUIBufferedProgressBarEditTests.cs + .meta | 输入、可见目标值/文字、引用/格式、正式生成和旧路径回归；Ember.UI.Tests |
| Packages/com.ember/UIExtension/Tests/PlayMode/EUIBufferedProgressBarPlayTests.cs + .meta | 短真实帧时钟、六组合渲染、生命周期；Ember.UIExtension.PlayModeTests |
| Packages/com.ember/Documentation~/ui/buffered-progress-bar.md | 唯一长期用法、层级图、六组合、API/支持矩阵、池化、回退与测试边界 |
| Packages/com.ember/Documentation~/ui/README.md、runtime-extensions.md、docs/dev/ember-api-reference.md | 最小导航/接口索引增补，保护原正文 |
| 本任务 implementation.md / evidence / prompts/04-acceptance.md | 自检、原始证据、偏差和独立验收接续 |

现有 Runtime/Editor/Test asmdef 已足够，预计无需改引用。EUIProgressBar、EUIComponent、Bridge/生成器、TMPEx、Loading 和三个既有进度 Item 作为受保护回归对象，不为新能力主动重构。不存在旧新序列化字段迁移；使用方按需添加 companion、专用白图和慢图，再中心重生成；旧 API 可继续使用。回退为移除新增 companion/层并重新生成旧绑定，旧页面资源和数据未迁移，不支持同一目标双写。

不新增常驻正式演示页或批量替换既有 UIFoundation 资源。**必须执行一次中心制作验证页**，以满足正式页面实际接入，不能仅拼字符串断言生成类型：实施读取 ember-eui-build 及实际 EUICreationService 签名，使用 TryBuildPlan/Create 创建唯一的 Business Page `EUIBufferedProgressValidationPage`，classPath=Module/BufferedProgressValidation，输出以中心报告为准，预期代码 Assets/Game/UI/Runtime/Module/BufferedProgressValidation、资源 Assets/GameResource/Resources/UI/Module/BufferedProgressValidation/Prefabs。编辑 PrefabContents 追加六行/文字和实际引用，登记 Extension 后保存、重新加载，通过 TryRegenerateCode 生成；读生成字段后才写用户 partial。编译新生成类，实际经 EUIBindingBridge.Attach/正式页面生命周期取得组件调用 SetValue，截取运行图。保留生成报告、代码/Binding 快照、引用/GUID与截图为证据。

验证页是任务临时夹具：预检冲突时选择唯一任务后缀，不覆盖同名资产。记录创建清单、生成配置/注册受影响内容的实际字节；只撤销本任务创建/增量，保护并发外部改动。验证后移除临时页/注册/生成文件、探针并再主动 MCP 编译确认它们已卸载，保留可从测试重建的配方和证据。不得手写 Binding、Prefab YAML、伪造 Framework 身份、修改 Templates~/ParentSnapshot~，不自动加载/切换/保存模板。中心不可用则这部分明确待验证，不能降低 AC09。

实施次序：保护当前字节 → 实现 Runtime/Inspector/定向用例 → MCP 主动编译确认完成 → 定向 EditMode/短 PlayMode → 中心生成并编译验证页/实际渲染 → 清理自有夹具并编译 → 文档/证据/交接 → 创建独立验收。每批新 C#/asmdef（含生成/删除）均编译；不是只在最后请求一次。

## 7. 增量验证、证据与 AC 映射

仅运行新增 EUIBufferedProgressBarEditTests、EUIBufferedProgressBarPlayTests 及直接相关旧路径的具体过滤；事先列范围和估计耗时。短帧场景预计数秒级，整轮以分钟级相关验证为目标；若实际预计长时，按公共规范先取得相应授权，不拆批绕过。不运行整个程序集/全工程，不机械重跑前任务全部用例。编译经 Unity MCP compile=request，保存请求 ID/时间、本轮 started/finished/domain reload 或新 API/MVID、Console 与最终状态；单次 idle、空 Console 和刷新成功均不足。断连先核对请求是否送达；无法送达立即告知手动触发编译，然后每30秒最多10次有界恢复；不以 Editor.log/BatchMode/dotnet 替代。

| AC | 增量行为/视觉验证与外部断言 |
| --- | --- |
| AC01 | 中心验证页的生成字段编译并实际经 Bridge/页面取得组件；设置 .6 与60/100，读实际 Slider normalized/fillAmount、TMP 内容与渲染边界。Slider [-50,150] 应实际 value=70；同时覆盖四方向、Image H/V 两origin。 |
| AC02 | 无白引用，100→60 主条 API 返回即 .6，delay 前尾1，delay 后中间位置，终点 .6；测真实帧经过时间和图像边缘，scaled/unscaled 暂停。 |
| AC03 | 无慢引用，Shrink/Fade 各采 hold、中段、终点；量白矩形实际世界角/像素范围和 TMP/主条区像素。二次输入在 hold、shrink中、fade中和结束后分别观察，不能复活已过期区。 |
| AC04 | 参数化六组合及100→0；六行同屏真实渲染，取 t=0、.19、.30、.68、1.05 秒附近帧（记录真实时间/容差），看颜色揭露/alpha/主区未被白覆盖；每组合独立终态，不能只读私有动画标志。 |
| AC05 | 无文字、None、百分比0/2位、60/100、小数、Custom有效/空/坏索引/括号；buffer中数值立即是60；TMPEx Key空数字与语言刷新保持，非空Key不写；独立字体实例渲染。 |
| AC06 | 100→80→50、同帧多次、同值每帧、缩/淡时回血/回满、上限200/50/0/恢复、normalized模式互换、六组合运行中切换、参数/引用更换；用实际目标位置/透明度/文字/时间检查新状态，没有回跳或旧残影。 |
| AC07 | never-active Configure/Set后激活，先 inactive Set后Clear回池，再绑定另一对象；OnHide、enabled=false、GameObject隐藏、再次显示、destroy component/white/main/text（含never-active），隔几帧看目标无再写；lease.Track(Clear)释放后文字0、白条隐藏。恢复编辑现场。 |
| AC08 | current/max含负值、极大finite、NaN/±Infinity、越界；delay/duration 0/负/非finite/超大；可选层缺失、别名共享fillRect、white非法父子、目标中途销毁、零尺寸轨道、Slider非法range/wholeNumbers、Image radial等配置拒绝。检查无异常、无非法顶点、无日志风暴、明确Diagnostic和可恢复性。 |
| AC09 | Extension候选精确选择、Bridge对象身份、真实中心生成与运行；加companion前后原生Slider仍自动ProgressBar、Image仍Image、ImageEx仍原Extension。旧EUIProgressBar Value既有行为不变（包含任意Slider range的历史语义），Loading及两类进度Item以0/.5/1定向调用并核对原文字/布局。GUID/受保护字节前后比较。 |
| AC10 | 保留MCP原始编译/测试JSON、具体过滤及数量（不累计重跑）、真实PNG及时间轨迹/像素判断、中心生成报告、清理后状态、文档复现步骤/链接与字体字节报告。自检仅写自检，实施后由另一对话逐条独立判断。 |

图像必须来自 Unity 真正 Canvas 渲染，测试可创建隔离 Camera/RenderTexture/Canvas；不能画一张解释图替代。按预期颜色区域/几何和分时画面对照，截图文件存在不是通过；可视报告需记录分辨率、容差、每一模式与文字的判断。没有人工输入时不记人工/真机/构建签收。不强行覆盖 radial/相对斜切、交互Slider或外部动画双写，文档说明限制。

### 字体与现场保护（必需）

完全承接前任务 README/decisions 的边界：接受旧钉钉 SDF 现状不允许新增写回。不依赖前任务已删除临时目录或恢复旧hash。测试前把当前 SharedAssets/Fonts **全目录及meta**、实际涉及的项目字体/配置做完整逐字节备份与路径/长度/SHA256清单，备份在本任务 evidence 下；新文件也需监测。实施拟改现有文件同样备份实际工作区字节及meta，不能只记HEAD。

只读共享 TTF，使用 TMP_FontAsset.CreateFontAsset 从原生 Font 创建**独立** FontAsset、atlas Texture、Material 与独立 fallback 列表，明确分离对象身份；不克隆共享动态 SDF，不继承共享 fallback，不切全局 EmberFontSkins，不写 TMP_Settings/defaultFontAsset。不允许测试 Text/TMPEx 初次 Awake/OnEnable 先碰共享默认字体：先在inactive对象/Prefab中配置独立font/material，确认激活前绑定；TMPEx设手动皮肤-1且Key为空，预填测试字符。必要时用同样隔离的静态测试字体资产。只读TTF本身可引用，不 SetDirty/SaveAssets 共享源。

font夹具销毁前从实际 TMP 及fallback/material引用卸下，销毁测试持有的 FontAsset/atlas/material/RenderTexture/相机/Canvas；不要全局 UnloadUnusedAssets 或 SaveAssets。生成页/注册恢复及探针移除后再比对完整字体目录字节、源文件和原有差异，保存清单。发现差异立即停止会继续写源的验证，保留前后字节/证据，只恢复能确证归本任务的增量；无法判归属时报告阻断，不从HEAD覆盖。无共享写回证据是本任务交付条件。

## 8. 交接、偏差与完成边界

需求无变化，无关键产品阻断。实施可自行优化内部拆分/测试夹具，若改变上述数值、损失区间、回血/连续输入、方向支持或生命周期合同，必须写 D2/偏差及受影响AC，实质范围变化询问用户，不能偷偷省略白条任一模式/组合/文字。

实施完成后写 implementation.md、维护 handoff 和 prompts/04-acceptance.md，按用户授权在同项目local创建独立验收，不指定模型/推理、不创建worktree/分支，保存threadId/hostId和wait_threads启动快照。派发后停止产品和共享交接变更。独立验收只在必要实现/自检前置就绪后创建；环境阻断时保留待验证，不预填通过。全部验收后由入口归档到 docs/tasks/20261117-buffered-progress-bar 并精确清理本任务临时目录，本阶段不归档/清理。

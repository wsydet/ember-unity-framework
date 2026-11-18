# 增强缓冲进度条：任务归档

- 任务：20261117-buffered-progress-bar。
- 日期/时区：2026-11-17，Asia/Shanghai。
- 状态：已完成；归档核验通过，本任务临时目录已精确清理。实际清理结果见 closure.json。
- 最终验收依据：[独立验收 A1](records/acceptance.md) 和 [独立定向复验 A2](records/acceptance-recheck.md)。A1 原失败历史保持；当前结论采用 A2，不能把旧报告的“需返工”当成仍开放，也不把旧失败改写成当时通过。
- 当前没有必需失败、受阻或待验证项；BPP-A01 已由独立 A2 关闭，BPP-A02 是已排除的历史疑点。

## 交付与使用入口

正式能力为 `Ember.UIExtension.EUIBufferedProgressBar`，以 MonoBehaviour companion 组合原生 Slider/Image；旧 EUIProgressBar 普通包装、ProgressBar→Slider 绑定和现有业务 Item 保持原行为。

| 能力 | 最终行为 |
| --- | --- |
| 真实主条 | normalized 或当前/上限数值输入，主条即时反映真实值；支持四方向及文档约定的 Slider/Image 配置 |
| 慢速缓冲 | 独立开关、延迟和时长，从当前显示尾端追到最新真实值 |
| 白条 | Disabled/Shrink/Fade，专用损失矩形；可独立或与慢尾组合，六种组合分别验证 |
| 文字 | 可选 Percent/CurrentOverMax/Custom、精度、安全回退；显示真实数据，不随尾条滞后；尊重 TMPEx Key 所有权 |
| 状态与生命周期 | 连续受击、回血/上限变化、同值去重、按效果复位、立即同步、inactive 初始化、显隐/池化/销毁与借入白图清退 |
| 正式接入 | 精确 Extension/中心生成强类型字段，AutoDetect=false，不抢旧 Slider/Image/ImageEx 自动选择；独立 Editor 提供配置与诊断 |

长期接口、默认参数、完整四方向表和制作方式只维护在[随包缓冲进度条说明](../../../Packages/com.ember/Documentation~/ui/buffered-progress-bar.md)。[UI 入口](../../../Packages/com.ember/Documentation~/ui/README.md)、[运行时扩展](../../../Packages/com.ember/Documentation~/ui/runtime-extensions.md)及[API 速查](../../dev/ember-api-reference.md)承接导航。本归档不复制一份使用手册。

## 关键决策与兼容边界

- 继承现有包装不是必选条件；正式页面实际取得 Component，因此采用可正式绑定的 companion 并复用原生目标，旧包装/Bridge/生成器不改。
- 每实例单 LateUpdate，两套独立延迟/线性时钟，无常驻 Manager、异步任务或全局订阅。白条只占损失区，作者基础颜色在结束后恢复。
- 默认慢尾开启、White=Shrink，慢尾 .35/.65 秒、白条 .08/.22 秒，默认非缩放时间；这是 D1 工程决策，非额外声称用户指定了参数。
- 展示专用、无外部双写；完整轨道同向对齐。Slider 为有限 min<max、非整数、非交互并有 fillRect；内部 Filled Image 须匹配 Horizontal/Vertical 和 origin、关闭 preserveAspect。Simple/Sliced 走原生 stretch。独立主 Image 仅合法 H/V Filled。radial、相对斜切、竞争布局等按 D1 不支持并诊断。
- 错误主目标停止其写入及两类反馈，保留真实数据/文字；仅慢目标错误不影响有效主/白。修复配置后用 SyncImmediate/ConfigureTargets 恢复最新值；不放宽同值去重，也不自动改作者配置。
- 无 Boss 业务、分段多管血条、伤害计算、飘字、粒子/音效等交付；没有自动替换已有页面或模板操作。

## 逐项收束结论

| AC | 最终结论和来源 |
| --- | --- |
| AC01 | A2 通过，关闭 A1 失败：内部 Filled 错向被诊断/停写，四方向实际 Sprite 网格、任意 min/max、WithoutNotify 及最新值恢复通过；正式中心链路承接 A1。 |
| AC02 | A2 六组合慢尾时序/终态通过；纯慢尾及暂停等未受影响边界承接 A1。 |
| AC03 | A2 Shrink/Fade 实际损失区、alpha、退休及失效基础色恢复通过；连续受击等未变部分承接 A1。 |
| AC04 | A2 五时刻×六行主区像素、四方向主图/损失图网格、慢目标失效独立性通过。 |
| AC05 | A2 真实数值/文字和源保护通过，格式/精度/Key 等承接 A1；没有全局语言/皮肤集成签收。 |
| AC06 | A2 运行中失效、同值去重和显式恢复通过；连续输入、回血/上限/模式及真实 Play Undo 承接 A1。BPP-A02 不成立历史保持。 |
| AC07 | A2 已借入/未借入/别名白图清理和重绑恢复通过；生命周期、never-active lease 等未变部分承接 A1。 |
| AC08 | A2 通过，关闭 A1 失败：轴/origin/radial/比例不兼容有诊断并停止对应写入，真实数据保持，合法网格有界；其余数值/时间边界承接 A1。 |
| AC09 | A2 受影响文件/Inspector/支持条件复核通过，其余产品、API、序列化、asmdef、旧包装/Bridge/生成器/Item/GUID未变；中心链路承接 A1，未声称 A2 再中心制作。 |
| AC10 | A2 定向测试、独立断言、PNG/像素、主动编译/Console、文档复核和完整保护证据齐全；失败、夹具错误和查询间隔偏差历史保留。 |

**A2 实际运行 20/20 EditMode、1/1 短 PlayMode，0 失败、0 跳过。** 独立动态探针 12 行断言通过，未计为额外 NUnit 测试。S2 自检 35/35 + 1/1 不加进 A2 数量；A1 的 11/11 EditMode、4/4 交付短 Play 和临时探针重复运行也保持各自范围，不能相加为新覆盖或声称全量。

A2 主动 compile=request：started=1794883443366、finished=1794883446630、domainReloadAfter=1794883454353；Runtime MVID=42cc011e-b222-4652-ac3b-501b8486b6e0，Edit MVID=0f75e22c-c505-40a3-ac90-d74be143a202，Console 错误 0 后测试。代码未变时 MVID 与 S2 相同，完成判断还依据本轮新事件和新方法加载。最终 FrameworkScene dirty=false、timeScale=1、临时条 0、PageLoaded/ProbeLoaded=false，非 Play/编译/更新。

入口本轮为文档/归档/精确清理，没有修改产品代码或资源，没有重新编译/运行 Unity 测试；以上为核对过的独立历史证据。

## 失败历史及验收边界

- BPP-A01：A1 独立发现 Slider 内部 Filled 实际方向与 Slider.direction 不匹配却被接受，导致主图与白损失图重叠。S2 修正校验、诊断、网格测试和手册；A2 以原错误配置、合法网格及运行中失效/恢复独立关闭。
- BPP-A02：EditMode Undo 疑点经更准确的真实 Play 数据/主条/终态验证排除，未为迁就初次探针修改产品。原疑点和排除依据均保留。
- 实施/验收的精确浮点断言、临时 Page 编译、旧 Item 契约误判、序列化环等夹具错误记录保留。A1 两次恢复查询间隔偏差不改写为历史完全合规；S2/A2 的后续等待修正另有证据。
- A2 新六组合截图全部 FormalPage=false。真实中心创建/保存/重生成、强类型字段、编译和 FormalPage=true 生命周期来自实施原始证据并经 A1 独立审阅；A2 对未变链路承接，没有再次中心制作/运行。
- A1 的 TMP Settings/meta 未在运行前字节快照内，结束补存及 clean/HEAD 比对边界保留；S2/A2 201 文件的完整运行前备份不能倒推 A1 当时也已备份。共享字体 34 文件及相关配置无本任务新增写回，没有从 HEAD 恢复用户修改。
- 自动渲染/像素判断不是人工、设备、Player 构建或全工程签收。全局语言/字体皮肤、radial/交互/双写/斜切/竞争布局等边界保持，不形成被隐去的必需待验证项。

## 基线、记录与证据

- Git：codex/local-snapshot-20261111-162819 / `16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2`；维护工程含未提交工作，不等同已发布版本。
- Unity 6000.5.4f1、embedded Ember 工作区 0.25.5、实际包路径 Packages/com.ember；正式编辑 topdown-2d 0.1.2 / hash `d83863b16bda8c2655f21d6aa3d55693`。没有新建分支/worktree、操作版本或模板。
- 入口重新核对：12 交付指纹、201 最终保护文件、201 实际备份、90 历史根证据及 34 字体文件集合均匹配 A2。归档清理前后还会按这些指纹检查，结果见 closure.json。
- [需求 R1](records/requirements.md)、[方案 D1](records/design.md)、[实施 S1/S2](records/implementation.md)、[独立验收 A1 原文](records/acceptance.md)、[独立定向复验 A2 原文](records/acceptance-recheck.md)。这些是原字节历史记录，其旧“待验收/未完成”状态由本页最终结论覆盖，原失败不改写。
- [全部路径/长度/SHA256 清单](archive-manifest.json)、[收束及精确清理结果](closure.json)。

完整原始归档 `task-evidence.tar.xz` 由维护者在本地留存，可按上述指纹清单复核。

证据包采用标准 tar.xz，`source-task/` 下保留本任务所有原始文件及备份，按相同内容相邻排列压缩以减少重复字体备份占用；路径和文件字节不合并或改写。归档验证实际逐文件解压流并核对长度/SHA256，包含 A1/A2、原始 JSON/PNG、中心快照/制作配方、提示词、失败与配置边界、201 文件保护备份和历史源字节。

用标准 tar 解压后，在 `source-task/` 中按原始相对路径读取证据；不需要恢复原临时路径。records 同时展开 A2 直接链接的关键 JSON/探针文本/可视证据，便于阅读。历史原文中的机器/任务路径仅作来源，不是当前运行依赖。长期用法不依赖临时目录。

归档可独立读取、校验成功且无剩余必要项后才标完成，再只清理精确任务子目录。用户未授权自动提交、推送、发布或模板操作，本轮均不执行。

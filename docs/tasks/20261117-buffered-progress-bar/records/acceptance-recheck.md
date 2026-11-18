# 增强缓冲进度条独立定向复验 A2

- 任务：20261117-buffered-progress-bar；日期/时区：2026-11-17，Asia/Shanghai。
- 性质：S2 实施对话之外的独立定向复验。依 requirements.md R1、当前真实源码判断，再参考 acceptance.md A1、implementation.md S2 和 handoff.md H5；R01–R08、AC01–AC10 与 D1 合同不变。
- **结论：BPP-A01（P2）独立复验通过，关闭。AC01、AC08 在 D1 支持范围内由 A1 的失败改判为通过；其余 AC 的受影响部分复核通过、未受影响部分承接 A1。没有必需失败/受阻/待验证项，可以回入口执行收束。整体流程尚未归档、标完成或清理。**
- 本轮没有修改产品 C#、测试源码、资源、公共 API 或模板，没有新增常驻探针/正式 Page；仅新增本轮证据、本文及更新 handoff。未提交、推送、发布、创建或发送其他对话。

## 1. 实际基线、保护和变更范围

复用原 checkout：codex/local-snapshot-20261111-162819 / 16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2，未建分支或 worktree。Unity MCP 确认工程根为本仓库、6000.5.4f1；UPM 实际 embedded com.ember 0.25.5，解析路径为本仓库 Packages/com.ember。正式编辑记录仍为 topdown-2d 0.1.2 / d83863b16bda8c2655f21d6aa3d55693；通用和该模板字典已读。工作区其他差异均保留，不把 HEAD 当作用户实际字节。

独立逐文件计算 SHA256/长度并对实际字节核验：

| 核对项 | 本轮结果 |
| --- | --- |
| S2 owned-final.json | 12/12 当前交付指纹相符，meta/GUID 保持 |
| S2 baseline.json / backup | 201/201 实际备份字节及长度与其初始指纹相符，未用结束补存冒充运行前备份 |
| S2 protection-final.json | 当前 201 文件全部符合其最终指纹；与 S2 初始实际字节仅六项差异：四个交付文件和授权 implementation/handoff 记录 |
| 相对 A1 产品 | 仅 Runtime companion、Inspector、EditMode 用例、随包缓冲条手册四文件变化；本轮独立重建 diff，不只信任 S2 摘要 |
| 历史保护 | S2 保存的 90 个既有根证据指纹全符；acceptance.md A1、requirements.md R1、design.md D1 原始指纹保持 |
| 共享字体与配置 | 全部 34 个 SharedAssets/Fonts 内部文件、相邻 meta、项目 FontSkins 配置/生成项、TMP Settings.asset、DefaultFontSkins.asset 及相关 meta 均在 S2 实际运行前备份内；本轮再独立实际备份 201 文件后才进入 Unity 测试 |

原始独立核对见 [s2-independent-audit.json](evidence/recheck-a2/s2-independent-audit.json)、[independent-product.diff](evidence/recheck-a2/independent-product.diff)。本轮备份清单为 [baseline.json](evidence/recheck-a2/baseline.json)，实际字节在 evidence/recheck-a2/backup，原 H5 也在其中。最终 [protection-final.json](evidence/recheck-a2/protection-final.json) 只允许本轮 handoff 更新，其他保护字节变化 0、90 根证据变化 0、共享字体新增 0；12 交付指纹保持。开始/最终同模式 Git 状态另存 workspace-start.txt / workspace-final.txt。

A1 未在运行前备份 TMP Settings 的边界原样承接 acceptance.md 与 acceptance-config-boundary.json；S2/A2 的完整备份不能倒推 A1 初始也有这些文件。本轮未切全局语言/字体皮肤，未修改共享源、TMP_Settings，未全局 SaveAssets/UnloadUnusedAssets。文字先在 inactive 节点配置独立 TTF→FontAsset/atlas/material、空 fallback、手动皮肤 -1 和空 Key，再激活；测试结束只释放独立对象。

## 2. 本轮编译与实际执行范围

本轮虽然未改产品源码，仍显式通过 MCP 提交一次 compile=request，以新请求对应的编译/域重载事件核对现场，再读取 Console 后测试：started=1794883443366、finished=1794883446630、domainReloadAfter=1794883454353。Runtime MVID=42cc011e-b222-4652-ac3b-501b8486b6e0，Edit MVID=0f75e22c-c505-40a3-ac90-d74be143a202，两个新方法实际已加载，Console 错误 0。因无源码变化，MVID 与 S2 相同是预期；完成判断还依据本轮新事件，未拿旧 MVID 单独代替本轮完成。

首次 state 查询返回 ping not answered 后，额外等待满 30 秒，第一次重试恢复；没有重复编译请求，也没有在恢复等待期间发起其他 Unity 查询。证据：compile-request.json、compile-state.json、compile-retry1-state.json、compile-loaded-console.json，均在 evidence/recheck-a2。没有用日志、BatchMode 或 dotnet 替代。动态 execute_code 探针未落成 Unity C# 资源，无新增/删除源码编译批次。

| 本轮范围 | 结果与证据 |
| --- | --- |
| 精确两方法 EditMode | **20/20，0 失败/跳过**，测试汇总 .9319251 秒。仅 SliderSpriteMeshAndLossMeshAgreeInFourDirections（12）及 InvalidSliderFilledConfigurationStopsOnlyAffectedWritesAndCanRecover（8）。job=0f3872893bd34d67a1610d96cb85f766；[edit-start.json](evidence/recheck-a2/edit-start.json)、[edit-result.json](evidence/recheck-a2/edit-result.json) 保存过滤和逐例原始结果。不是当前 50 情景类、全程序集或全工程。 |
| 独立补充探针 | **全部断言通过**，未计入 NUnit 数量。自行搭建 preview scene/独立字体与图形，覆盖五种错误×主/慢两角色，以及原始 RTL 从未借入情景、白图变主 Graphic 别名清理。见 [independent-probe.cs.txt](evidence/recheck-a2/independent-probe.cs.txt)、[independent-probe-result.json](evidence/recheck-a2/independent-probe-result.json)。 |
| 六组合短 PlayMode | **1/1，0 失败/跳过**，测试汇总 1.2481733 秒。仅 SixCombinations_RealCanvasFramesAndGeneratedPageLifecycle，job=e70b4a9fa6654231a9754cf2b82744ef；[play-start.json](evidence/recheck-a2/play-start.json)、[play-result.json](evidence/recheck-a2/play-result.json)。本轮 FormalPage=false，没有重新中心制作。 |
| Inspector/隔离/手册 | Editor.CreateEditor 实际选到 Ember.UIExtension.Editor.EUIBufferedProgressBarEditor；Diagnostic 读取不写数值。源码核对具体 Diagnostic→Warning HelpBox 接线和 Filled 前置说明；新增 Runtime companion 无 UnityEditor/自定义 Editor 依赖，asmdef 未变。37 个相对文档链接无断链，方向表、停止写入和显式恢复步骤与实现及本轮断言一致。见 inspector-wiring.json、static-review.json；未声称实际操作 Inspector GUI 或人工验收。 |

## 3. BPP-A01 的独立判断

### 3.1 拒绝配置、停止写入和运行中失效

Runtime 的 Slider 分支在 fillRect 的 Image 为 Filled 时先复用 Image 的 H/V、origin 0/1、preserveAspect=false 校验，再将实际图形方向与 Slider.direction 比较。检查在 WriteTarget 之前、每次有效 Set/反馈 Tick/显式同步时生效；Diagnostic 本身可只读查询。主条和慢尾都执行相同入口，诊断带“主条：”或“慢尾：”及 Slider fillRect/具体原因。无自动改写方向、method、origin 或比例。

新增八用例独立执行确认错误 origin、轴、Radial360、preserveAspect 的拒绝/恢复；补充探针另外用 Radial90、越界 origin=2，并在**不提交新业务值时**直接推进运行中 Tick，确认主/慢失效当场被识别、无效 slider.value 与 fillAmount 不被组件写入。随后 SetValue(30,100) 仍得到 Current=30、Max=100、n=.3、真实文字 30%，而无效目标保留作者状态。

主目标失效时，已借入且正处于 Fade 的白图隐藏、恢复基础色，两类反馈结束；仅慢尾失效时，主图 .3、白图仍启用并独立运行。探针没有把无效图形残留当作组件的有效反馈。

### 3.2 原始复现、未借入与别名清理

依原 A1 构造 RTL Slider + Horizontal/origin0 有 Sprite 的主图。本轮预先把作者 slider.value 设 .23，再配置组件并输入 100→60：得到明确方向诊断、真实 Current=60、无反馈动画，slider.value 仍 .23。该夹具实际主图仍有 4 顶点（x=-200..-108），这是原生作者图形的保留状态，并非组件接受为 60% 显示；没有再次出现“Diagnostic=null 且组件写 .6 后与白损失反馈重叠”的行为。

此时 White 从未有效借入，_ownedWhite=null，其作者 enabled/color/anchors 原样保持；不能以作者仍 enabled 判断为本组件反馈失败。修正 origin 后 ConfigureTargets 按最新 .6 同步，白图才进入有效借入并隐藏，不重播旧损失。探针对空网格显式返回顶点数而不对空集合 Min/Max，避免把 A1 旧复现程序的潜在异常误当产品异常。

另将一个已经借入且淡出中的白图通过引用变化成为主 Graphic 别名，再 Tick/Set：诊断失效并结束反馈，但 alias 的 enabled/color/anchors 均不被清退路径误写。该情景直接覆盖“为清理而损坏新主目标”的风险。

### 3.3 显式恢复、同值去重和合法网格

五种错误×主/慢两角色修正后，先同值 SetValue(30,100)：目标保持原停止写入状态，证明没有放宽去重合同迁就探针。再分别用 ConfigureTargets（主条分支）和 SyncImmediate（慢尾分支），目标按最新 .3 恢复、白图隐藏且基础色还原、IsAnimating=false、文字 30%，不重播旧损失。

合法 Filled/Simple/Sliced × 四方向的 12 用例实际调用 Image.OnPopulateMesh / VertexHelper，把主图和白图顶点换算至同一完整轨道：LTR/BTT 主图 [0,.6]、白损失 [.6,1]；RTL/TTB 主图 [.4,1]、白损失 [0,.4]，另一轴覆盖完整轨道。验证了实际 Sprite 几何和非重叠，未仅凭 Slider.value 或白 anchor 判断。min=-50/max=150 时 .6 映射到 70、onValueChanged=0；独立恢复探针 min=-37/max=263 时 .3 映射到约 53.0000038、通知 0。极大有限范围的原有算法未变，A1 已覆盖的极值结果保持其证据边界。

因此 **BPP-A01 关闭**。这是本对话的独立结论；S2 的 35/35、1/1 和自检 PNG 不计入本轮新运行数量，也不单独作为关闭依据。

## 4. 本轮可视结果和中心链路边界

五张新 1000×640 PNG 和 six-frames.json 保存在 [evidence/recheck-a2/visual](evidence/recheck-a2/visual)，实际时刻为 0 / .191428 / .313167 / .680511 / 1.081699 秒。六行依次为 slow 关 Disabled/Shrink/Fade、slow 开 Disabled/Shrink/Fade，全部 FormalPage=false。

独立查看 .19 与 .68 图，并对五图×六行重新执行像素断言：x100–579 每行连续 480 个主区像素均为绿色 (28,184,128)，文字为真实 60%；.1914 秒 Shrink 白尾约收至 x727、Fade 保持损失几何并露出背景/慢尾；.3132 秒白图全部退休；.6805 秒三条慢尾仍可见；1.0817 秒损失区全部回到背景 (22,28,34)，无残影。逐段颜色见 visual/independent-pixel-assertions.json，采集指纹见 visual/collection.json。

中心链路沿用 A1 已独立审阅的真实中心预检/创建/保存/再生成、Bar0–Bar5 强类型字段、编译和 FormalPage=true 原始轨迹。本轮复读 center-create.json、center-regenerate.json、center-snapshot 内生成类/用户类及 visual-formal-final 的 FormalPage=true 记录；受保护 Bridge/生成器/注册/旧 Item/GUID 未变，S2 改动只收紧内部 Filled 校验，不影响该合法 Image 页面。**本轮未再次中心创建或运行 Page**，不能把新夹具截图描述成中心复验。暂停、池化、旧 Item、BPP-A02 Undo 没有机械重跑，未受影响部分承接 A1。

## 5. 逐 AC 当前结论

| AC | 当前结论 | 本轮证据与支持边界 |
| --- | --- | --- |
| AC01 | **通过；关闭 A1 的失败** | BPP-A01 原错误配置被诊断并停写；12 实际网格路径、任意 min/max/WithoutNotify、最新值恢复独立通过。normalized/数值 API 和正式强类型中心链路承接 A1 无改动部分；不声称再次中心运行。 |
| AC02 | 通过 | 新六组合实际慢尾时序/终态与真实主条通过；无白引用纯慢尾及暂停规则承接 A1，未重跑其余 Play 方法。 |
| AC03 | 通过 | 新 Shrink/Fade 实际损失区、alpha、白图退休及主失效基础色恢复通过；连续受击/无慢引用专门边界承接 A1。 |
| AC04 | 通过 | 新六组合五时刻真实文字/主区逐像素不被白覆盖，合法四方向 Sprite 主图与白损失图边界匹配；慢尾失效时主/白独立。 |
| AC05 | 通过 | 独立失效/恢复探针文字始终为真实 30%，新 Canvas 文字 60%，共享字体/配置逐字节无新差异；格式/本地化 Key 其余规则承接 A1。无全局语言/皮肤签收。 |
| AC06 | 通过 | 运行中失效不重播旧损失，Sync/Configure 恢复最新值，同值 Set 去重未被放宽。连续输入、回血/上限/模式和真实 Play Undo 结论承接 A1；BPP-A02 仍是不成立的历史疑点。 |
| AC07 | 通过 | 已借入白图退休、从未借入不冒认、alias 清理不误写，以及显式同步/重绑恢复通过；生命周期/never-active lease/旧 owner 等承接 A1，未新增全局更新任务。 |
| AC08 | **通过；关闭 A1 的失败** | 内部 Filled 方向/origin/轴/radial/比例不兼容均诊断且停相应写入，真实数值/文字保持，恢复无旧反馈；合法 Sprite 顶点有界。其他数值/时间/销毁边界的代码未变，承接 A1 定向结果。 |
| AC09 | 通过（受影响部分复核） | 四交付文件外产品与 meta 不变，API/序列化/asmdef/旧包装/Bridge/生成器/旧 Item/GUID/模板保持；中心证据如上承接 A1，没有本轮再中心操作。 |
| AC10 | 通过 | 本轮新 20/20 Edit、1/1 Play、独立断言、新 PNG/像素、主动编译及 Console、Inspector 接线与手册复核齐全。A1/S1/S2 失败及夹具错误原始记录均保留；未把自动渲染记成人工/设备/构建签收。 |

支持范围仍按 D1/随包手册：展示专用、非交互/无外部双写，四方向同向完整轨道，Slider 内部 Filled 匹配 H/V/origin 且无 preserveAspect，Simple/Sliced stretch；独立主 Image 仅合法 H/V Filled。radial、相对斜切、竞争布局等继续明确不支持并诊断；无新需求/范围缩减。人工输入、设备、Player 构建、全工程/长时测试、全局字体皮肤/语言切换和模板操作均非本轮签收内容，不形成必需待验证项。

## 6. 最终现场与下一步

final-unity.json / final-editor-state.json：FrameworkScene dirty=false、timeScale=1、临时增强条实例 0、PageLoaded=false、ProbeLoaded=false，非 Play/编译/更新，Console 错误 0；Runtime/Edit MVID 与核对后的 S2 保持。独立 preview scene/字体/纹理/材质均已在 finally 中释放，无新增探针 C# 资源需删除；未清理本任务目录。

必要项已满足，更新 handoff 至 H6，交用户回入口收束：任务目录 `.utmp/ember-workflow/20261117-buffered-progress-bar/`，正式归档候选 `docs/tasks/20261117-buffered-progress-bar/`。入口仍须重新核对当前工作区及验收后是否有相关变化，再归档有效需求/方案/交付/逐 AC 结论、支持边界和必要证据，承接完毕后标完成并精确清理。本对话不提前归档、清理或创建/发送其他对话。

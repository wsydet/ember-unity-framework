# 增强缓冲进度条实施 S1

- 任务：20261117-buffered-progress-bar；日期：2026-11-17，Asia/Shanghai。
- 实施对话：01a1e56f-7213-70e0-9645-4951592eae6a；hostId=local；沿用当前 checkout，无分支/worktree。
- 有效输入：requirements.md R1、design.md D1、handoff H2（原文见 evidence/handoff-H2.md）。R01–R08 未裁剪，全部 AC 交另一对话独立判断。
- 状态：实现、必要增量自检、临时中心 Page 制作/运行/清理和文档已完成，待独立验收。没有提交、发布、版本/模板操作，不标整体完成。

## 代码归属与合同

新增公共 Runtime `Packages/com.ember/UIExtension/Runtime/Behaviour/EUIBufferedProgressBar.cs`，同文件包含紧密相关 White/Text 枚举；独立 Editor 为 `UIExtension/Editor/UIExt/EUIBufferedProgressBarEditor.cs`。新增 `Tests/EditMode/EUIBufferedProgressBarEditTests.cs` 和 `UIExtension/Tests/PlayMode/EUIBufferedProgressBarPlayTests.cs`。各 .meta 由 Unity 生成，最终指纹见 evidence/implementation-owned-final.json。没有修改 asmdef、旧 EUIProgressBar/EUIComponent、Bridge/生成器、TMPEx、Loading 或旧进度 Item。

组件为 MonoBehaviour companion、AutoDetect=false，组合现有 Slider/Image；数值、显示和生命周期按 D1。主条即时写真实 n，慢尾与白条各自 hold/linear 时钟，单 LateUpdate 无异步/全局订阅。白条专用矩形 [n,w] 四方向锚点，结束恢复基础色并隐藏；Fade 已开始后的二次受击仅显示新损失，不复活旧区。输入模式/maximum 变化和增值立即同步，重复同值不重启，各开关/模式/参数只重置对应效果。

公开 SetNormalized、SetValue(current,maximum)、ConfigureTargets、SyncImmediate、Clear、RefreshText、效果/时钟/文字配置及只读 CurrentValue/MaxValue/NormalizedValue/IsAnimating/Diagnostic。Slider 按原 min/max 通过 WithoutNotify 写入；极大有限区间用 double 避免映射溢出，并修复原生 Slider float 范围差溢出时的 fill/handle 锚点。H/V Filled Image 各两 origin；不支持的 radial、交互/整数 Slider、共享目标、非法白条和错位轨道提供诊断。Inspector 分组、多选/Undo、对应效果复位、明显同级排序检查和 Play 立即同步入口均在独立 Editor。

文字仅真实值；Percent/CurrentOverMax/Custom，精度0–4、InvariantCulture/AwayFromZero，非法格式回退。TMPEx Key 非空不写；空 Key 用 SetSource，不改字体/皮肤/全局翻译源。Clear 可由 lease.Track 注册，不依赖 never-active 对象的 OnDisable/OnDestroy。

未发生需要 D2 的数值、时序、模式或范围变更。内部拆分选择同文件枚举；实际制作输出与 D1 预期路径相符。测试补充主条销毁后的专用白图清退修复，属于 D1 已定生命周期合同。

## 环境、备份与保护

MCP 实际 UPM 探针确认 embedded com.ember 0.25.5 来自本仓库 Packages/com.ember；Unity 6000.5.4f1。Git 为 codex/local-snapshot-20261111-162819 / 16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2。正式编辑 topdown-2d 0.1.2 / d83863b16bda8c2655f21d6aa3d55693，未切换模板。

实施开始重新备份182个现有源/字体/配置及 .meta 的实际字节，清单为 evidence/implementation-source-baseline.json，字节在 evidence/implementation-backup。入口52文件开始全部匹配；结束51个不变，唯一预期变化是授权增加导航的 docs/dev/ember-api-reference.md。182基线中结束只有三份授权文档有差异：API速查、ui/README、runtime-extensions。保护报告/最终 Git 状态见 evidence/protection-final.json、workspace-final.txt。

SharedAssets/Fonts 内部34个文件（含子目录meta、字体/atlas相关asset及TTF）逐字节匹配，新增0、变化0，见 evidence/font-final-report.json。相邻 Fonts.meta 另存 evidence/Fonts-root.meta；它不在最初182字节备份内，但初始工作区为正常受跟踪且无差异，最终同HEAD、无skip-worktree，记录此证据边界，不把后补字节冒充初始备份。项目相关字体/配置也在182清单内且不变。

所有新增文字在 inactive 配置独立 FontAsset/atlas/material、独立空 fallback、TMPEx手动皮肤和空Key后激活。原生TTF只读；中心验证字体为独立静态资产，其余为测试持有对象。先销毁实际Text/Canvas再销毁持有字体/atlas/material；无全局 SaveAssets、UnloadUnusedAssets、TMP_Settings改写或皮肤切换。未恢复任何共享字体，没有引用上一任务临时目录。

## 编译与测试证据

每批 C#/生成/清理均通过 MCP `refresh_unity compile=request` 主动请求，随后核对本轮 compile/domain reload 和新API/MVID/生成类卸载，读取 Console 后再测试。编译原始记录为 evidence/compile1.json、compile2-failure.json、compile3.json、compile4.json、compile5.json、compile6-registration-restored.json、compile7-cleanup.json、compile8-final-tests.json。

首次刷新等待60秒超时，但已送达；没有重复提交，30秒恢复查询确认 started=1794850896074、finished=1794850906896、reload=1794850913214。后续域重载短暂 ping 未答均在一次30秒重试后恢复，没有耗尽重连，没有用 Editor.log/BatchMode/dotnet 替代验证。最后测试程序集编译 started=1794851936226、finished=1794851940028、reload=1794851945711，Console错误0，新文字测试已加载，临时Page类型不存在。最终运行现场和 Console 另见 evidence/final-unity-state.json。

最终过滤与结果（不累计多次重跑）：

| 范围 | 最新结果与证据 |
| --- | --- |
| Ember.UI.Tests.EUIBufferedProgressBarEditTests | 30/30，0失败/跳过，evidence/edit-delivery.json |
| Ember.UIExtension.Tests.EUIBufferedProgressBarPlayTests | 4/4短方法，0失败/跳过，evidence/play-delivery.json；清理后用独立可重建夹具 |
| 最终Runtime上的正式Page六组合+生命周期 | 2/2，evidence/play-final-targeted.json；FormalPage=true，更新PNG/轨迹在visual-formal-final |
| 正式Page下的scaled/unscaled暂停 | 前一正式3/3中覆盖，evidence/play-final-formal.json；后续只修主目标丢失清退，不改时钟 |
| 旧Loading/Library/DynamicRow进度Item与真实lease | evidence/legacy-and-lease-final.json、legacy-and-lease.tsv及可重放cs.txt；0/.5/1实际Slider/文字/布局/GUID；never-active lease.Track(Clear)幂等 |

没有运行整个程序集/全工程或长时测试。第一次 Edit 18/22通过，四失败为70.0000076与70精确比较，改为.0001容差后通过；历史在edit-first.json。编译曾因测试误用ShowViewOnly失败，按真实RestoreViewOnly修正。旧回归探针第一次错把DynamicRow固定50%文字当作应更新，源码显示其SetProgress只改Slider，修正探针契约并保留legacy-and-lease-first-failure.json；最终记录证明该文字与布局维持原状，不改旧业务行为。

## 正式中心接入与可视判断

TryBuildPlan/Create 创建唯一 Business Page `EUIBufferedProgressValidationPage`，ClassPath=Module/BufferedProgressValidation。预检/创建/绑定快照为 evidence/center-preflight.json、center-create.json、center-regenerate.json。通过 PrefabContents 追加六行、独立文字/图片并保存，再调用 TryRegenerateCode；读取真实生成 Bar0–Bar5 字段后才编写用户文件 SetAll、OnHide同步、OnDispose Clear。编译证明六个字段类型均为 Ember.UIExtension.EUIBufferedProgressBar；Play实际 EUIBindingBridge.Attach → EUIPage.Init/PlayShow → 生成字段 SetAll → HideViewOnly/RestoreViewOnly，并核对组件身份。

真实 WorldSpace Canvas/隔离Camera/RenderTexture，1000×640，六行slow关/开 × white关/Shrink/Fade。t≈0/.19/.31/.68/1.08的PNG、真实时间、几何/alpha、文字和像素全部保留；视觉判断见 evidence/visual-formal/report.md、pixel-report.json。关键事实：主条边界一直x580（.6），白区只在损失区；.192秒Shrink尾x729，Fade区alpha≈.469；.313秒白图隐藏并恢复色；.685秒慢尾边界x732；1.080秒反馈全结束。每行60%可见且主区未被白覆盖。最终Runtime正式复验与清理后夹具证据另存，不能把FormalPage=false夹具重建误称中心验证。

中心报告、生成代码、Prefab/GUID、所有独立资产和完整创建清单已保存在 evidence/center-snapshot、center-owned-manifest.json、center-created-files.json；可重建制作配方为 center-layout.cs.txt。

清理前核对精确路径、字节所有权及无reparse point；用户注册仅有本任务新增块，因此从实际创建前备份恢复。AssetDatabase 删除两个明确新建模块目录及meta。execute_code通用DeleteAsset模式检查首次阻止执行，随后在已经验证所有权的精确目录上显式关闭该工具模式检查并成功删除，未绕过路径/所有权核对。无审批自动审查拒绝或待用户批准动作。

清理后MCP主动编译 started=1794851795616、finished=1794851801276、reload=1794851807858，临时PageLoaded=false、Resources=false；原有UI/配置清单差异0、新增0、注册恢复原字节，见cleanup-preflight.json、cleanup-filesystem.json、compile7-cleanup.json。没有留下常驻演示页、探针或生成文件；本任务临时目录和必要证据保留等待入口收束。

## 逐项自检（不是独立验收）

| AC | 实施自检判断与证据范围 |
| --- | --- |
| AC01 | 符合D1：normalized/当前上限、任意Slider实际范围及四方向、Image H/V两origin；实际中心Page强类型字段/Bridge调用与主条.6渲染 |
| AC02 | 独立无白慢尾hold/追赶、当前尾端继续、scaled暂停/unscaled继续，实际几何/PNG |
| AC03 | 无慢Shrink/Fade、hold合并、Fade中二次新损失替代、完成隐藏恢复色；实际白矩形范围 |
| AC04 | 六组合外部目标与五时刻Canvas渲染，主区像素未覆盖，各层独立终态 |
| AC05 | 可选文字/百分比/当前最大值/Custom/非法格式、真实值立即刷新、精度、安全Key所有权；单实例ApplyLocalizedText刷新不改全局；共享字体无新增写回 |
| AC06 | 同帧/连续/同值、回血、上限/输入模式/参数/开关变化、重新绑定和淡区退休；外部填充/锚点/alpha断言 |
| AC07 | Sync/Clear、inactive输入、禁用/重显、实际never-active lease清理、目标销毁；新增主条丢失时白图清退测试 |
| AC08 | 非有限/非法上限、极大finite、零/非法时钟、零轨道、缺失层、共享fill/Graphic、非法白Track、Slider整数/范围、radial拒绝；有限几何与可恢复性 |
| AC09 | 真实中心生成/编译/Bridge/页面生命周期，添加companion后同节点Slider/Image/ImageEx自动选择保持；旧包装任意Slider历史语义保留；三个旧Item实际回归，受保护代码/GUID未改 |
| AC10 | 定向MCP编译/域重载/新程序集、测试原始结果、真实PNG/像素/轨迹、清理/字体报告、随包手册与链接检查具备；待另一对话独立验收 |

长期使用说明为 Packages/com.ember/Documentation~/ui/buffered-progress-bar.md，ui/README、runtime-extensions和API速查仅加最小导航。四份文档本地链接检查0断链（evidence/document-links.json）。不支持radial、交互/外部双写、相对斜切或竞争布局；没有人工、设备、构建、全工程或全局语言/皮肤切换签收。TMP实例语言回调覆盖不等于全局本地化集成回归。

## 下一步

使用更新后的 prompts/04-acceptance.md 在同项目local创建独立验收；创建和启动结果记evidence/acceptance-dispatch.json。派发后本对话停止产品与共享交接修改，只记录自己派发元数据。独立验收逐条重新判断，不照抄上表；完成后回入口收束，本阶段不归档/清理任务目录。

## S2：BPP-A01 定向返工（2026-11-17）

用户在 A1 验收对话明确要求“按定向返工提示词修复 BPP-A01，保留失败历史和现有改动，完成相关增量验证后再交独立定向复验”。因此本对话转入授权返工；本节为实施自检，不能自行独立关闭 BPP-A01。A1、R1、D1 及既有失败证据未改，原 S1/H4 实际字节在 evidence/rework-s2/backup 中保留。

状态：修复与相关增量自检完成，**待另一对话独立定向复验**。A1 的 AC01/AC08 失败历史仍有效，后续 A2 判断是否关闭；不改写成 A1 已通过。不提交、推送、发布或操作模板，不归档/清理任务目录，不创建或发送其他对话。

### 修复与范围

仅修改四个交付文件，准确差异和最终 12 文件指纹见 evidence/rework-s2/product.diff、owned-final.json；不覆盖原 implementation-owned-final.json。

| 文件 | S2 变化 |
| --- | --- |
| UIExtension/Runtime/Behaviour/EUIBufferedProgressBar.cs | Slider 的 fillRect 若为 Filled Image，复用已有 Image 有限支持校验，要求 H/V、合法 origin、preserveAspect=false，再核对实际填充方向与 Slider.direction 一致；radial、方向不符、保持比例均明确诊断。沿用既有主条失效停止两反馈、慢尾失效仅停慢尾、真实数据/文字保持及 Sync/重绑恢复路径，不改作者配置。 |
| UIExtension/Editor/UIExt/EUIBufferedProgressBarEditor.cs | 补内部 Filled 的方向/比例前置说明；既有 Diagnostic HelpBox 显示主/慢条的具体错误，无 Runtime→Editor 依赖。 |
| Tests/EditMode/EUIBufferedProgressBarEditTests.cs | 新增 12 个实际 Sprite 网格情景（Filled/Simple/Sliced × 四方向），把主图和白损失图顶点换算至同一完整轨道，核对边界、互不覆盖、任意范围映射及 WithoutNotify；新增 8 个主/慢条错误 origin、轴、radial、比例拒绝/退休/文字数据/配置不被改写/Sync 恢复情景。测试独立 Sprite/Texture 在销毁实际引用后释放。 |
| Documentation~/ui/buffered-progress-bar.md | 补 Slider 内部 Filled 的四方向 method/origin 表、比例限制、主/慢目标失效行为，以及修正后显式 SyncImmediate/ConfigureTargets 恢复；Simple/Sliced stretch 和旧 ProgressBar 支持不变。 |

无公共 API/字段序列化/asmdef/Manager/依赖变更，无旧 EUIProgressBar、Bridge/生成器、Loading/旧 Item、GUID 或模板内容变化。本修复落实 D1 的同向/诊断约束，不变更 R01–R08，也不要求 D2 缩减支持。

### 保护与编译

返工开始先实际备份 201 文件，含 34 个共享字体内部文件及相邻 meta、项目 FontSkins 配置/生成项、TMP Settings.asset、DefaultFontSkins.asset 及各自 meta，以及交付/保护源码和阶段基线；清单在 evidence/rework-s2/baseline.json，实际字节在 backup。另对 90 个既有证据根文件留指纹，保留 A1/失败历史。没有用补存或 HEAD 冒充本轮初始实际备份。

当前保护核对只有本轮四个产品/文档文件和允许追加的 implementation/handoff 变化，其他保护文件逐字节一致；旧证据指纹变化 0，共享字体新增 0。完整结果见 protection-final.json。所有字体/图形测试只创建独立对象，没有全局语言/皮肤切换、共享源写回、TMP_Settings 修改、SaveAssets 或 UnloadUnusedAssets。

源码批次通过 MCP 显式 compile=request。started=1794882676186、finished=1794882685840、domainReloadAfter=1794882695610；Runtime MVID=42cc011e-b222-4652-ac3b-501b8486b6e0，Edit MVID=0f75e22c-c505-40a3-ac90-d74be143a202，新网格/拒绝测试方法实际加载，Console 错误 0 后才测试。初次状态 ping 未答，严格另等 30 秒后第一次重试恢复，没有重复提交编译请求。原始请求、未答状态、完成事件和加载证据在 compile-request.json、compile-first-state.json、compile-completed.json。

### 实际自检结果与边界

| 范围 | S2 结果 |
| --- | --- |
| 精确 EditMode 过滤 | 35/35、0 失败/跳过，约 1.174 秒。新增网格 12 + 拒绝/恢复 8；相关旧 Slider 四方向 4、六组合 6、文字 1、极大范围 1、原生自动选择 3。过滤具体方法及逐例结果在 edit-result.json。不是整个类（当前 50 情景）、程序集或全工程。 |
| 精确短 PlayMode | 仅 SixCombinations_RealCanvasFramesAndGeneratedPageLifecycle，1/1、0 失败/跳过，约 1.276 秒；play-result.json。清理后独立夹具 FormalPage=false，本轮没有中心重新制作。 |
| 可视自检 | visual 下五张 1000×640 实际 Canvas PNG/轨迹；复用并实际执行像素断言脚本，对五时刻×六行连续 480 个主区像素、白条退休及无终态残影均通过，independent-pixel-assertions.json。本轮执行性质是 S2 自检，不是新的独立验收。 |
| 最终现场 | FrameworkScene dirty=false、timeScale=1、临时增强条 0、PageLoaded=false、非 Play/编译/更新、Console 错误 0；final-unity.json。未增加临时 C# 探针或中心 Page，无额外源码清理批次。 |

S2 没有测试失败；MCP 未答及恢复原始记录保留。S1 的 float 断言/编译/旧文字误判和 A1 的产品失败/夹具错误/Undo 排除历史全部保持，未累计重跑次数为覆盖数量。未重跑未受影响的暂停、池化和整个旧 Item 回归，不作新的人工、设备、构建、全局皮肤或中心运行签收。

### 交独立定向复验

提示词为 prompts/06-acceptance-recheck.md。下一对话先读 R1、当前真实实现、A1，再核对 S2/新指纹与本轮保护，独立判断 BPP-A01 与 AC01/AC08，不把本节通过表复制成独立结果。A1 其他结论只在受影响范围内复核，保留 A1 原文；写 acceptance-recheck.md 并更新 handoff。必要项独立满足后才回入口收束。

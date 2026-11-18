# UI 运行时扩展独立验收 A1

- 任务：20261116-ui-runtime-extensions；有效需求 R1（R01–R06、AC01–AC10），方案 D1。
- 日期：2026-11-16，Asia/Shanghai；本记录由独立验收对话形成，区别于 implementation.md S1 自检。
- 结论：**需返工，不能整体签收、归档或清理任务目录。** AC01–AC05、AC07–AC09 在下述范围通过；AC06 失败；AC10 因共享字体资产保护未决而待验证。未降低需求或改产品实现。
- 问题：UIRT-A01 / P2，never-active 仅移除 loader 时释放 token 而未卸下 Graphic 引用；UIRT-A02 / P2，已有字体与本轮 atlas 写回归属/保留策略未决。
- 自动输入、测试和截图检查不等于人工或设备签收；没有执行真机、构建、全工程测试、模板保存/Bump/部署、提交或发布。

## 1. 独立判定方法与实际基线

先读 AGENTS.md、CLAUDE.md、包内公共 ai-workflow/ai-handoff-template、架构/隔离/编码/glossary、完整 unity-validation 和活动 topdown-2d 字典；先以 R1 和当前 Resource/UIExtension 源码判断，再对照 D1/S1 及原始证据。没有仅复制实施逐项“通过”。

Unity MCP 实时确认工程 C:/Users/wuyu/My/ember-unity-framework，Unity 6000.5.4f1、com.ember Embedded 0.25.5，解析位置 Packages/com.ember，uGUI 2.5.0。分支 codex/local-snapshot-20261111-162819，HEAD 16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2。Assets/Editor/EmberEditingTemplate.json 仍为 topdown-2d 0.1.2 / d83863b16bda8c2655f21d6aa3d55693。未新建 worktree/分支，未操作模板或 PackageCache。

进入验收及最后核对：owned-files.json 的 61 个文件 SHA256 均匹配；protected-baseline.json 中 13 个非本任务文件均匹配，含前轮按钮、列表/Bridge、资源 Manager、模板身份及 package.json。28 份实施前备份均匹配 backup-manifest.json。既有 .meta 未被本阶段改动。这里只能对已列指纹作保护结论，不能以 Git status 相同推断任意文件内容不变。字体是单列例外，见 UIRT-A02。

证据：evidence/acceptance/workspace-baseline.json、workspace-final.json、backup-integrity.json、live-baseline.json。原 H3 保留为 evidence/acceptance/handoff-H3.md。

## 2. 本阶段实际执行

| 范围 | 过滤及方法 | 结果 |
| --- | --- | --- |
| 资源/图片/绑定/圆角/布局 EditMode | EmberAssetHandleEditTests 4；Slot 的同步完成/重入释放 1；图片 ABA/失败重试 2、inactive 同步/重入 2、ImageEx override 1、SceneUI adapter 1；Binding 5；圆角/多图 4；真实 LayoutGroup 1 | 21/21，4.714s，job 34dcfa2173cb4d51ba40d01751d75492；acceptance/edit-result.json |
| 滚动几何补证 EditMode | BeginEnd_ProjectActualContentEdges_ForPivotAndAnchors 6；TargetAlignmentUsesProjectedEdgesAndViewportOffset 6；SmallOrZeroContentAndLayoutChangesStayFinite 3；PixelDirectionAndVisibilityUseAreaAndRejectForeignTargets 1 | 16/16，0.807s，job 7bf56229c8134d6189e787bba3063f0f；acceptance/geometry-result.json |
| 短 PlayMode | 原 NeverActiveDestroyOrComponentRemoval 1（内含 8 组合）；原 ScrollAnimation_InputInterruptionSnapAndReentrantReplacement 1；独立 RemovingNeverActiveLoader_UnmountsOwnedGraphicBeforeRelease 1（Sprite/Texture 2 条路径） | 2 通过、1 失败，1.915s，job 175cb94ebc8d483397dff655be264d0e；acceptance/play-result.json、component-removal.json |

因此本阶段为 **37 个 EditMode 情景通过；3 个 PlayMode 方法中 2 通过、1 失败**。不是重新跑了实施的 92+6，也不是全量。原始 motionTests1/imageTests1 只枚举失败明细，不能以空的成功明细列表代替逐例覆盖；为此补跑滚动几何，圆角在首批已重验。结合历史原始结果和本轮补证，98 个既有情景（92 EditMode + 6 PlayMode）均可定位最终通过结果；这仍不能抵消新增验收探针发现的失败。明细索引为 acceptance/historical-evidence-audit.json。

临时探针仅为独立验收复现，未修复产品。先在 UIExtension/Tests/PlayMode 临时加入，完成后移除 .cs 及新 .meta；可复用源码保留为 acceptance/EUIRuntimeAcceptanceProbe.cs.txt。初次脚本范围编译未导入新文件，随后显式全范围刷新；探针使用本项目 NUnit 缺少的 Assert.Multiple 产生一次编译错误，已改为普通断言，之后主动编译确认类型与新测试程序集 MVID 94a6b03e-a8b6-4276-bc08-8427bed9faba，再运行探针。此错误只属于验收探针，不属于产品缺陷；没有将失败编译算作通过。

探针移除后再次 MCP 主动 compile=request，确认 last_compile_started=1794842642283、finished=1794842647844、domain_reload_after=1794842653156；loaded probe=false，产品 Runtime MVID 仍为 43403601-0519-40c9-a905-19b6876bccd7，PlayMode 测试程序集恢复为 423dd852-f22b-4295-9aa9-56a266f1795c，Console 错误 0。相关请求/状态/程序集/Console 分别在 acceptance/probe-*.json 和 cleanup-compile-*.json。域重载断连均在间隔 30 秒后的首次恢复查询连回，无重复提交结果不明请求，无十次耗尽。

结束时非 Play/非编译/非更新、FrameworkScene、timeScale=1、ResourceManager 与 IResourceProvider 服务无测试残留；最终 Console 错误 0。临时探针及 .meta 已不存在。没有长期测试依赖本任务目录。

## 3. 逐项 AC 结论

| AC | 结论 | 独立判断、证据与实际边界 |
| --- | --- | --- |
| AC01 | 通过 | 本阶段重验 17 个真实 ScrollRect/Canvas/LayoutGroup 几何情景：横纵首尾、pivot 0/.5/1、stretch、嵌套目标、三种对齐/正负 offset、零/小内容、布局高度变化、像素方向及有面积可见性。预期按投影边/中心断言，未镜像 normalized 算法。源码仅按 content 自身 rect 和 viewport 投影计算、限制开启轴。证据 edit-result/geometry-result；原横纵首尾 4 张 PNG 已独立查看。相对旋转/斜切为 D1 已声明边界。 |
| AC02 | 通过 | 本阶段短 Play 真实 ExecuteEvents 拖拽/松手/wheel、Scrollbar、timeScale=0、时长终态、替换/重入、TargetLost、等距顺序、惯性等待及边界吸附通过；审查 Begin/Finish 的代次和 guarded expected position，成功事件不会从旧请求迟到污染。acceptance/play-result.json、原 completion-play-tests.json、scroll-events.txt；截图已查看。为自动事件输入，未作人工设备操作。 |
| AC03 | 通过（行为范围） | 先审查当前 TMPMarquee：公开 OnPreRenderText 捕获各 materialIndex 基线，UpdateVertexData 上传，关闭/禁用恢复原几何；有限速度/停顿规范化。completion-edit-tests.json 5/5 和 completion-play-tests.json 最新跑马灯行为覆盖短/长文本、速度暂停/恢复、往返、首尾停顿、NaN、禁用复位。两张 Mask 截图和 marquee-mesh.json 已独立读取，4 材质 x 位移一致。源码没有布局或共享材质/Shader 写操作；**不能把此行为通过解释为共享字体文件未变**，字体保护由 AC10/UIRT-A02 保留待决。为避免再次污染共享字体，没有原样重跑切全局字体皮肤的 Play 夹具。 |
| AC04 | 通过（声明支持范围） | 原生 TMP 的 EditMode 与 TMPEx Play 夹具分别核对，改文本/宽度/margin、SetFontSkin/全局皮肤、真实中文+fallback 符号+sprite glyph 和 Mask/RectMask2D 均有实际测试和最终 PNG。独立查看两张图可见中文、三角 fallback、青色 sprite 及右端裁剪；mesh 4 材质实际共同位移。D1 在实施前排除 3D/RTL/多行及多顶点位置效果叠加；这些不适用子项不包含已声明支持的 fallback。源字体保护仍见 UIRT-A02，未声称其无变化。 |
| AC05 | 通过 | 本阶段 5 个 Binding 情景实际走中心生成文件、精确 Extension/Bridge、EUIPage Logic 调四种新 API；图片同步/inactive/重入和失败路径定向通过。当前源码覆盖 Sprite/Texture 共用状态、空地址清空、placeholder/keepPrevious/failure、NativeSize。imageTests3.json 中 EUIAsyncImageEditTests 14 例最终通过，包含真实 ResourcesProvider 导入 Sprite/Texture、同步显示、清理后共享资产仍存活及未初始化 Manager。没有把同次其他类的早期失败当成图片成功或失败。 |
| AC06 | **失败** | 多数竞态、同地址去重/ABA/失败重试、手动赋图和回调重入证据有效，本阶段再次验证相关情景；原 never-active 8 组合也通过。但独立探针表明 only-loader removal + never-active + 成功资源时，Graphic 仍挂着已释放 token 的 Sprite/Texture，违反先卸引用再释放的必要合同。见 UIRT-A01。现有释放计数测试没有该断言，不能以它通过覆盖缺陷。 |
| AC07 | 通过 | 独立审查 Create/TryComplete/Cancel/Dispose、Slot Ordinal/代次/失败重试/finally、Resources 仅放引用；本阶段 4 Handle + Slot 同步重入 + SceneUI adapter 通过，历史资源 10、ResourceHost 三例与真实 ResourcesProvider 共享资产存活证据已核对。取消先终态、cancel/release 独立、迟到 TryComplete false 后 Provider 仍负责后端 token。未交付 Addressables/YooAsset，未扩文件/场景合同。UI Graphic 卸载缺陷单独计 AC06，未伪装成后端问题。 |
| AC08 | 通过 | 本阶段 4 个多图/圆角情景：SerializedObject 多选/Undo、0/1/非法/空项/空数组回退，IsActive、NaN/极值半径、三角数、零尺寸、preserveAspect、非 Simple 直通。源码保留序列化字段/GUID，OnEnable 刷新、Inspector Apply 刷新、半径变更 dirty。两张真实青色异步图/品红索引图圆角截图已独立查看；UV/宽高比实现按当前 sprite。仅 Simple、非点击遮罩限制已在使用页和 Inspector 明确。 |
| AC09 | 通过 | 本阶段 5 Binding 情景通过；四者是正式 MonoBehaviour，自身精确 Extension，AutoDetect 默认 true 而四者 false，旧 ImageEx 自动选择仍在。生成文件含强类型声明，实际页面 Logic 经 Bridge 调新 API；未声称运行了已删除临时生成 Binding 的程序集。Runtime → Resource 依赖和 Editor 分离正确，无业务反向依赖、Burner namespace/私有 TMP 反射依赖。regressionTests.json 42/42 含按钮16、列表、FontSkins、Localization、旧 Extension、SceneUI 三例；相应保护指纹保持。不是全工程回归。 |
| AC10 | **待验证 / 资源保护待判断** | 每批实施编译证据与最终产品 MVID 已对照，验收探针加入/修订/移除也主动编译、确认域重载/类型/Console；正式 UI 使用页、Resource 合同、API 索引入链与本地链接有效，本文件完成独立逐项判断。然而未计划编辑的既有字体在实施和验收期间出现写回，UIRT-A02 未解决；不能签收“保留其他工作/资源清理”整体边界。失败 UIRT-A01 亦未修复。 |

没有整条 AC 被因未覆盖而写“不适用”。上述“通过”均仅表示列明范围的证据成立，不表示所有平台、设备、管线或任意非法布局均已验收。

## 4. 返工问题 UIRT-A01：移除未激活 loader 时留下已释放显示引用

- 严重度：P2（必要正确性缺陷，阻止 AC06 签收）；状态：已独立复现，未修复。
- 影响：R03/R06、AC06；Sprite 和 Texture 共用 EUIAssetImageSlot，均受影响。
- 预期：never-active GO 只移除 loader，而 Image/RawImage 继续存活时，先卸下 loader 拥有的 base sprite/override/texture，再恰好释放一次 token；保留任何外部借入 override。
- 实际：两种类型均 releases=1，但 MountedAtRelease=true 且 MountedAfterDestroy=true。成功 Load 的终态仍只有一次，故“释放一次”检查无法发现这个使用已释放资源的问题。
- 源码位置：UIExtension/Runtime/Behaviour/EUIAssetImageSlot.cs 的 Monitor.MoveNext（44 行）在 !owner 后调用 Destroyed；UnmountOwned（69–70 行）以 owner 存活为前提跳过 detach；Destroyed（152–155 行）继续 Clear/Dispose。Graphic 本身仍活着。具体行号以本轮 owned 指纹为准。
- 最小复现：
  1. 新 GO 立即 SetActive(false)，添加 Image+EUIImageLoader（或 RawImage+EUIRawImageLoader），从未激活。
  2. 使用每请求 token 的同步 Provider Load 成功，把资源挂到 Graphic。
  3. Object.Destroy(loader)，保留 GO/Graphic；等待 3 个真实 PlayerLoop 帧。
  4. 在 Provider release hook 记录 Graphic 当前引用，再检查释放后引用：仍指向拥有资产。
- 证据：acceptance/component-removal.json（两条路径完整观测）、play-result.json（失败堆栈）、EUIRuntimeAcceptanceProbe.cs.txt（可重放源码）。旧测试同批通过，证明不是未触发销毁或未释放。
- 交回实施：按 Graphic 的实际存活处理卸引用，不能以失效 loader 代替显示目标存活判断；确保不访问已销毁 Graphic，不清除借入 override，不重复释放。把新断言纳入长期 PlayMode 回归，再定向复验 pending/同步成功、GO/仅组件、Sprite/Texture、手动 override，主动编译后再交回独立验收。此处只说明修复目标，没有静默改产品。

## 5. 资源问题 UIRT-A02：字体写回不能无依据接受或回退

- 严重度：P2（资源保护待决）；状态：待归属/保留决策，影响 R06/AC10。
- 对象：Packages/com.ember/SharedAssets/Fonts/钉钉进步体/DingTalk-JinBuTi SDF.asset；入口已经是用户未提交修改。
- 实施历史：中途 22:56 SHA256 31A020893A20B28ACA7B1D532A4C31085CEA03D474835D3C13ED5FDE69947625；23:10 变为 EA416AE2392408783BB68D38B2527B8B63D316FB9151006700E2ECABAF381469。没有匹配 31A020… 的完整字节；HEAD 不是任务前基线，不能凭 HEAD 八个额外字符反推精确本轮增量。原 evidence/font-writeback.json 与 font-writeback/ 保留。
- 验收新事实：首批 21 EditMode 后仍为 EA416A…，在后续编译/域重载/短 Play 验证期间变为 1869CB8EFA4FBA1D7AD971639D8E2262BC125528A2D112C48982F1117AB42719。验收有 EA416A… 的完整字节备份 DingTalk-before-probe.asset，及新字节 DingTalk-after-probe.asset；当前保留新资产，没有覆盖原 .meta。
- 可精确比较的本轮差异：前后字符表均 210 项，无增减；只有第一份序列化 atlas 的 _typelessdata 变化，1,048,576 字节中的 4,281 字节由零变非零（653101 → 657382 个非零字节）；另一 atlas 完全相同；其他 YAML 行相同。证据 font-probe-diff.json、atlas-byte-summary.json。
- 来源判断：字体 m_AtlasPopulationMode=1（Dynamic）、多 atlas；本地 TMP_FontAsset 增字路径可登记资源更新，且实施夹具包含直接使用共享字体和切全局皮肤。**动态更新后被较晚序列化是可能解释，尚非归属证明。** 本地 TMP_EditorResourceManager 的更新队列中 SetDirty/SaveAssets 当前被注释，因此不能仅凭队列就断言自动磁盘保存的确切来源；没有完整的写入调用栈。没有因仅增加非零像素就宣称无害或确认像素对应哪些字符。
- 独立验收决定：**不接受为“源字体未变”或自动判为无害。** 保留当前文件、两份验收字节和既有历史证据；不擅自重置用户字体，也不执行猜测反补丁。
- 解决条件：若要恢复实施前/31A020… 状态，必须取得匹配目标指纹的真实旧字节（任务前状态还需更早真实基线），不能用本次 EA416A… 备份冒充；若决定保留当前 1869CB…，需用户明确接受该共享字体现状与无法精确还原早期增量的限制并记录来源。EA416A… 备份只允许讨论验收期间 atlas 增量的精确恢复，不自动授权覆盖。实施应隔离动态字体/皮肤测试对共享源资产的影响，取得前后完整字节及指纹，再进行必要定向验证。

## 6. 后续交接与保留边界

交回实施的可复制文本：prompts/05-implementation-rework.md。先修 UIRT-A01 并补有显示引用断言的长期回归；单独处理 UIRT-A02，已有同范围修复/文档/增量验证授权继续有效。之后回独立验收针对 AC06/AC10 及受影响项复验。

当前不具备入口收束条件。任务目录保留，不标整体完成、不删除证据、不主动给其他对话发消息或创建对话。全部必要项满足后才带本目录及修订后的 acceptance.md 回入口做正式归档/清理。没有人工/设备/构建签收；3D/RTL/多行/TMP 多顶点位置效果、旋转斜切 ScrollRect、非 Simple 圆角及第三方资源后端维持 D1 的明确非目标。

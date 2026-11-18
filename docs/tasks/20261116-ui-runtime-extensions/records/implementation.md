# 实施与自检记录 S2

## S2 实施返工（2026-11-17，Asia/Shanghai）

本节优先于后面的 S1 历史自检摘要。输入 handoff.md H4、requirements.md R1、独立 acceptance.md A1；本阶段只修复和自检，不代写独立验收通过。**UIRT-A01 已修复并通过定向自检，等待独立复验；UIRT-A02 / AC10 仍待用户决策，任务整体未完成。** A1 原文保留。

### 基线、实际改动与保护

- 进入返工时 61 个 owned 和 13 个非本任务 protected 指纹全部匹配；分支/HEAD、topdown-2d 0.1.2/hash d83863b16bda8c2655f21d6aa3d55693 保持。未新建 worktree/分支、未操作模板、版本或发布。
- 实际只修改三个已有任务文件：`UIExtension/Runtime/Behaviour/EUIAssetImageSlot.cs`、`UIExtension/Tests/PlayMode/EUIRuntimeExtensionsPlayTests.cs`、`Tests/EditMode/EUIRuntimeMotionEditTests.cs`（均相对 Packages/com.ember）。完整改前源码和 .meta 在 evidence/rework/code-before/；本阶段指纹/变化见 rework/changes.json。既有 .meta 不变。
- `UnmountOwned` 不再把 loader 的 Unity 存活值作为卸载前提，先取出并清除 `_displayOwned`，再调用自身检查 Graphic 存活的 Detach。两种 Detach 保留借入 override/texture，只清理相等的拥有引用；Graphic 已毁时安全跳过。资源 Clear/Dispose 幂等流程维持。
- 原 never-active 长期用例的 8 组合（Sprite/Texture × pending/同步成功 × GO/仅组件销毁）保留 cancel/release/终态/迟到完成断言，并新增成功加载前置、释放 hook 时及释放后实际 Graphic 引用断言。hook 只记录观测，在 hook 外断言，避免 Provider 异常隔离掩盖失败。新增 4 条两类型借入图/Graphic 失效路径；失效路径先移除 required loader，再销毁 Graphic，于后续 PlayerLoop 清理。

### 字体隔离与仍待决事项

返工前备份全部共享字体目录共 34 个完整文件（含 TTF、.asset 内嵌 atlas、.meta），索引 evidence/rework/font-manifest.json，字节在 font-before/。验证后 34 个源文件与备份均匹配，见 font-after.json。钉钉源始终为 **1869CB8EFA4FBA1D7AD971639D8E2262BC125528A2D112C48982F1117AB42719**，没有覆盖、接受或尝试反补丁。31A020… 的真实旧字节依旧缺失；EA416A… 备份不能冒充更早状态。动态字体延后写回仍只是可能解释，未证实旧写入归属。

TMPMarqueeEditTests 改为从只读 TTF 创建全新 TMP_FontAsset，各自拥有字符/字形表、atlas、material，测试后先销毁文本再销毁字体。Play 跑马灯同样使用独立主字体与 Symbols fallback、固定三角符号和 sprite glyph，产生三材质网格（S1 为四材质，不强行沿用旧数量）。不再 Instantiate 动态共享 SDF 或把共享 fallback 直接交给渲染。

原 Play 用例末段的 `SetFontSkin(0,1)` + 全局 `EmberFontSkins.SetSkin` **暂停**，当前只验证独立 fixture 字体切换、文本/宽度变化。所有 Play 夹具 teardown 也去除未必要的全局 SetSkin，防止图片用例清理时刷新其他活文本。没有声称本轮复验全局皮肤集成；独立验收 A1 的历史 AC04 证据保留，AC10/UIRT-A02 继续待决。后续全局皮肤相关回归需先建立可隔离全局目标的夹具，不能原样跑涉及共享源的旧段落；这不是降低产品验收标准。

### 主动编译与定向自检

执行前声明范围与估时：19 个 EditMode + 5 个短 PlayMode 方法；目标 EditMode 数秒、PlayMode 30–60 秒，域重载另计。没有全程序集、全工程或长时测试。

通过 MCP 主动 refresh_unity（all/force、compile=request），完成事件 started=1794846610879、finished=1794846621247、domainReloadAfter=1794846632039。新 Runtime MVID **3b807385-71e5-4803-80f8-1253147aee77**，Ember.UI.Tests **f923448d-4624-4d2a-9fae-3af5a86bb08f**，Ember.UIExtension.PlayModeTests **31251a40-03b5-49a2-b0e7-03718b52bdea**；非 compiling/updating，Console 错误 0 后测试。状态查询曾遇域重载 ping 未回答；未重提编译，后续只读程序集请求等待约 30 秒后返回新 MVID，恢复状态再次确认 ready。没有断连十次耗尽、日志/BatchMode/dotnet 替代验证。

| 过滤范围 | 实际结果 | 证据（相对 evidence/rework/） |
| --- | --- | --- |
| Ember.UI.Tests.EUIAsyncImageEditTests 14 + TMPMarqueeEditTests 5 | **19/19 通过，2.458s**；job cbe699f4d1594d97aacb92cdfbd702ba | edit-request-corrected.json、edit-result-corrected.json |
| Play NeverActiveDestroyOrComponentRemoval、NeverActiveRemoval_PreservesBorrowedOverrideAndDestroyedGraphicIsSafe、HideEndsExistingRequest、RealItemList、MarqueeRealTime 五方法 | **5/5 通过，1.686s**；job 215e3e8fc29e44dda3727049e696331a | play-request.json（完整方法名）、play-result.json、loader-removal.json |
| 隔离字体实际渲染 | 3 materialIndex 在约 0.159 秒内各移动约 6.204 UI 单位；两张 PNG 已查看，中文/三角 fallback/青色 sprite 和右端裁剪可见 | marquee-mesh.json、marquee-rectmask.png、marquee-mask.png |

首次 EditMode 请求误写 Ember.UIExtension.Tests 命名空间，实际 0 个用例，**不计通过或覆盖**；随后核对源码、更正为 Ember.UI.Tests 才得到上述 19 个结果。零命中历史保留 edit-request.json/edit-result.json。没有新的编译失败或失败行为用例被当成通过。

编译证据 compile-request.json、compile-completion.json、compile-ready.json；最后 final-editor.json 确认 FrameworkScene、非 Play/编译/更新、timeScale=1、无 ResourceManager/Provider 测试残留、临时独立探针未加载、Console 错误 0。没有重新加入 A1 临时探针 .cs，仅保留原 .txt。全局字体段未运行，滚动/按钮/Binding 等未受影响范围未机械重跑。

### 交接与完成边界

本阶段具备独立定向复验条件；启动文本为 prompts/06-independent-recheck.md。复验从 R1 和实际源码判断 AC06/UIRT-A01，核对释放前/后 Graphic 观测及借入/销毁边界，检查字体隔离影响。AC10/UIRT-A02 在用户明确保留当前字节或提供匹配目标真实旧字节并授权恢复之前保持待决。不能因 A01 修好就把整体改为通过。

本阶段沿入口已保留的阶段创建授权，在本项目 local 环境创建独立复验对话，具体 dispatch 与启动状态保存 evidence/rework/dispatch.json；不指定模型、不建 worktree、不发送其他对话消息。此文件仍为实施自检；独立对话写新的复验记录，保留 acceptance.md A1 历史。未归档或清理本任务、未提交/推送/发布/保存模板/Bump/部署。

---

以下保留 S1 历史记录；其中“已完成”仅指当时实施交接，不表示独立验收或当前任务完成。

任务：20261116-ui-runtime-extensions。有效输入 requirements.md R1、design.md D1、handoff.md H2、evidence/design-baseline.json。日期 2026-11-16（Asia/Shanghai）。本文件只记实施自检；独立验收由下一对话形成 acceptance.md。

## 基线与所有权

- 工作区仍为 codex/local-snapshot-20261111-162819 / 16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2，无新 worktree/分支。入口22个源指纹均与 D1 一致。
- MCP 实时确认 Unity 6000.5.4f1；UPM Embedded Packages/com.ember，工作区0.25.5，uGUI2.5.0；正式编辑 topdown-2d 0.1.2/hash d83863b16bda8c2655f21d6aa3d55693。未改版本或模板。
- 归属为框架公共 UI/必要 Resource 合同；只读 Burner 参考，不来自 UnityFarm，不改缓存。
- 修改前逐文件及 .meta 备份在 evidence/backups；evidence/backup-manifest.json 记录工作区原字节；evidence/owned-files.json 记录本任务16个既有文件、15个新文件及 .meta/目录 .meta。既有 .meta 无变化，TMPMarquee GUID 保留。
- 前轮按钮三份代码/EUIButtonEditTests、模板身份、package.json 与入口指纹保持一致。其他 Core/Editor/字体/发布等工作保留。未提交、推送、发布或保存/Bump/部署模板。

## 实际交付

| 文件/目录（相对 Packages/com.ember） | 改动与公开入口 |
| --- | --- |
| Resource/Runtime/EmberAssetHandle.cs | public Create(path,cancel,release)、TryComplete、IsCancelled/IsDisposed；取消先终态，hook/回调异常隔离，Dispose 每请求幂等释放 |
| Resource/Runtime/EmberAssetHandleSlot.cs | 句柄身份+代次、Ordinal 地址、同地址去重、失败可重试、CancelLoading 释放、Clear/CurrentAsset/LastError；显示切换回调后 finally 释放旧 token |
| Resource/Runtime/IResourceProvider.cs、ResourcesProvider.cs | 原接口签名及 internal 兼容入口保留，Resources 使用公开工厂，仅放强引用，不卸共享资产 |
| UIExtension/Runtime/Behaviour/EUIScrollController.cs、EUIScrollContracts.cs | ScrollToBegin/End/Normalized、ScrollByPixels、ScrollTo/ScrollToItem、IsVisible、Stop、RefreshLayout、SetSnapTargets/Container、SnapToNearest；operationId 与结构化终态，FocusChanged/SnapCompleted |
| UIExtension/Runtime/UIExt/TMPMarquee.cs | 原路径/GUID正式启用；公开 TMP geometry，所有 materialIndex 从基线位移；SetMarqueeEnabled/ResetPosition/Refresh、速度/首尾停顿/窗口/非缩放时间与诊断 |
| UIExtension/Runtime/Behaviour/EUIImageLoader.cs、EUIRawImageLoader.cs、EUIAssetImageSlot.cs、EUIImageLoadResult.cs | Load/CancelLoading/Clear/SetSprite或SetTexture、路径/状态、借入占位/失败、KeepPrevious、NativeSize；代次/接管/销毁/池化及 never-active PlayerLoop兜底 |
| UIExtension/Runtime/UIExt/EUIExtensionAttribute.cs、Editor/Pages/EUIBindingEditorUtility.cs | AutoDetect默认true，四 companion=false；下拉可发现、精确Extension、旧自动选择保持；WidgetTypes及序列化字段不变 |
| UIExtension/Runtime/UIExt/EUIImageEx.cs、Editor/UIExt/EUIImageExEditor.cs | SpriteArray/SpriteIndex复用；非法/空索引清override回退base，多选/Undo/首次显示刷新；帧率/速度无效防御 |
| UIExtension/Runtime/Behaviour/EUIRoundedImageModifier.cs | IsActive、零尺寸/有限半径/三角数、preserveAspect、实际sprite UV边界；仅Simple，其余原网格直通 |
| UIExtension/Editor/UIExt/EUIRuntimeExtensionsEditor.cs | 跑马灯配置/滚动坐标/圆角支持范围提示，无新业务菜单 |
| 两个Runtime/Test asmdef与新 Resource/Tests、UIExtension/Tests/PlayMode | Runtime直接引用Resource；独立资源EditMode、UI Editor测试、短PlayMode隔离；Editor代码不进入运行时 |
| Documentation~/ui/runtime-extensions.md及ui/README.md、dynamic-lists.md；resource/README.md；docs/dev/ember-api-reference.md | 单一使用说明和资源合同，必要索引/API同步 |

## 工程落地与偏差

D1能力和支持范围未删减，无新增产品范围。参数沿用D1默认。实现细节和自检修复如下：

1. 显式 Refresh 会 lazy 订阅 TMP geometry，普通EditMode生命周期不再使公开入口失效。
2. Image 手动override接管时同时卸下被遮住的 loader base sprite；保留借入覆盖图，先卸引用再释放。完成前检查使用独立的显示拥有引用，不能只看有效override。
3. 所有显式 Load 都允许 enabled 的 inactive 实例，包括已 Awake 的回池实例；OnDisable只退休已存在请求，Show不自动重播。首次与复用 Bind→Show 均成立。
4. 从未Awake且持资源时才用UniTask IPlayerLoopItem观察存活，Awake/Clear/销毁后退出；缓存destroyCancellationToken。无新Manager或场景扫描。
5. 滚动稳定后吸附，原生延后事件与自身写入按实际content位置区分；候选结构变化会退休当前吸附。对象hash仅用于结构变化检测，不写序列化。
6. TMP公开速度入口和实际播放读取都规范化有限非负数，覆盖序列化NaN绕过setter；不写字体材质或布局。
7. 临时正式生成夹具走EUI中心的SetBindings/TryRegenerateCode，合法Module/RuntimeExtensions路径；真实EUIPage Logic通过Bridge调用四种新API，不手写生成Binding。无持久正式页面/Prefab新增。
8. 测试证据输出到Temp/EmberUIRuntimeExtensionsTests，必要证据复制到本任务；长期测试不依赖任务临时目录。夹具恢复timeScale/字体皮肤/单例服务，删除临时导入资源/Prefab；一次失败清理留下的本任务Prefab已通过MCP manage_asset删除。最终实时Manager无残留、IResourceProvider服务为空。

## 编译证据与失败修复

各代码批次均通过MCP主动请求compile=request；首次状态compiling/域重载短断连时按30秒后恢复，不重复未知结果请求。实际均在首次恢复查询后连回，无10次耗尽。基线Console有历史包管理取消错误，单独识别；每次成功代码批次取得新类型/MVID、编译/域重载时间和空错误Console后才执行测试。

关键证据：resource-compile.json/resource-tests.json；imageCompileState/Api.json；loaderDetachCompileState/Api.json；playRetryCompileState/Api.json；finalCompileState/Api/Console.json；最终补充的完成编译证据见completion-compile.json。Resource MVID为9a9a67ed-4fca-4d1e-a218-6298affda402；Runtime最终值由完成证据承接。

保留失败记录，不把首次失败记作通过：缺Ember.UI using、PlayMode asmdef重复TestRunner、uint/int、GetInstanceID硬弃用均已编译修正。行为自检修复包含TMP显式初始化、隐藏base sprite所有权、复用inactive Bind。夹具修正包含PPU、剪贴板改实际生成文件、模块目录、Logic FullName、销毁后判空、TMP Font clone独立atlas/material、SpriteAsset版本、等待EndOfFrame、切轴布局复位及超高速停顿取样。

一次字体clone错误清理曾被Unity拒绝销毁共享资产；随后改为独立atlas/material，未绕过保护。Symbols字体指纹不变；最终检查发现钉钉字体相对中途指纹发生磁盘写回，见文末资产边界补记，不能按未变处理。未将该失败算作视觉通过。

## 定向测试与证据索引

按不同过滤批次运行，未跑全程序集/全工程或长时测试。最终唯一EditMode范围为55个本任务新增情景，加37个直接回归，共92个；重复失败修复复跑不重复计入数量。PlayMode为6个短方法，never-active方法内部包含Sprite/Texture×pending/同步成功×GO/仅组件移除8种组合。

| 范围 | 最终结果与证据 |
| --- | --- |
| EmberAssetHandleEditTests/SlotEditTests | 10/10；resource-tests.json，强化外部可观察断言后的imageTests2.json内同10例也通过 |
| EUIScrollControllerEditTests | 初轮16几何情景通过（motionTests1.json仅4个失败都来自TMP）；新增LayoutGroup真实preferredHeight变动1/1，layoutTests.json；共17 |
| TMPMarqueeEditTests | 修复后5/5，MCP job694c30419cd4442bb1636ecd62c08913，0.832s；摘要另见tmp-edit-summary.json |
| EUIAsyncImageEditTests | 14/14；imageTests3.json。随后inactive允许范围扩大由真实Play池化覆盖，未改其已有显示策略 |
| EUIImageExAndRoundedEditTests | 4/4；imageTests1.json总21个中失败5项均不在此类 |
| EUIRuntimeExtensionsBindingEditTests与回归 | regressionTests.json 42/42，9.784s；含5新Binding + EUIButton16、动态列表、字体皮肤、本地化、现有Extension查找、SceneUI ResourceHost三例。真实HandleAdapter在图片14例中 |
| EUIRuntimeExtensionsPlayTests | playTests2.json 6/6，2.605s；滚动/跑马灯补充取样后的最新结果由completion-play-tests.json承接 |
| 真实渲染 | 8张PNG、marquee-mesh.json、scroll-events.txt、visual-review.txt。4 materialIndex实际匹配位移；中文/fallback/sprite glyph在RectMask2D和Mask中均可见且裁剪。横纵首尾及Simple异步图/索引/圆角实际截图已检查 |

## 逐项实施自检（不是独立验收结论）

| AC | 自检证据及边界 |
| --- | --- |
| AC01 | 横纵首尾、pivot0/.5/1、stretch、嵌套、三对齐/正负offset、小/零内容、像素方向/可见性，以及真实LayoutGroup高度变化；17几何情景。以实际投影边/中心断言，无算法镜像 |
| AC02 | Play真实Begin/EndDrag、wheel、Scrollbar、timeScale0动画、替换/停止/目标销毁、回调重入、稳定吸附/边界完成；补充等距顺序和原生惯性等待；事件序列及横纵截图。用户手工设备输入未执行 |
| AC03 | 短文本稳定，实际网格速度/暂停/复位、非有限速度、首尾停顿/往返及显隐重建；公开几何/材质/rect引用检查。最新停顿复验结果以完成证据为准 |
| AC04 | 原生TMP EditMode与TMPEx PlayMode、文本/宽度/margin/字体皮肤更新、4份材质网格/真实fallback+sprite、双裁剪截图；无共享材质/Shader改写；3D/RTL/多行/多个位置效果叠加为D1明确不支持 |
| AC05 | 两类loader通过正式Extension及真实页面Logic取得；Manager未就绪、空地址、同步/延迟/失败、占位/失败/保留/NativeSize；实际ResourcesProvider导入Sprite和Texture并显示 |
| AC06 | A/B、A→B→A、同地址去重/失败重试、取消、直接sprite/override/texture、显式同值接管、回调重入/异常；先卸显示后release；真实池Hide/Show/never-Show回池、8种never-active销毁；alpha/Graphic隐藏不释放 |
| AC07 | 10资源情景、Resources共享资产存活、真实SceneUI adapter、ResourceHost三例；外部Provider通过public factory调用，无反射进入Resource内部。未交付第三方后端 |
| AC08 | SerializedObject多选/Undo、非法/空列表回退；有限圆角、尺寸/active/UV/preserveAspect及Simple组合截图；非Simple直通且Inspector提示 |
| AC09 | 四companion登记/精确Bridge/中心真实生成文件/实际Page逻辑调用，旧ImageEx与原生自动选择保持；直接相关回归42/42；旧按钮源码未改 |
| AC10 | 每批MCP主动编译及完成证据、精确过滤/失败历史/最终结果/截图、文档入链；本对话仅自检，下一对话独立验收尚未完成 |

## 限制、下一步与回退

本轮为Unity Editor中的自动行为/渲染自检，没有人工签收、真机/构建、跨渲染管线/所有设备覆盖；这些不能标人工或全量通过。没有Addressables/YooAsset后端交付。当前没有新增产品范围或迁移要求；四companion默认不挂现有Prefab。

独立验收应先从R1和源码重判，再读本文件定位证据，可针对边界择检，不把自检当验收。若发现实质问题，交回实施定向修复，不降低AC。验收完成后回入口归档；此处不清理任务目录，不代写acceptance.md。

回退仅依据本任务backup/owned-files逐hunk；保留用户修改与既有GUID，资源合同和UI依赖成组回退，不git reset/clean。正式页面采用新组件后需先经中心移除/重绑；当前没有持久采用页面。

## S1 完成补记

实施与自检已完成，具备独立验收交接条件。最终主动编译Runtime MVID为43403601-0519-40c9-a905-19b6876bccd7，completion-compile.json记录编译/域重载时间、新程序集及空错误Console；completion-editor.json记录最终状态和清理恢复。TMP有限速度改动后，completion-edit-tests.json 5/5（0.820s）；滚动惯性/等距和跑马灯首尾停顿/往返、NaN/渲染最新补验completion-play-tests.json 2/2（2.283s）。真实LayoutGroup1/1（0.600s）。唯一EditMode总92、PlayMode六方法均取得最终通过结果；失败修复复跑不重复计数。

最终视觉记录以visual-review.txt和8张PNG为准，4材质实际位移约5.488 UI单位/0.141秒；速度采样按帧时序容差验证。临时测试资产已清理、旧.meta无变化、22项保护源码无越权变化；钉钉字体存在另列的序列化差异待核查。独立验收仍待下一对话，不把本补记作为任务整体完成或人工签收。

## 最终共享字体序列化边界（接手必读）

最终静态检查发现钉钉字体从中途22:56:18的SHA256 31A020...变为EA416A...，磁盘写入时间23:10:32，Symbols仍匹配。入口该字体已经是未提交修改，不属于本任务计划编辑文件；最初未备份其完整字节，只在中途记录指纹。已有历史备份均不匹配该指纹，不能安全用HEAD、其他旧字体或猜测补丁回退。当前文件和.meta另存evidence/font-writeback；font-writeback.json记录事实与边界。相对HEAD可见备/号/历/史/隐/藏/X/q八个额外字符，HEAD字符无丢失；这不证明相对中途快照的具体增量，也不能推定全部来自本轮测试。

当前资产与用户既有修改均保留，没有强制销毁资产或以HEAD覆盖。可能涉及动态字体的延后序列化，准确来源归属尚未证实。独立验收须核查并明确该差异是否接受或需要实际旧字节备份；在此之前不能声称“所有源字体未变”、AC10全部通过或任务整体完成。产品能力、编译及行为测试结果不因该记录改写，但资源清理/所有权验收要保留待判断项。

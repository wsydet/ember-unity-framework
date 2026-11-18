# 独立验收：柔光玻璃 UI 与 GM 控制中心

## A2：A1-RW1 独立复验（当前有效）

- 日期：2026-11-17 17:48，Asia/Shanghai；仍由原独立验收聊天01a1e8b5-afdf-7ed3-b93a-e3b89435a271负责。
- 有效输入：requirements.md R4/F01—F04、handoff H12、rework-a1.md、implementation.md及prompts/05-reacceptance-a1.md。下方A1原始结论、复现和证据完整保留。
- **结论：SG-IA-01通过；SG-IA-02原始字符/Submit穿透已修复，但双Popup隐藏/重显的焦点返回链仍失败，问题未关闭。AC04仍失败；整个任务保持未完成。**
- AC09设备体验与AC11正式模板保存/分发继续待验证/完成；R2视觉确认有效，本轮不请求重复签收。

### 现场、编译与范围

沿用同一checkout，分支codex/local-snapshot-20261111-162819、HEAD16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2。UPM实时解析Packages/com.ember，Unity6000.5.4f1/URP17.5.0，WindowsEditor/D3D12，实际GameView2560×1440。正式编辑身份仍topdown-2d0.1.2，未切换或保存模板。

开始/结束分别复算返工manifest的890份产品文件，零漂移；相对R2确为8份既有文件变化和4份新增代码/meta，与交接完全一致。Shader、URP、Prefab、Binding、材质、字体、布局无变化，原5张R2图像指纹无变化。manifest SHA256仍为816A14A2C661BBF226BCC3515C67765A41AEB9569CC1B808B3248507733DDA33。见[evidence/reacceptance-a1/baseline.json](evidence/reacceptance-a1/baseline.json)、[final-baseline.json](evidence/reacceptance-a1/final-baseline.json)。

本聊天经MCP主动compile=request，域重载期间一次状态查询未响应；间隔至少30秒重连一次恢复，未重复编译。实际编译开始1794908498496、完成1794908501285、域重载完成1794908512356（Unix ms）。实际加载UI MVID为aad4d568-81c2-43f1-87df-31511fd34c11，与返工交付一致；非编译/非reload，最终Console无warning/error。见[compile-state.json](evidence/reacceptance-a1/compile-state.json)、[editor-final.json](evidence/reacceptance-a1/editor-final.json)、[console.json](evidence/reacceptance-a1/console.json)。

未修改产品或测试源码，未再生成Binding；仅新增本轮证据和验收交接。未创建worktree/子代理，未提交发布、模板操作、全量/长测或归档清理。最终Play=false、FrameworkScene未脏，圆点位置偏好键仍不存在；Git状态前后无变化。

### 实际独立验证

1. 独立选取19个相关EditMode用例并重跑，19通过/0失败/0跳过，1.3323154秒，job43f5d72aecf0448a8979f958e009329a。范围：EUIModalInputEditTests、EUIBackdropLeaseEditTests、EUIBackdropPolicyEditTests、EUIFullscreenBackdropEditTests、EUIGlassGeometryEditTests及单个mask池回归。不是全程序集。见[tests.json](evidence/reacceptance-a1/tests.json)。本轮19例与实施18例过滤范围不同，不沿用实施数量。
2. 阅读当前产品实现和实施运行探针后，在本独立聊天重新执行其40项回归矩阵，输出独立保存，40/40通过。见[runtime-regression.json](evidence/reacceptance-a1/runtime-regression.json)、[实际执行源码](evidence/reacceptance-a1/runtime-regression.cs.txt)。此部分复用探针来源已明确，不把实施旧JSON改名当作本轮结果。
3. 另编写并执行15项独立探针，重新从A1原故障触发点检查，并补双Popup隐藏/重显/销毁组合。原始结果为10通过/5失败，未覆盖或删掉false，见[independent.json](evidence/reacceptance-a1/independent.json)、[源码](evidence/reacceptance-a1/independent-probe.cs.txt)。
4. 对这5个false进行隔离：每个场景显式重设起始selection，加入不隐藏的对照。确认两个稳定失败现象；另3个false是先前焦点已丢失后的连锁结果，孤立的外部停用/重启及隐藏下层销毁路径均通过，不另报三个缺陷。见[hide-chain-repro.json](evidence/reacceptance-a1/hide-chain-repro.json)、[cross-hide-return-repro.json](evidence/reacceptance-a1/cross-hide-return-repro.json)及对应同目录探针源码。
5. 清理本轮运行消费者后再等60帧，捕获10900→10900，0边界/输出RT/相机段/专用材质，见[teardown.json](evidence/reacceptance-a1/teardown.json)。仅关闭运行实例，未删除产品资产。
6. 查看返工03/04实际图，并独立采集[模态焦点现场](evidence/reacceptance-a1/01-modal-focus.png)、[关闭mask后局部玻璃](evidence/reacceptance-a1/02-mask-off-local.png)、[隐藏顶部后现场](evidence/reacceptance-a1/03-top-hidden-dead-focus.png)。材质、布局及R2已确认效果无实质变更；焦点和正确恢复局部玻璃属于功能修复。

### 两个问题的复验结论

| 问题 | 当前结论 | 实际证据与限制 |
| --- | --- | --- |
| SG-IA-01 / 原P2 | **通过，当前890文件基线下关闭** | A1同样的Blur→UseMask=false→RefreshBackdrop路径：旧lease已disposed，新Group lease ready且未disposed，所有页面Graphic均不再持有旧引用；恢复mask、逻辑拒绝/恢复、Solid/Off、共享与隐藏重显、回池/销毁均通过。19例中的借用不Dispose、幂等释放与迟到撤销测试通过；最终资源归零。 |
| SG-IA-02 / 原P1 | **原始穿透通过；生命周期子项失败，保留问题未关闭** | 已聚焦输入框收到实际Deselect，GameView字符不改下层文字；下层Submit/Move无动作，上层默认焦点及导航、单层/双层普通关闭/外部销毁、GM更高层焦点、业务原本禁用根保护均通过。剩余双Popup隐藏/重显问题见下节，当前子问题严重度P2；不把“原穿透已修复”等同整个问题通过。 |

### SG-IA-02剩余子场景：双Popup隐藏后的交互和返回链（P2）

预期：最高Popup视图隐藏、其mask不再可见时，实际最高可见下层Popup应能接收焦点和正常交互；重新显示不抢错误层，最终关闭后恢复打开前仍有效的selection。不能因隐藏/重显丢失返回链；同时保留业务本来禁用状态。

稳定复现A（无需交叉隐藏）：

1. 运行Showcase，选择BackgroundMode按钮。
2. 打开下层EUISoftGlassConfirmPage，再打开上层EUISoftGlassAlertPage。
3. 调用HidePageViewOnly(上层定义)，等待30呈现帧。
4. 实际下层state=Paused、alpha=1、CanvasGroup.interactable=false、blocksRaycasts=false；Accept/Cancel/Close全部active但不可交互，EventSystem.currentSelectedGameObject=null。下层玻璃仍显示且mask仍存在，没有键盘目标，也不能直接点击卡内操作。
5. 不经过隐藏的关闭对照能正确恢复BackgroundMode；单纯将上层重显后正常关闭也能恢复，不能据此忽略隐藏期间的失效。

稳定复现B（交叉隐藏返回链）：

1. 从明确选中的BackgroundMode开始，依次打开下层Confirm、上层Alert。
2. 隐藏上层→重显上层→隐藏下层→重显下层，每步等待10—12帧。
3. 关闭上层，仍能选中下层Accept；再关闭下层。
4. 实际主页面可交互，但selection=null，原BackgroundMode未恢复。相同起始状态、不经过隐藏的对照正常。

证据：[hide-chain-repro.json](evidence/reacceptance-a1/hide-chain-repro.json)、[cross-hide-return-repro.json](evidence/reacceptance-a1/cross-hide-return-repro.json)、[截图](evidence/reacceptance-a1/03-top-hidden-dead-focus.png)。探针只用公开Hide/ShowPageViewOnly及ClosePage，不修改内部页面状态使其失败。

源码定位与推断：EUIManager.cs:215/229的视图隐藏/重显只入队页面本身操作，SynchronizeModalMasks处理mask可见性；下层此前经AddPopup.OnPause后仍保留Paused。EUIModalInputScope.cs:90—110把隐藏作用域移除并在重显时按当时selection重建；隐藏顶部造成selection为空后，交叉隐藏可使原返回关系丢失。上述为实现定位，实际失败由运行证据确认，不要求实施盲目调用OnResume或重新启用所有根。

交回实施：保持SG-IA-02稳定ID，补覆盖实际PageContext和scope联动的双层隐藏回归，修复可见顶层交互与返回链；继续保护业务原本禁用的根、GM已转移焦点、UseMask=false/逻辑拒绝语义，不隐藏下层规避采样。修复后定向复验本节两条路径，再回归原字符/Submit/Move、单/双关闭与销毁、FullScreen、租约和资源归零，不运行全量长测。SG-IA-01只有受后续修改影响时才重新打开相应结论。

### 当前AC状态（A2优先）

| AC | 当前状态 | 本轮变化或沿用边界 |
| --- | --- | --- |
| AC01 | 通过（本地集成） | 沿用A1；本轮旧Popup回归通过，资产/注册与外观源无变化。 |
| AC02 | 通过（Editor视觉） | 沿用A1并核新运行图；普通Popup下层继续实际渲染与更新。 |
| AC03 | **通过（当前Editor范围）** | 默认/继承/纯色/Off、UseMask/逻辑门禁、旧配置及原键盘阻挡缺陷均验证；隐藏返回链残留由AC04失败承接。 |
| AC04 | **失败** | SG-IA-01已通过；SG-IA-02双Popup隐藏/重显交互与返回链残留稳定失败。其他已通过路径与资源归零仍有效。 |
| AC05 | 通过（原声明范围） | 沿用A1，视觉资产/Shader未改；不扩展设备边界。 |
| AC06 | 通过（资产/控件基础） | 沿用A1；原下层输入穿透已修复，跨层隐藏子问题由AC04承接。 |
| AC07 | 通过（Editor布局） | 沿用A1/R2；未改布局。 |
| AC08 | 通过（Editor功能） | 沿用A1，本轮GM键盘/收起/更高层焦点回归通过。 |
| AC09 | **待设备验证** | 真实触摸、旋转、安全区、键盘覆盖原边界保留，不用自动事件替代设备体验。 |
| AC10 | 通过（测量与披露） | 沿用R2测量；本轮仅验证关闭后资源/捕获归零，未新增性能承诺。 |
| AC11 | **待完成/验证** | Assets仍base待回流草稿，正式中心保存/分发未完成且未执行。 |
| AC12 | 通过（本轮MCP/相关范围） | 本轮显式编译/域重载、19/19相关EditMode、40项回归及独立补充证据齐全；运行失败如实记录，编译不代替行为通过。 |
| AC13 | 通过（R2确认继续有效） | 原五图零漂移，未发现实质视觉变化，无需重复美术确认。 |

**下一阶段：实施继续定向修复SG-IA-02隐藏子项，再回本聊天复验。** 当前不能进入归档清理。AC09/AC11没有用户明确接受延期或移出范围记录，继续保留。原A1全部历史如下。

## A1：首次独立验收记录（历史原文）


- 任务：20261117-soft-glass-ui-gm；验收修订 A1。
- 日期：2026-11-17 17:02，Asia/Shanghai。
- 独立验收聊天：01a1e8b5-afdf-7ed3-b93a-e3b89435a271；区别于实施聊天 01a1e817-03d6-76b3-8811-b6c91093c862。
- 输入：requirements.md R4（R01—R08、F01—F04、AC01—AC13）、design.md D1、revision-r2.md；H10/H9 与 preview-confirmation.md 优先于历史状态文字。
- 总结论：**未通过整体验收，交回实施阶段定向返工；任务保持未完成。** 当前有 2 个已复现缺陷，AC03/AC04失败；AC09设备体验与AC11正式模板分发仍待验证/完成。R2用户视觉确认有效，AC13通过；不能据此归档清理。

## 环境和基线

沿用当前checkout，未创建worktree或子代理。分支 codex/local-snapshot-20261111-162819，HEAD 16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2。UPM实时查询解析为本仓库 Packages/com.ember，标称0.25.5，为未发布工作区；Unity6000.5.4f1、URP17.5.0、WindowsEditor/D3D12、RTX5060Ti。独立运行GameView为2560×1440；实施R2短测为1920×1080，二者不可混用性能结论。

正式编辑记录仍为topdown-2d0.1.2/hash d83863b16bda8c2655f21d6aa3d55693，扩展ownerVersion仍0.1.1；只记录，不替其他任务修正或签收。Assets新UI仍是base待回流草稿。没有加载/切换/保存/Bump/同步/部署模板，也没有提交、发布、归档或清理。

开始与结束各重算R2 manifest所列886份产品文件，差异均为0；5张R2图像指纹也无变化。manifest SHA256仍为242518ABCD1CC0124A05BA3F3BE6C4C5CFBA8476ACC65B840DEC5EA727761F9A。Git状态前后无变化。见[evidence/acceptance/baseline.json](evidence/acceptance/baseline.json)、[final-baseline.json](evidence/acceptance/final-baseline.json)。

## 本轮实际检查

先读取实际源码、Shader、Prefab及配置，再对照方案、实施原始证据判断。重点核查UI core/URP程序集边界、EUIBackdropMaskSession/Group生命周期、Manager默认及门禁、CameraHost根Canvas分段、Graphic/Shader圆角与clip、业务Appearance/GM/扩展示例、中心制作与Binding生成来源。没有改产品来使验收通过。

1. 经MCP主动请求force refresh/compile=request。请求已触发但等就绪60秒超时；一次间隔不少于30秒的状态重查恢复，未重复编译。该次编译开始1794905659230、完成1794905662080、域重载完成1794905671359（Unix ms）。恢复后非编译/非reload，读取Console；仅一条重连WebSocket警告，无编译错误。最终Console无warning/error。证据：[compile-state.json](evidence/acceptance/compile-state.json)、[editor-final.json](evidence/acceptance/editor-final.json)、[console.json](evidence/acceptance/console.json)。加载Ember.UI.Runtime MVID为3ceb14e0-74f6-4ee5-bd86-904395d7273b，业务程序集758efd54-60f7-442a-a396-8a0a4201ab01。
2. 独立重跑13个相关EditMode用例，13通过/0失败/0跳过，测试时间1.2489103秒，job e6d6697f73d949888bc8c8ef4e6764b5。范围仅租约、策略优先级/旧tint/快照、Fullscreen恢复、圆角含SquareTop、遮罩池和两条中心配置回归，未运行全程序集。见[tests.json](evidence/acceptance/tests.json)。
3. 独立运行25项有界探针，原始结果保留于[runtime.json](evidence/acceptance/runtime.json)，探针源码为[runtime-probe.cs.txt](evidence/acceptance/runtime-probe.cs.txt)。覆盖默认Blur、旧Popup、Solid/UseMask、逻辑拒绝mask、双Popup及先关下层、指针点击关闭、实时采样、输入只读/禁用/错误、时间同步/无handle键盘、FPS/拖动/收起区域、根Canvas拒绝。两个探针初次在内存编译时遇到局部变量命名及CodeDom不支持in调用的问题，修正探针后才运行；未改产品或将其称为产品编译错误。
4. 原始lower-input-paused断言误将普通Popup下MainPage必须Pause作为必要实现方式。源码允许下层继续渲染/交互并靠模态输入策略拦截，单看这个false不足以认定缺陷。进一步真实GameView字符事件与EventSystem Submit证实输入穿透，缺陷结论以SG-IA-02的复现为准；不要求修复必须采用Pause或隐藏下层。
5. 独立复核CommonSkin Legacy/SoftGlass路由与GM来回切换，均能打开对应旧/新逻辑；临时克隆配置只在内存替换，已还原。焦点复核暴露SG-IA-02。见[selection-focus.json](evidence/acceptance/selection-focus.json)、[issue02-repro.json](evidence/acceptance/issue02-repro.json)。
6. 实际加载24个正式Prefab：无Missing组件、17个Item根无Canvas、所有正式Slider引用无handle；旧GM GUID仍为bba4cad8012e3f64690d24af5becf55d。中心维护源使用TryBuildPlan/Create、Prefab API、Binding工具与TryRegenerateCode；未在验收中再生成修改时间戳。已检查实施保存的44文件语义重生差异0记录和实际Binding引用。见[assets.json](evidence/acceptance/assets.json)、[contract-audit.json](evidence/acceptance/contract-audit.json)、原evidence/regeneration-final-result.json。
7. 消费扩展示例实际出现两个Item，均有所有者；对已有实例再次Factory创建被明确拒绝。见[ownership.json](evidence/acceptance/ownership.json)。静态嵌套由父Binding持有，动态宿主为RectTransform，Factory路径自行释放；没有重复UILogic/Factory创建。
8. 独立截图深/浅/暖彩动态背景，见[background-0.png](evidence/acceptance/background-0.png)、[background-1.png](evidence/acceptance/background-1.png)、[background-2.png](evidence/acceptance/background-2.png)。逐张查看八张原参考与R2五图：材质有背景透色、柔化与柔边高光；浅背景切深色文字；GM横屏三卡、竖屏两卡加横向测试卡，四样本和六/四列操作齐全；FPS入口没有中心环，滑杆没有独立handle。当前截图中的FPS受Editor及工具采样影响，不用作性能测量。
9. 从R2逐帧性能原值重算p50，四组CPU主线程/GPU与记录完全一致；见[performance-audit.json](evidence/acceptance/performance-audit.json)。未再做长测。关闭全部本任务消费者后60帧捕获计数9665→9665，0边界/输出RT/相机段/专用材质，见[teardown.json](evidence/acceptance/teardown.json)。
10. 最终退出Play，FrameworkScene.isDirty=false；GameView尺寸未调整，圆点测试位置键恢复不存在（实施交接的初始状态），产品及Git状态无变化。未保存运行中的字体缓存。

## 逐项AC结论

以下“通过”仅覆盖写明的工作区与Windows Editor范围，不代表消费端正式分发、Player构建或移动真机签收。

| AC | 结论 | 独立判断与证据/边界 |
| --- | --- | --- |
| AC01 | 通过（本地集成） | 旧资产/GUID/注册保留；CommonSkin分别路由旧/新Confirm，GM Legacy/SoftGlass来回切换正常。默认Blur与皮肤选择独立。见assets.json、selection-focus.json、Manager/Appearance源码。模板交付状态另见AC11，不据此称已分发。 |
| AC02 | 通过（当前Editor视觉） | 已查看当前实现R2五图与八张参考，另采集当前深/浅/彩色动态背景。实时纹理版本5→9，Shader实际采下层合成而非普通透明板；前景独立绘制清晰。根Canvas分段/RenderGraph代码与S1同帧原始证据相符。SG-IA-01为切换生命周期例外，落在AC03/04。 |
| AC03 | **失败** | 默认/继承/显式Solid/旧schema tint/UseMask取消与逻辑拒绝mask路径已验证。但SG-IA-01中UseMask切换破坏局部玻璃，SG-IA-02中模态键盘输入仍穿透；R03的输入语义未完整满足。 |
| AC04 | **失败** | 指针mask首命中、ClickMaskToClose false/true、双Popup独立输出及先关闭下层、最终零资源均验证。实施R2还保留20次循环/Fullscreen/尺寸变化原始结果；仍被SG-IA-01外部lease失效和SG-IA-02键盘穿透阻断。修复后须复验关闭/隐藏/回池、叠层与焦点恢复。 |
| AC05 | 通过（Editor及声明支持范围） | 正式圆角12/20/32/48、circle/capsule及嵌套规范存在；当前2560横屏与R2 1920横屏/1080竖屏均保持形状和布局。Shader含Stencil、RectMask2D softness、CanvasGroup，已读GPU clip原始探针及当前相关分支；SquareTop定向测试通过。独立边界必须root Canvas，嵌套共享，现场请求嵌套边界被拒绝。设备刘海/手势区域体验未签收，见AC09。 |
| AC06 | 通过（资产与控件基础行为） | 24资源/22 Binding/5注册可定位，17Item根符合契约，库与旧库并存；输入焦点、只读、禁用、错误前景、无handle键盘与指针实现均核对；静态/动态扩展示例实际运行且重复所有权被拒。正常/选中/禁用状态见实际图、R2专项和组件接线。模态跨层输入缺陷不被此项掩盖，见AC03/04。 |
| AC07 | 通过（Editor布局） | R2横/竖GM实图及portrait-layout原值与当前Prefab/响应式网格一致：横屏三卡，竖屏两卡+横向测试区，四个样本完整，动作六/四列，滚动访问空间保留。与参考02的卡片/竖条/圆动作层次相符，不以手机功能充数。 |
| AC08 | 通过（Editor功能） | 独立时间1.85同步Unity/Ember，键盘上调2.14；关闭控制复1，收起/动作作用域与原始23项证据吻合。六动作含复位/增强/库/真实退出/两消费动作；源码保留旧四种测试与状态刷新。实际退出引用已核对到GameLauncher.Quit及实施quit-action真实退出记录，本轮没有为重复退出再启动第二轮Play。Legacy实际选择通过。 |
| AC09 | **待验证** | 本轮鼠标pointer handler拖动/吸边/不误触、收起区域不拦无关射线及FPS已验证；R2含第二指、安全区/方向回归。真实触摸、设备旋转、刘海/手势条、键盘覆盖体验仍未执行；不把合成PointerEventData写成真实设备签收。GM焦点涉及SG-IA-02修复后也需检查。 |
| AC10 | 通过（测量与限制披露） | R2四组180帧原值p50可复算；输出RT与总显存明确分开，GPU只取非零样本。当前独立关闭后0输出/材质/相机段，捕获停止。嵌套边界拒绝现场验证，partial viewport/XR/display/层池耗尽拒绝分支及Solid回退已核源码。没有目标设备硬阈值，不宣称移动性能达标；Player/其他API/热稳态/功耗待对应平台验证。 |
| AC11 | **待验证/未完成** | 框架core→独立URP→Assets业务归属及中心生成来源正确，Item/绑定所有权正确。当前Assets是base待回流草稿；未获模板加载/保存/Bump/同步授权，且确实未完成正式保存/分发。D1第9节S6明确此边界不得作为整个任务完成，记录为SG-PENDING-01，不擅自执行或标不适用。 |
| AC12 | 通过（本轮MCP与相关范围） | 本轮显式compile请求、完成/域重载时间、实际MVID、13/13定向EditMode、独立运行及Console记录具备。仅执行相关短验证，无全量/长测。真机/构建未执行分别披露，失败行为由AC03/04承接，不把编译成功等同功能完成。 |
| AC13 | 通过（R2原视觉基线） | 用户在实施聊天对R2回复“可以进行收束了”，preview-confirmation.md/H9/H10明确记录；当前886产品文件及5图指纹均与对应manifest一致。不重问已确认效果。此次验收只新增证据/报告，不改变视觉。后续修复若改变视觉，受影响预览仍需按R08更新确认。 |

## 缺陷与复验要求

### SG-IA-01 / P2：取消遮罩后，局部玻璃持有已释放的外部租约

- 影响：AC03、AC04；R02/R03。返工阶段：实施。
- 预期：UseMask只控制模态遮罩；关闭遮罩后，同页局部玻璃仍按其独立配置采样。文档popup-backdrop.md和base说明明确局部玻璃与模态遮罩独立。
- 实际：打开Blur Confirm后group.Lease为ready=true/disposed=false；设置UseMask=false并RefreshBackdrop，mask移除，但8—10帧后group.Lease仍非null、disposed=true、ready=false，卡片变为实色。重新开启遮罩后恢复。
- 复现：启动FrameworkScene → 打开EUISoftGlassShowcasePage → 打开EUISoftGlassConfirmPage（默认Blur）→ 设置page.UseMask=false → EUIManager.Instance.RefreshBackdrop(page) → 查看页面玻璃与EUIBackdropGroup.Lease。
- 证据：[runtime.json](evidence/acceptance/runtime.json)的mask-off-retains-local-glass、[issue01-repro.json](evidence/acceptance/issue01-repro.json)、[前](evidence/acceptance/issue01-before.png)/[后](evidence/acceptance/issue01-after.png)实图。
- 定位：EUIBackdropMaskSession.cs:41向group借出lease；:45—49释放时未撤销group借用。EUIManager.cs:965—970只归还mask，RefreshBackdrop后UseMask门禁跳过重新Configure；Group.cs:37要求_lease==null且!_external才能自取，残留disposed引用阻止恢复。
- 复验：Blur→UseMask=false→true、Blur→逻辑拒绝→恢复、Solid/Off切换、关闭/隐藏/回池/销毁、局部组恢复后仍单一所有权，确认无重复释放/资源增长。不能靠禁用局部玻璃规避需求。

### SG-IA-02 / P1：普通模态Popup未阻断下层已选控件的键盘输入

- 影响：AC03、AC04；R03的输入阻挡。返工阶段：实施。这是当前工作区可复现的不满足项；S0普通Popup也未暂停MainPage，因此不声称由本次Shader新增引入。
- 预期：UseMask=true的模态Popup存在时，下层输入框和按钮不接收字符/导航/Submit；关闭后按有效所有权恢复合理焦点，继续渲染下层用于模糊。
- 实际：在Showcase输入框输入Before并选中，程序事件打开默认Confirm后，input.isFocused=true且EventSystem仍选择下层输入框；向现有GameView.SendEvent(Event.KeyboardEvent("z"))后文本从Before变为z，Popup仍打开。另选中下层BackgroundMode后开Confirm，经EventSystem对当前selection发送Submit，下层按钮动作触发1次。
- 复现：运行Showcase → 聚焦输入框 → 不先点击其他控件，业务/定时调用ShowPopup打开默认Confirm → 在GameView键入字符。第二条路径：选中下层BackgroundMode → 同样开Popup → Submit。测试使用GameView真实字符事件；Submit使用与输入模块相同的ExecuteEvents路由，不是直接调用Button.onClick。
- 证据：[selection-focus.json](evidence/acceptance/selection-focus.json)、[issue02-repro.json](evidence/acceptance/issue02-repro.json)、[issue02-keyboard.png](evidence/acceptance/issue02-keyboard.png)。
- 定位：EUIPageContext.cs:170起AddPopup普通弹窗首层只登记排序，不处理下层selection；mask仅阻挡GraphicRaycaster。实际uGUI TMP_InputField.OnUpdateSelected在isFocused时继续消费GameView事件。
- 复验：输入聚焦/未聚焦、下层按钮/Slider选择、字母/方向/Submit/Cancel/ESC，单/双普通Popup及先关下层，ClickMaskToClose两模式、UseMask=false的业务自管语义、逻辑拒绝mask、FullScreen首次采样、旧Popup及GM开关，关闭/销毁后焦点恢复和不误触。保留背景渲染，不用隐藏下层破坏AC03采样。

## 未完成边界及交接

- SG-PENDING-01 / 交付门槛 / AC11：base正式保存/分发未完成。由实施/入口先准备保护topdown草稿的精确差异与恢复计划，再处理具体模板授权；独立验收不接管模板操作。
- SG-PENDING-02 / 设备验证 / AC09：真实触摸、安全区/方向、键盘/IME覆盖未完成。没有可用设备现场，也没有用户明确接受延期或移出范围记录，因此保持待验证。当前Editor结果不替代设备签收。
- Player构建、其他图形API/管线、XR、多显示器、热稳态/功耗未执行。当前后端明确支持URP17完整viewport单显示器；不支持目标按文档降级，不能扩写兼容承诺。

交回实施阶段：先修SG-IA-02、SG-IA-01，在已有原范围授权内完成产品修复及上述相关回归，不重复请求同一范围修复许可；由实施更新implementation/handoff、产品指纹及必要预览，再回本独立验收聊天定向复验。模板正式操作、设备现场仍按各自门槛承接。保留全部任务材料，未归档/清理，不提供“必需项全部完成”的入口收束许可。


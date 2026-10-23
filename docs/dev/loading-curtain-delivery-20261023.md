# Loading / 遮幕解耦交付记录 — 2026-10-23

本次框架实现和增量验证已完成，未发布。Windward 与 Mountain 仅作只读参考，没有修改消费项目、PackageCache、签收基线，没有提交、推送、部署或版本升级。

正式消费契约：[Loading 会话与 Windward 接入](../../Packages/com.ember/Documentation~/ui/loading-sessions.md)。

## 结论与架构检查

EUILoading 可以不发起任何场景请求独立运行。EUITransitionBlock 只做视觉采样；EUICurtainSession 管理本次页面和动画生命周期；EUIManager.SceneLoading 负责场景请求的展示凭证与自动编排。旧自动场景流程调用同一套 Cover/Reveal 接口。通用会话不包含 EmberTransitionOperation、SceneTransitionContext、HoldPresentation 或业务恢复引用。

原有 WhenPrepared / WhenRunning / Completion / WhenInteractive 接口保留。本次改变展示所有权的组织方式，没有把准备成功冒充整体成功。外层手动会话已覆盖时，内部 RequestInPlace 完成并释放事务暂停，外层仍保持；业务显式揭幕后才结算展示和可交互阶段。角色控制仍允许由更晚的复生动作结束决定。

进度与遮幕分离：手动会话不等待假进度；会话 SkipFakeProgress 在 Init/Show 前生效。普通自动 Loading 继续等待真实操作成功及其展示就绪。真实现有 Loading 在条与数字同时禁用时不推进 fast/tail 计时，不能以 100 秒假计时阻塞真实完成。SkipCustomTransition 允许死亡场景完全跳过进度组自定义淡入淡出。

## 实际改动清单

- 新增 UI/Runtime/EUICurtainSession.cs：独立句柄、状态、Covered/RevealFinished、显式暂停、取消/异常/释放。
- 新增 UI/Runtime/EUITransitionPlayback.cs：每会话时钟、受控动画接口、进入/退出/进度/Custom 选项。
- 新增 UI/Runtime/EUIManager.Curtain.cs：通用 Cover/Reveal；EndOfFrame + NextFrame 渲染屏障；销毁监测和所有权隔离。
- 新增 UI/Runtime/EUIManager.SceneLoading.cs：迁出场景相关编排与 HoldPresentation 关联。
- 修改 EUIManager.cs：旧 RunWithLoadingAsync/RunLoadingPage 成为会话调用方；揭幕成功等待页面 Cleanup；移除旧全局完成事件等待。
- 修改 EUIPage.cs / EUIViewEngine.cs：初始化前注入选项；受控方块播放；已取消打开/关闭队列防重；销毁页面仍清理逻辑；释放追踪。
- 修改 EUITransitionBlock.cs：进入/退出独立时长，退出未配置时沿用旧值；运行时按可暂停时钟采样；公开预览初始化 RectTransform；定向 Tween、停用/卸载清理。
- 扩充 EmberTransitionLoadingTests、增加 EmberLoadingProgressContractTests / EUITransitionBlockPreviewTests；PlayMode asmdef 增加真实 UIExtension 引用。
- 增加随包接入文档，更新 UI / 游戏转场入口和 API 速查；本记录和实帧证据单独存放。

本次未更改模板初始接线、生成页、正式 Prefab、Templates~、ParentSnapshot~ 或 hash。Assets / base / platformer-2d 的 EUILoadingPage.csSHA256 均为 `36A15A0A8CC6DB33BECADFAB2FD8C609B4024574C3146C010FA04F11591DEDE0`。当前工作区原有大量模板编辑差异不属于本次任务，没有撤销或封存它们。

## 生命周期决定

- 并发拒绝、不抢占；旧句柄不能操作新会话。重复 Reveal 共用首次结果。
- Covered 后无限保持；仅业务显式 Reveal 开始退出。Cover 令牌仅用于取得覆盖，迟到取消不能自动揭开提交中的业务。
- Dispose/Cancel 立即隐藏、停止受控动画、移除本次页面追踪，终止相关等待，不声明成功揭幕；Destroy 原生回收仍按 Unity 帧末规则。
- Reveal 成功等待页面 Cleanup，不只等动画。失败不会遗留展示持有。清理不受暂停时钟阻塞。
- 默认非缩放时间；SetPaused 是独立用户信号，业务内部 timeScale=0 不冻结方块。UseScaledTime 是明确 opt-in，不适用于内部 Pause 等待闭幕的恢复链。
- 业务输入锁和 PlayerModule.Pause 从不由遮幕取得，业务仍负责 finally。不可逆提交后先稳定结果/错误界面，再揭幕或释放；没有自动回滚承诺。
- 暂停契约覆盖受控方块；任意旧 Custom/Animator 不会自动变为可暂停任务。死亡接入用 SkipCustomTransition 跳过旧进度动画，普通自动流程保持旧默认。

## 实际参数与实帧

[十帧预览画廊](evidence/loading-curtain-20261023/preview.html) · [逐帧像素采样](evidence/loading-curtain-20261023/samples.csv)。源为正式 Loading Prefab 的临时 contents/克隆，公开预览入口产生 Tween 后按确定时间采样，真实 Unity Camera/Canvas 渲染到 960×540 RenderTexture；设计视口 1920×1080。不是合成示意图，也不是 Windward 实景录像。

固定3列 → 3×3，Lines 水平分组，进入 TopLeft，退出默认反向，进入0.75秒，退出0.6666667秒，错开0.08秒；纯黑，缩放/透明度恒1，旋转和Y位移0，X位移 EaseInOut(0,3,1,0)。

| 采样阶段 | 时间 | 黑色像素 / 518400 |
|---|---:|---:|
| 进入初始 | 0 | 0 |
| 进入中点 | 0.375 | 299100 |
| 完全覆盖 | 0.75 | 518400 |
| 退出初始 | 0 | 518400 |
| 退出中点 | 0.3333333 | 213000 |
| 完全揭幕 | 0.6666667 | 0 |

![进入中点](evidence/loading-curtain-20261023/enter-2.png)

可形成三条横向条带。正方形网格边长640，总高1920，16:9裁切后可见高度约220/640/220；不等同原版等高条带，不做逐像素复刻签收。所有项目默认外观未改变。

## 本轮编译与最终增量测试

Unity 实例 `ember-unity-framework@b23e9b768e85ed30`；Application.dataPath 确认框架路径，Unity 6000.5.4f1。每轮 C# 修改后均由 MCP refresh_unity 显式请求编译，随后读取加载程序集 MVID 与新 API/测试程序集、isCompiling/isUpdating，再读 Console 才运行测试。没有以刷新请求成功、空 Console 或切回窗口代替编译完成。

最终 UI Runtime MVID `9936e2c7-a76e-426e-8ffa-68b26c9bcb85`（此前 `d629ba76-10f9-4e6a-a1c1-d41229107466`）；最终 PlayMode 测试 MVID `179fa2aa-73e4-4b77-bb3c-e4dbd4d52248`（此前 `19c522b5-894a-4e1c-9355-f2c830c8785b`）；EditMode `55270774-91a6-4b22-801e-872beca15669`。退出测试后编辑器不在 Play、不在编译、不在更新。Console error/warning 0。

| 最终范围 | 结果 | MCP job |
|---|---|---|
| EUITransitionBlockPreviewTests | 4/4，1.319秒 | `6b0880a7d0cf471f82f183d8bad4d28a` |
| EmberTransitionLoadingTests + EmberLoadingProgressContractTests | 26/26，4.596秒 | `74270eb099644b188437b9fc0646be7b` |

[原始工具结果及状态](evidence/loading-curtain-20261023/validation.json)。这两批是增量相关类，不是全量重测或构建验证。

| 用户要求 | 覆盖证据 |
|---|---|
| 无场景闭幕、保持、业务揭幕、重复与并发 | ManualCurtain_HoldsWithoutSceneUntilExplicitReveal |
| 业务在覆盖后执行、慢准备不提前揭幕 | Success_CompletionIncludesCurtainClose、SlowPreparation_ExposesPendingBoundaryToLoading、RealBlocks_UserPause… |
| 原位恢复、内部暂停、阶段无循环 | ManualCurtain_InPlaceInternalPauseFinishesBeforeReveal、InPlace_UsesDefaultCurtainAndRetainsWorld |
| 用户暂停冻结进入/退出并连续恢复 | RealBlocks_UserPauseFreezesBothDirections_DespiteInternalPause；两段各0.15秒位置/缩放保持断言；ScaledClockIsExplicitOptIn |
| 取消/失败/销毁/迟到完成清理 | CancelledPreparation、PreparationFailure、Shutdown、DisposeDuringOpen、DestroyedView、CancelDuringPausedEnter、DestroyDuringEnter、CancelReveal、OldOwnerLateCompletion |
| 进度条/百分比不闪、不假等待、成功仍真实 | DisabledProgress_HasNoFirstFrameFlashOrFakeWait_ButRequiresRealSuccess；真实 TMP UGUI；100秒fast/tail配置下直接成功且计时器为0；QuickPath/GenericWork/SlowBusiness |
| 选项在 Init 前生效、跳过 Custom | SessionOptions_AppliedBeforeInit_AndSkipCustomTimers |
| 自动场景 Loading 保持兼容 | 原有9项 Loading 集成全部包含在最终26项中 |
| Inspector 与 Prefab contents 重复执行 | EditMode 公共入口、Inspector按钮处理方法、正式Prefab内容加载/卸载、池/Tween清理与像素端点 |
| 0.5隐藏 + 0.53333336动作、控制独立 | RebirthTiming_CommitThenReveal_ControlsRemainOwnedByBusiness：0.75闭幕、内部暂停恢复、0.2秒复生许可后0.6666667揭幕；揭幕已完成时控制仍由复生持有 |

场景端口使用受控替身，页面和方块是真实实现，进度测试反射装配当前真实生成类型，不依赖测试复制业务算法。控制独立用例是框架编排证明，未调用 Windward 实际角色；原位不卸载通过场景端口与状态断言验证，不宣称完成实景人工验收。

## 首次失败、修复与复测

保留失败记录，不将多批局部绿拼成首轮全绿：

1. Windward 原始公开预览 NullReference：_containerRect 仅 Awake 初始化。公开预览现在自行初始化；两条入口复测通过。
2. 初始 Loading 13项，job `01c801a6e0c64a35844625c002cc673e` 失败4项：取消/失败与打开期间释放遗漏追踪/清理；增加 AbortCurtainPage、早期 PageAttached、队列所有权检查。第二轮 `5376a3d15e5c4bbb9126d1c81342c8b6` 12/13。
3. 上述剩余1项是 UniTask.AsTask 取消断言错误；改为原 UniTaskStatus.Canceled + OCE，单项 `93e304e1967c4e0cbec3863b541d46f9` 通过。
4. 扩充22项，`c9198949191943f29c236ca8c3b63f99` 21/22：进入期间销毁被分类为故障；改为 OperationCanceledException，并保留清理。最终26项包含该项。
5. 参数渲染首轮 `120a36917d374c84853c78c3c891708e`、`cffa0a91242447239478a6707e128627` 只得到背景，黑像素0。问题是证据夹具在 PreviewScene 的相机/Canvas渲染，不把蓝图宣称成功。改到测试当前场景的临时根、明确 Graphic.Rebuild。
6. 中间 additive 场景方案 `34eeaad23d7f4ed19690e0a0aedbfb2a` 因未保存 untitled 场景而失败；未保存/丢弃用户场景，改用可 finally 清除的临时根。
7. `de51cec4ed274314b5694187dfb491f1` 像素端点已正确，但测试退出释放仍被相机引用的 RenderTexture 报错；先解除 camera.targetTexture 再释放。最终 EditMode 4/4 无残留错误。
8. `745790ea3533467a9aa1431f27a65292` 23/25：新百分比断言暴露夹具以抽象 TMP_Text AddComponent 没有真实控件；改为 TextMeshProUGUI 且断言组件非空。条与数字最终均通过。
9. `50befe314e8841168d8929fcf02a6b25` 25/26：新增0.75秒时序用例被旧150帧上限提前截断，高帧率下150帧不足0.75秒。改为4秒实时时限，不降低行为断言；最终26/26。
10. 辅助程序集查询曾误用不存在的 Ember.Tests.EditMode 名称，查询返回无匹配元素；改为实际 Ember.UI.Tests。不是项目编译错误，也未用该查询失败冒充编译完成。

## Windward 交接与未扩展范围

按随包文档实施：提前取得 Loading → 等 Covered/原 placement门槛 → 原 RecoverAsync → 内部 Completion释放暂停 → 复生许可启动揭幕 → 复生动作独立恢复控制 → 表现收尾。用户暂停单独绑定句柄，失败进入项目恢复策略，finally释放句柄与订阅。

后续消费方独立执行真实死亡/慢恢复、一次计数/声音/冲量、0.5+0.53333336、挑战区/安全区计时、进入/退出暂停、取消、普通出口这些相关验收后，才能移除旧重复遮幕并签收。未运行 Windward、Mountain、全量测试、平台构建或手柄/全路线验收。本次没有发布授权，因此不会替消费项目安装这些改动。

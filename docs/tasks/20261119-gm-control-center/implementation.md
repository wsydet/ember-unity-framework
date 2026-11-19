> 当前实施回交为 **I2 / H33**：[implementation-i2.md](implementation-i2.md)，四稳定问题 V1-001—004 已修复并完成相关自检，等待主聊天派 V2 独立复验。V1 的 AC06/08/19 失败反证仍有效保留，以下为 I1 历史，不能单独作为当前通过依据。

# I1 实施交付：GM 控制中心与玻璃接线

- 任务：20261119-gm-control-center；输入 R14 / D10 / A1；日期：2026-11-19，Asia/Shanghai。
- 实施聊天：01a1f366-9c93-7cc0-b2dd-3f29baa7c3b1，hostId=local。沿用完整模式，回交主聊天等待独立验收。
- 结果：授权代码、九个目标 Prefab 和必要样式/质量资源已实施；已完成 Unity MCP 主动编译、域重载/MVID 确认和相关 Editor Play 自检。此记录不是独立验收或人工签收。
- 基线：codex/release-0.26.0，HEAD 65ae680732323d83de9f898f89e9e8cc576ee4c8；Unity 6000.5.4f1；embedded com.ember 0.26.0；正式活动编辑 base 0.11.1，hash 9077bd5b5271898e8055d1c7cf72b0a7。
- 未执行模板保存、Bump、切换、快照编辑、提交、发布或消费升级。既有 DefaultLightSans.asset 修改已逐字节保留。

## 实际改动

### GM 制作与运行

`Assets/Game/UI/Editor/UIFoundation/GMControlCenterAuthoring.cs` 是本轮有界维护源，已接入 `Ember/UI/柔光玻璃制作` 的 D10 预检/制作按钮。经 PrefabUtility 编辑实际资源，再调用公开 EUIBindingCodeGenUtility.TryRegenerateCode；未手写 Prefab YAML 或 Binding 生成字段。`eui-authoring-receipt.json` 保存九个目标 GUID、正式路径、Binding 条目/类型/节点/所有权与遮罩声明。

目标为 GM State/Time/Action/Center/Page、正式 Setting/Loading、SoftGlass Alert/Confirm。原 GUID 均保留。GM Center 移出 GMTest 嵌套和默认演示注册；独立 Showcase、Legacy、原 Provider 接口保留。固定区为标题/拖动/关闭、真实顶层 FSM 方卡、两个图标时间圆钮、一根倍率条和整宽帧率区，消费操作独立两列滚动。主面板没有全屏深色 Backdrop。

`GMControlLayout` 与 `GMPanelDragHandle` 只作用于 GM；没有改全局 ConstrainedPanel。标题占左两列并居中，只有专用圆钮移动，按住黄态，取消/失焦恢复。时间关闭仅禁调并保值，恢复始终有效，Unity/Ember 时间一起写入；倍率文本固定在竖条内。

### 帧率与真实 FPS

`UIFrameRatePolicy` 作为 base 业务策略，复用既有启动接线，不新增 Manager。设置与 GM 使用同一状态与应用入口。默认高响应自动、VSync 0；精确余量表及表间 ceil 已实现；自定义严格为正整数且无余量。节能/平衡分别 VSync 2/1，保留但禁用目标控件。GM 只有模式快切与实际配置摘要，没有目标编辑或二级弹窗。

`UIFrameRateControls` 连接正式制作的 Button/Toggle/TMP_InputField。自动刷新率变化才重算仍由策略持有的目标；检测到消费方改写实际配置后不逐帧夺回。当前只应用桌面策略，移动/XR控件禁用，摘要仍读引擎实际值。策略选择在本次运行中共享，没有另造玩家设置存档服务。

框架 `FPSDisplay` 修正未满窗口仍用完整窗口数作分子的错误，重新启用清空采样，跳过首次不完整帧区间，并公开只读 FramesPerSecond 供观察。显示来自实际时间采样，目标不参与 FPS 计算。最终短采样记录 487 个实际帧、约5.24秒，稳定窗口约119.51 FPS；当时实际配置为120Hz、VSync 0、target 124。MCP执行/截图会阻塞部分 Editor 帧，所以前期低样本读数含真实工具停顿，不把这组数据当 Player 性能承诺。

### 收藏、手势与 HUD

`GMFavorites` 只保存 schema 与稳定 key，默认空、最多5；第6项明确拒绝，注销项保留潜在收藏。损坏数据安全回空，去重/截取和存储失败有提示。PlayerPrefs 本身按应用与系统用户隔离；消费方有登录账户时用 SetUserNamespace 切换业务用户。

`GMActionGesture` 统一短按、右键与约550ms长按，收藏手势消费释放，不执行动作；滚动、移出、取消、失焦与第二指针会清理。`GMActionRegistry.TryExecute` 服务主面板/HUD；危险操作统一公共确认，回调再按 key 解析并检查 IsAvailable。实际发现 FreePage 30000 会挡住 Popup，已在确认期间临时把 GM 排到当前下层页之上、确认框之下，回调恢复30000，HUD内部展开状态保持。

`EUIFloatingEntry` 只增加默认关闭的长按能力、拖动开始/位置事件与可选 DockConstraint；既有 Activated、8×Canvas.scaleFactor 阈值、旧 Right/Y 键、底起归一化高度、安全区、左右吸边和0.18秒缓出保持。GM首次越阈值关闭 HUD，松开不重开。其它使用者不启用长按时维持原行为。

`GMHudLayout` 只避安全区、入口、相邻命中圆与显式 RegisterAvoidRect；不会识别任意世界物体。正常83%，候选方向/弧度优先，再76/69/62%。命中至少 max(可见直径,入口64%,48DU)，同心透明圆真正参与射线过滤。放大需4DU余量与约180ms稳定；无解明确提示改用主面板并保留收藏。`GMHudController` 在 LateUpdate 求解，使入口先完成旋转/尺寸适配，不每帧重建动作。

### Loading 与其它玻璃 UI

`EUIAppearanceBootstrap.InitializeUI()` 在 GameInit 的 UI 启动处设置默认质量并请求安装，ShowGM 不再安装玻璃或自动注册示例。URP安装器既有幂等机制保持；没有修改 URP/RenderGraph 后端。

Loading 根 Camera Canvas 增加 EUIBackdropGroup，使既有进度玻璃使用自己的边界；没有改加载、准备、进度或遮幕逻辑。正常会话轨道 Ready，快速隐藏进度不申请边界，Reveal/Dispose/销毁均释放新增消费者。

`GMBackdropSettings.asset` 是独立 GM 模糊质量（默认16DU、4倍降采样、2次、30Hz）；`SoftGlassDialog.asset` 是设置/公共弹窗专用表面样式，tint权重0.14。目标页面明确 schema1 与专用遮罩色 (.025,.06,.1,.18)，没有批量改全局遮罩、Legacy 或普通 Image。设置/Popup前景为深色；GM前景白色、选中控件深色文字。GM在浅色 base 下使用(.025,.07,.11,.82)采样染色权重以保证对比，已用彩色下层及0/32DU对照确认不是实色替代。这个权重不是网页CSS透明度的直接换算。

## 编译与资源证据

- 最终脚本批次通过 Unity MCP refresh_unity compile=request 主动提交；编译完成、域重载完成、编辑器非编译/更新状态和新程序集 MVID 均已确认。
- 证据：[compilation.json](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/i1/compilation.json)、[compile-completion-state.json](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/i1/compile-completion-state.json)。业务 MVID 989b9fd3-bcb5-4e2d-9b04-2d02bd3e53e1；UI MVID 8da9108d-adb1-4585-be1d-8b6e2c8bceea；Extension MVID 95c1008b-3e54-4e42-afde-01398716563b；Editor MVID 2ba7b34b-f502-489d-a52a-acbfc0753ff6。
- 编译错误为0。最终资源回读有一条 NativeFormatImporter 对既有共享 DingTalk SDF 的 inconsistent-result 警告，完整栈在 [final-console.json](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/i1/final-console.json)；该字体磁盘字节与任务前 HEAD 一致，未为消警修改字体。
- Play中显式销毁 Loading 视图的故障注入触发既有 OnResetDefault 对已销毁 Slider 的 MissingReference 警告；异常被原清理链捕获，session Released、边界2→1。未把故障注入当正常用户路径，也未扩展修复旧重置逻辑。
- 备份在 evidence/i1/before/，入口清单 [backup-manifest.json](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/i1/backup-manifest.json)。受保护字体最终SHA-256为14C9718D55D832593C689643CD752A277E0915FE418BB71F8F85C883653C0E4D，与进场一致。制作器保护既有项目/共享字体字节，且只保存指定配置，不再全局 SaveAssets。
- [static-final-checks.json](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/i1/static-final-checks.json)：23项通过，九个目标 GUID 与Binding目标完整；无模板快照、无测试Fixture生产落盘、QualitySettings和共享回退字体无遗留差异。

## 相关自检与逐项 AC

范围仅本轮新增/相关逻辑：Unity MCP内存执行的定向检查、Play中的真实资源与合成输入、真实渲染截图。未运行全量测试、长时性能、桌面Player构建或移动真机。

| AC | I1 实际结果与证据 | 自检边界 |
| --- | --- | --- |
| AC01 | 彩色动态下层真实透底，32DU下层轮廓/文字模糊、0DU可辨；前景保持清晰。gm-colored-blur32-b.png、gm-colored-blur0.png。 | Editor/URP实际渲染；恢复默认16DU，非GPU预算测试。 |
| AC02 | 16:9、720×1280、960×720均实际检查，无面板越界；竖屏633.6×793.6，窄屏按既有Canvas DU缩放。gm-final-landscape-1.png、gm-portrait-final.png、gm-narrow-async-final.png、narrow-layout.json。 | 窄屏同步相机截图出现空帧，最终用异步ScreenCapture并实际查看；空帧图不是交付依据。 |
| AC03 | 24项独立滚动，固定标题/FSM/时间/帧率不随滚动移动；panel-drag-scroll.json、gm-24-actions-scrolled.png。 | 合成滚动与实际布局。 |
| AC04 | 注册、重复key、注销、同key恢复、可用性与专用拖动验证；provider-favorites.json 19项、panel-drag-scroll.json。 | Provider为隔离运行夹具，结束全部注销，不随base默认注册。 |
| AC05 | 默认Registry为0，无UI库/Enhanced/背景自动演示，GMTest从正式树/绑定移出；独立Showcase实际打开过，Legacy资产保留。 | 未扩大Legacy布局整理。 |
| AC06 | policy-and-layout.json含14个自动边界/精确自定义/三模式/保值，合计123项几何和策略检查通过；真实120Hz→124。FPS初120观测与稳定3秒见fps-startup-and-stable.json。 | 未物理切显示器刷新率；未构建Player；不声称既往持续600FPS已在本机复现。 |
| AC07 | approval.md保存主聊天用户对D10及配套玻璃修复的明确实施批准，按A1进入I1。 | 旧未确认段落仅历史。 |
| AC08 | settings-gm-sync.json四项通过，完整设置改200，GM平衡禁用但保值，回高响应仍200；GM无目标编辑。 | 当前运行共享，不新增帧率持久化服务。 |
| AC09 | FSM实际读取GameLauncher顶层Main，集中FPS/Hz/实际目标，无重复底栏；Main→Gameplay→Main真实路线完成。 | 长名通过自动字号/换行制作；没有伪造玩家子FSM。 |
| AC10 | time-controls.json六项：默认禁调、同步、关闭保值、禁用指针无效、独立恢复且不开启、方向键有效；最终1.00×可见。 | Editor合成指针/键盘；条内Value旧隐藏问题已修复。 |
| AC11 | 正式方卡、单时间轨、两图标钮、帧率长条、无扩展标题/分割线/二级浮层；九资产真实生成。 | 截图与Binding回执。 |
| AC12 | 四列统一基准、时间纯图标、屏幕Hz/目标FPS标签、完整设置两类目标；settings-final-settled.png。 | 默认字体保留项目皮肤。 |
| AC13 | 只有专用拖钮移动，按住黄态，释放恢复；gm-drag-yellow.png、panel-drag-scroll.json。 | 取消/失焦清理同时有源码守卫。 |
| AC14 | action-gestures.json七项：右键、长按不执行、短按一次、滚动、移出、取消、第二指针。收藏星角标真实显示。 | 合成Unity事件，非触屏设备签收。 |
| AC15 | 入口长按后主面板关闭，HUD同调度普通/开关/危险确认；hud-dispatch.json、hud-confirm-completed.json。执行后保持。 | 默认空态可达；点外与ESC/再长按路径已接线。 |
| AC16 | 第6项拒绝不替换/执行、取消可补；5个较小圆钮与轻弧真实图。provider-favorites.json、hud-five-right.png、hud-portrait-settled-final.png。 | 角落3/5项几何由123项定向检查覆盖。 |
| AC17 | 原FPSDisplay、阈值/吸边/旧位置键保留；entry-drag-d10.json、entry-after-snap.json；主拖钮黄态保留。 | 验证改过的入口位置Prefs已恢复，未留测试落点。 |
| AC18 | 左右/角落候选、显式Rect登记、真实命中圆；hud-adaptive-tiers.json，实际圆内命中/圆外拒绝。 | 不识别任意世界物体/未登记UI。 |
| AC19 | 登记通道224/208/200DU实测76/69/62%，立即缩小；撤销后先保小档再恢复83%；全部无解收起且保留收藏。hud-adaptive-tiers.json、hud-no-space.json、hud-restricted-62.png。 | HUD放大采用4DU/180ms守卫；入口不缩放。 |
| AC20 | D10 glass-ui-audit.md只读审查保留，并按A1落实明确三类修复，没有批量重做URP/Legacy。 | 本轮不是六模板运行验收。 |
| AC21 | pointerdown/微动保留HUD，越阈值立即关，吸边后不重开，后续新位展开；entry-drag-d10.json、entry-after-snap.json。 | hud-rotation-final.json同时确认旋转不误报无空间。 |
| AC22 | Loading normal Covered轨Ready，quick隐藏无lease，Reveal/销毁回到1个主菜单边界。loading-normal-ready.json、loading-quick.json、loading-destroy-cleanup.json。 | 实际Main→Gameplay路线也完成，但抓取时已结束，未把该条记录当过场中截图证据。 |
| AC23 | 设置/Confirm实际透底模糊、深色文字清晰；专用SO+烘焙Prefab+维护源同步，旧声明不批量改。settings-final-settled.png、danger-after-order-fix.png。 | 原中文回退为DingTalk JinBuTi Regular，固有字形略倾斜；TMP样式实际Normal，非新增斜体。 |
| AC24 | 卸载GM后Main仍Ready/1边界；销毁旧host后不调用ShowGM，InitializeUI两次只建一个新host；Main、Settings、Popup、Loading均Ready。no-showgm-*.json、gm-unload-other-leases.json。 | 初始启动会正常显示GM；验证用独立重安装周期证明安装不依赖ShowGM。 |

## 偏差、修正与保留边界

1. 网页20%与Unity染色权重不是同一个量。本轮先在浅色真实base背景比较，再选择GM专用深色采样权重0.82和高对比选中态；0/32DU图证明下层仍被采样。全局 Neutral/Accent/Light未批量改。
2. 中文初期乱码来自该文案编码，已修正；现有中文回退字体的字形造型保留。曾由TMP自动填图集造成的磁盘改动已从任务前字节恢复，不作为任务改动。
3. 实施中发现并修正：时间文本沿用旧隐藏分支、设置控件重叠、GM遮住危险确认、HUD在旋转更新顺序中过早求解、首个不完整帧区间。失败/中间截图留作审计，最终图以上表指定为准。
4. Exit没有新增“更多”菜单，也没有默认注册消费动作；原Legacy退出和GameLauncher.Quit接口保留。按钮排序未纳入，仍Group/Order/Key。
5. 自动刷新率硬件变化、物理触摸、移动/XR、Development Player、六派生模板及消费项目均未签收。这些不伪写为通过；本轮授权的维护工程桌面实现与相关自检完成，独立验收由主聊天派发。

## 恢复与下一阶段

- 当前Assets/包源码就是待验收基线，未提交。新建 companion、配置及对应meta须随本任务保留。不要用旧 RevisionTwo.Apply 覆盖 D10。
- 回退时用 evidence/i1/before/ 对本轮实际修改路径定向恢复；新文件清单见 static-final-checks.json。受保护字体必须用进场备份，不能 git checkout 抹掉用户原有修改。
- 隔离I1 Provider、显式避让矩形、测试收藏命名空间、临时GameView尺寸均已移除；原GameView与入口位置偏好已恢复；Unity结束Play。测试截图/备份留在本任务.utmp中。
- 主聊天下一步派发独立验收，优先复核最终截图、24项AC、危险确认模态、Loading故障注入警告及无GM安装周期。没有需要用户补授权的实施阻断。

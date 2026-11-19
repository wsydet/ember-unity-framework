> 当前验收索引（2026-11-19，V2）：四个V1问题已由I2修复并通过独立定向复验，AC06/08/19改为通过；当前本地范围AC01—AC24全部通过。见 [acceptance-v2.md](acceptance-v2.md) 与 [evidence/v2/](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/v2)。以下V1原始失败结论、问题及证据完整保留为历史，不回写成当时通过。

# V1 独立验收：需返工（历史）

- 任务：20261119-gm-control-center；2026-11-19，Asia/Shanghai。
- 验收聊天：01a1f39c-e305-7490-93a6-2f8bafb4524c / local。独立于 I1 实施聊天，未代改产品。
- 输入：approval.md、current-approval-r14.md（有效 R14/A1）、requirements.md AC01—AC21、design.md D10、decisions.md、glass-ui-audit.md、实际实现及 I1 原始证据。
- **结论：21 项通过，3 项失败（AC06、AC08、AC19），4 个 P2 问题。当前不得标记整体验收通过或归档清理。** 由主聊天派原实施聊天返工，再定向复验。
- 边界：实际 base Assets + 必要框架，Windows Editor/URP。通过不代表模板已保存/分发、Player、移动/XR、触屏设备、其它模板或消费项目通过。

## 实际基线与验证方法

分支 codex/release-0.26.0，HEAD 65ae680732323d83de9f898f89e9e8cc576ee4c8；Unity 6000.5.4f1；manifest file:com.ember，实际 embedded Packages/com.ember 0.26.0；正式活动编辑 base 0.11.1，contentHash 9077bd5b5271898e8055d1c7cf72b0a7。base 专属 glossary 不存在。

先独立读有效 AC 和当前源码，再将 I1 日志作为待核对输入，选择相关行为重新执行。本轮没有运行 Unity Test Runner 全套或长时性能测试。Unity 操作串行；运行夹具仅在内存，通过已有 Provider 接口、实际正式 Prefab、当前 EUIManager 和真实渲染链验证。

输入证据包括实际 EventSystem.RaycastAll 命中结果，再经 ExecuteHierarchy/ExecuteEvents 分发鼠标语义事件，覆盖右键、长按、短按、HUD 命中及入口越阈值拖动。它是 Unity EventSystem 链路验证，**不是操作系统鼠标录制、物理触摸或设备签收**。部分控件状态核查调用了实际 Button/Toggle/InputField 的已接线 UnityEvent；几何诊断明确另列，未替代真实 UI 接线。

主动请求过本轮脚本编译，资源记录显示编译完成、域重载完成、非更新；随后查 Console。为区分 Play 静态状态与冷启动持久化，另作一次明确编译/域重载。证据：evidence/v1/compile-request.json、compile-completed.json、domain-reload-request.json、domain-reload-completed.json。已加载业务程序集 MVID 989b9fd3-bcb5-4e2d-9b04-2d02bd3e53e1，与 I1 交付的同一内容基线一致；本轮未改源码，MVID 不变是正常情况。

FPS 组件重新启用后，用真实 URP endFrameRendering 时间戳独立采样首 120 帧并继续至少 3 秒。fps-startup-stable.json 共 487 帧/约 3.967 秒，首 120 帧实测约 124.61 FPS、末窗口 124.09 FPS，对应实际请求 124。启动窗口已不再用完整窗口数虚构分子；仍存在真实短帧波动，不能保证每帧都等于目标。本轮没有复现历史持续 600 FPS；该历史现象仍须区分启动统计错误与真实不限帧，不能由这次限帧采样倒推唯一旧根因。

## AC01—AC24 逐项结论

以下通过均限上述本地维护工程范围；设备和分发边界统一在后文列为待验证，不混入通过结论。

| AC | 结论 | 独立判断与可复核证据 |
| --- | --- | --- |
| AC01 | 通过 | 在主菜单下层加入仅内存的彩色移动条纹，GM 中颜色与轮廓随下层变化。32 DU 明显模糊边缘，0 DU 可辨分界，前景清晰。新图 gm-moving-blur32-a.png、gm-moving-blur32-b.png、gm-moving-blur0.png；恢复原 16 DU，文件字节不变。0.82 偏差另见下文。 |
| AC02 | 通过 | 新验 2560×1440、720×1280、960×720；竖屏面板 633.6×793.6，窄横屏 420×619.2，无越界。有效最终图 portrait-gm-final.png、narrow-gm-final.png，配套 *-final-state.json；保持面板外可见游戏。 |
| AC03 | 通过 | 运行时注册 24 项，扩展滚至底部，标题与时间 Value 世界位置前后一致，滚动值到约 0；见 scroll-fixed.json、gm-scrolled.png。真实固定区仅 FSM/时间/帧率，未默认加诊断样本。 |
| AC04 | 通过 | 复核 Registry/Register/Dispose/Snapshot 的真实接口与版本刷新；本轮真实注册 24 项并完整注销。I1 provider-favorites 原始用例的重复 Key、注销、迟注册与 IsAvailable 结果和当前实现一致；本轮经真实主列表执行、确认回调再校验及实际 ScrollRect 重放。未纳入按钮排序。 |
| AC05 | 通过 | 新启动 Registry=0、收藏默认空；ShowGM 无 RegisterSamples，正式 Center 树/Binding 无 GMTest；独立示例和 Legacy 资源保留。play-initial.json、runtime-cleanup.json、当前 Bootstrap.GM/Center 及 I1 正式制作回执交叉核对。 |
| AC06 | **失败** | 默认高响应自动、120 Hz→124、精确自定义 200、VSync 配置与真实 FPS 短采样有效；但界面全关时刷新率检查停更（V1-001），无域重载再次 Play 的选择/实际目标不一致（V1-003）。不能把策略计算正确等同完整运行策略通过。 |
| AC07 | 通过 | approval.md 保存主聊天用户对 D10 和配套三类玻璃修复的明确授权；与 D10/R13 网页、历史零 Unity 方案记录及 I1 派发顺序相符。旧“未确认”状态由 A1 覆盖，不重新征求同一授权。 |
| AC08 | **失败** | 实际设置选自定义 200→平衡禁用但保值→打开 GM 切高响应，两个摘要都为 200，GM 无目标输入/浮层；见 settings-balanced-sync.json、settings-custom200.json。页面内共享成立；完整设置没有跨启动保存（V1-002），再次 Play 还有 V1-003。 |
| AC09 | 通过 | 顶部实际 Main 与 GameLauncher.Fsm.Current.Name 对应；真实 Settings 路线已进入/返回。FSM/FPS/Hz/实际目标集中，无重复底栏。长名处理核对真实 TMP 自动字号/换行与制作源；未伪称玩家 FSM 或模拟值。 |
| AC10 | 通过 | 实际禁用指针不改时间；启用后 Unity/Ember 同步；关闭保留 2×，关闭状态复位至 1×且不开启；竖向方向键可调。time-controls.json 五条均真；最终恢复 1×。 |
| AC11 | 通过 | 当前正式资产及三种画幅实图为方卡、右侧唯一倍率轨和两图标钮、整宽帧率区；无旧暂停卡、二级浮层、扩展标题/分割线。*gm-final.png、gm-default.png 及正式 Binding/制作源对应。 |
| AC12 | 通过 | 四列同源边距与中心线，时间两钮纯图标；屏幕 Hz 与目标 FPS 标签明确。完整设置输入 200 与自动切换接线真实有效，设置模式/自动/自定义区域无重叠；settings-custom200.png 已看。 |
| AC13 | 通过 | 标题跨左两列并居中，四个圆钮使用相同尺寸；只有 GMPanelDragHandle 写面板位置。实际按下染黄，Cancel 恢复原色，见 drag-yellow-cancel.json；释放/失焦守卫与 I1 实际移动日志交叉核对。 |
| AC14 | 通过 | 经射线命中实际 Action 后右键收藏不执行，实际等待长按后释放也不执行，随后普通短按+click 只执行一次。right-click-eventsystem.json、long-press-eventsystem.json、short-press-eventsystem.json；星角标新实图可见。滚动/移出/取消/第二指针源代码与 I1 事件日志一致。 |
| AC15 | 通过 | 实际入口长按释放后主面板关闭而 HUD 打开；HUD 状态开关经真实射线命中执行一次且保持展开。entry-long-hud.json、hud-toggle-raycast.json。危险动作采用统一 Registry 确认；确认按钮实际命中 Accept，在等待回调完成期间不可用项不执行，order 恢复 30000；danger-revalidate-completed.json、danger-settled.png。空态中文及关闭接点已核对。 |
| AC16 | 通过 | 第六收藏拒绝，原五项不替换且不执行；重新装载命名空间及跨 Play 仍保留原顺序五项。favorite-cap-persistence.json、favorites-after-restart.json。实际 HUD 五圆直径 63.08 < 入口 76，轻弧、彼此分离。 |
| AC17 | 通过 | EUIFloatingEntry 原 8×Canvas.scaleFactor 阈值、左右吸边、归一化高度/原键与 0.18 秒缓出保留。实际射线链拖到左下，位置保存接近 Left/Y=0，再开 HUD 按该位置排布，hud-three-final.json/图；真实 FPS 组件采样独立。原位置键进场不存在，最终恢复为不存在。 |
| AC18 | 通过 | 五项实际 UI 四角排布与入口/彼此不重叠，hud-five-corners.json；竖屏尺寸适配后新图 portrait-hud.png 和 portrait-hud.json 无越界。真实 GMCircleHit 圆内通过、圆外拒绝，hud-toggle-raycast.json。显式登记 Rect 生效；全覆盖时收起且保留收藏并显示 Alert，hud-no-space.json。 |
| AC19 | **失败** | 实际左下三项紧凑、新图 hud-three-final.png；四角五项、受限实际 62% 都成立。但移除登记避让后不能自动恢复 83%，V1-004。final-shrink.json、final-grow-check.json、final-grow-settled.json、grow-guard-analysis.json。I1“稳定后恢复”声明未被采纳。 |
| AC20 | 通过 | 独立重读 D10 逐类审查并复核实际正式入口/Group/样式/安装源。已接入、小范围候选、应保持非玻璃和待运行类别区分正确；A1 后仅改获准目标，未批量修改 Legacy/World/模板快照。不是六模板运行验收。 |
| AC21 | 通过 | 实际入口 down→HUD 保持，2 px 微动保持，100 px 越阈值立即关；释放吸边不重开、收藏保持。entry-drag-threshold.json、entry-snap.json。原入口长按和移动手势互斥；之后可在新位置展开，未执行项目动作。 |
| AC22 | 通过 | 普通会话 Covered 时 Loading 根 Canvas 独立、进度面 Lease Ready；Reveal 后新增边界释放。快速模式无可见进度玻璃和租约。销毁 Loading 的故障注入最终 session Released、边界 3→2、视图消失；loading-covered.json、loading-revealed.json、loading-quick.json、loading-destroy.json。已记录既有 Reset 警告，不称零警告。未变更加载/遮幕业务。 |
| AC23 | 通过 | 设置实图前景清楚、区域无重叠；公共 Confirm 在彩色下层实际透底模糊且可点。settings-custom200.png、danger-settled.png。schema1/显式 mask tint .18、专用 SoftGlassDialog .14、Prefab 烘焙颜色与 GMControlCenterAuthoring 制作源一致；全局/Legacy 未批量替换。 |
| AC24 | 通过 | 实际 GM Unloaded 后只有主菜单一条边界且 Ready；销毁旧 host 后不调用 ShowGM，InitializeUI 连续两次只得一个新宿主，随后主菜单恢复 Ready。gm-unload.json、no-gm-install.json、no-gm-ready.json；启动源 GameInit 调用 InitializeUI 与 ShowGM 解耦。 |

表内未带目录的本轮文件均在 evidence/v1/。I1 文件明确标为交叉核对，不算本轮新跑的测试数。

## 稳定问题与返工要求

### V1-001 / P2：GM 与设置关闭后，自动跟随不再检查刷新率

- 影响：AC06；来源 UIFrameRateControls.cs:38、UIFrameRatePolicy.cs:61。
- 复现：高响应自动，关闭 GM 主面板和设置，保留悬浮入口；跨不同刷新率显示器/系统变更刷新率后继续游戏。
- 预期：自动策略继续按真实刷新率更新，不依赖编辑界面是否打开；仍尊重明确的外部配置接管。
- 实际/证据：CheckRefreshRate 仅在 UIFrameRateControls.Update 调用；两控件均 inactive。白盒探针把缓存设为 60，而实际屏幕保持 120，等待约 1.12 秒后缓存仍 60；显式检查才变 120。closed-auto-watch.json、play-initial.json、frame-rate-source.json。**该探针不是物理切屏实测**，源码唯一调用点与实际 inactive 状态共同证明缺少常驻更新入口。
- 返工：将策略生命周期和刷新率检查接到获准的 base 启动/更新业务，保留外部接管保护；定向复验两界面关闭的情况。

### V1-002 / P2：完整玩家设置的帧率选择没有跨启动保存

- 影响：AC08 及批准方案的 base 设置存储契约；UIFrameRatePolicy.cs:15—20、38、51—59。
- 复现：通过真实完整设置选择自定义 200，切模式保留目标；关闭，再发生真实程序集域重载并重新启动。
- 预期：非首次启动恢复玩家保存的模式、自动/自定义选择及合法目标；GM 快切同样更新该选择，默认值用于无有效记录时。
- 实际：只有静态属性，无读写存储。页面关闭不丢，但明确域重载后 Automatic=true、CustomTarget=180。settings-before-restart.json、settings-custom200.json、policy-after-domain-restart.json、domain-reload-completed.json；现有 base PlayerPrefsModule 已提供类型安全读写，frame-rate-source.json 保存源码。
- 判定依据：design.md:252 写“玩家设置与选择保存在base业务接线，优先核对已有设置服务”；R8 要求 GM 快切保留设置的类型与值。不能把原型阶段“不新增持久化系统”理解成正式完整设置永远仅会话有效；复用现有 base 偏好服务不需要新 Manager。故判功能缺口，而非仅记录技术偏差。这里是根据完整设置语义和已批准方案作的验收解释，**不是声称 AC08 原句逐字列了冷启动步骤**；若主聊天用户明确决定仅会话语义，应留下新的需求决定，不能由实施自行移出。
- 返工：采用已有合适的 base 偏好路径，验证首次默认、合法值保存恢复、损坏值回退及 GM/设置两入口一致。不要为此新建全局 Manager。

### V1-003 / P2：禁用域重载再次 Play，选择与实际引擎目标不一致

- 影响：AC06/AC08；UIFrameRatePolicy.Initialize 的静态 _initialized 生命周期。
- 环境：当前项目 enterPlayModeOptionsEnabled=true，DisableDomainReload。
- 复现：真实设置高响应自定义 200→停止 Play→再次 Play，保持项目原有禁用域重载选项。
- 预期：新运行开始时，以有效保存/当前选择正确应用策略，UI 所选策略与实际目标相符；单次运行内重复 Initialize 仍幂等。
- 实际：Mode=Responsive、Automatic=false、CustomTarget=200，但引擎 targetFrameRate=124、vSync=0；Initialize 因保留的 _initialized 直接返回。reenter-play-policy-mismatch.json、settings-after-restart.json。明确域重载后的恢复默认另属 V1-002，不混为同一次实验。
- 返工：补运行代次/静态重置与重新应用的明确契约，测试当前 Enter Play Mode 设置；不要要求用户开启域重载掩盖。

### V1-004 / P2：HUD 缩小后不能自动放大恢复

- 影响：AC19；GMHudController.cs:128、GMHudLayout.cs:29、63、74—77。
- 复现：真实入口拖至左下，三收藏展开；登记右侧大矩形使可用通道仅 93 DU，实际选择 tier3/62%；移除登记矩形，保持 HUD 展开，等待超过 180 ms。
- 预期：宽裕后经过约 180 ms 稳定窗口恢复 83%，无需收起重开；入口大小/位置与收藏不变。
- 实际：等待 13.69 秒后仍 tier3，_pendingTier=-1，后续渲染帧也仍 tier3。final-shrink.json、final-grow-check.json、final-grow-settled.json。真实左下三项图与坐标为 hud-three-final.png/json。
- 原因证据：guard=4 时，Clear 要求相邻中心距至少 hit+5+8=hit+13；侧列只构造 hit+12，局部弧只构造 hit+8 或 hit+6，候选未把 guard 计入间距。在该真实入口位置，无避让时四个 tier 均 guard0=true、guard4=false，grow-guard-analysis.json。因此恢复候选被全部拒绝且 pending 被清空。
- 返工：统一“额外空间余量”与候选间距的含义/构造，避免放大检查否定自己的正常布局。定向复验三/五项角落、76/69/62→83 自动恢复、180 ms 防抖及命中不重叠；不能只 force rebuild 得到大档就算恢复。

## 提示文末重点与偏差判定

- 空态乱码：新 gm-default.png 中“项目尚未注册扩展操作”正常，HUD 空态源码为正常中文；受保护字体未被修复/替换。DefaultLightSans.asset SHA-256 始终为 14C9718D55D832593C689643CD752A277E0915FE418BB71F8F85C883653C0E4D。
- 浅底对比/时间倍率：新默认、竖屏、设置图已看；GM 白字、选中态/设置深色字清楚；1.00×在轨内。time-controls.json 覆盖真实同步、禁用和恢复。
- GM 遮住危险确认：新 danger-settled.png 是有效稳定画面；Accept 射线命中，回调完成后 order=30000，IsAvailable=false 的危险动作没有新增执行。最初 danger-confirm.png 是重建相机阶段的空帧，**不得作为视觉通过依据**。
- 设置模式与自动重叠：新 settings-custom200.png 三块分离，真实已激活设置控件修改成功；缓存/预加载的 inactive 控件不算实际操作证据。
- 首次尺寸适配/HUD：portrait-hud.json 与新图验证入口完成尺寸适配后再布局；最终主面板视觉只用 portrait-gm-final.png / narrow-gm-final.png。早期 portrait-gm.png 捕获跨帧后变成 HUD，不拿它作面板证据。
- 隐藏重显：hide-reenable.json 记录隐藏后 lease=null，reenable-ready.json 的六个活跃表面重新 Ready；新真实渲染图可见圆钮，未以统计 Ready 单独替代玻璃视觉。
- **GM 染色 0.82：接受为本轮工程偏差，未另列失败。** 与 D10 原型相比确实更厚、在浅底上更偏灰；D10 原型无严格 Unity alpha 数值合同，且该值是采样混色权重，不能按 CSS 透明度换算。新移动彩色下层与 32/0 DU 图显示明显颜色/轮廓透底，前景在浅底仍可读，达成通透模糊的可观察要求。此结论不是仅检查 shader Ready 或“有采样”标志。若用户仍要求更轻外观，按视觉新决定返工，不能声称数值与原型完全相同。
- 帧率仅会话共享：不接受为完整设置交付，依据和冷启动/无域重载两种真实结果分列 V1-002/003。
- HUD 放大：早期单次取样不足以判断 180 ms，随后单独重建了稳定位置、登记/移除与等待实验，最终确认为 V1-004；没有把中间观察或 I1 声明当最终结论。

## Console、保护与未覆盖部分

两次本轮编译后 error/warning 查询均空。最后的定向销毁 Loading 故障注入重现既有 Logic.OnResetDefault 对已销毁 RectTransform 的 MissingReference 警告；session 随后 Released，新增边界释放。console-final.json 保留该警告，不宣称零警告或把它列为新增产品缺陷。I1 的共享字体导入警告本轮没有重现。

本轮全部生产文件最终 SHA-256 与进场一致，final-file-differences.json 为 []；Git 路径/状态边界完全一致，git-before.txt 与 git-after.txt 可对照。没有写任何产品代码/资源/模板，没有模板保存/Bump/切换、提交、发布或消费升级。运行 Provider 已注销，验证收藏专用键删除，原入口偏好键恢复为进场的不存在，GameView 还原 index5/2560×1440，自建分辨率移除，倍率1×。最终 final-confirmed.json 与 editor-final.json 确认非 Play、非编译、非更新，场景仍 FrameworkScene。

待验证边界：真实硬件切换刷新率、Player 实际 VSync 行为/性能、移动/XR策略、物理触摸/多指设备、设备刘海安全区、其它保存模板、模板分发及消费项目。本轮不包含全量或长时测试。返工后的定向复验至少覆盖 V1-001—004 及其直接影响的 AC06/08/19；修复若改共享启动或布局，补相关生命周期/输入回归。

阶段正常结束回交主聊天；主聊天统一调度 I2，不由本阶段反向发送消息或创建下一聊天。

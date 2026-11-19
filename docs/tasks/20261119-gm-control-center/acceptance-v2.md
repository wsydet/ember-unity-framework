# V2 独立定向复验：本地验收通过

- 任务：20261119-gm-control-center；2026-11-19，Asia/Shanghai；R14 / D10 / A1。
- 验收聊天：01a1f39c-e305-7490-93a6-2f8bafb4524c / local，继续 V1 独立角色。
- 输入：prompts/06-reacceptance-v2.md、H33、implementation-i2.md、I2 六文件实际差异、V1 原问题和反证。本轮独立执行新探针，没有直接采用 I2 的通过声明，也未重跑旧 123 项。
- **当前结论：V1-001—004 均关闭；AC06、AC08、AC19 改为通过。结合 V1 其余 21 项及本轮差异/回归核对，当前本地范围 AC01—AC24 全部通过，无仍开放的验收问题。**
- 通过范围：当前维护工程实际 base Assets + 必要框架、Windows Editor / URP。Player、硬件切换刷新率、移动/XR、物理触摸/多指设备、设备安全区、其它模板、模板分发与消费项目仍未签收。由主聊天核对后收束/归档，本阶段不自行归档或发布。

## 基线与变化核对

Unity 6000.5.4f1，实例 ember-unity-framework@b23e9b768e85ed30；FrameworkScene；embedded Packages/com.ember 0.26.0；正式编辑 base 0.11.1；Git codex/release-0.26.0，HEAD 65ae680732323d83de9f898f89e9e8cc576ee4c8。

独立对照 V1 生产指纹，恰好六文件变化：UIFrameRatePolicy.cs、UIFrameRateControls.cs、EUIAppearanceBootstrap.Startup.cs、GMHudLayout.cs、GMHudController.cs、base/soft-glass-ui.md。见 evidence/v2/differences-from-v1.json；已逐段阅读实际源码与 I2 diff。Prefab、Binding、框架 FPSDisplay、入口组件、样式、渲染后端、时间控制、Registry/危险确认实现均未变化。

运行前已加载业务 MVID 为 **4142319a-11da-4c49-9812-a81035d3a0c8**，与 I2 最终基线一致。为真实验证域重载后保存恢复，本轮通过 Unity MCP 主动请求编译，并确认本次 compile finished / domain reload after / 非更新，随后读取 Console。内容未变，编译后的 MVID 保持相同。证据 compile-request.json、compile-completed.json、compile-console.json、after-domain-before-start.json。

## 四个稳定问题的关闭依据

| 问题 | V2 状态 | 本轮独立结果 |
| --- | --- | --- |
| V1-001 | **关闭** | 全部帧率控件 inactive 且 Unity/Ember timeScale=0；将内部刷新率缓存设60、实际显示器保持120Hz，不手动调用 CheckRefreshRate，后台自动恢复缓存120、目标124。随后外部接管173/VSync1，重复 InitializeUI 两次并继续等待，值不被覆盖，同一 watcher lifetime 身份保持。 |
| V1-002 | **关闭** | 复用 PlayerPrefsModule 的版本记录实际落盘；真实激活设置页 Toggle/InputField 保存精确207；真实 GM 平衡按钮保存同一记录、保留自定义207。真实 Stop/Play 和明确程序集域重载后的新 Play 均恢复并应用207。缺失/损坏和合法同步记录另做隔离诊断。 |
| V1-003 | **关闭** | 项目原 enabled=true/DisableDomainReload 始终未改。真实新 Play 第一次观察、调用任何策略 setter 之前，模式Responsive、Automatic=false、CustomTarget=207、target=207、VSync0。旧 lifetime Stopped/取消，新 lifetime 不同；旧 Changed 回调在新启动及后续合法选择时均未调用。 |
| V1-004 | **关闭** | 3/5项八角落全部实际 HUD 正常83%，完整命中圆、安全区、相邻/入口分离、圆内/外过滤与首射线命中通过。六组76/69/62%实际缩小后持续展开，在移除避让后约0.246—0.291秒自动恢复83%；24次约100ms释放都保持小档，不累计旧计时提前放大。没有在移除避让后调用 Position(force)、Build、Toggle 或重开。 |

### V1-001：关闭界面、暂停时间、接管与取消

default-start.json 记录无偏好时的正式启动：Main、高响应自动、备用180、实际120Hz→124、VSync0，活跃帧率控件为空。

closed-paused-auto.json：V1 原缓存失效条件重放，缓存60→120，观察间隔约14.36秒，activeControls=0、timeScale=0、target124。这里是**白盒缓存失效探针，未真实切换显示器刷新率**。与 V1 相反，新代码无须打开 GM 或设置即可运行检查。

external-takeover.json：接管为 target173/VSync1、再次使缓存失效，重复 InitializeUI 后约2.24秒仍维持173/1，缓存已更新120，watcher会话身份相同。静态检查确认同一会话只在 _frameRateLifetime 不同时启动循环；CheckRefreshRate 已从控件 Update 移除，500ms UniTask Delay 忽略时间缩放并绑定 lifetime.Cancellation。

old-session-stopped-final.json 与 final-confirmed.json 确认旧会话 Stopped/取消；actual-reenter-play.json 确认新会话对象不同。源码取消 catch 与新会话 identity 防护一致，本轮未见重复循环或取消异常。没有用普通界面刷新代替后台检查。

### V1-002/003：记录与真实启动证据分开

preferences-diagnostic.json 是隔离键诊断，不冒充启动：8种无效/缺失记录均回退高响应自动/180且不覆盖坏原文；4种合法记录分别覆盖 Economy自定义207、Balanced自动/备用211、Responsive自动/备用211、Responsive自定义207。VSync分别2/1/0，自动实际目标124，自定义严格207；非法编辑不改变已有记录。该组为验证解析而使用私有 BeginRun；测试后恢复正式 _storageKey 和隔离偏好。

正式控件证据：

- settings-wired.json：实际激活完整设置，Toggle/InputField 已接线 UnityEvent 保存 `1|2|0|207`，引擎目标207。
- gm-wired.json：实际 GM 页打开后，平衡按钮保存 `1|1|0|207`、VSync1；自定义类型/207保留，实际摘要为120 FPS/VSync1。切回高响应后正式记录为 `1|2|0|207`。
- actual-reenter-play.json：真正 Stop→确认Stopped/非Play→Play，**在本次新运行任何 setter 之前**读取保存选择与实际207/0一致，旧回调0、新会话身份不同。
- stale-callback-final.json：上述初次快照之后才调用合法 setter，旧 Changed 计数仍0。
- after-domain-before-start.json：真实编译/域重载后非Play、_initialized=false，磁盘偏好仍 `1|2|0|207`；actual-after-domain-start.json 为随后真正新 Play 的第一次观察，无 setter，Main、_initialized=true、保存选择/实际207一致。

reenter-callback-check.json 是较早的一次回调探针，在读取目标前曾调用 setter，因此**不作为新启动自动应用的决定性证据**。另做了上面的全新 Stop/Play 并先读实际值，避免 setter 掩盖 V1-003。旧 old-session-stopped.json 同样保留早期 playing=true 状态，最终采用 old-session-stopped-final.json。

### V1-004：真实持续展开和防抖

本轮独立探针源为 evidence/v2/hud-v2.csbody，可复现完整流程；报告 hud-v2-results.json，摘要 hud-summary.json。

角落夹具只在每组开始时通过原入口的左右/归一化高度语义字段与 Reposition 建立位置并打开 HUD。随后正式 Provider、收藏、实际 Prefab、注册避让 Rect 及普通 GMHudController.LateUpdate 驱动全部缩小、抖动与恢复。几何函数只用于寻找恰好触发各小档的压力通道，判定来自实际 UI。

| 收藏数 | 实际缩小档 | 每组100ms释放次数 | 是否提前放大 | 最终释放到自动83% |
| --- | --- | --- | --- | --- |
| 3 | 76% | 4 | 否 | 0.246秒 |
| 3 | 69% | 4 | 否 | 0.285秒 |
| 3 | 62% | 4 | 否 | 0.291秒 |
| 5 | 76% | 4 | 否 | 0.257秒 |
| 5 | 69% | 4 | 否 | 0.286秒 |
| 5 | 62% | 4 | 否 | 0.291秒 |

24个短释放窗口实际约0.1000—0.1074秒，逐渲染帧检查，没有一次提前放大；最终稳定释放的前180ms仍保持小档，随后恢复。期间所有组 HUD 始终展开，入口位置/76DU大小与收藏顺序不变。八角落均正常83%；六组小档及六组恢复态共12个实际几何/过滤/首射线检查均通过。

最后以真实 EventSystem.RaycastAll 命中 Action，再 ExecuteHierarchy 分发 down/up/click，普通动作只执行一次，HUD保持打开。它是 Unity 实际 EventSystem 的鼠标语义合成链路，**不是 OS 鼠标录制或触屏设备签收**。相较 V1，恢复时不再出现 guard4 全拒；新几何变化清空 pending 也经24次短抖动验证，而不是只恢复一次就算通过。

## 当前 AC01—AC24 状态

“保持”表示 V1 通过项在六文件差异与直接回归核对后仍有效，不是声称本轮重新运行全部旧用例。V1完整方法/截图/历史结论保留于 acceptance.md 和 evidence/v1/。

| AC | 当前结论 | 当前依据 |
| --- | --- | --- |
| AC01 | 通过（保持） | 玻璃参数/Prefab/后端未改，V1动态32/0DU视觉证据有效。 |
| AC02 | 通过（保持） | 响应面板/资源未改；本轮HUD八角落补验。 |
| AC03 | 通过（保持） | 固定区/扩展ScrollRect实现未改，V1真实24项滚动结果保持。 |
| AC04 | 通过（保持） | 注册接口未改；本轮实际注册/注销六动作，执行与清理正常。 |
| AC05 | 通过（保持） | 默认清理接线未变，结束Registry0，无夹具生产落盘。 |
| AC06 | **通过（V2复验）** | 默认/精确目标、关闭界面暂停检查、接管保护和真实新运行应用通过；FPSDisplay未改沿用V1真实采样。 |
| AC07 | 通过（保持） | 原R14/D10/A1授权与先网页后实施顺序保持。 |
| AC08 | **通过（V2复验）** | 真实设置与GM共享保存，非法回退、原DisableDomainReload新Play和明确域重载新启动恢复通过。 |
| AC09 | 通过（保持） | FSM/FPS布局与真实来源未改，本轮实际Main/Settings路线正常。 |
| AC10 | 通过（保持） | 时间控制实现/资源未改，本轮暂停/恢复Unity与Ember时间并恢复1×。 |
| AC11 | 通过（保持） | 正式方卡/单轨/无浮层结构未改。 |
| AC12 | 通过（保持并补验） | 布局资源未改；真实设置207、GM同步模式保值与摘要复验。 |
| AC13 | 通过（保持） | 面板拖钮/黄态/关闭实现与资源未改。 |
| AC14 | 通过（保持） | 收藏手势实现未改，V1EventSystem互斥证据保持。 |
| AC15 | 通过（保持并补验） | HUD普通动作经实际EventSystem执行一次；危险/开关共享分发未改。 |
| AC16 | 通过（保持并补验） | 上限/存储实现未改；新3/5项真实大小、轻弧与收藏顺序保持。 |
| AC17 | 通过（保持） | 入口组件/键/FPS组件未改；新夹具恢复入口语义状态且未写位置键。 |
| AC18 | 通过（保持并补验） | 八角落、12个小档/恢复态真实命中边界/安全/相邻/入口/避让分离通过。 |
| AC19 | **通过（V2复验）** | 六组76/69/62→83持续展开恢复及24次100ms抖动通过，无force/rebuild/reopen替代。 |
| AC20 | 通过（保持） | V1玻璃审查及授权边界未变；六差异无批量迁移。 |
| AC21 | 通过（保持） | 越阈值收起订阅/入口实现未改，V1实际输入证据保持。 |
| AC22 | 通过（保持） | Loading资源/生命周期未改；本轮未重复故障注入。 |
| AC23 | 通过（保持） | 设置/Popup材质样式资源未改；实际设置接线复验正常。 |
| AC24 | 通过（保持并核对） | 安装与GM解耦保持；新帧率监测跟会话且同会话初始化幂等。 |

## 编译、保护与结束现场

本轮编译及最终 Console 查询均无 error/warning；最终业务 MVID 与 I2一致。本轮没有重复 Loading 销毁故障注入或扩测0.82染色，V1已有分类仍有效，不因此改写历史为“全程零警告”。没有硬件切Hz、Player或设备测试，没有全量测试/旧123项重跑。

evidence/v2/production-before.json、protected-before.json 与 production-differences-after.json 证明全部当前生产文件及 QualitySettings、EditorSettings、正式编辑模板身份等最终字节不变；git-before.txt / git-after.txt 边界一致。受保护字体 SHA-256 仍 **14C9718D55D832593C689643CD752A277E0915FE418BB71F8F85C883653C0E4D**。

正式帧率键、隔离键、V2收藏键及入口键均恢复原先不存在；若原键有内容，探针保存并按原值恢复，未使用 DeleteAll。Provider/避让/临时对象均注销或销毁，命名空间回local，Registry0、本地收藏原空、倍率1×。GameView未改，最终index5；Editor target124/VSync0恢复进场值，DisableDomainReload配置不变。preferences-restored.json 与 final-confirmed.json 留证。

final-confirmed.json / editor-final.json 确认 Unity非Play、非编译、非更新，FrameworkScene不变，最后会话Stopped且Cancellation已取消。没有产品代改、模板保存/Bump/切换、提交、发布、消费升级、归档或新聊天。

本阶段正常结束交回主聊天；主聊天可在当前本地验收范围满足后核对交付并按既有完整流程收束，未覆盖的平台/分发边界不得据此标为已签收。

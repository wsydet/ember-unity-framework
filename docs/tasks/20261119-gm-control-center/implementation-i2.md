# I2 定向返工回交 — V1-001—004

2026-11-19；R14 / D10 / A1，沿用完整多聊天模式。实施聊天 `01a1f366-9c93-7cc0-b2dd-3f29baa7c3b1` / local；主聊天 `01a1f2a7-d1b7-7073-b4d0-4331239ed002` / local。本轮依 `prompts/05-rework-i2.md` 完成四项代码修复及相关定向自检，**待主聊天安排 V2 独立复验，不表示整体验收通过**。V1 的 AC06/08/19 失败和全部原始证据保留。

## 修复对应

| 稳定问题 | 实际修改 | 本轮证据与自检结论 |
| --- | --- | --- |
| V1-001 / AC06 | `EUIAppearanceBootstrap.Startup.cs` 在既有 UI 启动接线启动 UniTask PlayerLoop 检查，每500ms一次，绑定 `GameLauncher.Lifetime.Cancellation`，同一会话重复 InitializeUI 不再启动另一循环。移除 `UIFrameRateControls.Update` 的策略检查，仅保留呈现刷新。没有新增 Manager、场景组件或常驻 GM 依赖。 | `closed-auto-watch.json`：全部控件 inactive，timeScale=0，缓存白盒失效60→实际120Hz，7.658秒后缓存120、目标124。`external-takeover.json`：外部目标173/VSync1在重复 InitializeUI、6.477秒后台检查后保持；`runtime-restored.json`/`final-confirmed.json`：旧会话 Stopped/取消。**不是物理切屏实测。** |
| V1-002 / AC08 | `UIFrameRatePolicy` 复用现有 `PlayerPrefsModule`，单条版本记录保存模式、自动选择、自定义正整数，键为 `Ember.UI.FrameRate.v1.<Application.identifier>`。全部合法用户修改保存并刷写；GM与完整设置共用 setter。无记录、缺字段、未知版本、非法模式/布尔/目标整条回退 Responsive+自动+备用180，读取不覆盖坏记录。同步模式保留目标类型和值。 | `preferences-focused.json` 16项通过（10种缺失/损坏记录、同步保存恢复、GM共用路径、非法编辑不改记录、正整数上界等）。`settings-wired-save.json`：实际激活设置 Toggle/InputField 已接线 UnityEvent 将201保存且实际目标201。`gm-quick-switch-wired-save.json`：实际GM Button切Balanced，存`1|1|0|201`、VSync1，保留自定义。`actual-start-after-domain-reload.json`：明确域重载后的新启动恢复201。 |
| V1-003 / AC06/08 | `SubsystemRegistration` 的 `BeginRun` 明确清除本次初始化资格、引擎写入缓存及旧事件订阅；新启动 Initialize 重新读偏好并应用。一次运行内 Initialize 仍幂等，重复接线不覆盖外部接管。未修改项目 Enter Play 设置。 | `actual-reenter-play.json`：真实Stop→Play，原DisableDomainReload下custom200且target200；`settings-after-actual-reenter.json`：实际设置改201后再次Stop→Play，custom201/target201。`cold-domain-reload-*.json` + `actual-start-after-domain-reload.json`：真实程序集域重载后冷状态新启动同样恢复201。不是只反射重置字段当成重启通过。 |
| V1-004 / AC19 | `GMHudLayout` 按每个圆的guard额外半径同时扩大侧列步距、局部弧半径/相邻间距、边界修正和距离预算。`GMHudController` 在安全区/入口/避让几何变化时清零pending，要求同一可用几何连续稳定180ms。普通guard0视觉候选保持。 | `hud-focused.json`：3/5项四角共8个实际HUD均tier0/83%，8项guard0/4诊断均可解；6组实际76/69/62缩小→持续展开自动恢复83%通过。每组3次100ms空间释放不提前放大；最终释放后约0.315—0.348秒恢复，pending起点后均超过180ms。完整命中圆安全区/避让/相邻/入口分离、圆内外filter与实际EventSystem首射线命中通过；入口76DU/位置/收藏顺序保持。移除避让后未调用force Position、Build、Toggle或重开。 |

## 中间失败与修正

首次余量修正已能放大，但新抖动探针发现旧 pending 在障碍重新出现时未清零，导致累计旧计时、再次释放后约0.05—0.09秒就放大。保留 `hud-focused-first-jitter-failure.json` 与对应 `hud-focused-first.csbody`，不覆盖或淡化。随后补几何变化重置计时，主动再编译，再跑同一相关用例，六组全部通过。

补修后第一次运行的第一角落 `topRaycasts=false`，其余角落与六组放大/命中都通过，保留为 `hud-focused-after-debounce.json`；该次紧接启动、角落等待150ms。为排除启动/画布尚未稳定的采样，增加400ms初始等待与300ms角落稳定等待，最终八角落和六组实际命中均通过，结果为 `hud-focused.json`。这里没有据早期失败改产品输入逻辑。最终测试源码 `hud-focused.csbody` 留存，可直接按原条件重跑。

## 编译与现场

- Unity6000.5.4f1，实例 `ember-unity-framework@b23e9b768e85ed30`，FrameworkScene；base0.11.1，com.ember0.26.0；branch `codex/release-0.26.0`，HEAD `65ae680732323d83de9f898f89e9e8cc576ee4c8`。
- 本轮初批修改与HUD稳定计时补修均主动通过Unity MCP请求编译，确认本轮compile finished、domain reload after、非编译/更新，以及已加载新MVID。编译重连按30秒有界等待，未重复发送结果不明请求。
- 最终业务 MVID **4142319a-11da-4c49-9812-a81035d3a0c8**；UI `8da9108d-adb1-4585-be1d-8b6e2c8bceea`，UIExtension `95c1008b-3e54-4e42-afde-01398716563b`。见 `final-compile-request.json`、`final-compile-completed.json`、`final-mvid.json`。帧率用例最初加载业务MVID `83e07713-ffc2-4275-b739-70ec2ce47deb`；最终再编译只补HUD计时，帧率代码未再改，最终实际GM保存也复核通过。
- 两次代码编译均无C#错误。最终编译重连期间观察到一次MCP WebSocket未初始化的传输警告，单独保留 `compile-transport-warning.json`；后续Play及最终Console查询为空，见 `console-final.json`。不宣称整个过程零警告。本轮未重做Loading故障注入，V1已有reset警告分类继续保留。
- 没有执行全量测试或旧123项检查。只做四问题相关策略、偏好、真实启动/关闭、HUD几何与输入链检查。完整设置/GM控件使用实际激活对象的已接线UnityEvent；HUD使用实际正式Prefab及EventSystem.RaycastAll，角落定位以私有dock语义字段建立夹具。不是OS鼠标录制或物理触摸签收。

## 保护与善后

- `production-differences-from-v1.json` 对照V1的54个生产指纹，**恰好6文件变化**：UIFrameRatePolicy、UIFrameRateControls、EUIAppearanceBootstrap.Startup、GMHudLayout、GMHudController、base/soft-glass-ui.md。`production-i2.diff` 为相对I1/V1的完整本轮diff；原始文件在 `evidence/i2/before/`。无正式Prefab/Binding/材质/样式/模板资源改动，保留V1接受的GM0.82参数。
- 字体DefaultLightSans SHA-256仍 `14C9718D55D832593C689643CD752A277E0915FE418BB71F8F85C883653C0E4D`。QualitySettings、EditorSettings及其余V1生产指纹保持；未改域重载选项，仍enabled=true/DisableDomainReload。
- 偏好边界：大部分用例反射隔离 `_storageKey` 至 `Ember.UI.FrameRate.I2.Validation.<identifier>`。真实程序集域重载会重置该字段，因此冷启动验证短时把201记录写入正式键，先用 `environment-and-preferences-before.json` 保存原存在性/内容，最后严格恢复。本次原正式/隔离/HUD测试键都不存在，最终仍不存在。没有DeleteAll。收藏测试命名空间恢复local，Provider与避让全部注销、临时GameObject销毁；入口位置键保持原先不存在，入口大小/位置在运行中恢复。
- Stop请求返回的早期 `runtime-restored.json` 仍观测到Application.isPlaying=true；未以该早期状态冒充最终。随后editor state确认Stop完成，再恢复编辑器target124/VSync0，最终以 `final-confirmed.json` 和 `editor-final.json` 为准：非Play、非编译、非更新，旧会话已取消。GameView分辨率、本轮没有修改。
- 无模板保存/Bump/切换、提交/发布、消费升级、新聊天、下游消息或归档清理。

## 回交边界

实施自检认为V1-001—004已修复，AC06/08/19相关定向用例通过，最终关闭仍由主聊天派V2独立验收决定。V1原issues.json/acceptance.md不代改为通过。真实硬件切Hz、Player/VSync性能、移动/XR、物理触摸、多指、设备安全区、其它模板和消费项目继续未签收；I1/V1已通过的视觉和其它业务本轮未扩测。未覆盖“改Editor Enter Play选项开启域重载再Play”的另一种配置；冷状态恢复证据来自真实MCP程序集编译/域重载后新启动，原项目配置始终保持。

本页提及的证据文件均位于 [evidence/i2/](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/i2)。主聊天下一步收取本回交，安排原验收聊天执行V2至少复核四稳定问题、AC06/08/19及本次直接影响的启动/输入/取消生命周期。

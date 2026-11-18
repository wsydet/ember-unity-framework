接续 Ember 任务 20261117-soft-glass-ui-gm，回原独立验收聊天对 A1-RW1 定向复验。

项目根 C:/Users/wuyu/My/ember-unity-framework，任务目录 .utmp/ember-workflow/20261117-soft-glass-ui-gm/。沿用 checkout，不创建 worktree/子代理。本聊天负责独立复验，不修改产品来通过验收、不归档清理。

先读 AGENTS.md、CLAUDE.md、实际 UPM 解析 Ember 包的 unity-validation.md；再读 requirements.md R4、handoff.md H12、acceptance.md A1、rework-a1.md、implementation.md 和 evidence/rework-a1/manifest.json。与 evidence/acceptance/final-baseline.json、原 R2 manifest 核对；890 文件是原 886 加四份新增作用域/焦点测试及 meta，既有文件变化必须仅为交接所列八份。

重点复验：
1. SG-IA-02：从已聚焦下层 TMP_InputField 业务打开默认 Confirm，用真实 GameView 字符事件检查文字不变、isFocused=false；EventSystem 对下层选择发送 Submit/Move 不触发。核查默认焦点、关闭后恢复输入/按钮/Slider，Cancel/ESC 不穿透；默认 Confirm 没有新增自动 ESC 关闭语义。
2. 单/双 Popup 上层先关/下层先关、单层或下层视图外部销毁、隐藏/重显后的返回链和采样；关闭 Popup 不抢已转移到更高 GM 的焦点，普通 Popup 关闭不重新启用业务本来禁用的页面根。保持下层实际渲染和 FullScreen 首次采样屏障。
3. SG-IA-01：默认 Blur → UseMask=false → RefreshBackdrop 后，旧 lease disposed，而 Group/Graphic 不再借用它且局部玻璃重新 ready；恢复 mask、逻辑拒绝/恢复、Solid/Off 均正确。Group 借用无 Dispose 权，迟到释放不撤销新借用，回池/关闭/隐藏/销毁幂等清理。
4. 按 A1 矩阵相关短测：共享租约、反复开关无资源增长、FullScreen 首次采样/冻结和恢复、旧 Popup、GM 打开/收起；清消费者后捕获停止，0输出 RT/段/专用材质。不要全量或长时测试。

实施证据：compile-delivery.json、loaded-assemblies-final.json、tests-delivery.json、runtime.json、teardown.json、console-final.json、editor-final.json（均在 evidence/rework-a1）。当前自检18条 EditMode、40项运行矩阵；不能照抄为独立通过。tests-first/runtime-first 和 round2 是已解释的历史夹具结果；最新 delivery/runtime 为当前来源。

R2 用户确认继续有效，原五图无漂移，未改 Shader/URP/Prefab/Binding/材质/字体/布局。03-modal-keyboard-final.png 和 04-mask-off-local-glass-final.png 是最终代码现场的实际 2560×1440 GameView 证据；不是新美术基线。若独立检查发现实质视觉变化，按 R08 更新受影响预览和确认，不能代签。

保持 AC09 真触摸、设备旋转、安全区、键盘覆盖待设备验证；AC11 Assets base 待回流草稿、正式模板保存/分发未完成。不得擅自模板切换/保存/Bump/同步/部署，不提交/发布，不归档清理。复验后更新 acceptance.md 与 handoff.md，区分已修复项和整体尚未完成边界；若失败给出稳定问题 ID、实际复现及证据，再交回实施。

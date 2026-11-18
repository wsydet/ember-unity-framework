# A1 定向返工 A1-RW1

本聊天只负责实施修复和自检；独立验收 A1 的失败结论仍保留，由原独立聊天复验后更新。有效需求保持 R4/F01—F04，R2 用户视觉确认继续有效。没有创建 worktree/子代理、提交发布、模板切换/保存/Bump/同步/部署或归档清理。

## 实际修复

- SG-IA-02 / P1：新增 UI core 内部 EUIModalInputScope，由 Manager 创建的有效模态 mask 建立焦点所有权。提前于 EventSystem 更新清除下层 selection，使 TMP_InputField 实际 Deselect；用页面交互总闸阻止 Selectable 导航和 Submit，页面返回键遍历跳过被阻挡层。下层 alpha、Canvas 与物理距离保持，继续参与背景采样。
- 单/双 Popup 的返回 selection 按作用域保存；下层先关或销毁时修复返回链。暂停/显示完成/恢复交互都经过总闸，隐藏视图退出作用域，重显重新采样并取得焦点。外部销毁跳过退出动画但完成路由、逻辑、mask 清理。已转移至更高层 GM 的有效 selection 不被关闭 Popup 抢回；普通 Popup 未隐藏下层时不重置其业务交互/可见标志。
- UseMask=false 和逻辑拒绝 mask 不建立焦点作用域；切换 Solid/Off 仍按实际 mask 保留模态阻挡。ESC 未被 Popup 处理时不新增自动关闭语义，也不越过阻挡层向下冒泡。当前默认 Confirm 的 Cancel 事件本身无新增业务回调。
- SG-IA-01 / P2：MaskSession 记录借用 Group，释放前按 lease 引用撤销本次借用，再由所有者 Dispose；Group 按自身设置重新取得局部 lease。旧会话释放不撤销新借用，Group 不 Dispose 外部 lease。外部租约异常先失效时，LateUpdate 也清除失效引用再恢复自采样。Acquire 失败时不留下 external-null 锁。

产品改动是六份原有 core C#、一份原有租约回归、一份说明，新增内部作用域及焦点回归（含各自 Unity 生成 meta）。没有修改 URP、Shader、材质、Prefab、Binding、字体、布局或配置。修改前保存当前真实文件，不用 HEAD 恢复工作区。原 886 文件按备份及未改文件重算与 R2 基线零差异，见 evidence/rework-a1/baseline-audit.json。

## 定向验证与证据

最终本轮证据以 evidence/rework-a1/compile-delivery.json、loaded-assemblies-final.json、tests-delivery.json、runtime.json、teardown.json、console-final.json、editor-final.json、manifest.json 为准。其他 first/round2/final/latest 命名文件保留实施过程，不替代 delivery 记录。

最终18/18 EditMode通过，job3faf0fa5330949a5bc8b336cbb92b418，1.2262788秒；40/40运行检查通过。显式编译开始1794908041615、完成1794908046809、域重载完成1794908055450，UI MVID aad4d568-81c2-43f1-87df-31511fd34c11。关闭消费者后60帧捕获4551→4551，0边界/RT/段/专用材质。最终Console无warning/error，Play=false、场景未脏，GameView index5、圆点偏好键不存在与初始一致。

首轮 EditMode 焦点夹具没有进入 EventSystem.OnEnable（非 ExecuteAlways），四项因此失败；已在夹具显式进入/退出该生命周期，未通过降低断言修正。首轮运行 GM 检查没有开启 Enable，滑杆按原业务禁用，因此失败；改为先启用再检查键盘。tests-first.json、runtime-first.json 保留原始结果。产品编译没有错误。

回归针对真实故障：租约测试直接验证 Release 后 Group 不再持有 disposed 外部引用、重新 Acquire、借用不 Dispose 和迟到释放；焦点测试直接发送 ExecuteEvents Submit/Move，检查下层动作未触发以及有效恢复，不依赖鼠标 raycast 首命中。Play 探针从已聚焦输入框业务打开默认 Confirm，向实际 GameView 发送 KeyboardEvent("z")，核对文字和焦点。

| 范围 | 复验内容 |
| --- | --- |
| 输入与焦点 | 字符、方向、Submit、Cancel、ESC；输入框聚焦/未聚焦、按钮、Slider、默认上层 selection、旧 selection 回收 |
| 生命周期 | 单/双 Popup，上层先关/下层先关，隐藏/重显、外部销毁单层/下层、有效返回链 |
| 策略与局部玻璃 | Blur、Solid、Off、UseMask 关闭/恢复、逻辑拒绝/恢复；本页表面共享有效租约，局部按自身设置恢复 |
| 资源与兼容 | 六轮 mask 开关/回池、关闭所有消费者后 60 帧计数及 RT/相机段/材质；FullScreen 首次 ready 后冻结、原距离恢复；旧 Confirm；GM 启用滑杆、收起清焦点、关闭 Popup 不抢高层 GM 焦点 |

最终实际预览 03-modal-keyboard-final.png、04-mask-off-local-glass-final.png 均为 2560×1440 MCP GameView；后者配 mask-off-preview-state.json，显示 UseMask=false、局部 lease ready=true/disposed=false，并记录最终UI MVID。01/02为前一轮现场。仅功能恢复和焦点态，不改变已确认外观；原 R2 五图指纹保持。截图中的 FPS 为工具采样现场值，不作性能结论。没有新性能长测、全量用例、构建或真机验证。

## 交回独立复验

AC03/AC04 的两个缺陷已实施修复，当前结果只能记实施自检，不能代签 A1 通过。AC09 真实触摸、旋转、安全区与键盘覆盖体验仍待设备验证；AC11 Assets 中仍是 base 待回流草稿，正式模板保存/分发未完成。其他平台、Player、热稳态/功耗的原边界保持。

使用 prompts/05-reacceptance-a1.md 回到原独立验收聊天 01a1e8b5-afdf-7ed3-b93a-e3b89435a271 定向复验；不在本聊天运行独立验收，不覆盖 acceptance.md 的原证据与结论。

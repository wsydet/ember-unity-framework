# GM 控制中心、帧率策略与玻璃接线

2026-11-19；本地实施完成，独立验收通过。用户批准D10后实施，经过I1 → V1 → I2 → V2，当前AC01—AC24全部通过，四个返工问题关闭。完整证据已核验，本任务临时目录已清理，完整材料和备份保留在独立归档。

GM采用紧凑真实模糊玻璃：顶部顶层FSM、时间控制、实际帧率策略与模式快切，下区只展示消费端注册操作。右键/长按收藏，最多五项较小弧形HUD、边角缩小及稳定恢复；保留入口吸边、位置与真实FPS，起拖收起HUD。

默认桌面策略为高响应、自动跟随屏幕；自定义目标精确不加余量。设置与GM共享偏好记录，后台监测独立于界面可见性，支持现有禁域重载Play生命周期。FPS启动窗口分子已修正。Loading采样接线、设置和公共弹窗通透参数、玻璃初始化与GM解耦均已完成。

- [当前正式交接](handoff.md)
- [最终独立验收与24项结论](acceptance-v2.md)
- [V1原失败/复现](acceptance.md)
- [I1实施](implementation.md) / [I2修复](implementation-i2.md)
- [用户授权](approval.md) / [需求补充](current-approval-r14.md) / [稳定需求](requirements.md)
- [D10方案](design.md) / [玻璃审查](glass-ui-audit.md)
- [证据包、截图和原型](evidence-index.md)
- [长期接口与配置说明](../../../Assets/Game/Documentation/base/soft-glass-ui.md)

验收范围为当前base Assets与必要框架的Windows Editor/URP。实际EventSystem合成鼠标语义输入、真实渲染、编译/域重载和短时相关行为均有证据；硬件切Hz、Player性能、移动/XR及触屏设备未签收。没有保存模板/Bump、提交或发布，模板分发与消费升级属于后续独立操作。

四个已关闭问题：界面全关自动跟随停更、完整设置未跨启动保存、禁域重载再次Play选择与实际不一致、HUD缩小后不能自动恢复。原反证保留，可从V1和V2报告核对。

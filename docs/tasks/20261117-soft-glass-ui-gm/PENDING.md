# 未完成事项与后续承接

状态：未完成。当前验收为A2，实施基线A1-RW1。用户要求归档不等于放弃以下事项。

| ID | 状态及责任阶段 | 完成条件 |
| --- | --- | --- |
| SG-IA-02剩余P2子项 / AC04 | 实施返工后交独立复验 | 双Popup隐藏/重显后实际最高可见Popup可交互；交叉隐藏后全部关闭，原有效selection恢复；保留业务禁用根、GM焦点、NoMask门禁与背景渲染 |
| SG-PENDING-02 / AC09 | 设备验证 | 真实触摸、旋转、安全区和键盘覆盖有对应设备证据，或用户明确接受延期并记录承接 |
| SG-PENDING-01 / AC11 | 正式模板交付 | 保护topdown草稿，准备精确差异/恢复计划，在具体授权下经项目中心完成base保存及所需分发；不手改快照/hash |

SG-IA-01租约问题已通过A2独立复验；后续改动影响该链路时才针对影响复验，不抹去已有通过范围。

## 稳定复现

先在Showcase选择BackgroundMode，依次打开Confirm和Alert。隐藏上层并等待30帧，下层仍Paused、alpha1、根不可交互、三个按钮不可用、selection=null。不隐藏的关闭对照正常。

重新从明确选中的BackgroundMode开始，开两层，依次隐藏上层、重显上层、隐藏下层、重显下层；再关闭上层和下层，主页面可交互但原selection丢失。见[隔离复现A](records/evidence/reacceptance-a1/hide-chain-repro.json)、[隔离复现B](records/evidence/reacceptance-a1/cross-hide-return-repro.json)、[现场图](records/evidence/reacceptance-a1/03-top-hidden-dead-focus.png)。

## 恢复实施与复验

按[后续实施提示词](records/prompts/06-implementation-rework-a2.md)和[完整验收矩阵](records/acceptance.md)继续。原范围修复已有授权，无需重复申请；原临时工作记录目前保留。若改从本归档恢复，将完整包解到新任务工作位置后先核对交付manifest、正式模板身份及之后的工作区变化，保留新修改，不覆盖当前源码。

完成修复后更新实施和指纹，经Unity MCP主动编译并确认域重载/Console，运行相关短验证，再由独立聊天复验。原40项矩阵不能代替双层隐藏组合回归。没有授权全量/长测、模板Bump、提交或发布；不在本阶段清理材料。

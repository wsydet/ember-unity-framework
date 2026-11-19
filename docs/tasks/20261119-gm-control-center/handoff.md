> 2026-11-20 提交接续：用户已授权本地提交。FPS修复与GM功能已分组提交，任务记录随docs组提交；实际结果和保留字体见[提交审查](commit-review.md)。以下H35描述的是2026-11-19归档时点，不能将其中“未提交”理解为当前Git状态。版本号/模板保存/Bump/发布边界保持。

# H35：主聊天收束记录

任务 20261119-gm-control-center；2026-11-19，Asia/Shanghai。当前本地实施与独立验收完成，完整证据包已核验；本任务临时目录已清理。

- 模式：完整多聊天，用户明确选择，主聊天统一调度与返工。
- 主聊天：01a1f2a7-d1b7-7073-b4d0-4331239ed002 / local。
- 方案：01a1f2ac-d6d1-7753-8a38-9624bc4f19d5 / local，D10。
- 实施：01a1f366-9c93-7cc0-b2dd-3f29baa7c3b1 / local，I1 / I2。
- 独立验收：01a1f39c-e305-7490-93a6-2f8bafb4524c / local，V1 / V2。
- 授权：[A1原话](approval.md)；需求R14/AC01—AC24，D10布局在主聊天获准后才实施。
- 当前结果：[V2](acceptance-v2.md)独立关闭V1-001—004，本地AC01—AC24全部通过，无开放问题。[V1](acceptance.md)原失败与反证保留。
- 基线：Unity6000.5.4f1、com.ember0.26.0、活动base0.11.1；branch codex/release-0.26.0，HEAD 65ae680732323d83de9f898f89e9e8cc576ee4c8。代码/资源仍为未提交工作区。
- 实施：[I1](implementation.md) / [I2](implementation-i2.md)；[长期接口与配置说明](../../../Assets/Game/Documentation/base/soft-glass-ui.md)。
- 最终业务MVID 4142319a-11da-4c49-9812-a81035d3a0c8；Unity MCP编译/域重载确认，最终非Play/非编译/非更新，Console空。
- 主聊天重新比对V2全部生产/保护指纹，差异0；原字体SHA256保持14C9718D55D832593C689643CD752A277E0915FE418BB71F8F85C883653C0E4D。真实偏好、ProjectSettings与现场恢复。
- 正式记录docs/tasks/20261119-gm-control-center/；完整材料与ZIP哈希见[evidence-index.md](evidence-index.md)、[archive.json](archive.json)。
- 清理只限.utmp/ember-workflow/20261119-gm-control-center/；完整归档保留。归档内H34/dispatch为阶段快照，本文件是主聊天当前状态。
- 范围为当前base Assets+必要框架、Windows Editor/URP。帧率策略当前桌面生效；Player、物理切Hz、移动/XR、触屏设备/设备安全区、其他保存模板、消费项目未签收。
- 未执行模板保存/Bump/切换、提交、发布、消费升级，不把本地完成当已分发。GM0.82混色偏差经V1真实视觉验收接受；历史Loading故障注入reset警告和字体导入/编译传输警告完整保留，最终Console空不改写历史。

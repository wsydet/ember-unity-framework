# 收束准备索引

2026-11-17更新：用户已明确授权创建独立验收聊天，已创建01a1e8b5-afdf-7ed3-b93a-e3b89435a271（host local），以prompts/04-acceptance.md全文接续，同一checkout。下文“尚未授权新建”作为此前历史；当前等待独立验收结果，未归档删除。

任务20261117-soft-glass-ui-gm，2026-11-17，当前为“R2用户确认已取得，待独立验收”。用户原回复“可以进行收束了”，已记入preview-confirmation.md。R2 manifest SHA256：242518ABCD1CC0124A05BA3F3BE6C4C5CFBA8476ACC65B840DEC5EA727761F9A；886份产品文件无漂移。

## 已具备的可审阅资料

- requirements.md：有效R4/F01—F04、R01—R08、AC01—AC13及用户原请求。
- design.md：D1架构；revision-r2.md承接用户视觉返工及实际偏差。
- implementation.md、revision-r2.md：维护源、24资源、默认模糊、GM/FPS/无滑块、根Canvas限制与平台边界。
- evidence/r2：编译/程序集、13/13 EditMode、23项运行回归、7项专项、横竖样本、性能原值、保护文件检查及最终Console。
- evidence/previews/r2：5张实际实现图、886份相关文件及图像指纹。
- preview-confirmation.md：本聊天用户对R2的确认，AC13视觉签收已满足。
- prompts/04-acceptance.md：已更新为R2确认后的完整独立验收任务，可直接交给另一聊天，不要求用户重新整理。

## 当前缺口与操作边界

acceptance.md尚不存在，需要区别于实施聊天的独立验收。当前自检不能改名为独立验收。用户尚未明确要求新建或派发聊天，因此没有擅自使用聊天创建/发送工具；下一步只需授权创建独立验收聊天，即可使用现有04提示词在同一checkout进行相关定向复核。

独立验收发现问题交回实施定向修复；必需项满足后，正式归档目标为docs/tasks/20261117-soft-glass-ui-gm，保留需求、最终决策、实施与验收、确认及必要证据。先核对归档独立可用，再标完成，最后仅清理精确任务临时目录。当前未执行归档完成/删除，未伪造acceptance.md。

base保存/分发、模板切换/Bump、Git提交/发布、消费部署以及全量/长时测试仍分别按具体授权处理；收束请求不自动扩大为这些操作。

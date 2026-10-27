# 本轮技能调用与续接证据

本会话实际读取并采用 unity-mcp-orchestrator、skill-creator 和 ember-eui-build；扩展原 ember-eui-build，未创建重复的PSD/业务专用技能。调用者是本轮执行代理，不声称另一个干净会话或外部消费项目已安装运行。

## 五种入口的实际覆盖

| 入口 | 本轮操作与产物 |
| --- | --- |
| 已保存Prefab | EUIContractService重新导出4份before契约，识别Settings原Framework模式/全屏弹窗/实际节点，保护用户代码与共享源 |
| 源PSD | 原生创建设置source；复制working后Rename，source-layers/rename-report真实存在，source不覆盖 |
| 工作PSD | bridge inspect核对id/path/saved/sha；严格字段export获得真实BUI、2PNG和3文字参考；首次rejected保留 |
| 有效BUI | S4输入重新hash并比较当前契约后继续本轮验收，不重放历史Apply；S6通过当前map进入Analyze/Preview/Apply/VerifySaved |
| 已完成回执 | settings同请求Analyze noChanges=true，Apply返回completed；增量之后旧回执仅作历史，当前layout回执VerifySaved completed |

本轮更新后的 `references/artwork-workflow.md` 明确了上述起点、真实API、产物、完成判定、失败/未知写入边界。实际小改从工作PSD继续，新的increment-request仅更新关闭按钮图片，证明不要求重建有效上游或全部节点。

对应材料：settings-plan/preview/receipt/verified/repeat，increment-plan/preview/receipt/repeat，settings-layout-plan/receipt/verified，以及bridge/的原生终态。所有文件在本目录；输入在art/ui/S6。

## 错误处理确实发生

- 首个export字段不合法被拒绝；查终态后修正请求，新请求成功，无未知写入重放。
- Sandbox COM/硬链接失败时核对未发布请求，使用同一已授权本地工具；未伪造PS导出。
- 后续增量使用新资源路径，避免现有imagePath字节变化导致的门禁问题。
- 首版保存后在实际Play发现分辨率导致字小，以新当前契约发起明确layout请求，未修改旧摘要。

未在本轮注入saved/partial或unknown崩溃，因此 ResumeGeneration/RecoverNestedNames 的恢复说明来自当前正式API和已读S5证据，不能称本轮又完成故障注入。

## 验证与分发

实际执行 `python -X utf8 .../skill-creator/scripts/quick_validate.py .agents/skills/ember-eui-build`，结果 `Skill is valid!`。框架源不含开发机绝对路径或产品目录，引用当前解析包而非历史版本号推断能力。

维护源为 `.agents/skills/ember-eui-build`，既有catalog继续分发此技能，无新ID、无模板专用技能或发现副本需同步。普通EUI能力的最低版本保持；新的美术工作流使用前核对实际EmberBuiService和协议，旧包缺失时明确停止该路径。

本轮不提交或发布。AISkills~生成包要求已提交sourceCommit，不能用未提交源伪造；后续获准提交后运行正式bundle脚本并Check。当前源码调用通过，不等于已发布给所有消费者。

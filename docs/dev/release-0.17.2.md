# Ember Framework 0.17.2

2026-09-30。框架兼容修复补丁，随包交付 visual-novel 0.18.1。框架版本与模板内容版本独立：框架只修复技能安装校验，小说模板新增流程结构，已通过正式保存和 minor Bump，再以文档 patch 封存；本轮不重复 Bump。

## 发布范围

- 技能安装严格检查 UTF-8 无 BOM、文件头分隔符及唯一必需字段；剧情导入技能源去除 BOM，保留正文、原字节备份和事务语义。新增只读技能文件头审计脚本。
- 小说模板加入 Jump / Receiver 成对跳转和 FlowStart / FlowCall / FlowReturn 复合流程：稳定节点引用、命名结果、独立局部变量、禁止递归及最多 16 层调用栈。
- 存档格式 9 保存调用栈与剩余等待；支持流程内部对白、选择、计时等待恢复。保留旧格式 8 的随机/BGM 兼容边界；自定义步骤与外部小游戏仍遵循既有保存限制。
- 图编辑器支持关联定位、分区折叠、独立视图、复制重映射、撤销和连续试播。提供 18 节点日程循环示例，不改变默认剧情。
- base 0.7.0、source3d-2p5d 0.4.0 内容不变；三个模板兼容声明保留 0.17.0。第三方依赖、11 项公共技能与 bundle sourceCommit 不变。

## 模板封存

visual-novel 0.18.1 / preview，实际内容、contentHash、versionedContentHash 均为 `4f853face913953d7939443048d355a1`。父级 base 0.7.0，父快照实算 hash 为 `274ae24260525ab08b7264fc66625b57`。发布时通过正式 Unity API 验证全部模板 hash、父快照、谱系及编辑记录，0 问题，编辑副本未过期。

## 验证

- 本轮 Unity MCP 确认正式框架项目 Unity 6000.5.4f1，刷新及编译请求完成，控制台无编译错误。
- 本轮重跑 EmberTemplateSkillsEditTests 与 EmberAISkillInstallerEditTests：66/66 通过、0 失败、0 跳过，job `e01bfda715ac4cb4807706f88e6b16bc`。
- 复用同批实现的小说 299/299 及最后图编辑器调整后 15/15 回归；已核对本地 NUnit XML。两批有重叠，不相加；实现证据和测试映射见 [流程交付记录](visual-novel-flow-nodes.md)。封存后的变更为文档，本轮未重复运行全部小说测试。
- 42 份技能文件头审计 0 错误，Python 文件头回归 2/2 通过；公共 bundle 11 项技能、32 个文件一致。bundle 检查在 PowerShell 7 运行；Windows PowerShell 5 的默认编码读取失败不作为内容损坏或通过证据。
- 候选提交及 HEAD 的共享二进制、字体配置、Git 空白检查通过。
- 未完成消费端安装/模板迁移、桌面技能菜单验收、完整 PlayMode、发行构建及设备测试。框架开发项目结果不替代消费项目验收。

## 消费升级

已有项目必须通过 `Ember/UPM Manager` 升级到 v0.17.2。升级包不会自动更新 Assets。

只需修复技能发现时，在项目中心预览并更新模板专属 AI Skill，按冲突提示保留备份，不需要完整部署业务。

采用流程能力时，visual-novel 0.17.x → 0.18.1 跨 minor，先保护本地定制及旧模板基线，再按模板冲突恢复流程迁移；不能以“补齐缺失”替代升级。结构调整导致存档指纹失配时应安排重开或显式版本迁移。旧小说项目还需遵循 0.17.0 的同 GUID 字体迁移要求。

使用说明见 [NovelFlow.md](../../Assets/Game/Documentation/NovelFlow.md)，编码修复证据见 [编码修复记录](template-skill-encoding-fix.md)。

发布声明：[release-0.17.2.json](../../Packages/com.ember/Dependencies~/release-0.17.2.json)。完整依赖：[manifest-0.17.2.json](../../Packages/com.ember/Dependencies~/manifest-0.17.2.json)。

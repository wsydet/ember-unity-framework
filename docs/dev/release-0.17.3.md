# Ember Framework 0.17.3

2026-09-30。模板编辑器补丁，随包交付 visual-novel 0.18.2 / preview。

## 内容与迁移

选中接收点会以橙色边框和标签标记同章、同作用域的关联跳转点；选中跳转点标记对应接收点。多选合并标记，取消选择、重建和程序定位后重新计算。概览中关联节点为橙色，实际选中节点蓝色优先；执行位置绿色边框优先，关联标签保留。

标记依据稳定 ID，不改变选择集、移动/复制/删除范围、剧情资产、运行时或存档格式。不会自动展开折叠区域或切换流程视图；继续通过原定位菜单导航。

消费项目通过 `Ember/UPM Manager` 升级框架，再预览模板更新。0.18.1 → 0.18.2 可用同 minor 三方补丁并逐项解决本地冲突；从 0.17.x 升到 0.18.x 仍须保护定制内容并按跨 minor 流程迁移。框架升级不会自动更新已部署 Assets。本轮没有升级消费项目。

## 模板与依赖

正式 SaveTemplate 保存 1153 文件并执行 patch Bump。visual-novel 实际内容、contentHash、versionedContentHash 均为 `b7acc31df3161bcb52a807f3fd929fdd`。父级 base 0.7.0，父 hash `274ae24260525ab08b7264fc66625b57`；全部模板谱系 0 问题，编辑副本未过期。

base 0.7.0、source3d-2p5d 0.4.0、框架兼容声明 0.17.0、第三方依赖、11 项公共技能 bundle 及 sourceCommit 均保持不变。

## 验证和限制

- 本轮 NovelFlowEditorTests、NarrativeGraphTests、NarrativeGraphInteractionTests **16/16 通过、0 失败、0 跳过**。job `2b3d2233134b4ab38a2f6553002ff604`，Unity Test Framework NUnit 报告 `.utmp/visual-novel-m3/tests-20260930-061050879.xml`，执行 96.42 秒。
- 首次 15/16 的失败为新增同步测试未隔离 Undo 分组；修正后增加稳定关联 ID 断言并通过上述完整重跑，未删除或跳过失败用例。
- Unity 6000.5.4f1 正式框架项目刷新后 Console 无错误；MCP 测试状态查询超时，停止进一步编译验证。测试结果来自本轮落盘报告，不把查询超时算作测试通过，也不沿用上一版测试数字。
- 模板实际内容、封存、父快照和编辑记录已通过正式 API 校验；42 份技能文件头、11 项技能/32 文件 bundle、候选提交及 HEAD 共享二进制与字体静态检查通过。
- **本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。**
- 实际颜色与屏幕外定位的视觉验收、消费端迁移、完整 PlayMode、Player 构建及设备测试尚未完成。

[使用说明](../../Assets/Game/Documentation/NovelFlow.md#选择关联标记) · [实施与封存记录](visual-novel-flow-nodes.md) · [发布声明](../../Packages/com.ember/Dependencies~/release-0.17.3.json) · [完整依赖](../../Packages/com.ember/Dependencies~/manifest-0.17.3.json)

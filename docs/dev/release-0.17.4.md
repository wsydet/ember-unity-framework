# Ember Framework 0.17.4

2026-10-01。交付 visual-novel **0.18.3 / preview** 只读导航补丁。

## 修复与迁移

原 LocateFlowNode 在流程内节点定位时调用 SetFlowCollapsed，实际只读复现抛出 InvalidOperationException（LocateFlowNode → SetFlowCollapsed → RequireMember → RequireEdit）。修复移除导航写入，临时切换流程视图、显示目标、选择并聚焦；公共 SelectModel 隐藏节点入口保留现有节点与运行标记。导航不创建或标脏布局、不增加撤销记录，返回主线按原折叠状态显示。持久化折叠菜单根据当前章节及流程可编辑状态禁用，执行时再次检查并走编辑事务。

消费项目通过 **Ember/UPM Manager** 升级框架到 0.17.4，再在项目中心预览 **visual-novel 0.18.2 → 0.18.3** 三方补丁增量。冲突逐项合并，保留剧情资源和专用图布局；不修改 PackageCache、manifest/lock 或部署基线来绕过正式流程。包升级不会自动更新已部署 Assets。本轮未修改或升级消费项目。

## 模板封存

通过正式 SaveTemplate 保存 1153 文件，显式 patch Bump 到 0.18.3。实际内容 hash、contentHash 和 versionedContentHash 一致为 `e0279904363d9734d495ed7f75fbed5b`，父级 base 0.7.0 / `274ae24260525ab08b7264fc66625b57`。全部模板实际内容及父快照验证通过，谱系 0 问题，当前编辑记录对应 0.18.3 及相同 hash。

base 0.7.0、source3d-2p5d 0.4.0、兼容声明 0.17.0、第三方依赖及公共技能 bundle 保持不变。未手改模板快照或 hash，未回填产品剧情及专用布局。

## 验证与边界

- 用户于 2026-10-01 确认手动编译无错误；测试程序集更新时间为本日 15:16，包含此前断言修正。
- 本轮 NovelFlowEditorTests、NarrativeGraphInteractionTests、NarrativeGraphTests、NovelFlowNavigationPlayModeTests **19/19 通过，0 失败、0 跳过**。job `35febb2caab6493e84649b4a13449584`，报告 `.utmp/visual-novel-m3/tests-20261001-074611990.xml`。
- 新增回归覆盖真实 Play Mode 菜单定位、只读资产、持久折叠流程内部节点定位、导航不修改资产 JSON/dirty/磁盘字节、不创建布局、不推进 Undo 分组，下一次撤销仍恢复用户折叠。
- 前一日首次 16/19 的新增测试断言问题已修正；其后一次运行未导入新程序集，旧报告不计作本次通过依据。本日重跑通过全部 19 项。
- 正式模板保存、Bump 与实际内容/父快照/编辑记录校验通过。共享二进制、字体、11 项/32 文件技能 bundle 及候选提交空白检查通过。
- 尚未执行消费端迁移、屏幕外聚焦人工视觉验收、完整 PlayMode、Player 构建或设备测试。

[发布声明](../../Packages/com.ember/Dependencies~/release-0.17.4.json) · [完整依赖](../../Packages/com.ember/Dependencies~/manifest-0.17.4.json) · [流程导航说明](../../Assets/Game/Documentation/NovelFlow.md#选择关联标记)

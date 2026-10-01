# Ember Framework 0.17.5

2026-10-01。交付 visual-novel **0.19.0 / preview** 指定节点测试存档及模板技能 **ember-vn-generate-save**。

## 功能与使用

图编辑器顶部剧情工具及节点右键菜单提供生成入口。普通对白（含 Say 且无 CustomStep）和选择节点可直接生成；特殊节点沿实际控制流回退到前置普通节点，多前驱及多调用链须选择。支持默认变量调整、本机空闲手动槽和完整 VisualNovelSaves 目录导出。技能按节点名称、台词或小游戏名称查找目标，使用同一生成和验证 API。

生成器沿用存档格式 9、剧情指纹、正式存储与恢复校验；生成时不执行自定义步骤。定位档使用默认变量和空舞台，不补跑之前的赋值、背景、立绘或音乐指令。特殊节点从前置普通节点读档后正常推进进入，业务依赖状态可手动调整。

## 消费项目迁移

通过 **Ember/UPM Manager** 升级框架至 0.17.5，再保护本地剧情、自定义小游戏、UI 和布局，按模板冲突恢复流程迁移到 visual-novel 0.19.0。0.18.x → 0.19.0 跨 minor，不能当作普通三方补丁升级。包升级不会自动更新业务 Assets；只更新 Skill 也不会安装生成器 API。迁移完成后可调用 `$ember-vn-generate-save 我要测试某某节点`。

## 封存与验证

- 正式 SaveTemplate 保存 1170 文件并显式 minor Bump；实际内容、contentHash、versionedContentHash 均为 `4071a85ea78c6c4d77b6cb640c22c76f`，编辑记录一致。
- 父级 base 0.7.0 / `274ae24260525ab08b7264fc66625b57`；全部模板实际内容与父快照一致，谱系 0 问题。base 0.7.0、source3d-2p5d 0.4.0、兼容声明 0.17.0、第三方依赖和 11 项公共技能 bundle 不变。新技能随小说模板分发。
- Unity 6000.5.4f1 MCP 编译成功，控制台 0 错误。NovelTestSaveTests、前驱存档恢复到自定义小游戏的会话集成用例、NovelCheckpointTests 和 NovelFlowTests 共 **44/44 通过，0 失败、0 跳过**；job `3d5566385c4f4e50b42826c9e97728c7`。
- 覆盖默认及覆盖变量、特殊节点回退、选择节点、真实调用返回地址、JSON 请求默认值、存储恢复和防止覆盖。真实生成窗口可用；模板技能正式同步且执行门禁通过。
- 消费项目自定义小游戏、独立 Player 构建及设备目录导入尚未验收。测试证据来自框架开发项目，不替代消费端实机验证。

[发布声明](../../Packages/com.ember/Dependencies~/release-0.17.5.json) · [完整依赖](../../Packages/com.ember/Dependencies~/manifest-0.17.5.json)

# Ember 文档索引

从 [框架包说明](../Packages/com.ember/README.md) 了解安装与入口，从 [当前进度](dev/framework-progress.md) 了解已有能力和待办。当前发布版本为 **0.12.3**，恢复受 Git 换行转换损坏的两份共享 TTF，并增加二进制完整性回归。静态检查通过；Unity MCP 不可用，Unity 编译与 UnityFarm 中文标记验收待手动完成。模板内容未变化，现有 0.12.0 兼容声明按 major.minor 规则继续适用。

## 使用框架

| 文档 | 内容 |
|---|---|
| [项目规则](../CLAUDE.md) | 架构、编码、Unity 验证与模板维护约束 |
| [UI 开发参考](user/UI开发参考.md) | 页面、Item、Binding、状态路由和代码生成 |
| [Package 清单](user/package-inventory.md) | 实际依赖、来源与版本 |
| [API 速查](dev/ember-api-reference.md) | 常用类型、签名、路径和限制 |
| [启动时序](dev/ember-boot-sequence.md) | Manager/Module、场景和启动过渡 |
| [日志](dev/ember-debug.md) | 标签、SO 配置和文件日志 |
| [UnityFarm 改动分流](../Packages/com.ember/Documentation~/maintenance/unityfarm-change-routing.md) | 判定项目、框架、模板或两者升级，并规定消费端接收方式 |
| [UPM 交付维护](dev/upm-migration-plan.md) | 开发仓库与消费项目、安装升级和发布流程 |
| [0.11.5 发布说明](dev/release-0.11.5.md) | UnityFarm 改动分流、强制阅读入口、模板兼容声明与升级边界 |
| [0.12.0 发布说明](dev/release-0.12.0.md) | 强类型配置表能力、模板接入目标、验证边界与 UnityFarm 迁移 |
| [0.12.3 发布说明](dev/release-0.12.3.md) | 共享字体二进制恢复、完整性检查与中文标记验收 |
| [0.12.2 发布说明](dev/release-0.12.2.md) | Table 脚本编码修复、用户测试确认与消费回归步骤 |
| [0.12.1 发布说明](dev/release-0.12.1.md) | 配置表可视化、代码提示、单表导出与验证边界 |

## 模块与业务接入

| 文档 | 内容 |
|---|---|
| [Core](../Packages/com.ember/Documentation~/core/README.md) | 运行管线；[Manager/Module](../Packages/com.ember/Core/Runtime/Manager/README.md)、[Event](../Packages/com.ember/Core/Runtime/Event/README.md)、[Service](../Packages/com.ember/Core/Runtime/Service/README.md)、[State](../Packages/com.ember/Core/Runtime/State/README.md)、[Update](../Packages/com.ember/Core/Runtime/Update/README.md) |
| [Resource](../Packages/com.ember/Documentation~/resource/README.md) | Provider、资源/文件 Handle、真实同步/异步边界 |
| [Scene](../Packages/com.ember/Documentation~/scene/README.md) | 场景加载与状态机桥接 |
| [UI 概述](../Packages/com.ember/Documentation~/ui/README.md) | Page/Logic/Binding/Item；[EUI 明细](dev/eui-reference.md)、[过渡块](dev/ember-transition-block.md) |
| [Audio](../Packages/com.ember/Documentation~/audio/README.md) | 当前 BGM/SFX 接口 |
| [Camera](../Packages/com.ember/Documentation~/camera/README.md) / [Input](../Packages/com.ember/Documentation~/input/README.md) | 相机注册、输入读取与重绑定契约 |
| [SceneUI 设计](dev/scene-ui-module-design.md) | [包内结构](../Packages/com.ember/SceneUI/README.md)、[接入说明](../Packages/com.ember/Documentation~/scene-ui/README.md)、[业务示例](../Packages/com.ember/Templates~/source3d-2p5d/Assets/Game/Module/SceneUI/README.md) |
| [Table](../Packages/com.ember/Table/Documentation~/table/README.md) | 强类型 Row、CSV/TSV、ETBL V1、Binding/Catalog、Engine 与 ModuleBase |
| [PlayerControl](dev/player-control-module.md) | 2.5D 输入、拖动、缩放、边界与模板归属 |
| [Guide](dev/guide-module-design.md) | 可选新手引导模块、配置与扩展 |

## 模板、编辑器和验证

| 文档 | 内容 |
|---|---|
| [模板升级体系](dev/template-upgrade-system.md) | 项目中心、schema v2、父子同步、场景合并、事务、消费端边界 |
| [Core 编辑器](../Packages/com.ember/Core/Editor/README.md) | 当前文件职责与菜单；[兼容入口](../Packages/com.ember/Documentation~/core/README-Editor.md) |
| [框架测试清单](dev/framework-test-checklist.md) | 当前回归入口，不沿用旧版本通过结论 |
| [Editor 工具测试](dev/editor-tools-test-checklist.md) | 工具检查项与历史结果 |
| [UIExtension 测试](dev/uiextension-test-plan.md) | 控件回归、历史问题及未解决的 GM ScrollRect 布局 |
| [UIExtension 学习路线](dev/uiextension-learning-path.md) | 当前源码阅读顺序 |
| [Odin 规范](dev/odin-usage-notes.md) / [面板清单](dev/odin-panel-inventory.md) | 已有写法、历史观察与当前源码位置 |
| [Unity MCP 排查](dev/mcp-troubleshooting.md) | 连接诊断及编译验证边界 |

## 维护与后续设计

| 文档 | 内容 |
|---|---|
| [提交规范](dev/contributing.md) / [Git Hooks](../.githooks/README.md) | 分支、提交粒度和本地检查 |
| [Skill 速查](dev/skills-reference.md) / [编写指南](dev/skill-writing-guide.md) | 全部项目 Skill，包括文档维护复用入口 |
| [API 文档模板](dev/api-doc-template.md) | 编写模块说明的参考结构 |
| [本轮文档维护记录](dev/documentation-maintenance.md) | 逐文件处理结果、删除去向与验证范围 |
| [Audio 升级预案](dev/audio-upgrade-plan.md) | 未实施的分类、池化和播放代理设计 |
| [独立升级器预案](dev/independent-updater-package-plan.md) | 触发拆包条件和迁移策略，当前未实施 |
| [UI 设计取舍](dev/ember-vs-burner-ui-comparison.md) | 当前实现与历史研究的对应关系 |
| [Burner 历史研究](dev/burner-architecture.md) | 外部项目历史资料，不作为当前 Ember API |
| [CHANGELOG](../Packages/com.ember/CHANGELOG.md) | 发布历史与未发布变更记录 |
| [美术目录](../Assets/Art/README.md) | 美术源资产约定 |

第三方包、素材随附说明和许可证保持原始语义；缓存、构建产物和模板 Assets/ParentSnapshot 快照不作为普通文档直接清理。

# Ember Framework 0.11.3 发布说明

发布日期：2026-09-08

## 修复内容

- Unity Farm 等 Git/Registry 消费项目只允许部署包内模板。模板创建、保存、加载、父级同步、删除、版本和 metadata 修改等写 API 全部要求 embedded 框架项目。
- 非活动模板入口改为“部署此模板”。确认后只把目标完整模板部署到消费项目，替换五个模板管理目录；不会保存当前 Base，也不会写入包内 `Templates~`。
- 移除 0.11.2 的持久化切换前备份。目录替换仍使用临时文件事务，任一步失败会自动恢复原目录；事务成功后不保留当前 Base 的模板副本。
- 保留 0.11.2 对 Git 行尾 `contentHash` 误报及部署版本头伪差异的修复。

## 发布与依赖

- 框架 Git tag：`v0.11.3`
- 安装 URL：`https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.11.3`
- 完整依赖基线：`Packages/com.ember/Dependencies~/manifest-0.11.3.json`
- 第三方包内容未变化，继续固定到已发布的 `ember-thirdparty-upm` tag `ember-v0.11.1`。

## 验证边界

发布前已检查模板 metadata/hash/父快照、版本文件与静态差异。当前没有可用的 Unity MCP，因此本轮未完成 Unity 编译验证或 0.11.3 的 EditMode 执行。Unity Farm 需要在安装 0.11.3 后验证目标模板部署、项目校验和 Play Mode 行为。

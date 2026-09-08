# Ember Framework 0.11.5 发布说明

发布日期：2026-09-08

## 本次内容

- 随 `com.ember` 新增《UnityFarm 改动回流与升级规则》，统一判定一项产品开发改动应只留在 UnityFarm、升级 Ember 框架、升级 Ember 模板，还是同时更新框架与模板。
- 规则覆盖所有权判断、常见示例、框架与模板独立版本语义、从 UnityFarm 回流的标准流程，以及升级框架后的模板补齐、完整重新部署和人工迁移边界。
- 仓库根 `AGENTS.md` 与 `CLAUDE.md` 增加强制阅读入口；包 README 和仓库文档索引提供可发现入口。正式规则位于包内 `Documentation~/maintenance/unityfarm-change-routing.md`，会随 UPM 包交付。

## 框架与模板版本

- 框架：`com.ember 0.11.5`。
- 根模板：`base 0.5.6 / stable`。
- 派生模板：`source3d-2p5d 0.2.7 / preview`，父基线保持 `base 0.5.6`。
- 本次没有修改模板 Assets、内容版本、内容 hash 或父快照；父子模板只将 `frameworkVersion` 兼容声明推进到 `0.11.5`。

模板内容存放在 `com.ember` 中，所以未来即使只有模板内容变化，也必须发布新的框架 tag 才能交付给 UnityFarm；但模板 Assets 未变时不应为了框架发版而伪造模板内容 Bump。

## UnityFarm 升级说明

安装 URL：

```text
https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.11.5
```

0.11.4 已经完成模板 GUID 修复部署的 UnityFarm 项目，本次只需升级框架，不需要再次部署模板。
升级后可从包说明进入改动回流规则。框架升级不会自动更新已部署模板；未来模板真正发生变化时，按规则选择补齐缺失、备份后完整重新部署或人工迁移。

## 发布与依赖

- 框架 Git tag：`v0.11.5`
- 完整依赖基线：`Packages/com.ember/Dependencies~/manifest-0.11.5.json`
- 版本化发布声明：`Packages/com.ember/Dependencies~/release-0.11.5.json`
- 第三方包内容未变化，继续固定到 `ember-thirdparty-upm` 的 `ember-v0.11.1`。

## 验证边界

发布前复核文档入口、相对链接、JSON 语法、版本声明、模板 metadata/hash/父快照静态一致性和 Git 差异。
本次没有修改 C#、asmdef、场景、Prefab 或模板 Assets，因此不产生新的 Unity 编译或运行行为；UnityFarm 实际安装和规则可读性仍应在消费项目升级后确认。

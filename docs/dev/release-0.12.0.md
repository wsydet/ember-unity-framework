# Ember Framework 0.12.0 发布说明

发布日期：2026-09-10。发布 tag：`v0.12.0`。UnityFarm 的 UPM 更新、模板迁移和实际读表作为发布后消费验收单独记录。

## 内容

- 新增 `Ember.Table.Runtime`：不可变 Row 契约、严格 ETBL V1、只读表/索引、实例数据库、诊断、Catalog/Binding/Codec 和原子快照 `EmberTableEngine`。
- 新增 `Ember.Table.Integration`：统一 Resource → Engine 的 `EmberTableModuleBase<TModule>`。
- 新增 `Ember.Table.Editor`：严格 CSV/TSV、Schema/主键/引用校验、稳定 Hash、确定性烘焙、回读、Binding/Catalog 生成、所有权清单、批量回滚、陈旧检测和项目中心扩展校验。
- 新增 Runtime、Integration、Editor 三套独立 EditMode 测试和跨平台固定向量。

## 版本与模板

- 框架版本：`0.12.0`；不可变发布 tag：`v0.12.0`。
- 第三方依赖内容不变，继续复用 `ember-thirdparty-upm` 的 `ember-v0.11.1`。
- 根模板已通过项目中心加入默认关闭的 `GameTableModule`、User 扩展、空 Catalog、Rows/Definitions/源表/二进制目录占位和项目内最小示例，并从 `0.5.6` 封存为 `base 0.6.0 / stable`；实时、内容与封存 Hash 均为 `506baffc678f37d420308607c5f70e42`。
- `source3d-2p5d` 已通过正式父同步吸收该结构并保留场景、PlayerControl、SceneUI 差异，最终封存为 `0.3.1 / preview`；实时、内容与封存 Hash 均为 `cb76b93536d23691f81faf22f9fc0902`。父基线为 `base 0.6.0`，ParentSnapshot Hash 与父内容 Hash 均为 `506baffc678f37d420308607c5f70e42`。
- 两份模板均已通过项目中心声明兼容框架 `0.12.0`：根模板先显式“声明当前框架”，派生模板再经父同步继承该声明；内容 Hash 与模板版本均保持不变。

模板版本、Hash、ParentSnapshot 和 frameworkVersion 只能由项目中心保存、Bump、声明和同步；禁止手改快照或 metadata。接线工具只补缺失文件，现有 `GameTableModule.cs` 不一致时停止，`GameTableModule.User.cs` 永不覆盖。

## UnityFarm 迁移

发布后 UnityFarm 应安装 `https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.12.0`。已有项目不要完整覆盖业务目录，默认人工迁移新模板中的 `Assets/Game/Module/Table`、`Assets/Game/Table/Generated` 空 Catalog 与所需目录；再启用 `GameTableModule`，声明产品 Row/Definition、烘焙真实表，并由项目 Adapter 转换为领域配置。不得修改 PackageCache，也不要让领域核心直接依赖表文件、资源路径或 Engine。

## 发布验证与后续验收

- [x] 用户在 Unity `6000.5.4f1` 中确认零编译错误，并报告 Runtime、Integration、Editor 三套 Table EditMode 测试通过；本会话没有 Unity MCP，未独立复核该结果。
- [x] base 加载、项目 Table 接线、项目中心保存和 minor Bump 完成；实时 Hash、metadata 与封存 Hash 一致。
- [x] source3d-2p5d 父级预览/同步、冲突处理、版本封存和 ParentSnapshot 校验完成；派生差异保留。
- [x] base 通过项目中心声明兼容框架 `0.12.0`，source3d-2p5d 再次父同步并继承该声明；内容 Hash 保持不变。
- [ ] UnityFarm 通过 UPM Manager 更新到 `v0.12.0`，完成所需模板文件迁移、启用 Module、启动并读取实际测试表。
- [x] 用户已明确授权完成收口后提交、创建不可变 `v0.12.0` tag 并推送。

`v0.12.0` 发布后保持不可变；若 UnityFarm 消费验收发现框架缺陷，修复后发布新的 patch 版本，不移动现有 tag。

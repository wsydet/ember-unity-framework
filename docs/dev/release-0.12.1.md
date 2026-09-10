# Ember Framework 0.12.1 发布说明

发布日期：2026-09-10。发布 tag：`v0.12.1`。

## 内容

- `Ember/配置表中心` 增加表声明列表、搜索、Row Schema、字段约束和严格类型化数据预览。
- 根据实际 Table ID、Row 类型、样例主键和二级索引生成可复制的强类型查询代码；示例通过 Module Collector 读取已发现模块，不会访问 `Instance` 意外创建未启用模块。
- 增加“导出当前表”：只烘焙、Runtime Codec 回读并原子替换所选表的 `.bytes`，不重写其他表、Binding、Catalog 或 Manifest。
- 单表导出只接受由当前 Manifest 持有且生成代码未陈旧的表；首次接入或 Schema、路径、Definition 清单变化时零写入停止，并提示执行“导出全部表”。
- 全量提交跳过字节未变化的产物，减少无效文件替换和 Unity 资源重导入；局部与全量提交共用暂存、备份和失败回滚路径。

## 版本、依赖与模板

- 框架版本：`0.12.1`；不可变发布 tag：`v0.12.1`。
- 第三方依赖内容不变，继续复用 `ember-thirdparty-upm` 的 `ember-v0.11.1`。
- `base 0.6.0 / stable` 与 `source3d-2p5d 0.3.1 / preview` 的 Assets、版本、内容 Hash、封存 Hash、父基线和 ParentSnapshot 均未修改。
- 两份模板仍声明框架 `0.12.0`；模板兼容闸门比较 major.minor，因此可直接用于 `0.12.1`，不需要伪造内容 Bump 或手改模板 metadata。

## 使用与升级

消费项目可通过 UPM Manager 更新，或安装：

```text
https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.12.1
```

升级只替换框架 Package，不覆盖 UnityFarm 已部署的模板和业务表。首次生成、添加/删除表、修改 Row Schema、二级索引、输出路径或加载限制后执行一次“导出全部表”；只修改某张 CSV/TSV 的数据时，选中该表执行“导出当前表”。

## 发布验证边界

- [x] 修复用户报告的 `IReadOnlyList<EmberTableArtifactContent>` 到 `IList<EmberTableArtifactContent>` 编译参数错误。
- [x] 静态差异、JSON 语法、资源/.meta 配对和 Table GUID 唯一性检查通过。
- [x] 单表事务测试覆盖：只替换所选产物、其他二进制与 Manifest 保持不变、拒绝未持有路径，以及注入失败时恢复旧文件。
- [ ] 当前会话没有 Unity MCP；修复后的 Unity 编译与新增 `Ember.Table.Editor.Tests` 尚待用户在 Unity 中确认。
- [ ] UnityFarm 更新至 `v0.12.1` 后验证配置表窗口、当前表/全部表导出、Play Mode Module 启动和实际查询。

`v0.12.0` 与 `v0.12.1` tag 均保持不可变；后续修复发布新版本，不移动旧 tag。

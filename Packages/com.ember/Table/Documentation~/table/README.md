# Table — 强类型配置表

Table 把项目声明的不可变 C# Row 作为 Schema，把 UTF-8 CSV/TSV 作为 Editor 编辑源，烘焙成严格的 ETBL V1 二进制，并由生成的强类型 Binding 在 Runtime 无反射读取。首版不读取 `.xlsx`，不做远程表、热更新、Row 自动生成、压缩、加密或第三方解析依赖。

## 分层

```text
项目 Row / Definition / 生成 Binding 与 Catalog
        ↓
Ember.Table.Integration（Module 生命周期与 Resource 接线）
        ↓
Ember.Table.Runtime（纯 Engine、Codec、数据库与只读查询）
```

- `EmberTableEngine` 只消费 Catalog、Binding 和调用方提供的字节，不实现 `IEmberModule`，不访问 Resource、Scene 或业务程序集。
- `EmberTableModuleBase<TModule>` 检查 `EmberResourceManager`、同步取得小型表字节、持有 Engine 并统一处理失败回滚、销毁和热重启。
- 项目 `GameTableModule` 是模板部署后归项目所有的 Global 薄壳，只提供生成 Catalog、项目钩子与领域 Adapter；业务 Module 不另建静态表缓存。

## Row 与源文件

Row 必须是 `sealed class` 或 `readonly struct`，带 `[EmberTable("stable_id")]`，恰好一个只读字符串成员带 `[EmberTableKey]`，并有恰好一个 public 构造函数带 `[EmberTableConstructor]`。所有持久化成员是 public readonly 字段或 public getter-only 自动属性；`[EmberTableColumn]` 可覆盖列名，`[EmberTableReference]` 声明 Editor 跨表引用。

`.etable.csv` 使用逗号，`.etable.tsv` 使用 Tab。两者必须是 UTF-8，可带 BOM，支持 CRLF/LF、RFC 4180 引号、双引号转义、单元格内分隔符和换行。第一行是唯一列名；类型来自 Row。首版支持 string、bool、8/16/32/64 位有符号/无符号整数、float、double、decimal、enum 及其 nullable 值类型。bool 只接受忽略大小写的 `true/false`；数字按 InvariantCulture；非法值和溢出不会回退默认值。

## ETBL V1

```text
[Magic:4 = "ETBL"][FormatVersion:u16 = 1][Flags:u16 = 0]
[TableIdByteLength:u16][TableId:utf8]
[RowTypeIdByteLength:u16][RowTypeId:utf8]
[SchemaHash:32][SourceHash:32]
[RowCount:i32][PayloadLength:i32][PayloadHash:32][Payload]
```

全部整数小端，Hash 是 SHA-256 原始 32 字节。字段顺序固定为主键优先、其余最终列名 Ordinal 排序。string 是 `i32` UTF-8 长度（`-1` null、`0` 空串）；nullable 值类型先写 `0/1`；decimal 写 `decimal.GetBits` 四个 i32；enum 使用声明的底层整数宽度。Codec 在解码行前验证 Magic、版本、Flags、标识、Schema、行数/文件/字符串上限、Payload 长度和 Hash，并要求 Binding 恰好消费全部 Payload。

稳定 Row Type ID 冻结为 `程序集简单名:完整类型名`。Schema Hash 对 Table ID、Row Type ID、规范字段顺序、字段名、线格式、nullable、主键和引用元数据做长度前缀规范序列化；Source Hash 对按 Schema 顺序重排后的单元格矩阵做同类规范序列化，因此 BOM 与 CRLF/LF 不改变结果。

## 加载和查询

Engine 先构建独立 staging database。任何 Required 表失败都会丢弃整批并保留旧快照；Optional 表失败会记录诊断并从新快照省略，绝不混用旧 Optional。Required 全成功后只交换一次数据库引用。

```csharp
if (EmberModuleCollector.TryGetInstance(out var moduleCollector)
    && moduleCollector.TryGetModule<GameTableModule>(out var tableModule)
    && tableModule.IsReady
    && tableModule.Database.TryGetTable<MyRow>("my_table", out var table)
    && table.TryGet("stable_key", out var row))
{
    // 使用 row
}
```

该写法只读取已经由框架发现并初始化的模块，不会因访问 `Instance` 意外创建一个未启用模块。字符串键和 Table ID 使用 Ordinal、大小写敏感。枚举保持源行顺序。默认只有主键 Dictionary；高频非主键查询必须在 Definition 显式声明，由 Binding 构建 `EmberTableIndex<TKey,TRow>`。

## Editor 工作流

1. 创建不可变 Row 和 `EmberTableDefinition`，源文件放在项目业务目录。
2. 打开 `Ember/配置表中心`，先“校验全部”，修复带文件/行/列/字段的诊断。
3. “预览全部变更”检查将创建、替换和清理的路径。
4. 首次接入，或者 Row Schema、输出路径、Definition 清单发生变化时，使用“导出全部表”；它会先暂存全部二进制和 C#，逐表用 Runtime Codec 回读比对，再按所有权清单整批提交。
5. 生成 `<RowType>.TableBinding.g.cs` 与 `GameTables.Catalog.g.cs`。数据值变化不会改变 C#；Schema、路径或表清单改变时才更新生成代码。
6. 之后只修改某张表的 CSV/TSV 数据时，选中该表点击“导出当前表”，只回读验证并原子替换这一张表的 `.bytes`，不会重写其他表或触发生成代码变化。

## 可视化浏览与代码提示

打开 `Ember/配置表中心` 后，左侧会列出项目中的全部 `EmberTableDefinition`。选中一张表可以直接查看：

- Table ID、Row 类型、Required/Optional、源文件、Runtime 路径和 Hash；
- CSV/TSV 列到 C# 成员的映射，以及类型、主键、可空和跨表引用约束；
- 通过严格校验后的类型化数据，字符串、枚举、数值和 null 以接近 C# 字面量的格式显示；
- 根据实际 Row、Table ID、样例主键和二级索引生成的可复制查询代码。

数据预览复用正式 Schema 与校验结果，不提供绕过错误的宽松解析；表有错误时先根据诊断修复，避免窗口展示与 Runtime 实际读取不一致。

“校验当前”只报告所选 Definition（引用目标仍参与校验）。“导出当前表”要求现有 Binding、Catalog 与 Manifest 已由一次全量导出建立且仍与当前 Schema 一致；如果结构发生变化，窗口会停止单表写入并提示先“导出全部表”。工具可定位 Definition、Row、源文件和二进制，并通过正式 Runtime Binding 显示元信息及最多 200 行只读数据。

模板开发时先通过项目中心加载 `base`，再点击“创建/补齐项目 Table 接线”。该操作会生成当前（可以为空）Catalog，并且只在缺失时创建默认关闭的 `GameTableModule.cs`、永不覆盖的 `GameTableModule.User.cs`、数据目录占位和项目内最小接入说明。完成 Unity 验证后，必须回到项目中心保存模板并显式 Bump；不得直接修改 `Templates~` 或父快照。

默认二进制路径为 `Assets/GameResource/Resources/Config/Tables/<TableId>.bytes`，Binding 保存不带扩展名的 Resources 逻辑路径。生成 C# 位于 `Assets/Game/Table/Generated`，没有用户手写区。项目中心的“项目校验”会报告无效 Definition、引用错误和陈旧生成物。

## 限制

首版不支持数组、List、Dictionary、嵌套对象、多态、UnityEngine.Object、公式、`.xlsx`、运行时 CSV、压缩或加密。Runtime 不验证跨表引用；该错误必须在 Editor 烘焙前消除。Resources 默认后端仍是同步 `Resources.Load<TextAsset>`，大型或远程表应由未来独立 Provider/格式版本扩展。

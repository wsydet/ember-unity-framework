# Ember 配置表系统开发方案

> 状态：F0→F5 已由 0.12.0 交付；0.12.1 补充配置表可视化、代码提示和单表导出，消费项目验收待记录
> 更新日期：2026-09-10
> 当前框架版本：`com.ember 0.12.1`
> 当前模板候选：`base 0.6.0 / stable`、`source3d-2p5d 0.3.1 / preview`
> 来源：UnityFarm MVP1 P2 对稳定 ID、作物配置和确定性测试的实际需求
> 对照参考：Burner 的 CSV → 生成 C# → 每表二进制 → YooAsset Runtime 读取链路（2026-09-09 只读审计）

## 1. 目标与改动分流

目标是为 Ember 增加一套可供多个项目复用的强类型配置表能力，并通过基础模板交付最小业务接入骨架，最终由 UnityFarm 使用它承载小麦、胡萝卜等产品数据。

本任务分流固定为“框架 + 模板升级”：

- Ember Framework 拥有纯 `EmberTableEngine`、表容器、加载结果、诊断和 Binding/Codec 契约，并在独立 Integration 层提供通用 `EmberTableModuleBase<TModule>`；Editor 层拥有 CSV/TSV 导入、校验、烘焙和生成能力；
- `base` 模板拥有部署后归项目所有的薄 `GameTableModule` 壳、空 Catalog 接入点与用户扩展入口，不复制底层加载事务；
- `source3d-2p5d` 通过正式父子同步获得基础能力，不维护另一份分叉实现；
- UnityFarm 只拥有具体表、具体行模型、生成的 Binding/Catalog、项目 Module 定制以及向 `Game.Farm.Runtime` 的配置适配；
- 不修改 UnityFarm 的 `Library/PackageCache`，也不把小麦、田地或 FarmM1 规则放进 Ember 框架或模板。

## 2. 为什么现在做，以及不做什么

### 2.1 要解决的问题

- 为稳定字符串 ID 提供确定、可诊断的查询容器；
- 让策划数据可以从电子表格导出的 CSV/TSV 进入 Unity；
- 在 Editor 阶段发现重复主键、列缺失、类型错误和跨表引用错误；
- 生成或烘焙稳定的运行时数据，避免业务代码各自重复写 CSV 解析；
- 让基础模板提供统一的 Global Module 接入方式；
- 让 UnityFarm 的纯数据核心只依赖规范化配置，不依赖文件格式或资源路径。

### 2.2 首版明确不做

- 不直接读取 `.xlsx`；Excel、WPS 或飞书表格先导出 UTF-8 CSV/TSV；
- 不做在线表格同步、远程配置、热更新或服务器下发；
- 不做本地化系统、复杂公式、宏或 Excel 计算引擎；
- 不从表结构自动生成业务 Row 类，首版由项目显式声明强类型 C# Row；
- 不支持 UnityEngine.Object、嵌套对象、任意多态或任意容器字段；
- 不依赖外部 Python 或操作系统脚本，导入、校验、烘焙和生成全部在 Unity Editor C# 程序集中完成；
- 不对字符串主键做 CRC/Hash 替换，运行时仍以原始稳定字符串 ID 查询；
- 不提供默认线性扫描的任意字段查询；需要高频非主键查询时必须显式声明并生成二级索引；
- 不把 XOR 当作安全能力；首版二进制不压缩、不加密，未来如有保密需求通过独立 Provider/发布管线扩展；
- 不把配置表注册成所有项目必需的 `IEmberManager`；
- 不在首版引入新的第三方解析依赖；如果未来必须直接读取 `.xlsx`，另做依赖、许可证和 UPM 迁移评估。

## 3. 冻结的总体方案

首版采用：

> C# 强类型 Row 是 Schema → UTF-8 CSV/TSV 是编辑源 → Editor 校验并烘焙 → 生成强类型 Codec/注册绑定 → Runtime 加载只读表 → 业务 Module 查询或适配。

```text
*.etable.csv / *.etable.tsv
          +
项目声明的强类型 Row
          +
EmberTableDefinition
          │
          ▼
Ember.Table.Editor
解析 → Schema 校验 → 主键/引用校验 → 批量暂存 → 回读验证 → 原子提交
          │
          ├── 每表一个版本化二进制（带 sourceHash / schemaHash / payloadHash）
          ├── <RowType>.TableBinding.g.cs（生成器管理）
          └── GameTables.Catalog.g.cs（生成器管理）
          │
          ▼
项目层 GameTableModule（由 base 模板部署，Global，可选）
          │ 继承
          ▼
Ember.Table.Integration
EmberTableModuleBase<TModule>（生命周期、资源读取、失败回滚）
          │ 持有
          ▼
Ember.Table Runtime
EmberTableEngine（staging、校验、快照切换）
          │
          ▼
EmberTableDatabase（实例对象）
          │
          ▼
具体业务 Module / Config Adapter
```

### 3.1 为什么采用代码优先 Schema

- Row 类型天然参与 C# 编译和重构；
- 项目可以明确控制 API 名称、枚举和业务语义；
- 避免首版引入“表改动 → 生成 Row → Unity 编译 → 再导入数据”的循环依赖；
- 生成器只负责稳定的强类型读取、主键访问和注册绑定，不拥有用户业务类型；
- 以后仍可增加 Schema-first Row 生成器，不破坏 Runtime 查询契约。

### 3.2 为什么 Runtime 不直接消费源 CSV

- 源文件错误应在 Editor/CI 阶段暴露，不应延迟到玩家运行时；
- 烘焙结果可携带版本、Schema Hash 和 Source Hash，便于陈旧产物诊断；
- Runtime 不需要理解注释行、Excel 导出差异或 Editor 专用元数据；
- 后续更换二进制格式、Addressables 或远程 Provider 时不影响业务查询 API。

### 3.3 Burner 对照结论与取舍

Burner 已在两百余张配置表的规模下验证了“策划维护 CSV、工具生成强类型代码和每表二进制、Runtime 只读二进制”的主路径。Ember 采用这个宏观分层，但不复制其项目级实现细节。

| Burner 做法 | Ember 决策 | 原因 |
|---|---|---|
| CSV 只作为编辑源，Runtime 只读 `.conf` | 采用 | 将格式容错、策划错误和反射限制在 Editor |
| 每张表独立二进制 | 采用 | 支持细粒度更新、定位、缓存和陈旧检测 |
| 生成逐字段 C# 读取代码 | 采用并强化 | 生成 Binding/Codec，Runtime 无反射且适配 IL2CPP/AOT |
| Unity 菜单调用外部 Python | 不采用 | UPM 框架应跨机器开箱即用，避免 Python 版本和路径依赖 |
| 字符串 ID 转 CRC 整数 | 不采用 | Ember 需要可读、稳定、`Ordinal` 的原始字符串 ID，避免碰撞和反查成本 |
| 每表静态 `List` / `Dictionary` 懒加载 | 不采用 | 改用实例数据库、Required 表预加载和整批原子切换，便于生命周期与测试隔离 |
| 无版本头的顺序字段流 | 不采用 | 增加 Magic、版本、Table/Row 标识、Schema/Source/Payload Hash 和长度校验 |
| XOR 混淆 | 不采用 | 不提供虚假的保密保证；真正的加密应属于独立发布能力 |
| 非主键条件线性扫描 | 不采用 | 查询热路径只允许主键或显式生成的二级索引 |
| 导出后再汇总错误 | 不采用 | 所有表通过校验和回读验证后才提交正式产物，失败保持旧版本完整 |

### 3.4 与 SceneUI 一致的三层所有权

表系统采用与 `EmberSceneUIEngine → EmberSceneUIModuleBase<TModule> → SceneUIModule` 相同的分层，不让具体项目重复实现通用生命周期：

| 层级 | Table 类型 | 所有权与职责 |
|---|---|---|
| 框架核心 | `EmberTableEngine` | 纯实例引擎；消费 Catalog、Binding 和已取得的字节，完成解码、staging database、Required/Optional 校验与快照切换；不实现 `IEmberModule`，不直接访问具体资源后端 |
| 框架集成 | `EmberTableModuleBase<TModule>` | 实现通用 `IEmberModule` 生命周期；通过 `EmberResourceManager` 取得字节、持有并释放 Engine、处理初始化异常和热重启 |
| 模板/项目 | `GameTableModule` | `base` 部署的具体项目 Module 薄壳；选择 Global Phase、启用策略、生成 Catalog 和项目扩展钩子 |
| 具体业务 | `FarmModule`、`InventoryModule` 等 | 查询 `GameTableModule`，或优先消费项目配置 Adapter 转换后的领域配置；不自行解析二进制或持有另一套数据库 |

框架契约如 `IEmberTableBinding`、`IEmberTableCatalog` 和诊断类型必须定义在 `com.ember`，不能定义在模板。模板无法预知的表专用接口或领域接口应由具体项目定义；首版不为了形式统一而增加一个没有业务语义的 `IGameTableModule`。

`base` 模板部署后，其 `GameTableModule` 文件已经归具体项目所有，因此项目通常通过 `GameTableModule.User.cs`、Row、Definition 和生成 Catalog 完成定制，不再派生第二个项目 Module。只有某个项目确实需要不同生命周期时，才显式替换该薄壳，而不是修改框架 Engine。

## 4. 程序集与目录规划

```text
Packages/com.ember/Table/
├── Runtime/
│   ├── Ember.Table.Runtime.asmdef
│   ├── Attributes/
│   ├── Database/
│   ├── Diagnostics/
│   ├── Engine/
│   ├── Loading/
│   └── Serialization/
├── Integration/
│   └── Runtime/
│       ├── Ember.Table.Integration.asmdef
│       └── EmberTableModuleBase.cs
├── Editor/
│   ├── Ember.Table.Editor.asmdef
│   ├── Import/
│   ├── Bake/
│   ├── Generation/
│   ├── Validation/
│   └── UI/
├── Tests/
│   ├── EditMode/
│   │   └── Ember.Table.Tests.asmdef
│   ├── Integration/
│   │   └── Ember.Table.Integration.Tests.asmdef
│   └── Editor/
│       └── Ember.Table.Editor.Tests.asmdef
└── Documentation~/table/README.md
```

建议命名空间：

- Runtime：`Ember.Table`；
- Integration：`Ember.Table.Integration`；
- Editor：`Ember.Table.Editor`；
- 测试：`Ember.Table.Tests`、`Ember.Table.Integration.Tests`、`Ember.Table.Editor.Tests`。

依赖方向固定为：

```text
具体项目 / 生成 Catalog
          ↓
Ember.Table.Integration → Ember.Core + Ember.Resource
          ↓
Ember.Table.Runtime
```

`Ember.Table.Runtime` 不引用业务程序集、`Ember.Core`、`Ember.Resource`、Scene、UI 或具体资源 Provider；Engine 只消费调用方提供的字节和框架 Binding/Catalog。`Ember.Table.Integration` 只负责把 Core 生命周期、Resource Manager 与 Engine 接起来。Editor 可以引用 Runtime、必要的 Ember Editor 基础能力和 UnityEditor，但不应通过 Integration 才能完成纯导表。框架程序集禁止反向引用 `Game.*` 或生成 Catalog。

## 5. Runtime 能力契约

最终类型名可在实现审计后小幅调整，但职责必须保留。

| 能力 | 建议类型 | 要求 |
|---|---|---|
| Row 标识 | `EmberTableAttribute` | 声明稳定 Table ID |
| 主键 | `EmberTableKeyAttribute` | 每个 Row 恰好一个主键 |
| 列映射 | `EmberTableColumnAttribute` | 可覆盖默认成员名 |
| 跨表引用 | `EmberTableReferenceAttribute` | Editor 校验目标 Table ID |
| 只读表 | `EmberTable<TKey, TRow>` | `TryGet`、索引查询、Count、确定顺序枚举 |
| 数据库 | `EmberTableDatabase` | 实例化持有多张表，不使用新的静态全局表缓存 |
| 纯加载引擎 | `EmberTableEngine` | 持有当前数据库快照，接收 Catalog/Binding/字节并完成 staging、校验、提交和释放 |
| 表目录 | `IEmberTableCatalog` | 只读枚举 Table ID、Binding、Required/Optional、逻辑资源路径和限制，不负责资源加载 |
| 加载结果 | `EmberTableLoadResult` | 成功、错误码、诊断列表、表信息 |
| 诊断 | `EmberTableDiagnostic` | Table、文件、行、列、字段、错误码和消息 |
| 二进制读写 | `EmberTableBinaryReader/Writer` | 固定小端、UTF-8、长度上限和严格类型语义，不直接暴露 `BinaryReader/Writer` 差异 |
| 生成绑定 | `IEmberTableBinding` | 提供 Table/Row 标识、资源路径、Schema Hash、逐行读取和强类型主键访问 |
| 格式入口 | `IEmberTableCodec` | 校验文件头并驱动 Binding 构建表，隔离当前二进制版本以允许未来升级 |
| Module 集成 | `EmberTableModuleBase<TModule>` | 位于 Integration 程序集，统一封装 Resource → Engine 生命周期，不包含具体表或领域逻辑 |

Runtime 必须满足：

- 字符串 ID 使用 `StringComparer.Ordinal`，大小写敏感；
- 主键非空，重复主键整表加载失败；
- 同一 Table ID 重复注册得到明确结果，不静默覆盖；
- 表成功构建后只读，调用方不能获得或修改内部 `List` / `Dictionary`；Row 本身遵守第 6.3 节的不可变契约；
- Runtime 加载和查询路径不使用反射、`Activator` 或表达式动态编译；反射仅允许 Editor 分析 Row Schema；
- Engine 不直接调用 `EmberResourceManager`；Integration ModuleBase 先取得每张表的字节，再把完整加载输入交给 Engine；
- 一次加载先在独立 staging database 中构建全部表；任一 Required 表失败则丢弃整个 staging，保留旧数据库；
- Optional 表失败时记录诊断并从新快照中省略，不把旧 Optional 表与新 Required 表混成跨版本快照；
- 全部 Required 表成功后只原子替换一次数据库引用，调用方不会观察到半批新旧数据；
- 查询未知表或未知 ID 不抛出难以诊断的空引用；
- 浮点和数字转换固定使用 `InvariantCulture`；
- 表的枚举顺序固定为源数据行顺序；
- 默认查询路径只包含主键字典；二级索引必须由 Definition 显式声明、在 staging 阶段完整构建并与主表一起提交；
- 加载/查询不依赖 Scene、MonoBehaviour、EUI 或具体资源后端；
- 纯容器不直接输出日志，错误通过结构化结果交给拥有者决定如何使用 `EmberDebug` 报告。

## 6. CSV/TSV 与 Schema 规则

### 6.1 源文件基线

- UTF-8，支持有 BOM 或无 BOM；
- 支持 CRLF 与 LF；
- `.etable.csv` 使用逗号，`.etable.tsv` 使用制表符；
- 遵守 RFC 4180 的引号、转义引号、单元格内分隔符和单元格内换行；
- 空白数据行可以跳过，但不能改变诊断的真实行号；
- 第一行是列名，列名必须唯一；
- 不使用“第二行写类型”的隐式 Schema，类型来自 C# Row；
- 默认要求所有必填列存在；未知列默认报错，可通过显式兼容选项降级为警告；
- 不静默 Trim 主键或修改大小写；格式问题应给出准确诊断；
- 非法数字、溢出、非法枚举和非法 bool 一律报错，不得回退为 `0`、默认枚举值或其他默认值；
- bool 首版只接受忽略大小写的 `true` / `false`，不接受拼写容错、`0/1` 或项目方言。

### 6.2 首版支持类型

- `string`；
- `bool`；
- `byte`、`short`、`int`、`long` 及无符号对应类型；
- `float`、`double`、`decimal`；
- enum；
- 可空的上述值类型。

数组、List、Dictionary、UnityEngine.Object、嵌套对象和多态类型不进入首版。需要引用另一张表时，Row 保存对方稳定 ID，并用 `EmberTableReferenceAttribute` 做 Editor 校验。

### 6.3 Row 构造与不可变契约

- Row 必须是非抽象 `sealed class` 或 `readonly struct`，不得继承 `UnityEngine.Object`；
- 每个参与持久化的成员必须是实例只读字段或只有 getter 的属性，静态成员、索引器和计算属性不参与 Schema；
- 每个 Row 恰好一个成员标记 `EmberTableKeyAttribute`，主键首版只允许 `string`；
- Row 必须提供一个明确标记的表构造函数；其参数与持久化列一一对应，生成 Binding 直接调用该构造函数；
- 不允许通过无参构造后反射写字段，也不允许运行时成员赋值；
- 二进制字段规范顺序固定为“主键优先，其余列按最终列名的 `Ordinal` 顺序”，不得依赖反射返回顺序或 CSV 列顺序；
- Schema Hash 必须覆盖 Table ID、稳定 Row Type ID、字段顺序、字段名、线格式类型、可空性、主键和引用元数据；
- 稳定 Row Type ID 使用“程序集简单名 + 完整类型名”，不包含程序集版本号、Culture 或 PublicKeyToken。

## 7. Editor 导入、烘焙与生成

### 7.1 EmberTableDefinition

项目为每张表建立 Definition，至少包含：

- Table ID；
- 源 CSV/TSV `TextAsset`；
- Row `MonoScript`；
- 是否 Required；
- 未知列策略；
- 运行时输出路径；
- 单表最大字节数、最大行数和最大字符串 UTF-8 字节数；
- 可选的二级索引声明；
- 可选的跨表校验设置。

Definition 必须验证 Row 类型满足不可变构造契约、Table ID 稳定、主键唯一且类型受支持。默认物理产物为 `Assets/GameResource/Resources/Config/Tables/<TableId>.bytes`，生成绑定保存不带扩展名的逻辑资源路径；项目覆盖输出路径时仍必须位于允许的资源根目录内。

### 7.2 烘焙产物

每张表生成一个 `.bytes` 文件。V1 线格式冻结如下，所有整数均为小端，Hash 均保存 32 字节原始 SHA-256 值：

```text
[Magic:4 = ASCII "ETBL"]
[FormatVersion:u16 = 1]
[Flags:u16 = 0]
[TableIdByteLength:u16][TableId:utf8]
[RowTypeIdByteLength:u16][RowTypeId:utf8]
[SchemaHash:32]
[SourceHash:32]
[RowCount:i32]
[PayloadLength:i32]
[PayloadHash:32]
[Payload:PayloadLength]
```

读取文件头时必须先完成以下验证，再为行或字符串分配内存：

- Magic、格式版本和 Flags 合法；V1 遇到未知 Flags 直接失败；
- Table ID、Row Type ID 和生成 Binding 完全一致；
- Schema Hash 和生成 Binding 完全一致；
- 行数、字符串长度和 Payload 长度非负且不超过 Definition/Runtime 上限；
- Payload 长度等于剩余字节数，Payload SHA-256 完全一致；
- 解码完成后必须恰好消费全部 Payload，不允许尾随未知数据。

Payload 按第 6.3 节的规范字段顺序逐行连续写入：

- `bool`：一个字节，只允许 `0` 或 `1`；
- 整数：按声明位宽写入小端补码/无符号值；
- `float` / `double`：IEEE 754 小端；
- `decimal`：`decimal.GetBits` 的四个 `i32`，按固定顺序写入；
- enum：按其声明的底层整数类型写入，并在 Editor 阶段拒绝未定义值；
- `string`：`i32` UTF-8 字节长度后跟字节；`-1` 表示 null，`0` 表示空字符串；主键不得为 null 或空字符串；
- nullable 值类型：一个 `0/1` 存在标记，存在时紧跟底层值；
- V1 不写字段名、数组、对象图、压缩块或加密块，字段含义由已验证的 Schema Hash 和 Binding 确定。

Source Hash 基于解析后的规范单元格矩阵计算，不基于源文件原始字节，因此 BOM、CRLF/LF 差异不改变 Hash；Schema Hash 与 Source Hash 的规范序列化算法必须在 F0 用固定测试向量冻结。

单表烘焙和全量烘焙都必须满足：先完成解析、Schema/引用校验、二进制暂存和回读解码比对，再提交正式产物。全量操作把所有二进制、生成代码和产物清单暂存为一个批次；任一表失败则正式目录零写入。提交阶段保留旧文件备份，任一替换失败则回滚整批。只有带生成器所有权清单的孤儿文件可以清理，不得按目录或文件名猜测删除用户文件。最后只触发一次 `AssetDatabase.Refresh`。

### 7.3 强类型 Binding 与注册生成

生成器不生成用户 Row 类型，而是为每张表生成强类型 Binding/Codec，并生成一个 Catalog 汇总注册。单表数据值变化只重写二进制，不改 C#；只有 Schema、资源路径或表清单变化时才重写生成代码。

每表 Binding 至少固化：

- Table ID、稳定 Row Type ID、逻辑资源路径、Format Version 和 Schema Hash；
- 按 V1 线格式读取各字段并直接调用 Row 构造函数的 `ReadRow`；
- 无反射的强类型主键访问器；
- 显式声明的二级索引访问器和冲突策略。

生成文件必须：

- 有明确的生成器所有权标记；
- 每次生成可完全重建；
- 不包含用户手写区；
- 使用每表 Binding 和 `GameTables.Catalog.g.cs` 注册全部表；
- 对重复 Table ID、重复输出路径和无效 Row 类型零写入失败；
- 不依赖 Runtime 反射、`Activator`、动态代码生成或第三方序列化器；
- 使用第 7.2 节的批量暂存、所有权清单、原子替换和失败回滚，避免 Unity 导入半成品；
- 纳入项目中心的生成物一致性校验或提供等价检查入口。

### 7.4 Editor 操作入口

至少提供：

- 创建 Table Definition；
- 校验当前表；
- 烘焙当前表；
- 校验全部；
- 烘焙并生成全部；
- 查看带行列定位的诊断；
- 定位源文件、Definition、Row 类型和产物；
- 用系统默认表格应用打开源 CSV/TSV，但不在首版内置单元格编辑器；
- 预览将写入、替换和清理的生成器所有产物；
- 检测 Source/Schema 改变后产物是否陈旧；
- 浏览已烘焙表的元信息和只读行数据，浏览功能复用 Runtime Binding 解码，不另写一套解释器。

## 8. 基础模板接入

### 8.1 所有权与生命周期

表能力是可选业务能力，不新增一个会被所有项目自动初始化的框架 Manager。通用 Engine 和 Module 生命周期基类属于 `com.ember`，`base` 模板只提供下面的项目层薄壳和数据目录：

```text
Assets/Game/Module/Table/
├── GameTableModule.cs
└── GameTableModule.User.cs

Assets/Game/Table/
├── Rows/
├── Definitions/
└── Generated/
    ├── <RowType>.TableBinding.g.cs
    └── GameTables.Catalog.g.cs

Assets/GameResource/TableSources/
Assets/GameResource/Resources/Config/Tables/
```

`EmberTableModuleBase<TModule>` 的约束：

- 位于 `Ember.Table.Integration`，实现 `IEmberModule`，具体模板和项目不复制其生命周期代码；
- 创建、持有和释放一个 `EmberTableEngine` 实例，对外只暴露当前只读 Database、`IsReady` 和最近一次 `LastLoadResult`；
- `OnInit` 先检查 Resource Manager 已完成初始化，通过 Catalog 逐表调用 `EmberResourceManager.LoadFileSync` 取得小型配置字节，再一次性交给 Engine 构建 staging database；
- Required 表缺失或无效时保存完整诊断、释放 staging、关闭 Ready 状态并抛出明确初始化异常；现有 Collector 捕获异常且不会把模块标为 Active；
- Optional 表失败时保留结构化诊断，按 Runtime 快照规则决定新数据库内容；
- 全部 Required 表成功后才接受 Engine 的单次快照交换并把 `IsReady` 设为 true；
- `OnDestroy`、`ResetModuleData` 及初始化中途异常都走同一个幂等 Shutdown 路径，不遗留 Engine、数据库或旧诊断引用；
- 不包含具体 Table ID、Row 类型、资源路径、项目日志文案或领域适配。

`GameTableModule` 的约束：

- 是 `GameTableModule : EmberTableModuleBase<GameTableModule>` 的项目层具体业务 Module，`ModulePhase.Global`；
- 基础模板默认 `Enabled = false`，项目明确需要后再启用；
- 只把生成的 `GameTables.Catalog.g.cs` 提供给 ModuleBase，并声明项目级启用策略或少量扩展钩子；
- `GameTableModule.User.cs` 归项目维护，可以添加领域适配、项目事件或便捷查询，但不得重新实现二进制解析、资源加载事务和数据库切换；
- 模板随一个可编译的空 Catalog 接入点交付，项目生成真实 Catalog 后替换生成器所有文件；
- 模板业务日志使用 `EmberDebug`，不直接使用 `Debug.Log`；
- Gameplay Module 在使用前显式检查 `IsReady`；领域核心优先依赖项目 Adapter 输出的规范化配置，不直接依赖表文件、资源路径或 Engine；
- 模板部署后该类已经是具体项目 Module，项目不需要再继承第二层 Module；如需面向测试或领域隔离的接口，由具体项目定义有业务语义的接口。

### 8.2 模板开发流程

1. 在 embedded 框架开发项目中加载 `base` 模板；
2. 在项目 `Assets` 副本中创建和验证模块、空目录/占位、生成入口与最小示例；
3. 通过 `Ember/项目中心 → 模板开发` 保存 `base`，不得手改 `Templates~`、`ParentSnapshot~` 或 hash；
4. 这是新增结构，预期对 `base` 做 minor Bump，实际版本以实施时仓库状态为准；
5. 预览 `source3d-2p5d` 父级更新并通过正式三方同步吸收；
6. 重新编译、校验并对派生模板做相应版本 Bump；
7. 验证两份模板的 metadata、contentHash、versionedContentHash、父版本和 ParentSnapshot 一致。

`source3d-2p5d` 不另写 2.5D 专用表系统，只继承 `base` 的业务接入骨架。

## 9. UnityFarm 最终接入契约

框架和模板发布后，UnityFarm 另开任务完成消费端接入：

1. 升级 `com.ember` 到包含 Table 功能的已发布不可变 tag；
2. 不修改 `Library/PackageCache`；
3. 对已持续开发的 UnityFarm 优先人工迁移新的模板文件；只有确认目标均为缺失文件时才考虑“补齐缺失”，不完整重新部署覆盖业务目录；
4. 启用 `GameTableModule`；
5. 在 UnityFarm 定义 `CropRow` 等业务 Row 和源表；
6. 通过框架工具校验、烘焙和生成注册绑定；
7. `FarmM1` 配置适配层把 `CropRow` 映射为 `Game.Farm.Runtime` 的 `CropConfig`；
8. `Game.Farm.Runtime` 仍然只依赖规范化配置，不直接引用 `GameTableModule`、Ember Table、TextAsset 或资源路径；
9. P2 纯数据测试继续直接构造配置；另补一组 UnityFarm EditMode 集成测试验证表数据到 Farm 配置的映射。

建议首批 UnityFarm 表：

| Table ID | 用途 | 首批字段 |
|---|---|---|
| `crop` | 作物配置 | id、displayName、growthSeconds、yieldAmount、unitPrice、needsWater |
| `farm_m1` | M1 运行参数 | updateInterval、wetDuration、wetGrowthMultiplier、完成条件 |

田地 `field_1` / `field_2` 和无人机 `drone_a` 是否进入表，由 UnityFarm 场景绑定方案决定；场景实例 ID 不应为了“都表格化”而强行移出显式 SceneBinding。

## 10. 实施阶段与 Gate

### F0：契约冻结

- 审计现有 Basic、Resource、Core、代码生成、项目校验和模板 API；
- 冻结命名、CSV 规则、不可变 Row 构造契约、主键语义、诊断格式、V1 线格式和所有长度上限；
- 为 Schema Hash、Source Hash 和完整 V1 文件建立跨平台固定测试向量；
- 确认首版无 `.xlsx` 和第三方依赖。

Gate：职责不与 Resource Manager、DataSaver 或业务 ScriptableObject 重复；没有 UnityFarm 产品字段进入框架。

### F1：Runtime 核心与框架集成

- 创建相互隔离的 Runtime 与 Integration asmdef；
- 在 Runtime 实现属性、严格二进制 Reader、只读表、数据库、加载结果、诊断、Catalog、Binding 和 Codec 契约；
- 在 Runtime 实现纯 `EmberTableEngine`、staging database、整批原子替换和确定性查询；
- 在 Integration 实现 `EmberTableModuleBase<TModule>`，只连接 Core 生命周期、Resource Manager 与 Engine；
- 完成 Runtime 和 Integration EditMode 测试。

Gate：Runtime asmdef 不引用 Core、Resource 或业务程序集；无场景、无 Runtime 反射即可独立测试 Engine；ModuleBase 的初始化失败、热重启和销毁均不泄漏状态；任一 Required 表失败时整批回滚。

### F2：Editor 导入与生成

- 实现 CSV/TSV 解析；
- 实现 Definition、Schema 校验、跨表引用校验；
- 实现确定性 V1 二进制、Hash、长度限制和陈旧检测；
- 实现每表强类型 Binding、Catalog、所有权清单和诊断 UI；
- 实现单表/全量暂存、回读验证、批量提交和失败回滚；
- 接入项目校验或等价生成物校验；
- 完成 Editor 测试。

Gate：错误精确定位到行列；校验或提交失败不改变任何正式产物；相同逻辑输入产物逐字节稳定。

### F3：基础模板业务接入

- 在已加载的 `base` 项目副本中实现继承 `EmberTableModuleBase<GameTableModule>` 的薄 `GameTableModule` 和用户扩展入口；
- 通过实际生成链路建立空 Catalog/Binding；
- 验证模板 Module 只提供 Catalog/项目钩子，没有复制 ModuleBase 的加载和清理逻辑；
- 用项目中心保存并封存 `base`。

Gate：模块默认关闭时零影响；启用后 Resource → ModuleBase → Engine 的启动顺序正确；Required 失败不会伪装成功；部署后的项目无需再派生第二个 Module。

### F4：2.5D 父子同步

- 预览父级变化；
- 解决冲突并正式同步；
- 验证 ParentSnapshot、hash、版本与编译；
- 不在派生模板复制第二套实现。

Gate：`source3d-2p5d` 完整获得能力且保留自身场景、PlayerControl 和 SceneUI 差异。

### F5：框架发布

- 作为向后兼容的新公开能力做 minor 版本发布；当前基线下预期是 `0.12.0`，但实施时必须先检查是否已有更新版本；
- 更新 `package.json`、CHANGELOG、API 速查、Table 文档、测试清单、依赖发布声明和发布说明；
- 完成 Unity MCP 编译、EditMode 测试、模板验证和新消费项目安装验证；
- 创建不可变 tag 并推送；
- 输出 UnityFarm 消费迁移说明，不在该框架任务中直接修改 UnityFarm。

Gate：发布声明、代码、模板兼容版本、模板内容版本和实际 tag 一致；消费项目能从已发布 tag 安装并读到测试表。

## 11. 最低测试矩阵

### Runtime

- `EmberTableEngine` 可在不引用 Core、Resource、Scene 或业务程序集时独立构造、加载和释放；
- 注册并按主键查询；
- 未知表、未知键；
- 重复 Table ID；
- 空键和重复行键；
- 大小写敏感；
- 源顺序枚举；
- 只读集合和不可变 Row 不能污染内部状态；
- 多张 Required 表中任一失败不替换旧数据库；
- Optional 表失败被省略且不会混入旧版本表；
- 数据库交换前后查询只能看到完整旧快照或完整新快照；
- 多次清理和热重启幂等；
- 诊断内容确定。

### CSV/TSV 与 Editor

- UTF-8 BOM/无 BOM；
- CRLF/LF；
- 逗号、Tab、引号、转义引号、单元格换行；
- 缺列、重复列、未知列；
- bool、整数、浮点、decimal、enum、nullable；
- `InvariantCulture`，不受机器区域设置影响；
- 类型错误精确到文件/行/列；
- 非法 bool、数值溢出、非法枚举不得静默转换为默认值；
- 重复主键；
- 跨表引用存在/缺失；
- Source Hash/Schema Hash 变化检测；
- V1 Magic、版本、Flags、Table/Row 标识和 Schema Hash 不匹配；
- 负数或超上限的行数、字符串长度和 Payload 长度；
- Payload 截断、尾随字节和 Payload Hash 不匹配；
- 固定测试向量验证小端、UTF-8、decimal、nullable 和各数值位宽；
- 相同输入重复烘焙结果一致；
- BOM 与换行差异不改变 Source Hash 和最终产物；
- 全量校验/回读/提交任一失败均整批回滚旧产物；
- 生成路径越界、同路径冲突、用户文件保护；
- 孤儿清理只处理所有权清单内产物；
- 生成绑定与 Definition 不一致时可被项目校验发现。

### Module、模板和消费端

- `EmberTableModuleBase<TModule>` 统一拥有 Engine，具体 `GameTableModule` 不复制资源加载和快照事务；
- Resource Manager 先于 Global `GameTableModule`；
- 默认关闭不构造、不登记；
- 启用后 Required/Optional 表行为正确；
- 初始化异常、`OnDestroy` / `ResetModuleData` 共用幂等清理且无残留；
- 模板空 Catalog 可编译，项目生成 Catalog 后能直接加载而无需新增第二个 Module；
- 具体业务 Module 不能绕过 `GameTableModule` 另建全局静态表缓存；
- base 模板编译与校验；
- source3d-2p5d 父同步后编译与校验；
- 新消费项目安装、部署、运行和表查询；
- 已有项目升级框架不会自动覆盖模板业务文件。

## 12. 工程规范与验证约束

- 开始写新数据结构前完整阅读 `docs/dev/ember-api-reference.md`；
- 遵守 `CLAUDE.md` 的命名、Region、XML 注释、`[HasGC]` / `[NoGC]` 和 `EmberDebug` 规范；
- 所有文件编辑使用 `apply_patch`，保留现有用户改动；
- 不直接修改模板快照、ParentSnapshot、metadata hash；
- C#、asmdef 或资源改动后只使用 Unity MCP 做有界编译和测试验证；
- 不以 dotnet、BatchMode、Editor.log 或静态检查替代 Unity 验证；
- MCP 不可用时明确记录未完成验证，并要求用户在 Unity 中手动触发编译；
- 测试和生成物必须使用固定、可复现的样例，不把 UnityFarm 产品数据塞入包测试。

## 13. 最终交付物

- `Ember.Table.Runtime`、`Ember.Table.Integration` 与 `Ember.Table.Editor`；
- Runtime、Integration 和 Editor EditMode 测试；
- CSV/TSV Definition、校验、V1 二进制烘焙、强类型 Binding/Catalog 生成和诊断入口；
- Table 包文档与 API 速查更新；
- `base` 的可选 `GameTableModule` 业务接入骨架；
- `source3d-2p5d` 正式父子同步结果；
- 框架/模板版本、CHANGELOG、发布说明和不可变 tag；
- 新消费项目实际安装与读表验证记录；
- 面向 UnityFarm 的升级与人工迁移清单。

## 14. 已知风险

| 风险 | 处理 |
|---|---|
| 把表系统做成另一个 Resource Manager | Table 只处理 Schema、数据和查询；资源后端仍由 Ember Resource 负责 |
| Engine 被 Core/Resource 生命周期污染 | Runtime 与 Integration 分 asmdef；Engine 只消费 Catalog、Binding 和字节，ModuleBase 单独负责 Resource 接线 |
| 每个项目复制 Module 生命周期 | 通用初始化、失败回滚、热重启和清理固化在框架 ModuleBase；模板只交付薄 `GameTableModule` |
| 模板 Module 与项目 Module 重复继承 | 模板部署文件本身即项目 Module；项目通过 User 扩展、Row 和生成 Catalog 定制，不再派生第二层 |
| Global Module 初始化时异步未完成 | 首版 Required 小表使用 `LoadFileSync`，保证 `OnInit` 返回时状态确定 |
| Runtime 反射、动态代码和 GC 失控 | Row 分析反射只存在于 Editor；Runtime 使用生成 Binding，查询路径只走强类型 Dictionary |
| CSV 方言不一致 | 冻结 UTF-8、分隔符和 RFC 4180，引号/换行纳入测试 |
| Schema 与二进制错配 | V1 文件头校验 Table/Row/Schema，Payload 长度和 Hash 校验损坏，固定测试向量锁定线格式 |
| 导出失败覆盖有效数据 | 全量暂存、回读验证、所有权清单、批量提交和提交失败回滚 |
| Runtime 出现半批新旧表 | 全部表先构建 staging database，Required 全部成功后只交换一次数据库引用 |
| 模板升级覆盖 UnityFarm | 框架任务只发布；UnityFarm 后续优先人工迁移或确认后的补缺 |
| 首版范围失控到 Excel/热更 | `.xlsx`、远程表、热更新和 Row 自动生成全部留待独立提案 |
| 把混淆误认为安全 | V1 明确不压缩、不加密；保密能力以后在 Provider/发布管线独立设计 |
| 业务开始依赖表系统而不是领域模型 | UnityFarm 通过适配层转换为 `Game.Farm.Runtime` 配置，核心保持来源无关 |

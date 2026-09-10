# Ember 配置表系统实施提示词

复制下面内容到一个新的 Codex 对话中执行。该提示词假定方案已经得到用户确认，目标是按权威计划实施，而不是重新停留在方案讨论。

---

继续开发 Ember Framework，完整实现通用配置表系统及基础模板接入。

项目路径：

`C:\Users\wuyu\My\ember-unity-framework`

权威实施方案：

`C:\Users\wuyu\My\ember-unity-framework\docs\dev\table-system-development-plan.md`

该计划是本任务的唯一架构依据，必须从头到尾完整阅读。如果下面的摘要与计划存在差异，以计划为准；不要在实施中自行改回另一套分层、二进制协议或 Row 生成路线。

背景：

- 当前框架基线为 `com.ember 0.11.5`；实施前必须重新读取当前实际版本，不能在仓库已经前进时硬写旧版本。
- 这项能力来自 UnityFarm 对稳定 ID 和作物配置的实际需求，但实现必须保持产品无关。
- 改动分流已经确定为“框架 + 模板升级”：通用表能力进 Ember Package，基础业务接入进 `base` 模板，`source3d-2p5d` 通过正式父子同步获得；本任务不直接修改 UnityFarm。
- 方案已确认采用“C# 强类型不可变 Row + UTF-8 CSV/TSV 编辑源 + Editor 严格校验/确定性烘焙 + 强类型 Binding/Catalog 生成 + Runtime 只读查询”。首版不直接读取 `.xlsx`，不做远程表、热更新、Row 自动生成，也不引入新的第三方解析依赖。
- 分层已经冻结为 `EmberTableEngine → EmberTableModuleBase<TModule> → GameTableModule`：纯 Engine 和通用契约属于 Runtime，生命周期/资源接线属于 Integration，模板只交付部署后归项目所有的薄 Module 壳。
- Burner 仅作为“CSV → 强类型代码 + 每表二进制 → Runtime 读取”的规模验证参考；不要复制它的 Python 依赖、CRC 字符串 ID、静态表缓存、懒加载 Required 表、无版本头二进制、XOR 或线性扫描查询。

开始前必须完整阅读：

1. `C:\Users\wuyu\My\ember-unity-framework\AGENTS.md`
2. `C:\Users\wuyu\My\ember-unity-framework\CLAUDE.md`
3. `C:\Users\wuyu\My\ember-unity-framework\docs\dev\table-system-development-plan.md`
4. `C:\Users\wuyu\My\ember-unity-framework\Packages\com.ember\Documentation~\maintenance\unityfarm-change-routing.md`
5. `C:\Users\wuyu\My\ember-unity-framework\docs\dev\ember-api-reference.md`
6. `C:\Users\wuyu\My\ember-unity-framework\docs\dev\ember-boot-sequence.md`
7. `C:\Users\wuyu\My\ember-unity-framework\docs\dev\template-upgrade-system.md`
8. `C:\Users\wuyu\My\ember-unity-framework\docs\dev\upm-migration-plan.md`
9. `C:\Users\wuyu\My\ember-unity-framework\Packages\com.ember\Documentation~\resource\README.md`

先审计：

- 当前 Git 状态和用户未提交改动；
- Basic、Resource、Core、Editor 代码生成与项目校验中是否已有可复用能力；
- 现有 asmdef、测试程序集和命名规范；
- 当前 `base`、`source3d-2p5d` 版本、父基线、contentHash 与编辑副本状态；
- 当前 package、CHANGELOG、Dependencies 发布声明和最新 tag；
- Unity MCP 是否连接。

必须实现：

1. `Packages/com.ember/Table/Runtime/Ember.Table.Runtime.asmdef`：属性、不可变 Row 契约、严格二进制 Reader/Writer、只读表、实例数据库、诊断、Catalog、Binding、Codec 和纯 `EmberTableEngine`。
2. `Packages/com.ember/Table/Integration/Runtime/Ember.Table.Integration.asmdef`：通用 `EmberTableModuleBase<TModule>`，只负责连接 Core 生命周期、Resource Manager 与 Engine。
3. `Packages/com.ember/Table/Editor/Ember.Table.Editor.asmdef`：CSV/TSV 解析、Definition、Schema/主键/跨表引用校验、确定性烘焙、Hash、陈旧检测、强类型 Binding/Catalog 生成和诊断工具。
4. Runtime、Integration 与 Editor 的独立 EditMode 测试程序集，覆盖计划第 11 节的全部最低测试矩阵。
5. 包内 Table README、`ember-api-reference.md`、测试清单与必要索引更新。
6. 在 embedded 项目中通过“加载 base → 修改项目 Assets → Unity 验证 → 项目中心保存 → 显式 Bump”的正式流程，加入默认关闭的 Global `GameTableModule : EmberTableModuleBase<GameTableModule>` 薄壳、User 扩展入口和可编译空 Catalog。
7. 使用项目中心预览并同步 `base` 到 `source3d-2p5d`，保留派生模板现有场景、PlayerControl 和 SceneUI 差异；不得手改 `Templates~`、`ParentSnapshot~`、`template.json` hash 或运行旧同步脚本。
8. 完成向后兼容的 minor 框架发布准备。当前基线下预期为 `0.12.0`，但必须依据实施时的实际版本选择下一版本。
9. 更新 `package.json`、CHANGELOG、Dependencies 发布声明、发布说明、模板兼容版本及模板内容版本。
10. 在新消费项目中实际验证从最终 Package 内容安装、部署模板、启用 Module、启动并读取测试表。
11. 输出面向 UnityFarm 的升级和人工迁移清单，但不要在本任务中修改 UnityFarm。

分层和依赖硬约束：

- `Ember.Table.Runtime` 不得引用 `Ember.Core`、`Ember.Resource`、Scene、UI、具体资源 Provider、业务程序集或生成 Catalog；
- `EmberTableEngine` 是可独立构造和释放的纯实例引擎，不实现 `IEmberModule`，不直接调用 `EmberResourceManager`；它只消费框架 Catalog/Binding 和调用方已取得的字节；
- `Ember.Table.Integration` 只引用 Runtime、Core 和 Resource，提供 `EmberTableModuleBase<TModule>`；通用初始化、资源读取、异常回滚、热重启和清理不能复制到模板；
- 框架契约如 `IEmberTableBinding`、`IEmberTableCatalog` 和诊断类型必须留在 `com.ember`，不能定义到模板；
- `GameTableModule` 是模板部署后归具体项目所有的 Module 薄壳，只提供生成 Catalog、项目策略和 User 钩子；项目不再派生第二个 Module；
- 具体项目拥有 Row、CSV/TSV、Definition、生成 Binding/Catalog 和领域 Adapter；具体业务 Module 不得自行解析二进制、另建静态表缓存或绕开 `GameTableModule`；
- `Ember.Table.Editor` 可以引用 Runtime 和必要的 Editor 能力，但纯导表不能依赖 Integration；框架程序集禁止反向引用 `Game.*`。

关键运行时要求：

- 字符串键使用 Ordinal、大小写敏感；
- 空键、重复键、重复 Table ID 明确失败；
- Row 必须是非抽象 `sealed class` 或 `readonly struct`，通过明确标记且参数覆盖全部持久化列的构造函数创建；持久化成员只读；
- 表构建后只读，不向调用方暴露可修改的内部 `List` / `Dictionary`；
- 枚举顺序保持源行顺序；
- 数字使用 InvariantCulture；
- Runtime 加载与查询不使用反射、`Activator`、表达式动态编译或第三方序列化器；反射只允许 Editor 分析 Schema；
- 全部表先构建 staging database；任一 Required 表失败则丢弃整批并保留旧数据库，全部 Required 成功后只交换一次数据库引用；
- Optional 表失败时记录诊断并从新快照省略，不能把旧 Optional 表与新 Required 表混成跨版本状态；
- 默认只提供主键查询；非主键查询必须显式声明并生成二级索引，禁止默认线性扫描；
- 纯容器不直接打日志，使用结构化结果；拥有它的 Module 才用 EmberDebug；
- 不依赖 Scene、MonoBehaviour、EUI 或具体资源 Provider；
- 不创建新的静态全局表缓存。

CSV/TSV 要求：

- UTF-8 BOM/无 BOM、CRLF/LF；
- RFC 4180 引号、转义引号、分隔符和单元格换行；
- 第一行为唯一列名，类型来自 C# Row；
- 缺列、重复列、未知列、类型错误必须带文件/行/列诊断；
- 首版支持 string、bool、整数、浮点、decimal、enum 和 nullable；
- bool 只接受忽略大小写的 `true` / `false`；非法 bool、非法枚举、数字格式错误和溢出必须失败，不能回退到默认值；
- 跨表引用在 Editor 校验；
- 相同输入得到稳定烘焙内容和 Hash；
- Source Hash 基于规范化单元格矩阵计算，BOM 和 CRLF/LF 差异不能改变 Source Hash 或最终产物；
- 单表和全量烘焙必须先暂存、回读解码比对，再提交正式产物；任一失败保持旧产物完整；
- 全量提交中任一替换失败必须回滚整批；生成文件必须带所有权标记和清单，只能清理清单内孤儿文件。

V1 二进制必须严格实现计划第 7.2 节，不要另选 JSON、MessagePack、SQLite 或 Burner `.conf` 协议：

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

- 所有整数小端，Hash 为 32 字节原始 SHA-256；
- 读取时在分配行或字符串内存前校验 Magic、版本、Flags、Table/Row 标识、Schema Hash、长度上限和 Payload Hash；
- 解码完成必须恰好消费全部 Payload，拒绝截断和尾随数据；
- 字段顺序固定为主键优先，其余列按最终列名 `Ordinal` 排序，不能依赖反射或 CSV 列顺序；
- 按计划冻结 bool、整数、IEEE 754、decimal、enum、string 和 nullable 的线格式；
- 首版每表一个 `.bytes`，不写字段名、数组、对象图、压缩或加密；默认输出到 `Assets/GameResource/Resources/Config/Tables/<TableId>.bytes`；
- Schema Hash、Source Hash 和完整文件必须建立固定跨平台测试向量。

生成要求：

- 每张表生成 `<RowType>.TableBinding.g.cs`，Catalog 生成 `GameTables.Catalog.g.cs`；不生成用户 Row 类型；
- Binding 固化 Table ID、稳定 Row Type ID、逻辑资源路径、Format Version、Schema Hash、强类型 `ReadRow`、主键访问器和显式二级索引；
- 数据值变化只重写二进制；仅 Schema、资源路径或表清单变化时重写 C#；
- Catalog 通过框架 `IEmberTableCatalog` 暴露只读加载元数据，不负责资源加载；
- 生成目录没有用户手写区；使用所有权标记、Manifest、暂存和原子提交保护用户文件；
- Editor 提供当前/全部校验与烘焙、诊断定位、源文件打开、陈旧检测、写入/清理预览，以及复用 Runtime Binding 的只读产物浏览。

模板生命周期要求：

- `EmberTableModuleBase<TModule>` 位于框架 Integration 程序集，统一拥有和释放 `EmberTableEngine`；
- `GameTableModule : EmberTableModuleBase<GameTableModule>` 是 `ModulePhase.Global` 的具体项目业务 Module，不是框架 Manager；
- base 默认 `Enabled = false`；
- Resource Manager 先初始化，启用后使用 `EmberResourceManager.LoadFileSync` 读取小型 Required 表；
- OnInit 返回前必须得到确定成功/失败结果；
- Required 表失败时 ModuleBase 保存完整结果、关闭 Ready、清理 staging/Engine 并抛出明确异常；现有 ModuleCollector 捕获异常且不把 Module 标为 Active；
- ModuleBase 提供只读 Database、`IsReady` 和最近 `LastLoadResult`，供后续 Gameplay Module 检查；
- 初始化异常、OnDestroy 和 ResetModuleData 必须共用幂等 Shutdown 路径，无残留且可热重启；
- `GameTableModule.User.cs` 只添加项目事件、领域适配和便捷查询，不重复实现资源加载与数据库事务。

测试至少覆盖权威方案第 11 节的全部项目。不要用测试文件存在、静态检查、dotnet、BatchMode 或 Editor.log 代替 Unity 结果。

工程约束：

- 保留用户现有改动，避免覆盖无关文件；
- 文件编辑使用 apply_patch；
- 遵守 EmberDebug、XML 注释 `《》`、Region、访问级别及 `[HasGC]` / `[NoGC]` 规范；
- 不把 UnityFarm 的 crop、field、drone 或 FarmM1 类型放进框架测试和模板；
- 不修改任何消费项目的 PackageCache；
- Unity MCP 是唯一自动编译和测试入口。只做有界状态确认；不可用时立即停止验证并明确要求用户手动触发 Unity 编译，不能声称通过；
- 模板只能通过项目中心的正式开发、保存、Bump 和父子同步流程修改。

执行方式：

- 方案方向已经确认，不要只输出另一份方案后停下；先完成必要审计，然后按 F0→F5 实施；
- 每个 Gate 通过后再进入下一阶段；
- 优先完成 Runtime/Integration/Editor 的代码与自动测试，再进入必须通过项目中心落盘的模板阶段；不要因为后续模板或发布需要人工授权而提前停止前面的可实施工作；
- 如果 Unity GUI 中必须由用户完成模板保存、Bump、父子冲突选择或发布授权，先把代码和可自动验证部分完成，再给出一次明确、最小的用户操作清单；
- 如果发现方案与当前源码存在实质冲突，停止对应写入，给出证据、影响和最小调整建议，不要自行扩大范围。
- 保持任务持续推进，但不要自行执行 `git commit`、创建/推送 tag、推送远端或发布 Package；这些操作必须在新对话中获得用户明确授权。

最终交付必须列出：

- 新增和修改文件；
- Runtime Engine、Integration ModuleBase、模板 Module 与具体项目的职责和依赖方向；
- 表格式、类型、主键、引用、诊断和原子替换规则；
- 实际 Unity 编译、EditMode、模板同步与消费项目验证结果；
- 框架和模板最终版本或建议版本；
- 尚未执行或需要人工完成的步骤；
- 已知限制和技术债；
- UnityFarm 应升级到的已授权发布 tag；若尚未授权发布，则给出建议 tag 和人工迁移清单，不得假称已经发布。

不要在这个任务里开始 UnityFarm MVP1 P2；框架能力完成、发布并给出消费指引后停止。

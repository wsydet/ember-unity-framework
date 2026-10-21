# Unity 工程外的 CSV/TSV 源表

框架 0.21.0 起，配表、多语言、图片库和模板工具可共用一份 Unity 工程外源文件。
运行时 Table ID、Resources 键、ETBL V1 格式、Schema 和加载 API 不变。
源表权限与 Unity 生成物权限可以继续按仓库目录分别管理；不需要放宽策划的 Program 写权限。

## 共享配置与绑定

在 Unity 工程的 `ProjectSettings/EmberTableSources.json` 提交配置。例如仓库包含并列的
`Design/`、`Art/`、`Program/`，Unity 工程在 Program：

```json
{
  "allowedRoots": ["../Design/TableSources"],
  "defaultRoot": "../Design/TableSources",
  "bindings": [
    { "tableId": "common_ui_text", "path": "../Design/TableSources/common_ui_text.etable.csv" },
    { "tableId": "novel_ui_text", "path": "../Design/TableSources/novel_ui_text.etable.csv" },
    { "tableId": "novel_content_text", "path": "../Design/TableSources/novel_content_text.etable.csv" }
  ]
}
```

为每份实际源表添加一项 bindings，包括图片、角色、语言、剧情正文等项目表。示例不是完整小说表清单。
所有路径相对 Unity 工程根目录，不能写机器绝对路径。允许中文、空格及两种路径分隔符。
新克隆或仓库换位置后只要相对布局相同即可继续解析。`defaultRoot` 只用于新建源表的默认位置，
不会自动改变既有 Definition 的绑定。Python 图片工具使用共享 bindings（未映射的表才使用显式参数或默认根）。

绑定优先级：共享配置中的 Table ID 映射 → 资产 `ExternalSourcePath` → 旧 `TextAsset Source`。
共享映射与非空资产外部路径同时存在时必须指向同一规范化文件，否则报错。旧 TextAsset 在显式绑定生效时
可以继续保留以兼容旧资产，但不会作为失败回退。映射目标缺失、不可读、非法 CSV/TSV 都会阻止导出。
同一 Table ID 的多语言注册和 Definition 使用相同共享映射；不允许两个 Definition 共用源文件、重复配置项，
或两个多语言注册共用源文件。Table ID 和配置键区分大小写。

未配置外部目录时继续支持旧 Assets 内源表。包内测试源允许读取，包内源禁止写入。
规范化路径后，外部文件必须位于显式允许根目录内，且后缀为 CSV/TSV；路径组件中的符号链接、
Windows 目录联接及其他 reparse point 被拒绝。不要把磁盘或整个 Unity 工程根目录设为允许根。
该边界用于工具误操作防护；具有本机文件系统管理员权限的恶意进程不属于此策略的隔离对象。

## API 与编辑行为

```csharp
string path = definition.SourcePath; // 替代 AssetDatabase.GetAssetPath(definition.Source)
byte[] bytes = EmberTableSourceFiles.ReadBytes(path); // 替代 definition.Source.bytes
var draft = EmberTableSourceDocument.Load(path);
// 修改 draft.Rows 后：
draft.Save(definition); // 并发检查、原子源文件替换、当前表烘焙及失败恢复

string localizationPath = localizationSource.SourcePath;
EmberLocalizationEditorService.Save(localizationSource,
    EmberTableSourceDocument.Load(localizationPath));
```

直接磁盘读写也应经过 `EmberTableSourceFiles.ReadText/ReadBytes/WriteBytes`，不要对外部文件调用
AssetDatabase.ImportAsset。WriteBytes 使用同目录临时文件与原子替换，不创建父目录；管理员应先建立根目录。
需要烘焙和草稿保护时使用 SourceDocument.Save，而不是直接 WriteBytes。

多语言和图片窗口按秒检查源表变化：干净文档重新加载，存在草稿则保留并报告冲突。
配表中心重新获得焦点及执行校验、预览、当前导出、全量导出时重读磁盘。保存比较原始字节，连格式变化
也会触发并发冲突。流水线提交前再次比较源文件快照；生成物仍通过原事务写入原 Assets 路径。
失败不会返回成功，也不会把旧 .bytes 当作本次导出结果。构建前多语言更新后，配表构建处理器在 order 110
校验全部源表并检查二进制是否当前；陈旧产物要求管理员先全量导出。

## 旧项目迁移步骤与恢复

消费端先通过 `Ember/UPM Manager` 升级框架。不要修改 PackageCache、manifest、lock 或模板部署基线来完成升级。
模板调用方须吸收本次改动；跨模板版本升级按项目中心预览结果决定人工合并范围。

1. 关闭源表编辑窗口并保存或导出草稿；暂停其他人导表。建立迁移分支，记录迁移前提交。
   在配表中心校验全部、预览全部、导出全部；记录每个 .bytes 的 SHA-256 和 Definition 的 Resources 路径。
   若旧源表已报错，先修复，不用现有产物作为成功依据。
2. 列出 `EmberTablePipeline.FindAllDefinitions()` 的 `TableId/SourcePath/RuntimeOutputPath`，以及
   `EmberLocalizationEditorService.Sources()` 的 `TableId/SourcePath`。按规范化源文件去重，保留两者的对应关系。
   对用户场景，预期是 21 个 Definition 加 common_ui_text，共 22 份源；以实际项目盘点为准。
   另查业务代码中 `Definition.Source`、`Source.bytes/text`、旧 TableSources 路径、直接 File 与 ImportAsset 调用。
3. 形成迁移清单：每项包含 Table ID、旧路径、目标相对路径、关联 Definition/注册资产、原始文件 SHA-256、
   目标是否存在及内容是否相同。将清单与所有源、.meta、Definition、注册资产及配置备份到 Assets 外的迁移备份目录。
   目标已有不同内容时停止，人工合并并重新校验；禁止覆盖。相同内容可复用但仍要核对绑定。
4. 创建 Design/TableSources；用 Git move 移动源文件到清单目标（保留原始字节），不要维护两份可编辑源。
   外部源不需要 .meta，将旧 CSV 的 .meta 留在迁移备份并从 Assets 移除。
   同一批次提交配置 allowedRoots/defaultRoot/bindings；程序和管理员维护 Program 内配置。
   不重建 Definition、Row、注册或生成资产，不动它们的 .meta/GUID、Table ID 和输出路径。
5. 共享映射已覆盖每个 Table ID 后，可在原 Definition 和多语言注册资产中清空失效的旧 TextAsset 引用；
   有旧 ExternalSourcePath 时清空或改为同一目标。只修改原资产字段，不删除重建资产。
6. 打开多语言中心，逐表保存；检查 Reviews 的 Key、SourceText、Pending 保持一致，再执行配表全量校验和导出。
   比较迁移前后 .bytes SHA-256（相同数据应逐字节一致）、Resources 路径、Schema，以及多语言 .asset 和审核状态。
   检查旧目录没有残留可编辑同名表。提交 Design 源与 Program 配置/引用更新；生成物无变化时不制造无意义提交。
7. 在全新克隆中重复验证；策划仅改 Design，管理员在 Program 导表并提交生成物。校验实际构建入口。

迁移失败时停止继续写入，保留草稿和迁移清单。恢复迁移前的配置及原资产内容（保留原 .meta），
按清单把源文件及其 .meta 从备份恢复至原位置，再刷新 Unity 并重做全量校验。
只删除此次迁移新建且 SHA-256 未被他人改动的目标；目标原本存在或随后被修改时保留并人工合并。
恢复前先备份当前状态，不使用全仓库 reset/clean。步骤 4–6 完成前不要让其他人使用中间状态导表。

## 模板与业务调用方

公共配表、项目校验、多语言、图片库基类和接线生成器由 Package 提供，所有模板复用。
base、platformer-2d 等无需复制一份源文件定位实现。接线生成器在外部 defaultRoot 生效时不再补建旧源目录 README。
视觉小说模板修改的是 Assets 源中的以下调用方，部署后的业务副本需人工核对：

- `Game/Module/Narrative/Editor/NovelContentToolsSetup.cs`：注册 UI/正文文案使用 SourcePath。
- `NarrativeE2AssetImport.cs`、`NarrativeE3Assets.cs`：导表不再 ImportAsset 旧目录。
- `NarrativeM2Assets.cs`：示例初始化通过 PathForTable/ReadText/WriteBytes 访问唯一源。
- `NarrativeSampleArtMigration.cs`：迁移检查从实际 Definition 源读取。
- 自定义图片、剧情导入脚本若使用旧 Source 或目录，按上文 API 示例调整。

图片导入、图片同步、剧情导入技能的维护源位于模板 Assets/Game/Documentation/TemplateSkills；
通过项目中心保存/Bump 及模板技能预览/更新分发，不手改 .agents 安装副本。

## 验收入口

- `Ember/配置表中心`：校验全部、预览全部变更、导出当前表、烘焙并生成全部。
- 多语言中心：common_ui_text、novel_ui_text、novel_content_text 保存、翻译审核、运行时预览。
- 图片资源管理：新增图片与外部草稿保存；外部文件变化后重载及草稿冲突。
- Unity Test Runner EditMode：`EmberExternalSourceTests`、`EmberTableEditorEditTests`、`EmberLocalizationSourceEditTests`。
- `python -B -m unittest discover -s Assets/Game/Documentation/TemplateSkills/ember-vn-import-images/scripts -p "test_*.py"`
- `python -B -m unittest discover -s Assets/Game/Documentation/TemplateSkills/ember-vn-sync-images/scripts -p "test_*.py"`

这些自动化用例不替代真实消费项目编译、人工 UI 操作、全新克隆与发行构建验收。实际结果另见本次交付记录。


2026-10-21 框架开发项目实际结果：Unity 6000.5.4f1，配表 65/65、多语言源表 5/5、Python 图片导入 17/17、同步 8/8。11 表和 3 个文案注册的外部源整链路及构建前回调通过，现有运行产物逐字节一致。真实消费编译、交互验收、全新克隆和发行构建仍待消费项目执行；本次能力随框架 v0.21.0 发布。

# 模板开发、父子同步与消费端升级

> 核对日期：2026-09-10；以当前工作区源码为准。
> 当前已发布框架为 `0.12.1`，父子模板兼容声明保持 `0.12.0`；major.minor 一致，因此两份模板继续兼容本补丁。schema v2、父子同步、场景合并、部署前 GUID 冲突预检与消费端完整模板部署已实现；消费项目安装与运行验收单独记录。

## 1. 两类升级

| 场景 | 当前能力 |
|---|---|
| 开发端：父模板 → 派生模板 | 完整物化模板、父基线、O/N/C 三方计划、冲突选择、场景语义合并和事务回滚 |
| 消费端：新模板 → 已有用户工程 | 已有兼容过滤、部署记录、同模板补缺、显式完整重新部署、GUID 预检与所有权标记；P-B 用户区合并向导尚未实现 |

父子同步的双方都是框架模板，不能直接用它覆盖消费项目中的用户文件和用户代码区。

## 2. 存储模型

```text
Packages/com.ember/Templates~/<id>/
  template.json
  Assets/                         完整可部署内容
  ParentSnapshot~/Assets/         派生模板上次同步的完整父内容
  Preview~/                      独立的真实截图和来源记录，不参与部署或内容 hash
```

每个模板最多一个直接父模板。谱系允许多级单继承，但拒绝循环、父缺失、重复 id；有子模板的父模板不能直接删除。
消费部署只需要模板自身的 Assets，不在运行时解析父链。

| metadata 字段 | 含义 |
|---|---|
| `schemaVersion` | 当前格式为 2；旧格式由显式初始化操作迁移，读取不落盘 |
| `id / displayName / description / order` | 模板身份与展示 |
| `version` | 模板内容版本，独立于框架版本 |
| `frameworkVersion / channel` | 兼容声明与 stable / preview / deprecated 通道 |
| `parentId / parentVersion / parentContentHash` | 上次成功同步的父身份、版本与树 hash |
| `contentHash` | 最近保存的模板完整 Assets 树 hash |
| `versionedContentHash` | 当前 version 已封存的内容 hash |

树 hash 包含 `.meta`，相对路径统一 `/` 并按 ordinal 排序，复用 Basic 的 MD5 工具汇总路径和文件内容。
已知文本格式在计算单文件 hash 前把 CRLF 归一化为 LF，避免 Git URL 包安装受消费端 `core.autocrlf` 影响；二进制文件仍按原始字节计算。包内 `.gitattributes` 同时要求 `Templates~` 保留原始字节，形成传输与运行时两层保护。
用途是变更检测，不是安全认证。资产文件与 `.meta`、目录 metadata、GUID 唯一性和 Windows 路径大小写冲突都参与校验。

当前模板仍为 `base 0.6.0 / stable` 与 `source3d-2p5d 0.3.1 / preview`，派生父基线为 `base 0.6.0`，内容 Hash、封存 Hash 与 ParentSnapshot 已校验一致；两份模板声明兼容框架 `0.12.0`，按 major.minor 闸门继续兼容 `0.12.1`。消费端验证仍需在实际项目执行。

## 3. 项目中心与正常开发

菜单 `Ember/项目中心`：embedded 开发环境显示“项目初始化 / 模板开发 / 项目校验”，消费环境显示“项目初始化 / 项目校验”。
`EmberTemplateEditorWindow` 仅是旧 API 兼容壳，不再有独立模板编辑器菜单。

1. 加载目标模板到项目。
2. 修改项目 Assets 中的业务内容，按项目规范完成编译与验收。
3. 在“模板开发”页保存，更新 `contentHash`。
4. 显式 Bump 主/次/补丁版本，将当前内容写入 `versionedContentHash`。
5. 父模板改变后，在派生模板预览并同步，再重新验收。

模板开发采用左侧模板树、右侧“概览与效果 / 内容差异 / 父级更新 / 版本与设置”。选中模板不会自动加载。
中间分隔条可拖动调整左栏宽度、双击恢复默认；当前 Unity 会话内记忆宽度，并为右侧保留最小操作空间。左栏长名称、ID 和状态自动换行，两侧独立滚动。
内容差异显式执行，只比较磁盘文件，复用保存时的目录和开发场景剥离规则；显示新增、修改、移除以及文本两侧内容。
保存前会提示保存场景并保存资源，再重新计算差异。新建/派生/另存为表单按需打开，手动版本与删除收纳到高级操作。

项目校验统一接收模板状态、资源/.meta/GUID、场景配置、代码管理区和 UI 生成链路结果。错误、警告和正常业务差异分开，支持筛选、定位和复制报告。
开发模式优先使用编辑记录；消费模式使用活动部署记录。部署记录不含历史文件 hash，因此只标注“与当前包模板参考比较”，不把当前模板当作部署时快照。
原“校验生成物一致性”独立菜单已移除，统一从项目中心的“项目校验”页操作；旧代码 API 仅保留兼容跳转。校验与预览不会修复或保存资源；内容完整扫描由用户触发，缓存过期后提示重新检查。

概览页支持捕获运行中的 Game 视图或导入真实截图，先保存草稿。效果记录包含模板 ID、模板/框架版本、内容 hash、项目文件指纹和图片 hash。
项目内容必须与截图来源一致，且模板已保存并封存，才能确认为版本效果；版本、内容或兼容声明变化后提示效果图待更新。
截图存放在 `Preview~`，独立于 Assets、ParentSnapshot 和模板 hash。当前项目演示入口会再次核对编辑模板身份；不会为了查看截图而加载模板。
本批新增校验和面板代码的 Unity 编译、EditMode 与图形交互验收尚未执行，不继承此前模板同步专项的通过状态。

普通保存不改变 version、versionedContentHash 或 frameworkVersion。
`contentHash != versionedContentHash` 表示存在未版本化内容，不能用相同版本号封存，也不能作为可同步的父版本。
根/独立模板可在封存后显式声明框架兼容版本；派生模板不能单独声明，只在父同步成功后继承。
新派生模板从 `0.1.0 / preview` 开始，同时复制父内容到自身 Assets 和 ParentSnapshot。

快照覆盖项目 Assets 下的 `Game`、`Resources`、`Ember/Editor`、`Settings`、`GameResource`。
`Assets/Art`、`Assets/ThirdParty`、`Assets/Editor` 和 ProjectSettings 不在覆盖范围。
例如 Project-wide Default Input Actions 不会随模板切换，需单独核对其资产引用。

禁止手工改模板快照、ParentSnapshot、hash 或运行 `sync-scaffold.ps1` 绕过面板流程。
磁盘与 metadata 不符时先核对真实差异；确需采纳磁盘变更的恢复操作要同时处理根/父基线/编辑记录，不能只改子模板 hash。

## 4. 编辑副本与部署记录

`Assets/Editor/EmberEditingTemplate.json` 保存 `templateId, templateVersion, contentHash, loadedAt`。
加载记录落后于模板存储时标为 `EditingCopyStale`，禁止旧项目副本反向覆盖新模板；需要重新加载。
当前编辑模板与保存目标不同也会阻止保存，复制工作使用明确的“另存为新模板”。

父同步在模板存储事务完成后，开发面板会在当前正编辑该派生模板时尝试重新加载项目副本。
面板应用同步前会检查当前编辑模板的项目改动和未保存场景/资源；存在改动时要求先保存，再重新预览同步。
重新加载失败会报告错误并要求手动恢复；它不是与模板存储提交合并的一个事务。
正在编辑其他模板时不会自动覆盖项目。执行同步前应按面板提示保存当前业务改动。

`EmberDeployedTemplates.json` 记录消费端 `activeTemplateId` 和历史部署 records。
每个模板都是独立可部署的完整内容，派生模板不是先部署父模板后再叠加的扩展。首次部署应直接选择最终模板；同模板可补缺，也可在覆盖警告后完整重新部署。完整部署当前或另一活动模板时，项目中心以文件事务替换模板覆盖的五个业务目录；事务中断自动原位回滚，非模板目录不受影响。部署写入前会检查模板 GUID 是否已被不会替换的项目资源占用，冲突时零写入中止。消费端不会将现有业务内容写入包内模板，模板创建、保存、加载、父同步与 metadata/版本修改 API 也只允许 embedded 框架项目调用。旧记录只有一条可在明确写操作中迁移；
多条旧记录没有 active 时要求选择，不在只读扫描中猜测或修改文件。

## 5. 父子三方分类

```text
O = 派生 ParentSnapshot~/Assets（旧父）
N = 当前父 Assets
C = 当前派生 Assets
```

| 差异 | 处理 |
|---|---|
| 仅父修改 / 父新增 / 父删除且子未改 | 采用父变化 |
| 仅子修改 / 子新增 / 子删除而父未改 | 保留子变化 |
| 父子结果相同 | 已一致 |
| 同路径不同新增、双方不同修改、删除对修改 | 冲突，选择保留派生或接受父版 |
| GUID、路径类型或 metadata 不合法 | 阻断或明确冲突，不静默猜测 |

普通资源与 `.meta` 是原子单元，接受、保留或删除都成对处理；目录 metadata 单独处理。
普通 C#、Prefab、asset 等并发修改不做自动文本 diff3。未解决冲突不能应用。

同步前检查：父存在且谱系无循环；N/C 的实时 hash 与 metadata 一致；父已版本化；O 与 parentContentHash 一致；
资产/meta/GUID 有效；相关编辑副本不过期。状态包括 Root、Synced、ParentChanged、ParentUnversioned、
TemplateContentDirty、EditingCopyStale、MetadataNotInitialized、ParentMissing、CycleDetected、SnapshotMissingOrCorrupted、InvalidAssetMetadata。

## 6. 场景语义合并

文件级计划保持纯分类。仅用户显式“查看差异并同步”时，`ComputeParentSyncPreviewPlan` 才对候选场景调用
`EmberTemplateSceneMergePlanner` 和 `EmberUnityYamlMerge`，普通状态树刷新不启动外部合并进程。

条件：`.unity` 的 O/N/C 均存在、三方 `.meta` GUID 相同、Force Text、输入有效、属于双方并发修改，且工具和规则可用。
参数映射固定为 O=base、N=theirs/left、C=mine/right。工具以无 GUI、无外部 fallback 的模式运行，带超时和输出校验。
不自研 Unity YAML AST，不把独立 `.prefab`、`.asset`、新增/删除场景纳入这条语义合并路径。

无冲突结果标记为 AutoMergeScene / SemanticMerge。工具缺失、超时、输出无效或真正冲突均回退文件级人工选择，
并显示原因。“保留派生/接受父版”作用于整个场景，不是某个对象。

预览记录 O/N/C、工具版本、规则和结果 hash。应用时在 stage 重跑合并，校验输入、工具/规则及结果 hash 与预览一致；
不复用预览临时文件。任一变化都拒绝应用，重新生成预览。

## 7. 事务与版本

同步先复制 C 到独立 stage，再应用选择/语义结果、校验资产与 hash、准备完整 N 快照与新 metadata。
提交前再次复核 O/N/C；正式 Assets、ParentSnapshot 和 template.json 通过备份/替换事务落位，异常时逆序恢复。
资产加载/部署期间禁止自动刷新，完整资产与 `.meta` 落盘后再同步导入，避免 Unity 提前生成错误 GUID。

有效内容改变必须同时选择派生版本 Bump；只推进父指针且最终内容不变时可以不 Bump。
成功后推进 parentVersion、parentContentHash 与 frameworkVersion。
面板在 Unity 编译或导入中禁用相关操作。

## 8. 消费端所有权与 P-B（待实现）

| 文件归属 | 标记与约定 |
|---|---|
| 全文件框架 | `// Generated by Ember Setup v...` 头标记；未来向导可预览整体刷新 |
| 混合文件 | `[EmberManaged:begin X]` / `[EmberManaged:end]` 配对；框架区与用户区分开 |
| 用户文件 | 无框架头标记，例如 `GamePages.User.cs`；不能自动覆盖 |
| `.Binding.cs` | 代码生成器管理，每次生成刷新；不交给模板文本合并器 |

Framework 页面 Lifecycle 块内是框架 override，块外固定六个基础 `XxxUser` 钩子。
UIUpdate、Popup 等可选钩子由 Binding 配置；移除含用户代码的可选钩子必须走生成器的保护流程。
GamePages.cs / GamePages.User.cs 为 partial 注册表，Page 写入注册，Item 不写入。

“补齐缺失”只新增文件和刷新受管头标记，不能声称已更新旧框架逻辑；“完整重新部署”会覆盖五个受管目录，只能在用户确认并备份业务修改后使用。
P-B 将需要差异预览、所有权检查、保留用户区、弃用迁移和场景策略。标记缺失/不匹配应留给人工处理，不能插入猜测区块。
模板版本语义：major 表示破坏性重构，minor 表示结构变化，patch 表示修复；这只是升级提示，不是覆盖安全证明。

兼容过滤仍以模板和框架 major.minor 相同为条件；preview 可见并提示实验性，deprecated 隐藏。
历史模板通过旧框架 tag 获取，不在当前包永久堆积所有版本。

## 9. API 与验收入口

核心实现位于 [Core.Editor](../../Packages/com.ember/Core/Editor/README.md)。`EmberProjectSetup` 提供：

- 扫描与兼容：GetTemplates、GetCompatibleTemplates、GetFrameworkVersion、ValidateTemplateGraph。
- 编辑：CreateTemplate、SaveEditingCopyAsNewTemplate、LoadTemplate、SaveTemplate、GetEditingTemplate。
- 版本：BumpTemplateVersion、SetTemplateVersion、DeclareFrameworkVersion、SetTemplateChannel。
- 同步：ComputeParentSyncPlan、ComputeParentSyncPreviewPlan、GetTemplateSyncStatus、ApplyParentSync。
- 部署：Initialize、DeployReplacingActiveTemplate、IsTemplateDeployed、GetActiveDeployedTemplate、HasAmbiguousDeploymentState、SetActiveDeployedTemplate。

测试源码在 `Packages/com.ember/Tests/EditMode`：TemplateMetadata、TemplateDeployment、TemplateSyncPlan、TemplateTransaction、
UnityYamlMerge 与 Integration 等测试类。

2026-09-07 专项验收已由用户确认通过：Unity 编译和相关 EditMode 测试通过；真实 UnityYAMLMerge 验证了不同场景对象的父子修改可同时保留、同一属性不同值会报告冲突、相同修改结果稳定；手工 O/N/C 验证中，父模板 GameBoot 修改与派生模板正交 2.5D 相机修改显示为场景语义自动合并，同一属性冲突安全回退为整场景“保留派生/接受父模板”。事务测试覆盖 stage 重跑、O/N/C 与结果 hash 复核、工具指纹变化、metadata/GUID 保持和失败零写入；同步当前编辑模板后的自动重载也已通过验证。非冲突合并完成后派生封存为 `source3d-2p5d 0.2.5`、父基线 `base 0.5.4`；随后根模板已前进到 `base 0.5.5`，尚未写入派生父指针。

[框架验收清单](framework-test-checklist.md) 包含父子同步、场景语义合并、回滚、UI 和消费端回归。
Preview 模板已创建，P-C 的 stable/deprecated 演练与新消费工程验收仍需记录；上述专项通过不替代 0.11.5 消费端运行验收。

# lane-defense-2d 实施记录

核对日期：2026-10-29（Asia/Shanghai）。执行依据：[F0～F7 执行文档](lane-defense-2d-template-execution.md)。

## 当前状态

2026-10-29 续接：用户追加授权仅对 base 执行 0.10.0 → 0.10.1 Bump。已重新核对仍只有先前 3 个新增文件，并通过项目中心 `BumpTemplateVersion("base", 2)` 完成封存。base 实时及封存 hash 均为 `e9d789311cad169a19b6a76331a2877a`。

随后通过中心 `CreateTemplate` 正式创建 `lane-defense-2d 0.1.0`（preview，701 个文件），父为 `base 0.10.1 / e9d789311cad169a19b6a76331a2877a`。新模板初始实时/封存 hash 均为上述父 hash；该初始状态尚不含塔防实现。

已排队调用项目中心原有 `LoadTemplateIntoProject` 流程，保留人工“加载模板”及后续技能同步/场景确认。当前等待用户处理弹窗，不重复提交加载、刷新或编译请求；未确认实际加载成功前不写入玩法 Assets。F0 隔离退出条件尚待确认，F1～F7 待实施。新模板自身 Bump、Git 提交、推送和发布仍未授权、未执行。

以下保留首轮 F0 现场与阻塞历史，后续结果以上方续接记录为准。

阻塞是中心要求父模板先封存，而本轮用户明确未授权 Bump。执行文档 F0 第 5 项要求：父基线不明确或中心不可用时停在此关口；不得复制 Templates~ 创建替代品。当前父基线内容已明确，但未封存，中心不允许使用它派生。

## F0 实际证据

| 项目 | 实际结果 |
| --- | --- |
| 工程 | embedded 开发仓库；Unity MCP 定向 `ember-unity-framework@b23e9b768e85ed30` |
| Unity | 6000.5.4f1 |
| 当前包 | com.ember 0.24.3；本轮未发布 |
| Git HEAD | b04da0de17617a1a38685b0d2aebb557b05405b7 |
| 正式编辑模板 | topdown-2d 0.1.0 |
| 编辑记录 hash | b3bf3acb890f00980fd6633f8765a93a |
| 编辑记录时间 | 2026-10-29 12:06:43 |
| 中心编辑副本比较 | 0 错误、0 警告、0 文件差异；ProjectFingerprint 与编辑记录一致 |
| 编辑副本过期检查 | false |
| 已加载场景 | Assets/Game/Scenes/FrameworkScene.unity；isDirty=false |
| 未保存资产 | 本次查询的已持久化 ScriptableObject 中无 dirty 资产 |
| 打开工具 | 项目中心、UPM 管理器；已记录序列化窗口状态，未发现打开的玩法草稿窗口 |
| 模板继承图 | 中心 ValidateTemplateGraph 返回空问题列表 |
| lane-defense-2d | 不存在；创建请求在父校验处拒绝，未产生模板目录 |

现有 Git 工作区在本轮新增记录前包含 1,218 个删除项、127 个修改项、3,392 个未跟踪项，暂存列表为空。这些属于进入任务时的现场，不视为本轮删除/修改。完整现场清单见 [git-status.txt](lane-defense-2d/evidence/F0/git-status.txt)。没有执行 reset/clean。

中心、hash、编辑副本比较和模板存在性原始结果见 [baseline.json](lane-defense-2d/evidence/F0/baseline.json)；场景、dirty 资产和窗口原始结果见 [editor-inspection.json](lane-defense-2d/evidence/F0/editor-inspection.json)。查询覆盖的是当前加载和持久化的对象，不宣称检查了所有外部文件或未打开工具的内存草稿。

## 父模板与创建阻塞

| 字段 | 实际值 |
| --- | --- |
| 父 ID / version | base / 0.10.0 |
| 父声明 frameworkVersion | 0.24.0（与当前包 0.24.3 分别记录） |
| 父实时 contentHash | e9d789311cad169a19b6a76331a2877a |
| 中心计算父磁盘 hash | e9d789311cad169a19b6a76331a2877a |
| 父 versionedContentHash | 61cbe49aa797b1604f293b9756674566 |
| 已核验旧基线副本 | topdown-2d/ParentSnapshot~/Assets；中心计算 hash 与上述封存 hash 一致，只读用于比较 |

从已核验旧基线到 base 实时内容仅新增以下 3 个文件，没有 C#、场景或 Prefab 差异：

- `Assets/Game/Config.meta`
- `Assets/Game/Config/EmberExtensionDefaults.json`
- `Assets/Game/Config/EmberExtensionDefaults.json.meta`

JSON 声明 schemaVersion=2、ownerTemplateId=base、ownerTemplateVersion=0.10.0；enabled 为空；仅记录 `ember.core` 的契约版本 1 及其内容指纹。这是既有未封存变更，本轮没有编辑它。

通过正式 `EmberProjectSetup.CreateTemplate("lane-defense-2d", ..., "base")` 实际请求创建，返回：

> 父模板 [base] 有未版本化内容，请先 bump 版本。

对应实现为 `Packages/com.ember/Core/Editor/EmberProjectSetup.cs` 的 `EnsureParentCanBeBranched`，比较 `contentHash` 与 `versionedContentHash`，不一致则拒绝。没有改写该保护、metadata 或 hash，也没有用旧 ParentSnapshot 复制创建新模板。

## 现场保护与恢复入口

备份目录：`Library/EmberBackups/lane-defense-2d-F0-20261029/`。该目录由 Library 管理，不随包或 Git 交付。

已复制 `Assets/Game`、`GameResource`、`Resources`、`Settings`、`Ember/Editor`、`Assets/Editor`、`.agents/skills` 和 `UserSettings`，并保留对应目录 .meta。目录内 1,111 个文件、12,356,227 字节逐文件核对 SHA-256，备份与源一致；[校验清单](lane-defense-2d/evidence/F0/backup-manifest.csv) 留在实施证据目录。

当前未切换，原现场可直接继续使用。日后切换后如需恢复，应先保护新现场，再经项目中心加载 topdown-2d，保留人工弹窗流程；若需恢复本机设置或未受模板管理内容，再依据本备份清单逐项处理，不能全目录覆盖其他会话后续工作。

## 下一步具体处理项（尚未授权、尚未执行）

解除阻塞的最小动作是追加授权仅对 **base 0.10.0 → 0.10.1** 执行项目中心 patch Bump，封存以上 3 文件差异。按当前中心实现，该操作将父 `versionedContentHash` 指向实时 hash `e9d789311cad169a19b6a76331a2877a`；执行后仍必须重新读取真实 metadata 核对，不能把预期写成结果。该操作不自动同步现有子模板，也不代表发布包。

获得该项授权后的续接顺序：

1. 重新检查 base/工作区指纹没有变化，通过中心 Bump base；若现场变动，先重新比较。
2. 通过中心从 base 创建 lane-defense-2d（中心初始版本规则为 0.1.0），核对父 ID/version/hash。
3. 目标切换为 lane-defense-2d，提前告知技能同步、场景重载或未保存场景弹窗；由用户人工确认，不能自动点击或绕过。
4. 完成 F1 契约冻结及 F2～F5 的 Assets 实现、EUI 中心制作；每批代码显式请求 Unity MCP 编译，确认本轮域重载后读取 Console。
5. F6 仅运行新增或直接相关测试，独立消费部署和画面验收分别记账；F7 在本轮既有授权范围内普通保存。
6. lane-defense-2d 自身 Bump、包版本调整、Git 提交/推送/tag/正式发布继续等待单独授权。

## 验证边界与交接状态

本轮执行了 Unity MCP 现场查询、中心只读编辑副本比较、模板图检查、中心创建拒绝验证和备份字节校验。读取到 Console error 条目为 0；这只是当前 Console 状态，不是本轮编译通过证据。一次内存查询因尝试直接访问 internal 校验类型而编译失败，随后通过反射调用同一只读比较入口成功；它没有写入项目 C# 文件或触发项目程序集编译。

本轮仅新增实施记录和证据，没有代码/场景/资源变更，无需请求 Unity 重编译；未执行玩法 EditMode/PlayMode 测试、正式启动试跑、画面/设备/性能验收或独立消费部署。

| 最终交接字段 | 当前状态 |
| --- | --- |
| 包版本、commit | 0.24.3；HEAD 见上；本轮未发布 |
| 新模板版本、实时/封存 hash、父基线 | 模板未创建，均不能填造；父候选见上 |
| 运行程序集、输入 Action、扩展接口签名 | 尚未实施 |
| 示例场景、配置与 UI 资源路径 | 尚未创建 |
| 消费部署步骤 | 新模板当前不能部署；需完成创建、实施、普通保存及验证后补齐正式说明 |
| 已验证范围 | F0 现场、hash、继承图、中心阻塞及备份 |
| 未完成项 | F0 隔离退出条件、F1～F7、消费端 T12 及其余验收矩阵 |

本记录保留执行关口；恢复工作时从父封存授权及实际状态核对开始，不重新进行方案讨论。

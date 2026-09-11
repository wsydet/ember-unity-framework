# UPM 包交付与维护

> 更新：2026-09-11。早期转包迁移已完成；本文维护当前交付流程。
> 当前发布版本为修复共享字体二进制损坏的 **0.12.3**；静态完整性、Unity 与消费项目验证分别记录。

## 目录与依赖

框架统一位于 `Packages/com.ember`。原来的多个 `com.ember.*` 包已经合并，程序集边界仍由各子目录 asmdef 保持。

| 位置 | 所有权 |
|---|---|
| `Packages/com.ember/{Basic,Core,Resource,Scene,Audio,Camera,Input,UI,UIExtension,SceneUI,...}` | 框架源码 |
| `Packages/com.ember/Templates~/<id>/Assets` | 完整模板快照，由模板开发页保存 |
| `Templates~/<id>/ParentSnapshot~/Assets` | 派生模板上次同步的父基线 |
| `Assets/Game` / `Assets/GameResource` | 当前加载模板的业务开发副本 |
| `Assets/Ember/Editor/SOs` | 项目可写的编辑器配置，仍是有效目录 |
| `Assets/Editor/EmberEditingTemplate.json` / `EmberDeployedTemplates.json` | 项目编辑状态与消费部署记录，不属于模板内容 |
| `Assets/Art` / `Assets/ThirdParty` | 开发素材与未转 UPM 的第三方内容 |
| `upm-stage` | 被 Git 忽略的私有依赖暂存区 |

框架依赖以 [package.json](../../Packages/com.ember/package.json) 为准；本项目的直接依赖、来源和版本见
[包清单](../user/package-inventory.md)。Odin/DOTween 等团队依赖位于独立的 `ember-thirdparty-upm` 仓库，
本次维护不替第三方包重写许可证，也不假设本地 tag 已推送远端。

Rainbow Folders、Rainbow Hierarchy、Console Pro、InputDeviceDetector、Feel 明确纳入第三方交付范围，统一使用私有 `ember-thirdparty-upm`。前四个包已与 embedded 副本逐文件核对一致；Feel v5.4 已准备 `com.moremountains.feel 5.4.0` 本地封装，保留 GUID、许可证和原始结构，当前工程尚未切换来源。MCP 不可用时不宣称迁移已通过，也不删除源插件；后续完成安装/编译/功能验收再清理。

## 消费项目首次安装

1. 取得 Odin Inspector、DOTween 以及对应仓库访问权限；当前 Runtime/Editor 仍引用这些程序集。
2. 在项目 manifest 配置 OpenUPM 的 `com.neuecc` scope，供 UniRx 解析。
3. 添加框架 Git URL：`https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.12.3`；需要完整开发环境时同时按包名合并随包 `Dependencies~/manifest-0.12.3.json`，不覆盖消费项目其他依赖。
4. 让 Unity Package Manager 完成解析与编译。不要再单独导入内置 UniTask。
5. 使用安装版本提供的项目初始化入口。当前开发版为 `Ember/项目中心 → 项目初始化`。
6. 首次部署选择兼容模板，核对 Build Settings、场景映射、UI 资源和输入配置，再执行 Play 验收。

Git URL 依赖在消费项目 manifest 中直接声明；框架 package.json 只声明可解析的包版本。
`unity: 6000.0` 是包声明的最低版本字段，不等于所有 Unity 6 版本均经过本项目测试。

## 模板开发

完整规则见 [模板体系](template-upgrade-system.md)。0.12.0 已通过项目中心封存 `base 0.6.0` 与 `source3d-2p5d 0.3.1`，派生父基线为 `base 0.6.0`，内容与 ParentSnapshot Hash 已校验；二者已通过根声明和父同步把兼容框架推进到 `0.12.0`。消费项目只能部署包内模板，模板写 API 仅允许 embedded 框架项目。

2026-09-07 经用户授权恢复保存失败后缺失的派生 `Assets`：备份 275 个文件的 contentHash 为 `81729c743e325157f56ff10dc28ac88d`，与 metadata/编辑记录一致；父快照 hash 为 `89f9cf6d5de00747eaf5dbf3f812a9d4`，与 parentContentHash 一致。恢复未改动版本、hash、父快照或项目业务副本；额外恢复副本在本地 `Library/EmberTemplateRecovery`，不进入发布。该恢复不代表原目录访问拒绝的原因已经消除，仍需验证下一次正常保存。

在“模板开发”页加载目标模板，在项目 Assets 中开发，保存后显式 Bump 封存版本。
不要手动复制模板 Assets、ParentSnapshot 或修改 hash，也不要运行旧的 `sync-scaffold.ps1` 代替面板保存。
`Assets/Art` 不属于快照范围；框架共享字体和精选资源放在 `SharedAssets`，通过 GUID 引用。

## 框架升级

**禁止直接修改项目的 manifest 文件来升级，所有的消费端升级都必须通过 `Ember/UPM Manager`。**
不得手改 `Packages/manifest.json` 的版本/Git URL 或 `Packages/packages-lock.json` 的版本/提交 hash 来升级。
该要求适用于所有 Ember 消费项目及代理操作；上面的首次安装说明不能作为已有项目升级的替代流程。
如果无法操作 UPM Manager，应由用户在 Unity 中执行升级，不能退回文件编辑。升级后可以只读核对声明、锁文件和实际包内容。
正式规则见 [UnityFarm 改动回流与升级规则 §7.1](../../Packages/com.ember/Documentation~/maintenance/unityfarm-change-routing.md#71-仅框架变化)。

`Ember/UPM Manager` 读取已安装版本和远程 tag，消费端选择目标后通过 Package Manager 安装。
当前工作区新增的 `EmberUPMUpgradeTracker` 记录校验、下载/解析、注册/编译、版本验证阶段，
通过 SessionState 跨脚本域重载续接。成功以安装结果版本核对为准，不把估算进度当成下载百分比。

Embedded 开发副本不能通过消费端升级按钮覆盖。常规升级不需要删除整个 lock 文件。
当前升级器只更新 `com.ember`，Odin/DOTween 只检测存在与提供安装按钮。随包 `Dependencies~/release-0.12.3.json` 和完整 manifest 基线继续使用第三方 `ember-v0.11.1`；消费端差异确认、权限检查和自动安装流程仍未实现。消费端需手动合并声明，不能复制开发机 `file:` 路径或覆盖其整份 manifest。
更新包与更新已部署模板是两个动作：同模板“补齐缺失”保留已有文件；“完整重新部署”会在确认后事务替换五个模板管理目录，用于显式应用已有文件修复；P-B 的用户区合并向导仍待实现。
不同活动模板不能走普通“补齐缺失”；项目中心提供“部署此模板”，只把包内目标完整模板事务部署到消费项目，不保存当前内容或修改包内模板。

## 发布维护

0.12.3 恢复两份被 Git CRLF→LF 转换损坏的共享字体，修正共享二进制 attributes，并加入完整性清单与 CI 检查。发布前运行 `python scripts/check-shared-binaries.py --revision : --checkout`，提交后及 tag 发布时使用 `--revision HEAD --checkout`。消费端解析后使用 `--revision v0.12.3 --package-root <实际解析的 com.ember 目录>` 只读核对；不能以 manifest/lock 已更新替代实际安装验证。详见 [0.12.3 发布说明](release-0.12.3.md)。Unity MCP 不可用，本次 Unity 编译与运行验收未完成。


0.12.2 修复 Table 脚本编码冲突，用户已确认框架项目测试通过；本会话未独立执行 MCP 验证。正式依赖声明位于 Dependencies~/release-0.12.2.json 和 manifest-0.12.2.json，UnityFarm 消费回归仍待完成。详见 [0.12.2 发布说明](release-0.12.2.md)。以下 0.12.1 功能记录保留为历史。

0.12.1 在 `Ember.Table.Editor` 增加声明/数据可视化、可复制查询代码和安全单表导出，不增加第三方依赖。正式发布声明与完整 manifest 位于 `Dependencies~/release-0.12.1.json` 和 `manifest-0.12.1.json`；本次会话无 Unity MCP，修复最后一条用户报告的编译错误后尚未独立取得 Unity 编译与新增 Editor 测试结果。

框架包版本为 **0.12.3**；根模板仍为 `base 0.6.0`，派生模板仍为 `source3d-2p5d 0.3.1` 且父基线对齐 `base 0.6.0`。模板声明保持 0.12.0，并按 major.minor 闸门兼容本补丁；第三方内容未变化，复用 `ember-v0.11.1`。发布信息与验证边界见 [0.12.3 发布说明](release-0.12.3.md)。

发布按 `package.json → CHANGELOG → release/manifest 声明 → commit → tag → push` 对齐版本。Unity MCP 不可用时不得用 BatchMode 或 dotnet 结果替代 Unity 编译结论，必须在发布说明中保留手动验证边界。

2026-09-07 已完成模板父子同步与 `.unity` 场景语义合并专项验收；这不替代新消费项目部署、P-C 生命周期和整体发布验收。

检查模板实时内容、metadata、versionedContentHash、父版本/快照和当前编辑记录一致；保存和版本封存是两个动作。
已发布 tag 应保持不可变；不要把模板 patch 升级解释成能够无条件覆盖用户改动。
发布历史见 [CHANGELOG](../../Packages/com.ember/CHANGELOG.md)，未完成事项见 [当前进度](framework-progress.md)。

## 回归重点

- 新消费项目首次部署 stable / preview 模板，页面、场景、输入、材质和字体引用有效；非受管资源占用模板 GUID 时应在零写入状态中止。
- 已有项目补齐同模板时保留业务代码；部署其他模板时只以包内目标模板完整替换受管目录。
- 受 0.11.3 GUID 冲突影响的 2.5D 项目升级后执行“完整重新部署”，确认 GameplayScene 使用模板 Input Actions 且 PlayerControl 输入映射有效。
- consumer 不显示模板开发页；embedded 的编辑副本不会串写另一模板。
- Package Manager 错误、域重载与版本不匹配可以被诊断，重复点击不会并发发起升级。
- 编译与测试遵循 [CLAUDE.md](../../CLAUDE.md) 的 MCP 规范，记录实际结果和未验证项。

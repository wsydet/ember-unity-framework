# UPM 包交付与维护

> 2026-09-26：框架 **0.14.13 / visual-novel 0.13.0** 发布退出封屏（`EmberQuitCurtain`）与多语言即时切换（`LanguageChanged` 广播 + 示例小说四语言），并封存开局演出/名字输入步骤与 7 个页面的安全区归属（封存 hash `0aa62a24f632c2f426a691f97002eb56`）。`NovelSessionTests` 179/179 通过；框架退出封屏用例与新增的 3 个存读档 PlayMode 用例本轮未跑。**必须先升级框架再吸收模板内容**（模板代码依赖 0.14.13 新增 API）。见 [0.14.13 发布说明](release-0.14.13.md)。

> 2026-09-24：框架 **0.14.10 / visual-novel 0.9.0** 发布剧情整数运算、可存档随机事件、动态文字绑定与逻辑导入审计。70 项相关回归和最终 19 项专项通过（部分重叠）；跨 minor 迁移须保护定制内容。见 [0.14.10 发布说明](release-0.14.10.md)。

> 2026-09-24：框架 **0.14.9 / visual-novel 0.8.0** 发布章节卡渐隐、可定制 UI、二级步骤和三项剧情技能更新。177 项相关回归通过；跨 minor 迁移须保护项目定制。见 [0.14.9 发布说明](release-0.14.9.md)。

> 2026-09-24：框架 **0.14.8 / visual-novel 0.7.5** 发布小说选择、试播布局和阅读 UI 修复。消费项目先升级框架，再预览模板补丁增量。见 [0.14.8 发布说明](release-0.14.8.md)。

> 2026-09-23：框架 **0.14.7** 新增模板冲突恢复技能，通用技能清单与 bundle 共 10 项；模板内容保持不变。见 [0.14.7 发布说明](release-0.14.7.md)。

> 2026-09-23：框架 **0.14.6 / visual-novel 0.7.3** 补充三项剧情技能的本地选择页面流程说明。升级框架后使用模板技能独立更新入口；页面端到端待验收。见 [0.14.6 发布说明](release-0.14.6.md)。

> 2026-09-23：框架发布至 **0.14.5**，提供模板补丁三方增量更新、部署原稿基线与可复制冲突日志。模板内容保持不变；155 项相关 EditMode 测试通过。详见 [0.14.5 发布说明](release-0.14.5.md)。

> 2026-09-23：已发布流程推进至 **0.14.3 / visual-novel 0.7.2**，修复消费项目布局菜单身份判断；验证及定制工程迁移步骤见 [0.14.3 发布说明](release-0.14.3.md)。以下旧版本段落保留历史语境。

> 更新：2026-09-17。早期转包迁移已完成；本文维护当前交付流程。
> 当前发布版本为增加 Unity MCP 安装入口的 **0.13.2**；静态检查、Unity 与消费项目验证分别记录。

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

0.13.0 起另提供 [AI Skill 独立更新](../../Packages/com.ember/Documentation~/maintenance/ai-skills.md)：在 UPM Manager 中单独检查和安装框架仓库的技能目录，不触发框架包或业务模板升级。技能发布与框架发布分开，但安装前必须满足技能所需的 API 能力。

1. 取得 Odin Inspector、DOTween 以及对应仓库访问权限；当前 Runtime/Editor 仍引用这些程序集。
2. 在项目 manifest 配置 OpenUPM 的 `com.neuecc` scope，供 UniRx 解析。
3. 添加框架 Git URL：`https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.13.2`；需要完整开发环境时同时按包名合并随包 `Dependencies~/manifest-0.13.2.json`，不覆盖消费项目其他依赖。
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

0.13.2：可选包列表新增 Unity MCP，使用官方 `CoplayDev/unity-mcp` 仓库的 `/MCPForUnity` 目录及既有依赖基线提交 `4ce7dd3cc54e37e2ed6dc59cb5a047f3dccb3f50`（10.1.2）。支持状态检测、按需安装及复制地址，既有安装不被覆盖；安装和连接配置分别处理，见 [Unity MCP 安装与连接](mcp-troubleshooting.md)。历史依赖声明保持不变；消费端通过 UPM Manager 升级框架到 0.13.2 后取得此入口。

0.12.11 增强可选包体检：安装状态依据 Unity 已注册包信息，显示实际版本与来源；另检测已加载插件类型及 Assets 中的插件脚本。
待编译的插件文件单独标识，不误报安装成功或允许重复导入。打开窗口、返回窗口、项目资源变化、包注册及安装完成时刷新，支持手动“刷新安装状态”。
未安装时“安装 v版本”调用 Client.Add 对应的 Git URL；安装前重新确认状态，由 Unity Package Manager 管理消费端 manifest/lock。
Rainbow Folders 使用 rainbow-folders-v2.4.5，Rainbow Hierarchy 使用 rainbow-hierarchy-v2.6.5，InputDeviceDetector 使用 inputdevicedetector-v1.0.0，Console Pro 使用 consolepro-v3.9.81；Feel 暂用 ember-v0.11.1。
这些标签已只读核对远端存在；历史发布 JSON 保持不可变。本轮未实际安装插件，Unity 编译和新增测试尚未验证。

**禁止直接修改项目的 manifest 文件来升级，所有的消费端升级都必须通过 `Ember/UPM Manager`。**
不得手改 `Packages/manifest.json` 的版本/Git URL 或 `Packages/packages-lock.json` 的版本/提交 hash 来升级。
该要求适用于所有 Ember 消费项目及代理操作；上面的首次安装说明不能作为已有项目升级的替代流程。
如果无法操作 UPM Manager，应由用户在 Unity 中执行升级，不能退回文件编辑。升级后可以只读核对声明、锁文件和实际包内容。
正式规则见 [UnityFarm 改动回流与升级规则 §7.1](../../Packages/com.ember/Documentation~/maintenance/unityfarm-change-routing.md#71-仅框架变化)。

`Ember/UPM Manager` 读取已安装版本和远程 tag，消费端选择目标后通过 Package Manager 安装。
0.12.10 已增加“更新内容”功能：检测到可升级版本后，最新版本默认展开说明，其他版本可展开查看。
通过已有 Git 凭据对最新固定标签做 bare partial clone，仅读取包内 CHANGELOG 并按版本匹配，不检出 Assets。
读取异步执行，60 秒超时；同一窗口复用已取得的说明，失败可重试或打开该版本日志原文，缺少说明不阻挡升级。
后台结果只在 Layout 阶段应用，关闭窗口取消读取；临时 Git 数据位于 Library/EmberUPMReleaseNotes，按请求清理。
5 个新增离线 EditMode 回归包含真实 IMGUI 重绘；已验证远端标签的 Git 文本读取，Unity 编译与测试尚未执行。
当前工作区新增的 `EmberUPMUpgradeTracker` 记录校验、下载/解析、注册/编译、版本验证阶段，
通过 SessionState 跨脚本域重载续接。成功以安装结果版本核对为准，不把估算进度当成下载百分比。

Embedded 开发副本不能通过消费端升级按钮覆盖。常规升级不需要删除整个 lock 文件。
框架升级只更新 `com.ember`；Odin/DOTween 标为框架必需，可选第三方包单独按需安装。随包 `Dependencies~/release-0.13.2.json` 的 `optionalPackageInstallTargets` 记录安装按钮的独立标签；完整 manifest 基线继续使用第三方 `ember-v0.11.1`。依据整份依赖声明自动同步仍未实现，不能复制开发机 `file:` 路径或覆盖消费端整份 manifest。
更新包与更新已部署模板是两个动作：0.14.5 起，同模板 major.minor 内的 patch 升级走三方增量；“补齐缺失”仅修复相同版本/hash 的缺失文件，不推进版本。前两位变化时先保护本地内容，再完整部署五个受管目录并恢复；自动用户代码区合并仍待实现。
不同活动模板不能走普通“补齐缺失”；项目中心提供“部署此模板”，只把包内目标完整模板事务部署到消费项目，不保存当前内容或修改包内模板。

## 发布维护

0.13.2 在 UPM Manager 的可选包列表增加 Unity MCP，支持状态检测、官方固定提交安装与复制地址；安装后另行配置连接。静态检查通过；Unity 编译、新增 EditMode 回归和消费端安装尚未验证。见 [0.13.2 发布说明](release-0.13.2.md)。

0.13.1 在原有 EUI 技能之外新增 8 个可安装技能，共 9 个；补齐独立参考资料、项目规则适配和依赖包只读保护。文档审计脚本隔离回归通过；Unity 面板安装尚未实测。见 [0.13.1 发布说明](release-0.13.1.md)。

0.13.0 增加 UPM Manager 独立 AI Skill 更新、框架维护的 EUI 技能及公开重新生成 API，并修复 EUIBinding 的 Player 编辑器引用。静态检查通过；Unity 编译、EditMode、实际技能安装与 UnityFarm 打包仍待验证。见 [0.13.0 发布说明](release-0.13.0.md)。

0.12.11 增强可选第三方包安装状态，显示实际版本与来源，兼容直接导入插件；提供固定版本安装、刷新与复制地址按钮，安装前重查以防重复安装。安装标签与静态检查已核对；Unity 编译、8 个新增 EditMode 案例与实际安装待验证。见 [0.12.11 发布说明](release-0.12.11.md)。

0.12.10 为每个可升级版本增加可展开的更新内容，默认展开最新版本，异步读取固定标签发布日志并支持失败重试。远端中文日志传输已验证；Unity 编译、5 个新增 EditMode 案例与消费交互尚未验证。详见 [0.12.10 发布说明](release-0.12.10.md)。

0.12.9 修复快速打开场景刷新后依赖 SO Inspector 的问题，直接按资产路径打开并保留刷新选择。Unity MCP 不可用，编译、4 个新增 EditMode 案例与消费交互待验证。详见 [0.12.9 发布说明](release-0.12.9.md)。

0.12.8 补充 Noto Sans Symbols 2 共享后备字体，发布原版 TTF、OFL 许可证、动态 SDF 与回归检查。用户确认编译无报错；新增 PlayMode 与消费渲染待验收。详见 [0.12.8 发布说明](release-0.12.8.md)。

0.12.7 交付引导步骤编辑器和按模块 Enabled 控制的顶部菜单，适配 EntityId 资产打开回调，并修复 UI 开发中心在保存资源后的布局状态错误。详见 [0.12.7 发布说明](release-0.12.7.md)。

0.12.6 增加 EUI 中文用途、维护列表用途显示和基础 UI 删除保护，并交付两个已封存模板；4 个新增保护回归案例未执行，详见 [0.12.6 发布说明](release-0.12.6.md)。

0.12.5 增加 UPM 检查更新的循环进度、计时、取消与 60 秒超时，并通过非阻塞 Git 查询避免冻结编辑器。同时增加可选包体检并修复旧完成提示抹掉新查询结果。6 个新增 EditMode 测试及交互验收尚未执行，见 [0.12.5 发布说明](release-0.12.5.md)。

0.12.4 启用共享钉钉 SDF 多图集。发布前运行 `python scripts/check-shared-font-config.py --revision :` 和既有二进制检查，提交后将 revision 改为 HEAD。容量与材质/SceneUI 回归在 Unity Test Runner 的 PlayMode 执行 `SharedFontAtlasPlayModeTests`，不能把开关检查当作实际分配第二张图集的结果。详见 [0.12.4 发布说明](release-0.12.4.md)。

0.12.3 恢复两份被 Git CRLF→LF 转换损坏的共享字体，修正共享二进制 attributes，并加入完整性清单与 CI 检查。发布前运行 `python scripts/check-shared-binaries.py --revision : --checkout`，提交后及 tag 发布时使用 `--revision HEAD --checkout`。消费端解析后使用 `--revision v0.12.3 --package-root <实际解析的 com.ember 目录>` 只读核对；不能以 manifest/lock 已更新替代实际安装验证。详见 [0.12.3 发布说明](release-0.12.3.md)。Unity MCP 不可用，本次 Unity 编译与运行验收未完成。


0.12.2 修复 Table 脚本编码冲突，用户已确认框架项目测试通过；本会话未独立执行 MCP 验证。正式依赖声明位于 Dependencies~/release-0.12.2.json 和 manifest-0.12.2.json，UnityFarm 消费回归仍待完成。详见 [0.12.2 发布说明](release-0.12.2.md)。以下 0.12.1 功能记录保留为历史。

0.12.1 在 `Ember.Table.Editor` 增加声明/数据可视化、可复制查询代码和安全单表导出，不增加第三方依赖。正式发布声明与完整 manifest 位于 `Dependencies~/release-0.12.1.json` 和 `manifest-0.12.1.json`；本次会话无 Unity MCP，修复最后一条用户报告的编译错误后尚未独立取得 Unity 编译与新增 Editor 测试结果。

框架包版本为 **0.13.2**；根模板为 `base 0.6.3`，派生模板为 `source3d-2p5d 0.3.5` 且父基线对齐 `base 0.6.3`。用户已通过模板面板保存和 Bump，内容、封存 Hash 与父快照一致。模板兼容声明推进到 0.13.0，内容版本、Hash 和父快照不变；第三方包内容未变化，完整清单复用 `ember-v0.11.1`，可选安装按钮使用上述逐包标签。发布信息与验证边界见 [0.13.2 发布说明](release-0.13.2.md)。 两个模板的 GuideModule 均保持 Enabled=false。

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

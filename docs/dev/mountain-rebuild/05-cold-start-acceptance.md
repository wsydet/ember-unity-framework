# 冷启动组合续玩与发行 Player 验收

本轮范围为 platformer-2d 公共验收夹具。未修改 Windward/Mountain、框架运行时或 base 模块。前轮单次 Play 的 42 条断言保留在 [两关验收](04-two-level-acceptance.md)，本轮单独验证退出 Play 与系统进程退出。

## 实际入口

- `Assets/Game/State/Development/PlayerColdAcceptanceRunner.cs`：共用的保存/续玩运行流程。
- `Assets/Game/State/Editor/PlayerColdAcceptance.cs`：生成 ColdBoot、启动编辑器验收及构建。
- `Assets/Game/Development/LevelAcceptance/ColdBoot.unity`：FrameworkScene 的独立验收副本，引用已有 ConfigA/B；未改正式 FrameworkScene 或默认发行列表。
- 示例进度参与者现在在 `UNITY_EDITOR || EMBER_PLAYER_ACCEPTANCE` 下编译。构建通过 `BuildPlayerOptions.extraScriptingDefines` 单次传入标记，不写 PlayerSettings。

保存阶段：正式 Main→A→B；A 卸载并注销相机；B 稳定落地后捕获检查点与平台 `(12,3)`，随后把玩家和平台移离基线并注入空中外力；用最新示例项目数据与合法基线组合保存，成功后返回 Main、卸载 B、停止存档服务并退出。

续玩阶段：新启动不得持有玩家会话或 Pending；从同一隔离库的槽位 0 读取组合记录，经目标场景 Prepare 恢复世界、项目数据和基线，再启动 B 玩家。验证检查点而非空中位置，再验证死亡恢复只提交一次及最终卸载。加载/续玩/死亡恢复均检查相机绑定、历史清除后的初始位置、Brain 实际输出，并生成渲染截图。

`expected.json` 只保存验收预期值和前次启动标识，不参与业务恢复；实际玩家、世界及项目数据都从 PlayerData 组合存档读取。所有验证操作调用正式 API，未模拟 UI 点击。

## 编辑器两次 Play

2026-10-17 14:30:54 保存阶段 16/16；14:31:16 续玩阶段 19/19。两次报告 errors 均为空，均自动退出 Play；第二次启动明确在 `isPlaying == false` 后重新发起。当前项目关闭 Domain Reload（原设置保持），因此覆盖现有设置下的清理与重新装配，不冒充两个系统进程。

- 隔离根：`Library/PlayerColdAcceptance/c9323a6a56314027b144d4876bcdce2c`。
- 前次 boot：`ea962fbef46049ad9f3852b7caad352f`；后次：`abe21bad6d4d460e8a5f9bfe02e1d14a`。
- `report-save.json` / `report-resume.json`、4 张截图保存在上述目录；已查看续玩截图，玩家、地面和正式 UI 正常显示。
- 此计数是夹具运行断言，不是 Unity Test Runner 的测试数量。

## 构建过程发现

1. Unity 6 拒绝 Library 内的 Player 输出。改用仓库忽略目录 `.utmp`，构建入口增加路径检查及空 BuildReport 提示。
2. 首次生成的非 Development Windows64 Mono Player（GUID `a895f2f9dc864b2b8db126bc46edd148`）完成了两阶段业务断言，但 Windows 截图写入报 `Failed to capture screen shot`，两次退出码均为 1，不能计为通过。夹具改为帧末捕获 Texture2D、显式 PNG 写盘并释放纹理；旧失败证据保留在 `.utmp/windward-public-20261017/player-build/evidence`。
3. 首次 BuildReport 为 Succeeded、1 error、18 warnings。错误来自 Odin 构建预处理器：以 `Library/PackageCache/.../NoEmitAndNoEditor/Sirenix.Serialization.dll` 查找 PluginImporter 失败。MCP 验证规范 `Packages/com.sirenix.odin-inspector/...` 路径可取得 PluginImporter，而缓存路径返回 null。未绕过插件回调，未改 PackageCache，未把此第三方问题提升为 Ember Manager。完整消息在首次输出目录 `build-messages.txt`。警告包括 UniRx 过时 API、未使用的序列化字段、UI 扩展 shader 和 Feel 可选 PostProcessing 资源缺脚本。

## 复跑

编辑器使用 `Game.State.Editor.PlayerColdAcceptance.CreateBoot()`（已有资产不覆盖），然后 `Launch(绝对隔离根, "save")`；报告生成且退出 Play 后，用同根 `Launch(..., "resume")`。新保存测试使用新目录；恢复必须使用前次目录。

发行调用 `Build(绝对输出目录)`，默认 Windows64、当前 Mono 后端、BuildOptions.None，包含 ColdBoot/Main/Settings/A/B。生成 Player 以 `--ember-acceptance-root "绝对隔离根" --ember-acceptance-phase save` 启动；该进程退出后，再以同根和 `resume` 启动新进程。不要用 batchmode/nographics 代替渲染验证。验收库必须隔离，不能使用用户真实存档目录。

普通产品构建不得包含 ColdBoot/A/B 验收场景或 `EMBER_PLAYER_ACCEPTANCE` 标记。产品自行提供组合数据类型、恢复准备参与者及 UI 接线；示例 Pending 不作为产品全局状态方案。

第二次构建 GUID `5b67f20578984623ada240e0203b95a0`，报告 Succeeded、1 error（同一 Odin 导入器问题）、0 warnings；增量构建的零警告不代表首次警告已修复。第二次保存运行在 A 相机检查后因 `CaptureScreenshotAsTexture` 返回 null 失败，未作为通过证据。该 Player 为隐藏窗口，改用实际 Brain 驱动的主相机 RenderTexture 输出留证，并恢复原 targetTexture/active RT、释放临时纹理。此画面是离屏主相机渲染，不声称覆盖屏幕 Overlay UI；UI 仍由正式 EUI 打开状态断言验证。

## 最终发行运行证据

最终构建输出 `.utmp/windward-public-20261017/player-build-v3`；Unity MCP读取BuildReport：Succeeded、1 error、0 warnings，GUID `203d249dd3784155b4913fb87a08c13d`，BuildOptions.None。错误仍是上述Odin预处理器问题，没有删除/屏蔽回调；因此只确认该生成Player的运行验收通过，不声称发行构建零错误或插件构建兼容问题已修复。

| 阶段 | 时间 | 进程ID | boot | 断言 | 退出码 |
|---|---|---:|---|---:|---:|
| save | 2026-10-17 14:42:26 | 52592 | `64ed5a2508074156ad246b52276344bb` | 16/16 | 0 |
| resume | 2026-10-17 14:43:00 | 274576 | `ad98c21d285349a8b20e5beb86a718cd` | 19/19 | 0 |

两份报告 `editor=false`、`development=false`、`errors=[]`，共享同一构建GUID和隔离库。第二个进程在第一个退出后启动。实际运行使用窗口Player和Direct3D12，没有batchmode/nographics。报告与4张主相机PNG位于输出下`evidence/`。

SHA256：保存报告 `b0225c2117045db969e659e685e16219086ae9cb7a1ffd9c2f5d4d48c53451fd`；续玩报告 `67c6d563622a6d1717677d453a4ddf7fba0af754e75b1e57ae33bce816d3d27a`。4张图均为 `1b7d59051bcae3b849c2f9190ad04d7c4d13935afe2a176b67556a648c9b86e3`：两张最小场景外观一致且镜头跟随玩家，构图相同属预期；图片不能独立证明换关，场景对象销毁/注册检查负责验证。已查看续玩图，玩家与地面可见。

发行运行全链路没有发现需要修改框架公共API或base维护源的问题。本轮修正均为platformer-2d验收夹具；Odin插件构建预处理错误作为独立发行限制保留，不写入包内Manager。Unity构建产生的唯一ProjectSettings batching默认序列化已按构建前备份精确恢复，发行场景列表始终未改。

最终源码（含离屏捕获修正）再次执行编辑器两次Play：14:43:13保存16/16，14:43:38续玩19/19，errors均为空；根目录`Library/PlayerColdAcceptance/final-a747d277a1964e17825e668ac415f523`。boot分别为`b0953c4b99c848c0a663245cdeaaefa1`、`81a00c967bf346c0bc1cb3c12587ecb1`。未重复宣称历史564项回归是本轮重新执行。

## 保存前公共校验修正

项目中心实际扫描出现三个“未关联场景”警告；MCP读取映射证实 Gameplay/Main/Settings 的 `SceneName` 缓存均为空，但 `EditorScenePath` 分别正确指向正式场景。`EmberSceneField._sceneName` 没有序列化，域重载后校验不能依赖该缓存。框架Editor维护源 `Packages/com.ember/Core/Editor/EmberProjectValidationService.cs` 的场景检查改为优先使用现有EditorScenePath提取名称，再回退代码构造名称；未改运行时SceneField API或场景资源。修改后Unity实际扫描由0错误/4警告降到0错误/1警告，三个误报消失，剩余警告是有意保留的“已保存但未版本封存”。这是本轮新增实证支持的框架编辑器修正，前述“无需修改框架运行时”结论不变。

构建还自动更新了PC_RPAsset的13项URP预筛选缓存。已逐项核对仅这些字段变化，用Unity SerializedObject恢复对应原值；最终SHA256与保存基准一致`7cfa894e760f2aa087d24b2fa260e2efa497b58b89d52e4c2b28371cf6336aaf`，未整包复制/重新部署。ProjectSettings与构建前备份逐字节一致，原有UnityConnectSettings等修改保持。

## 2026-10-17 冷启动交付与最新普通保存

冷启动与发行运行的完整证据、失败尝试、调用方法及限制见[冷启动验收](05-cold-start-acceptance.md)。最终源码在编辑器两次Play通过16+19项断言；Windows64 Mono非Development Player在两个不同进程中也通过16+19项，退出码均0，运行错误列表为空。T05退出Play/进程后组合续玩和T06发行Player加载/恢复/卸载已补齐。T01—T04/T07沿用前轮结果，本轮未重复完整回归；T08未启动。

发现并修正框架Editor校验器依赖非序列化场景名称缓存的误报，维护源为`Packages/com.ember/Core/Editor/EmberProjectValidationService.cs`；没有修改运行时API。Unity实际扫描确认三个误报消失，项目校验EditMode回归14/14通过，job `e98fc546d17848d4b6fcd4188b05d44c`，0失败/跳过，0.214秒。模板夹具维护源为新ColdAcceptance Editor入口、运行器、ColdBoot及示例参与者编译条件；真实调用方法见模板LevelAcceptance.md。

14:47:21通过项目中心实际`SaveTemplate`普通保存1395文件，沿用原模板名称和说明。保存前0错误/1警告/9差异；保存后0错误/1警告/0差异。唯一警告是已保存未版本封存，按授权保持，不Bump。最终Console零错误/警告，FrameworkScene未脏且不在Play。

- 实际框架0.19.2；platformer-2d 0.4.1；模板frameworkVersion声明0.19.0；父base 0.8.3，均未变。
- 最新contentHash `db6747ebceedde8a4b1966a4ef00dfe6`，编辑记录一致。
- versionedContentHash仍为`373437cccbe9f4ba6e93359da2c8b2fd`，parentContentHash仍为`1270ce6dfcae3c01d050c09ecfd60337`。
- 只完成普通保存。未Bump、提交、推送、发布、消费升级或创建Windward。框架Editor修正仍为本地源码修改，未发布。消费项目不会自动获得这些内容，须后续另行授权正常版本升级与模板接入。
- 发行构建仍存在Odin构建预处理器PluginImporter路径错误；最终BuildReport为Succeeded但1 error。该生成Player运行验收通过，不等于零错误发行构建。IL2CPP、其他平台、Windward产品关卡/玩法及组合存档UI按钮接线仍未验收。

## Odin 构建问题后续状态（2026-10-17）

此前记录的PluginImporter路径错误已在正确第三方维护源修正，原回调保留并实际执行。通过UPM安装到PackageCache后重建Windows64 Mono非Development Player，BuildReport为Succeeded、0错误、20警告（18条既有内容警告和2条构建期间MCP重连警告）；两个新进程保存16/16、续玩19/19，退出码均0。详见[Odin修正与新证据](06-odin-build-path-fix.md)。历史失败记录保留，但不再作为当前未解决限制。

修正属于`ember-thirdparty-upm/com.sirenix.odin-inspector`，不是Ember运行时或模板能力。原远端tag未变化，尚未发布；工程的本机依赖接线与模板保存状态分别记录在上述文档。

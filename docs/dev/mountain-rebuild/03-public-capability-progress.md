# Windward 公共能力实施记录

日期：2026-10-17。范围为 Ember/platformer-2d 公共能力；未创建Windward、未修改只读Mountain，未收入旗面、风合成表、蒲公英倍率、贴纸或关卡业务。

## 当前出口

T01/T07已完成本轮实测修正；T02/T03及T04/T05公共接合代码已实现并通过Unity测试。完整回归564/564通过；随后补充恢复组件禁用保护、区域恢复刷新等收尾改动，专项108/108与29/29通过。2026-10-17 13:51通过项目中心正式SaveTemplate普通保存1371个文件。后续已完成T05公共两关正式链路及T06加载/恢复/卸载实测，42条运行断言通过，详见[两关实测证据](04-two-level-acceptance.md)。产品镜头装配和Windward关卡验收仍独立进行。T08未启动。

普通保存已获授权；Bump、Git提交、推送、发布及消费升级均未执行。新项目应以实际保存结果和下面的接线边界判断依赖，不以模板版本字符串单独判断新内容是否存在。

## 版本与保护

| 项目 | 核对结果 |
| --- | --- |
| HEAD | 79fe3889b744361b5481fb9a90a50a7531c5baa6 |
| 框架/Unity | com.ember 0.19.2 / 6000.5.4f1 |
| 正式活动模板 | platformer-2d 0.4.1 |
| 父模板 | base 0.8.3，parentContentHash=1270ce6dfcae3c01d050c09ecfd60337 |
| 开工保存基线 | contentHash=versionedContentHash=373437cccbe9f4ba6e93359da2c8b2fd |
| 模板frameworkVersion声明 | 0.19.0；不是当前实际包版本，普通保存不自行修改该声明 |
| 初始文件对照 | 已保存快照1317个文件在活动五目录中存在且SHA256一致；不是对额外文件的反向检查 |
| 工作区 | 大量既有修改/删除/未跟踪内容保留；备份在忽略目录.utmp/windward-public-20261017 |

代码归platformer-2d模板。未证实框架公共API缺陷，未修改框架运行代码或base PlayerData/SceneFlow维护源。Packages中原有字体等改动不归本任务；不清理、回退或重新部署工作区。

## T任务与实际API

| 任务 | 实际实现与证据边界 |
| --- | --- |
| T01 | PlayerExternalMotionRequest/Lease、ExternalMotion、单玩家交互锁及观察指标；真实速度抵消缺陷已复现并修正。局部锁仅稳定地面，冻结该玩家刚体，不改Time.timeScale |
| T02 | EntityTranslationSupport2D/PlayerSupportMotion；支撑与环境分开，跳离继承、瞬移失效、禁用、下穿、升降、风叠加、不同dt与顶棚限速均有物理用例。只支持平移，宿主须遵守发令顺序 |
| T03 | LaunchRequest2D/ILaunchTarget2D、玩家与平移载具适配、LaunchZone2D冷却去重；初速度、限时上升、结束策略、外部来源替换已测。内部互斥动作返回Busy，产品挥旗FSM实际接线由Windward验收 |
| T04 | WorldResetScope2D/WorldResetParticipant2D、PlayerCheckpointRecord/Recovery/Zone、危险区与平移平台复位示例；失败保留暂停、诊断和可重试路径。默认位置解析接口仍只读 |
| T05入口 | PlayerLevelDefinition、PlayerEntryRequest.ForLevel冻结方案、GameGameplayState.EnterLevel同步目标/重入窗口；跨场景候选不覆盖旧玩家。已验证冻结候选跨旧模块退出交接，并用A/B场景通过正式加载和UI全链路；产品装配仍须消费验证 |
| T05组合 | CheckpointProgress<T>/Codec<T>/CheckpointSaveSession<T>；单条记录组合合法基线与最新项目数据；写入冻结、旧轮次drain、保存失败保留现场/成功离场已测。同场景提供RestoreAsync；跨关项目业务由SceneScope参与者准备提交 |
| T05区域 | EntityRegion2D/PlayerRegionTracker2D；独立RegionId与优先级，重叠/禁用/传送选区测试；不包含安全区和计时含义 |
| T06 | 复用既有PlayerCameraParticipant、注册器、Cinemachine3 Follow；本轮不改相机Manager。现有相机准备/交接/恢复/释放五项测试包含在完整回归；新增A/B正式链路已测加载、续玩/死亡恢复、Brain输出和卸载；产品区域镜头和旗面UI覆盖需接线验收 |
| T07 | CollapsingPlatform2D；秒数、提前离开、占位恢复、暂停、复位、表现分离及组件销毁保护；真实探头用例已执行 |
| T08 | 未启动：没有调参后仍不能支持原关卡解法的实测证据 |

接口和真实调用示例：

- [外部运动/局部锁/塌陷平台](../../../Assets/Game/Documentation/platformer-2d/ExternalMotion.md)
- [支撑/弹射/检查点/关卡/组合保存/区域](../../../Assets/Game/Documentation/platformer-2d/CheckpointIntegration.md)

## 修改位置

新增主要维护源：

- Entity：CollapsingPlatform2D、EntityTranslationSupport2D、LaunchRequest2D、LaunchZone2D、TranslationSupportLaunchTarget2D、WorldResetScope2D、TranslationSupportReset2D、EntityRegion2D。
- Player：PlayerExternalMotion、PlayerConstantVelocitySource、PlayerSupportMotion、PlayerLaunchTarget2D、PlayerCheckpointRecovery、PlayerCheckpointZone2D、PlayerRecoveryHazard2D、PlayerLevelDefinition、CheckpointProgress、CheckpointSaveSession、PlayerRegionTracker2D。
- Tests：PlayerExternalMotionTests、CollapsingPlatformTests、PlayerPublicPhysicsTests、PlayerSupportTests、PlayerLaunchTests、CheckpointIntegrationTests。

既有PlayerContext/Controller/InputAdapter/RuntimeCandidate/LifeSession/PlatformerSnapshot接入生命周期和合法性；原移动/跳跃/冲刺/攻击/翻滚读取自身速度，观察面板记录来源；PlayerSessionContracts/Module/SceneParticipant与GameGameplayState接入冻结关卡和检查点。没有重建玩家图、方案、场景或已有GUID。

## Unity验证记录

MCP实例：ember-unity-framework@b23e9b768e85ed30，工程路径正确。前轮refresh_unity等待就绪60秒超时；本轮重新读取editor/state和Console成功，Console无编译错误，确认就绪超时不能直接当作连接不可用。

| 作业 | 结果 |
| --- | --- |
| 96b433af10764bb488c9086936a53ac3 | 初始完整回归539/539通过，0失败/跳过，807.628秒 |
| 10761d631d8e48f0af20c946c8b5e25c | 新测试首次匹配0项，不计为通过证据；补完整资源导入后重跑 |
| 39f3d6ade5584379b4b6275b262b4389 | 3项中速度抵消用例失败：期望自身-5，实际0；构成T01修正依据 |
| 0af795d991bb46eda8d2e2cda07300df | 修正后T01/T07共13/13通过 |
| dc6653f724024c11bc2bc6c9d02ca489 | 18项中旧版测试Stop后读Context失败；修正测试生命周期并重新导入 |
| 93abafb76a6b480fa4c578e53e3ceb01 | 来源/支撑/塌陷20/20通过 |
| ce646aa8ea9e48c6b19e1fbb158bf570 | 10项中顶棚挤压用例失败；增加乘员净空限制，未放宽断言 |
| 97a05be08b7640449c64ad65707e9b6f | 修正后联合23/23通过 |
| 915a2cab023243f09f44d3043d83003a | 加入弹射后联合28/28通过 |
| b3adc5280fac4bf2928178a2cee74183 | 检查点/组合保存/跨场景候选4/4通过 |
| 1e359f29eac6452b9f556014339775a2 | 完整回归564/564通过，0失败/跳过，846.479秒（收尾保护改动前） |
| bd364a6578f64acf9b12bc99f1ee2dab | 收尾后会话/检查点/保存退出/相机生命周期108/108通过，36.103秒；含新增恢复失败后禁用组件用例 |
| b0d11abca7b14199939dff97d83544b8 | 收尾后外力/承载/弹射/区域/塌陷专项29/29通过，0.498秒 |

测试使用真实Unity物理、正式图和虚拟输入，存档用例使用实际PlayerData服务及临时目录。没有使用BatchMode、dotnet或日志轮询替代。后续两关正式链路及默认UI已由42条运行断言验证，详见[证据](04-two-level-acceptance.md)。未验证：Windward产品玩法、完整关卡原解法、组合存档按钮点击接线、人工手感及消费项目部署。公共Windows64 Player冷启动验证见[新增证据](05-cold-start-acceptance.md)，Odin构建预处理错误仍保留。

## 普通保存与消费

已通过Unity MCP调用项目中心实际入口 `Ember.Core.Editor.EmberProjectSetup.SaveTemplate`，沿用原名称/说明。保存前中心比较为0错误、0警告、74项差异；FrameworkScene无未保存场景改动；先执行AssetDatabase.SaveAssets，再由中心完成暂存、.meta/GUID校验和事务保存，返回1371文件。未直接编辑快照、父快照或hash。

- 保存时间：2026-10-17 13:51:28。
- contentHash：`b427848a53546ff5e88fcac8c2cae370`。
- 编辑记录Assets/Editor/EmberEditingTemplate.json：相同contentHash、platformer-2d 0.4.1。
- versionedContentHash仍为`373437cccbe9f4ba6e93359da2c8b2fd`，尚未Bump封存。
- version=0.4.1、frameworkVersion=0.19.0、base 0.8.3及parentContentHash均保持不变；当前实际包仍为0.19.2。
- 保存后13:52:09再次执行中心比较：0错误、0警告、0差异，活动磁盘内容与保存基准一致。Bump、提交、推送、发布、消费升级均未执行。

新项目不要复制PackageCache或直接搬快照作为升级。后续发布需单独授权模板Bump与框架发布，再通过Ember/UPM Manager和项目中心处理消费版本及本地差异。已保存未发布的内容不等于消费端自动获得。

消费手动检查：按文档配置平台发令顺序和碰撞层；挂好Scope/玩家/可选世界复位及相机参与者；用两关验证新游戏→换关→组合保存退出→检查点续玩；验证错误场景键/缺失参与者/恢复回调失败、区域重叠/传送/卸载；最后再验收产品挥旗与原关卡路线。Q01—Q08不变。

## 剩余验收与具体步骤

公共能力源码已交付；以下区分后续已经补齐的白盒集成验收与消费项目仍需验证的事项：

1. **T05公共两关链路已补齐**：A/B独立资产通过正式Main→A→B→组合保存退出Main→检查点续玩，默认UI正常打开。缺少Scope/玩家参与者时旧会话保持、失败候选与相机清理。完整断言和复跑入口见[两关证据](04-two-level-acceptance.md)。
2. **组合续玩已补齐**：空中暂态运动使用旧合法基线组合最新示例进度，保存失败留场、成功离场；磁盘读回后Prepare阶段恢复世界/示例数据再启动玩家。产品仍须选择自己的数据类型与UI入口；后续已补齐退出Play和Windows64 Player两个系统进程的冷启动测试，见[冷启动证据](05-cold-start-acceptance.md)。
3. **T06公共镜头已补齐**：加载A/B、检查点续玩和死亡传送均检查绑定、虚拟相机历史和Brain实际输出，卸载检查注册释放。产品房间边界、区域优先级和旗面UI覆盖仍须消费接线验收。
4. **T02平台宿主时序**：平台必须在玩家固定步前提交SetVelocity，上升净空约束之后不得再次覆盖速度；用实际载具路径验证横移/升降/转向与风配置。旋转承载、多个运动驱动器争抢及复杂多形状乘员不在保证范围。
5. **产品及发行**：Windward挥旗FSM的Accepted回调、风速合成、蒲公英倍率、原关卡解法、美术/UI和发行构建须由消费项目实测；没有改动Mountain或创建Windward。T08只有出现具体关卡实测缺口再评估。

恢复失败保留暂停和LastError；部分世界变更没有自动回滚。若此时禁用恢复组件，会停止玩家会话，后续须走正式入口重建。世界参与者按显式策略和原因处理，项目决定收集、机关、历史记录的业务含义。

本轮新增维护源：Game/State/Editor/PlayerLevelAcceptance.cs、Player/Development/PlayerCheckpointAcceptanceParticipant.cs、Game/Development/LevelAcceptance下A/B场景和配置、模板LevelAcceptance.md。2026-10-17后续普通保存结果以下方最新保存记录为准，13:51记录保留为历史。

## 最新普通保存（两关验收交付）

2026-10-17 14:17:47，通过Unity内项目中心正式SaveTemplate普通保存，返回1388文件。保存前0错误、0警告、19项预期差异；FrameworkScene未脏，未覆盖任何原有场景或发行列表。

- 当前contentHash：`f817fbbc7c77dc4e6697178635978246`，编辑记录一致。
- platformer-2d仍为`0.4.1`；versionedContentHash仍为`373437cccbe9f4ba6e93359da2c8b2fd`。
- 实际包`0.19.2`，模板frameworkVersion声明`0.19.0`，父base `0.8.3`，均未修改。
- 未Bump、提交、推送、发布或执行消费升级。Library的运行库、报告和截图没有收入模板。

保存后14:18:01项目中心复核：0错误、0警告、0差异；最终Console零错误。

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

本轮最终普通保存于2026-10-17 15:10:15完成（1395文件），当前contentHash为`7a2972f9d108f3c7472f3dbc90c0262c`；保存后0错误、0差异、1条未封存警告。版本与版本封存hash未变。前节`db6747...`为前轮历史保存值，最新状态以本节及06文档为准。构建期间另有6条Unity编辑器原生Assert，已在06中独立记录，未声称全过程Console零诊断。

## 构建调度复核（2026-10-17 15:20）

验收Build入口现先排队返回，再由一次性Editor update构建，拒绝重复请求并记录原始诊断。新发行构建0错误、0警告、0 Assert，两进程保存16/16、续玩19/19通过。原同步入口和显式SynchronizationContext对照均未复现历史Assert，故确切触发条件仍未确定，不声称已证实MCP为唯一根因。详见[调度修正及三组对照](07-build-scheduling-acceptance.md)。

最新普通保存时间15:19:57（1395文件），contentHash为`35505b28730529dd56ab61a475994056`，校验0错误、0差异、1条未封存警告。框架/模板版本、版本封存及父级hash不变，无Bump、提交、推送或发布；以上早先保存值均为历史记录。

## 非增量复核（2026-10-17 15:28）

新调度入口使用CleanBuildCache完成非Development Windows64 Mono构建：0错误、18条既有内容警告全部复现、0 Assert；两个独立进程保存16/16、续玩19/19通过。不能将前轮缓存构建零警告算作问题已修复；历史6条Assert此次仍未复现，根因不作确定归因。证据见[非增量构建记录](08-clean-build-acceptance.md)。

最新普通保存时间15:27:48，1395文件，contentHash为`b0d2b8954453f5eefeb8f094854c24eb`，版本和版本封存hash不变，未Bump、提交、推送或发布。

## 警告修正（2026-10-17 15:43）

18条警告已按Ember自有包、embedded UniRx与Feel Assets/私有镜像分别修正；最终CleanBuildCache发行构建0错误、0警告、0 Assert，两进程16/16及19/19通过。没有关闭诊断或修改PackageCache。详见[维护归属、改动与证据](09-build-warning-fixes.md)。

本轮项目中心检查0错误、0模板差异，未改模板内容，故不重复普通保存；最后保存及hash保持上节值。全部包版本未Bump，修正均未提交/发布。另记录既有UniRx关闭域重载时的Editor播放检测限制，不把Player通过结果扩展为该编辑器路径已修复。

## UniRx播放生命周期修正（2026-10-17 15:55）

上述关闭域重载限制已在embedded UniRx维护源修正：播放检测不再依赖脚本重载，Dispatcher每轮Play复位单例与退出状态。隔离场景连续三轮进入/退出Play，初始化、工作线程运行时Post、重复实例清理、销毁后重建全部通过。新CleanBuildCache发行构建0错误、0警告、0 Assert，两独立进程保存16/16、续玩19/19通过。详见[维护改动与实测证据](10-unirx-play-lifecycle.md)。

项目中心0错误、0差异；本轮无模板内容改动，不重复普通保存，最后保存及版本/hash保持15:27:48记录。保留已有修改，未Bump、提交、推送或发布；embedded修正不会随注册表旧版本或模板保存自动分发。

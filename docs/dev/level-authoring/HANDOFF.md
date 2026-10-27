# Ember 2D 关卡制作工作台交接记录

截至2026-10-27，契约1.2的框架与platformer-2d实施及本仓库适用验收已继续补齐。五项优先工作均取得新证据：缩放Grid、失败注入、BuildProfile切换、真实工程重开、四类skill新增/更新闭环。本文区分本地实现、本地验证与发布；不宣称Windward适配或干净消费升级通过。

## 版本、来源与交付状态

| 对象 | 来源与当前状态 |
|---|---|
| 包 | 本仓库 `Packages/com.ember`，0.24.0；发行引用 `v0.24.0` |
| platformer-2d | 正式维护源为 `Assets/Game`；正式Save/Bump封存0.13.0，共1948文件 |
| 内容与封存指纹 | `ff217ca17005f39519049c46a83c138b`；实际磁盘hash一致，见[发行模板校验](evidence/release-template-validation.json) |
| 父模板 | base 0.10.0，内容不变；所有模板通过正式父级同步兼容0.24.0，保留已有子模板GUID |
| 发布 | 本报告随正式发行 `v0.24.0` 交付：[发行源码](https://github.com/wsydet/ember-unity-framework/tree/v0.24.0)。没有修改Windward或PackageCache；消费适配仍未验证 |


实施契约只读来源：`C:\Users\wuyu\My\Windward\docs\design\editor\framework-level-authoring-request.md` 1.2。配套消费适配文档只读参考。使用现有LaunchZone2D、检查点及Player正式生命周期，没有新增二段跳、恢复点或造风玩法。

## 本次补齐的实现

- 世界尺寸按实际Grid格轴测量，支持非均匀缩放、镜像与XY旋转，保留Transform和Tile Anchor。非整除、退化、剪切与非XY网格明确阻止，不静默规整。
- 修复非默认PPU、非方形格子和Pivot导致视觉/碰撞不一致的问题：可重建的Sprite顶点缓存保留UV/三角形，归一化视觉到占用格；Tile变换为单位矩阵，原生Grid碰撞不会二次缩放。缓存几何键含尺寸和锚点。
- 全局场景同步改用Unity6公开 `EditorBuildSettings.globalScenes`。实际发现 `scenes` 会指向活动独立Profile，旧写法会覆盖Profile；修复后全局和独立列表互不覆盖。
- 试玩部分准备失败会清理并恢复；准备期取消可恢复；旧请求的异步错误不再停止新会话。PlayerData故障在退出Play前通过公开桥接报告，恢复后保留明确错误。
- 新建/复制完成后校验完整装配，失败回滚本次三份文件。校验补充缺失Prefab、缺失脚本和弹射接收端，重复检查点/非法起点均可定位。
- 修正Full Rect背景被错误判成不支持平铺的问题；样例补装已有PlayerLaunchTarget2D；新增LaunchZone2D公开配置入口，供人工与skill共用。

## 公开API、程序集和依赖

完整签名用途、生命周期和使用步骤见[模板文档](../../../Assets/Game/Documentation/platformer-2d/LevelAuthoring.md)。人工入口：`Ember/Level/2D 关卡制作工作台`。

| 程序集 | 公开入口 |
|---|---|
| Ember.LevelAuthoring.Runtime | EmberTerrainTile.Configure/Evaluate；EmberParallaxLayer.Configure/Evaluate/BindRuntimeCamera/RebaseCamera |
| Ember.LevelAuthoring.Editor | IEmberLevelAdapter、IEmberLevelObjectAdapter、EmberLevelAdapters.Register/Unregister；EmberLevelAssetService.Find/Upsert/SaveSize/SaveMaterial |
| Ember.LevelAuthoring.Editor | EmberTerrainEditing.GetWorldCellSize/GetCellCount/Validate/Stamp/Paint/Move/ReplaceStyle/Refresh/ValidateRules |
| Ember.LevelAuthoring.Editor | EmberLevelObjectEditing.Place/Duplicate/Delete/ConfigureRenderer/SetEditorVisibility；EmberLevelTrialSession.Start/Stop/Retry/Request/Status/ReportFailure |
| Ember.Core.Editor | EmberBuildSceneRegistry.Register/Unregister/Merge/GetEffectiveScenes/ValidateEffectiveScenes |
| Game.LevelAuthoring.Runtime | PlatformerAuthoringLevel、PlatformerAuthoringCheckpoint、PlatformerLevelPresentation |
| Game.Player.Runtime | PlayerEntryRequest.ForLevelTrial、PlayerLabEditorBridge.PrepareLevelAuthoring/ReportDevelopmentFailure；检查点区公开Configure/CheckpointId |
| Game.Entity.Runtime | LaunchZone2D.Configure/Configuration/Cooldown |
| Assembly-CSharp-Editor | Game.State.Editor.PlatformerLevelAuthoringAdapter、PlatformerLevelSamples |

框架Editor命名空间为 `Ember.LevelAuthoring.Editor`，运行类型为 `Ember.LevelAuthoring`。状态适配器位于预定义Editor程序集是因为现有GameGameplayState仍在Assembly-CSharp；框架包不反向引用Assets。

验证环境：Unity6000.5.4f1。新增显式依赖为Tilemap1.0.0、Physics2D1.0.0、Unity.Collections6.5.0；其余复用Ember Basic/Core/Camera/UniTask及模板Player/Entity/SceneFlow/PlayerData。不需要RuleTile/Extras；Tile Palette可选，不静默安装。

## 四类skill与样例

正式维护源是 `Assets/Game/Documentation/TemplateSkills/catalog.json`（schema v2）及四个目录：

| 调用入口 | 制作范围 |
|---|---|
| $ember-level-add-terrain | 自定义宽高/格锚点/网格/物理类型、连接规则、登记、邻域与碰撞验证 |
| $ember-level-add-mechanism | 已有机关Prefab、公开配置、身份/关联、范围、正式试玩 |
| $ember-level-add-background | 层、排序、覆盖和平铺、视差往返/镜头切换 |
| $ember-level-add-decoration | 锚点、声明变换、排序、无意外碰撞、保存恢复 |

本地正式同步后四项GetTemplateSkillExecutionBlockReason均为null。技能先核对正式模板身份和真实API，再原路径同ID更新；不能从发现副本反向修改维护源。四类新增、更新及重复更新已经执行，实际资源与GUID保持一致，见[资源流程](evidence/skill-workflows-resources.json)、[运行验证](evidence/skill-workflows-runtime.json)、[画面](evidence/skill-workflows-game.png)及[可复现调用脚本](evidence/skill-workflows.cs.txt)。

样例根目录：`Assets/Game/Development/LevelAuthoring`。

- AuthoringSample.unity及Level/Config：无需外部美术，可正式试玩的白盒关。
- Solid/OneWay/Decoration.asset、WhiteboxAtlas.asset、Size0—Size4及OneWaySize/DecorationSize：实体/单向/无碰撞、四基础尺寸与短平台。
- LaunchPad.prefab及四类Material：资源制作参考。
- Acceptance/SkillWorkflow.unity：四skill闭环，实际Grid缩放(2,0.5,1)，自定义尺寸更新、现有机关真实物理触发、背景与装饰。
- Acceptance/NewLevel.unity、CopiedLevel.unity：新建/复制示例。IncompleteSource.unity是故意不完整的失败夹具，不属于关卡目录。
- Acceptance/GlobalScenesProfile、OwnScenesProfile、BuildAcceptanceProfile：继承全局、独立列表及构建验收配置。
- AuthoringBuildBoot.unity：正式Framework引导场景的验收副本，额外挂开发构建探针；不进入默认发行集合。

## F01—F20验收结果

“通过（本仓库样例）”仅覆盖列出的代表性场景和公开服务/生命周期，不代表全部消费项目、全部故障排列或全量测试。人工绘制手感、干净消费部署与产品规则属于后续接入验收，未用本地证据替代。

| 项 | 结果 | 证据和边界 |
|---|---|---|
| F01 | 框架能力通过；消费部署未覆盖 | 正式模板入口/技能可用；[缺适配器明确阻止](evidence/missing-adapter.json)。没有创建干净消费工程，也没有改带本地修改的Windward |
| F02 | 通过（本仓库样例） | [新建复制与冲突](evidence/create-copy.json)、[复制后装配失败回滚](evidence/level-generation-rollback.json)，没有半登记 |
| F03 | 通过（本仓库样例） | [资源事务](evidence/resource-roundtrip-result.md)、[22项回归](evidence/editmode-delivery.json)、四skill流程；Prefab连接、参数、地形、Undo/Redo、保存重开 |
| F04 | 通过支持的XY矩形Grid | [实际缩放/镜像/旋转/自定义Anchor、Pivot、PPU回归](evidence/editmode-delivery.json)，不改原Transform；剪切/非XY/间隙网格仍明确不支持 |
| F05 | 通过（注入样例） | [缺Prefab、重复检查点、非法位置/检查点](evidence/diagnostic-injection.json)、[缺适配器](evidence/missing-adapter.json)；诊断指向对象，无假就绪 |
| F06 | 通过模板PlayerData范围 | [入口](evidence/entrance-ready.json)、[位置](evidence/position-ready.json)、[检查点](evidence/checkpoint-ready.json)、[Death事务](evidence/death-recovery.json)、[重试](evidence/retry-ready.json)、[41份持久化文件比较](evidence/storage-comparison.json)。碰撞伤害致死完整输入链与消费旁路存储未覆盖 |
| F07 | 通过要求的代表故障 | [预检/部分准备/关联失败](evidence/failure-rollback.json)、[真实IoFailed](evidence/storage-failure-fixed.json)、[真实编译错误](evidence/compile-failure-blocked.json)、[准备取消/连续点击](evidence/cancel-and-deduplicate.json) |
| F08 | 通过已支持模式 | 实际域重载、DisableDomainReload预检阻止、取消及重试；[关闭/重开窗口](evidence/window-reconnect.json)保留同请求且只消费一次；旧异步错误加所有权检查，未穷举所有线程调度竞态 |
| F09 | 通过（本仓库样例） | [多场景/选择恢复](evidence/edit-scene-restored.json)、[异常Scene视图恢复](evidence/exception-view-restored.json)、[直接退出Play](evidence/manual-exit-restored.json)，无运行修改回写 |
| F10 | 通过 | [GUID移动](evidence/registry-move.json)、[Profile切换](evidence/profile-switch-fixed.json)、[重开前](evidence/reopen-before.json)/[重开后](evidence/reopen-after.json)：进程50460→113428，OwnScenesProfile独立两项与全局五项保持，业务禁用状态不丢 |
| F11 | 通过实际Windows Player | [最终Profile构建](evidence/delivery-build-result.json)、[实际Player](evidence/delivery-build-player.json)；正式初始化进入业务样例，移动约1.20、跳跃约1.27世界单位 |
| F12 | 消费阶段未覆盖 | 本次没有发布或消费模板迁移授权；提供三方迁移范围，不能声称包升级/消费工程迁移通过 |
| F13 | 本仓库白盒通过；干净部署未覆盖 | [白盒实际运行](evidence/entrance.png)、四尺寸/单向/装饰和矩形事务；没有导入Windward美术 |
| F14 | 通过（规则样例） | [最终形状回归](evidence/terrain-final.json)：矩形/L/内洞/孤立/窄条，原生SetTile、追加擦除移动、Undo/Redo逐格核对实际Sprite与占用 |
| F15 | 通过基础规则范围 | 256邻域、同级冲突/缺图回退、异组硬边界；规则草稿显式应用只刷新打开场景。无随机变体，不宣称复杂过渡或全库压力测试 |
| F16 | 通过兼容样例替换 | [最终形状回归](evidence/terrain-final.json)保持占用与碰撞；不兼容物理/格/锚点拒绝。未代替Windward正式美术验收 |
| F17 | 通过（四skill运行样例） | 平铺/排序/锁定公开编辑；[真实视差往返、相机切换](evidence/skill-workflows-runtime.json)，停止/重开基准无漂移 |
| F18 | 通过（本仓库样例） | 装饰允许变换、排序、Prefab复制Undo、无Collider、保存重开；[画面](evidence/skill-workflows-game.png)检查背景覆盖与装饰呈现，复杂产品前景仍需消费验收 |
| F19 | 模板正式流程通过；消费发现待迁移 | 四skill从正式发现副本执行新增/更新，真实公开API、资源、验证、报告齐备；[同步回执](evidence/template-delivery-save.json)。未部署到Windward |
| F20 | 通过 | [四基础尺寸实际碰撞](evidence/resource-roundtrip-result.md)、自定义尺寸重复更新、[22项回归](evidence/editmode-delivery.json)涵盖世界尺寸、缩放、非方格、Pivot/Anchor/PPU和不兼容拒绝 |

最终只运行EmberLevelAuthoringTests相关22项，22通过、0失败，约3.55秒；没有运行全项目测试。最终Windows构建27.72秒、237.34MB、0错误0警告；实际Player使用显式独立测试根。

发布检查：候选代码提交 `4cfb4e6eabb6a601bb01653e31eb60e5633b0fa4` 的共享二进制/字体静态检查通过，二进制分别在core.autocrlf=true/false检出通过。干净候选worktree执行正式bundle检查，11个通用Skill、33文件通过；四类关卡Skill属于模板分发。0.24.0加载后编译/导入/编译失败/Play状态均为false，四类Skill执行闸门均为null。主工作区bundle检查曾因未跟踪的模板发现副本拒绝，未删除副本或将其混入通用bundle，而在干净检出复核通过。封存过程Console有已有空Editor程序集提示，不将其混称为构建警告；上述最终构建仍为零警告。

[交付时Unity状态](evidence/delivery-state.json)记录最终程序集MVID：非编译/非导入、无编译失败、非Play、无活跃试玩、FrameworkScene、全局四场景及零构建诊断；同期Console为0错误0警告。EditorSettings和EditorBuildSettings测试差异已清零。

## 失败记录与修复

- 保留首轮[夹具失败](evidence/initial-tests.json)、续轮[物理轮廓方案失败](evidence/geometry-initial-failure.json)。Sprite.OverridePhysicsShape在本环境不满足编辑需求；没有忽略错误，最终改为顶点缓存＋原生Grid碰撞并通过22项及实际Player。
- 续轮曾出现资源回滚测试的命名预期错误、测试服务初始化超时；修正断言并将初始化窗口设120秒后执行完成。[重开后19项](evidence/editmode-19-passed.json)及最终22项分开保留。
- BuildProfile覆盖全局/独立列表、Full Rect平铺误判、样例漏弹射接收端、存储故障退出竞态丢诊断均已修复。
- 中间构建的辅助代码警告、TileBase生命周期覆盖警告已修复。历史[Profile构建](evidence/profile-build-result.json)和[中间构建](evidence/final-build-result.json)保留原警告计数；最终构建零警告。
- 一次中间编译遇到NativeArray using变量写入错误，已改为构造前准备数组并重新完成Unity编译；失败时误启动的旧程序集测试不作为有效证据。最终证据只指最终编译/回归结果。

## Windward升级和模板迁移

1. 使用Ember 0.24.0（发行引用v0.24.0）与platformer-2d 0.13.0，记录解析出的commit及本报告封存hash。正式安装前确认远端tag已可解析；旧0.23.0/0.12.1不包含本次工作台。版本来源与验证边界见[发布说明](../release-0.24.0.md)。
2. 包通过Ember/UPM Manager升级；Assets采用旧模板、新模板、本地内容三方迁移，保护Windward自有GUID、关卡ID、玩法和配置。包升级不会更新Assets。
3. 合入LevelAuthoring运行模块及状态Editor适配样例；GameInitState只合入PrepareLevelAuthoring接点，PlayerSessionContracts增加ForLevelTrial，PlayerLabEditorBridge增加准备与失败回报，PlayerDevelopmentEnvironment增加即时故障回报。
4. 合入检查点公开配置、LaunchZone2D公开Configure；有弹射区的场景显式接现有PlayerLaunchTarget2D。按诊断在候选关处理，不扫描批量修改旧关。
5. 实现Windward自己的IEmberLevelAdapter，读取既有WindwardLevel，不把模板资产格式强加给产品。Init前隔离每个产品持久化出口，明确测试起点/检查点恢复；对象适配器维护产品ID和关联。
6. 工作台目录与发行登记分开；Register只维护globalScenes。独立BuildProfile.overrideGlobalScenes自行显式维护，不能当成全局同步目标；BuildPlayerOptions.scenes是当次构建的直接输入。
7. 完成模板业务迁移后，在项目中心预览并同步模板专属AI Skill，刷新AI会话；四skill均先执行GetTemplateSkillExecutionBlockReason及API能力检查。
8. 在Windward候选关完成E00—E05及对应F项，再迁入正式制作；之后才开展新跳跃、新交互、新玩法关卡。

## 已知限制与回退

首版保存后试玩，不支持未保存草稿副本、运行修改回写或DisableDomainReload试玩；保留项目原设置，用户要试玩时须显式启用域重载。仅支持无间隙、无剪切的XY矩形Grid，不隐式跨Grid连接；没有随机变体、复杂材质过渡、自动散布或节点图。带额外法线贴图的自定义地形材质与数万Tile性能分位数未验证，不提供10秒启动保证。人工长期绘制手感仍需制作人员验收。

隔离证据只覆盖模板PlayerData和显式构建探针；Windward的Prefs/其他产品出口、全路线、正式美术及升级冲突尚未验证。F01/F12的干净消费部署与升级属于下一阶段，不用本仓库结果冒充。

回退先撤回新组件/Tile在场景中的引用，再恢复旧包与模板组合，保留消费自有资产和修改。场景编辑用Undo；磁盘新增资源按创建清单回退，不能承诺Undo删除资产文件。模板只经项目中心恢复/保存，不手改Templates~、ParentSnapshot~或hash。测试新增发行登记已撤销，活动Profile还原为无独立Profile，全局四基础场景与原Play Mode选项已恢复；其他既有ProjectSettings修改保留。

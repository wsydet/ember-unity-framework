# 2D 关卡制作工作台

本页随 platformer-2d 及合法派生模板分发。关卡工作台首次交付组合为 Ember 0.24.0（v0.24.0）与 platformer-2d 0.13.0；必须同时升级框架并迁移模板接线，旧0.23.0包或0.12.1模板不包含这些能力。正式发布状态以对应tag及发布说明为准。

入口：`Ember/Level/2D 关卡制作工作台`。模板身份不匹配时隐藏菜单，Player关闭时不能操作。关卡权威数据仍为Unity场景、Tile和Prefab；没有第二份JSON占用地图。

## 快速使用

1. 打开工作台并选择白盒制作样例，点击打开。地形页选择目标Tilemap，选择尺寸素材，启用Scene放置。点击/连续拖动绘制，矩形模式拖出选区；Esc取消笔划。擦除、Undo/Redo使用相同格子事务。
2. 基础尺寸为0.5×0.5、1×1、0.5×10、10×0.5、3×0.5，宽×高为世界单位。示例网格0.5，分别占1×1、2×2、1×20、20×1、6×1格。印章落下后按普通格子编辑。
3. “制作尺寸预设”选择样式，输入正有限宽高、格锚点偏移，预览占格后保存到项目目录。选择已有预设可原位更新；只影响后续放置，不隐式重铺旧地图。
4. 机关页选择已登记的编辑适配器，声明物理与变换权限后登记Prefab，或直接放置现有弹射机关；Inspector编辑真实组件参数和Collider范围。背景/装饰页可登记Project中的Prefab或Sprite，编辑排序、色调与平铺。支持网格吸附、放置旋转/缩放/镜像，超出素材权限时阻止；复制/删除使用同一Undo服务，复制保留Prefab连接。锁定/隐藏为编辑器辅助。
5. 检查问题列表可定位对象。入口/测试位置/已登记检查点试玩均保存后执行，通过Framework/Main/GameSceneScope正式初始化。测试位置不写正式PlayerConfig。
6. 停止等待正式异步清理与PlayerData释放，再恢复场景组合、活动场景、选择和Scene视图。窗口关闭不终止或丢失会话，重新打开可停止。重试重用起点描述并新建测试进度；配置依赖变化时拒绝重试，要求重新启动。

未命名或未保存场景不能试玩；编译错误/导入中禁止启动。首版要求Domain Reload启用。位置预检采用玩家BoxCollider的保守空间检查，复杂非矩形几何应由消费项目替换为精确校验。

## 样例与依赖

- `Assets/Game/Development/LevelAuthoring/AuthoringSample.unity`：实体台阶、单向平台、现有LaunchZone2D、检查点、背景和装饰。
- 同目录 `AuthoringSampleLevel.asset`、`AuthoringSampleConfig.asset` 为关卡身份和完整玩家方案接线。
- `WhiteboxAtlas.asset`、`Solid.asset`、`OneWay.asset`、`Decoration.asset`：无外部美术依赖的白盒及256邻域规则。
- `Size0.asset`—`Size4.asset` 与相应Material：五种实体尺寸。`OneWaySize.asset`、`DecorationSize.asset` 为单向与无碰撞条。
- `LaunchPad.prefab`、`LaunchMaterial.asset`、`BackgroundMaterial.asset`、`DecorationMaterial.asset` 为四类制作的资源参考。

验证环境为Unity6000.5.4f1。包显式依赖 `com.unity.modules.tilemap 1.0.0`、`com.unity.modules.physics2d 1.0.0`；另依赖现有Ember/Core/Camera/UniTask及平台Player/SceneFlow/PlayerData/Entity接线。基础自动拼接不依赖RuleTile/Extras。Tile Palette是可选编辑包；缺失时工作台提示，不静默安装。

## 公开接口与程序集

| 程序集 | 类型/入口 | 用途 |
|---|---|---|
| Ember.LevelAuthoring.Runtime | `EmberTerrainTile.Configure/Evaluate` | 连接身份、八邻域规则、确定性优先级与碰撞语义；原生Tile编辑同样刷新邻域 |
| Ember.LevelAuthoring.Runtime | `EmberParallaxLayer.Configure/Evaluate/BindRuntimeCamera/RebaseCamera` | 基准与相机位移求值，不接管相机 |
| Ember.LevelAuthoring.Editor | `IEmberLevelAdapter`、`EmberLevelAdapters.Register` | 发现、新建/复制、诊断、正式试玩适配 |
| Ember.LevelAuthoring.Editor | `IEmberLevelObjectAdapter` | 机关预检、身份/关联事务、范围绘制和诊断 |
| Ember.LevelAuthoring.Editor | `EmberLevelAssetService.Find/Upsert/SaveSize/SaveMaterial` | 项目自有资产创建/更新与稳定ID检查 |
| Ember.LevelAuthoring.Editor | `EmberTerrainEditing.GetWorldCellSize/GetCellCount/Validate/Stamp/Paint/Move/ReplaceStyle/Refresh/ValidateRules` | 世界格轴几何、事务、规则检查与兼容替换 |
| Ember.LevelAuthoring.Editor | `EmberLevelObjectEditing.Place/Duplicate/Delete/ConfigureRenderer/SetEditorVisibility` | Prefab/背景/装饰编辑 |
| Ember.LevelAuthoring.Editor | `EmberLevelTrialSession.Start/Stop/Retry/Request/Status` | 试玩去重、描述符、保存与编辑现场恢复 |
| Ember.Core.Editor | `EmberBuildSceneRegistry.Register/Merge/GetEffectiveScenes/ValidateEffectiveScenes` | 全局发行登记与有效BuildProfile诊断 |
| Game.LevelAuthoring.Runtime | `PlatformerAuthoringLevel`、`PlatformerAuthoringCheckpoint`、`PlatformerLevelPresentation` | 模板关卡资产、已有检查点机制、正式相机/世界基线接线 |
| Game.Player.Runtime | `PlayerEntryRequest.ForLevelTrial` | 完整冻结方案下的合法零初速位置请求 |
| Game.Entity.Runtime | `LaunchZone2D.Configure/Configuration/Cooldown` | 已有弹射机制的完整验证与公开参数更新，不改变Collider |
| Assembly-CSharp-Editor | `Game.State.Editor.PlatformerLevelAuthoringAdapter`、`PlatformerLevelSamples` | 现有GameGameplayState位于Assembly-CSharp，状态适配放其Editor侧 |

Editor类型完整命名空间为 `Ember.LevelAuthoring.Editor`；运行地形/视差为 `Ember.LevelAuthoring`。Editor资产仅做目录与预设，运行场景引用实际Tile/Prefab/组件。

## 连接规则与尺寸

八邻域位序：右、上、左、下、右上、左上、左下、右下。`required`为必须同组的位，`forbidden`为必须不连接的位，未指定位任意；只在同一Tilemap求值。不同组明确断开。每条规则指定Sprite、priority、quarterTurns、mirrorX；只有规则显式声明才旋转/镜像。最高优先级确定输出，同级多命中或缺图使用fallback并诊断。

选中地形资产的Inspector编辑草稿，预览六类图形与受影响场景后明确应用。仅刷新当前已打开场景，不打开/保存其他关卡。外部API修改规则应先校验/预览，再显式刷新目标区域。

地形使用与占用格一致的原生Grid碰撞，纯装饰无碰撞；单向必须有实际PlatformEffector2D。图片顶点缓存可重建，保留源UV与三角形，不改变源Sprite。缓存补偿PPU、Pivot、Tile Anchor及规则旋转/镜像，Tile变换保持单位矩阵，使视觉对应占用格，碰撞不被二次缩放。该实现引用Unity.Collections；本环境解析为6.5.0，已显式加入包依赖。带额外法线贴图等自定义材质的地形尚未验证。

尺寸印章支持非均匀缩放、镜像及XY平面旋转的无间隙矩形Grid，保持原Transform与Tile Anchor；`GetWorldCellSize`测量目标格轴世界长度，`GetCellCount`检查世界尺寸能否精确整除。旋转后宽高指格轴长度，不是世界轴对齐包围盒。退化、剪切或倾离XY平面的网格明确阻止。样式Anchor仍需匹配目标，不做隐式转换。兼容样式替换保留占用；不支持跨Grid隐式连接、随机变体、手工锁定格外观或复杂跨材质过渡。

## 背景与装饰

渲染顺序以真实SortingLayer/Order为准。平铺要求Full Rect图片；编辑器隐藏/锁定不写产品运行状态。视差0为世界固定、1为完全跟随，默认关闭。`PlatformerLevelPresentation`需显式登记到GameSceneScope参与者尾部，在玩家启动后建立世界基线并绑定现有主相机。消费项目镜头切换/复生后按自身语义调用RebaseCamera，不再添加竞争的相机控制器。

选择带视差组件的对象可在工作台编辑参考相机、系数和基准；相机位移预览只画黄色覆盖线框，不移动对象、不写入场景。指定正交参考相机时同时画相机范围。重置预览清空临时位移。

## 四类技能

- `$ember-level-add-terrain`：白盒/自定义尺寸、规则资产、登记及邻域验证。
- `$ember-level-add-mechanism`：已有组件的Prefab、身份/引用适配、范围与生命周期。
- `$ember-level-add-background`：背景层、排序、覆盖和平移视差。
- `$ember-level-add-decoration`：锚点、允许变换、排序和物理声明。

唯一维护源位于 `Assets/Game/Documentation/TemplateSkills` 的schema v2清单。经项目中心保存，发布时另行Bump；消费端完成模板业务迁移后，通过项目中心“模板专属AI Skill”预览并同步，再刷新AI会话。不得复制发现目录或伪造安装指纹。每项技能先执行正式技能前置检查，再核对所需API，支持同ID原路径更新。

## Windward接入与升级

包升级通过Ember/UPM Manager。包升级不会更新已部署Assets；使用旧基线、新模板与本地改动三方迁移，保护Windward自有状态、玩法、资源和GUID。迁移新增LevelAuthoring模块与状态Editor接线；对GameInitState只合入 `PlayerLabEditorBridge.PrepareLevelAuthoring` 调用，保留消费端其余初始化。PlayerSessionContracts增加ForLevelTrial，桥接增加PrepareLevelAuthoring。

Windward应实现自己的 `IEmberLevelAdapter`，读取既有WindwardLevel，不复制模板关卡资产格式替换现有真相。其BeginTrial必须在任何模块Init前隔离所有产品存储；模板只证明PlayerData通道，不替Windward承诺贴纸、旗面、Prefs旁路已隔离。对象适配器负责真实产品身份和关联；默认样例适配器不能直接用于任意Windward机关。

先只读发现，再候选关验证地形/机关/背景/装饰与入口、位置、检查点；通过后才采用到正式地图。不修改PackageCache，不批量规整原有Grid。不要求新跳跃或造风玩法作为工具前置。

## 发行登记与回退

`EmberBuildSceneRegistry.Register(owner, scene, enabled)`显式加入全局场景列表。工作台目录中的草稿不自动登记发行资格。自动同步保留外部条目和启用状态，只保证FrameworkScene在首位；失效条目保留供诊断。Unity6中维护全局集合必须使用 `EditorBuildSettings.globalScenes`；`EditorBuildSettings.scenes`可能指向活动Profile，不能用它实现全局同步。独立BuildProfile开启overrideGlobalScenes时优先使用自身scenes，不被全局同步改写；需作者显式将发行关卡加入该配置并验证首个引导场景。显式BuildPlayerOptions.scenes又是本次构建的直接输入。

迁移时还需合入 `PlayerLabEditorBridge.ReportDevelopmentFailure` 及开发环境失败回报接线，避免存储故障先停止Play后丢失诊断。存在LaunchZone2D的关卡需要现有PlayerLaunchTarget2D接收组件；新样例已装配，旧场景校验会明确报告缺失，不扫描自动改造消费场景。

回退只撤销本功能文件或恢复本轮记录的旧版本组合，保留原用户工作区。场景操作用Undo；磁盘资源按创建清单回退，不承诺Undo删除资产文件。模板快照通过项目中心恢复/保存，不手改hash。不要在新版场景仍引用新增组件时先移除包；先撤回样例或迁移引用。消费项目升级仍需其维护者授权。

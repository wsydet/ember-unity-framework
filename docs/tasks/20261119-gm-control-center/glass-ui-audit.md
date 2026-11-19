# 玻璃UI覆盖只读审查 D10 / R13

2026-11-19，Asia/Shanghai。实际活动编辑为base 0.11.1、embedded Ember 0.26.0，HEAD `65ae680732323d83de9f898f89e9e8cc576ee4c8`。只读检查当前Assets及自有采样源码，另对六个保存模板各抽查五个直接相关文件；没有切换模板、加载场景、运行探针、编译、Play或Unity测试。本报告不是本轮视觉/采样验收。

## 可直接回给用户的结论

**当前正式base的主菜单、设置、公共Alert/Confirm和SoftGlass控件，已经接入与GM同一套真实背景采样能力；不建议把所有UI再批量“改成GM玻璃”。** 静态证据支持“已有接线、局部观感/配置表达需要核对”，不能支持“所有页面现在都正确采样”的运行结论。

**优先补核的具体接线是Loading进度轨：轨已玻璃化，但在当前Loading宿主链未见BackdropGroup或显式lease绑定，有退回纯染色的可能。** 先确认其玻璃设计意图和实际采样；若确需采样，最小修正是Loading根Canvas的宿主接线，不改转场幕和进度业务。

其余保留现有玻璃链和各页面风格，在后续获准Unity实施时定向对比设置与公共弹窗的遮罩染色，以及GM独有的0.64重染色Backdrop；若想统一通透程度，只在这些页面调整专用参数与制作源。设置仍使用旧schema的黑0.5遮罩，公共弹窗跟随全局目前同样是黑0.5，这会偏厚，但不是“没模糊”。普通图片、原Legacy库、视觉小说原皮肤和World UI不应自动转换。

## 证据入口与边界

- [当前Prefab/资产清单](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d10/glass-assets.json)：逐组件记录文件、行、类型、节点、启用、颜色、Group设置与嵌套源/修改；包含SHA-256。只解析本地YAML，不等于Unity反序列化/完整变体覆盖求值。统计中的表面数量是文件本体显式组件数量，不把嵌套实例的零组件误判成没玻璃。
- [源码/配置证据](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d10/glass-source-evidence.json)：38个实际自有文件的行号摘录与指纹；[保存模板抽样](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d10/template-samples.json)明确均未加载运行。
- 本轮不引用旧截图证明当前成功；历史交付仅用于说明哪些风格刻意保留。网页CSS `backdrop-filter` 是浏览器实现，不是Unity的lease、相机分段或RenderGraph结果。

## 正式入口与安装链

1. [EUIAppearance.asset:16](../../../Assets/GameResource/Resources/Config/UIFoundation/EUIAppearance.asset) 为 `_commonSkin:1`、`_gmPresentation:1`；[枚举](../../../Assets/Game/UI/Runtime/Module/UIFoundation/EUIAppearanceConfig.cs)中1均为SoftGlass。[Bootstrap](../../../Assets/Game/UI/Runtime/Module/UIFoundation/EUIAppearanceBootstrap.cs)读取该Resources配置。[EUICommonDialogs](../../../Assets/Game/UI/Runtime/Module/UIFoundation/EUICommonDialogs.cs)据CommonSkin选SoftGlass Alert/Confirm，而不是简单改旧Prefab的Image类型。
2. [GameMainState](../../../Assets/Game/State/GameMainState.cs)实际打开 `GamePages.EUIMainPage`，[GameSettingsState](../../../Assets/Game/State/GameSettingsState.cs)打开 `EUISettingPage`。[GamePages.cs](../../../Assets/Game/UI/GamePages.cs)指向 `UI/Common/Prefabs` 的正式Main/Setting/Gameplay；[GamePages.User.cs](../../../Assets/Game/UI/GamePages.User.cs)另行注册SoftGlass弹窗/展示页/GM及Legacy展示页。CommonSkin开关并不全局替换所有主页面或所有普通Image。
3. [GameInitState:31](../../../Assets/Game/State/GameInitState.cs)调用ShowGM；[EUIAppearanceBootstrap.GM.cs](../../../Assets/Game/UI/Runtime/Module/UIFoundation/EUIAppearanceBootstrap.GM.cs)先设置DefaultSettings并RequestInstallation，再决定GM呈现。因此当前base即使选择Legacy GM仍会发安装请求；但若消费端完全删去ShowGM调用，其他玻璃页也可能失去这一安装入口。这是启动职责耦合的条件性风险，不能写成当前已断链。
4. [EUIBackdropInstallation.asset](../../../Assets/GameResource/Resources/Config/UIFoundation/EUIBackdropInstallation.asset)同时登记PC与Mobile管线、RendererIndex=1、4个保留层。两份[PC_RPAsset](../../../Assets/Settings/PC_RPAsset.asset)/[Mobile_RPAsset](../../../Assets/Settings/Mobile_RPAsset.asset)的RendererDataList索引1均指向GUID `10ff52c0d52a88944b1fb17d15e1fd38`，即[SoftGlassUI_Renderer](../../../Assets/GameResource/Settings/UIFoundation/SoftGlassUI_Renderer.asset)，内有启用的EUI Backdrop Feature。PC默认Renderer的SSAO或Mobile默认Renderer无Feature，不能据此判断玻璃没安装，因为专用UI Renderer在索引1。
5. [Installation源码](../../../Packages/com.ember/UIRendering/Runtime/URP/EUIBackdropInstallation.cs)按当前URP资产选择索引并配置host；[CameraHost:87](../../../Packages/com.ember/UIRendering/Runtime/URP/EUIBackdropCameraHost.cs)要求独立root Canvas、合法视口/显示和可用保留层；[RendererFeature](../../../Packages/com.ember/UIRendering/Runtime/URP/EUIBackdropRendererFeature.cs)在透明段后采集、下一前景段前发布。静态安装资源齐全不证明运行时相机堆栈、rootCanvas或IsReady一定满足。

## 逐类盘点

| 类别/实际文件 | 当前静态事实 | 判定与建议 |
| --- | --- | --- |
| [EUIMainPage.prefab](../../../Assets/GameResource/Resources/UI/Common/Prefabs/EUIMainPage.prefab) | L610为Camera Canvas，L748为Group；本体3个EUIGlassImage，主卡L48与设置钮L542染色权重0.18，开始钮L807为0.46。Group无独立settings引用，使用DefaultSettings。嵌套旧Button/Image及原背景仍有身份/覆盖记录。 | **已接入**。保持主菜单浅色风格；不把它换成GM深色。静态背景纹理缺少变化时，模糊不显眼不能证明没采样。运行时对照卡内外与移景。 |
| [EUISettingPage.prefab](../../../Assets/GameResource/Resources/UI/Common/Prefabs/EUISettingPage.prefab) | L483 Camera Canvas、L621 Group；本体2个玻璃面，L274/L680均0.18。L586启用Mask，effect=Inherit、schema=0、黑0.5、无settings override。 | **已接入；建议小范围核对遮罩声明**。旧schema固定黑0.5，而非随未来全局染色变化；不要把暗遮罩当没模糊。获准后仅此正式弹窗可经中心明确选择继承新染色或保留显式值，并对比效果。 |
| [EUIGamePlayPage.prefab](../../../Assets/GameResource/Resources/UI/Common/Prefabs/EUIGamePlayPage.prefab) | L202 Canvas、L340 Group；本体2个0.18玻璃面，对应返回/设置操作。普通背景/装饰仍有嵌套源；正式制作源明确保留节点与绑定，不等于全部内容都是玻璃。 | **已接入，应保持玩法/底图职责**。先验证实际操作面，不把整屏玩法画面替换玻璃。 |
| [EUISoftGlassAlertPage.prefab](../../../Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassAlertPage.prefab)、[ConfirmPage.prefab](../../../Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassConfirmPage.prefab) | 各本体4个玻璃面、1个Group，Camera Canvas；面板/取消/关闭0.18，确认0.46。schema=1、overrideMaskTint=false、effect=Inherit，无settings覆盖。CommonDialogs当前选中它们。 | **已接入**。跟随全局Blur和当前黑0.5染色，按需只同步染色/质量引用，不重造采样机制。不要见序列化maskColor黑0.5就断言schema1显式覆盖。 |
| [EUISoftGlassGMPage.prefab](../../../Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassGMPage.prefab)及Entry/Center/State/Time/Action | GMPage L45 Canvas、L188 Group，嵌套正式玻璃Item；L369 CenterHost/Backdrop染色0.64，范围由制作源拉伸；GMPage本身不用Popup Mask。[GM逻辑](../../../Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMPage.cs)展开/收起后RefreshSurfaces。 | **已接入；本GM任务已建议缩小重Backdrop范围**。它的0.64染色/全host范围是独有观感因素，不能当其他页必须同步的标准；D10未制作Unity新GM。 |
| [SoftGlassPrimaryButton](../../../Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassPrimaryButtonItem.prefab)、Panel、Input、Slider、Switch等 | 直接EUIGlassImage或继承其他SoftGlass Prefab。Primary权重0.46，面板/次要钮多为0.18；Slider fill可到0.98、Switch On/Off到1，属清晰功能前景。Item通常无独立Group，依赖宿主页面Group。 | **已接入，应保持功能前景**。不能要求滑杆填充、开关旋钮全部透景；也不应给每个按钮加独立Group/RT。单独拖入无Group容器的行为需宿主接线验证。 |
| [EUISettingSliderRowItem](../../../Assets/GameResource/Resources/UI/Common/Prefabs/EUISettingSliderRowItem.prefab)、[ToggleRowItem](../../../Assets/GameResource/Resources/UI/Common/Prefabs/EUISettingToggleRowItem.prefab) | 部分滑轨/开关已有SoftGlassSurface，普通EUIImageEx行底仍存在。ToggleRow本体有3层同名Surface，其中2层disabled、最内层启用；[Rows制作源](../../../Assets/Game/UI/Editor/UIFoundation/BaseSoftGlassPageAuthoring.cs)会把当前targetGraphic禁用后向其子层添加Surface，存在重复应用继续嵌套的风险。当前正式SettingPage嵌套表没有使用这两行。 | **应保持通用行背景；制作源建议独立小修/复核幂等性**。先明确后续正式设置要复用何种行，再只修目标控件，不把休眠层当三次活跃模糊、不全局替换普通行底图。 |
| [SoftGlassShowcasePage](../../../Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassShowcasePage.prefab)、[ExtensionExamplePage](../../../Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassExtensionExamplePage.prefab) | Group+玻璃控件；各有5个普通Image作为彩色/纯色/场景背景示例。Showcase Backdrop在Prefab中是World Canvas，但[SoftGlassBackdropAttachment](../../../Assets/Game/UI/Runtime/Module/UIFoundation/SoftGlassBackdropAttachment.cs)运行时移至页面同级、切Camera Canvas、排序在页面下2层。 | **显式示例，应保持**。这些Image是被采样内容，不能全部改成玻璃或仅凭Prefab的World模式判采样失败；运行时重排/归还需实测。 |
| [EUILoadingPage](../../../Assets/GameResource/Resources/UI/Common/Prefabs/EUILoadingPage.prefab)与[EUILoadingProgressItem](../../../Assets/GameResource/Resources/UI/Module/SceneFlow/Prefabs/EUILoadingProgressItem.prefab) | LoadingPage无Group，嵌套ProgressItem/基础EUIProgressItem/TransitionSurface也无Group；ProgressItem本体L48有0.18 EUIGlassImage。当前[Loading逻辑](../../../Assets/Game/UI/Runtime/Framework/EUILoadingPage.cs)与[进度逻辑](../../../Assets/Game/UI/Runtime/Module/SceneFlow/EUILoadingProgressItem.cs)未见lease绑定；页面走TopMost而非Popup，序列化useMask不能代替宿主Group。 | **建议优先小范围补核/同步候选**。静态链缺少玻璃宿主，有纯染色风险；运行时确认是否另有祖先/外部借用后，仅为需要采样的进度轨补正确根Canvas Group/lease，或明确保留实色轨。保持转场幕和加载时序，不把整个Loading改玻璃。 |
| Legacy EUIAlert/EUIConfirm、EUILibraryPreview/ExpandedLibrary、标准Image/Sliced与背景 | 公共旧库保留普通Image/EUIImageEx；当前CommonDialogs不默认选Legacy弹窗。Legacy Popup仍可得到全局Blur遮罩，控件皮肤不会自动变玻璃。[EUIBackgroundPage](../../../Assets/GameResource/Resources/UI/Common/Prefabs/EUIBackgroundPage.prefab)是底图内容。 | **应保持**。不删除Legacy/GUID，不把图片资产或转场幕算缺陷。业务主动直开Legacy页时先确认皮肤选择。 |

## 颜色、样式与“看起来不透明”的含义

[DefaultSettings](../../../Assets/GameResource/Resources/Config/UIFoundation/EUIBackdropSettings.asset)实际为16DU、downsample4、iterations2、Throttled30Hz，非Off。Neutral/Accent/Light三个[样式资产](../../../Assets/GameResource/Resources/Config/UIFoundation/SoftGlassNeutral.asset)的tint权重分别0.18/0.46/0.90；有些已制作表面使用直接颜色0.36/0.38差异。**这些alpha在当前Backdrop shader中是采样背景和染色的混合权重，不等于普通Image的透明输出。** [Graphic](../../../Packages/com.ember/UI/Runtime/Backdrop/EUIBackdropGraphic.cs)把前景顶点alpha设为1、把tint alpha传入shape；[Shader](../../../Packages/com.ember/UI/Runtime/Resources/EmberBackdrop.shader)按它混合采样背景，未Ready则走纯染色。因此0.90、0.98或1看起来接近实色可能是设计选择，未Ready也可能呈实色，单看颜色不能区分。

`EUIGlassStyle`不是每个表面的持续绑定材质。[ApplyTo](../../../Packages/com.ember/UIExtension/Runtime/Behaviour/EUIGlassStyle.cs)在制作时写入surface.color；现有Prefab已烘入颜色。只改SO不保证所有现有Prefab自动更新，需要经EUI中心/有界制作操作应用到目标表面，同时同步维护源，不能重跑旧批处理覆盖全部库。

Popup模糊和面板皮肤是两条配合的路径：[BindingBridge](../../../Packages/com.ember/UIExtension/Runtime/EUIBindingBridge.cs)注入Mask配置；[HasExplicitMaskTint](../../../Packages/com.ember/UIExtension/Runtime/EUIBinding.cs)对schema0总保留旧色；[EUIManager](../../../Packages/com.ember/UI/Runtime/EUIManager.cs)当前全局Blur、黑0.5；[MaskSession](../../../Packages/com.ember/UI/Runtime/Backdrop/EUIBackdropMaskSession.cs)申请lease并借给页面Group，失败才走fallback。公共弹窗schema1虽不显式override，目前仍取全局黑0.5；设置schema0和它现在数值相同，但今后改全局时会分叉。

Main/Gameplay/Loading里保留的useMask序列化字段不表示这些Main/TopMost路线都会自动创建Popup遮罩，不能据字段给Loading推定已有外部lease。

普通Image能否遮住玻璃还依赖实际层级/覆盖面积/启用：制作源明确停用旧Panel/Button Image，Prefab变体也存在m_Enabled=0覆盖；背景图片通常在采样边界下方，功能fill/标识在前景。不能把“存在普通Image”直接当遮挡缺陷；本轮未完整求值所有变体、CanvasGroup与运行时重排，仍需现场确认。

## 六个已保存模板的有限抽样

当前实际读到保存版本为ui-game0.2.5、visual-novel0.22.6、source3d-2p5d0.6.5、lane-defense-2d0.1.5、platformer-2d0.13.5、topdown-2d0.1.5；它们不是当前活动编辑身份。本轮每个只读Appearance、DefaultSettings、Installation、Neutral和Bootstrap.GM五个文件，未加载运行。五项除视觉小说Appearance外均与当前Assets相同；视觉小说 `_commonSkin:0`、`_gmPresentation:1`，符合[历史交付保留原玩家UI、玻璃GM](../20261117-base-soft-glass-migration/README.md)的范围。不能把此差异自动迁成SoftGlass，也不能由五文件相同推定各模板所有玩法页面正确。

历史封存记录中的验收版/旧版本只作历史线索；本轮版本来自各实际template.json，未用旧实图替代当前验证。3D跟随气泡等World UI按历史明确保留；未扩大扫描其场景/全部玩法或第三方库，未编辑任何Templates~/ParentSnapshot~。

## 最小同步建议与后续验证

| 项目 | 最小建议 | 归属/现在状态 |
| --- | --- | --- |
| G01 正式设置与公共Popup通透观感 | 后续获准后对同一动态有色下层对比设置/Confirm；先只明确设置MaskTint/schema选择、必要时页面专用settings，避免重染色遮掉变化。不要把schema批量改1抹掉原显式意图。 | base UI接线/样式；**建议小范围同步候选，未改**。 |
| G02 GM重Backdrop | 沿本GM方案收缩范围、选择专用染色/质量，不把0.64推广全UI；其余玻璃面先复用现有链。 | 本GM/base制作范围；已列设计、**未实施**。 |
| G03 安装职责与控件制作幂等 | 将“安装不依赖是否显示GM”列为可选有界接线修正；ToggleRow重复Surface先核对实际调用/宿主，再修制作源幂等性。两项不是当前运行失败结论。 | base UIFoundation/制作源；**建议小修候选**，不新建Manager、不改全局后端。 |
| G04 Legacy/图片/功能前景/World UI | 保留；仅被用户指定且确需SoftGlass的正式新增页面选择适当控件皮肤和宿主Group。 | **应保持**，不批量统一。 |
| G05 真实采样状态 | 入口主菜单、设置、公共Confirm、GM展开/收起分别检查Canvas.isRootCanvas、Provider/lease.IsReady/Diagnostic、相机分段、动态下层是否真实进入、Solid回退、关闭清退和移动质量档。 | **待授权后的Unity定向验证**；本轮没有探针/编译/Play/Unity测试。 |
| G06 Loading进度轨宿主 | **优先**确认玻璃轨是否期望采样及运行时实际Group/lease；若缺失只补Loading根边界/借用接线，配对释放；不改转场幕、快进/真加载和进度时序。 | base/SceneFlow与UIFoundation制作源；**建议小范围同步候选，静态宿主链未见，待运行确认**。 |

未发现仅凭静态证据就必须全局改采样后端的依据。若后续G05复现同源采样缺陷，再以实际证据决定框架修复；视觉统一优先限于目标页面参数/接线和制作来源，保持现有Legacy与消费项目业务边界。

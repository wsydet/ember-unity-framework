# S6 完整试点与技能实施记录

2026-10-27，工程 `ember-unity-framework`，活动模板 base。完成本轮有限范围的实际制作、技能续接、编辑器视觉/交互验收与普通保存。没有进入 S7、Bump、Git 提交、推送、发布或消费升级。

## 本轮产物与归属

| 位置 | 本轮内容 |
| --- | --- |
| `art/ui/S6` | 原生 Photoshop 分层设置源稿、Rename 工作稿、蓝色/青色两个真实 BUI 导出、精确 PNG/文字参考、请求与报告 |
| `Assets/GameResource/Resources/UI/Common` | 设置弹窗原位换肤、BuiSettings 三张正式 PNG（首版两张，增量一张） |
| `Assets/GameResource/Resources/UI/Module/UIFoundation` | 独立 EUIBuiCandidateMainPage 深青色项目皮肤；真实源 Variant、原继承逻辑 |
| `.agents/skills/ember-eui-build` | 框架技能唯一维护源：扩充触发描述及 artwork-workflow.md，支持 Prefab/PSD/工作PSD/BUI/回执五种起点 |
| `Assets/Game/Documentation/base` | 模板正式使用说明及 README 入口，随普通保存封存 |
| 当前目录 | 本轮契约、制作计划/回执、交互日志、截图、保护核对、编译/测试及保存证据 |

无框架运行代码修改，无新专用换肤逻辑。生成器只改了两个 Binding 的时间戳，见 [generation.diff](generation.diff)。未手改 Prefab YAML、meta、Binding 或 EmberManaged；Templates~ 仅由项目中心保存生成，未直接编辑。

## 基线、输入与制作链

开始时重新枚举并固定 `ember-unity-framework@b23e9b768e85ed30`，核对 projectRoot；没有操作 UnityFarm/Burner。实际 base0.8.4/framework0.22.0，初始比较差异0，contentHash `846454530d463521510735d5e796ce43`。备份位于 `.utmp/ui-production-S6/before`。

主界面、动态宿主和动态行从有效 S4 BUI／S5 正式资源继续，不重放历史 Apply。当前契约重新导出并与 S5 final 比较；[contract-comparison.json](contract-comparison.json) 证明三者资产摘要、GUID、模式、绑定、用户代码及列表契约连续一致。真实 PSD→BUI→Prefab 的历史输入链保留，本轮运行验收是新执行结果。

设置页补齐真实链路：

1. [source PSD](../../../../../art/ui/S6/EmberSettings.source.psd) 原生创建，1280×720，三个可编辑文本、两个图片层；独立 [renamed PSD](../../../../../art/ui/S6/EmberSettings.renamed.psd) Rename。源层与命名报告见 `art/ui/S6/source-layers.json`、`rename-report.json`。
2. Photoshop27.5.0／Ember UXP1.0.0 实际加载；bridge inspect 确认文档身份。导出 [EmberSettings.bui](../../../../../art/ui/S6/EmberSettings.bui)：1 Target／6节点／2 PNG／3文字参考。
3. 当前设置契约建立 [settings.eui-map.json](settings.eui-map.json) 与 [settings-request.json](settings-request.json)。Analyze/Preview/Apply/VerifySaved 均走 EmberBuiService1.0.0，正式生成由 EUI 中心完成。保留原几何、中文文案、TMP资源、Framework/IsFramework/FullScreenPopup；更新图片、字号和颜色。
4. 同请求再次 Analyze 返回 noChanges，Apply 返回 completed，不重复生成。之后复制独立工作稿，原生修改 layer10 蓝色→青色，实际导出 [EmberSettingsIncrement.bui](../../../../../art/ui/S6/EmberSettingsIncrement.bui)。[increment-request.json](increment-request.json) 只更新 CloseBackground，图片使用独立 `Increment/` 路径，未覆盖首版输入。
5. 首轮真实画面文字偏小：建立当前契约和 [settings-layout-request.json](settings-layout-request.json)，只将 CanvasScaler 参考分辨率调整为1280×720。最终 [settings-layout-verified.json](settings-layout-verified.json) completed；旧回执因后续合法修改成为历史证据，不再重放。

完整 SHA256 与文件大小见 [inputs.json](inputs.json)，12条跨版本真实 PNG 引用/尺寸校验见 [resource-validation.json](resource-validation.json)。S4四个原始摘要全部保持。BUI中的资源来自节点精确引用，不按目录全集导入。

## F01–F07

| ID | 本轮结论 | 证据与范围 |
| --- | --- | --- |
| F01 | 通过（明确替换范围） | 主界面/动态页/行模板承接有效真实链；设置页新增完整原生PS→Rename→UXP→Maker→正式保存链，输入版本与契约连续 |
| F02 | 通过（按显式映射保留差异） | [视觉核对](visual-review.md)：1280×720默认/主界面来源设置及固定3项数据实际渲染；文字、图片、九宫与保留布局逐项解释。不是Arial到TMP映射或逐像素还原 |
| F03 | 通过（编辑器EventSystem） | [runtime.txt](runtime.txt)：正式启动、主界面设置三次开关、遮罩实际射线命中、Start进入玩法、玩法设置来源及关闭恢复、列表Back返回 |
| F04 | 通过（有限屏幕/安全区范围） | 194字符文本、禁用0回调、选中、单击1回调、滚轮、重排旧lease失效、20→0→3缓存复用；1280×720及1600×720；完整Screen.safeArea和显式padding模拟 |
| F05 | 通过（本会话实际调用） | [skill-invocation.md](skill-invocation.md)：从现有BUI/当前Prefab续接、实际新PSD输入、Analyze/Preview/Apply/VerifySaved、相同输入noChanges、后续增量；不是仅写技能文档或模拟回执 |
| F06 | 通过 | 青色增量真实导出和应用；普通保存627文件，比较差异0，版本不变 |
| F07 | 通过（独立测试资产） | 候选深青色背景/薄荷按钮/白字，公开槽覆盖及新切图引用；实际Settings/Start事件，契约/继承逻辑不变，42个保护源文件未变 |

F02/F04 的通过范围严格限定为本映射和列出的编辑器窗口，不扩展为所有比例、原生特效、全语言、刘海真机或构建验收。当前无未决写入，S7具备接续产物；本轮未执行S7，发布和消费交付仍须独立验收。

## 编译、测试和交互

- 最后生成后主动 MCP compile=request；started `1793035204368`、finished `1793035206469`、domainReloadAfter `1793035210871`，idle且非更新，见 [unity-compilation.json](unity-compilation.json)。不能以空Console代替编译证明。
- 本轮相关18/18 EditMode通过：EmberBuiEditTests、EUIDynamicListEditTests、EUILibraryEditTests；job `3340404ae3684ccd8c3ce9b0b870dbb5`，测试耗时14.56秒，非全量，见 [tests.json](tests.json)。
- 交互使用真实已保存Prefab、正式启动、EventSystem pointer/scroll派发；遮挡验证实际RaycastAll命中EmberBgMask，没有直接向底层按钮绕过射线发送点击。未称实体鼠标/真机人工试玩。
- 设置三轮单次关闭均回主界面。两个同名Canvas是UI Root缓存实例与UIPrefabs加载源，不是两份活动页面。动态页第二/第三轮单击增量1、订阅1；关闭动画完成后count0、lease失效、created=destroyed=3。
- 首次关闭立即采样仍count3/lease有效，是关闭动画未完成；日志保留，后两次在关闭后1秒采样确认释放。20行缓存复用验证同一实例数和逻辑数均20。
- 工具栏实际按钮：One1、Twenty20、Reverse首key option-19、Remove19、Empty0、Three3；Back恢复背景、GM与EUIMainPage。
- 最终退出Play，FrameworkScene未标脏，恢复原GameView尺寸并移除本轮临时尺寸。

## 字体、效果和视觉限制

[font-audit.txt](font-audit.txt) 记录实际TMP与材质。设置/主标题/中文按钮保留既有 DingTalk-JinBuTi SDF，副标题/动态行保留DefaultLightSans（LiberationSans.ttf，目录OFL.txt）。已读取现有字体文件与许可；本轮无新字体导入、无新许可判断或重新分发授权，已有字体发布边界须在后续发布中按原许可核对。

不声称ArialMT映射完成；没有擅自换成另一默认字体。设计字形、中文长度、居中和粗斜效果沿用工程资源。九宫256设计样本→130PNG，四边64/中心2；动态文字仍由EUITextRowLayout排版，Icon与Progress实时来自数据，未烘焙。阴影、描边、混排、遮罩效果、全语言/全平台不在本轮验证范围。

## 首次失败与恢复

1. Photoshop COM／桥接硬链接在沙箱受限，核对无发布请求后通过授权本地入口完成；原生文档结果有正式报告。UDT中加载既有Ember插件，未动其他产品插件。
2. 首个export `ce29e1e567594563af510550ca189504` 明确rejected/INVALID_PARAMS：document多余name、缺少overwrite:null。修正协议后新请求 `5288996a58fe45629cafaf859ff7ac56` completed；增量 `76830caf534d4b09ae69ab8c5e5f390e` completed。终态与inspect见 [bridge-statuses.json](bridge-statuses.json) 和 bridge/；无unknown/pending，不重放拒绝或已完成写入。
3. 临时MCP查询代码曾使用错误EUIBinding/EUILibrary命名空间，以及已废弃GetInstanceID，CodeDom编译前拒绝，无产品代码落盘；核对实际类型后继续。不是工程编译错误。
4. 玩法初次设置点击落在转场Busy期，状态机拒绝；转场完成后实际点击通过。保留日志，不把首次点击写成通过。
5. 部分即时截图抓到转场或未重绘画面，保留为排查资料；正式宽屏设置证据用帧末异步截图 [settings-1600-2.png](settings-1600-2.png)，已读图确认来源与尺寸。不要使用空场景截图作验收图。
6. 技能验证器默认GBK读取中文失败，改为Python UTF-8模式，最终 `Skill is valid!`。未伪造技能发布bundle；源需提交后由正式脚本构建，属于后续发布工作。

Console中曾出现转场Busy和MCP自身WebSocket未初始化warning，后续连接正常；最终Console证据单列，不宣称整个会话零警告。

## 普通保存与保护

[template-save.json](template-save.json)：627文件，base0.8.4／framework0.22.0；contentHash `f5ae9bcc0e3bd361a65b056b367cda71`，versionedContentHash仍 `936dcb1d411afefcacd961cbad1d1d0d`。[template-comparison.json](template-comparison.json) DifferenceCount=0。源与普通保存内容一致；没有Bump或版本封存发布。

[protection.json](protection.json)：S4原始输入保持、42个公共/原主界面保护文件保持、1159条既有删除状态保留。备份差异仅预期设置页/候选/生成时间戳/正式说明，见 [backup-differences.json](backup-differences.json)。可在仓库根运行 `python -X utf8 docs/dev/ui-production-upgrade/evidence/S6/audit.py` 复核只读输入、资源、契约与保护摘要。

## S7 接续边界

使用本轮 final契约、当前设置layout回执、真实S4/S6输入、正式模板说明和框架技能源。不要重放旧pilot/layout/candidate请求或修改旧摘要绕过OUTPUT_CHANGED。新工作再次导出当前契约。

框架公共技能仍列在既有catalog；本轮扩展原技能，不新增模板技能。`.agents/skills`为唯一源，`AISkills~`须在获准提交后由发布脚本生成，不手写sourceCommit。没有干净消费部署、CCX签名、全部外部引用迁移、构建或真机验收。独立皮肤只在候选资产验证，默认导航仍使用原主界面。

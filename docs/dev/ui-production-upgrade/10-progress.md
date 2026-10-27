# UI 基础建设进度与跨对话交接

本页供后续执行对话更新。初稿编制时仅完成方案文档。2026-10-26 执行轮已完成 S0 现场续验：用户确认 16:53:30 手动切换 base，并明确不再追查切换前模板；当前 base 工作区已备份，本轮编译、域重载、基础导航与资产盘点通过。随后 S1 已完成：六页保留 GUID 迁移、契约导出/差异、快照元数据和嵌套所有权修复、正式生成示例、相关测试与真实运行验证；base 已通过项目中心普通保存。S2 现已完成公共库、默认皮肤及编辑器范围 L01–L09 验证；S3 现已完成单模板动态容器、借用取消与有界缓存、独立示例及 D01–D05；S4 现已完成真实分层 PSD、Rename 与 UXP/BUI3 试点导出；S5 已完成真实 BUI 有限制作、原位更新与候选重建，S6现已完成明确映射范围的完整试点、技能调用与独立项目换肤，S7现已完成ui-game本地实现、T01–T05与普通保存；当前交接见下方S7及evidence/S7，最终Bump、发布和T06仍待执行。

## 阶段状态

| 阶段 | 实施状态 | 关键证据 | 下一步 |
| --- | --- | --- | --- |
| S0 现状和 base | 已完成（原模板追查按用户澄清排除） | [工程与验收](evidence/S0/01-workspace.md)、[运行基线](evidence/S0/04-behavior.md) | 保留当前 base；直接进入 S1，不重复加载 |
| S1 组件与绑定 | 已完成 | [组件](evidence/S1/01-components.md)、[迁移](evidence/S1/02-migration.md)、[验收](evidence/S1/03-validation.md)、[机器证据](evidence/S1/validation.json) | 进入 S2，保留新 Page 资产与契约，不重复制作 S1 |
| S2 公共库 | 已完成（编辑器范围验收；实机/消费部署边界见记录） | [L01–L09](evidence/S2/acceptance.md)、[素材](evidence/S2/materials.md)、[机器证据](evidence/S2/validation.json) | 从现有 ContentHost/OptionRow 进入 S3，勿重建公共库 |
| S3 动态 Item | 已完成（编辑器运行范围） | [实现与D01–D05](evidence/S3/README.md)、[测试13/13](evidence/S3/tests-final.json)、[普通保存](evidence/S3/template-save.json) | S4使用最终三份List/PrefabRef契约；未做消费部署 |
| S4 PSD 与导出 | 已完成（BUI3/current-psd试点；效果边界见记录） | [P01–P07及交接](evidence/S4/README.md)、[产物摘要](evidence/S4/artifacts.json)、[校验](evidence/S4/validation.json) | S5消费真实BUI及必需EUI映射，不重新制作PSD或降级List |
| S5 BUI 制作 | 已完成（有限制作与编辑器运行范围） | [M01–M07/恢复/交接](evidence/S5/README.md)、[18项测试](evidence/S5/editmode-tests.json)、[普通保存](evidence/S5/template-save.json) | S6 从当前真实资产及 final 契约继续；不重放历史请求 |
| S6 试点与技能 | 已完成（明确映射与编辑器有限范围） | [F01–F07/限制/交接](evidence/S6/README.md)、[视觉](evidence/S6/visual-review.md)、[技能调用](evidence/S6/skill-invocation.md)、[普通保存](evidence/S6/template-save.json) | 使用当前 final 契约继续；真机/构建/消费部署及技能bundle发布未执行 |
| S7 模板与交付 | 本地实现完成，T01–T05通过；T06未验证 | [实现与交接](evidence/S7/README.md)、[验证](evidence/S7/validation.json)、[普通保存](evidence/S7/template-save.json)、[视觉](evidence/S7/visual-review.md) | ui-game最终Bump、技能bundle、提交/发布、真实消费部署分别待授权与验证 |

“已实现待验证”可用于工具代码完成但环境验证未完成的状态。只写文档不能将对应实施阶段改为完成。

## 已确认选择

| 事项 | 当前选择 |
| --- | --- |
| 后续执行环境 | Ember 开发仓库，通过项目中心切换 base |
| 切换授权 | 用户已明确允许；保护草稿后推进，不重复询问同一选择 |
| 练习对象 | 优先 base 主界面和设置页；动态 Item 用中性示例 |
| UI 信息规范 | 真实 Prefab/Binding 为来源，工具导出契约，手工只补语义和行为 |
| 美术输入规范 | 独立 PSD 交付规范，命名与导出能力同步落实 |
| 首版公共复用 | 准确组件引用和槽位映射优先 |
| 页面命名 | S1 将 Page 角色的旧 Panel 资产统一为 Page，弹窗类型通过 PageType 表达 |
| 初版视觉 | S2 制作 macOS 风格的中性浅色默认库，提供完整控件状态 |
| 初版素材 | 可使用 imagegen 生成并导入背景、空态等图片；基础几何和动态文本保持可编辑 |
| 项目换肤 | 保持规范名、绑定与行为，通过同一流程替换项目自己的素材与样式 |
| 产品路线 | 原色彩顺序 V2 和三个短关选择继续有效 |
| 真实 PSD | 尚未指定外部稿；S4 可用 S2 素材在 Photoshop 制作分层试点 PSD |
| Bump 与发布 | 未因编写本文自动获得执行授权；后续按明确范围处理 |

## 每阶段交接模板

### 2026-10-26 S0 实际交接

- 工程 HEAD：`5119adc2c142295f0367fde05c4b01d25b672c91`；Unity 6000.5.4f1、embedded com.ember 0.22.0；正式 base 0.8.4，hash `936dcb1d411afefcacd961cbad1d1d0d`。
- 用户确认切换是手动执行，旧模板不再追查；本轮不宣称恢复了切换前草稿。当前工作保护位置：`C:/Users/wuyu/My/ember-unity-framework/.utmp/ui-production-S0-20261026`，430 文件 SHA256 校验损坏项 0，另有完整 Git 状态/binary patch/运行前存储备份。
- 五份交接：[工程与保护](evidence/S0/01-workspace.md)、[能力](evidence/S0/02-capabilities.md)、[页面与迁移](evidence/S0/03-pages-and-migration.md)、[行为与截图](evidence/S0/04-behavior.md)、[提取候选](evidence/S0/05-extraction-candidates.md)。机器读取证据为同目录 prefab-inventory.json、runtime-observations.json、validation.json。
- 本轮主动 MCP 编译、域重载完成；六页 ValidateBinding 均无 issue。正式 FrameworkScene 启动，Main→Settings→Main→Gameplay→Settings→Gameplay→Main 实测通过，遮挡按钮点击无导航；四张 2560×1440 截图已保存。交互为真实 Button 事件派发，不冒充物理鼠标或人工试玩；未运行全量测试。
- 退出 Play 后只读全 Assets 摘要查询无有效结果，MCP 在间隔 30 秒的第一次重试恢复，最终无 error、两条 WebSocket warning。没有未决写入，不重放该大型查询；最终 Play=false、FrameworkScene 未标脏。
- 本轮新增的只有 evidence/S0 内容并更新本页；正式代码、Prefab、字体逐字节保持。base 快照文件与对应 Assets 文件差异 0。普通保存无必要、未执行；Bump/提交/推送/发布均未执行。四份 contracts 未修订。
- 下一阶段实际入口：先读 [S1](01-components-and-binding.md)。六页仍为旧 Panel 资产名；保留 GUID、Framework 模式、IsFramework 标记。同步 FrameworkScene 六处根名 override、EUICreationRequest 的 NewPanel 默认值、GamePages 注册及 Loading 测试旧路径，通过 EUI 中心再生成；不手改生成代码。S1 的契约导出、差异分类、命名迁移与相关验收均尚未实施。
- S2/S3 不要重复实现已有效的 TMPEx/EUIButtonEx/EUIImageEx/EUIToggleEx/EUIItemFactory；UIContainer/TabLoader/PagePreloader/TMPMarquee 仍是注释参考。候选详情见第五份清单。

将每轮完整结果保存到本目录 evidence 下的实际文件，并在阶段表中链接。不要创建空证据或预填通过。

~~~text
本轮阶段与日期：
执行工程绝对路径：
Unity、包来源/版本、代码 commit 与本轮差异：
正式模板身份、版本、父级、内容状态：
用户授权范围及仍待授权的动作：

任务前已有改动与保护位置：
本轮新增或修改文件：
本轮实际资产路径与 GUID：
旧名到规范名迁移、引用检查与兼容处理：
默认皮肤版本、图片来源/提示词、导入设置与预览：
正式 API/菜单/配置入口及用法：
契约/命名/BUI/插件的实际版本：

已实现能力：
已生成的资产和代码：
编译与域重载证据：
实际运行用例及过滤范围：
视觉/适配/行为结果：
失败或未验证项目：

模板普通保存：未执行 / 已完成，附中心结果
Bump：未执行 / 已完成，附实际版本
提交和发布：未执行 / 已完成，附实际来源
消费验证：未执行 / 已完成，附目标工程

已提交但结果未知的操作及查询办法：
恢复位置与方法：
下一对话从哪个具体步骤继续：
下一对话必须保留的资产、决定与限制：
~~~

### 2026-10-26 S2 实际交接

- 已实现公共库、默认皮肤和 EUI 开发中心实际接入；详细 [L01–L09 验收](evidence/S2/acceptance.md) 区分实现、验证及边界。8 个库条目、10 个新 Prefab，配置源 `Assets/Ember/Editor/UILibrary/DefaultLibrary.asset`；目录 `catalog.json` 已生成且当前有效。实际路径/GUID/Variant/Binding 见 [资产清单](evidence/S2/assets.json)。
- 菜单 `Ember/UI/UI 开发中心 → 公共库` 提供验证、目录生成、引用方查询及预览；创建页支持明确源骨架与插槽。正式 API：EUICreationService.TryBuildPlan/Create、EUIBindingEditorUtility、TryRegenerateCode；库 API 用法见 [公共库文档](../../../Packages/com.ember/Documentation~/ui/prefab-library.md)。不手改生成区。
- 两个结构源 EUIStandardPage / EUIStandardPopupPage 不生成类、不注册页面；EUILibraryPreviewPage / EUILibraryPopupPage 是实际 Variant。预览入口 `EUIManager.Instance.ShowMainPage(GamePages.EUILibraryPreviewPage)`，不接默认导航。EUIOptionRowVariantItem 单独验证资源继承与 Binding 继承；结构源不能作为类基类，失效继承会阻止生成。
- 默认皮肤 EmberDefaultLight 1.0.0，独立背景/透明空态由 imagegen 生成，提示词、导入与字体来源见 [素材记录](evidence/S2/materials.md)。字体复用 Liberation Sans，动态 TMP、项目字体皮肤4，原默认字体不改。正式资源已在 GameResource，无个人缓存依赖。
- base 主界面仅两个按钮改用嵌套库源；六页原 GUID、Framework/IsFramework、PageType 保持，主界面用户代码 SHA256 未变。Main/Gameplay 两条设置导航、开始/返回通过。S1 所有权示例保持，不冒充公共库。
- 相关测试56/56，job `360078f706604c7eb952624ebf196901`；最后代码编译 started1793013404725 / finished1793013407962 / domainReloadAfter1793013411822。最终 Console error/warning=0、Play=false、FrameworkScene 未标脏。四份契约连续导出diff=0，含实际 libraryEntries；已知序列化/未登记语义仍显式列在 unresolved。
- 真实验证包括实例文字/颜色独立覆盖、源字节保持、Item Variant 创建和释放、弹窗关闭/遮罩与 Raycast、0/100%进度、选中/禁用/按下、滚动列表、长文本、2560×1440及1024×768。交互是 EventSystem 事件派发和滚动位置设置，不称人工触屏/拖拽。最终 [组件预览](evidence/S2/preview-final.png)、[弹窗](evidence/S2/popup-final.png)。临时 GameView 尺寸已撤销。
- 保存前通过项目中心同一比较服务预览77项差异、0错误/警告；普通保存473文件，保存后差异0。base仍0.8.4 / framework0.22.0，contentHash=`a326eb5801392b8ebf71661c4a5ddd8d`，versionedContentHash仍`936dcb1d411afefcacd961cbad1d1d0d`。见 [保存结果](evidence/S2/template-save.json)。没有 Bump、Git 提交、推送或发布。
- 传播准备：platformer-2d、source3d-2p5d、visual-novel 仍指向 base 0.8.4 的旧封存 hash，未同步。将来封存后重点审查它们的主菜单 Prefab/Binding、GamePages.User 注册、字体目录/枚举、UIContracts 与 UILibrary；各模板自有字体皮肤ID与主菜单 override 可能冲突，不能批量覆盖。普通保存不是继承传播。
- 未验证：干净消费部署/构建、实体触屏与键盘辅助导航、竖屏/刘海实机、完整多语言/CJK视觉、派生合并。初始构建/排序/关闭修复历史和部分备份限制保留在验收文件，不以最终结果掩盖历史失败。
- S3 具体入口：先读 [S3 动态 Item 与容器](03-items-and-containers.md)，复用 EUIContentHostItem 的 `Viewport/Content`、EUIOptionRowItem.SetContent 与 EUIItemFactory；确定创建/刷新/移除/Dispose 的唯一宿主，补批量刷新和取消策略后做相关用例。不要提前实现 PSD/UXP/BUI/完整换肤，也不要重建这批资产或调用初始 Builder 覆盖 GUID。

## 证据保存约定

建议 evidence/S0、evidence/S1 等按阶段存放小型文字报告和截图索引。大型 PSD、PNG 集合与备份放项目工作资料或指定位置，记录绝对路径、版本/摘要与获取方式。消费交付采用可携带来源，不依赖个人机器。

保留已完成的真实结果；新输入到来后标明哪些结果失效并补验，不覆盖历史失败以伪装一次通过。未知请求先查询，下一对话不能通过换任务 ID 重放同一写入。

## 本次文档交付检查

初稿交付时共 14 份 Markdown，42 个本地链接与 11 处提示词文件路径检查通过，45 个验收编号无重复。旧色彩顺序实施包已补入前置入口。

2026-10-26 本轮增加 Page 命名迁移和默认皮肤两份规范，更新相关阶段、提示词及验收。现共 16 份 Markdown，55 个本地链接与 11 处提示词文件路径检查通过，八个阶段提示词齐全，52 个验收编号无重复。框架模式审计在本目录内无问题，仓库其他位置的 65 个既有链接问题前后相同，没有新增。

文档修订不代表实际改名、生成图片、制作皮肤或通过 Unity 验收。原有文档在修改前已备份，已有项目资源与模板切换现场保持不变。

### 2026-10-26 S1 实际交接

- 本轮只完成 S1；未进入 S2 美术/公共库、S3 容器或 S4–S7。HEAD 未改变，未提交/推送/Bump/发布。原有模板切换造成的大量工作区删除及四份字体资产修改不回退、不混入新功能。
- 工作保护：`.utmp/ui-production-S1-20261026/files`，变更列表见 [backup-diff.txt](evidence/S1/backup-diff.txt)。六页 .cs（以及 LoadingSettings）除生成头 prefab name 外与备份内容一致，见 [用户代码保护](evidence/S1/user-code-preservation.json)。
- 六个 Common/Prefabs 页面已改为 EUIBackgroundPage、EUIMainPage、EUIGamePlayPage、EUISettingPage、EUILoadingPage、GMPage，全部 GUID 保持。FrameworkScene 根名、Binding、注册、生成头、Loading 测试和当前参考文档同步；内部 Panel_GM 保留。迁移表与历史例外见 [迁移证据](evidence/S1/02-migration.md)。不要重新 LoadTemplate 或再次做 Panel 全仓替换。
- 框架修补：EUIBindingSnapshot 补模式/遮罩/过渡/IsFramework/空引用保留；EUIInstanceOwnership 阻止同一对象双逻辑；EUILogic 命名子逻辑表与生成器 GetChildLogic 修复了实际无法编译的 UILogic 字段生成。父级释放逐子逻辑处理，单子异常不跳过其余释放。
- 正式契约 API：`EUIContractService.Export/Compare/Write/GetSupplementPath/WriteSupplement`；菜单 `Ember/UI/UI 开发中心 → UI 总览`。schemaVersion=1、toolVersion=1.0，正式字段与边界见 [包内 API](../../../Packages/com.ember/Documentation~/ui/eui-contract.md)。只读导出、差异分析与语义登记已实现，自动应用替换仍属 S5。
- 人工语义唯一来源：`Assets/Ember/Editor/UIContracts/9b27fbe343055e94fb5f3ae24a87ee96.json` 与 `13157152ce048b740bd1e740a35951af.json`；包含稳定定位、行为证据、历史名称。导出文件位于 evidence/S1 下的 Main、Setting、OwnershipExample 三份 `.eui.contract.json`，最新连续导出均 diff=0、blockers=[]。资源本体摘要可识别同 GUID 图片内容更新。
- 正式生成示例：`Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUIOwnershipExamplePage.prefab`、`EUIOwnershipLeafItem.prefab`，用户逻辑在 `Assets/Game/UI/Runtime/UIFoundation/`。通过创建中心及再生成 API 完成；示例页不进入默认导航。正式入口 `EUIManager.Instance.ShowFreePage(GamePages.EUIOwnershipExamplePage)`。固定子实例走 UILogic，动态实例走 Extension/UnityEngine.RectTransform + EUIItemFactory，不重复所有权。
- 验证：相关三类测试 52/52；最后契约资源摘要补充后 EUIContractEditTests 再测 7/7（job `fd249ffc2c1b4d3f89cbefd9d40a1667`，4.318s）。最终 MCP compile started=1793011172739、finished=1793011181800、domainReloadAfter=1793011187201；最终 Console error/warning=0。非全量测试。
- 真实 Play：Main/Gameplay 两种 Settings 来回导航、GM 展开/Toggle/时间缩放恢复、Loading 覆盖与揭幕、两种所有权 Init/Open/Dispose 各一次均通过。最后退出 Play，FrameworkScene 未标脏。截图和边界见 [运行验收](evidence/S1/03-validation.md)。示例首次位置与主按钮重叠已调整，最终视觉为 ownership-1.png。
- 曾短暂关闭目标工程，工具误路由一次 0 用例补测至 Unity Farm，不计本轮验收；没有修改其源码。目标在第一次间隔重试恢复，重新确认路径与程序集后续验，详见运行记录。
- 项目中心普通保存最终返回 399 文件；base 仍 0.8.4 / frameworkVersion 0.22.0，contentHash=`d5621e6ed041f733d54e76c3cb5c80c5`，versionedContentHash 仍 `936dcb1d411afefcacd961cbad1d1d0d`。普通保存不等于版本封存。派生模板与消费项目未同步/部署，不手改其快照。
- 四份 contracts 的建设路线未改变；仅给 eui-contract.md 补入实际 API 权威文档链接，避免模型说明与实现脱节。默认皮肤和 PSD 规范保持，尚无 S2 图片或 S4 PSD。
- 下一阶段：读取 S2 详细文档、default-skin 与 eui-contract；使用已实现的槽位、身份与语义导出建立真实公共 Prefab/Variant 库。S2 补 family/允许覆盖范围、皮肤标识、长文本/分辨率/状态视觉；未启用的 UIContainer/TabLoader 不可当作现成 API。新建 UI 继续用 EUI 开发中心，默认资源归 base，产品玩法留消费项目。




### S2 视觉纠正（用户反馈后）

此前对圆角和九宫格的通过结论过宽，已按实际缺陷修正。新对勾使用 imagegen 独立透明图片；圆角换新制高分辨率素材，普通控件与12高进度条分别校准。皮肤1.0.1，base普通保存477文件，当前hash 597adad19e785452af360455243b8983，版本未Bump。标题槽位误指向选中标记也已修复。详情、提示词、截图和未复测边界见 [视觉修正](evidence/S2/visual-fix.md)。后续图标优先正式新素材，尽量不用 Unity 内置图或字体符号代替。

### 2026-10-26 S2 公共组件补齐

- 用户授权补齐此前 Burner 分析清单，但不进入 S3。新增26个 Item、2个 Popup Page 和1个预览 Page；公共库现37条（36项可复用资产+1项演示页），皮肤1.1.0。按钮变体、标签/分段、开关/设置行、Slider/Stepper、输入/搜索、下拉、徽标/卡片/空态、Toast/Tooltip、标题、横向/网格宿主已落盘。详情见 [扩充交接](evidence/S2/expanded-library.md)，[资产与GUID](evidence/S2/expanded-assets.json)。
- 正式创建/绑定/生成均走公开服务；新增通用展示行为在框架包，默认资源及演示逻辑在base。没有动态数据容器、对象池、TabLoader或虚拟列表。原主菜单不新增导航入口，原六页资产/用户逻辑未被本次扩充修改。
- 新增8张imagegen透明图标，复用已有背景、空态、对勾和高分辨率圆角素材；提示词、用途、导入与SHA256见 [素材记录](evidence/S2/expanded-materials.json)。实际发现并修复滑杆手柄拉长、Tab选中底板不伸展及默认选中末项问题。
- 已验证：37条目录校验无错误且摘要当前；完整预览契约连续导出差异0；公共库测试8/8、新控件测试修正夹具后4/4；Play重复启用、单次事件、只读清除、禁用选项、弹窗回调、Toast到期、长正文和1024×768。最终编译started1793018221489 / finished1793018224987 / domainReloadAfter1793018232086。失败历史和准确边界见 [验证证据](evidence/S2/expanded-validation.json)。
- 普通保存base成功567文件，保存后差异0；base0.8.4 / framework0.22.0，contentHash=20a0b4a5bfb39b175a5ca567ed4229b9，versionedContentHash仍936dcb1d411afefcacd961cbad1d1d0d。[保存结果](evidence/S2/expanded-template-save.json)。没有Bump、提交、推送、发布或消费部署。
- 未验证：干净消费部署/构建、实机触控/IME/无障碍、完整键盘/多语言/CJK、竖屏刘海及派生模板传播。固定下拉选项和固定短提示面板不是任意动态内容系统。最终退出Play、FrameworkScene未标脏、临时GameView尺寸移除。
- S3尚未启动。后续如获授权，按03文档从三个Host的Viewport/Content、IconCard展示接口和EUIItemFactory继续；不要再次创建覆盖本批GUID或重新加载base。

### 2026-10-26 公共库 Odin 面板与蓝图模板隔离

- 公共库改为 Odin 分类搜索树和单项只读详情，槽位分页、覆盖范围折叠，保留真实配置/目录服务。框架 FSM 蓝图统一放 `Ember/FSM/蓝图观察器`，按模板能力和模块条件显示；base 默认隐藏，旧窗口和公开操作同样拒绝跨模板执行。
- 修正文档对“框架公共工具”的可见性豁免；根 CLAUDE 与包内业务隔离规则链接到正式能力接入契约，要求后续 AI 显式声明所有者、模块条件并验证全部入口。
- 已完成本轮 MCP 编译/域重载与 Console 检查；相关23项测试最终通过，base 菜单隐藏与条件动态增删已实测。Odin 完整截图及交互、初次失败和未验证边界见 [面板与隔离交接](evidence/S2/editor-panels.md)。
- 只改包内编辑器、测试和文档，没有新资源/GUID/Binding/皮肤变化；本轮不再保存 base，沿用前次567文件普通保存。未实际切模板或部署消费项目；未加载模板的旧接线显式 owner/module 迁移留待其 Assets 维护源加载后处理。S3未开始，没有Bump、提交、推送或发布。


### 2026-10-26 S3 动态 Item 与容器

- 已完成单模板纵向动态列表：EUIDynamicListHost / EUIItemList<T> / EUIItemLease<T>，复用EUIItemFactory与BeginUse/Show/Hide/ResetForPool/Dispose。整表重新绑定、稳定key和显示index分离、重复key拒绝、小规模有界闲置缓存、借用事件清理/取消/迟到回调保护。不是增量差分或虚拟列表；没有多模板、页签和预加载改造。
- 正式创建三个Variant：EUIDynamicListPage、EUIDynamicListItem、EUIDynamicOptionRowItem，位于Module/UIFoundation；原OptionRow真实路径也在此模块，GUID不变。创建/绑定/生成走EUI中心，用户逻辑保留Runtime层级，页面注册由生成器更新。原六页、公共源和37条库配置保持；新增文本使动态TMP字体图集补入字形。无新图片、皮肤仍1.1.0。
- 入口：Play后 EUIManager.Instance.ShowMainPage(GamePages.EUIDynamicListPage)，不接原默认导航。三个最终 *.eui.contract.json 在evidence/S3；schemaVersion1/toolVersion1.1，requiredCapabilities=dynamic-list-v1，lists记录List/PrefabRef、真实GUID/Content/Viewport、数据绑定与所有权。正式API、所有权、调用示例及S4/S5输入见 [交接](evidence/S3/README.md) 和 [包内规范](../../../Packages/com.ember/Documentation~/ui/dynamic-lists.md)。
- D01–D05通过本轮编辑器运行验收：0/1/3/20增减清空，重排命中当前key且一次回调，禁用项0回调，三次20行打开关闭创建/销毁各20、订阅归零；状态复用与300ms迟到图片回调拒绝；长短行动态高度、真实Scroll事件和裁剪射线；宿主Destroy兜底释放。交互为自动EventSystem派发，不称实体输入或人工试玩。
- 最终相关13/13测试，job36e1675d77e74abf97d8e5895630f3ed。Back回调最后单独修正为ClosePage(Page)，再次MCP编译started1793021928156 / finished1793021930701 / domainReloadAfter1793021935730，Play复验恢复EUIMainPage且3实例全部释放。最终Console错误/警告0，Play=false，FrameworkScene未标脏。初次夹具、行宽/重复Icon、Back失败及修复记录均保留。
- 真实资源为EditorUIResourceProvider/AssetDatabase加载，容器独立资源句柄0；模拟句柄只验证清理回调，不冒充打包资源全链路。未验证干净消费部署、构建、Addressables、实体输入、全部比例/多语言或20行以上性能。
- 项目中心普通保存585文件，比较差异0；base0.8.4/framework0.22.0，contentHash=c58c6c5ed41b18efc8fe38fd4999dd5f，versionedContentHash仍936dcb1d411afefcacd961cbad1d1d0d。未Bump、提交、推送、发布或消费升级；没有切换模板和修改蓝图隔离。下一阶段是S4，本轮未开始PSD/UXP或S5制作器。


### 2026-10-26 S4 PSD 规范与 UXP 导出

- 已在Photoshop27.5.0制作原生分层源稿和独立工作稿，保留8个可编辑文本及组件/Item/状态组；素材复用S2，未引入Burner产品美术。本轮只覆盖主界面两个按钮与动态列表/行模板，未扩展完整换肤。
- 框架分发位于 `Packages/com.ember/Extensions~/ember-psd-to-bui`：Ember UXP1.0.0，来源实际解析的prefabmaker2.0.0-preview.17/UXP3.0.14；命名1.5.0、文件桥1、BUI3。源码UDT加载与真实导出已通过，CCX/发布/干净消费安装未做。
- 最终输入 `art/ui/S4/EmberPilot.bui` + `EmberPilot.eui-map.json` + BUI准确引用PNG；3个Target、24节点、8张资源PNG及4张可用文字参考。原始BUI来自UXP，动态生命周期由独立映射绑定S3最终契约，不伪造BUI字段。真实主界面契约本轮由正式API重新导出。
- P01–P07在试点范围完成，细分真实PS、只读检查及模拟故障见 [S4验收](evidence/S4/README.md)。修复了Rename显示隐藏层、白底图标对比度、SecondaryButton公开槽位ID不一致；失败和首轮结果保留。九宫256设计框→130PNG/64四边+2中心，四角像素差0。
- 相关15项Node门禁/桥接测试通过；本轮Unity MCP主动编译/域重载、Console检查证据见S4记录。无Unity运行代码或Assets改动，未重复S3 Play回归或全量测试。真实桥接超时后query原ID完成；过期输入写前失败；重复原请求不重放，现无未决写入。
- 隐藏文本合成参考缺失警告保留；状态预览由PS临时复制稿生成。原生阴影/描边/遮罩、智能对象、完整多语言/TMP还原、人工面板全交互、Unity制作与全链路换肤未验证。只读门禁不是完整通用JSON Schema引擎，未知关键语义和能力仍阻断消费。
- 仅工具、外部美术和文档变化，不需要项目中心重存；base0.8.4/framework0.22.0与前次585文件/hash保持。未加载模板、未Bump/提交/推送/发布/消费升级；保护既有删除和其他修改。S5尚未实施。


### 2026-10-27 S5 BUI 制作

- 新增单包内 EmberBuiService 1.0.0 和 UI 开发中心“BUI 制作”入口，支持 BUI3 + 必需 EUI map1/contract1 tool1.1，实际能力 ember-eui-map-v1 / dynamic-list-v1。Analyze、隔离 Preview、Apply、VerifySaved、ResumeGeneration 共用 API；正式创建、绑定、代码生成均调用既有服务。详见 [包内说明](../../../Packages/com.ember/Documentation~/ui/bui-maker.md)。
- 真实 S4 BUI/PSD/原 map 不变，精确校验8张资源PNG/4张文字参考，仅按范围导入6张。主界面、动态页、行模板原位保存，保留页面GUID/Framework模式/用户逻辑/动态所有权；设置按钮由Action切Secondary，公开槽label→Label，内部local ID迁移已记录。共享源与原用户代码SHA256未变。
- 三个 EUIBuiCandidate* 候选经创建中心建立为真实源Variant，正式生成并继承原逻辑；候选列表宿主切到候选Item，旧资源保留、默认导航不变。真实Play确认候选行逻辑；不宣称全工程消费者自动迁移。
- M01–M07及局限见 [S5完整证据](evidence/S5/README.md)。首次嵌套重命名、BOM摘要、零缩放Canvas与安全区坐标、外层Variant公开槽失败均保留记录并修复复测。未知结果先查真实产物、导出当前契约后定向恢复，没有重放创建。隔离Preview是构建诊断，驻留视觉预览窗口尚不支持。
- 相关18/18 EditMode通过，非全量；最后新增语义门禁/VerifySaved后主动MCP编译 started1793033009138 / finished1793033013161 / reload1793033019445，随后真实负例验证陈旧计划/非法绑定/缺失输入/未知能力/重复身份/关键选项均阻断且正式文件未变。无编译/产品错误；保留一条MCP自身WebSocket未初始化warning，后续调用正常。
- 真实Play验证3项重排key/index、旧lease失效、单次点击、20→0→3缓存和单订阅；Settings按钮上下文为主界面、Start进入玩法页、Back事件保留。截图见S5。字体仍保留工程资源，不宣称ArialMT映射或完整效果/多语言还原；长文案保持EUITextRowLayout，动态Icon/Progress未烘焙。
- 项目中心普通保存617文件，比较差异0。base0.8.4/framework0.22.0，contentHash=846454530d463521510735d5e796ce43；versionedContentHash保持936dcb1d411afefcacd961cbad1d1d0d。未重新加载、Bump、提交、推送、发布、消费升级或手写模板快照；既有修改/删除保留。
- S6下一步使用当前正式资产、final契约及包内API开展完整换肤与字体/效果/更多比例验收；不要重跑已被后续合法改动取代的旧请求。候选请求原writing回执是历史记录，结果已由candidate-recovery completed查明，当前无未决写入。

### 2026-10-27 S6 完整试点与技能

- 主界面/动态页/行模板从有效S4 BUI与S5正式资源继续，重新导出当前契约核对连续一致；未重放历史Apply。设置页新增真实分层source/renamed PSD、UXP BUI/PNG导出、Maker Analyze/Preview/Apply/VerifySaved与正式生成。随后独立工作稿将关闭按钮蓝色改青色，增量路径完成；首轮字小通过新layout请求将参考分辨率调为1280×720。
- 扩展框架唯一维护源 `.agents/skills/ember-eui-build` 的触发和美术续接参考，实际调用五种起点流程；Skill验证通过。正式模板使用说明随base保存。未手写AISkills~，需获准提交后由正式bundle脚本生成；不把源码验证冒充发布安装。
- F01–F07在明确映射与编辑器范围完成，详见 [S6证据](evidence/S6/README.md)。主界面三轮设置、遮罩射线阻断、Start进玩法、玩法设置来源与恢复、动态工具栏/Back均实际执行。194字符长文案、禁用0回调、普通1回调、选中、滚轮、重排lease失效、20→0→3复用及重复开关释放通过。
- 1280×720/1600×720实际截图与PS参考逐项核对；保留中文、既有TMP、公共控件和动态容纳框，非Arial映射或逐像素还原。安全区覆盖完整Screen.safeArea及左右60/上下24布局模拟，不声称异形屏真机、全语言或原生效果通过。
- 独立EUIBuiCandidateMainPage保存深青色/薄荷按钮皮肤，引用新切图、公开槽覆盖；原名称/Binding/继承逻辑不变，实际Settings/Start通过，默认源未变，正常导航仍用EUIMainPage。
- 本轮相关EditMode18/18，job3340404ae3684ccd8c3ce9b0b870dbb5。最终生成后MCP编译started1793035204368/finished1793035206469/reload1793035210871；最终Console错误/警告0。会话中转场Busy和MCP WebSocket warning如实保留，未决写入0。退出Play、FrameworkScene未标脏，原GameView设置已恢复。
- 项目中心普通保存627文件、DifferenceCount=0；base0.8.4/framework0.22.0不变，contentHash=f5ae9bcc0e3bd361a65b056b367cda71，versionedContentHash仍936dcb1d411afefcacd961cbad1d1d0d。S4原输入、42个保护文件及1159条既有删除状态保持，生成差异仅两个Binding时间戳。
- S7未开始：没有Bump、提交、推送、发布或消费升级。接续用S6 final契约、当前settings-layout回执、实际S4/S6输入及正式说明；旧回执因后续合法修改失效时不得改摘要或盲目重放。构建、干净消费部署、CCX与技能bundle发布、真机输入仍待对应阶段。


### 2026-10-27 S7 UI 游戏模板与交付准备

- 按本轮明确授权保护1520个既有文件，1159条原删除全部保留；备份 `.utmp/ui-production-S7-20261027/before`，恢复须按manifest选择路径。初始base0.8.4普通保存627文件/差异0，内容尚未封存；正式中心minor Bump至base0.9.0，内容及封存hash均f5ae9bcc0e3bd361a65b056b367cda71。其他三个派生仅预览未同步，均含父更新冲突。
- 正式CreateTemplate(parent=base)再LoadTemplate创建ui-game0.1.0/preview，parentVersion0.9.0、父内容和快照一致。中性三步UiGameModule采用Gameplay生命周期；正式主页/局内/结果Business Variant、嵌套公共按钮、动态宿主和lease；沿用Settings/SceneFlow，完成暂停、结果、重开、返回及迟到请求保护。没有新增框架Manager或ColorOrder产品规则。
- 正式说明进入Assets/Game/Documentation/ui-game，含运行/比例、所有权、存储边界、素材/字体、契约/PSD/BUI、回执与升级保护；不依赖开发证据目录。公共库37项有效、111项资源依赖合法、旧四对象GUID/Binding/用户逻辑保持；新增文字产生TMP动态字形数据，原S4/S6素材未改。
- T01–T05在本地编辑器范围通过，T06未验证。最终相关EditMode4/4，job38dcf612dbdc4e80960ad691f0f36db3；本轮最后主动编译started1793037580960/finished1793037583440/reload1793037588193。正式启动、同帧双击、设置遮罩与暂停恢复、重开、三轮进出通过：活动页3/3/3，行实例每轮创建销毁3/3，最终会话创建释放7/7。
- 实际720×1280和720×1600截图逐图检查，含完整安全区及左右24/上下40运行实例padding模拟。操作为EventSystem派发/少量清理Button调用，不称实体输入；未做真机、构建、全语言、横屏、消费部署。首次固定等待测试提前读页失败、修正后的完整复测、CS0114修正和GameView清理索引错误均保留，当前无未决操作。
- 中心保存前53项预期差异，普通保存675文件，保存后DifferenceCount=0/ErrorCount=0，唯一警告为尚未最终封存。ui-game0.1.0/framework0.22.0，contentHash=98470c704b0e51b0d40df193b8b8a088，versionedContentHash仍f5ae9bcc0e3bd361a65b056b367cda71。已退出Play、FrameworkScene未标脏，原GameView尺寸与列表恢复。详见[S7完整证据](evidence/S7/README.md)。
- 未执行ui-game最终Bump、Git提交/推送/tag/发布、技能bundle或消费部署。发布前使用真实sourceCommit生成bundle并封存模板；指定消费工程后执行T06，再按已确认V2先三个短关路线进入产品阶段。本地S7完成不等于跨项目交付通过。

### 2026-10-27 S8 base 全量 UI 组件化迁移

- 用户明确追加 S8，包含 GM，保留功能/绑定，完成备份、验证与普通保存，暂不发布。先备份 2927 文件并 SHA256 核对，再正式加载 base；原 1159 条删除保持，ui-game 和其他派生未同步。
- 61 个 UI Prefab 中 60 个带 Binding，公共库 37→44 项；补齐 Text/Image/RawImage/CircleProgress/FreeScrollHost/TransitionSurface/ContinuousSlider StructureOnly Item。迁移常规页面、GM、所有权及库示例中的独立控件，原生渲染仅作为公共组件源和框架程序化效果的实现。复用既有 macOS 风格素材，无新增图片/PSD需求，无旧 BUI 重放。
- 原 53 对契约的 GUID、模式、字段声明/Framework/语义、自定义设置和用户逻辑摘要一致；最终绑定、库、缺失脚本、业务独立渲染控件均为 0 错误。17 对象经公开生成入口，MCP 编译 started1793065373389/finished1793065379215/reload1793065384163；无编译错误，短暂 MCP WebSocket 警告后调用正常，最终 Console 空。
- 相关 EditMode 18/18（库/契约 16 项，加转场 2 项）。主/玩法/设置往返、GM 13 字段及时间还原/FPS/拖拽/双向滚动/增强控件/实际退出、弹窗回调、动态列表和所有权、候选继承、普通/快速加载与转场有运行证据；1280×720/1600×720和安全区 padding 模拟完成。首轮引用/视觉问题已修复；工具脚本的相机、等待、转场池、主页面栈及 GameView 索引假设修正如实保留，见[S8证据](evidence/S8/README.md)。
- 项目中心普通保存 641 文件，DifferenceCount=0/ErrorCount=0，唯一警告为未封存。base0.9.0/framework0.22.0不变，contentHash=10a068917ab92c18c53f948abd556734，versionedContentHash仍f5ae9bcc0e3bd361a65b056b367cda71。1574 个保护文件中 1573 个最终一致，另 1 个为回归用例；原本干净的转场源码另以 HEAD 基线补充备份，包内临时字体图集恢复本轮备份，模板字体动态字形正常保存。
- 已退出Play、FrameworkScene未标脏，GameView原索引5/尺寸数量10恢复；归档清理脚本日志后最终Console空。未Bump、提交/推送/tag/发布、真机/构建/消费部署或派生同步。下次继续以当前base契约为准，ui-game仍保存S7状态。

- 最终视觉复核修复 EUITransitionBlock 在父容器缩放后复用旧网格的问题，同一实例两比例各 153 点覆盖采样全部通过；Loading 百分比与 Ownership 正文对比度已调整。最终截图使用 delivered-gm-*、delivered-loading-final.png 和 delivered-ownership-1280.png。

### 2026-10-27 S8 补充：Loading 进度条职责收拢

- Loading 改为嵌套公共 EUIProgressItem 的真实 Variant EUILoadingProgressItem。填充、百分比和独立显隐由 Item 管理，页面保留加载时序、准备门槛与转场。原 Image/Text 字段正式迁为 LoadingProgress UILogic，父级唯一拥有；页面用户钩子未改。
- 相关 PlayMode 13/13，最后夹具兼容调整后 5/5 复测；主动编译、域重载和真实 37% 运行截图通过。结构 62 Prefab/61 Binding/44 库项无错误。
- 备份 361 文件；普通保存 652 文件，差异/错误 0。base0.9.0，contentHash=8467209c7cd7d3d9542293050b4c774e；未 Bump/发布/派生同步。详见 [补充交付记录](evidence/S8-loading-progress/README.md)。

### 2026-10-27 S8 补充：组件职责与派生同步

- base 再补九个专用 Variant，将 GM 倍率/状态/增强 Toggle、设置上下文、动态摘要与进度等表现职责交给组件；页面保留业务动作。相关 EditMode16/16、运行检查12项及候选检查4项通过；71 Prefab/70 Binding/44库项无错误。详见[组件职责验收](evidence/S8-component-responsibilities/README.md)。
- 用户随后要求逐个更新依赖模板。备份6318文件，经项目中心本地封存 base0.10.0，hash61cbe49aa797b1604f293b9756674566。ui-game0.2.0、platformer-2d0.12.1、source3d-2p5d0.6.0、visual-novel0.22.1全部指向该父基线并封存，正式父同步状态均Synced。
- 保留各模板玩法模块、场景和定制页面；对齐旧Panel资源路径且保持GUID，补候选继承字段。小说额外保留读档遮幕、取消、语言、偏好和皮肤稳定键，追加父字体映射。直接相关测试为ui-game4项、平台13项、3D13项、小说13+7项，全部通过，非全量或完整玩法验收。
- 已切回base，最后MCP主动编译/域重载完成、Console空；项目比较差异0、错误0。中心8条测试状态未关联场景提示保留。没有提交、推送、tag、发布、构建、真机或消费升级。完整冲突取舍、备份、失败续接和验证见[父模板同步记录](evidence/S8-parent-sync/README.md)。

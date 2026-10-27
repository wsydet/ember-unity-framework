# 各阶段对话启动提示词

这些提示词供用户复制到新的对话。文件中的提示词本身不启动执行；用户发送对应指令后，执行者按所选阶段推进。建议一轮只执行一个阶段，完成后将记录写回本目录。

所有框架阶段工作目录为 C:/Users/wuyu/My/ember-unity-framework。参考项目仅用于只读研究，默认不得修改 C:/Users/wuyu/Project/burner1。

## 首次启动或从断点继续

不知道上次停在哪里时，使用这一段。它要求根据真实进度推进一个阶段，避免重复实施。

~~~text
请在 C:/Users/wuyu/My/ember-unity-framework 继续 UI 组件化与美术替换基础建设。

先读取 AGENTS.md、CLAUDE.md，以及 docs/dev/ui-production-upgrade/README.md、10-progress.md、08-acceptance.md 和当前待执行阶段文件。按其中入口读取适用的公共规则与实际源码。

根据真实文件和交接记录判断最近未完成的阶段，本轮完成该阶段及必要前置修复，实施后验证并更新交接。没有实施记录就从 S0 开始，不把文档编写当作阶段完成，不重复创建已有成果。

我已明确允许通过项目中心切换到 base，并使用 base 的基础 UI 作为练习。先保护当前模板草稿和已有修改，再正式加载；已经在 base 时核对并补齐缺少的证据，不要重载覆盖。

规范 EUI 所保存的信息与美术 PSD 输入是核心工作，请以 contracts/eui-contract.md 和 contracts/psd-art-spec.md 为共同依据。正式 UI 通过 EUI 中心创建和生成，通用能力归包，默认资源归 base，产品业务留在消费项目。

另外读取 contracts/ui-naming.md 和 contracts/default-skin.md。S1 实际统一 Page 命名并迁移旧 Panel 资产与引用；S2 完成 macOS 风格浅色默认组件库，允许使用 imagegen 生成并导入所需图片。后续项目通过同一替换流程改成自己的视觉，保持绑定与功能。

本轮按当前阶段完成实现、相关验证以及必要的项目中心普通保存；不要直接编辑模板快照或 PackageCache。Bump、提交、推送、发布和覆盖性消费部署仅在已有明确授权时执行，未授权则先准备准确结果与差异清单。

最后更新 10-progress.md，提供实际 API/菜单、资产、验证结果、未完成项和下一阶段入口。
~~~

## S0 核对并切换 base

~~~text
请执行 C:/Users/wuyu/My/ember-unity-framework/docs/dev/ui-production-upgrade/00-baseline-and-base.md 的 S0。

先读同目录 README.md、08-acceptance.md、10-progress.md，以及工程 AGENTS.md、CLAUDE.md 和适用公共规则。本轮完成现场核对、原工作区与草稿保护、通过项目中心加载 base、基础 UI 运行基线和能力清单。

我已允许切换到 base，不要重复询问同一选择。先保留原模板的未保存内容，加载后检查正式身份；不得手改模板快照、hash 或编辑身份文件。当前活动模板和版本以现场为准。

优先确认主界面、设置页及相关基础流程；EUIMainPanel、EUISettingPanel 是迁移前旧名，现场已迁移时按真实 GUID 与记录定位。记录按钮、导航、关闭返回和绑定，并按 contracts/ui-naming.md 准备全部 Page 迁移清单。区分有效 UI 组件与注释参考代码。本轮不开展批量 UI 重构，不 Bump 或发布。

通过 Unity MCP 完成本轮必要编译与相关基线验证，按规范处理断连。将证据、备份与恢复位置、下一阶段输入写回 10-progress.md。
~~~

## S1 组件与绑定契约

~~~text
工作目录：C:/Users/wuyu/My/ember-unity-framework。
请执行 docs/dev/ui-production-upgrade/01-components-and-binding.md 的 S1。

先读 README.md、10-progress.md、08-acceptance.md、contracts/eui-contract.md、contracts/ui-naming.md 和适用工程规则，核对 S0 已完成且实际编辑 base。

本阶段实际将 base 的六个页面从 Panel 旧名统一为 Page，弹窗也遵循 Page 命名并保留 PageType。通过 Unity 正式接口保留 GUID，同步根名、Binding、生成来源、注册和引用，确保以后生成不会恢复旧名；不要全仓替换 Panel 或手改生成文件。

在现有 EUIBinding、EUIItem 和生成器上完善基础控件、嵌套所有权、信息导出与绑定差异检查。优先复用 TMPEx、ButtonEx、ImageEx、ToggleEx 等有效实现，不整批启用注释参考代码。

EUI 信息从真实 Prefab、Binding、生成结果和明确行为说明提取；生成描述不成为手工维护的第二份结构。检查框架标记、代码模式、继承、类型和资源引用能否无损保留。页面重新生成后，用户区和美术布局必须保持。

实现并执行阶段内相关编译与验收；需要生成正式 UI 时使用 EUI 中心。模板 Assets 变化经项目中心普通保存，Bump 和发布单列。更新进度，留下实际导出/API、模型字段和下一阶段可用能力。
~~~

## S2 公共预制体库

~~~text
工作目录：C:/Users/wuyu/My/ember-unity-framework。
请执行 docs/dev/ui-production-upgrade/02-prefab-library.md 的 S2。

先读 README.md、10-progress.md、08-acceptance.md、contracts/eui-contract.md、contracts/ui-naming.md、contracts/default-skin.md 和工程规则，核对前置 S1 的真实结果。

以 base 基础 UI 为来源建立中性的 Page、Popup 和必要局部容器骨架，以及按钮、Toggle、进度条和 OptionRow 等最小公共库。优先复用已有同职责资产。规范组件 family、稳定 ID、公开槽位、状态与允许覆盖属性。

正式页面和弹窗资产都以 Page 结尾，轻量行以 Item 结尾。按 macOS 风格完成浅色默认皮肤和组件预览，本阶段允许使用 imagegen 生成并导入背景、空态等所需图片；保留真实 TMP 文本、状态和可伸缩结构。交付能直接用的初版，不只交空骨架或整页效果图，也不等待外部 PSD。

扩展现有 EUI 开发中心的计划和创建服务，使页面使用真实 Prefab Variant，组件使用真实嵌套引用，并分别核对 Binding 继承。不要绕开中心制作另一套注册和生成链。

库配置需能随 base 分发；第一版准确引用和显式槽位优先。用两个组件实例证明覆盖互不影响且不修改公共源，再对主界面有限范围改造并验证原功能。

完成相关 Unity 验证和项目中心普通保存。保留其他模板草稿，只列出派生影响，不擅自批量同步或发布。更新真实资产、GUID、配置和 API 交接。
~~~

## S3 动态 Item 与容器

~~~text
工作目录：C:/Users/wuyu/My/ember-unity-framework。
请执行 docs/dev/ui-production-upgrade/03-items-and-containers.md 的 S3。

先读 README.md、10-progress.md、08-acceptance.md、contracts/eui-contract.md 和工程规则，核对 S1/S2 的实际接口及资源。

复用 EUIItemFactory 与 EUIItem 生命周期，为公共 EUIOptionRowItem 建立最小可用的动态容器，支持数据刷新、增减、清空和正确释放。原生 ScrollRect/LayoutGroup 能满足的能力先复用，池化、页签按试点需求实现并明确边界。

实例、资源、逻辑和回调各有唯一所有者；动态 Item 不同时由父级 UILogic 和工厂创建两份逻辑。重用前重置状态，旧异步结果不能影响新数据。

用独立的中性示例验证，不为演示给 base 正式设置页添加未知业务。执行阶段规定的小范围测试和 Unity 编译/运行，必要资产经中心普通保存，记录真实容器 API、生命周期和后续导入所需语义。
~~~

## S4 PSD 与 UXP

~~~text
工作目录：C:/Users/wuyu/My/ember-unity-framework。
请执行 docs/dev/ui-production-upgrade/04-psd-and-export.md 的 S4。

先读 README.md、10-progress.md、08-acceptance.md，以及 contracts/eui-contract.md、contracts/psd-art-spec.md 和工程规则。

只读研究 C:/Users/wuyu/Project/burner1/burner/client/game 中当前解析的 Rename、UXP 与 BUI 协议。将可复用能力适配到 Ember，明确命名规则、导出器和消费者版本关系，不复制 Burner 产品路径或依赖缓存作为最终方案。

以 base 主界面/设置页和公共组件为目标，读取真实 EUI 契约与旧行为，规范美术 PSD 的结构、状态、动态文本、切图和九宫格。Rename 修改工作副本，保留源稿；正式语法以已验证的导出器支持能力为准。

优先使用本对话或交接中的有效 PSD。没有外部稿时，按 contracts/default-skin.md 用 S2 素材和 EUI 契约在 Photoshop 制作真实分层试点 PSD，再执行 Rename/导出；保留可编辑文本、组件、Item 和状态组，不把生成整图或 PNG 改名当 PSD。工具暂不可用时继续独立工作并准确记录未完成环节，不使用任意 Burner 产品美术充当 base 资源。

有实际输入时完成真实 Photoshop/UXP 导出并核对 BUI/PNG。未知请求先查询，失败后保留部分产物。更新版本兼容表、实际产物、支持限制和 S5 入口。
~~~

## S5 BUI 制作与替换

~~~text
工作目录：C:/Users/wuyu/My/ember-unity-framework。
请执行 docs/dev/ui-production-upgrade/05-bui-maker.md 的 S5。

先读 README.md、10-progress.md、08-acceptance.md、contracts 目录四份规范和工程规则，检查 S2/S3/S4 已确定的实际协议与接口。

实现 BUI Read、Analyze、Preview、Apply、Bind 和 Verify 的必要能力，统一接入 EUI 开发中心。页面使用真实骨架 Variant，组件保留嵌套连接，List 使用实际容器和独立 Item，不能将未支持节点静默变成空 Group。

区分原位更新与结构重建。保留资产身份、未触及绑定和用户代码；重建时提供新旧接口与消费者迁移。先生成可审阅计划并重新校验输入，再应用；不能按新画面猜旧业务行为。

准确记录部分成功和失败续接，重复执行不重复建资源，实例覆盖不改公共源。实际生成后读取 Binding 再接逻辑，通过 MCP 编译和相关验证。模板资产经项目中心普通保存；更新真实工具/API、生成结果、限制和 S6 输入。
~~~

## S6 base 完整换肤试点

~~~text
工作目录：C:/Users/wuyu/My/ember-unity-framework。
请执行 docs/dev/ui-production-upgrade/06-base-pilot-and-skills.md 的 S6。

先读 README.md、10-progress.md、08-acceptance.md、contracts 目录四份规范和工程规则，依据真实结果核对 S0 至 S5 的必要前置。

使用 S1 迁移后的 EUIMainPage、EUISettingPage 和中性 EUIOptionRowItem，完成一次真实初版 UI → PSD → Rename → UXP BUI/PNG → EUI Prefab → 绑定/逻辑 → 验证。旧 Panel 名称仅用迁移表定位。保留原来的开始、设置、关闭和来源恢复行为，不增加未经定义的产品功能。

分别验证视觉、布局、状态、动态 Item、交互和生命周期，使用同尺寸同状态截图；再做一次小美术变化验证增量更新。缺少实际素材或环境时保留独立成果，准确记录尚未执行的环节。

同时在独立项目副本或测试资产验证默认浅色皮肤替换为另一组项目视觉：新素材生效，规范名、Binding、事件和状态保持，默认库源不被修改。

在真实接口上完善公共 AI 工作流，优先复用 ember-eui-build；新增技能遵循技能维护与正式分发规则。不能只写技能说明就宣称工作流通过。

完成 base 的正式说明与普通保存，留下实际输入输出、验证、技能入口、失败恢复和 S7 所需信息。Bump、提交与发布仅按明确授权处理。
~~~

## 创建 ui-game 前的 base 父模板封存

仅当 S6 已通过、base 已普通保存但仍有未封存变化时使用。创建新派生模板需要有效的父内容基线，因此这一步可以先于 ui-game 创建完成；无需为此提前发布远端版本。

~~~text
请在 C:/Users/wuyu/My/ember-unity-framework 为创建 ui-game 准备 base 父模板基线。

读取 docs/dev/ui-production-upgrade/10-progress.md、07-ui-game-template.md、实际 base 差异和项目中心校验结果。确认本轮 UI 基础改造已通过约定验收并普通保存；若已经正确封存，不重复 Bump。

本次授权通过项目中心对这批已验证的 base 内容执行必要的本地 Bump，并记录真实版本、hash、兼容性和派生影响。保留原模板草稿及范围外修改，不修改模板快照或手填版本/hash。

本次不包含 Git 提交、推送或远端发布。完成后将 S7 可用父基线写入交接，继续创建 ui-game 的对话无需重新执行此步骤。
~~~

## S7 UI 游戏模板

~~~text
工作目录：C:/Users/wuyu/My/ember-unity-framework。
请执行 docs/dev/ui-production-upgrade/07-ui-game-template.md 的 S7，并联合读取 docs/dev/color-order-ui-game/02-framework-template.md。

先读本包 README.md、10-progress.md、08-acceptance.md 和工程规则。确认 base 换肤试点已通过、父模板维护与封存状态有效；版本和现有模板以现场为准。

本轮允许在保护当前草稿后，通过项目中心从 base 创建并加载 ui-game；若已存在本任务成果，继续维护而不覆盖重建。使用已经完成的公共 UI 库、Item 和制作工具，建立主页、局内、暂停/设置、结果、重开和返回的中性示例。

具体色彩顺序玩法、九关、评分和产品存档留在消费项目。正式 UI 使用 EUI 中心，模板说明归 Assets/Game/Documentation/ui-game，不依赖开发机绝对路径。

首次部署应直接获得 macOS 风格默认皮肤和可用图片，页面命名统一；附上项目级替换皮肤、保存本地修改及升级合并的实际步骤。

执行相关 Unity、模板和资源校验，通过中心普通保存，准备 base/派生影响、Bump 与发布所需的具体清单。Bump、提交、推送和发布只有已有明确授权时执行；未授权就交付本地结果与准备材料，不能伪造发布。

若本对话已指定并授权消费工程接入，再通过 UPM Manager 和项目中心完成实际部署验证；否则将该项明确留作后续。更新版本交接及原产品实施包的衔接说明。
~~~

## 后续封存与发布

下面是单独的发布授权提示词。仅在用户确实希望执行这些动作时发送；不作为前面阶段的隐式附加步骤。

~~~text
请在 Ember 框架开发仓库完成本轮 UI 基础建设的正式交付。

读取 docs/dev/ui-production-upgrade/README.md、10-progress.md、07-ui-game-template.md 和实际待交付差异，使用 ember-release-framework 技能。

本次授权对本轮已验证且确有内容变更的 base、ui-game 通过项目中心执行必要 Bump，并按仓库流程完成相应包版本、变更说明、提交、推送和正式 tag 发布。版本由实际兼容性和改动范围决定，不照抄方案中的历史版本。

先完成发布前检查，明确是否需要同步其他派生模板。保留非本轮内容和原模板草稿；遇到范围外修改不要混入发布。发布成功后提供实际版本、tag/commit、模板父关系、技能/UXP 兼容信息及消费升级步骤。
~~~

## 实际游戏项目接入

这段在用户打开并明确选定实际游戏工程后发送。

~~~text
请在当前明确的游戏工程接入已经交付的 Ember ui-game 模板。

先读取工程规则、实际安装包的维护规范、框架侧 UI 基础建设交接和 ui-game 正式说明，核对包来源、模板版本、公共库及工具兼容关系。工程路径或安装来源不明时先准确定位，不能猜一个目录写入。

安装/升级通过 Ember/UPM Manager，模板通过项目中心部署；保护现有项目内容，不改 PackageCache，不手改 manifest/lock 完成升级。核对公共配置和技能是否真正可用，完成编译、中性示例运行和一次必要生成验证。

如果这是《有趣的色彩顺序》项目，继续读取已交付的 color-order-ui-game 产品实施包，以已确认 V2 为基线，先实现三个短关，再根据试玩扩展。当前阶段只做部署时，不自动扩展为全部产品开发。

留下实际部署记录、入口、验证范围、未完成项和下一对话可直接使用的提示词。
~~~

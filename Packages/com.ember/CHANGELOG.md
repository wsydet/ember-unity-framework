# Changelog

## [0.14.8] - 2026-09-24

- **视觉小说模板 0.7.5**：新增“当前小说”选择入口，通过资源引用与稳定剧情 ID 定位小说，支持小说资产重命名、移动和切换；修复编辑器试播画面随状态区高度上下移动。
- **阅读与菜单 UI**：统一主菜单、设置、存档视觉；正文框居中、名字与正文同色并增加竖线。设置改为保留下层阅读画面的普通遮罩弹窗，各滑条显示数值或效果档位。
- **加载与章节卡**：新游戏恢复阶段加载条和百分比，首个可读画面就绪后揭幕；章节标题使用全屏底色与中央文字，退出后恢复对白布局。
- **消费项目升级**：通过 UPM Manager 升级框架，再在项目中心预览 visual-novel 0.7.x → 0.7.5 补丁增量；逐项合并本地冲突。仅升级包不会自动修改已部署业务 Assets。
- **验证边界**：Unity MCP 编译无错误，144 项 EditMode 回归通过，正式 Prefab 离屏渲染已检查；完整 Play Mode 端到端和消费项目升级验收待完成。框架 API、base 和 source3d-2p5d 模板及第三方依赖不变。

## [0.14.7] - 2026-09-23

- **模板冲突恢复 Skill**：新增 ember-template-conflict-recovery，三方合并消费项目模板升级冲突、保留本地业务改动，合并后重新预览；完整部署后恢复需可信备份。
- **技能分发**：框架级清单与包内 bundle 增至 10 项，最低使用框架版本 0.14.5；已有项目通过 AI Skill 独立更新入口安装。
- **验证边界**：技能格式、元数据、隔离目录和发布静态检查通过；真实 Unity 冲突恢复与消费端安装待验收。无 C#、业务模板或依赖变更。详见 docs/dev/release-0.14.7.md。

## [0.14.6] - 2026-09-23

- **视觉小说技能选择页面**：visual-novel 0.7.3 为剧情导入、演出优化、剧情回写补充必须生成并实际打开本地选择页面的说明；预填已有信息，展示完整计划，读取有效提交后执行。图片导入流程保持不变。
- **交付边界**：本次更新技能说明，三个页面由执行技能时生成，未新增随附页面脚本，尚未完成页面端到端或消费工程验收。模板经正式 Save/Bump 封存，当前项目发现副本已同步。
- **更新方式**：升级框架后，使用项目中心的模板技能独立更新入口；本次不要求覆盖业务模板。详见 docs/dev/release-0.14.6.md。

## [0.14.5] - 2026-09-23

- **模板补丁增量更新**：同模板 major.minor 内向前升级支持三方差异预览、保留本地内容、冲突选择与可复制交接日志；跨 major.minor 提示保护本地内容后完整部署。
- **部署基线与事务**：持久保存模板原稿，支持按旧部署 hash 恢复历史基线；变化文件、技能发现副本、版本记录和新基线一起提交及回滚，过期预览拒绝写入。补缺仅用于相同版本，不再推进业务版本。
- **验证**：用户确认无编译错误；Unity MCP 155/155 项相关 EditMode 测试通过，Console 无 error。模板基线、共享二进制、字体与技能包检查通过；真实消费项目交互与端到端升级待验收。

## [0.14.4] - 2026-09-23

- **模板技能独立升级**：解除包内模板与业务部署版本/hash 必须相等的限制；项目中心显式预览并更新专用技能源、发现副本及技能记录，保留剧情、配表、图片、场景、业务代码和部署记录。完整重新部署仍会覆盖业务目录。
- **兼容与保护**：schema v3 分别记录业务部署及技能源基线，兼容旧记录；最低模板版本按实际业务部署检查。源/发现副本本地修改需明确备份，预览后变化拒绝提交，GUID 冲突阻断，异常原子回滚。
- **回归**：Unity MCP 154/154 项相关 EditMode 测试通过；最终 Console ping 查询未响应，按规范停止追加编译确认，仍需手动触发编译确认。未操作 CMH。
- **交付**：模板内容和版本不变（visual-novel 0.7.2）；详见 docs/dev/release-0.14.4.md。

## [0.14.3] - 2026-09-23

- **已部署小说布局菜单修复**：visual-novel 0.7.2 的 Gameplay 主UI布局使用统一的正式模板身份判断；支持开发、部署及多级派生模板，消费项目不再依赖 EmberEditingTemplate.json。
- **统一判断与程序集边界**：在 Ember.Core.Editor 提供 EmberProjectSetup.IsTemplateActive，剧情图与布局工具共用，避免 Game.Narrative.Editor / Game.UI.Editor 循环引用。保留 Play、NarrativeModule、阅读页及阅读菜单 Prefab Mode 保护，Play 主菜单入口条件不变。
- **回归**：Unity MCP 编译完成、Console 无错误；24 项 EditMode 身份、菜单、Prefab 冲突及布局外观回归通过。
- **升级**：先通过 Ember/UPM Manager 升级框架，再迁移已部署模板脚本及程序集引用。补齐缺失不会更新旧脚本；完整重新部署会覆盖受管目录，应先备份并合回项目定制。详情见 docs/dev/release-0.14.3.md。

## [0.14.2] - 2026-09-22

- **视觉小说 0.7.1 图片确认页面**：图片导入技能随附 Python 回环服务与交互 HTML，实际打开并展示缩略图、尺寸、透明度和重复提示；支持逐图/批量剧情、用途、已有及新角色、表情与场景变体。
- **明确提交与回读**：页面生成资源 ID、目标/Resources 路径和表行计划，草稿、确认及取消保存为结构化文件；确认绑定独立批次及源图、配表、表定义、资源指纹。未确认/取消不写资产；无经验证的 Unity MCP 时仅允许浏览和草稿。不同内容冲突阻断，支持改 ID、同内容复用与跳过。
- **正式交付**：从 Assets 技能源经 Unity MCP 正式 SaveTemplate → Bump 封存到 visual-novel 0.7.1，并通过正式模板技能同步安装开发发现副本。消费端须经 UPM Manager 升级后按项目中心固定版本部署规则处理。
- **验证边界**：14 项隔离脚本回归及浏览器打开、逐图/批量、草稿恢复、确认回读、离线与取消通过；Unity MCP 刷新与正式保存/同步成功、Console 无错误。未执行真实消费项目导入/烘焙或覆盖部署。

## [0.14.1] - 2026-09-22

- **模板框架兼容声明修复**：base、source3d-2p5d、visual-novel 的 frameworkVersion 从 0.13.0 统一推进到 0.14.1，修复 0.14.x 框架按主/次版本检查时将三个模板判为不兼容的问题。模板内容版本、封存 hash 和父快照不变。
- **升级交付**：框架包版本推进到 0.14.1，新增对应 release/manifest 声明；发布 v0.14.1 后，已有 0.14.0 项目可通过 Ember/UPM Manager 检查并升级。
- **验证边界**：本次仅修改版本元数据和发布文档；未重新执行 Unity 运行或消费项目验收。

## [0.14.0] - 2026-09-22

- **框架与模板 AI Skill**：九项通用技能随包首次安装，保留独立更新；模板自有及继承技能随固定模板快照部署，提供差异预览、所有权记录、冲突备份及异常回滚。兼容原 schema v1，混合记录使用 v2，模板切换移出旧技能。
- **视觉小说 0.7.0 / preview**：继承 base 0.6.4，包含剧情/分支编辑、阅读与存档、E0–E5 演出和试播，以及图片导入、飞书剧情导入、演出优化、流程新页回写四项专属 Skill。剧情编辑器按正式编辑/部署身份工作，支持派生模板。
- **框架配套修复**：音频播放句柄及暂停、UI 异步显示有效性与生命周期、控件类型查找缓存、已保存页面的实际 Prefab 路径注册、编辑器退出 Play Mode 行为。
- **模板文档与版本**：业务说明随模板部署；base 0.6.4、source3d-2p5d 0.3.7、visual-novel 0.7.0 均已封存。升级框架不自动覆盖消费项目 Assets，需通过项目中心显式部署。
- **验证边界**：用户已确认手动编译无报错；技能格式、bundle、封存 hash/谱系及发布静态检查通过。自动编译请求此前超时，没有本版完整自动测试 XML；飞书 CLI 和消费项目端到端流程待验收。不声称全项目 482/482。

## [0.13.2] - 2026-09-17

- **Unity MCP 按需安装**：UPM Manager 的可选第三方包列表新增 Unity MCP，显示安装状态、实际版本与来源，提供安装与复制地址按钮。包条目支持独立仓库地址，Unity MCP 使用官方仓库的 10.1.2 固定提交，与既有依赖清单一致；保留其他插件地址及重复安装保护。安装后需另行配置服务和 AI 客户端，安装状态不代表连接状态。新增官方安装地址回归；Unity MCP 不可用，Unity 编译、测试及消费端实际安装待验证。

## [0.13.1] - 2026-09-14

- **9 个可安装技能**：保留 EUI，新增提交审查、Region 整理、方案评估、包清单同步、API 文档、Odin 面板检查、文档维护及 Odin 风格记录。固定技能来源为 v0.13.1；0.13.0 的更新器也可检查该技能标签。
- **消费项目适配**：技能优先遵循当前项目规则，携带必需参考资料；项目文档与风格记录留在项目中，禁止向依赖包/PackageCache 或技能安装目录写入这些产物。Odin 历史观察不再当作跨版本硬性禁令。
- **文档审计**：新增默认 consumer 与显式 framework 模式、自定义第三方目录排除和目录联接去重；补充隔离回归。提交审查覆盖全部未跟踪文件及缓存/目录联接差异。
- **验证边界**：本版不修改 C#、程序集或 Unity 资产；技能结构/链接、Python 回归及发布静态完整性检查通过。Unity 面板实际安装及此前代码版本的 Unity 编译/消费验收仍待完成。

## [0.13.0] - 2026-09-14

- **AI Skill 独立更新**：UPM Manager 新增技能分支/标签检查、目录下载和项目级安装入口；只安装发布目录中的技能，记录来源提交与文件哈希，保护本地修改、其他技能和框架维护源，支持备份、失败恢复及取消/超时。
- **EUI 技能归入框架**：统一维护 `ember-eui-build`，通过技能目录向消费项目分发；新增公开的已保存 Prefab 重新生成 API，技能适配器不再反射内部生成方法。保留旧内部入口和 UnityFarm 已部署适配器，模板不变。
- **验证边界**：新增技能下载/安装与 EUI 入口回归；Unity MCP 不可用，Unity 编译、EditMode 和面板实际安装尚待验证。技能目录随 v0.13.0 固定发布，可独立检查和安装。

- **EUIBinding Player 编译修复**：将代码输出路径属性与自定义过渡源码检查隔离到 `UNITY_EDITOR`，修复打包时引用 `OnGetCodeRootPath` / `OnGetGeneratedPath` 导致的三处 CS0103；序列化字段保持不变。Unity MCP 不可用，尚未完成 Unity 编译和 UnityFarm 打包验证。UnityFarm 同次报告的 Odin UPM DLL 导入异常仍需单独处理。

## [0.12.11] - 2026-09-14

- **可选第三方包状态与安装**：UPM 管理器突出显示已安装/未安装、实际版本和 Git/本地/内嵌来源，兼容直接导入插件和待编译的 Assets 文件。提供刷新安装状态、固定版本安装及复制地址按钮；Rainbow Folders、Rainbow Hierarchy、InputDeviceDetector 和 Console Pro 改用各自已发布标签，Feel 保留 ember-v0.11.1。安装前重查避免重复安装，包注册与安装完成后刷新，状态快照在 Layout 更新。新增检测和地址回归；Unity MCP 不可用，编译、测试和实际安装尚未验证。

## [0.12.10] - 2026-09-14

- **UPM 版本更新内容**：检查更新后，各可升级版本支持展开查看发布日志，默认展开最新版本。通过已有 Git 凭据异步读取最新标签中的 CHANGELOG 并按版本提取，保持零框架/Odin 依赖；同次窗口缓存复用，支持超时、重试、查看原文及关闭取消。读取失败或缺少说明不阻挡升级，说明状态在 Layout 阶段更新。新增 5 个离线 EditMode 回归（含异步完成后的实际 IMGUI 重绘）；Unity 编译及测试尚未执行。

## [0.12.9] - 2026-09-14

- **快速打开场景刷新修复**：直接按已绑定 SceneAsset 的真实路径检查和打开场景，不再依赖 Inspector 才更新的场景名缓存。刷新按状态保留选择并重建布局；切换前检查主场景和已选叠加场景，同名的纯名称引用要求明确绑定资产。新增 4 个 EditMode 回归，覆盖空名称缓存、重命名/重名、刷新选择及无效叠加场景；Unity MCP 不可用，编译与测试尚未执行。

## [0.12.8] - 2026-09-12

- **共享符号后备字体**：钉钉进步体 SDF 接入随包交付的 Noto Sans Symbols 2，补齐 `▶` 等缺失符号；保留主字体中文样式，使用独立 Dynamic + Multi Atlas 字体资产与材质，不依赖系统字体。原版 TTF 与 OFL 许可证一同交付，不修改模板或消费端 TMP Settings。
- **字体回归**：二进制清单增加符号字体完整性及常用符号覆盖检查，新增后备链/材质/许可证静态检查，以及混排与 TMP 子网格 PlayMode 回归。用户确认 Unity 编译无报错；本会话 Unity MCP 不可用，新增 PlayMode 案例与消费端实际渲染仍待验证。

## [0.12.7] - 2026-09-12

- **UI 开发中心保存后布局错误**：资源变更只标记目录待刷新，扫描与统计切换统一在新一轮 Layout 开始时进行；编译/导入状态提示也按 Layout 固定，避免 Repaint 控件数量变化。新增快照保留与实际 IMGUI 重绘回归，Unity 编译和测试尚未执行。

- **引导步骤编辑器**：参考 Burner 增加独立左右分栏窗口、步骤排序/深复制/撤销、中文条件与参数编辑、页面用途和目标节点选择，以及只读配置检查。用户已保存并封存 base 0.6.3 与 source3d-2p5d 0.3.5，父基线一致，两个模板的 GuideModule 均保持 Enabled=false；Unity 编译、3 个新增 EditMode 案例和交互验收未执行。

- **模块菜单与 Unity 6 适配**：引导编辑器顶部菜单跟随 GuideModule.Enabled；关闭时隐藏，编译后刷新。资产打开回调直接接收 EntityId，消除已废弃 API 及 int 转换警告。两份模板均保留 Enabled=false。

## [0.12.6] - 2026-09-12

- **基础 UI 删除保护**：EUIBinding 新增只读删除保护标记，总览和维护列表显示状态并禁用删除按钮；删除计划与执行阶段重新核对真实 Prefab，阻止陈旧/伪造计划绕过。新增 4 个保护回归案例；base 0.6.1 的六个基础 UI 已保存删除保护。

- **UI 用途说明**：EUIBinding 新增可填写中文的「UI 用途」，Page 与 Item 通用；UI 开发中心总览和清理与删除列表突出显示，总览支持用途筛选，绑定快照保留该字段，不影响类名、页面标识和代码生成。

- **模板交付**：用户通过模板面板保存并封存 base 0.6.1 和 source3d-2p5d 0.3.3，派生父基线对齐 base 0.6.1；保留派生模板自定义用途及保护配置，内容 Hash 与父快照一致。
- **验证边界**：完成静态发布完整性检查；Unity MCP 不可用，Unity 编译、4 个新增测试和交互验收未完成。第三方依赖不变。

## [0.12.5] - 2026-09-11

- **UPM 检查更新进度**：远程 Git tag 查询改为非阻塞执行，面板显示循环进度条、活动动画、已耗时和慢响应提示；不把等待动画表示为下载百分比。
- **查询生命周期**：支持取消、60 秒超时和重试，关闭窗口/脚本重载时释放查询；并行读取标准输出和错误，避免管道相互阻塞，查询期间阻止安装与升级并发。
- **第三方包体检**：明确 Odin/DOTween 为框架必需，增加 Rainbow Folders、Rainbow Hierarchy、Console Pro、InputDeviceDetector、Feel 的可选包状态与按需安装；优先识别 UPM 版本，兼容直接导入的插件，并为面板增加滚动区域。
- **升级提示不再清空查询结果**：成功提示绘制改为只读，新检查自动收起旧提示；升级后过滤已经安装或更旧的候选版本。新增实际 IMGUI 重绘回归，保护新版本列表与查询信息。
- **结果与回归**：空版本列表不再误报已是最新，重新查询清理旧结果；新增 5 个离线 Git EditMode 测试，覆盖轻量/附注 tag、空仓库、失败、取消后重试和超时。
- **验证边界**：Unity MCP 不可用，本次未完成 Unity 编译、EditMode 执行与面板交互验证；模板、第三方依赖、共享字体和业务资源不变。

## [0.12.4] - 2026-09-11

- **共享钉钉 SDF 支持容量溢出**：启用 Dynamic 字体的 Multi Atlas Textures；单张 1024×1024 图集装箱失败后，TMP 可创建后续图集。采样字号 90、padding 9、SDF/TTF GUID、源字体及材质样式不变，不靠清空图集规避问题。
- **容量与渲染回归**：新增共享配置检查和 Editor PlayMode 测试，先填满临时单图集，再在同一字体上启用多图集重试；验证中文、图集材质、TMP 子网格及真实 SceneUI EUI 对象池显隐与复用。所有压力测试资源均为非持久化对象，原始 SDF 文件逐字节保护。
- **验证边界**：静态配置和 0.12.3 二进制完整性检查通过；Unity MCP 不可用，容量压力测试和实际渲染尚未执行。首轮用户报告的测试 API 编译错误已修正，修正后的编译结果待确认。消费端必须通过 Ember/UPM Manager 升级，模板与农场规则不变。

## [0.12.3] - 2026-09-11

- **修复共享字体发布损坏**：SharedAssets 默认关闭 LFS 与换行转换，meta、TXT 和文本序列化 asset 单独保留文本处理；从已验证的本地原件恢复钉钉进步体和阿里妈妈东方大楷 TTF，分别补回被 CRLF→LF 删除的 299 和 3,943 字节。
- **二进制完整性回归**：新增已验证原件的长度/SHA256 清单与标准库 Python 检查，覆盖完整共享目录二进制清单、工作区/暂存区或发布 Git blob、两种 autocrlf 的干净检出、可选消费包核对，以及 TTF 表校验和/字形偏移/重点中文 cmap。CI 在提交、PR 与 tag 执行静态检查。
- **验证边界**：两份本地字体通过结构检查与 FreeType 栅格化，旧发布均被结构检查拒绝。Unity MCP 不可用，Unity 编译、导入与 UnityFarm 六个正式 SceneUI 标记的运行验收待手动完成。仅框架升级，模板、SDF 发布内容、GameplayScene、FarmM1、EUI 美术、Binding 与农场规则不变。

## [0.12.2] - 2026-09-10

- **修复 Table 导入后的生成物误判**：Binding/Catalog 的 C# 文本产物显式包含 UTF-8 BOM，与框架脚本导入器保持一致，避免导入后多出三字节而触发 ArtifactStale、重复全量替换或阻止单表导出。Manifest、CSV 和 ETBL 编码与格式不变，生成物仍逐字节校验。
- **修复 Table 接线编码误判**：新建 Module/User 脚本使用相同编码策略；现有默认 Module 兼容带/不带 BOM，真实业务修改仍受保护。
- **回归验证**：新增 10 个 Editor 测试案例，覆盖 BOM、正式编码转换工具、重复提交、单表提交前置校验和业务修改保护；用户已确认修复后的框架项目测试通过。本会话未通过 Unity MCP 独立执行，未收到本轮 XML 或精确案例数；UnityFarm 更新后的消费回归仍待完成。模板内容及第三方依赖不变。

## [0.12.1] - 2026-09-10

- **配置表中心可视化浏览与单表导出**：按 Definition 列出当前项目声明的表，展示 Row 字段、主键/引用/可空约束和严格类型化数据预览，并根据实际 Table ID、Row 类型、字段、样例主键与二级索引生成可复制的运行时查询代码；数据内容变化可只回读验证并原子替换所选表的二进制，结构变化仍要求全量同步 Binding、Catalog 与 Manifest。
- **减少无效资源重导入**：全量提交跳过字节未变化的生成物；单表提交只允许替换 Manifest 已持有的目标，保持其他二进制、生成代码和所有权清单不变，并在写入失败时恢复旧文件。

## [0.12.0] - 2026-09-10

- **新增强类型配置表 Runtime**：加入不可变 Row 契约、Ordinal 字符串主键、只读表/二级索引、实例数据库、结构化诊断、严格 ETBL V1 Reader/Writer/Codec，以及 Required 整批回滚、Optional 省略和单次数据库快照交换的纯 `EmberTableEngine`。
- **新增 Table Integration**：`EmberTableModuleBase<TModule>` 统一连接 Core Module 生命周期、`EmberResourceManager.LoadFileSync` 与 Engine；初始化异常、销毁和热重启共用幂等关闭路径，具体项目只需提供 Catalog 与项目钩子。
- **新增 CSV/TSV Editor 管线**：支持 UTF-8 BOM/无 BOM、CRLF/LF、RFC 4180 引号/转义/单元格换行、严格类型与引用校验、规范 Source/Schema Hash、确定性 V1 烘焙、Runtime Codec 回读、强类型 Binding/Catalog 生成、陈旧检测、项目校验和诊断中心。
- **提供模板项目接线入口**：配置表中心可先生成空或当前 Catalog，再只补缺失的默认关闭 `GameTableModule` 与项目所有的 User 扩展；模板内容仍须由项目中心保存、Bump 和父级同步。
- **保护生成物事务**：全量产物以所有权清单管理，提交前预览并暂存，保护非生成器文件；替换或孤儿清理失败时整批回滚，成功后只刷新一次 AssetDatabase。
- **补齐独立测试矩阵和文档**：新增 Runtime、Integration、Editor 三个 EditMode 测试程序集、跨平台固定向量与 Table 包文档。用户已确认 Unity 零编译错误和三套测试通过，基础模板与 2.5D 模板已通过项目中心封存、声明兼容 `0.12.0` 并完成父同步；UnityFarm 的 UPM 更新与实际读表作为发布后消费验收单独记录。

## [0.11.5] - 2026-09-08

- **建立 UnityFarm 改动回流判定规则**：随包新增统一维护文档，明确项目专用改动、框架升级、模板升级及两者联动的边界、版本策略、回流流程和 UnityFarm 接收方式。
- **增加强制阅读入口**：仓库根 `AGENTS.md` 与 `CLAUDE.md` 在处理 UnityFarm 来源改动前要求先阅读该规则；包说明和文档索引同步提供入口，避免规则只存在于会被遗漏的独立文档。
- **模板兼容声明同步**：`base 0.5.6` 与 `source3d-2p5d 0.2.7` 的 Assets、内容版本和 hash 均未改变，只将框架兼容声明推进到 `0.11.5`。

## [0.11.4] - 2026-09-08

- **修复标准 URP 项目的模板 GUID 冲突**：为 GameplayScene 与 2.5D Input Actions 分配 Ember 专用稳定 GUID，并同步更新基础模板、派生模板、父快照、场景映射和项目引用；避免 Unity 与根目录默认 SampleScene / InputSystem_Actions 冲突后静默改写 `.meta`。
- **部署前原子阻断未知 GUID 冲突**：首次部署与完整模板部署都会在写入前扫描模板 GUID 和不会被替换的项目资源；发现占用时给出模板/项目路径并零写入中止，避免出现 `.meta` 已改写但 YAML 仍引用旧 GUID 的半损坏状态。
- **支持当前模板完整重新部署**：项目中心保留“补齐缺失”，并新增带覆盖警告的“完整重新部署”，用于显式应用已有文件修复；五个模板管理目录事务替换，Art、ThirdParty 等非模板目录保持不变。
- **模板版本同步**：基础模板封存为 `0.5.6`，2.5D 模板封存为 `0.2.7`，兼容声明更新至框架 `0.11.4`。

## [0.11.3] - 2026-09-08

- **收紧消费端模板边界**：Git/Registry 安装的消费项目只能部署包内模板；模板创建、保存、加载、父级同步、版本和 metadata 修改等写 API 现在全部要求 embedded 框架项目，不能通过绕过 UI 修改包内 `Templates~`。
- **跨模板操作改为纯部署**：非活动模板按钮改为“部署此模板”，确认信息明确只将目标模板写入消费项目。移除 0.11.2 的持久化 Base 备份及“备份并切换”交互，不再产生类似保存当前模板的行为；目标完整模板仍以文件事务替换五个受管业务目录，事务中断会原位回滚。
- **发布验证边界**：模板 hash、父快照、版本文件和静态差异已复核；当前没有可用的 Unity MCP，Unity 编译、EditMode 与 Unity Farm 消费验收由安装 0.11.3 后继续完成。

## [0.11.2] - 2026-09-08

- **支持完整模板显式切换**：项目中心对非活动模板提供“备份并切换”。切换前备份当前模板覆盖的五个业务目录、部署记录与 Build Settings，然后以文件事务完整替换目标模板；事务失败自动回滚，`Assets/Art`、`Assets/ThirdParty` 等非模板目录不受影响。
- **修复 Git URL 安装后的模板 hash 误报**：已知文本资源计算 hash 时统一 CRLF/LF，包内 `.gitattributes` 同时保护 `Templates~` 原始字节，避免消费机 Git 行尾设置让已封存模板被误判为损坏。
- **修复部署后校验的批量伪差异**：项目校验现在用与部署器相同的版本头转换生成比较基准，不再把 `Generated by Ember Setup` 版本声明的正常改写报告为业务内容变化。
- **发布验证边界**：模板 hash、父快照关系、版本文件和静态差异已复核；当前没有可用的 Unity MCP，Unity 编译、EditMode 与消费项目功能验收由安装 0.11.2 后继续完成。

## [0.11.1] - 2026-09-08

- **0.11.1 消费声明**：补齐随包 Dependencies~ 的 56 项直接依赖 manifest 基线及版本化发布声明，7 个私有第三方包固定到统一 `ember-v0.11.1`，MCP 固定当前已解析 commit；提供第三方先、框架后的上传命令。经用户授权、在父子内容和父快照 hash 一致性预检后，将父子模板兼容声明同步到 0.11.1，不修改内容版本或 hash。声明不自动执行安装，Unity/消费端验证及依赖自动同步仍未完成。
- **0.11.1 发布准备**：package.json 已声明 0.11.1，尚未创建/推送发布 tag。旧 `v0.11.0` 错误指向 0.10.0 提交，本次使用全新 `v0.11.1`，不移动或覆盖旧 tag。经授权从 hash 一致的备份恢复派生模板 `source3d-2p5d 0.2.6` 的正式 Assets，保留本地恢复副本，不改写模板内容版本或 hash。Rainbow 两包、Console Pro、InputDeviceDetector、Feel 明确纳入私有第三方仓库交付；Feel v5.4 的本地 UPM 封装已准备，安装切换、Unity 验收和依赖自动同步尚未完成。
- **项目中心校验与模板编辑优化**：生成物校验统一到项目中心，提供明确基准、错误/警告/业务差异分类、资源/.meta/GUID、场景、管理区和 UI 生成链路检查；模板开发调整为左侧模板树与四个详情分区，支持保存差异、文本对照、冲突筛选和版本封存提示。新增真实截图草稿、来源校验、版本效果与演示入口，图片独立于模板部署内容；父级同步自动重载前检查未保存修改。已补充只读边界测试，Unity 编译、测试执行及图形验收待完成。
- **新增模板场景对象级语义同步**：仅在显式预览和事务应用阶段对并发修改的 `.unity` 场景调用 Unity 自带 UnityYAMLMerge，以 O/N/C 三方语义合并保留不同场景对象的父子修改；工具不可用、超时、输出无效或同一属性真实冲突时安全回退为整场景“保留派生/接受父模板”，普通资源、脚本、Prefab 与 `.meta` 继续使用文件级规则。应用阶段在独立 stage 重跑合并并复核输入、工具/规则、结果 hash、metadata 与 GUID，失败零写入或完整回滚；面板支持语义预览、版本 Bump 与当前编辑副本自动刷新。2026-09-07 已通过 fake runner、事务、真实 UnityYAMLMerge 集成和手工 O/N/C 验收；非冲突合并后派生封存为 `source3d-2p5d 0.2.5`、父基线 `base 0.5.4`，收口时根模板已继续前进为 `base 0.5.5`，等待下一次派生同步决策。
- **明确业务模块两阶段生命周期**：`InitState` 在 Manager 初始化前统一发现模块，先通过 `EmberModuleAttribute` 判断阶段和启用状态，仅为启用模块创建实例；场景绑定可通过无创建查询获取已登记实例，再由状态 Phase 调用 `OnInit`。`EmberUpdateManager` 复用已登记模块，不再独立创建禁用业务模块，并只驱动 `OnInit` 成功后的活动模块。
- **完善玩家输入基础设施**：`EmberInputManager` 接入 Manager 反射初始化管道，增加按 Map 获取 Action 的无歧义入口，并预留稳定 BindingId、交互式重绑定、显示文本、恢复默认及 Overrides 导入导出契约；当前仍不内置具体重绑定服务。
- **新增场景相机自注册组件**：`EmberCameraRegistration` 随 CinemachineCamera 的 Awake/Destroy 自动注册与注销，支持普通或强制的 Awake 激活；玩家控制只使用相机，不再代管相机注册职责。
- **新增 2.5D PlayerControl 样例**：Gameplay 模块消费 Player Map，等待 Loading 方块完全退出后才开放输入；支持 WASD、世界锚点鼠标拖动、可解锁 BoxCollider 区域列表、可见 3D 围栏边界、滚轮调整正交相机 Orthographic Size，以及播放模式实时读取的 PlayerControlSettings；移动按当前位置约束连续可达区域，避免鼠标拖动跨越区域空隙瞬移，并丢弃撞边后的拖动残差；InputActionAsset 归入模板覆盖的 `Assets/Game`，确保模板交付包含完整输入依赖。
- **修复 Loading 与 UI 退出时序**：`LoadingFadeOutComplete` 延后到进度条和方块全部退出后广播；UI 引擎 Shutdown 强制释放活动及延迟关闭页面，使未完成异步过渡通过销毁取消正常结束，不再回写已销毁组件。
- **补齐 SceneUI 模板验收工具**：`SceneUIObject` 使用项目 Odin 风格面板并提供播放模式手动刷新位置；新增往复移动 Cube 验证动态 SceneUI 的 PollPosition 跟随。
- **修复 UI 创建规则**：输出子目录填写 `Module/<模块>` 时不再重复把 `Module` 当作模块名，Prefab 会正确生成到 `UI/Module/<模块>/Prefabs`，同时保持原有 `<模块>/Page|Component` 写法兼容；非 Page UI 不再依赖或自动嵌套 `EUISafeArea`。
- **新增 UI 模块目录模板管理**：UI 开发中心增加「模块模板」页签；新业务模块会按 `UI/Module/模板` 自动创建独立 GUID 的空目录树，并提供只补缺失项的一键同步、保留 GUID 的跨模块重命名，以及仅允许空目录且移入系统回收站的双重确认删除。现有模块目录不会随模板删除。
- **新增通用 SceneUI 模块**：提供场景锚点到 UI 的统一投影、可见性、缩放、优先级/深度排序、View 池化与生命周期管理；完成 EUI、Cinemachine 和 Ember.Resource 接入，并支持预算化遮挡检测、分帧预热、批量注册及运行时诊断。
- **完成 2.5D SceneUI 模板验收与封存**：`source3d-2p5d` 已包含可运行的方块锚点、Marker UI、正交镜头移动/缩放和 100/500/1000 目标规模验证示例，P0-P4 均已通过手动编译与 Play Mode 测试；业务内容已保存并 Patch 封存为 `source3d-2p5d v0.2.3`，父基线对齐 `base v0.5.3`。
- **UPM 升级进度可观测**：框架升级改为四阶段进度展示（校验、下载与解析、注册与编译、版本验证），同时显示活动动画、已耗时和慢任务提示；通过 `SessionState` 与 Package Manager 注册事件跨脚本域重载续接，并同步至 Unity 后台 Progress，最终以实际安装版本确认成功。

## [0.10.0] - 2026-09-02

- **UI 资源与代码路由重构**：逻辑代码统一位于 `Assets/Game/UI/Runtime`，UI 资源根独立为 `Assets/GameResource/Resources/UI`；框架页面进入 `Common/Prefabs`，业务页面按 `classPath` 首段进入 `Module/<模块>/Prefabs`，Prefab、GamePages 与默认 Resources 加载器共用同一套路径规则。
- **新增 UI 开发中心**（`Ember/UI/UI 开发中心`）：整合「创建 UI / UI 总览 / 清理与删除」；创建前完整预检目标，生成标准 Canvas/EUIBinding、逻辑与 Binding/Settings、GamePages 条目，并在 Unity 编译成功后打开 Prefab。清理与删除增加影响预览、路径守卫、跨 Framework/User 注册查重、共享脚本保护、全 Assets 引用复核和安全 dry-run。
- **标准 UI 骨架与 SafeArea**：CanvasScaler 统一为 Scale With Screen Size / 2560×1440 / Match 0.5；页面使用公共 `EUICommon_Ani.controller` 与嵌套 `EUISafeArea.prefab`，安全区提供中心及周围八个定位节点，并在设备旋转或安全区变化时通知页面逻辑。
- **页面类型和层级收口**：EUIBinding 页面类型改为互斥 `PageType`，新增独立 `FullScreenPopup`；统一 Background 0 / Normal 1000 / Popup 2000 / TopMost 25000 / FreePage 30000，SubPage 步长保持 50，UIRoot Hierarchy 按实际 sortingOrder 稳定排序。
- **弹窗遮罩完善**：支持逐页面 `useMask`、`maskColor`、`clickMaskToClose`；独立遮罩 Canvas 使用 UI Camera、继承所属弹窗 Layer、自带 GraphicRaycaster，sortingOrder 精确位于弹窗下一层。状态机驱动的弹窗可关闭遮罩点击退出，改由业务按钮统一执行状态退出。
- **UIUpdate 与生成代码所有权收口**：Framework 页面仅在勾选「使用 UIUpdate」时生成 `NeedUpdate`、`OnUpdate` 和 `OnUpdateUser`；取消时自动删除默认空钩子，已有用户代码则要求确认。可选 Popup 覆写保留在 Lifecycle 管理块，业务增量继续位于块外用户钩子。
- **过渡动画时序统一**：普通 UI 在 None / PresetFade / Animator / CustomCode 中选择唯一负责人；修复 Animator-only 跳过、事件桥位置、根 Alpha、退出阶段重复交互和点击穿透。Loading 方块过渡继续使用「方块扫入 → Custom Enter / Custom Exit → 方块扫出」专用链路。
- **页面与场景生命周期修复**：FullScreenPopup 仅在完全遮盖时隐藏下层；Popup 遮罩和下层恢复延迟到退出动画完成；Main → Gameplay 替换式状态切换会关闭旧 `EUIMainPage`，不再把旧场景 UI 保留为活动 MainPage。
- **模板与配置操作安全化**：模板保存/加载改为暂存区完整复制、成功后换入、失败自动回滚，避免中断导致资产丢失；EUIBinding Settings 可在引用丢失时从现有配置资产自愈恢复。
- **演示页收尾**：GM 退出按钮改为调用 `GameLauncher.Quit`，确保先逆序关闭业务模块与框架 Manager、刷写文件日志，再退出应用；UPM Manager 安装示例更新为 `v0.10.0`。
- **验证与模板状态**：dev 六个通用 UI 已完成静态检查、Unity 编译和 Play 回归；基础模板已由模板编辑器手动保存为 `v0.5.0 / frameworkVersion 0.10.0 / stable`，并完成路径、GUID、GamePages、SafeArea、Binding 与关键状态逻辑复检。

## [0.9.2] - 2026-08-31

- **UPM Manager 面板增强**：检查更新成功后显示「远程最新：vX.Y.Z」总览行（含与当前版本对比）；未检测到包时的帮助文本 tag 示例更新为 v0.9.2

## [0.9.1] - 2026-08-31

- **修复（高）：部署/加载模板时 .meta GUID 错位**——Unity 运行中 File.Copy 落盘瞬间文件监视器抢先生成随机 GUID 的 .meta，消费端全新部署后场景预制体实例/脚本引用全部断链（Missing Prefab Asset）；DeployTemplate 与 LoadTemplate 改为拷贝期间 `DisallowAutoRefresh` + 落盘后一次性同步导入，GUID 原样保留
- **修复：UPM Manager 检查更新永远显示「已是最新」**——`git ls-remote --tags` 返回的 `v` 前缀 tag 无法被 `Version.TryParse` 解析；tag 归一化（去 `^{}` 后缀/去 v 前缀/去重）
- **新增：UI 预制体管理器**（`Ember/UI/UI 预制体管理器`）——总览所有含 EUIBinding 的预制体（位置/页面定义/生成脚本/EUIPageDef 状态）；一键清理（失效 EUIPageDef / Missing Script / 空引用绑定，孤儿脚本与空叶子节点 dry-run 后确认）；**一键删除 UI**（预制体 + .cs/.Binding.cs/Settings.cs + EUIPageDef 条目整体删除）

## [0.9.0] - 2026-08-31

- **编辑器 SO 布局统一**：5 个编辑器 SO 迁移至 `Assets/Ember/Editor/SOs/`（为未来 Ember/Runtime 预留）；孤儿 EmberUIBindingSettings 删除；defaultBindingTemplatePath 指向 SOs 文件夹；代码路径常量/模板镜像同步
- **EUIBinding 代码生成改造**：双路径合并为单路径（统一生成到业务层）；框架/用户模式差异改为生成文件块标记——框架模式 .cs 带 `[EmberManaged:begin/end Lifecycle]` 单块 + 块外 6 个 `XxxUser` 用户钩子；页面注册条目框架→GamePages.cs、用户→GamePages.User.cs（跨文件防重、幂等就地更新）；绑定条目 IsFramework 标记（清除/重收集受保护、列表 🔒 只读）；包内预制体生成拦截；FreePage 生成修复（TopMost + sortingOrder 30000）；框架模板缺失硬报错
- **框架 UI 迁出包至业务层**：EUIBackgroundPage/EUILoadingPage/EUIMainPage/EUIGamePlayPage/EUISettingPage/GMPage 重命名（预制体尾缀 Panel，文件夹 Framework/MainScene/GamePlayScene/SettingScene），全部 Lifecycle 块格式 + 头标记；EUILoadingPageSettings SerializeReference 修复
- **UI 生命周期补充**：OnBeginLoad/OnPreload(param,isOpen)/OnReopen/OnResetDefault；EUIObserver.OnPageLoadStarted；HidePageViewOnly/ShowPageViewOnly + PageState.ViewHidden；预加载不再提前跑 Init；EUIPage 构造 alpha 归零防首帧闪现
- **Loading 双模式**：SkipFakeProgress + UseFakeProgress 判定（快速转场/未勾选进度显示/无配置 → 真实加载完成即就绪）；进度显示遵循 OnResetDefault→OnShow 约定
- **状态机连接线模型**：GetEdges() 统一边数据包 + Edges 实例列表；TransitionTo 统一入口（切换/叠加按场景路径自动判定）；框架 5 条边 ReadOnly（仅 QuickSceneLoad 可改）；Main→Gameplay 假进度、Gameplay→Main 快速
- **头标记双版本与标记刷新**：头标记格式升级为 `Generated by Ember Setup v{模板版本} (framework v{框架版本})`；补齐缺失时对已部署文件全量刷新头行版本（不覆盖内容、无头标记的用户文件不受影响）
- **初始化窗口**：「场景与配置」归入各模板线框（部署按钮下方）；场景注册检查改为按模板目录扫描（GetTemplateScenes），多模板自动适配
- **修复**：EmberSetupWindow / EmberTemplateEditorWindow 静态 GUIStyle 初始化在域重载时序抛 NRE（改懒加载属性）
- **模板体系**：base 模板 v0.3.0（结构变更 minor bump），frameworkVersion 对齐 0.9.0
- **依赖管理**：UniRx dev 改走 OpenUPM registry（与消费端同源）；rainbow-folders/rainbow-hierarchy/inputdevicedetector 托管至 ember-thirdparty-upm 私有仓
- **发布前测试**：dev 全量功能测试 51 项全部通过（编译/代码生成链路/模板编辑器/初始化窗口部署与闸门/两级标记/字体/演示链路）

## [0.8.0] - 2026-08-26

首个包含「演示形态」模板体系的发布版。内部里程碑 0.5.0/0.6.0/0.7.0 从未发布，变更合并于此：

- **模板体系**：框架交付的就是演示形态——包内 `Templates~/base/Assets` 全量镜像 dev 演示业务层（状态类全继承、演示 UI 四页、演示模块、4 完整场景、配置资产，`.meta` 随行 GUID 全链有效）；Setup 从代码生成改为整树一键部署；多模板支持（自动扫描 `Templates~/*/template.json`，未来 2D 平台等模板同机制接入）
- **初始化窗口**：`Ember/Setup/初始化项目` 弹出 EmberSetupWindow（框架/模板状态总览 + 模板列表 + 一键部署/补齐缺失/重新部署）；EmberProjectSetup 重构为公共引擎 API（Initialize/GetTemplates/IsTemplateDeployed）
- **模板编辑器**：`Ember/Setup/模板编辑器`（保存/加载/新建模板，新建可选 fromBase 复制基础模板）；目标模板下拉选择（不再手输 id），保存/删除/元数据编辑作用于选中模板；**模板版本号独立于框架版本**（新建从 0.1.0 起，保存内容不覆盖版本，编辑器内主/次/补丁一键 bump，版本可编辑回退）；面板显示「当前正在编辑的模板」状态与目标一致性警告；引擎新增 CreateTemplate/SaveTemplate/LoadTemplate/IsEmbeddedPackage/StripSceneObjects/BumpTemplateVersion/SetTemplateVersion/UpdateTemplateMetadata/DeleteTemplate
- **共享字体入包**：演示字体（钉钉进步体 / 阿里妈妈东方大楷，许可证随包）从模板移入包内 `SharedAssets/Fonts/`，多模板共享零重复；场景/预制体按 GUID 引用不受影响
- **模板升级协同 P-A**：template.json 增 frameworkVersion/channel；消费端兼容闸门（major.minor 一致才显示、preview 徽标、deprecated 隐藏）；部署记录 EmberDeployedTemplates.json + 部署时重写头标记版本；升级提示矩阵（patch/minor/major 分级提示，只提示不合并）；**两级标记铺入模板**（49 个 .cs 全文件头标记 + 5 个混合文件钩子块标记）；**GamePages 框架/用户拆分**（GamePages.cs + GamePages.User.cs partial 拼接，codegen 写用户文件）；设计文档 docs/dev/template-upgrade-system.md
- 同步工具：`scripts/sync-scaffold.ps1`（dev 改动一键同步模板）+ `scripts/strip-template-scene-objects.ps1`（剥离 dev 测试对象）

## [0.4.1] - 2026-08-26

- 修复升级面板 CS0815：`Client.Resolve()` 在该 Unity 版本返回 void，改用「提取 manifest URL → 替换 #tag → `Client.Add` 重装」

## [0.4.0] - 2026-08-26

- EmberUPMManager 一键升级（检查更新 + 改 manifest tag + 重装），并按版本语义标注强制/可选更新
- 版本语义定稿：开发期 major=0；第二位=框架变化（强制更新）；第三位=小修补（可选）
- 累计 0.3.x 变更：UniTask 内置、Setup 向导 4 场景、场景映射 SO 修复

## [0.3.0] - 2026-08-26

- 11 个包合并为单一 `com.ember`（basic/extensions/uiextension/core/resource/scene/audio/camera/input/ui/editor 全部并入）
- 程序集边界不变（原 11 个 asmdef 原样保留），模块化由 asmdef 保证
- 新增 EmberUPMManager 面板：Odin/DOTween 检测 + 一键安装 + 手动安装指引
- 消费端安装简化为一行 git URL；升级只改一个 tag

## [0.2.0] - 2026-08-24

- 框架转 UPM 包首个发布版（11 包结构，已被 0.3.0 取代）

Ember 框架单一包，lockstep 版本。

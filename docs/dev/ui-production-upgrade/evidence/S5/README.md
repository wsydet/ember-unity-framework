# S5 BUI 制作交接 — 2026-10-27

本阶段完成真实 BUI 的有限制作、原位更新、候选重建、保存回读及编辑器运行验收。没有进入 S6 完整换肤、S7 交付、发布或消费部署。入口和正式请求协议见 [包内说明](../../../../../Packages/com.ember/Documentation~/ui/bui-maker.md)。

## 现场与输入

开始时确认活动 base 0.8.4/framework 0.22.0，585 文件，contentHash `c58c6c5ed41b18efc8fe38fd4999dd5f`、versionedContentHash `936dcb1d411afefcacd961cbad1d1d0d`。未重新加载模板。原有工作区变化和模板切换删除保留；状态清单位于 `.utmp/ui-production-S5/status-before.txt` 和 `status-after.txt`，逐操作备份在各 receipt.backupPath。

Unity 重新枚举后固定 `ember-unity-framework@b23e9b768e85ed30`，projectRoot 为本仓库，Unity 6000.5.4f1。所有制作在此实例执行，没有路由 Unity Farm 或参考工程。

真实输入仍为 `art/ui/S4/EmberPilot.bui` 和 `EmberPilot.eui-map.json`：3 Target、24 节点、8 张资源 PNG、4 张文字参考。初始 Node 只读门禁通过。BUI SHA256 `4b28841b937af76cf728eafabc2d93054cf679a3d7564386860d92bdad2b6f48`；source PSD `7254350f66baf66bdf89b47fa7c8395d6808603703b79aaa4e2eb0d1eefcb8a2`；working PSD `81ef4a3c69a9fa2ec710f06f3ccf877a59989dd71952d58fd3f7ab1e0519baa9`；原 map `f6453165f190e3715feff71292f73385e6a267547a29cce0f37762ffbe5f58a9`。每项输入摘要在 [初始计划](analyze-before-apply.json)。未重跑旧 UXP 请求或改动原 S4 文件。

参考只读研究了 Burner 当前解析包 `com.burner.ui.prefabmaker@2.0.0-preview.17` 的 Reader、WorkflowApi、MakerRequestService、InterruptedRecovery、export-capabilities 和 BuiMakerReceiptRecoveryTests。采用输入固定、先保留回执、未知结果不重放等通用原则；Ember 使用现有 LitJson、创建/绑定/生成服务，未引入 Burner 业务、绝对路径或 Newtonsoft 依赖。

## 实现与资产

框架代码在 `Packages/com.ember/UIExtension/Editor/Pages/EmberBui{Models,Service,Service.Read,Service.Edit,Panel}.cs`，UI 开发中心增加 BUI 制作页；EUILibraryService 修复外层 Variant 内的精确公开槽解析。测试在 EmberBuiEditTests 和 EUILibraryEditTests，框架能力不放入模板业务目录。

真实原位目标：EUIMainPage、EUIDynamicListPage、EUIDynamicOptionRowItem。主页面仍是原 GUID `9b27fbe343055e94fb5f3ae24a87ee96`，Framework/PageType/IsFramework/导航保留；动态页、容器、Item 的原 GUID 保留。Settings 嵌套源从 ActionButton 显式改为 SecondaryButton：设计 label 映射 catalog 的 Label/TMPEx，Start 使用 label。设置按钮局部 GameObject ID 从 `4880201848577585989` 变为 `231103182672518052`，这是记录过的结构迁移，**不是全部对象 ID 保持**；外层字段名、类型、路径不变。

精确导入 6 张 PNG 到 Common/Atlas/BuiPilot 和 Module/UIFoundation/Atlas/BuiPilot。ViewportExtent 与列表页背景仍完成输入校验，但本次请求明确 preserve，不额外导入。Surface 是 256×256 设计框、130×130 PNG、四边 64/中心 2 的实际 Sliced Sprite。动态图标继续取数据，进度保持工程满宽/filled，未把设计 50% 当满宽。

新候选资源位于 `Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/`：EUIBuiCandidateMainPage、EUIBuiCandidateDynamicListPage、EUIBuiCandidateDynamicOptionRowItem。均经 EUICreationService 创建为真实源 Variant，正式生成 Binding；继承原逻辑。候选列表宿主引用新候选 Item，页面没有登记固定动态行。原资源保留，默认导航未切到候选。UIFoundation 是通用示例目录，没有对应 EmberModule 类型，因此请求 moduleType 为空；服务对指定的业务模块检查启用状态。

[protected-files.json](protected-files.json) 证明原主界面/列表/行用户代码、独立容器及 Action/Secondary 共享 Prefab 的 SHA256 全部未变。全部实际最终契约以 `final-*.contract.json` 保存。

## M01–M07

| 编号 | 实际结果与证据 | 边界 |
| --- | --- | --- |
| M01 | Analyze 列出全部节点/目标/写入，隔离 Preview 构建后卸载，文件逐字节未变；[negative-validation.txt](negative-validation.txt) | 预览是临时构建诊断，不是驻留可视化窗口；截图为实际 Game View |
| M02 | 三个候选经创建中心建立 Variant、嵌套公共按钮、独立动态 Item；[candidate-analysis.json](candidate-analysis.json)、[候选回读](candidate-verified.json) | 真实 BUI 没有独立 Component Target；组件复用是实际嵌套，通用库组件另有测试夹具 |
| M03 | 三份原位 Prefab 保存回读，GUID/绑定/用户代码/列表契约保持；[首次回执](apply-receipt.json)、后续布局请求及 final 契约 | 主界面几何与 Canvas 参考尺寸是明确范围内变化；未改列表工具栏 |
| M04 | 新旧逻辑通过继承接口可追踪，候选宿主迁移 Item 引用，Play 实例确为候选行逻辑；[candidate-runtime.txt](candidate-runtime.txt) | 只切候选消费者，不自动查改全工程动态路径/动画/外部注册，不删除旧资源 |
| M05 | 陈旧计划、重复绑定、缺 BUI、未知必需能力、重复 nodeId、未知关键选项均阻断；无正式文件变化、无 reserved 回执；[负例](negative-validation.txt) | 负例是明确夹具，正向制作仍使用真实 S4 BUI |
| M06 | 首次保存后验证失败，定向恢复名字并续接生成；候选部分保存后按真实产物导出契约再恢复；见下表 | 未模拟断电或磁盘满；首版不承诺崩溃事务回滚/通用跨目标自动重放 |
| M07 | 原始首次请求与最终候选恢复请求重复执行均 noChanges，未新增资产/类/注册；[final-repeat-analysis.json](final-repeat-analysis.json)、protected-files | 旧请求在后续合法制作后会因输出变化失效，这是预期保护 |

## 首次失败和恢复链

| 操作 | 首次发现 | 修复与复测 |
| --- | --- | --- |
| pilot-request | 3 Prefab 已保存/生成，Verify 发现设置按钮根名被 Unity 改成源名；[原失败](apply-first-failure.json) | Replace 后恢复实例名；RecoverNestedNames 仅定位唯一源 GUID，重新生成主页；apply-receipt completed |
| 原回执续接 | 编码导入器给 Binding.cs 添加 BOM，导致摘要变化 | 验证去掉开头 EF BB BF 后与原摘要完全一致，仅允许此精确变化，不放宽其他文件 |
| 第一版视觉 | Canvas 磁盘根 scale=0，世界矩阵把坐标压成零；文字裁切框过窄 | 按锚点帧/参考尺寸换算，明确文本容纳框。原错误截图保留 |
| layout-request | EUISafeArea 保存 anchors=0，但运行时撑满，标题/背景仍错位 | Frame 对已知 EUISafeArea 使用设计完整安全区；safearea-request completed；最终截图正常 |
| candidate-request | 已建 3 候选，行/列表保存生成；主候选嵌套来源 API 返回外层主页面，报 NESTED_SOURCE_CHANGED | 使用 nearest-instance-root 与 source-at-path 解析精确公共槽。导出实际候选契约，再以 candidate-recovery-request 原位完成3个已知资产；不重跑创建 |
| 负例夹具 | option 夹具首次复用 LitJson 缓存 JSON，先被摘要门禁拦下 | 重建 map 后得到 CRITICAL_OPTION_UNSUPPORTED；两个结果均保留，不把摘要失败冒称语义测试 |

原 `candidate-receipt.json` 保留 writing 状态作为历史证据，其结果已由实际契约和 `candidate-recovery-receipt.json` completed 查明、接续；当前没有待重放写入。不要点击原候选请求 Apply。`layout-input`、`safearea-input`、`current-input`、`candidate-recovery` 等 map 均基于前一轮已知制作结果与新导出的真实契约，原 S4 map 不动，没有通过改原摘要跳过调查。

## Unity、视觉和行为

相关 EditMode 三类 **18/18** 通过，job `732b67307ae440c6b191f9538fb9d810`，约14.94秒；见 [完整结果](editmode-tests.json)。包含零缩放 Canvas/安全区坐标与 Variant 嵌套槽回归，以及原动态列表 lease/cache/异常释放检查。非全量测试。最后语义门禁及 VerifySaved 增补后再次显式编译，完成时间：started `1793033009138`、finished `1793033013161`、domainReloadAfter `1793033019445`；[编译证据](compile-final.json)。随后负例和 VerifySaved 用已加载新代码执行。

Console 无编译/产品错误；最终保留一条 MCP 传输自身的 `WebSocket is not initialised` warning，后续调用持续正常，不能写成最终 warning=0。最后退出 Play，FrameworkScene；没有保存运行时场景变化。

真实 Play 通过：[runtime-behavior.txt](runtime-behavior.txt) 中记录 3→重排（key稳定/index变化/旧lease失效）、一次点击、20→0→3（cache20/单订阅）；候选页面创建的行逻辑是 EUIBuiCandidateDynamicOptionRowItem。Back 派发实际 Button 事件；Settings 实际按钮打开“当前场景：主界面”，关闭后 Start 实际按钮进入 EUIGamePlayPage。不是实体鼠标输入，也未重复完整 S3 压力/迟到回调全套测试。

![主界面最终 Game View](main-final.png)

![动态列表普通、禁用、选中与长文案](list-final.png)

文字保留工程字体、材质、皮肤及原文案（标题仍为 Ember Framework、按钮仍中文）。新副标题显式采用已有 DefaultLightSans，并不宣称它等于 ArialMT。新增隐藏 DisabledLabel 保留原始文字/隐藏状态，不伪造缺失参考图；禁用表现由原 Toggle 状态与 interactable 驱动。列表 Label 保留真实 EUITextRowLayout 容纳框，图片/进度不烘焙。尚未验证原生阴影/描边/遮罩、混排、所有屏幕比例、安全区设备或完整多语言；视觉不是逐像素 PSD/TMP 一致性验收。

## 保存及 S6 接续

已调用项目中心 `SaveTemplate` 普通保存 **617 文件**，[保存结果](template-save.json)、[比较结果](template-comparison.json) DifferenceCount=0。base 0.8.4/framework0.22.0 不变，contentHash 更新为 `846454530d463521510735d5e796ce43`，versionedContentHash 保持 `936dcb1d411afefcacd961cbad1d1d0d`。Templates~ 内容由该正式保存事务生成，未手工写快照。未 Bump/提交/推送/发布/消费升级。

S6 从当前主界面和原动态示例，以及 final 契约继续；候选页可用 GamePages.EUIBuiCandidateDynamicListPage 打开。工具 API 已在包内，真实制作请求可追溯。新的制作须导出当前契约并准备新明确请求，不重放已过期历史请求。S6仍需完成正式字体映射/效果、完整换肤与更广布局/交互验收；外部消费者迁移和旧资产清理按实际范围另行设计，不能把本轮候选验证当作全项目替换完成。

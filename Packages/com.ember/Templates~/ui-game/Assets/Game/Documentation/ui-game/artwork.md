# 美术替换与默认资源

## 默认视觉及恢复

默认库配置为 `Assets/Ember/Editor/UILibrary/DefaultLibrary.asset`，目录文件为同目录 `catalog.json`。在 **Ember → UI → UI 开发中心 → 公共库** 校验并生成目录。以配置和目录实际版本为准，继承说明中的历史皮肤数字不覆盖当前资产。

浅色背景、圆角面板、蓝色按钮与细进度条来自 `Assets/GameResource/Resources/UI/Common/`。`Textures/DefaultLight/Background.png` 和 `Atlas/DefaultLight/EmptyState.png` 是已随模板保存的 imagegen 中性素材；独立图标、RoundedSurfaceHiDPI 九宫图也是实际资源，运行不调用图片服务或外部缓存。三个业务页的文字和控件可独立编辑。

正文使用 `Fonts/DefaultLight/DefaultLightSans.asset`，源 `LiberationSans.ttf` 与 `OFL.txt` 同目录；中文继承工程字体及回退链。设置页保留 base 钉钉进步体与中文文案。本模板未宣称匹配 PSD 的 ArialMT 或覆盖所有语言。新字体先核对来源与许可，再通过字体中心和 ember-add-font 登记 TMP 资源、材质、皮肤及映射。

恢复时使用自己的版本控制/成对备份，或在项目中心预览来自已交付模板的受管内容差异，再选择补缺/合并。不要重跑初始库制作助手重建 GUID，也不要整目录覆盖产品 Assets。保留默认公共源，项目专属外观使用自己的 Variant/嵌套实例覆盖。

## 给美术的资料

1. 在正式 Prefab 保存后，用 `EUIContractService.Export(prefabPath)` 获取当前契约，再 `Write(outputPath, contract)` 输出。公共说明见安装包 `Documentation~/ui/eui-contract.md`。
2. 交付当前契约、真实比例截图、目标尺寸/安全区、可编辑节点与必须保留的交互；附当前公共库 catalog 的精确 GUID、公开槽、字体来源和需要的状态图。不要把历史回执内契约当当前真值。
3. PSD 由项目自己提供或从自己的设计工具新建；模板运行不需要仓库试点 PSD。按 `Documentation~/ui/bui-maker.md` 完成 Rename、UXP 导出 BUI3 / EUI map1；已有有效 BUI 可直接进入制作。

## 登记、制作和回执

新公共组件先制作独立 Prefab/Variant，在公共库配置登记真实 GUID、类别、公开槽和限制，校验后生成 catalog。绑定的引用必须来自该实例的公开槽，不能按显示名猜 GUID 或把另一个 Variant 的槽 ID 套过来。

使用 **UI 开发中心 → BUI 制作** 或 `EmberBuiService` 的 Analyze → Preview → Apply → VerifySaved。输入明确 target/nodeId 的 update、create、reference、preserve；原位更新匹配当前 objectId。Analyze/Preview 的差异和诊断先核对，再应用；Preview 是隔离构建诊断，真实视觉仍需保存回读与 Play 检查。输出目录由本次请求指定，制作差异、回执、验证结果都保留在那里，不硬编码开发机路径。

绑定通过 EUIBindingEditorUtility 维护，生成通过 EUIBindingCodeGenUtility.TryRegenerateCode。不要手改 Binding.cs、页面注册生成区、EmberManaged、Prefab YAML 或 meta。新业务页经 EUICreationService.TryBuildPlan/Create，Business 模式、Runtime/Module 层级；继承的 Framework 页只用既有用户文件和钩子，消费工程不越过生成保护。

completed 回执先 VerifySaved；输出未变才可认定 noChanges。保存成功而生成未完才用 ResumeGeneration。writing/unknown 先查询原操作与实际输出，不换名字重放。后续合法编辑导致 OUTPUT_CHANGED 时保留旧摘要，为当前明确变更导出新契约和新请求，不能改旧摘要绕过检查。

每次改完验证当前绑定、字体/图片、遮罩、状态来源、三次开关和动态行释放，以及实际支持比例。参见继承的 [美术流程](../base/ui-artwork-workflow.md)与 [动态列表](../base/dynamic-lists.md)。

# 基础 UI 美术替换与继续制作

归属：base。主界面、设置弹窗和动态列表示例使用正式保存的 EUI Prefab；默认导航仍使用 EUIMainPage，深青色 EUIBuiCandidateMainPage 是独立换肤示例。

## 入口与输入

使用 `ember-eui-build` 的美术替换流程。先核对当前包实际提供 `EmberBuiService` 和 BUI3 / EUI map1 / contract1(tool1.1)，再从已有 Prefab、源 PSD、工作 PSD、BUI 或回执进入。包说明位于 `Packages/com.ember/Documentation~/ui/bui-maker.md`；消费项目按实际 UPM 路径读取。

已有有效 BUI 时，不要求重新导出 PSD。用 `EUIContractService.Export/Write` 获取当前契约，明确每个 Target/nodeId 的 update、create、reference 或 preserve；原位更新核对当前 objectId。公共实例按精确 GUID 和 catalog 公开槽覆盖，不能假设不同 Variant 的槽 ID 相同。

通过 `Ember → UI → UI 开发中心 → BUI 制作`，或公开 `Analyze → Preview → Apply → VerifySaved` 完成制作。Preview 为隔离构建诊断，不是驻留视觉窗口。生成仍由正式 EUI 接口完成，不手改 Binding、EmberManaged、Prefab YAML 或 meta。

## 必须保留

- EUIMainPage 的开始与设置导航、Framework 模式、IsFramework、PageType 和用户代码；设置来源文字与关闭返回栈。
- 动态列表工具栏及 Back 的 `ClosePage(Page)`；key 使用 Id，index 只表示当前顺序。
- 动态 Item 由宿主与 lease 管理；Label、Icon、Progress 由数据设置，长文本沿用 EUITextRowLayout。
- 禁用、选中、可见性分开验证；公共源保持不变，项目换肤使用实例覆盖或独立测试资产。

## 素材和字体

当前主界面与设置沿用现有钉钉进步体 TMP，副标题及动态行沿用 DefaultLightSans（Liberation Sans）；字号、颜色可以来自 BUI，保留字体策略不代表 ArialMT 已映射。字体来源文件和许可随现有字体目录核对；需要新字体时使用 `ember-add-font`，不把默认字体冒充设计字体。

设置面板采用九宫图，设计样本 256×256，导出 PNG 130×130，四边 64、中间 2；保持工程弹窗几何和中文文案，CanvasScaler 参考 1280×720。新版本切图放独立路径，避免覆盖已登记摘要的图片。

## 续接与验收

先检查原回执与当前输出。completed 且输出不变时可验证 noChanges；保存成功但生成未完时才使用 ResumeGeneration；writing/unknown 不盲目重放。后续合法修改使旧输出摘要失效时，保留历史回执，为明确的新修改建立当前契约和请求。

每次改完检查保存回读、编译和域重载、Console、相关用例、同尺寸同状态画面、导航与遮罩、三次开关、列表复用及两种比例。当前试点范围是编辑器 1280×720、1600×720、完整 Screen.safeArea 和显式内边距模拟；不代表异形屏真机、全语言、完整 Photoshop 效果或构建通过。

独立换肤示例通过 `EUIManager.Instance.ShowMainPage(GamePages.EUIBuiCandidateMainPage)` 打开，继承原逻辑，无专用换肤代码；正常启动不切换到候选。其设置关闭仍服从原状态机来源，不能据此声称所有外部消费者已自动迁移。

修改模板维护源后按项目中心普通保存并比较；版本发布、Bump 和消费部署另行执行。开发仓库的 `art/ui/S6` 与 `docs/dev/ui-production-upgrade/evidence/S6` 是试点资料，不是消费项目运行依赖。

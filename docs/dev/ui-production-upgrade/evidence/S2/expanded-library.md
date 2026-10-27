# S2 公共库扩充交接（2026-10-26）

本轮完成此前分析清单的 S2 范围。新增 29 个 Prefab：26 个 Item、2 个弹窗 Page、1 个演示 Page。连同原有 8 个库条目，目录共 37 项，其中 36 项复用资产、1 项演示页。S1 所有权示例不计入。默认皮肤 EmberDefaultLight 1.1.0。

## 已实现的资产

公共 Item 在 `Assets/GameResource/Resources/UI/Common/Prefabs/`，以下名称均带 EUI 前缀、Item 后缀：

| 类别 | 本次新增 |
|---|---|
| 按钮 | SecondaryButton、DestructiveButton、IconButton、CloseButton、BackButton、AmountButton |
| 选择 | Tab、TabGroup、SegmentedControl、Switch、Dropdown |
| 设置与数值 | SettingToggleRow、Slider、SettingSliderRow、Stepper |
| 展示与反馈 | EmptyState、Badge、IconCard、Toast、Tooltip |
| 布局与标题 | HorizontalListHost、GridHost、TitleBar、SectionTitle |
| 输入 | Input、SearchField |

`Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/` 新增 EUIAlertPage、EUIConfirmPage、EUIExpandedLibraryPage。页面均通过创建中心创建和再生成，弹窗 PageType=Popup；没有改写 Binding.cs。资产完整 GUID、实际 Variant 状态及 Binding 快照见 [expanded-assets.json](expanded-assets.json)。

真实继承包括按钮样式、关闭/返回按钮、分段控件、输入搜索、列表宿主、弹窗及预览页。Item 复用已有 EUIButtonEx、EUIToggleEx、EUIToggleGroupEx、TMPEx、EUIImageEx、ContentSizeFitterEx。原有 OptionRow 的资源/Binding 双继承示例继续保留。EnableState 与 interactable 没有合并。

新增框架通用展示脚本位于 `Packages/com.ember/UIExtension/Runtime/UIExt/`：
EUINumericControl、EUIInputActions、EUIBadgeView、EUIIconCardView、EUITimedNotice、EUIAnchoredTip、EUIStaticChoiceMenu。
它们只处理展示和输入，不查询产品数据。数值组件支持范围/步长/静默初始化及单次 Changed；输入支持清除、只读和外部错误；Badge 支持点/数量/标签；卡片支持标题/图标/数量/选中/锁定；Toast 新消息替换旧消息并到期隐藏；Tooltip 显式点击打开并在 Canvas 内定位；Dropdown 只操作预制选项，支持禁用选项、外部点击关闭及 Cancel 事件。

TabGroup 复用固定子 Toggle；预览页用四个预置面板演示切换。横向/纵向/网格宿主仅 ScrollRect、裁剪和布局，演示页预置卡片。没有增加数据容器、运行期列表创建、对象池、TabLoader、虚拟列表或 S3 生命周期框架。

## 使用入口

- `Ember/UI/UI 开发中心 → 公共库`；配置 `Assets/Ember/Editor/UILibrary/DefaultLibrary.asset`，只读目录 `catalog.json`。
- 扩充预览：Play 后 `EUIManager.Instance.ShowMainPage(GamePages.EUIExpandedLibraryPage)`，不接默认主菜单。
- 弹窗：`ShowPopup(GamePages.EUIConfirmPage, new EUIDialogRequest { Title=..., Message=..., Completed=... })`；结果 Accepted / Cancelled / Dismissed 在关闭完成后回调。Alert 用同一请求结构。
- 创建：`EUICreationService.TryBuildPlan/Create`；绑定 `EUIBindingEditorUtility`；生成 `EUIBindingCodeGenUtility.TryRegenerateCode`。制作记录脚本 `Assets/Game/UI/Editor/UIFoundation/ExpandedUILibraryBuilder.cs` 只创建缺失项，不用于覆盖重做美术。
- 每条真实槽位声明可覆盖文字/字号/颜色、图片、interactable 等属性；配置 family、GUID:localId 及目录摘要已接入 S1 导出。完整预览契约 [Expanded.eui.contract.json](Expanded.eui.contract.json)，连续导出差异 0。

## 图片和视觉

新增 Close、Back、Search、Help、Plus、Minus、Chevron、Lock 八张 imagegen 透明图标。正式文件位于 `Common/Atlas/DefaultLight/`，不是 Unity 内置 Sprite 或字体符号。背景、空态和 Checkmark 复用已有正式素材，不重复生成。素材提示词、用途、SHA256 和导入记录见 [expanded-materials.json](expanded-materials.json)。

真实 UI 使用浅色表面、圆角、边界、留白、轻阴影、动态 TMP。新增控件复用已修正的 RoundedSurfaceHiDPI 九宫格。首轮发现滑杆手柄纵向拉长、标签选中背景未随实例伸展，已修复源 Prefab 和制作脚本；标签组默认选中也修正为首项。

最终截图：
- [控件修正后](expanded-controls-final.png)
- [表单和下拉](expanded-dropdown.png)（此图在滑杆手柄修正前，仅证明输入与下拉状态）
- [反馈组件](expanded-feedback.png)
- [确认弹窗](expanded-confirm.png)
- [长正文弹窗](expanded-dialog-long.png)
- [静态布局](expanded-layout.png)
- [1024×768](expanded-4x3.png)

## 已验证

- 本轮最终显式编译 started=1793018221489、finished=1793018224987、domainReloadAfter=1793018232086，目标工程路径已核实。
- EUILibraryEditTests 8/8；新增 EUICommonControlsEditTests 修正测试夹具后 4/4。只运行相关测试。首次 9/12 的失败记录和原因保留在 [expanded-validation.json](expanded-validation.json)，不冒充一次全过。
- Play 实例重新启用后：数值增加一次值变为8、事件1次；只读输入拒绝清除，恢复可编辑后可清除；下拉变化1次，禁用项拒绝选择。
- 确认弹窗取消回调1次、Alert 接受回调1次；Toast 到期自动消失；真实 TMP 长正文被 ScrollRect 裁剪，动作区保持；固定列表与网格真实嵌套引用。
- 37条配置无校验错误，目录当前有效；重复身份、失效槽位、过期目录、实例覆盖独立、Binding继承诊断由上述相关8项测试覆盖。
- 2560×1440 GameView 与1024×768检查；临时尺寸已移除并恢复原选择5、原10项尺寸。交互使用程序设置、UnityEvent 和 EventSystem pointerClick 派发，未声称人工触屏。
- 本次未改原六页 Prefab/用户逻辑；生成注册新增3页。回到原主界面未见异常。FrameworkScene未标脏、退出Play。

## 历史问题与未验证边界

制作器首次因同名子实例生成重复槽位被拒绝，修正唯一子名后继续，保留已生成 GUID。测试夹具未触发普通 MonoBehaviour 的 EditMode 生命周期，修正显式生命周期测试并另做 Play 验证。证据脚本一次因 Color 自引用序列化失败，改用 Unity JsonUtility 成功。临时 GameView 首次删除索引不正确产生日志，后按核实的绝对索引清理成功。Alert 在 Showing 期间关闭产生框架延迟关闭提示，最终回调验证通过。这些记录保留，不用清空 Console 掩盖历史。

尚未验证：干净消费项目部署/构建、实体触控/IME/无障碍、完整键盘导航、竖屏刘海与完整多语言/CJK；派生模板传播未执行。Dropdown 固定三个选项是默认资源容量，可编辑 Prefab 扩充；没有动态选项生成。Tooltip 固定面板适合短说明，未做任意长正文自动分页。

## 普通保存与 S3 入口

项目中心普通保存 base 成功567文件，保存后比较差异0。base0.8.4、framework0.22.0、contentHash=`20a0b4a5bfb39b175a5ca567ed4229b9`、versionedContentHash=`936dcb1d411afefcacd961cbad1d1d0d`。见 [expanded-template-save.json](expanded-template-save.json)。没有 Bump、Git提交、推送、发布或消费部署。

用户要求不进入 S3，本轮在此停止。将来 S3 从 [03-items-and-containers.md](../../03-items-and-containers.md) 开始，复用 ContentHost/HorizontalListHost/GridHost 的 Viewport/Content、IconCard 展示接口及 EUIItemFactory，另行确立批量刷新、取消和释放的单一所有者。


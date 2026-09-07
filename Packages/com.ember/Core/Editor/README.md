# Core.Editor — 项目中心与场景配置

## 工具入口

| 入口 | 实现与行为 |
|---|---|
| `Ember/项目中心` | `EmberSetupWindow`；项目初始化 / 项目校验，embedded 环境额外显示模板开发 |
| 场景映射 | `EmberSceneMapping` / `EmberSceneMappingCreator`，维护状态与场景引用 |
| Build Settings 同步 | `FrameworkSceneBootstrapper`，同步框架场景和业务场景，管理进入/退出 Play 的场景恢复 |

生成物校验统一从项目中心的“项目校验”页操作，不再保留独立的“校验生成物一致性”菜单。

场景映射资产位于 `Assets/Ember/Editor/SOs/EmberSceneMapping.asset`。这是项目可写配置，
不是旧框架源码目录。`EmberSceneMapping` 提供 `PopulateFromStates()` 与 `SyncNewStates()`。

## 模板实现

| 文件 | 职责 |
|---|---|
| `EmberProjectSetup.cs` | 模板扫描、兼容过滤、部署、创建/保存/加载、版本和同步编排 |
| `EmberProjectSetupPanel.cs` | 消费端项目初始化和活动模板状态 |
| `EmberTemplateDevelopmentPanel.cs` | 左侧模板树；概览与效果 / 内容差异 / 父级更新 / 版本与设置 |
| `EmberProjectValidationService.cs` / `EmberProjectValidationModels.cs` | 只读报告、基准说明、文件差异、资源元数据、场景和管理区检查 |
| `EmberProjectValidationPanel.cs` | 分类筛选、问题定位、复制报告；状态变化后提示报告过期 |
| `EmberTemplatePreviewPanel.cs` | 真实截图草稿、版本效果确认、场景打开与运行验证；图片独立存放于 Preview~ |
| `EmberTemplateModels.cs` | schema v2、编辑/部署记录、变更与冲突模型 |
| `EmberTemplateInheritanceEngine.cs` | 文件树 hash、谱系、三方分类与资产校验 |
| `EmberTemplateSceneMergePlanner.cs` / `EmberUnityYamlMerge.cs` | 显式预览中的场景语义合并与工具适配 |
| `EmberTemplateTransaction.cs` | 暂存、过期检查、应用与多目标回滚 |
| `EmberTemplateEditorWindow.cs` | 已弃用的旧窗口兼容壳，无独立模板编辑器菜单 |

模板通过项目中心保存；不要从外部脚本改快照或 hash。
开发仓库的 [模板体系](../../../../docs/dev/template-upgrade-system.md) 说明版本、同步和消费端边界。

## 操作流程

先加载目标模板，在项目 Assets 中修改，再用“内容差异”检查保存结果。“预览并保存到模板”会先提示保存场景、保存资源并重新计算差异；普通保存保留版本，之后在“版本与设置”中封存补丁/次/主版本。
选中模板与当前编辑模板分别显示；新建表单按需打开，高级设置收纳手动版本和删除操作。父级更新默认显示冲突，取消筛选可查看自动处理项。同步当前编辑模板前，必须先保存项目修改，避免自动重载丢失内容。
左侧模板树与右侧详情之间的分隔条可拖动调宽，双击恢复默认宽度；宽度在当前 Unity 会话内保留，缩小窗口时自动限制以保留右侧操作空间。模板名称、ID 和状态按栏宽换行，左右区域独立纵向滚动。

项目校验检查的是当前项目；业务新增、修改、删除只记为“差异”。消费记录没有历史文件 hash，因此明确标注与当前包模板的参考比较，不把版本相同当成内容已经升级。UI 检查由 `Ember.UIExtension.Editor` 的 `IEmberProjectValidator` 适配器复用只读目录扫描提供，Core 不反向引用 UI 编辑器。

效果图可以从运行中的 Game 视图捕获或导入真实 PNG/JPEG。所有新图先存为草稿；退出播放模式、保存并封存模板后，只有项目内容仍匹配截图来源时才可确认版本效果。版本、内容 hash 或框架兼容声明变化后，旧图提示待更新。没有截图时显示明确的空状态；工具不自动加载其他模板生成效果图。

本轮新增功能已补充 `EmberProjectValidationEditTests`；Unity 编译、EditMode 执行、面板布局和真实截图链路仍需在 Unity 中验收。

## 其他编辑器工具的真实位置

- 快速场景打开、Toolbar、Odin 集成检查在 [FrameworkTools/Editor](../../FrameworkTools/Editor)。
- 日志配置创建和 Inspector 在 [Basic/Editor](../../Basic/Editor)，配置资产是 `Assets/Resources/EmberDebugConfig.asset`。
- 依赖安装与升级在 [UPMManager/Editor](../../UPMManager/Editor)。
- UI 创建、Binding 生成与维护在 [UIExtension/Editor](../../UIExtension/Editor)。

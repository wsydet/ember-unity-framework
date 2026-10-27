# S7 本地实现与交付准备

2026-10-27，活动工程 ember-unity-framework，Unity 6000.5.4f1 / Windows Editor。S7 本地实现、T01–T05 已完成；最终封存、发布和 T06 消费部署未执行。工作分类为 ui-game 模板升级，未新增通用框架运行 API，没有产品玩法或必备 GameManager。

## 实际身份

| 对象 | 最终事实 |
| --- | --- |
| 框架声明 | 0.22.0，当前 embedded 开发包；本轮无提交/tag/远端发布 |
| base | 0.9.0 / stable；本轮经中心执行必要的本地 minor Bump |
| base 内容及封存 | `f5ae9bcc0e3bd361a65b056b367cda71`，磁盘计算一致 |
| ui-game | 0.1.0 / preview，当前正式编辑身份 |
| 父级 | base 0.9.0；parentContentHash 与 ParentSnapshot 磁盘均为上述 base hash |
| ui-game 普通保存 | 675 文件，保存后 DifferenceCount=0 |
| ui-game 内容 | `98470c704b0e51b0d40df193b8b8a088`，编辑记录、模板元数据、磁盘计算一致 |
| ui-game 版本封存 | `f5ae9bcc0e3bd361a65b056b367cda71`；新增实现未最终 Bump |
| 最终状态 | Play=false，FrameworkScene 未标脏；原 GameView 选项5、10个尺寸均恢复 |

初始 base0.8.4 普通保存627文件、差异0，但 contentHash 与 versionedContentHash 不同。先完成备份、校验和派生预览，再由 `BumpTemplateVersion("base",1)` 封存为0.9.0。minor 对应 Page 命名及 UI 制作结构变化，没有提升框架兼容声明。未重复加载 base。

通过 `CreateTemplate(...,"base")` 正式派生、再 `LoadTemplate("ui-game")`，不是当前副本另存冒充父关系。操作、身份、父快照见 [创建](template-created.json)、[加载](template-loaded.json)、[base封存](base-bump.json)。保存前53项差异、错误0；保存后56个UI校验、错误0，唯一警告是有已保存未封存内容。见 [保存前校验](pre-save-validation.json)、[保存](template-save.json)、[最终状态](final-state.json)。

其他派生模板仅预览，未 Apply：platformer-2d 758项、source3d-2p5d 212项、visual-novel 688项父更新差异，均含冲突；仍基于base0.8.4。其内容hash保持原值，后续须单独授权逐项合并。见 [影响清单](derived-impact.json)；不能把 base Bump 当作这些模板已同步。

## 保护与恢复

[备份](backup.json)位于仓库 `.utmp/ui-production-S7-20261027/before`，1520文件 SHA256 核对，旁边保存manifest、Git状态、working/staged补丁。恢复时比对manifest，只选择获准路径和meta配对恢复；不要批量回滚整个工作区或恢复所有Git删除，也不要直接写模板身份/快照。

[最终保护检查](protection.json)：1520个既有文件无缺失，1159条原删除均保留。相对备份变更仅正式身份、文档入口、两个状态用户区、生成页面注册、动态TMP字体、Unity生成的slnx、base元数据及本轮进度交接文档。两个状态的EmberManaged内容逐块一致。S4四份原始输入、S6美术/回执、其他模板资产和公共技能源均未改动。新增ui-game正式快照仅由中心产生。

## 实现与制作

- `UiGameModule` 显式 Gameplay / Enabled=true；Session拥有代次、三步进度、暂停、导航、完成与释放状态。按钮只提交意图，旧会话不能变更新局。没有存档、颜色规则、九关、评分或产品美术。
- FrameworkScene正式启动，复用GameSceneScope/SceneFlow、Main/Gameplay/Settings状态；主页→局内→结果→重开/返回闭环。Settings来源与Pop沿用base，业务暂停钩子明确接线。
- 三个新页面经EUICreationService计划/创建，Business、Runtime/Module/UiGame；真实EUIStandardPage Variant、嵌套ActionButton公开label槽。绑定通过EUIBindingEditorUtility和TryRegenerateCode，读取实际生成字段后编写用户文件。见 [创建](page-creation.json)、[绑定](page-layout.json)、[最终契约](final-EUIUiGamePlayPage.contract.json)。初始布局脚本为制作历史，最终竖屏几何以正式Prefab和final契约为准，不重放脚本。
- 动态列表复用现有宿主与EUIDynamicOptionRowItem，稳定key `step-N`，index仅展示；刷新使旧lease失效，页面关闭解绑并Dispose列表。未重做S3容器、池或虚拟化。
- 页面异步打开捕获session并通过isCurrent拒绝失效请求。重开只接受当前完成会话；返回等待正式状态请求Completion，失败撤销当前导航锁。
- 公共库37项校验0错误、catalog当前；新增页/既有四对象绑定均0错误。111项依赖均为项目Assets或安装Packages，无开发机绝对依赖或产品缓存。见 [资源](resource-audit.json)、[依赖](dependencies.json)。
- 默认实际背景/空态/图标继承imagegen素材，未新增图片或字体。DefaultLightSans使用现有LiberationSans及OFL；设置保留钉钉进步体。新增文字导致TMP动态字形资产变化，不宣称匹配ArialMT。四份旧契约的GUID/绑定/用户逻辑不变；资源摘要因字体变化、动态页注册摘要因新增页面变化。见 [契约比较](contract-comparison.json)。

正式说明位于 [ui-game/README](../../../../../Assets/Game/Documentation/ui-game/README.md)，另含生命周期/存储、美术流程、升级和术语；均进入675文件普通保存。文档链接及字体来源检查见 [文档审计](documentation-audit.json)。使用者只需要模板、继承base文档和实际安装包，不要求本证据目录或试点PSD。

## 验收

| 编号 | 结果 | 范围与证据 |
| --- | --- | --- |
| T01 | 通过 | 正式创建、父版本/内容/快照、编辑身份和谱系检查，见最终状态 |
| T02 | 通过 | FrameworkScene启动，中性三步→结果→重开；[最终运行](runtime-check-final.json) |
| T03 | 通过（编辑器范围） | 暂停拒绝输入、遮罩命中EmberBgMask、恢复同会话；双击仅一次推进/重开；旧lease/会话失效；三轮活动页均3，动态行每轮创建/销毁3/3；最终7会话创建/释放7/7，见[清理](runtime-cleanup.json) |
| T04 | 通过（本地受管资源范围） | 37项公共库、111依赖、真实素材、正式文档、现有制作工具入口；本轮未重跑PSD/BUI历史Apply，也不替代消费端安装验证 |
| T05 | 通过（授权范围） | base本地封存、ui-game普通保存675/差异0/错误0，预期未封存警告1；无ui-game最终Bump |
| T06 | 未验证 | 没有指定消费工程和实际交付包；未安装、部署、构建或消费再生成 |

最终主动编译 started1793037580960 / finished1793037583440 / domainReloadAfter1793037588193，ready且不compiling/updating，见 [编译](compilation.json)。新增相关EditMode **4/4**，最终job `38dcf612dbdc4e80960ad691f0f36db3`，非全量，见 [测试详情](tests.json)。此前同4项首次也通过，因域重载后旧job详情已释放，本轮保存最终详细结果。

真实Play截图是已保存正式Prefab的运行结果，覆盖720×1280与720×1600；操作主要为EventSystem射线后派发，少量清理/模拟使用Button事件调用，不称实体触控或人工试玩。见 [视觉审阅](visual-review.md)。未验证真机异形屏、构建、Addressables打包句柄、全语言/键盘/IME/无障碍、横屏和消费部署；当前引擎以Editor资源提供方式加载。

## 首次失败与恢复

1. 新模块OnDestroy初次隐藏基类造成CS0114，改为显式IEmberModule实现与基类override，后续编译通过。
2. 第一次运行检查以固定1800ms等待转场，在返回后下一轮提前读Play页面，发生NullReference；前11项通过，完整失败记录 [runtime-check.json](runtime-check.json) 保留。改为有界等待页面Opened及Fsm非转场，再从FrameworkScene重新运行；[最终21项断言](runtime-check-final.json)全部通过。没有将未知写入另名重放，这是已知失败的只读验证/输入操作修正后新一轮运行。
3. 清理GameView第一次错误传自定义相对索引，Unity报告 Invalid index 4/3、列表未变；查实际12项后按绝对11/10移除，最终保留原10项。日志见 [清理错误](console-cleanup-errors.json)。已记录后清空控制台再检查，最终错误/警告0，见 [最终Console](console-final.json)。
4. 过程中的EUIBinding命名空间、EUIPage查找类型、临时C#局部变量、诊断Fsm类型错误均在内存编译前拒绝，没有执行资产写入；apply_patch重复路径/BOM匹配失败未落盘，之后仅写新生成的用户逻辑文件。Git LFS沙箱读取失败使用禁用过滤器的只读status/diff完成；rg不可用使用文件工具。
5. 主动编译后曾有MCP自身WebSocket未初始化warning，后续请求正常，未发生需重试的断连。无未决写入/测试，未重放历史S5/S6 Apply，未改旧回执摘要。

## 下一阶段具体待办

1. 获准发布后核对当前ui-game已保存内容与版本兼容性，通过中心执行最终Bump；不要沿用初始0.1.0封存hash冒称交付实现已封存。
2. 核对本轮与此前大量未提交内容的提交边界；按授权提交真实技能维护源，用正式工具从真实sourceCommit生成AISkills~ bundle。S6技能源已维护，本轮未修改；不手写bundle或伪造sourceCommit。
3. 按ember-release-framework流程完成包版本、模板声明、依赖、变更说明、提交/tag/远端发布，并记录真实可安装来源。本轮未提供虚构tag或commit。
4. 指定消费工程后通过UPM Manager安装真实交付包、项目中心部署ui-game和正式技能入口，重新编译、正式启动并执行一次Business页面再生成，再判定T06。已有工程先三方保护/合并。
5. 消费验收后按已确认ColorOrder V2路线先三个短关，再九关与巡游；规则、评分、进度、美术在消费项目实现，本轮不改变路线。

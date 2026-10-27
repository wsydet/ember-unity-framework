# S8 实施与验收记录

2026-10-27，活动模板 base 0.9.0。完成全量 UI 组件化迁移，包含 GM，已普通保存，未封存或发布。

## 实际交付

- 共 61 个 UI Prefab：60 个带 EUIBinding，另一个是布局基础 EUISafeArea。公共库从 37 项增加到 44 项。原生 Unity UI/TMP 仍是公共组件源的实现基础；业务页面的渲染控件均可追溯到已登记的库源，运行时遮罩和程序化转场由既有框架负责。
- 迁移主菜单、设置、玩法、背景、加载、GM、Ownership 页/Item，以及 LibraryPreview、LibraryPopup、Alert、Confirm、ExpandedLibrary、DynamicOptionRow 中残余独立控件。BUI 候选继续继承正式源。
- 新增 7 个 StructureOnly Item：Text、Image、RawImage、CircleProgress、FreeScrollHost、TransitionSurface、ContinuousSlider。使用 EUI 中心预检与创建；库配置和槽位经公开服务验证并生成 catalog。
- GM 保留 13 个字段及原事件逻辑，FPS/拖拽、时间控制及关闭后恢复 1×、状态文字、双轴滚动、图像、增强开关、圆形进度、退出均可用。连续 Slider 保持原连续值行为，不引入数值步长量化。
- 加载页保留 Image.fillAmount、CanvasGroup、自定义设置、普通/快速进度和转场效果。全屏底图使用空 Sprite 实色组件覆盖，防止圆角图在屏幕四角漏底。
- 本轮没有新增图片需求，复用既有 macOS 风格素材，没有生成或声称完成新的 PSD/BUI。S4/S6 原始美术与回执保持，后续换 UI 必须重新导出当前契约预检。

## 证据与结果

| 检查 | 结果与证据 |
| --- | --- |
| 备份 | `backup.json`：2927 文件 SHA256 校验；恢复目录 `.utmp/ui-production-S8-20261027/before`，仅按 manifest 选择恢复 |
| 全量结构 | `final-audit.json`：61 Prefab，60 Binding，44 库条目；绑定错误、库错误、缺失脚本、业务独立渲染控件均为 0，catalog 有效 |
| 契约连续性 | `final-contract-comparison.json`：原 53 个对象的 GUID、模式、字段名/声明类型/Framework/语义标记、自定义设置、用户逻辑摘要一致；无 blocker |
| 正式生成与编译 | `generation.json`：17 个受影响对象经公开生成入口；`compilation.json`：请求后编译和域重载完成，无编译错误；短暂 MCP WebSocket 警告后调用正常，最终 Console 错误/警告 0 |
| 相关 EditMode | `tests.json`：EUILibraryEditTests 与 EUIContractEditTests，16/16 通过，13.35 秒；`tests-resize.json` 转场相关 2/2 通过，合计 18/18；未全量测试 |
| 正常导航与 GM | `runtime-main-gm-final.json`：21/21 检查通过，EventSystem 射线/事件派发及 Slider/ScrollRect/Draggable 输入处理器验证 |
| 双比例与扩展行为 | `runtime-extended-final.json`：1280×720/1600×720、主/设置/玩法、确认/取消/Alert 回调、动态列表 3→20→0、旧 lease 失效、所有权、加载进度；18 个断言通过，转场池断言修正见下文 |
| 转场与库示例 | `runtime-finish.json`：退出视觉隐藏、池重置、快速进度隐藏、Expanded Forms/弹窗、LibraryPopup、候选列表和主页面字段，8 个断言通过；尾部测试导航清理假设修正见下文 |
| 最终 GM 复测 | `runtime-final-smoke.json`：公开 Label 保存、两个比例滚动及退出射线共 4 项通过；`cleanup-final.json` 确认实际退出 Play |
| 安全区 | `safe-area.json`：1600×720，运行实例左右 60/上下 24 padding 模拟，面板完整落在安全范围；不代表异形屏真机 |
| 普通保存 | `template-save.json`：中心 SaveTemplate 保存 641 文件；`final-state.json` 差异 0、错误 0，唯一警告为按授权未封存 |
| 保护 | `protection-final.json`：1574 个保护文件中 1573 个一致，另 1 个为回归用例；原本干净的转场源码另以 HEAD 基线补充备份，见 `framework-baseline.json`，1159 条既有删除未恢复，ui-game 与其他 3 个派生元数据/快照未改 |
| 清理 | `cleanup-delivery.json`：非 Play、场景未标脏、GameView 原索引 5/原尺寸数量 10 恢复；`console-final.json` 为空 |

普通保存后 base 仍为 0.9.0，框架仍为 0.22.0。contentHash 为 `10a068917ab92c18c53f948abd556734`；versionedContentHash 保留 `f5ae9bcc0e3bd361a65b056b367cda71`。未 Bump、Git 提交、推送、tag、发布或消费部署。

## 失败、修正和边界

证据保留首轮失败，未把失败报告覆盖成通过：

1. 初始节点清单尝试直接序列化 Unity Vector2，遇到 normalized 循环引用；契约已先成功输出，清单改为字符串后完成，无资产写入。
2. 设置文字节点替换后 HasGameObject 残留 false，完整绑定检查发现后通过快照 API 修复；最终 60 项绑定校验通过。
3. 首轮视觉发现 GM 旧滚动组仍隐藏、全屏背景继承圆角图片、部分嵌套文字覆盖未登记，均在正式 Prefab 修复。GM Toggle 必须写公开 Label 槽，GetComponentInChildren 先取到 Selected 辅助文本；最终独立复测验证了正确槽位持久化。
4. 运行脚本先用了错误的类型/命名空间及不可访问 setter，均为工具内存编译失败，未执行。更正后实际运行。加载测试通过反射模拟内部“准备完成”标记；真实主菜单/玩法往返另外通过正式状态机验证。
5. 首轮射线未使用 ScreenSpaceCamera，未命中 GM；修正相机后 21 项通过。扩展脚本一度仅等待页面打开，未等状态机转场结束，截到加载页；改为同时等转场完成后双比例路径通过。
6. `PlayExitAsync` 将池方块缩至零，并不清空 `_activeCount`。原 `HasActiveBlocks == false` 断言不符合实现；后续实测 0 个可见方块，显式池重置后 inactive。没有为错误测试改框架行为。
7. 连续 ShowMainPage 示例会保留主页面栈；向已有较低页面再 Show 不会代替弹栈。补充脚本最后等待原主页超时发生在 8 项验证后。最后独立启动、GM 双比例及完整退出验证完成，无残留测试页面。
8. GameView 首轮清理使用了相对索引，工具调用没有抛异常但 Console 记录 2 个 Invalid index，数量仍为 12；检查实际数量后改用已核对名称的绝对索引，恢复为 10。错误保留在 `console-cleanup-errors.json`，归档后清空并读取最终 Console。
9. Play 为包内 DingTalk 动态字体生成额外图集；退出后从本轮备份精确恢复该文件并重新导入，保留原有修改。最终 1573 个保护文件一致，已有回归测试文件为本轮有意修改；另外修改了原本干净的转场源码，其 HEAD 基线已补充备份。模板内 DefaultLightSans 的动态字形数据随本轮普通保存。

10. 最终视觉复核发现复用幕布在父容器尺寸改变后沿用旧网格，已修复 EUITransitionBlock 的尺寸缓存判断并添加回归用例。`runtime-resize.json` 在同一实例的 1280×720 与 1600×720 各采样 153 点，未覆盖点均为 0；相关 2 项测试通过。Loading 百分比改为白色，Ownership 正文恢复深色以保证对比度。

未做构建、真机输入、全语言、竖屏、干净消费部署、派生模板同步或发布。测试以编辑器 EventSystem 派发、相关输入处理器及少量直接按钮/数据调用为主，不宣称硬件鼠标全流程。加载快速模式为生命周期级验证，未另外模拟磁盘/网络慢加载故障。

## 视觉复核

最终 GM 使用 `delivered-gm-1280.png` / `delivered-gm-1600.png`；完整内容和启用后的时间滑杆均在屏内。常规页面使用 `final-main-*`、`final-settings-*`、`final-gameplay-*`；弹窗、列表、所有权和候选截图独立留存。早期 `*-first-*`、旧 `gm-*` 及中间 `final-gm-*` 保留用于追溯，不作为最终 GM 外观。

Loading 最终外观见 `delivered-loading-final.png`（2560×1440）；所有权页见 `delivered-ownership-1280.png`。旧 Loading 漏边或深色百分比截图仅作为中间证据保留。

字体继续使用 DefaultLightSans 与已有 DingTalk 中文回退，中文略带倾斜是既有字体外观；未声称还原 macOS 系统字体。布局、文字可读性、遮罩、滚动裁切、状态和按钮完整性已逐图检查。

## 后续补充

Loading 已进一步迁移为公共进度条的专用 Variant，数字显示职责收拢到 Item。当前保存状态与新增验证见 [Loading 补充记录](../S8-loading-progress/README.md)；本页保留最初 S8 的历史证据。

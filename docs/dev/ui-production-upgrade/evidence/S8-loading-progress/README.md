# S8 补充：Loading 专用进度条

2026-10-27，按用户要求将 Loading 进度展示整体迁入公共进度条的专用 Variant。已实施、验证并普通保存，未封存或发布。

## 结构与职责

- 原 S8 只有 Image/Text 两个基础组件复用，数字仍由 Loading 页面格式化。本轮改为 `EUIProgressItem → EUILoadingProgressItem → EUILoadingPage`，由 EUI 中心 SourceSkeleton 创建真实 Variant；公共源本身未改。
- Variant 位于 `Assets/GameResource/Resources/UI/Module/SceneFlow/Prefabs/EUILoadingProgressItem.prefab`。继承 Slider、Track/Fill、Value，在 Variant 内设置蓝色细条、浅色轨道、白色数字及布局。
- Item 用户逻辑负责 Clamp01、Slider 值、向下取整百分比、两个独立显示开关、隐藏与复用重置。动态文字使用 TMPEx.SetSource；仅整数百分比变化时更新文本。
- Loading 保留假进度时序、准备成功门槛、快速路径、CanvasGroup 淡入淡出与转场。对外 SetProgress 和自定义设置不变，内部 Image/Text 字段经正式 Binding API 改为 LoadingProgress UILogic；嵌套逻辑由 Bridge/父页唯一拥有，不再额外创建 EUIItem。
- 页面 GUID、模式、类型、自定义设置、页面注册和用户钩子保持。Loading 的维护源位于当前 Assets，其展示调用已调整；Binding 由中心重新生成，模板快照由项目中心保存。

## 证据

- backup.json：361 文件 SHA256 校验，恢复源 `.utmp/ui-loading-progress-20261027/before`。protection.json：仅 7 个既有文件为预期改动，原 1159 个删除未恢复。
- migration.json：真实 Variant 与公共父源；structure.json：页面/Item 绑定无错误且新程序集已加载。
- final-audit.json：62 Prefab、61 Binding、44 公共库条目；绑定错误、缺失归属的渲染控件、库错误均为 0，catalog 有效。
- tests.json：两个相关 PlayMode 测试类 13/13 通过，含准备等待、快速隐藏、零时间缩放、手动进度门槛、四种条/数字组合以及 Item 独立显示与重置。最后调整包内夹具兼容旧模板后，verification.json 中相关 5/5 复测通过；不重复计为 18 个不同用例。
- 最后主动 MCP 编译 started1793067815991 / finished1793067818480 / reload1793067823367，确认域重载与就绪。曾出现 MCP WebSocket 未初始化警告，后续调用正常，最终 Console 0 错误/警告。
- runtime.json / loading-37.png：真实运行 Loading，子逻辑存在，37% 与 Slider 0.37 一致，快速路径全部隐藏；2560×1440 截图已目视复核。
- template-save.json / final-state.json：项目中心保存 652 文件，DifferenceCount=0、ErrorCount=0；唯一扫描警告为按授权未封存。base0.9.0，contentHash=`8467209c7cd7d3d9542293050b4c774e`。已退出 Play，未编译/更新，场景未标脏。

创建完成后的报告序列化曾因 Unity Color.linear 循环引用失败；已检查实际产物后继续，没有重复创建。MCP 中途断连，第 5 次重连恢复，用户确认已连接。历史 inspection.json 是修改前快照，最终状态以本页和 final-state.json 为准。

本轮没有新增图片需求。未发布、Bump、Git 提交、派生模板同步或消费部署；未做全量测试、真机或构建。包内测试夹具保留旧 Image 绑定分支，但本轮只在当前 base 执行，其他模板未切换复测。

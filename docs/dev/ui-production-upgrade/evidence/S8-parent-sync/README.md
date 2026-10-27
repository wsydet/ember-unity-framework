# S8 父模板同步记录

2026-10-27。用户要求逐个更新所有依赖 base 的模板，保留业务功能，不发布。全部通过项目中心加载、保存、父同步与本地 Bump 执行，没有直接修改 Templates~ 或 ParentSnapshot~。

全模板备份位于 `.utmp/ui-parent-sync-20261027/before`，6318 文件，SHA 清单见 backup.json。目标父基线为 base 0.10.0 / `61cbe49aa797b1604f293b9756674566`。

| 模板 | 当前进展 | 验证 |
|---|---|---|
| ui-game | 0.2.0，父基线已更新；保留子模板动态字体缓存冲突 | 显式编译完成，UiGameSessionTests 4/4，job 1e37bd0868184e0791d7dbcc9cc00509 |
| platformer-2d | 0.12.1，父基线已更新；保留场景、主菜单/玩法页定制、存档与继续逻辑 | 加载进度相关 PlayMode 13/13，job 2c796854efa74e60b9f0ab03ece8e002；71 UI Binding 无错误 |
| source3d-2p5d | 0.6.0，父基线已更新；保留场景、玩法页与 SceneUI 注册 | 加载进度 PlayMode 13/13，job dd4dc459518c4dbc99daf9dcbfabd0d5；72 UI Binding 无错误 |
| visual-novel | 0.22.1，父基线已更新；完成公共组件、小说本地化与皮肤键兼容 | 加载进度 PlayMode 13/13，job 714f63aced5f4842be4a41c8ab66729d；小说相关 EditMode 7/7，job 7f453e17024743698c0393daf0b62ced；80 UI Binding 无错误 |

platformer 主菜单与玩法 Prefab 通过 AssetDatabase.MoveAsset 对齐 Page 名称，GUID 不变。新版候选主菜单补齐继承的 Btn_Continue/Txt_Status 两项绑定，通过官方生成器重新生成；补齐后显式编译 started1793072538851 / finished1793072542476 / reload1793072547867，Console 无错误/警告。补齐前检查失败如实记录，不代表最终遗留。

同步 API 在有内容改动时要求同时本地 Bump。ui-game 首次未提供 Bump 参数的请求被写前拒绝；修正后成功。platformer 先同步为 0.12.0，再修复候选绑定、保存并封存为 0.12.1。

现有测试均为直接相关的部分测试，不是模板全路线、实体操作、构建或消费部署验收。没有 Git 提交、推送、tag 或远端发布。

## 合并与最终状态

- source3d 生成遇到 Win32 1224 文件映射占用。确认部分写入后完成一次显式编译，仅续接剩余 Loading/GM 生成；未丢弃已生成资产。最终编译 started1793073159615 / finished1793073162246 / reload1793073169141。一次 MCP WebSocket warning 后工具恢复，不是产品编译错误。
- visual-novel 保留场景、主菜单/设置布局、六个小说页面注册、存档格式与业务模块。Loading 的存档请求、祖先 CanvasGroup 射线恢复、取消与成功门槛保留；数字转由进度组件管理。设置页面把上下文本地化交给组件 partial，偏好控件及暂停所有权保留。GM 嵌入专用 Variant；候选主菜单补齐小说新增继承字段。
- visual-novel 同步后曾因保留用户注册区而缺少三个公共页面注册；通过官方生成入口补齐。更新旧资源路径的编辑工具和测试，皮肤编辑器使用显式 Page→原 Panel 稳定键适配，原皮肤数据与运行时键不变。确认皮肤编辑器仍找到九个目标，包含主菜单。
- 小说字体目录 GUID、原三套皮肤和 DefaultSkinId=2 保留；用字体中心 SaveAndGenerate 追加父模板 DefaultLightSans=4。最终编译 started1793074751725 / finished1793074757023 / reload1793074765540，Console 空。
- 最终存储检查见 final-stored-audit.json：四份模板全部封存、父 hash 一致、重复 GUID=0。自定义模块未变文件数分别为 ui-game12、platformer738、source3d18、visual-novel370；差异仅 platformer 的加载测试及小说两个 UI 测试适配。运行期间产生的两份共享字体动态缓存已另行留档并恢复受保护的 S8 基线。
- 已恢复活动模板 base0.10.0，退出 Play。最后显式编译 started1793075393796 / finished1793075396787 / reload1793075404492，Console 空。final-base-validation.json：DifferenceCount=0、ErrorCount=0；8 条 Warning 均为测试状态 Destination/InPlaceHost/Origin/Host/Other/OldState/NextState/OverlayState 未关联场景，不是本轮 UI 错误。
- final-parent-status.json 中全部四份派生均为 Synced。本地同步及相关验证完成；尚未发布、构建、真机或消费部署。

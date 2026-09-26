# Ember Framework 0.14.13 发布说明

2026-09-26。本次交付 visual-novel **0.13.0**（模板内容封存 hash `0aa62a24f632c2f426a691f97002eb56`）；
框架新增退出封屏与全局语言变更广播。base / source3d-2p5d 模板与第三方依赖不变。

> 两个能力：
> ① **退出封屏**——退出应用前先把画面变成不透明黑幕，不再露出「UI 已销毁、3D 场景还在渲染」的穿帮帧；
> ② **多语言即时切换**——切换语言时正文、选项、提示、历史回看与界面文案当场重刷，不需要重开页面或重进剧情。

## 框架更新

### 退出封屏（`EmberQuitCurtain` 与 `EUIQuitCurtainCover`）

- 问题：`GameLauncher.Quit` 先逆序销毁 Module 与 Manager（UI 页面随之销毁），再调用
  `ApplicationQuitUtil.Quit()`；而 `Object.Destroy` 在「当前 Update 之后、渲染之前」生效，
  PC 等平台在 `Application.Quit()` 之后仍会出帧，于是「UI 已经没了、只剩 3D 场景」的那一帧会被真的显示出来。
- 两道封屏：引擎侧把 3D 主相机清成纯黑（`EmberQuitCurtain.BlackoutCamera`，不依赖任何 UI 对象）；
  UI 侧通过 `EmberQuitCurtain.CoverRequested` 请求盖一张不透明全屏遮罩，由 `EUIQuitCurtainCover`
  在 `EUIViewEngine` 初始化时预创建并订阅；遮罩**不进 EUI 页面栈**，所以框架销毁页面时不会把它一起销毁
  （`Uninstall` 只退订、不销毁遮罩）。
- 调用时机由框架负责：`GameLauncher.Quit` 与「系统/窗口关闭」这类不经过 `Quit` 的退出兜底都会先 `Show()`；
  `GameLauncher` 新增 `_isQuitting` 防止连点重复封屏与重复清理，并在进入 Play Mode 时 `EmberQuitCurtain.Reset()`。
- 新增诊断 `ApplicationQuitUtil.DescribeBranch()`（返回 `EditorStopPlayMode` / `AndroidKillProcess` /
  `ApplicationQuit`），排查「真机退出露场景」时先确认走的是哪条退出路径；新增日志标签 `CoreQuitCurtain`。

### 全局语言变更广播

- `EmberBroadcastEvent` 新增 `Localization = 7000` 基址与 `LanguageChanged = 7001`（载荷为新语言标识）。
- `TextLocalization.PublishLanguageChanged(language)` 是**唯一发布点**：先 `RefreshAll()` 重刷所有活动
  `TMPEx`，再播报事件。语言偏好由接入方自己持久化，本方法只负责让已经显示出来的东西跟上新语言。
- `TMPEx.SetSource(text)` 新增：写文本并**同时清掉 Key**，给「运行期接管一个挂了 Key 的控件」用。
  只写 `text` 不清 Key 时，下一次切语言的 `RefreshAll` 会把这段文本顶回表里的静态值。

### 其他

- `EmberAudioManager` 新增只读 `BgmMixerGroup` / `SfxMixerGroup`：业务自建循环音源（视觉小说主题曲、
  环境音）赋给 `AudioSource.outputAudioMixerGroup` 即可与 BGM 走同一条混音路径；注释里写明配置 Mixer 时
  玩家音量由分组承担，调用方不要再叠乘一次，否则双重衰减。

## 视觉小说模板 0.13.0

### 开局演出与名字输入（新增自定义步骤与页面）

- 新增**开场段落**步骤（`NovelOpeningSegmentSO`，成对 Begin/End）：Begin 加一路推进锁并切自动播放、
  固定本段自动间隔（不受玩家间隔偏好影响，玩家设成 60 秒也不会拖慢开场），End 恢复手动并解锁；
  Begin 被页面/场景暂停挡下时逐帧重试，超时或被打断（读档/退出/故障）时兜底解锁并还原间隔。
- 新增**输入玩家名字**步骤（`NovelPlayerNameInputStep`）与配套的 `EUINovelNameInputPage`：
  结果是**强制步骤，不提供取消按钮**，玩家只能确认；页面被其它路径关闭时（读档拆会话、退出主菜单、故障）
  在 `OnClose` 兜底交回默认名，否则剧情会永久停在等待步骤。留空或纯空白写默认名（示例配置为「旅人」）。
- 新增 `ember-vn-custom-node` 模板技能（自定义节点扩展契约、确认流程与实现规则）。

### 多语言即时切换

- 业务侧 `NovelLocalization.SetLanguage` 改为调用框架的 `PublishLanguageChanged`，并保留 `Changed`
  静态事件作为旧订阅点；**不使用 `NovelSaveModule.Changed`**（那是存档 IO/通道，每个自动保存稳定点都会触发，
  语义也不对——语言不进存档、不影响剧情指纹）。
- 内容侧重刷：当前句由推理器的「语言缓存键」自然重建，且 `NovelSession.Render` 按「同一条 Say 只换文本」
  保留显示进度（切语言不再重播打字机）；选项走 `NovelRoute.TextKey` + `NovelLocalization.RouteText`，
  语言变更时阅读页清缓存代次整批重建（选项文字原本在读定义时就被烤成当时的语言）；
  历史按 `NovelHistoryEntry.TextKey` 在回看时重解析，带文字变量绑定的句子固定用存档文本
  （历史里没有可复用的变量值，重解析会露出未替换的占位符）；阅读页 `RefreshLocalization()`
  让设置弹窗盖在阅读页上时也能当场生效。
- 界面文案：设置页「当前场景 / 字每秒 / 秒 / 正常·减弱·关闭」、阅读页控制条（自动开·关、速度、停止快进）、
  阅读页状态行（准备中 / 已暂停 / 结局）、历史「旁白」与空提示、槽位名与「尚未保存」全部改走配表。
- 示例小说 LastLight：83 条台词 + 选项/提示/章节名/剧情名 + 角色名与全部界面文案补齐
  `zh_Hans`/`zh_Hant`/`ja`/`en`；`ui.reader.Choice.Label` 三列刻意留空，作为单元测试的回退链探针。
- **指纹安全**：`NovelCompatibility.Fingerprint` 不写 `Text`/`TextKey`，补 Key 与译文不作废既有存档；
  译文里的 `{playerName}` 现在也会被替换（`NarrativeRunner` 把译文同样过一遍文字绑定替换）。

### 安全区归属与 EUI 规范

- 7 个页面的内容从 `EUISafeArea` 直接子节点迁入全尺寸锚点 `Center`：`EUISafeArea` 自身撑满父级 +
  padding，而 `Center` 是它的撑满子节点，两者 rect 恒等，因此**布局逐像素不变**（迁移脚本对每个被移动节点
  比对移动前后的世界矩形，7 个页面全部一致）；迁移后 9 个页面的 `EUISafeArea` 直接子节点只剩 9 个锚点。
  7 个页面的 Binding 均经 UI 中心重新生成，`NovelUiTextKeyBinder`、`NovelLanguageSettingsMigration`
  与相关测试里的硬编码层级路径同步更新。
- 名字输入页的绑定子组件改名到规范前缀（`m_Txt_Title` / `m_Inp_Name` / `m_Btn_Confirm`）并补上
  root `EUIBinding` 的 UI 中文简述；补 `ui.name.Hint`、`ui.name.Confirm`；移除关闭按钮与其绑定条目。
- 输入名字期间禁止存档（状态不是稳定点，`EUINovelSavePage` 按钮禁用、`NarrativeRunner.TryCapture` 也会拒），
  并在存档页给出明确提醒 `ui.save.InputPending`，不再只把按钮变灰。

## 验证

- **Unity 编译**：MCP 触发编译，0 error（本批修掉一处自己引入的 `TMPEx` 缺 `using Ember.Basic`）。
- **配表烘焙**：`Succeeded=True`、0 诊断、24 项产物；产物里可读到新增 Key 与四个语言列。
  注意：改完 CSV 必须先 `AssetDatabase.ImportAsset(..., ForceSynchronousImport)` 再烘焙，
  否则 Unity 未重新导入源 CSV，烘焙会静默沿用旧内容（本轮踩到并已修正）。
- **跨表核对**：内容表 95 行 / UI 表 73 行，每行 5 列；资产、Prefab、C# 引用的 Key 与表 0 缺失、
  0 孤儿（`ui.save.Feedback`、`ui.save.Slot.Summary`、`ui.setting.CurrentScene.Label` 三行为历史遗留未使用行）。
- **EditMode 回归**：`Game.Narrative.Tests.NovelSessionTests` **179/179 通过**（含多语言、说话人称呼、
  指纹基线；新增用例 `LanguageSwitchMidSessionRefreshesVisibleTextHistoryAndBroadcast` 覆盖切语言契约，
  并在首次运行时抓到 `NovelSession.History` 投影漏拷 `TextKey` 的真实缺陷）。`Game.Narrative.Tests`
  全量 EditMode 批次 **327/328**，唯一失败项 `NovelGameplayTests.RealMenuReaderBranchEndingAndRepeatedExit`
  （真 UI 全流程，报「剧情连续 15 秒未推进」）在改动前同一批日志里同样失败过，属该用例自身时序抖动。
- **安全区迁移**：逐节点世界矩形比对 `rectIdentical=True`；迁移后审计探针 9 个页面 `outsideAnchor=0`。
- **模板封存**：`SaveTemplate` + 显式 **minor** Bump，visual-novel **0.12.0 → 0.13.0**，
  内容与封存 hash 一致 = `0aa62a24f632c2f426a691f97002eb56`（1030 文件）。保存前的项目差异清单为
  16 个新增 + 58 个内容变化，全部属本批与上一批未封存的开局演出/名字输入/技能内容，未夹带其他改动。
  父模板 base 0.6.4 本轮未改，`ParentSnapshot~` 未动。
- **未运行**：`Packages/com.ember/Tests/EditMode`（退出封屏用例 `EmberQuitCurtainEditTests`）本轮**未跑**；
  新增的 3 个名字输入存读档 PlayMode 用例按用户要求**只写不跑**；未跑 Play Mode，未在消费项目验收。
  编辑器面板的实际排版观感需人工确认。
- **已知卫生项**：`Assets/GameResource/Resources/UI/Common/Fonts/NotoSerifSC/NovelSerif SDF.asset`
  带入了编辑器预览自动补字形造成的差异（约 1.9 万行），本次按现状封存；需要干净批次时先
  `git checkout` 还原该资产再重新 `SaveTemplate` + Bump。

## 兼容与迁移

- 框架 **0.14.12 → 0.14.13**：向后兼容，无 API 移除。消费端通过 `Ember/UPM Manager` 升级，
  **不要**直接改 `manifest.json` / `packages-lock.json`。
- 模板 **0.12.0 → 0.13.0** 是 **minor**（前两位变化）：补丁增量不适用，先备份本地剧情/资源/业务代码，
  再完整重新部署或按下面的人工迁移清单合并。
- **顺序约束**：本批模板代码调用了 0.14.13 新增的 `TextLocalization.PublishLanguageChanged` 与
  `TMPEx.SetSource`，必须先升级框架再吸收模板内容，否则模板侧无法编译。
  模板的 `frameworkVersion` 由父模板 `base` 继承（仍为 `0.14.1`）——派生模板不能单独声明框架版本
  （`DeclareFrameworkVersion` 会拒绝），兼容闸门按 major.minor 判定所以不会报错；要如实声明
  「本模板最低需要 0.14.13」需要先在 **base** 上声明并同步派生模板，本轮未做。
- 人工迁移清单（消费项目已在用模板 0.12.0 时）：① 框架升级到 0.14.13；② 用项目中心预览
  visual-novel 0.12.0 → 0.13.0 的差异并逐项合并（重点：7 个页面 Prefab 的层级、名字输入页与新步骤资产、
  `novel_ui_text` / `novel_content_text` 两张表的四语言内容、示例剧情资产新增的 Key）；③ 合并后重新烘焙配表；
  ④ 在消费项目重跑验收：主界面与游戏内切四种语言是否当场生效、输入名字期间存档是否被拒并给出提醒、
  输入前/中/后的存读档是否符合预期、退出应用是否不再露出 3D 场景帧。

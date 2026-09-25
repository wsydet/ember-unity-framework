# Ember Framework 0.14.11 发布说明

2026-09-25。本次交付 visual-novel 0.9.2；框架 API、base/source3d-2p5d 和第三方依赖不变。

> 本次是 0.14.10 之后累积的一次模板交付：既包含上一步落地的配表多语言与 UI 皮肤，也包含本次的界面文案挂 Key
> 与设置界面语言选项。

## 更新内容

### 配表多语言

- 新增 `novel_languages`（语言清单）、`novel_content_text`（内容文案）、`novel_ui_text`（界面文案）三张表，
  源 CSV 在 `Assets/GameResource/TableSources/`，含表定义与烘焙产物。
- 剧情资产写稳定 Key：对白/旁白/章节卡 `text.<storyId>.<lineId>`、选项与分流 `option.<storyId>.<optionId>`、
  章节出口 `route.<storyId>.<routeId>`、选择提示 `prompt.<storyId>.<nodeId>`、章节名/剧情名 `chapter.*`/`story.*`、
  角色名 `character.<characterId>`（未命中回退 `novel_characters.displayName`）。
- 回退链固定为「目标语言列 → 源语言列（`isSource`，默认 `zh_Hans`）→ 资产原文」；Key 查不到不报错。
- **Key 与译文不进剧情指纹**，补译文、加语言都不会让既有存档失效；`lineId` 等 ID 仍进指纹。
- 正文本地化放在 runner 的运行期副本（在 `NarrativeValidator` 之后），译文长短不会让 `TextBeats` 报错；
  选项/路线/提示/章节名在读定义时替换。

### UI 皮肤（本期只覆盖图片）

- 新增 `novel_skins`、`novel_skin_sprites`、`novel_story_skin` 三张表，按 `(page, control, node)` 相对锚点寻址，
  同一个同名节点（如多个 `SkinIcon`）可按所属控件分开覆盖；`spritePath` 必须是项目 Resources 下的图片。
- 只换图片、不动位置与大小；各小说自己的布局调整仍走布局窗口。
- 作者工具：菜单 `Ember/视觉小说/导出皮肤可覆盖图片清单`（扫描全部页面 Prefab，输出候选清单并复制待填 CSV 行），
  以及布局窗口外观区的「把此元素加入皮肤清单」。

### 界面文案与语言选择

- 9 个正式 Prefab 的 44 个静态文案控件换成 `TMPEx` 并挂上 `novel_ui_text` 的 Key（主菜单、设置面板、阅读页、
  阅读菜单、历史、字号弹窗、存档页、选项与槽位 Item）。运行期按当前语言显示，未接入或查不到条目时回退原文，
  行为与普通 TMP 一致；编辑期不改写文本，保住正式 Prefab 的所见即所得。
- 新增 `TMPEx` 组件与 EUI 的 `CONTEXT/TextMeshProUGUI/替换为 TMPEx（多语言文本）` 入口；替换保留字体、材质、
  对齐等全部设置，且不带 `[EUIExtension]`，EUI 仍按 `Text` 控件识别，既有代码生成与 `ControlMap` 不受影响。
- 设置界面新增语言行（简体中文 / 繁體中文 / 日本語 / English）：选择写入 `PlayerPrefs`（不进存档、不影响指纹），
  按钮文案用各语言自己的名字、不挂 Key。默认中文——语言偏好为空时解析器回退源语言列，界面高亮落在简体中文。
- `novel_ui_text` 增加 12 行设置面板文案（只填源语言，其余列留空待译），并把 `ui.menu.Saves.Label` 的源语言
  对齐成预制体实际文本。
- 新增编辑期多语言解析器装配，`TMPEx` 的 Inspector 多语言区可逐语言预览（此前只有打开流程图/试播/布局三个窗口
  才会装配，普通编辑时会提示"没有注入多语言解析器"）。

### 技能

- `ember-vn-import-story`：清单要记 Key，导入时写 Key 并在 `novel_content_text` 追加行（只填源语言、不覆盖已交付译文），
  完成条件加入多语言核对；新增 `references/localization.md` 作为 Key 与配表的唯一口径。
- `ember-vn-export-story`：列头增加多语言 Key，台词从当前语言导出。
- `references/import-mapping.md` 增加多语言小节。技能最低业务模板版本提升到 0.9.2。

### 测试与工程卫生

- Play Mode 用例统一走场景脚手架：进入 Play Mode 前丢弃未保存的临时场景、只在确实处于（或正在进入）Play Mode 时
  退出、进 Play 后确认过渡真的落地（此前前面有用例做过资产增删时，过渡会被资源管线打断，导致整批运行期用例连锁失败）。
- 测试运行前后自动还原被 Unity 改写的非业务文件（`FrameworkScene.unity`、TMP 动态字体图集、调试配置、
  贴图导入器补的平台设置），跑完测试不再污染工作区。
- 检查点版本断言改用 `NovelCheckpoint.CurrentSchemaVersion` 唯一真源（原先两处硬编码 `6`，实际 schema 已是 7）；
  外观用例的断言改落在可见图片上，并去掉依赖编辑器 Undo 栈状态的"重做"断言。

## 验证

Unity MCP 编译通过。

- UI Key 挂载做了双向校验：9 个 Prefab 与上一版逐行对比，除 `m_Script`、`m_EditorClassIdentifier`、`_key` 三行外
  无任何改动；再逐个重导入并断言 44/44 组件为 `TMPEx`、Key 正确、字体与材质引用完好。
- 语言选项用真实页面实例验证：四个控件进入 `ControlMap`；未选择语言时高亮简体中文；点击英语后
  `NovelLanguageSettings` 变为 `en` 且高亮跟随；按钮文案保持各语言自己的名字不被翻译。
- 编辑期解析器装配后逐语言解析正常（`ui.main.Start` 英文为 `New Game`；未翻译项按文档回退源语言）。
- 剧情指纹安全性：两个示例夹具在装配多语言并切三种语言后，指纹与改动前基线逐字节一致。
- 未做：语言行的画面观感验收、消费项目迁移与跨平台验证。

visual-novel 版本 0.9.2，父模板 base 0.6.4；内容及封存 hash 为 `3a7ae624944ecd5baf2fa058b6782055`。
最低框架兼容声明保持 0.14.1。本机无关的 BOM、动态共享字体与调试配置改动未纳入发布。

## 消费项目更新

1. 通过 Ember/UPM Manager 升级框架至 v0.14.11。
2. 在项目中心预览并应用 visual-novel 的补丁增量；跨 minor 的旧版本（0.8.x 及更早）先备份本地剧情、资源与代码，
   再按项目中心流程部署并合回定制内容。
3. 设置面板会由一次可重入的模板升级自动补出语言行（缺 `LanguageZhHans` 绑定才动手），并重新生成
   `EUISettingPage.Binding.cs`；若本地已改过该面板，先备份 Prefab 与 `.Binding.cs` 再合并。
4. 升级后同步模板技能；只升级框架不会自动更新已部署的 Assets。
5. 验证：切语言时界面文案与设置页高亮同步变化；既有存档不受影响（Key 与译文不进指纹）。

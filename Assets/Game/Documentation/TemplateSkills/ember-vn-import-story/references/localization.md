# 多语言 Key 与配表约定

本文件是视觉小说模板多语言的**唯一口径**。导入、回写、演出优化都按这里的约定写数据。

## 1. 数据放在哪

| 表 | 用途 | 何时改 |
|---|---|---|
| `novel_languages` | 语言清单：`id,displayName,order,isSource` | 新增语言 |
| `novel_content_text` | **内容**文案：`key,zh_Hans,zh_Hant,ja,en` | 随剧情内容增长 |
| `novel_ui_text` | **UI** 文案：同上 | 只有增删改 UI 时才动 |
| `novel_skins` / `novel_skin_sprites` / `novel_story_skin` | 皮肤与其按小说赋值 | 见第 6 节 |

源文件在 `Assets/GameResource/TableSources/*.etable.csv`，表定义在 `Assets/Game/Table/Definitions/`。
**只放 CSV 不会被生成**：每张表都必须有对应的 `EmberTableDefinition` 资产才参与烘焙与生成。

## 2. Key 规范

Key 必须**稳定**：重新导入换个顺序、改一个错字都不能换 Key，否则译文会跟错行。

| 对象 | 字段 | Key |
|---|---|---|
| 对白 / 旁白 / 章节卡（Say） | `_textKey` | `text.<storyId>.<lineId>` |
| 选项、分流 | `_textKey`（NarrativeRoute） | `option.<storyId>.<optionId>` |
| 章节出口路线 | `_textKey`（NarrativeChapterRoute） | `route.<storyId>.<routeId>` |
| 选择提示 | `_promptTextKey` | `prompt.<storyId>.<nodeId>` |
| 章节名 | `_displayNameKey` | `chapter.<storyId>.<chapterId>` |
| 剧情名 | `_displayNameKey` | `story.<storyId>` |
| 角色名 | 无字段，按约定查表 | `character.<characterId>` |
| 说话人临时称呼（揭晓前） | `_speakerNameKey`（Say） | `speaker.<语义>`，如 `speaker.unknown` |

`lineId` / `optionId` / `routeId` / `nodeId` / `chapterId` 都是已有的稳定 ID，**导入时保留既有值**，
所以 Key 天然稳定。不要用行号、序号或台词文本本身拼 Key。

**Key 不进存档指纹**，因此改译文、加语言都不会让既有存档失效；反过来，
`lineId` 等 ID 是进指纹的，改它们会作废存档。

## 3. 写入方式

1. 写资产时同时写 Key 字段：Say 用 `_textKey`，选项/路线用 `_textKey`，提示用 `_promptTextKey`，
   章节/剧情名用 `_displayNameKey`。**原文照旧写进 `_text` / `_displayName`**，它们同时是源语言文本与回退文本。
2. 在 `novel_content_text` 追加（或更新）一行：`key` 如上，`zh_Hans` 填原文，其余语言列留空待译。
   已存在的 Key **只更新 `zh_Hans`**，绝不覆盖其它语言列。
3. 角色名在 `novel_content_text` 里按 `character.<characterId>` 追加，`zh_Hans` 填显示名；
   `novel_characters.displayName` 保持不变，它是源语言回退值。
4. 说话人临时称呼按 `speaker.<语义>` 追加，`zh_Hans` 填原文（例如 `？？？`），其余语言列可一起填。
   模板已提供 `speaker.unknown → ？？？`，同一个语义复用同一个 Key；只有语义不同（例如`？？？` 与
   `神秘的声音`）才新开一个 Key。**不要**用 `speaker.<characterId>` 这种带角色键的写法。
5. 改完 CSV 后烘焙。**Windows 上必须先卸载已加载资源再烘焙**，否则会
   `Table artifact batch was rolled back: 无法删除要被替换的文件`：

   ```
   EditorUtility.UnloadUnusedAssetsImmediate();
   EmberTablePipeline.BakeAndGenerateAll();
   AssetDatabase.Refresh();
   ```

   烘焙改写了 `.bytes`，不 `Refresh` 的话 `Resources.Load` 仍返回旧缓存。
6. 不改生成代码、不手改 `.bytes`、不手改 `template.json` 的 hash。

## 4. 回退链

解析顺序固定为：**目标语言列 → 源语言列（`isSource` 的那行，默认 `zh_Hans`）→ 资产里的原文**。

- 某一语言列留空 = 该语言未翻译，显示源语言文本，不会显示空白。
- Key 查不到 = 回退原文，不报错。
- 角色名查不到 `character.<id>` = 回退 `novel_characters.displayName`。
- **说话人显示名**多一层：`_speakerNameKey` 命中 → `character.<角色键>` → `novel_characters.displayName`。
  称呼 Key 留空或查不到都按后面的链继续，不显示空白、不报错。
- 没有装配多语言配表时，全部回退原文，行为与未接入前完全一致。

## 4.1 揭晓前显示占位名

「主角先遇到一个人、后面才知道名字」的台词，**仍然填真实角色键**，另加一个称呼 Key：

| 字段 | 填什么 |
|---|---|
| 角色键 | 真实角色键（例如 `lastlight_wan`）。**不要为了显示 ？？？而留空或换成别的键** |
| 说话人称呼 Key | 揭晓前那句填 `speaker.unknown`，揭晓后那句留空 |
| 台词 | 照常写，照常配 `_textKey` |

说话人称呼是**表现层字段**：它只改这一句显示的名字，不参与存档语义指纹，也不影响
「角色强调 → Auto」按角色键找说话人，所以把称呼 Key 加上去不会作废玩家已有的存档。
历史回看按当时的称呼 Key 重新解析，旧句不会因为后面揭晓了真名而变成真名。

## 5. 两条不能踩的线

- **正文节奏点按原文校验**：`TextBeats.At` 是原文的 Unicode 标量索引，
  译文长短不同不会报错、也不会崩（越界的节奏点自然不触发），但**不要**为了让节奏点对齐去改译文长度。
- **选项、提示、章节名会在读定义时替换**，它们没有基于长度的校验；正文只在运行时替换，
  所以校验始终面对原文。

## 6. 皮肤（本期只覆盖图片）

- `novel_skins`：`skinId,displayName`。
- `novel_skin_sprites`：`id,skinId,page,control,node,spritePath`。
  `id` 约定 `skinId.page.control.node`；`page` 取 Prefab 文件名（如 `EUINovelReaderPage`、`EUIMainPanel`）；
  `control` 取 EUI 绑定控件名（留空表示页面根）；`node` 是锚点下的相对节点路径（留空表示锚点自身）。
- `novel_story_skin`：`storyId,skinId`。没有记录的剧情不套皮肤，沿用 Prefab 外观。
- **`spritePath` 必须是项目 Resources 下的图片**（如 `UI/Common/Atlas/Novel/return`）。
  Unity 内置的 `UISprite`（`Resources/unity_builtin_extra`）**无法用路径表示**，只能当被替换方。
- 取候选行有两种方式：布局窗口选中元素后点「把此元素加入皮肤清单（复制 CSV 行）」单个取，
  或用运行菜单 **`Ember/视觉小说/导出皮肤可覆盖图片清单`** 批量取：
  它扫描全部页面 Prefab，给出每一张可覆盖图片的 `(page, control, node, 当前路径)`，
  并把待填的 CSV 行复制到剪贴板（把 `SKINID` 换成皮肤标识即可）。
- 皮肤只换图片，**不改位置与大小**；各小说自己的布局调整仍走布局窗口。

## 7. 新增一种语言

1. `novel_languages` 加一行（`id` 用 BCP-47 风格，如 `ko`；`isSource` 只有一行是 true）。
2. `novel_ui_text`、`novel_content_text` 的 CSV 与对应 Row 类各加一列。
3. `Game.Narrative.NovelLocalizer.Pick` 加一个分支把语言标识映射到新列。
4. 烘焙并刷新。

第 2、3 步是宽表方案的固有代价：加语言要动 Row（代码）而不只是加数据。

## 8. 界面文案（TMPEx）

界面的静态文案用 `Ember.UIExtension.TMPEx` 承接：在 TMP 的 Inspector 右上角三点菜单里选
**「替换为 TMPEx（多语言文本）」**，然后在多语言区填 Key 即可。原 TMP 的字体、材质、对齐等设置全部保留。

- **为什么 EUI 不会把它当成陌生组件**：`TMPEx` 派生自 `TextMeshProUGUI` 且**故意不带** `[EUIExtension]`。
  EUI 判定控件类型时先按精确类型和已注册扩展匹配（都不命中），最后走 `GetComponent(TextMeshProUGUI)`
  的子类判定，因此它仍被识别为 `Text` 控件——既有代码生成与 `ControlMap` 取值都不受影响。
- **只给静态文案挂 Key**。`Speaker`、`Body`、`Status` 这类由剧情在运行时写入的文本不要挂 Key，
  否则会被多语言覆盖。
- **编辑期不改写文本**：组件带 `ExecuteAlways`，为保住正式 Prefab 的所见即所得，只有运行期才写入译文；
  编辑期预览看 Inspector 的多语言区或小说流程面板。
- Key 留空、配表未装配或查不到条目时都显示原文，行为与普通 TMP 完全一致，所以可以逐个控件慢慢迁移。
# 视觉小说图片与配表映射

路径均相对实际消费项目根目录。以下为 visual-novel 0.6.0 的既有结构；执行时以当前项目的表 Definition、Row 和模板文档为准。这个 Skill 还依赖支持模板技能身份检查的框架实现，已发布的旧 0.13.2 不因此自动获得该能力。

## 图片

| 用途 | 文件目录 | 源表 |
| --- | --- | --- |
| 角色立绘、表情变体 | `Assets/GameResource/Resources/UI/Module/Narrative/Atlas/<剧情名>/Portraits/` | `novel_portraits`，必要时 `novel_characters` |
| 场景背景 | `Assets/GameResource/Resources/UI/Module/Narrative/Atlas/<剧情名>/Backgrounds/` | `novel_backgrounds` |
| 阅读 UI 专用图片 | `Assets/GameResource/Resources/UI/Module/Narrative/Atlas/` 下按现有用途组织 | 无通用小说图片表；核对实际 UI 引用 |
| 多 UI 共用图标 | `Assets/GameResource/Resources/UI/Common/Atlas/` 下按现有分组组织 | 不写小说背景/立绘表 |

不要将 `<剧情名>` 固定为 LastLight 或模板名。从用户正在制作的剧情或明确指定的分组选择；不清楚时询问。遮罩和特效需核对 `Assets/Game/Documentation/visual-novel/EffectConfiguration.md` 的现有资产配置，不能仅凭一张图推断完整效果。

CSV 中 `resourcePath` 去掉 `Assets/GameResource/Resources/` 和文件扩展名，使用 `/`。例如 `UI/Module/Narrative/Atlas/MyStory/Portraits/hero1_neutral`，不是绝对路径、GUID 或带 `.png` 的路径。同一个 Resources 相对路径即使扩展名不同也可能冲突，应一并检查。

## 源表与列

当前源文件在 `Assets/GameResource/TableSources/`，扩展名是 **`.etable.csv`**。表定义在 `Assets/Game/Table/Definitions/`；如果项目改过源路径，读取 Definition 的 Source 引用解析实际文件，不另建一份同名 CSV。

| 源文件 | 精确列名 | 规则 |
| --- | --- | --- |
| `novel_characters.etable.csv` | `id,displayName` | `id` 唯一；已有角色保持原 ID/显示名，除非用户明确修改 |
| `novel_portraits.etable.csv` | `id,characterId,expression,resourcePath` | `characterId` 引用角色表；多个表情各有独立立绘 ID |
| `novel_backgrounds.etable.csv` | `id,resourcePath` | 背景 ID 独立；不添加不存在的场景/时段列 |

例：用户确认“这是 MyStory 的主角1，普通表情，新建角色 hero1”后，可提议（最终仍由用户确认）角色 `hero1,主角1` 与立绘 `hero1_neutral,hero1,neutral,UI/Module/Narrative/Atlas/MyStory/Portraits/hero1_neutral`。不把显示名直接当文件路径，不因为表里有其他角色就覆盖它。时段等信息可体现在背景 ID 和文件名，不扩展表结构。

## 现有 Unity API

通过 Unity MCP 调用 `Ember.Table.Editor.EmberTablePipeline`：

- `FindAllDefinitions()`：取得现有表定义，按 TableId 找到本批涉及项。
- `ValidateAll()`：检查全部表与跨表引用；检查 `Succeeded` 和 `Diagnostics`，报告既有错误，不擅自修其他业务。
- `BakeCurrent(definition)`：现有 schema、路径和清单不变时只烘焙指定表。本任务通常仅新增数据，优先使用它。先将所有本批 CSV 写好并导入，再按角色、立绘/背景顺序执行。
- 如果明确报告 schema/路径/表清单变化，调用 `PreviewAll()` 查看差异；全量输出会影响哪些文件需先说明，再用 `BakeAndGenerateAll()`。不能因单表失败便盲目全量生成。
- 每次查看结果的 `Succeeded`、`Diagnostics`；调用成功返回不代表烘焙成功。多次 BakeCurrent 不是一个批次事务，需保留本批备份并在失败时恢复已产生的输出。

源表、生成代码、bytes 和产物 manifest 的具体位置以定义和生成预览为准。验收时用 `Resources.Load<UnityEngine.Sprite>(resourcePath)` 确认可加载，并卸载本次临时加载的资源引用；表数据的剧情使用可以在流程编辑器重新校验/下拉中检查，无需改剧情。

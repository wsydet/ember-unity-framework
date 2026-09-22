# 视觉小说模板目录规范

2026-09-21：按业务模块和资源实际使用方归档。`visual-novel` 是模板名称，不作为运行时脚本、图片、剧情及音频的统一收纳目录。

| 内容 | 目录（相对项目根） |
| --- | --- |
| 剧情业务、数据类型、运行器、输入、章节编辑器及测试 | `Assets/Game/Module/Narrative/` |
| 存档业务和存储实现 | `Assets/Game/Module/NovelSave/` |
| 阅读、历史、字号、阅读菜单及演出 Item 脚本 | `Assets/Game/UI/Runtime/Module/Narrative/` |
| 存档页、槽位 Item 及 UI 协调脚本 | `Assets/Game/UI/Runtime/Module/NovelSave/` |
| 阅读布局编辑器 | `Assets/Game/UI/Editor/Narrative/` |
| 阅读相关 Prefab | `Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/` |
| 存档 Prefab | `Assets/GameResource/Resources/UI/Module/NovelSave/Prefabs/` |
| 阅读背景及立绘 | `Assets/GameResource/Resources/UI/Module/Narrative/Atlas/<剧情名>/{Backgrounds,Portraits}/` |
| 主对话框与阅读菜单的整组 8 张图标 | `Assets/GameResource/Resources/UI/Common/Atlas/Novel/` |
| 各界面共用的字体及许可证 | `Assets/GameResource/Resources/UI/Common/Fonts/NotoSerifSC/` |
| 剧情、章节及节点 SO | `Assets/GameResource/Resources/Config/Narrative/<剧情名>/` |
| M1 跨章节测试剧情（不作为运行示例） | `Assets/Game/Module/Narrative/Tests/Fixtures/M1Sample/` |
| 小说输入配置 | `Assets/GameResource/Resources/Config/Narrative/NovelInput.inputactions` |
| 音乐、音效及配音 | `Assets/GameResource/Resources/Audio/Narrative/<剧情名>/` |

`Narrative` 和 `NovelSave` 的业务脚本本来已按模块归档，不改模块名称、命名空间或程序集边界。配表声明、Row 和生成代码继续使用项目统一的 `Game/Table` 流程，CSV 源文件仍在 `GameResource/TableSources`。

图片随使用它的 UI 放在 `Atlas`；多个 UI 共用的图片放 `Common/Atlas`。主对话框与阅读菜单的图标按同一套通用控件素材管理，history、hide、auto、speed、save、quick_save、settings、return 全部放 `Common/Atlas/Novel`，不再按当前某个按钮的唯一使用方拆分。

`Resources/Config/Narrative/M1Sample` 已移出运行目录；其中三章节剧情保留为测试夹具，编辑器测试通过 AssetDatabase 加载，确保跨章节测试仍覆盖真实 SO。正式默认剧情只保留 LastLight。旧 M1Sample 剧情存档不再作为运行示例支持，不将其偷偷转向 LastLight；原音频目录中的 BGM/SFX 仍被 LastLight 使用，因此保留。已有配表引用的占位背景、立绘也不随剧情目录一起删除。

## 路径及迁移约束

- 移动使用 Unity `AssetDatabase.MoveAsset` 保留 `.meta` 和 GUID；不重建剧情稳定 ID，不修改控件层级及绑定字段。
- Prefab 的 `classPath` 分别为 `Module/Narrative`、`Module/NovelSave`，与脚本目录一致；页面注册使用实际新 Prefab 路径。以后从开发中心生成代码不应重新创建旧 VisualNovel 目录。
- 运行时默认剧情是 `Config/Narrative/LastLight/Story`。旧存档里的 `VisualNovel/...` 在 `NovelNewGameRequest` 中转换为 `Config/Narrative/...`，不主动重写已有存档；再次保存时使用新路径。
- 图片、音频的 Resources 相对路径由 CSV 配置，通过 `EmberTablePipeline.BakeAndGenerateAll` 导出，禁止手改生成 bytes。
- 剧情编辑器默认创建目录、样例资产生成器、阅读布局预览及测试路径同步迁移。阅读布局草稿仍服从原有冲突检测，不自动覆盖作者未保存的布局。
- `Implementation.md` 是历史实施记录，保留当时路径；当前结构以本文为准。
- 修改先落项目 `Assets`，再通过模板开发面板使用的 `EmberProjectSetup.SaveTemplate` 保存。普通保存不 Bump 版本，也不发布框架。

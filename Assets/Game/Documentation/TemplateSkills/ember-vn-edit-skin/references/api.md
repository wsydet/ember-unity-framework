# 皮肤接口

通过 Unity MCP `execute_code` 调用，下列示例中的目标必须替换为本次已读取的真实资产/Key。先读取源码核对当前版本签名。

```csharp
using Game.UI.Editor;
using Game.Narrative;
```

- `NovelUISkinEditorService.Skins()`：现有皮肤资产。
- `LegacySkins()`：旧配表皮肤 ID → 显示名称；无旧皮肤功能时为空。
- `CaptureBase()`：当前正式 UI 可编辑图片列表，不写资产。
- `Prefabs()`：支持页面的实际 Prefab 路径。
- `Create(id, displayName, source = null, legacyId = null)`：从来源复制配置和素材，返回 `NovelUISkin`。ID 为小写字母开头，后接字母、数字、下划线或短横线，最多 48 字符；不覆盖已有 ID。source 和 legacyId 不能同时指定。
- `ReplaceImages(skin, Replacement[])`：每项 `{ Key, File }`；Key 为 `Page|Control|Node`，允许空 Control/Node，File 使用绝对路径或项目相对路径。整批先预检，再导入；失败回滚本批。PNG/JPG/TGA/PSD 按单 Sprite 导入，原始文件保留。
- `SetAppearance(skin, key, Color, Image.Type, preserveAspect, pixelsPerUnitMultiplier, Vector4 border)`：保存显示设置；border 顺序左下右上。保留无需改变的已有参数。
- `Assign(story, skin)`：story 为实际 `NarrativeStorySO` 资产；null skin 恢复基础外观。仅改变该剧情关联，下次打开页面生效。
- `Validate(skin)`：返回待检查的问题列表。

资源目录：`Assets/GameResource/Resources/UI/Common/Atlas/NovelSkins/<id>/Skin.asset` 及 `Images/`；剧情关联为 `Assets/GameResource/Resources/Config/Narrative/UISkinAssignments.asset`。皮肤资产是新增编辑器皮肤的维护源，不手工将同一配置再写一份旧配表。

截图示例：

```csharp
using (var preview = new Game.UI.Editor.NovelUISkinPreview(prefabPath, 1920, 1080))
{
    preview.SavePng(beforePath, null); // 基础外观
    preview.SavePng(afterPath, skin);
}
```

如需与修改前皮肤比较，在修改之前先用该 skin 截图，不能把基础外观冒充本次修改前的效果。预览使用隔离场景实例，不保存页面 Prefab，也不启动游戏逻辑。

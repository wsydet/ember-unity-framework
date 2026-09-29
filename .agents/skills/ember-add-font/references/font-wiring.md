# 当前字体接线接口

以下路径相对目标工程实际解析的 com.ember 包根。以目标版本源码为准；缺失 API 时说明具体差异，不向消费项目复制一套同名框架类。

| 源码 | 用途 |
|---|---|
| UIExtension/Runtime/UIExt/EmberFontSkinCatalog.cs | Slots、Skins、FontEntry、Validate、TryGetFont |
| UIExtension/Runtime/UIExt/TMPEx.cs | 控件模式与每套皮肤的字体选择 |
| UIExtension/Runtime/UIExt/EmberFontSkins.cs | 项目库加载、全局换肤、Reload |
| UIExtension/Editor/UIExt/EmberFontSkinWindow.cs | 创建项目库、保存配置并生成枚举 |
| UIExtension/Editor/UIExt/EmberFontSkinMigration.cs | 需要转换旧文字时再读取，不作为普通加字体的默认步骤 |

## 配置与 API

运行时命名空间 Ember.UIExtension；编辑器工作台为 Ember.UIExtension.Editor.EmberFontSkinWindow。

- CreateProjectCatalog() 返回既有项目库，或从框架预设复制到 Assets/GameResource/Resources/Config/FontSkins/EmberFontSkins.asset。
- SaveAndGenerate(catalog) 验证配置、保存并生成 Assets/Game/Generated/FontSkins/EmberFontSkinIds.cs，枚举命名空间 Game.Fonts。
- Slots 是稳定 ID + Name + EnumName 的选项；Skins 每项包含稳定 ID、Name、EnumName 和 Fonts；FontEntry 用 SlotId 关联 TMP_FontAsset。
- Validate 检查槽位/皮肤 ID、非空字体、有效引用及默认皮肤，但允许各皮肤拥有不同的槽位子集，不检查场景和 Prefab 的控件映射。
- TMPEx.FontSkinId：-1 固定字体，0 跟随全局，正数固定某套皮肤。SetFontSkin(skinId, slotId) 同时设置模式和默认槽位；新增皮肤接线通常不需要改变它。
- SetFontForSkin(skinId, slotId) 追加或更新该控件的特定皮肤映射并应用预览；GetFontSlotForSkin(skinId) 在无显式项时返回默认 FontSlotId。默认槽位可能不属于新皮肤，必须检查解析结果。
- ApplyFontSkin() 无有效映射时返回 false 并保留当前字体，因此肉眼看到旧字形不代表新配置成功。换字体时会将 fontSharedMaterial 设为新字体默认材质；特殊材质需要单独检查。
- EmberFontSkins.Reload() 重载库并重置全局皮肤选择；SetSkin(id) 只改变当前运行/预览选择，不持久化 DefaultSkinId 或玩家存档。临时验证应记录并恢复原选择。

## 接线例子

控件原本使用“正文槽位 1”，新增皮肤 7 的 Fonts 含 SlotId=1、新字体 A，则为应跟随的控件设置 SetFontForSkin(7, 1)。标题原本使用槽位 2，若新皮肤仍保留标题槽位 2，则映射到 2；不要统一改成 1。新皮肤若只提供槽位 8，则须将目标控件映射到 8，不能依赖默认槽位 1 自动生效。

只在旧皮肤中用 A 替换槽位 1 的 Font 时，已有映射仍有效；回读验证即可，不必给每个控件造新槽位或更改模式。

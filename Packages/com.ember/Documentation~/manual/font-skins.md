# 字体皮肤

入口：`Ember/Tool/字体皮肤`。所有模板都可使用，字体随 `com.ember` 交付。

## 配置

首次打开选择“从框架预设创建项目字体皮肤库”。项目配置保存在
`Assets/GameResource/Resources/Config/FontSkins/EmberFontSkins.asset`，可随模板保存。
项目未创建配置时使用包内默认库，不需要复制字体文件。

预设为皮肤 1「思源宋体」、皮肤 2「钉钉进步体」、皮肤 3「阿里妈妈东方大楷」。
每套预设有正文、标题、特殊文字三个字体选项，初始分别使用该皮肤的同一款字体。
可在面板添加/删除皮肤和字体选项，为同一皮肤的不同选项指定不同的 TMP Font Asset。
“为此皮肤添加字体选项”只加入该皮肤，各皮肤允许不同数量的字体；顶部的“添加字体枚举”可一次加入所有皮肤。
缺字回退通过对应 TMP Font Asset 的 `Fallback Font Assets` 配置；自动回退用于缺失字形，
不是强制指定某个字符的字体。主动使用不同字体的文字应放在独立 TMPEx 中选择字体选项。

“保存配置并生成皮肤 / 字体枚举”生成 `Game.Fonts.FontSkinId` 和 `FontSlotId`，
位置为 `Assets/Game/Generated/FontSkins/EmberFontSkinIds.cs`。
ID 不随重命名或列表排序改变，删除后不复用；删除仍被使用的选项后需重新配置相关 TMPEx。
缺失映射时保留原字体，Inspector 显示缺失项，不擅自改用另一个选项。

## TMPEx

面板顺序为多语言 Key、TMP 原生文本编辑、语言预览、字体皮肤、TMP 原生其他设置。
不需要多语言时 Key 留空。文本编辑区只绘制一次，仍保留 RTL、文本样式和链接文本限制。

选择“跟随全局皮肤”后，“字体皮肤”区域分别列出所有皮肤。**每行单独选择该皮肤下此文本使用的字体**。
选择“固定字体”时，在下方 TMP 的 Font Asset 设置字体；已有皮肤映射保留，重新跟随时继续使用。
多选对象的切换方式或字体映射不一致时显示混合值；只有主动选择新值时才同时修改所选文本。
例如皮肤 1 选择“正文”、皮肤 2 选择“标题”、皮肤 3 选择“特殊文字”。
新增皮肤还未单独配置时，使用该文本的默认字体槽位（初始为正文）。

- **跟随全局皮肤**：切换时读取此文本对目标皮肤的选择。
- **固定字体（不跟随皮肤）**：使用 TMP 面板上直接指定的 Font Asset；保留但不应用各皮肤映射。
- 多语言 Key 可留空。换肤不改写 Key、原文、字号、颜色、对齐和布局。
- 更换字体时匹配新字体的材质；原字体的自定义描边等材质效果不会跨字体复制。

## 一键切换

编辑模式的“一键切换并设为项目默认”保存默认皮肤并刷新已启用的 TMPEx。
未打开的场景/Prefab 及隐藏文字会在下一次启用时使用新默认，无需逐个改写字体引用。
运行模式按钮仅切换当前会话，不修改项目默认；若游戏需要跨启动记忆，由游戏存档保存皮肤 ID。

```csharp
EmberFontSkins.SetSkin((int)Game.Fonts.FontSkinId.DingTalk);
text.SetFontForSkin((int)Game.Fonts.FontSkinId.Serif, (int)Game.Fonts.FontSlotId.Title);
text.SetFontSkin(0, (int)Game.Fonts.FontSlotId.Body); // 跟随全局
text.SetFontSkin(-1, (int)Game.Fonts.FontSlotId.Body); // 固定当前字体
```

运行时注册表只刷新已启用的文本；禁用或池化对象重新启用时自动跟上当前皮肤。
`EmberFontSkins.Reload()` 重新读取项目配置并恢复配置中的默认皮肤。

## 接入旧 UI

面板提供指定 `Assets` 目录下的 Prefab 转换：普通 TextMeshProUGUI 原位转换为 TMPEx，
保持组件身份和输入框等引用；原有 TMPEx 的固定/跟随选择及多语言 Key 保持不变。
新转换文本自动跟随全局，Key 留空。EUI 绑定由 UI 中心重新生成。
保存前备份到 `Library/EmberFontSkinBackups/<批次>/`，该目录不随模板发布。

旧版 `UnityEngine.UI.Text` 仅在没有其他组件引用它时自动转换，按显示属性逐项迁移；
存在引用则停止并报告位置，需要先迁移调用方。第三方 TMP 派生组件不自动替换。
此入口处理 Prefab，场景对象应在对应源 Prefab 中接入，或在其 TMPEx Inspector 中配置。

## 思源宋体迁移

思源宋体源文件、TMP 资源及许可证现位于 `Packages/com.ember/SharedAssets/Fonts/NotoSerifSC/`，
保留原资源 GUID。视觉小说模板不再复制同 GUID 的字体到项目 Assets。
旧消费项目升级时必须同时处理模板字体迁移：旧 Assets 中同 GUID 的字体不能与包内版本并存。
本次属于框架与模板配套变更；经 `Ember/UPM Manager` 升级后按模板迁移流程处理本地业务修改，
不要通过手改 PackageCache 或仅复制新字体来完成升级。

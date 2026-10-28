# 字体皮肤

入口：`Ember/Tool/字体皮肤`。所有模板都可使用，字体随 `com.ember` 交付。

## 配置

首次打开选择“从框架预设创建项目字体皮肤库”。项目配置保存在
`Assets/GameResource/Resources/Config/FontSkins/EmberFontSkins.asset`，可随模板保存。
包内默认库提供源配方。需要动态补字的项目应创建项目库，由工具生成一组项目自有字体资源；不按 UI 复制字库。

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

## 动态字体的可写归属（0.24.3）

TMP 动态补字会修改字形表和 Atlas；Font Asset 本体 dirty=false 时，Atlas 子资源仍可能已被标脏。
包内四份字体作为共享源保留 Dynamic、源 TTF、材质参数、fallback 和字重配方，但关闭源上的
Clear Dynamic Data On Build，避免 TMP 的全工程构建预处理清空包资产。动态显示应使用项目生成资源。
只关闭源构建清理不能替代引用迁移，也不能证明旧工程已经安全。

创建项目库、或在字体皮肤窗口执行“保存配置并生成皮肤 / 字体枚举”，现在会调用
`EmberWritableFonts.EnsureProjectFonts(catalog, outputFolder)`。默认输出：
`Assets/GameResource/Resources/UI/Common/Fonts/EmberGenerated/<源GUID>/<源文件名>`。
同一源 GUID 在项目中共享一份资源；TTF 和许可证继续引用包内源，不重复复制。
生成器复制字体及其 Atlas/材质子资源，递归重定向共享 fallback 和字重引用，保持皮肤/槽位 ID。
已有项目生成资源不会被覆盖。生成资源保持 Dynamic 和 Multi Atlas 设置，并启用构建清理；
Editor 动态缓存、构建清空及 Player 内存中的运行状态均由这些项目资产及 TMP 原有生命周期管理。
不额外手动销毁共享源材质/Atlas，不引入每帧刷新或第二套字体渲染器。

已有工程定向迁移，先备份项目字体库与待迁移 Prefab：

```csharp
// Editor-only；先迁移库，再迁移指定目录内的固定字体/普通 TMP 引用。
var catalog = AssetDatabase.LoadAssetAtPath<EmberFontSkinCatalog>(
    "Assets/GameResource/Resources/Config/FontSkins/EmberFontSkins.asset");
EmberWritableFonts.EnsureProjectFonts(catalog);
EmberWritableFonts.MakePrefabsWritable("Assets/GameResource/Resources/UI");
EmberFontSkins.Reload();
```

`MakePrefabsWritable` 只处理指定 Assets 目录，修改前备份 Prefab/meta 到
`Library/EmberFontSkinBackups/<批次>/`，路径见 `LastBackupPath`。保持组件/FileID、输入框/Binding
引用、文本、颜色、字号和材质预设参数；自定义材质也生成项目副本并绑定对应项目 Atlas。
未替换组件或 Binding，不需要重新制作 UI。场景内独立固定字体、其他自定义字体调用方及 TMP Settings
全局 fallback 不在该 Prefab API 的迁移范围，必须定向核对其引用是否仍指向包源。

TMPEx 在 TMP 的 Awake/OnEnable 准备之前选择映射字体，避免先用 Prefab 中遗留的包源准备字形。
当字体没有变化时保留已有自定义材质。固定字体仍遵循其明确引用，须完成上述迁移。
没有项目库、继续直接使用包内动态 Font Asset 的工程不在资产保护验收范围内；不要只升级代码就宣称字体迁移完成。

本轮只在框架夹具生成资源并定向验证，未更改、重部署或升级 Windward。消费项目应通过正式 UPM Manager
取得0.24.3或后续包含该能力的版本，再执行资源差异迁移，保留活动模板部署身份，无需全量重新部署。
详见[三项独立回归记录](../../../../docs/implementation/b03-player-exit-font-20261028.md)。

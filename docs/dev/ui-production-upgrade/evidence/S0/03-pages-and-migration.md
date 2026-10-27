# S0 页面与 S1 迁移输入

真实资产完整读取见 [prefab-inventory.json](prefab-inventory.json)，所有路径均在 `Assets/GameResource/Resources/UI/Common/Prefabs/`。本轮只盘点，没有改名。

| 旧文件/根名 | S1 目标名与现有逻辑类 | GUID | PageType | ClassPath |
| --- | --- | --- | --- | --- |
| EUIBackgroundPanel | EUIBackgroundPage | 70cb3c02d6d5b064eb30f18186932548 | Background | Framework |
| EUIMainPanel | EUIMainPage | 9b27fbe343055e94fb5f3ae24a87ee96 | MainPage | MainScene |
| EUIGamePlayPanel | EUIGamePlayPage | 27bddabca8ed57a489e17f0d937534bf | MainPage | GamePlayScene |
| EUISettingPanel | EUISettingPage | 13157152ce048b740bd1e740a35951af | FullScreenPopup | SettingScene |
| EUILoadingPanel | EUILoadingPage | 2ddab893e6d6e3248935b57e43ee4063 | TopMost | Framework |
| GMPanel | GMPage | bba4cad8012e3f64690d24af5becf55d | FreePage | Framework |

六页均为 Regular Prefab、Page 角色、Framework 代码模式、删除保护开启、Binding 基类 GUID 为空。条目均带 IsFramework=true。GM 注册固定 sortingOrder=30000，不能把其他页面序列化的同值字段误当实际页面排序。

EUISafeArea.prefab GUID=`89b5f16b84d545f45b919cfc3f1c61d3`，无 EUIBinding，保持原名。所有页面依赖真实嵌套 SafeArea；尚无公共 Page Variant 库。

## 接口与资源

- Main：Btn_Start、Btn_Settings，位于 `Animator/EUISafeArea/Center/`。
- Gameplay：Btn_Back、Btn_Settings，同上。
- Settings：Btn_Close、Txt_NowScene，位于 `Animator/EUISafeArea/Center/PanelBg/`。
- Loading：Cg_Progress、Img_ProgressBar、Txt_ProgressNum、TransitionBlock。
- GM：13 个绑定，含 Panel_GM、ProgressBar、Toggle、ScrollRect 与扩展组件；完整节点路径在 JSON 中。Panel_GM 是内部容器，不迁移为 Page。
- 文本使用包内 `SharedAssets/Fonts/钉钉进步体/DingTalk-JinBuTi SDF.asset`，依赖 NotoSansSymbols2 fallback。既有四个字体 SDF 修改不归本轮。
- 共同动画是 `Common/Animator/EUICommon_Ani.controller` 与 Enter/Exit；Loading 另用 `Common/Atlas/OverlayGradient.png`。完整依赖路径在 JSON 中。
- 运行设计分辨率 2560×1440，页面 CanvasScaler 为 ScaleWithScreenSize、match=0.5；Canvas 为 ScreenSpaceCamera。实际截图也是 2560×1440。

## 生成与引用迁移

[panel-references.txt](panel-references.txt) 记录 39 处现场旧名引用；[generator-references.txt](generator-references.txt) 记录生成入口。迁移至少覆盖：

1. Unity AssetDatabase 移动六个 Prefab 并保留上述 GUID；同步根名和 Binding.PrefabName；PageName/逻辑类已经是目标 Page 名，保持大小写。
2. `Assets/Game/Scenes/FrameworkScene.unity` 有六个实例根 m_Name 旧名覆盖，GUID 移动不会自动消除此类 override，必须核对。
3. `Assets/Game/UI/GamePages.cs` 六个 Asset 路径注册，通过正式生成来源/注册服务维护；运行业务用 GamePages 定义，不维护第二套字符串路径。
4. 对应 `Assets/Game/UI/Runtime/<ClassPath>/<类名>.cs` 与 `.Binding.cs` 的头部来源由 EUI 中心再生成，保留 EmberManaged 边界和用户区。
5. `EUICreationRequest.PrefabName="NewPanel"` 默认值须修正并新增 Page 命名校验，防止新建恢复旧名。
6. Loading 相关 `EUITransitionBlockPreviewTests.cs` 的两处路径、`docs/user/UI开发参考.md` 当前例子同步迁移。

配置为 `Assets/Ember/Editor/SOs/EUIBindingSettings.asset` → `EmberCSharpImplementation.asset`（GUID dab9ba0aa58acbf4d94a2a186be6bc16）。implementation 的 codePath 为 Assets/Game/UI/Runtime，namespace=Game.UI，默认 pageDefFile=GamePages.User.cs；Framework 模式由正式服务定位 GamePages.cs。不能仅按默认字段把六个 Framework 页改成 Business。

模板资源位于 `Packages/com.ember/UIExtension/Editor/Settings/`：CSharpBindingTemplate.tpl、CSharpCodeTemplate.Framework.tpl、CSharpCodeTemplate.tpl、CSharpPageDefTemplate.tpl。本轮未发现六个旧名在这些框架代码中的硬编码；实际旧名由 Prefab Binding 输入。

S1 应先检查目标 Page.prefab 冲突，再移动和再生成；内部 PanelBg/Panel_GM、Editor Panel 类型、历史记录无需机械替换。模板快照不直接编辑，修改后走项目中心普通保存。

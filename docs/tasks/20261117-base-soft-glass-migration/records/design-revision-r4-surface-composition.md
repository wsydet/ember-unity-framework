# R4（纠正稿）：base / UI 游戏接入正式背景

2026-11-18；任务 `20261117-base-soft-glass-migration`。按[新版R4需求](requirements-revision-r4.md)及用户“能看到unity默认背景，当时这里好像没处理”执行。**本文件此前的玻璃 tint／混合调整稿全部作废，正文已替换；旧消息中的面板换色、增大tint或模糊建议不得实施。** 目标是补正式底景，玻璃参数保持。本轮仅静态核查，未操作Unity或改产品；等R3独立对话交还编辑器后由原实施执行。

## 实际缺口

- 两模板 `Assets/Game/State/GameMainState.cs` 已在 `LoadBackgroundAsync` 通过 `SetBackgroundAsync(GamePages.EUIBackgroundPage)` 等待背景，但 **OnMainExit立即ClearBackground**。两份 `GameGameplayState.cs` 只启动会话/显示玩法页，没有补背景；UI游戏结果仍在同一Gameplay会话，设置Push也不会补背景。
- 两份 `Game/Scenes/GameplayScene.unity` 序列化对象只有GameSceneScope与方向光，没有MeshRenderer。FrameworkScene主相机 `330585545` 使用Skybox（m_ClearFlags=1），UI相机 `1413517660` 不清色并接在Base栈。背景页被清后，默认天空/地平线能进入最终画面及玻璃输入；步骤/结果原图与此吻合。无需改far、玻璃或全局Renderer。
- 现有 `EUIBackgroundPage` 是正式Background页（GamePages.cs：UILayer.Background / PageType.Background），Context固定sortingOrder=0，EUIViewEngine绑定UICamera。其 `Animator/Background` 是无Sprite浅色不透明EUIImageItem；不是完整美术背景。base首页另有 `Animator/BuiBackground`，所以首页已有图不代表玩法路径接通。
- 已查看可复用资源 **`GameResource/Resources/UI/Common/StaticTexture/BuiPilot/Background.png`**：1280×720浅白蓝抽象柔和曲面，无地平线、文字或深色主体；GUID `7bcbdb973c5c5084ba7eece4f8ca0649`，正是base Main的RawImage引用，两模板PNG字节相同。适合现有柔光玻璃和深色文字，不需生成新图或暗化界面。

## 最小可靠实现：两模板各3个正式文件

只在正式Load后改当前活动Assets，通过Prefab/Scene API、项目中心普通Save落盘。各模板独立应用，不父同步；meta/GUID及已有Binding保持。预计每模板仅下列3文件，额外路径若确有技术依赖先回交。

| 活动路径 | 操作 |
| --- | --- |
| `Assets/Game/State/GameMainState.cs` | 仅删除用户方法体OnMainExit里的 `EUIManager.Instance.ClearBackground()`，继续关闭各自首页。背景由已有EUIManager单槽位跨Main/Gameplay/设置/结果持有；保启动/返回Main的LoadBackgroundAsync和await。不改EmberManaged签名、框架状态机、玩法或场景会话。 |
| `Assets/GameResource/Resources/UI/Common/Prefabs/EUIBackgroundPage.prefab` | 原位保页、Animator、旧Background与GUID。在全屏Animator下增加非Binding装饰 `FormalBackground`：RawImage引用上述既有PNG，白色/alpha1、raycastTarget=false、UI层5；置于本Background页旧浅色Image上方，不是前景页或Overlay。旧Image可留作图缺失的浅色底，关闭其raycast。新节点只用RectTransform/CanvasRenderer/RawImage/AspectRatioFitter等必要组件，不加Canvas/Group。EnvelopeParent、aspectRatio=1280/720、中心pivot/居中裁切；父Animator继续stretch整个屏幕，不经SafeArea内缩，不用Contain留边或固定1920尺寸铺图。保根/Animator alpha1，不加淡入。 |
| `Assets/Game/Scenes/FrameworkScene.unity` | 通过Scene API仅将上述主相机改SolidColor，backgroundColor设项目浅色 `(.94,.96,.98,1)`，兜底启动第一帧、退出/遮幕边缘及背景尚未就绪时的底色。稳定正式界面仍是背景图。UI相机、stack顺序、RendererIndex、Brain/Lens/far及全局RenderSettings/Skybox不改；不整场景回灌。 |

横竖屏均用EnvelopeParent覆盖与中心裁切，背景无文字/人物，不需另做竖图。复用PNG且保持白色乘色。**不修改面板/列表/行/按钮tint、Gloss/Frost/EdgeLight/半径、Shader、BackdropSettings、Renderer、专用Steps Variant或共享对照资源。**

## 生命周期和采样约束

- SetBackgroundAsync已有同路径复用/共享pending请求；不另建常驻Manager、重复实例或fire-and-forget加载。启动仍由InitState在BootSplash揭开前await；返回Main的Prepare仍await。删除退出Clear后，Main→Gameplay及结果换页不出现“先清再补”的天空空窗。
- 所核两模板运行源的主动ClearBackground只在上述Main退出；Gameplay退出仅关popups/对应玩法页。设置Push/Pop、重开不清底景。最终关闭全部UI/框架退出仍由Context.Clear和UIViewEngine.Shutdown回收，不删除退出清理。若实测还有关闭背景的路径，沿既有await机制在揭幕前补齐，不每帧补实例。
- 背景Canvas排序0、在UICamera上先于内容页和GM前景段绘制，第一玻璃采样取得正式底景。FullScreenPopup隐藏对应Main/Popup，Background是独立槽位，不应被一起隐藏。不得把图塞入玩法Content前面、提高到Overlay或盖Loading。
- 目前这两模板是纯UI示例，玩法场景无可见世界几何；此处用正式Background槽位，不加遮住真实玩法的前景补丁。其他五模板不复制此常驻策略，不改世界/气泡/皮肤/相机。
- base Main已有BuiBackground可保原样（同图），先核无叠层/转场闪变，不为去重复绘制扩改菜单。新装饰没有Binding，一般无需生成代码；实际需绑定变化时走公开中心，不手写.Binding.cs。

## 短验收、保护与普通Save

1. 等R3交还Unity，保护topdown/I5草稿、字体/index/窗口和两份当前成品；核真实活动身份及未完请求。每模板保存修改前文件/Binding/GUID快照。C#修改后MCP主动编译，核本轮域重载/新MVID/Console；Prefab/场景也做MCP刷新验证。只加载活动Assets资产。
2. 沿真实FrameworkScene启动：BootSplash→首页→Gameplay→设置暂停/恢复→UI游戏3步/结果/重开→返回首页。记录转场和Loading揭幕前后帧，默认天空/地平线在屏幕外缘及玻璃内部都不出现；临时藏Skybox截图不算正式修复。
3. 1920×1080、1280×720、1080×1920核首屏/玩法/设置/结果全覆盖，无露边、拉伸或SafeArea空带。玻璃Lease Ready且实际采到项目背景，文字清楚；真实导航/点击不被背景抢走，Loading可见，暂停同局恢复/重开保持原语义。约2–4分钟相关运行并沿T01累计，不扩全量/设备/Player。
4. 运行跨状态仅一份Background。关闭内容消费者后玻璃边界/前景Camera/RT按原语义清退，最终框架退出背景页回收。两共享对照 `EUIDynamicListPage / EUIBuiCandidateDynamicListPage` 与OptionRow/Progress闭包SHA、Steps Variant及模板引用、所有玻璃参数不变。其他五子正式内容/metadata/快照不变。
5. 各走中心普通Save一次，核真实提交、镜像/meta/GUID/新实时hash；base仍0.11.0、ui-game仍0.2.3，原封存与ui-game的889父快照不动。恢复topdown/I5/原缓存和窗口现场，草稿恢复后不再Save topdown。旧图保留，新两模板真实图交用户与独立复验，本方案不签收效果。

**保存影响照旧：**当前base实时=封存 `1c5918eb6d798246aa4d080ac7b9eeee`。普通Save只改实时hash；保旧sealed载荷与六子原父快照，不把新草稿称为原sealed成品。现有正式Load/Save不以父封存为前置，可完成本次修正；base未封存后BuildParentSyncPlan会报告ParentUnversioned，新派生/父同步须未来另行授权封存。不能宣称六子同步了新背景，也不追加Bump、父同步、改hash绕过或发布。若实际需要框架/Renderer改动，停下并把具体证据交主对话。

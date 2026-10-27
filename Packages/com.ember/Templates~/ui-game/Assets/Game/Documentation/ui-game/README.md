# UI 游戏模板

ui-game 从 base 派生，提供一个三步完成的中性 UI 示例。页面与会话可以替换；模板没有颜色排序、产品关卡、评分或产品存档规则。

## 运行

1. 在项目中心确认当前模板为 ui-game，依赖安装完整且 Console 无编译错误。
2. 打开 `Assets/Game/Scenes/FrameworkScene.unity`，进入 Play。不要直接打开 GameplayScene 代替完整启动。
3. 首页点击 **Start a session**；局内点击 **Complete next step** 三次，到结果页选择 **Play again** 或 **Return home**。
4. 首页 Settings 使用 Main 来源；局内 Pause / settings 使用 Gameplay 来源。关闭按钮通过状态 Pop 返回，遮罩不负责退出状态。

`UiGameModule` 位于 `Assets/Game/Module/UiGame/`，声明 Gameplay Phase、Enabled=true，是本示例的装配条件。Gameplay 进入创建会话、退出释放。移除示例时同时替换 `GameMainState`、`GameGameplayState` 的页面接线，再禁用/移除模块；不要只关闭模块而保留依赖它的页面。PlayerData、GameTable、GameInput 保持继承的可选状态，本示例不要求启用它们。

GameplayScene 保留唯一启用的 GameSceneScope；纯 UI 也经过 SceneFlow 的准备、开始和退出。结果是同一 Gameplay 会话的完成阶段，不新增框架顶层状态。

## 页面与资源

| 页面 | 职责 |
| --- | --- |
| EUIUiGameHomePage | 开始、首页设置 |
| EUIUiGamePlayPage | 状态文字、三个动态步骤、推进、暂停、返回 |
| EUISettingPage | 继承 base 的设置弹窗，显示来源，关闭后恢复状态 |
| EUIUiGameResultPage | 完成提示、重开、返回 |

三个新增页面是 EUIStandardPage 的真实 Prefab Variant；按钮为公共 EUIActionButtonItem 嵌套引用。资源位于 `Assets/GameResource/Resources/UI/Module/UiGame/Prefabs/`，用户逻辑与生成绑定位于 `Assets/Game/UI/Runtime/Module/UiGame/`。动态宿主和 EUIDynamicOptionRowItem 复用 UIFoundation，保留文本、图标、进度和状态，没有整页截图充当 UI。

新页面采用 720×1280 参考尺寸，已在 Windows Editor 的 720×1280（9:16）、720×1600（9:20）验证鼠标指针交互。继承的设置弹窗沿用原参考尺寸并在上述竖屏检查。安全区验证范围为完整 Screen.safeArea 与明确标注的内边距布局模拟；真机异形屏、触屏设备、横屏、构建和全语言不在本轮覆盖范围。

## 继续开发

- [生命周期与扩展](lifecycle.md)：会话、页面、Item、回调所有权，以及持久化接入边界。
- [美术替换与默认资源](artwork.md)：当前契约、公共库、PSD/BUI、绑定与回执。
- [保护与升级](upgrade.md)：本地修改保护、普通保存、封存和消费升级。
- [术语](glossary.md)及继承的 [base 文档](../base/README.md)。

公共说明随安装包提供：`Packages/com.ember/Documentation~/ui/`。消费工程应使用实际兼容的包和正式 AI Skill 安装入口；本说明不要求开发仓库试点 PSD、开发机缓存或开发证据目录。创建模板和普通保存不代表已发布；实际可安装版本以发布包与项目中心记录为准。

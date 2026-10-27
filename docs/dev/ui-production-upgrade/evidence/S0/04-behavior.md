# S0 运行与视觉基线

工程/模板/代码基线见 [工程记录](01-workspace.md)。本轮在 2026-10-26 17:34—17:37 左右运行 FrameworkScene 的现有 GameBoot/GameLauncher 正式入口。不是孤立实例化页面测试。分辨率 2560×1440；仅覆盖此比例。

交互由 MCP 在真实运行实例上向 Button 发送 `ExecuteEvents.pointerClickHandler`，经过 Button.OnPointerClick 与真实监听器；没有直接调用状态切换替代按钮。它验证事件与行为接线，不等于物理鼠标坐标点击、触摸或手柄人工试玩。

| 操作 | 预期与实际 | 结果/证据 |
| --- | --- | --- |
| FrameworkScene → Play | 自动进入 Main；主界面、背景、GM FPS 可见，无在途转场 | 通过；main.png、runtime-observations.json 的 main |
| 主菜单设置按钮 | 进入 Settings；显示“当前场景：主界面”，主菜单被隐藏且按钮不可交互 | 通过；settings-main.png |
| 被遮挡的开始按钮发送点击 | Button.IsInteractable=false；保持 Settings，未进入 Gameplay | 通过；本轮 MCP 点击与状态查询结果 |
| 设置关闭按钮 | Fsm.Pop，恢复 Main，开始/设置重新可交互 | 通过；return-main |
| 开始游戏 | 进入 Gameplay，按钮变为返回主菜单/设置，转场结束 | 通过；gameplay.png |
| 玩法内设置 | 进入 Settings；显示“当前场景：玩法中”，返回/设置按钮不可交互 | 通过；settings-gameplay.png |
| 关闭玩法设置 | 恢复 Gameplay；按钮可交互 | 通过；return-gameplay |
| 返回主菜单 | 恢复 Main 及背景，主菜单按钮可交互，无在途转场 | 通过；final-main |
| 退出 Play | 回 FrameworkScene，未标脏 | 通过；validation.json |

运行观测原始记录：[runtime-observations.json](runtime-observations.json)。FindObjectsByType 包含 FrameworkScene 中原有禁用模板实例/运行缓存，所以不能用此列表总数证明泄漏或单实例所有权。后续 S1/S3 要针对实例 ID、归属和生命周期计数专项验证。

## 行为来源

- `EUIMainPage.OnInit` 的受管逻辑：Btn_Start → TransitionTo<GameplayState>；Btn_Settings → TransitionTo<SettingsState>(SettingsContext.Main)。
- `EUIGamePlayPage.OnInit`：Btn_Back → MainState；设置传 Gameplay 上下文。
- `EUISettingPage.OnInit`：Btn_Close → Fsm.Pop；OnOpen 按 SettingsContext 写入 Txt_NowScene。
- `GameSettingsState`：进入 ShowPopup，退出 CloseAllPopups。不能换肤后只隐藏 GameObject 而不 Pop。
- GM FPS 文本在运行采样之间变化，说明运行更新存在；未点击退出应用或改变 timeScale。
- timeScale 采样始终为 1；不据此推断所有 Gameplay Module 的暂停策略。

## 视觉事实与后续改造

- [主界面](main.png)：现有深灰背景、蓝色矩形按钮、标题换行、右上 GM FPS、右下版本号。
- [主菜单设置](settings-main.png)：深色卡片、上下文文本较窄而换行、关闭按钮；主菜单不再显示。
- [玩法页](gameplay.png)：深蓝底、标题及两个按钮。
- [玩法设置](settings-gameplay.png)：下层 UI 被隐藏后可看到场景天空/地面背景，与主菜单设置的背景不同。这是本轮旧版现场，后续替换对照时不要误记为新引入。

现有画面尚不满足 S2 的 macOS 中性浅色视觉目标，S0 仅建立真实基线。未验证：长文本压力、窄屏/安全区设备、禁用控件全部状态、实际鼠标射线命中、GM 全功能、Loading 动画逐帧截图与全部取消分支、重复开关泄漏计数。S0 的必要启动和导航已通过，以上不冒充后续阶段验收。

编译后与 Play 期间 Console 各查询为 0 错误/0 警告；退出后的只读摘要查询曾造成连接暂不可用，恢复后留下两条 MCP WebSocket 警告，详见工程记录。未运行全量或长时间测试。

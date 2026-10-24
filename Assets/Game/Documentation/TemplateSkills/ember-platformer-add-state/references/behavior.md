# 行为与源码路由

先从项目根读实际源码；下列是定位起点而非固定版本保证。

| 问题 | 主要源码 |
|---|---|
| 已有动作与顺序 | `Assets/Game/Module/Player/States/`，尤其PlayerLocomotionState、GroundState、AirState、RollingState、PlayerRule |
| 每角色执行能力 | PlayerContext、PlayerController、PlayerRuntimeCandidate；`Assets/Game/Module/Entity/`的Motor/Sensors/Powers/Effects |
| 方案/冻结 | PlayerScheme、PlayerSchemeCapture、PlayerConfig、PlayerSchemeValidation、PlayerSchemeObservation |
| 有限规则编辑 | PlayerSchemeRules、PlayerRuleConditions、Editor/PlayerSchemeRuleWorkflow、PlayerSchemeRuleEditor |
| 图与声明 | `Packages/com.ember/Core/Runtime/Fsm/`；相关文件名通过搜索类型定义确认 |
| 保存恢复 | PlatformerSnapshot、PlayerModule及PlayerData适配 |
| 动画 | PlayerAnimationProfile、PlayerAnimationSession、PlayerAnimationView |
| 编辑/资产 | Editor/PlayerSchemeWorkflow、PlayerSchemeLibrary、PlayerFsmParameterEditor；框架EmberFsmExecutionPreview与Blueprint |

运行机制、稳定资产ID与方案引用是事实来源，不能以节点显示名称或旧文档决定行为。

## 最小行为约定

将用户需求和已知代码整理成可审阅事实：

- 动作是互斥阶段、复合阶段、已有动作的附加许可，还是可以并存的效果？是否已有对应节点/Ability？
- 哪个父级负责公共运动，进入需要哪些支撑/几何/输入？默认子节点及禁用回退是什么？
- 在Update还是FixedUpdate、哪个segment尝试，相邻请求顺序及继续策略如何？父级哪些动作可打断？
- 进入预检只查询什么？何时消费输入/次数/CD，谁创建/释放运动接管和效果？拒绝、异常、停止如何处理？
- 持续条件、时长单位、每步运动和所有退出路径是什么？动画播放完不是默认退出契约。
- 暂停是否冻结，恢复是回安全状态还是重建该动作？动作期间允许保存哪些已有事实？
- 唯一启用开关/参数位置、所需动画、共享图影响和新增物理接线是什么？

必要答案可从现有约定推导时直接说明依据；缺失关键退出/恢复规则时停止依赖实现，不默设“动画结束退出”或把瞬时阶段塞进存档。已有地空冲刺、二段跳、墙面、下穿等需求先考虑开启对应能力；若用户要八方向冲刺或全新行为，说明当前仅水平等真实限制，进入新行为设计。

## 共享与版本影响

用AssetDatabase.FindAssets查所有PlayerScheme，读取每项Graph引用，以GUID/localID归组；另查PlayerConfig的旧装配、开发选择和相关自定义图。配置资产可能含子资产，检查LoadAllAssetsAtPath。列出内置只读、用户自定义和受影响动画配置；不能只检查当前窗口。

扩展项目共享图会影响所有使用者，动画映射必须同步补齐。单方案专用图涉及新的图身份、独立布局和引用，范围不明先确认。只改参数不复制图；修改内置共享资产需符合既有保护/维护流程，不能清除hideFlags或篡改登记来强行写入。

新能力稳定ID、最大次数/CD和恢复策略可能影响旧档严格兼容。先查PlayerPowerSession与快照验证，不能给旧档补资源来掩盖不兼容。添加快照字段或精确动作恢复属持久化边界，必须在当前对话给出兼容方案再实施。

# GameInputModule（base 公共模块）

本模块是可选的 Global 业务模块，运行时程序集为 `Game.Input.Runtime`。base 示例显式
`Enabled = false`；项目采用本模块时改为 `true`。平台跳跃的玩家协调模块、动作名映射、
土狼时间及跳跃缓冲不在这里实现。输入服务不暂停 FSM、物理或游戏时间。

## 配置与生命周期

默认配置位于 `Assets/GameResource/Resources/Config/GameInput/GameInputConfig.asset`。
`OnInit` 自行同步加载 `Resources/Config/GameInput/GameInputConfig`（API 路径不含 Resources），
再让已有的 `EmberInputManager` 准备输入资产。一个通道对应一个独占 Action Map；配置重复、
缺失 Map 或已有 Manager 使用不同资产会得到明确失败，不悄悄接管另一套输入。

示例通道 `gameplay` 默认关闭，`ui` 默认打开。名字全部来自配置，公共代码不认识这些名字。
示例 `Direction`、`Primary`、`Confirm` 用于演示连续输入和新按下语义；UI Map 不是完整的
`InputSystemUIInputModule` 配置，不会自动替换已有 uGUI 事件系统。

初始化前允许 `SetConfigurationForNextInit(config)` 注入配置；额外传入 `preparedActions`
时由特殊启动流程/测试负责其资产寿命，跳过 Manager 准备。注入不激活服务。
常规流程中 Manager 持有输入资产，模块只管理配置中的 Map，不销毁 Manager 或资产。

- `TryGetModule` 只表示登记，不能证明服务 Ready。
- `State` 区分 Uninitialized、Preparing、Ready、Faulted、Stopped。
- `WhenReady` 是当前世代的准备结果，可重复等待。初始化前立即返回 NotInitialized，
  不通过读取属性隐式初始化。本版配置与准备为同步操作，OnInit 返回前给出最终结果。
- Collector 在 OnInit 返回后才标记模块活动，后续玩家流程仍须在正确的 Gameplay 钩子交接。
- Reset/OnDestroy 关闭通道、清空订阅和请求，旧句柄失效；重新 OnInit 产生新服务和世代。
- InputShutdown 同样使服务停止。普通 Main/Gameplay 场景切换不 Reset Global 输入模块。

## 请求与仲裁

请求者持有句柄并在自身退出时释放。每通道独立取最高优先级的明确规则；同级 Deny 优先。
Unchanged/未列出的通道不干预；无请求采用配置默认值。释放只撤回该请求，不恢复历史模式。
请求与配置在服务内取得快照，调用者随后修改数组不改变已接受规则。

```csharp
using Game.Input;
using Ember.Core;

// 由已激活的业务流程执行，不在角色 Awake 自动开始。
if (!EmberModuleCollector.Instance.TryGetModule<GameInputModule>(out var input))
    return; // 当前项目没有装配模块。
var ready = await input.WhenReady;
if (!ready.Succeeded || input.State != GameInputServiceState.Ready || ready.Epoch != input.Epoch)
    return; // 向业务流程返回依赖错误。

var service = input.Service;
var gameplay = service.GetChannel("gameplay");
var primary = new GameInputButton(gameplay, "Primary");
// 玩家流程在依赖、Context、FSM 已准备且 FSM 启动成功后才开放。
var sessionRequest = service.Request(sessionOwner, 0,
    new GameInputRule("gameplay", GameInputPermission.Allow));

// 菜单持有独立请求；过场可以同时持有自己的 Deny。
var menuRequest = service.Request(menuOwner, 100,
    new GameInputRule("gameplay", GameInputPermission.Deny),
    new GameInputRule("ui", GameInputPermission.Allow));
menuRequest.Dispose(); // 过场仍在时 gameplay 仍关闭。

// 在角色统一驱动链中采样连续意图、消费一次新按下。
var direction = gameplay.ReadContinuous<UnityEngine.Vector2>("Direction");
bool pressed = primary.ConsumePress();
bool charging = primary.IsHeld;

// 会话结束（幂等），业务同时自行停止 FSM 和运动。
primary.Dispose();
sessionRequest.Dispose();
```

`ReleaseOwner(owner)` 用引用相等统一清理该所有者的请求。所有者不是自动销毁检测器，
MonoBehaviour/菜单/会话必须显式释放；不要用短命对象创建无主请求。
请求 `Update` 先验证全部规则后替换，重复通道与未知通道立即报参数错误。
全部 API 限 Unity 主线程调用。

## 输入恢复与适配器

`ReadContinuous<T>` 显式选择连续语义，关闭时返回零。启用后的下一次 Input System 更新
可恢复仍按住的轴值。不能只因 Action 类型是 Value 就推断它没有触发语义。

`GameInputButton` 显式选择物理新按下/蓄力起始语义，实例拥有自己的待消费标记与按住状态：

1. 关闭时立刻清除待消费标记并结束按住语义；禁止期间不累积事件。
2. 开放时检查绑定控件的物理状态，仍按住则等释放再按下；新创建适配器同样遵守这个规则。
3. 在设备状态事件边界追踪释放与新按下，同一次输入更新内的释放/再按下也能分辨；
   不依赖 Button 的初始状态检查，不把恢复时按住当作一次新动作。
4. 新按下标记可跨输入更新与物理更新，ConsumePress 只成功一次；短促点按释放后仍可消费。
5. 同一动作的多个适配器互不消费；每个适配器由自己的宿主 Dispose。

该示例支持直接 Button/Axis 控件绑定，拒绝组合按键绑定（避免把修饰键单独按下当作整个组合成立）。
这是物理控件语义选择，与 Action 的 Button/Value 类型区分开；Vector2 等连续输入仍走 ReadContinuous。
它读取绑定控件的物理按压阈值，不把 Tap/Hold/自定义 Interaction 的 performed
解释为技能完成，也不提供时间窗口、连击计数、重绑定或多人设备配对。需要这些语义时，
项目适配器订阅通道 AvailabilityChanged，保留相同的清缓冲和新按下契约。
通道通知在 Map 状态更新后发出；订阅者应读取当前 IsAvailable，绑定时也先读一次。
使用本模块时，业务不得另行 SwitchMap 或直接启停它管理的 Map，否则会绕过仲裁。

## 验证与交付

EditMode 测试程序集：`Game.Input.Tests`，测试依赖 `Unity.InputSystem.TestFramework`。
测试通过官方 InputTestFixture 隔离输入环境，创建独立输入资产与虚拟键盘并在结束时恢复原环境；
覆盖仲裁、所有权、配置快照、恢复、新按下、一次消费、模块世代及失败结果。
基础输入资产和配置也有 Resources 加载测试；资产重载、真实启动及设备输入仍需 Unity 验收。
2026-10-08 经 Unity MCP 验证：24 个用例通过，0 失败/跳过；Unity 编译及域重载完成。
这不替代默认 Manager 接线的完整 PlayMode 启动、真实设备操作及后续玩家场景验收。

这些目录归 base：`Assets/Game/Module/GameInput/`、
`Assets/GameResource/Resources/Config/GameInput/`、本说明及各自 `.meta`。
先在 base 编辑副本验证；保存 base 后再通过正式模板父级同步将其传给 platformer-2d。
上述24项为早期模块验证；公共输入随后已通过正式 base 保存与派生同步。新的改动仍须核对当前模板与父基线，不能把历史保存当作已发布或已部署；切换模板由用户操作，先保护未保存成果。

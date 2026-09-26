# 实现与目录规范

## 1. 先判定成果归属

| 判定 | 归属 | 本技能该怎么做 |
|---|---|---|
| 只是当前游戏的输入流程、小游戏、外部系统联动 | **消费项目** | 只新增消费端文件，正常落盘 |
| 让所有项目都能通过 API 拿到通用能力 | Ember 框架（`Packages/com.ember`） | **不在本技能范围**：报出候选抽象，交框架批次处理 |
| 新项目应获得的 `Assets` 初始内容 | 模板 | **不在本技能范围**：需要正式模板保存 → Bump |
| 想改 `NovelCommandKind`、校验器、`NovelSession` 等模板自带文件 | 模板 | **一律不做**，见下一节 |

改 `Packages/com.ember` 的框架源码、或改 `Templates~` 快照、或执行 SaveTemplate/Bump，
都属于框架/模板发布动作，本技能不执行。

## 2. 只新增文件，不修改模板自带文件（硬约束）

模板管理**五个目录**：

```
Assets/Game
Assets/Resources
Assets/Ember/Editor
Assets/Settings
Assets/GameResource
```

- **完整重新部署会覆盖**这五个目录里的内容。
- 补丁增量更新只保留「本地独有内容与单方修改」。

因此：

| 做什么 | 允许 |
|---|---|
| 在 `Assets/Game/Module/<你的模块>/…` 新增自己的脚本 | ✅ |
| 在 `Assets/GameResource/Authoring/Narrative/Scripts/<你的模块>/` 新增脚本资产 | ✅ |
| 修改 `Assets/Game/Module/Narrative/NovelSession.cs` 等模板自带文件 | ❌ 会在下次模板更新时冲突 |
| 往 `NovelCommandKind` 里加枚举值 | ❌ 同上 |

确认服务会在闸门处直接阻断「`ownership=template-owned` + `action=modify`」的计划，
这条规则不是风格建议，是部署模型决定的。

## 3. 目录与命名

| 内容 | 位置 |
|---|---|
| 业务 Module（玩法逻辑） | `Assets/Game/Module/<你的模块>/` |
| 自定义节点脚本类 | `Assets/Game/Module/<你的模块>/Narrative/` |
| 脚本资产（`NovelCustomStepSO` 子类资产） | `Assets/GameResource/Authoring/Narrative/Scripts/<你的模块>/` |
| UI 逻辑 | `Assets/Game/UI/Runtime/Module/<模块名>/`（`Runtime` 是必需层级，由 UI 中心生成） |
| UI Prefab 资源 | `Resources/UI/Module/<模块名>/` |

模块目录名用你的玩法名，**不要**把内容塞进 `Narrative` 目录冒充模板自带文件。

## 4. 业务 Module

```csharp
[EmberModule(ModulePhase.Gameplay, Enabled = true)]
public sealed class MyModule : EmberSingleton<MyModule>, IEmberModule, INovelStepService, IEmberUpdate
{
    public void OnInit() => EmberServiceLocator.Register<INovelStepService>(this);
    void IEmberModule.OnDestroy() { /* 注销 + 中止所有未完成请求 */ }
    public void Invoke(string requestKey, string executionId, string payload,
                       Action<string> complete, Action<string> fail) { /* 启动业务 */ }
    public void Abort(string executionId) { /* 立刻停止回调、关页 */ }
    public void Update() { /* 自己推进，不依赖剧情会话的 Tick */ }
}
```

必须遵守：

- `[EmberModule(...)]` 的 `Phase` 与 `Enabled` **都要显式写**，不得依赖默认值。
- 用 `EmberSingleton<T>` + `EmberServiceLocator` 暴露能力，**不要**让剧情层去访问具体 Module 类型。
- 小游戏的推进放 Module 自己的 `Update`（`IEmberUpdate`），不要放节点脚本的 `OnTick`。
- `OnDestroy` / `Abort` 必须把未完成请求显式失败掉，避免剧情侧永久等待。

## 5. 正式 UI 必须经 UI 中心

**不允许手搓 Prefab、不允许手工拼 binding。** 新页面走 UI 中心：

```csharp
EUICreationService.TryBuildPlan(request, out var plan, out var preflight);  // 先预检
var result = EUICreationService.Create(request);                            // 通过后再创建
```

- 业务界面用 `Business`，可复用控件用 `Item`。
- 同名资源先检查 / 修改，**不能删掉用户资源重建**。
- 绑定调整后用统一的 `EUIBindingCodeGenUtility.TryRegenerateCode` 重新生成，再基于实际生成结果写用户逻辑。
- 细节流程见框架技能 `ember-eui-build`；本技能只负责把「这一批需要哪个页面」写进方案并在页面里确认。

如果环境里没有可用的 UI 中心接口，就**报告所需升级**，不要绕过它去手写 Prefab。

## 6. Unity 编译与验证

- **Unity MCP 是唯一的自动编译验证入口。** 改完 C# / 程序集 / 场景 / 资源后，用 MCP 触发刷新并读取编译结果。
- MCP 可用时只做**一次有界**的编译状态确认；只有在 MCP 明确报告正在编译时才等待该次编译，禁止无意义轮询。
- MCP 不可用、调用超时或首次查询失败时：**立即停止编译验证**，不要用等待 Unity 自动刷新、反复读 `Editor.log`、
  轮询 Unity 进程、启动 BatchMode、改生成的 `.csproj` 或跑 `dotnet build` 来替代。
- MCP 不可用时仍可做即时静态检查，但**不得声称「编译通过」**。

MCP 不可用时，最终回复必须原样写明：

> 当前 Unity MCP 未连接或不可用，本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。

## 7. 落盘后的核对

1. **剧情校验**：走当前 Story 的 `TryReadDefinition` / 编辑器「校验剧情」，确认没有
   `BadCustomStep` / `MissingStory` / `BadSpeakerVariable`。
2. **自定义节点专项**：`Game.Narrative.Tests` 的 `NovelCustomStepRegistryTests` 与
   `NovelSessionTests` 的自定义节点分部覆盖了调度与指纹契约；模板有这些测试时至少跑一次。
3. **指纹影响**：本批若新增了自定义节点步骤，明确告诉用户**旧存档可能不兼容**；
   若只加了说话人变量，说明它不进指纹、旧档不受影响。
4. **手动验收**：编写流程 / 输入 / 结果回写这类需求，必须由用户在 Unity 里实际跑一遍；
   静态校验通过不等于画面通过。

## 8. 交付报告要写什么

- 落在了哪一级阶梯，为什么不用更低的一级。
- 新增 / 修改的**文件清单**（含归属：`new` / `template-owned`）。
- 脚本 ID、脚本资产路径、是否已登记进剧情清单。
- 剧情步骤的插入位置、变量 ID / 类型 / 作用域。
- 业务 Module 名、请求键、超时、`Abort` 行为。
- UI 是否经 UI 中心制作。
- 指纹影响（进 / 不进）与旧档兼容结论。
- 实际完成的验证与**未完成**的验证（不要混为一谈）。

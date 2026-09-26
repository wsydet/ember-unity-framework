# 自定义节点（剧本层扩展点）

> 适用：需要在剧情中间插入**框架没有内置**的行为时，例如开局让玩家输入名字、把一段小游戏插到步骤之间、
> 调用外部系统并等它返回。本文是这套扩展点的契约说明；面向 AI 客户端的流程化版本见
> [技能文档](../TemplateSkills/catalog.json)。

## 1. 先分清四个层次

剧情编辑里已经有三层可复用的东西，自定义节点是第四层。它们不是替代关系：

| 层 | 形态 | 谁来写 | 典型用途 |
|---|---|---|---|
| 基础步骤 | 段内一条指令（`NovelCommand`） | 不用写代码 | 对白、演出、赋值、等待 |
| 二级步骤 | 一串基础步骤的彩色分组 | 不用写代码 | 把「进场」「受击」这样的一组参数化步骤折叠起来 |
| 自定义步骤（预设） | `NarrativeStepPresetSO` 资产 | 不用写代码 | 把一组步骤存成资产，供其他剧情复用 |
| **自定义节点** | 继承 `NovelCustomStepSO` 的脚本 + 资产 | **写 C#** | **引入全新行为**：输入、小游戏、外部系统 |

> **术语提醒**：「自定义节点」指的是本文这一层（脚本级）。流程图上真正叫「节点」的是
> 对话段 / 选择 / 条件分流 / 结局 / 章节出口这五种 SO；自定义节点是**对话段内部的一个步骤**。

## 2. 扩展性分三级，优先用低的一级

| 级 | 做法 | 需要写代码吗 |
|---|---|---|
| 1 | 二级步骤预设（包括 `MemoryAtmosphere` 这类现成预设） | 不用 |
| 2 | **模块桥接节点**：填「请求键 + 结果变量 + 超时」，业务逻辑放业务 Module | 只写 Module |
| 3 | 继承 `NovelCustomStepSO` 写专属脚本 | 写脚本（+ 可选 Module） |

**绝大多数小游戏都落在第 2 级。** 开头让玩家输入名字、插入猜数字/拼图之类的小游戏，
都应该先试第 2 级：节点侧零代码，业务逻辑完整地留在业务模块里。

## 3. 三级分工（务必遵守）

```
剧情层（本模板，Game.Narrative）
  NovelCustomStepSO 基类、NovelCustomStepContext、调度 / 取消 / 校验 / 指纹
        ↑ 只通过中性契约调用
桥接层（消费端，很薄）
  自定义节点脚本：启动业务 + 接收结果；不写玩法逻辑
        ↑
业务层（消费端）
  独立的 IEmberModule：小游戏、输入页、外部系统的全部逻辑
```

**为什么必须这样分**：剧情层不应该知道任何具体玩法。事件、小游戏进度、关卡数值都属于业务，
放在脚本字段里会让一次剧情的复用和存档都失控；而脚本资产是**共享**的。

## 4. 最小自定义节点

```csharp
using Game.Narrative;
using UnityEngine;

// 放在消费端自己的模块目录：Assets/Game/Module/<你的模块>/Narrative/
public sealed class MyStep : NovelCustomStepSO
{
    [SerializeField] private string _variableId = "flag";

    public override string Summary() => "我的步骤 → " + _variableId;

    public override string Validate(NovelCustomStepValidation validation)
        => string.IsNullOrWhiteSpace(_variableId) ? "必须填写目标变量" : null;

    public override void OnBegin(NovelCustomStepContext context)
    {
        // 本次执行的状态放 context.State，不要放脚本字段（资产是共享的）
        context.SetVariable(NovelVariableScope.Global, _variableId, new NovelValue(true));
        context.Complete();   // 不调用就会一直等下去
    }
}
```

## 5. 生命周期钩子

| 钩子 | 时机 | 对应 MonoBehaviour 的说法 |
|---|---|---|
| `Validate(NovelCustomStepValidation)` | 剧情校验期，不接触运行状态 | `OnValidate` |
| `Summary()` | 步骤列表与图上的摘要 | —— |
| `OnBegin(context)` | 启动。默认实现直接 `Complete()` | `Start` |
| `OnTick(context, delta)` | 逐帧，只在**未暂停**时调用 | `Update` |
| `OnEnd(context)` | `Complete` 或 `Fail` 之后各一次 | `OnDestroy` |
| `OnCancel(context)` | 被读档 / 退出 / 故障打断 | `OnDestroy`（被打断） |

| 属性 | 默认 | 说明 |
|---|---|---|
| `ScriptId` | 类型全名 | 稳定 ID。**重命名类型时必须显式覆写并保留旧值**，否则旧档判为不兼容 |
| `DisplayName` | 资产名 | 下拉与图上显示的名字 |
| `WaitForCompletion` | `true` | `false` = 发射后不管，`OnBegin` 返回后剧情立即继续 |
| `IncludeInFingerprint` | `true` | 参数是否进剧情指纹（见第 8 节） |

## 6. `NovelCustomStepContext` 能做什么

| 成员 | 说明 |
|---|---|
| `Command` | 本步骤的参数（只读） |
| `ExecutionId` | 本次执行的唯一标识，跨模块请求 / 结果配对时原样带回 |
| `ChapterId` / `NodeId` | 稳定的执行位置 |
| `State` | **本次执行私有的状态槽**，脚本运行状态必须放这里 |
| `IsAlive` | 会话是否仍然有效；**业务模块的迟到回调必须先检查它** |
| `Complete()` / `Fail(msg)` | 结束本节点，各只能生效一次 |
| `SetWait(reason)` | 追加等待原因（资源、配音、动作等） |
| `SetVariable(scope, id, value)` | 写入剧情变量；未声明、类型不符、跨作用域都会失败并给出原因 |
| `TryGetVariable(scope, id, out value)` | 读取剧情变量 |
| `AcquirePause()` | 暂停凭据。注意：暂停会同时停掉本节点的 `OnTick`（见第 9 节） |
| `SetInputLock(reason, locked)` | **玩家推进锁**：锁定期间点击与空格都不推进剧情，但演出、计时、自动播放照常。与暂停的区别见第 9 节 |
| `SetReadMode(mode)` | 切换手动 / 自动 / 快进阅读模式 |
| `SetAutoIntervalOverride(seconds)` | 临时覆盖自动播放间隔（`null` 恢复玩家设置）。脚本化段落用它固定节奏 |

脚本**拿不到** `NarrativeRunner`，也不能改变执行位置。

## 7. 推荐形态：模块桥接节点

`NovelStepServiceBridgeSO` 是模板自带的通用桥接脚本（`Assets/Create/Ember/视觉小说/模块桥接节点`）。
节点侧只填三个字段：

| 字段 | 说明 |
|---|---|
| 请求键 | 业务模块用它区分请求 |
| 参数（可空） | 自由文本，格式由模块自己解释 |
| 结果变量 / 作用域 | 模块返回的文本写进哪个变量；留空表示只看完成 / 失败 |
| 超时（秒） | **必须大于 0**：没有超时的节点会让剧情永久停在这里 |

业务侧实现中性契约并注册：

```csharp
[EmberModule(ModulePhase.Gameplay, Enabled = true)]
public sealed class GuessNumberModule : EmberSingleton<GuessNumberModule>, IEmberModule, INovelStepService, IEmberUpdate
{
    private readonly Dictionary<string, (Action<string> ok, Action<string> fail)> _pending = new();

    public void OnInit() => EmberServiceLocator.Register<INovelStepService>(this);

    void IEmberModule.OnDestroy()
    {
        EmberServiceLocator.Unregister<INovelStepService>();
        foreach (var pending in _pending.Values) pending.fail?.Invoke("小游戏模块被销毁");
        _pending.Clear();
    }

    public void Invoke(string requestKey, string executionId, string payload, Action<string> complete, Action<string> fail)
    { _pending[executionId] = (complete, fail); /* 打开小游戏页面 */ }

    public void Abort(string executionId) { _pending.Remove(executionId); /* 关页、中止 */ }

    public void Update() { /* 小游戏自己的推进，不依赖剧情会话的 Tick */ }
}
```

结果写回变量后，**后续分支继续用既有的选择 / 条件分流节点**：顺序命中、必填兜底、存档兼容都是现成的，
不需要为「小游戏胜负」新增节点类型。

| 契约 | 为什么 |
|---|---|
| 同一 `executionId` 最终恰好完成或失败一次 | 会话按执行标识去重，重复回调会被忽略 |
| `Abort` 必须立刻停止回调并收干净 | 读档、退出、故障都会打到它 |
| 迟到回调先查 `context.IsAlive` | 否则会给已经结束的会话写变量 |
| 失败必须显式 `Fail` | 运行器不会自己超时；既不完成也不报错会让剧情永久卡住 |

## 8. 存档与指纹（两套规则，不要混）

| 新增数据 | 是否进剧情指纹 | 理由 |
|---|---|---|
| 自定义节点的 `ScriptId` + **脚本资产内容** | **进** | 它改变执行语义。改脚本参数会让旧档判为不兼容，与「语义变化拒绝旧档、不静默重置」的既有口径一致 |
| 说话人变量绑定（`_speakerVariableId`） | **不进** | 与既有 `speakerNameKey` 同口径：纯表现层，不改变对白角色键与强调匹配 |

- 脚本可以关掉 `IncludeInFingerprint`，作者自行承担「旧档配新参数」的风险。
- **没有任何自定义节点的既有故事，指纹逐字节不变**；说话人变量也不影响指纹。
- `NovelCheckpoint.CurrentSchemaVersion` **本轮没有变化**：说话人变量是新增的可空字段，
  旧档读回来是空值，与「没填」等价，行为退回原回退链。

## 9. 运行期契约（最容易踩的几条）

| # | 规则 |
|---|---|
| 1 | **既不 `Complete` 也不 `Fail` = 剧情永久卡住。** 运行器不会自己超时，这也是为什么桥接节点强制要求超时 |
| 2 | **不要把关键推进放在 `OnTick`。** 会话一旦被暂停（`AcquirePause`、打开菜单 / 存档 / 历史等），逐帧 Tick 会提前返回，`OnTick` 就不再被调用。持续推进的逻辑交给业务 Module 的 `IEmberUpdate` |
| 3 | **运行状态放 `context.State`**，不要放脚本字段：脚本资产是共享的 |
| 4 | **`OnCancel` 必须收干净**：退订事件、释放暂停、通知业务模块中止。用 `EmberEventGroup` 可以一键退订 |
| 5 | **脚本抛异常不会带崩剧情**：`OnBegin` 异常记为故障，`OnTick` / `OnEnd` / `OnCancel` 异常只记警告 |
| 6 | 外部等待期间**不能存档**（会话处于演出等待，不是稳定点）。这天然避免了「读到小游戏进行到一半的存档」 |
| 7 | 读档时旧会话会被 `Dispose`，`OnCancel` 一定会被调用一次 |
| 8 | **`ScriptId` 默认是类型全名，同一个脚本类型只能有一个资产。** 需要「一个类型配多个资产」（例如成对的开场段落 Begin / End）时，必须像 `NovelOpeningSegmentSO` 那样覆写 `ScriptId` 给出各自稳定的区分，否则剧情校验会直接报「自定义节点 ScriptId 重复」 |
| 9 | **「推进锁」不是「暂停」。** 暂停冻结一切（连自动播放、计时、`OnTick` 都停）；`SetInputLock` 只挡玩家输入，剧情该走还走。想要「自动播放且玩家点不动」的段落就必须用锁，用暂停会把它冻在原地 |

### 自动播放的脚本化段落（示例参考实现）

`NovelOpeningSegmentSO` 是模板自带的成对节点，演示了「一段不被打断的自动演出」怎么做：

```
CustomStep(开场段落 · Begin)   ← 锁输入 + 切自动 + 覆盖自动间隔
Say …（渐显）                   ← 自己按固定节奏往前走，玩家点不动
CustomStep(名字输入)
Say …（渐显）
CustomStep(开场段落 · End)      ← 恢复手动 + 解锁 + 还原间隔
```

它同时处理了两件容易被忽略的事：Begin 若**在页面/场景暂停期间**执行，`SetReadMode(Auto)` 会被拒绝——节点于是保持存活逐帧重试，超时后放弃自动**并解锁**，绝不把玩家锁死；`OnCancel` 也兜底解锁并还原间隔，避免锁残留到下一次开局。

## 10. 目录与文件放置

| 内容 | 位置 |
|---|---|
| 脚本类 | `Assets/Game/Module/<你的模块>/Narrative/` |
| 脚本资产 | `Assets/GameResource/Authoring/Narrative/Scripts/<你的模块>/` |

**唯一必须遵守的规则：只新增文件，不要修改模板自带的文件。**
`Assets/Game`、`Assets/Resources`、`Assets/Ember/Editor`、`Assets/Settings`、`Assets/GameResource`
都是模板管理的目录：完整重新部署会覆盖它们，补丁增量更新只保留「本地独有内容与单方修改」。
扩展点就是为这条规则存在的——消费端只新增自己的脚本与资产，永不修改 `NovelCommandKind` 之类的模板文件。

## 11. 编辑流程

1. **登记脚本**：流程编辑器 → 章节总览 → 「自定义节点脚本清单」加入脚本资产。
   *步骤里只保存 `ScriptId`，资产实例必须登记在剧情上，才能随剧情一起加载、校验并计入指纹。*
2. **插入步骤**：对话段内容页 → 「插入二级步骤 / 自定义步骤…」或直接在步骤类型里选「自定义节点」，
   然后在下拉里选脚本。
3. **校验**：点击「校验剧情」。未登记、`ScriptId` 重复、脚本自身 `Validate` 报错都会在这里出现，
   **不会等到运行时才炸**。

> 运行期只按剧情清单解析脚本，不做全项目反射扫描：因此 IL2CPP 裁剪不会把脚本裁掉，
> 也不会出现「运行时找不到脚本」。反射只用在编辑期的下拉列表里。

## 12. 常见错误

| 现象 | 原因 |
|---|---|
| 「自定义节点脚本未在本剧情登记」 | 脚本资产没有加进剧情的脚本清单 |
| 「自定义节点必须在剧情会话中运行」 | 用独立章节启动（`NarrativeRunner.Start`）跑了带自定义节点的章节；独立 Start 没有脚本清单 |
| 「项目里找不到脚本 <id>」 | 脚本资产被删掉了，或脚本类型改过名而 `ScriptId` 仍然写死旧值 / 反过来 |
| 剧情停在自定义节点不动 | 脚本没有调用 `Complete` / `Fail`，或业务模块没有返回 |
| 「结果变量未声明」/「类型不符」 | 桥接节点的结果变量没声明，或声明成了不支持的类型 |
| 「自定义节点 ScriptId 重复」 | 同一个脚本类型的多个资产默认共用类型全名；需要覆写 `ScriptId` 区分（见第 9 节第 8 条） |
| 玩家点不动、剧情也不走 | 推进锁被打开却没有配对解锁。检查成对节点的 `_lockReason` 是否一致，并确认 `OnCancel` 兜底解锁 |
| 说话人栏空白 | 说话人变量未声明或不是字符串（校验期会报 `BadSpeakerVariable`）；空白值会自动退回角色名 |

## 13. 验证

- `Game.Narrative.Tests` 里 `NovelCustomStepRegistryTests`（指纹划界、剧情校验、编辑器步骤名称表守卫）
  与 `NovelSessionTests` 的自定义节点分部（启动 / 等待 / 完成 / 失败 / 取消 / 桥接 / 说话人变量）覆盖上述契约。
- 手动验收：黑幕 + 几段话 → 输入名字 → 后续对白与**说话人栏**都显示这个名字 →
  存档后读档名字仍在 → 中途改名后历史里的旧句跟着变。
- 改脚本参数后确认旧档被判为不兼容（而不是静默读取）是预期行为。

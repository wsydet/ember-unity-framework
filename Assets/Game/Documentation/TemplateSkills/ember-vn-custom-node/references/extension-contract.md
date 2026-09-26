# 扩展点契约速查

> 权威依据是模板内的 [`Documentation~/visual-novel/CustomNodes.md`](../../../visual-novel/CustomNodes.md)。
> 本页是给执行代理用的速查；与源码冲突时以**当前项目源码**为准，并把差异报出来。

## 1. 先判断落在哪一级阶梯（必须逐级比较）

| 级 | 形态 | 代码量 | 什么时候用 |
|---|---|---|---|
| 1 `preset` | 二级步骤预设（`NarrativeStepPresetSO` + `NarrativeStepGroups`） | 零代码 | 想要的只是把**已有指令**编成一组可复用步骤 |
| 2 `bridge` | 模块桥接节点 `NovelStepServiceBridgeSO` + 业务 Module | **只写业务 Module** | 输入、小游戏、外部系统——绝大多数需求在这一级 |
| 3 `script` | 继承 `NovelCustomStepSO` 写专属脚本 | 写脚本（+ 可选 Module） | 需要特殊交互、要按剧情变量分支、要自定义表现 |

**默认走第 2 级。** 只有第 2 级确实表达不了时才降级到第 3 级，并在方案里写明为什么。
第 1 级不需要新增文件，因此它的确认页面里文件列表应当为空。

## 2. 数据侧（模板已实现，不要另行设计）

| 位置 | 内容 |
|---|---|
| `NovelCommandKind.CustomStep` | 枚举末尾追加的步骤类型；**只能继续追加在末尾** |
| `NovelCommand._customStepId` | 只保存脚本的稳定 ID `ScriptId`，**不保存资产实例** |
| `NovelCommand._speakerVariableId` / `_speakerVariableScope` | 说话人显示名的变量绑定（表现层） |
| `NarrativeStorySO._customSteps` | 剧情的「自定义节点脚本清单」，按 `ScriptId` 解析的**唯一权威** |

为什么只存字符串 ID：`NovelCommand` 在 SO→运行时定义、二级步骤包装、节点深拷贝时都走
`JsonUtility` 往返，而 `JsonUtility` 不支持 `[SerializeReference]` 多态，也不适合承载
`UnityEngine.Object` 引用。把实例放进指令会在这些路径上静默丢字段。

## 3. `NovelCustomStepSO` 钩子

| 成员 | 默认 | 说明 |
|---|---|---|
| `ScriptId` | 类型全名 | 稳定 ID；重命名类型必须显式覆写并保留旧值 |
| `DisplayName` | 资产名 | 下拉与图上显示 |
| `WaitForCompletion` | `true` | `false` = 发射后不管 |
| `IncludeInFingerprint` | `true` | 参数是否进剧情指纹 |
| `Validate(NovelCustomStepValidation)` | `null` | 剧情校验期静态校验；**此时业务 Module 可能还没激活**，不要在这里判断服务可用性 |
| `Summary()` | `DisplayName` | 步骤列表摘要；只读自己的参数 |
| `OnBegin(context)` | `context.Complete()` | 启动 |
| `OnTick(context, delta)` | 空 | 逐帧，**只在会话未暂停时**被调用 |
| `OnEnd(context)` | 空 | `Complete`/`Fail` 之后各一次 |
| `OnCancel(context)` | 空 | 读档 / 退出 / 故障 |

`NovelCustomStepValidation` 只提供 `ChapterId` / `NodeId` / `CommandId` 与
`TryGetVariable(scope, id, out value)`（查询已声明变量）。

## 4. `NovelCustomStepContext`

| 成员 | 说明 |
|---|---|
| `Command` | 本步骤参数（只读） |
| `ExecutionId` | 本次执行唯一标识；跨模块请求 / 结果配对时原样带回 |
| `ChapterId` / `NodeId` | 稳定执行位置 |
| `State` | **本次执行私有的状态槽**；脚本资产共享，运行状态只能放这里 |
| `IsAlive` | 会话是否仍有效；业务模块的迟到回调必须先检查 |
| `Complete()` / `Fail(msg)` | 各只生效一次 |
| `SetWait(reason)` | 追加等待原因（`NarrativeWait`） |
| `SetVariable(scope, id, value[, out error])` | 写剧情变量；未声明 / 类型不符 / 作用域不对会失败并给原因 |
| `TryGetVariable(scope, id, out value)` | 读剧情变量 |
| `AcquirePause()` | 暂停凭据；会同时停掉本节点的 `OnTick` |

脚本**拿不到** `NarrativeRunner`，不能改变执行位置。

## 5. 中性契约 `INovelStepService`

```csharp
void Invoke(string requestKey, string executionId, string payload,
            Action<string> complete, Action<string> fail);
void Abort(string executionId);
```

业务 Module 用 `EmberServiceLocator.Register<INovelStepService>(this)` 注册。
桥接节点用 `EmberServiceLocator.TryResolve<INovelStepService>()` 取用——
**用 `TryResolve` 而不是事件总线**：事件总线在没人订阅时是静默的，剧情会永久卡在等待里；
`TryResolve` 返回 null 可以当场 `Fail`。

桥接节点字段：请求键、参数（自由文本）、结果变量 + 作用域、**超时秒数（必须 > 0）**。
结果按目标变量声明类型转换：`String` 原样、`Int` 解析整数、`Bool` 接受 `true/false/1/0`。

## 6. 两套指纹规则（不要混）

| 数据 | 进剧情指纹？ | 原因 |
|---|---|---|
| 自定义节点 `ScriptId` + **脚本资产内容** | **进** | 改变执行语义；改脚本参数会让旧档判为不兼容 |
| 说话人变量绑定 | **不进** | 与既有 `_speakerNameKey` 同口径：纯表现层，不改对白角色键与强调匹配 |

推论：**没有自定义节点的既有故事，指纹逐字节不变**；`NovelCheckpoint.CurrentSchemaVersion`
不会因为说话人变量而变化（它是可空字段，旧档读回为空即走原回退链）。

## 7. 运行期契约（漏一条就会出问题）

| # | 规则 |
|---|---|
| 1 | **既不 `Complete` 也不 `Fail` = 剧情永久卡住。** 运行器不会自己超时；桥接节点强制要求超时 |
| 2 | **关键推进不要放 `OnTick`。** 会话被暂停（`AcquirePause`、菜单 / 历史 / 存档页）后逐帧 Tick 提前返回，`OnTick` 不再被调用；持续推进交给业务 Module 的 `IEmberUpdate` |
| 3 | **运行状态放 `context.State`**，不要放脚本字段（资产共享） |
| 4 | **`OnCancel` 必须收干净**：退订事件、释放暂停、通知业务模块中止（`EmberEventGroup` 可一键退订） |
| 5 | 同一 `executionId` 最终恰好完成或失败一次；重复回调会被忽略 |
| 6 | 外部等待期间**不能存档**（会话处于演出等待，不是稳定点）——这天然避免「读到小游戏进行一半的存档」 |
| 7 | 读档时旧会话被 `Dispose`，`OnCancel` 一定被调用一次 |
| 8 | 脚本抛异常不会带崩剧情：`OnBegin` 异常记为故障，`OnTick`/`OnEnd`/`OnCancel` 异常只记警告 |

## 8. 校验与诊断

| 现象 | 原因 |
|---|---|
| 「自定义节点脚本未在本剧情登记」 | 脚本资产没加进剧情清单，或 `ScriptId` 不一致 |
| 「自定义节点必须在剧情会话中运行」 | 用独立章节启动（`NarrativeRunner.Start`）；独立 Start 没有脚本清单 |
| 「自定义节点必须选择脚本」 | 步骤的 `_customStepId` 为空 |
| 「BridgeStep…请求键」 | 桥接节点没填请求键 |
| 「超时必须大于 0」 | 桥接节点超时 ≤ 0 |
| 「结果变量未声明」/「类型不符」 | 结果变量没声明或类型不可转换 |
| 「说话人变量必须是已声明的字符串变量」 | `BadSpeakerVariable` |
| 剧情停在自定义节点不动 | 脚本没 `Complete`/`Fail`，或业务模块没返回 |

**未登记的脚本会在剧情校验阶段就报错**，不会等到运行到那一步才炸——
所以实施后必须跑一次剧情校验，并把结果写进交付报告。

## 9. 编辑器入口

1. 流程编辑器 → **章节总览** → 「自定义节点脚本清单」→ 加入脚本资产。
2. 对话段内容页 → 步骤类型选「自定义节点」→ 下拉选脚本；
   或「插入二级步骤 / 自定义步骤…」走预设。
3. 点击「校验剧情」，确认上面的错误码都不出现。
4. 说话人变量：对白步骤的「说话人变量 ID」+ 作用域；留空则退回称呼 Key / 角色名。

运行期只按剧情清单解析脚本，**不做全项目反射扫描**，因此 IL2CPP 裁剪不会裁掉脚本，
也不会出现「运行时找不到脚本」；反射只用在编辑期下拉列表里。

# 生成器调用契约

通过 Unity MCP `execute_code` 在已确认的消费项目中调用以下 Editor API。
代码来自 `Assets/Game/Module/Narrative/Editor/NovelTestSaveAutomation.cs`，窗口与 Skill 使用同一 `NovelTestSaveBuilder`。
不需要写入临时 C# 文件。

## 只读查询

```csharp
return Game.Narrative.Editor.NovelTestSaveAutomation.Search("用户给出的节点关键词");
```

返回 JSON：`Nodes[]` 包含 `StoryAssetPath/StoryName/ChapterId/ChapterName/NodeId/NodeName`，以及 `Truncated`。

```csharp
return Game.Narrative.Editor.NovelTestSaveAutomation.Inspect(storyAssetPath, chapterId, nodeId);
```

用真实结果替换参数。返回 `Candidates[]`：

- `ChapterId/NodeId/Name`：实际普通节点。
- `Distance`：0 表示直接目标，大于 0 表示回退；排序不是替用户选择路线的授权。
- `Lines[]`：可停留的 `CommandId/Text`，选择节点为空。
- `CallChains[]`：每项 `CallIds[]` 是从外到内的真实调用链；主线为单个空数组。空集合表示没有合法主线调用入口。
- `DefaultsJson`：首条调用链的默认检查点，提供全局、章节及各层流程变量的 ID、类型和默认值供查看。
  更换调用链时 `Generate` 重新获取该链的默认值；不能把 `DefaultsJson` 当作手工写档模板。

## 生成

推荐构造请求对象并由 JsonUtility 序列化，避免字符串转义出错：

```csharp
var request = new Game.Narrative.Editor.NovelTestSaveAutomation.Request
{
    StoryAssetPath = "从 Search 获取的 Assets 路径",
    ChapterId = "所选目标章节 ID",
    NodeId = "所选目标节点 ID",
    TargetChapterId = "从 Inspect 选择的实际章节 ID",
    TargetNodeId = "从 Inspect 选择的普通节点 ID",
    Slot = -1,
    OverwriteExisting = false
};
return Game.Narrative.Editor.NovelTestSaveAutomation.Generate(UnityEngine.JsonUtility.ToJson(request));
```

可选字段：

| 字段 | 语义 |
|---|---|
| `TargetChapterId/TargetNodeId` | 唯一候选时可省略，多候选必须填写 |
| `CallIds` | `string[]`，多调用链时必须填写；主线传空数组；不修改返回地址 |
| `CommandId` | 停留台词，省略时普通目标第一句、回退目标最后一句 |
| `OutputRoot` | 存档目录绝对路径，目录中将生成 index.json 和 slot payload；省略为当前游戏 persistentDataPath/VisualNovelSaves |
| `Slot` | -1 自动选空闲手动槽；0–5 指定槽；不写快速槽和自动槽 |
| `OverwriteExisting` | 默认 false，已有槽拒绝覆盖 |
| `Variables` | `List<VariableOverride>`，只接受已声明变量 |

变量覆盖示例（必须先确认项目中存在这些变量）：

```csharp
request.Variables.Add(new Game.Narrative.Editor.NovelTestSaveAutomation.VariableOverride
{
    Scope = "Global", Id = "playerName", String = "测试玩家"
});
request.Variables.Add(new Game.Narrative.Editor.NovelTestSaveAutomation.VariableOverride
{
    Scope = "Chapter", Id = "score", Int = 10
});
```

`Scope` 严格为 `Global/Chapter/Flow`。值按实际声明类型填写 `String/Int/Bool` 对应字段。
`Flow` 默认修改最内层；覆盖外层流程变量时补上该层的真实 `CallId`。

返回 JSON：`Root/IndexPath/PayloadPath/ChapterId/NodeId/CommandId/Slot/FellBack`。
成功返回前已通过正式恢复校验，并写入后重新读取 payload、核验摘要和再次校验恢复。
这不会启动小游戏；读档后由正式 NovelSession 执行它。

## 常见阻断

- 剧情/脚本资产未保存：让用户保存后重读，不悄悄保存整个工程。
- 没有前置普通节点：建议添加，不改图、不在特殊步骤内部伪造存档。
- 变量使选择节点没有合法选项：报告条件，按用户给的测试场景调整变量。
- 调用链不唯一：显示调用点让用户选择，不能选择任意返回地址。
- 实机剧情语义与生成时不同：使用匹配版本或重新生成，不关闭指纹校验。
- 工具缺失：报告所需模板业务版本；单独安装 Skill 不能补上业务 API。

# FSM 蓝图能力与模板隔离

唯一菜单为 `Ember/FSM/蓝图观察器`，由项目中心正式扩展 `ember.fsm.blueprint` 的有效启用状态开放。base 默认组合不启用；消费项目可以明确选择通用蓝图。模板专属参数与观察源继续按正式模板和业务模块条件隔离。完整组合和迁移见[可组合扩展](composable-extensions.md)（本地未发布）。不得恢复 `Tools/Ember/FSM`、无条件 MenuItem 或双菜单。不要用图资产、场景、残留脚本或模板快照是否存在推断模板身份。

## 模板如何接入

业务接线放在所属模块的 Editor 程序集中。复用 `Ember.Core.Editor` 的登记 API，明确传入所有者和模块条件：

```csharp
// 在所属模板的 InitializeOnLoad 接线中调用；以下业务方法由该模块实现。
EmberFsmBlueprintParameters.Register(
    "my-module.parameters", DrawParameters, DrawToolbar,
    ownerTemplateId: "my-template", enabled: () => ModuleEditorAccess.Available);
EmberFsmObservationSources.Register(
    "my-module.runtime", ReadInstances,
    ownerTemplateId: "my-template", enabled: () => ModuleEditorAccess.Available);
```

模板在活动 Assets 的 `EmberExtensionDefaults.json` 声明默认组合，消费项目在项目中心保存自己的组合。参数/观察源登记不再授予通用蓝图开关；`EmberFsmBlueprintAccess.Register(ownerTemplateId, enabled)` 保留旧调用兼容，不作为新开关使用。提供者仍用同一个 key 替换登记，通过各自 `Remove(key)` 移除；提供者条件同时校验正式扩展、所属模板与 enabled，不得依赖自身登记结果递归判断。

模板判断使用 `EmberProjectSetup.IsTemplateActive`：开发端取正式编辑模板，消费端取正式活动部署模板，并支持合法父链。缺失、无效和无关身份拒绝。包内不引用 Player 等模板业务类型，也不硬编码平台玩家模块配置。

独立消费项目的自有提供者可明确传 `ownerTemplateId: ""`，表示项目范围，仍须声明 enabled 模块条件并启用正式扩展。省略/null 仍采用旧兼容语义捕获正式模板，不会自动获得项目范围；模板专属提供者必须继续填写真实模板 ID。

既有 `Register` 调用仍可编译：省略 owner 时，在登记时捕获当前正式模板 ID，而不是读取回调时的模板 ID；无有效身份时不启用。这个兼容入口不能表达模块禁用条件，也不能识别被错误保留在无关模板中的旧接线。**新增和迁移接线必须显式填写 ownerTemplateId 与 enabled。** 未加载模板的旧接线应在该模板后续正式加载时从 Assets 维护源迁移；禁止为修复它直接编辑 Templates~ 或 ParentSnapshot~。

## 窗口和操作边界

`EmberFsmBlueprintAccess.IsAvailable` 控制动态菜单和公开 Open。不可用时 Open 抛出 `InvalidOperationException`，调用方应先检查并提供不可用说明。不能绕过 Open 直接创建窗口作为业务入口。

窗口保存打开时的精确活动模板 ID。模板身份变化或正式扩展关闭后，旧窗口禁用内容并停止观察；SetGraph、布局预览/应用/保存、导航和参数设置再次检查 `IsTemplateAvailable`。原图引用、参数预设和已保存本机布局保留。业务模块单独关闭时只撤销其参数/观察回调，通用静态蓝图仍可使用。切到另一个合法模板后再 Open 会使用新窗口，避免把旧窗口内容改成另一模板内容。

此规则不改变 FSM 运行时、图资产格式、绑定或模板分发。观察器布局仍为项目路径和图 GUID 定位的本机 EditorPrefs，不是模板资源。没有复制图、保存业务配置或启动玩家试跑的隐式行为。

## 后续 AI 必须完成的检查

1. 先声明实现归属、模板所有者和模块启用条件，再选择菜单路径；公共实现不得豁免入口隔离。
2. 同时检查菜单可见性、公开 API、Inspector/快捷入口、参数扩展、观察源以及已打开窗口，不能只写 MenuItem 校验让它置灰。
3. 用目标工程 Unity MCP 编译并确认本轮域重载完成，再读 Console。分别检查实际菜单的新增/移除，以及无权限时操作被拒绝。
4. 覆盖所属/派生/无关/无效/消费部署身份、模块关闭和旧窗口。身份单元测试与实际模板切换是不同证据；未实际切换就标明未验证，不覆盖用户草稿补验。

2026-10-26 的 base 隐藏结果为旧能力登记机制的历史记录，不能替代本轮组合配置验证。本轮测试与限制见[迁移说明](composable-extensions.md#验证与回退)。未实际切换其他模板或升级消费项目；旧模板接线的显式 owner/module 迁移仍按上文边界处理。

# UI 中心接口核对

核对基线：框架 0.13.0（v0.13.0）。安装目录声明需要 `eui-regenerate-v1` 能力；仅有 0.12.5～0.12.11 的版本号不足以证明公开 API 已存在。路径相对实际解析的包目录；版本变化时阅读源码确认。

| 位置 | 用途 |
|---|---|
| UIExtension/Editor/Pages/EUICreationService.cs | EUICreationRequest、TryBuildPlan、Create；Item/Page 标准骨架和首次生成 |
| UIExtension/Editor/Pages/EUIBindingEditorUtility.cs | GetBindingSnapshot、SetBindings、CollectBindings、ValidateBinding |
| UIExtension/Editor/EUIBindingCodeGenUtility.cs | UI 中心使用的统一生成流程，保留用户骨架、重新生成 Binding |
| UIExtension/Editor/Settings/CSharpLogicImplementationData.cs | 路径计算、模板、PageDef 对侧 partial 冲突检查 |
| UIExtension/Runtime/EUIBinding.cs | WidgetTypes、BindingEntry、CodePathMode |
| UIExtension/Runtime/EUIItemFactory.cs | TryCreate：不实例化、不销毁 GameObject，所有权仍属于宿主 |

新建 Item 示例：

```csharp
var request = new EUICreationRequest {
    PrefabName = "EUIInventorySlotItem",
    ClassName = "EUIInventorySlotItem",
    ClassPath = "Module/Inventory",
    Role = EUIBindingRole.Item,
    CodePathMode = EUIBinding.CodePathMode.Business,
    GenerateCustomSettings = false
};
if (!EUICreationService.TryBuildPlan(request, out var plan, out var preflight))
    throw new InvalidOperationException(preflight.Error);
var result = EUICreationService.Create(request);
if (!result.Success) throw new InvalidOperationException(result.Error);
```

Create 不自动 Refresh；调用方完成一批修改后统一刷新。检查 `RequiresRefresh`、`WaitingForCompilation` 只能判断请求状态，不能当成编译通过。

对已保存项目 Prefab 的重新生成使用公开入口：

```csharp
public static bool TryRegenerateCode(EUIBinding binding, out string error)
```

调用前使用 `AssetDatabase.LoadAssetAtPath<GameObject>` 读取刚保存的 Prefab，再取根 EUIBinding；临时场景对象或未保存的 Prefab Contents 不是这个入口的输入。入口不弹窗、不创建 Prefab、不主动 Refresh，内部仍经过原生成器的模式、配置、路径和包内资产检查。成功后由调用方统一刷新，不等于编译通过。适配器直接调用该公开 API，不再反射 internal 方法。

UnityFarm 早期部署的适配器通过反射调用五参数 `TryGenerateCode`。框架保留此内部入口兼容旧调用，但不建议新项目继续复制旧适配器。更新已有适配器时同时检查 `CheckGenerator` 等旧调用方；仅更新技能文件夹不会自动修改项目 Assets。

绑定路径相对当前 Item 根；根自身用空字符串。`SetBindings` 整体替换条目，未包含 `IsFramework`，只用于本次新建 Item。对既有 Page 使用 SerializedObject 精确追加授权条目，保留其原有模式和受保护条目。

绑定字段私有是正常的：用户 partial 和生成类合并后可以访问。读取真实生成结果后再实现业务，不凭计划提前写字段。

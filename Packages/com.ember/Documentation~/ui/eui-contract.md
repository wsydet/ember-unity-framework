# EUI 契约、页面命名与嵌套所有权

正式入口：`Ember/UI/UI 开发中心 → UI 总览`，选择资产后使用“导出只读 EUI 契约”或“与已导出契约比较”。不会修改 Prefab 或运行代码生成。批处理使用 `Ember.UIExtension.Editor.EUIContractService`。

## 读取与比较

```csharp
var current = EUIContractService.Export(prefabPath);
EUIContractService.Write(outputPath, current);
var differences = EUIContractService.Compare(previous, current);
bool blocked = differences.Any(d => d.blocksApply);
```

`EUIContract` schemaVersion=1，toolVersion=1.1，UTF-8 JSON。GUID 和 local file ID 合成 `guid:localId` 字符串。`nodes`、`bindings`、`properties` 和 `resources` 按稳定键排序；`exportedUtc` 为非业务时间字段。`assetDigest` 是 Prefab SHA256，`dependencyDigest` 是 Unity 依赖摘要，生成文件和 PageDef 有独立路径/摘要。摘要用于输入校验，不代替语义差异分类。

- `role/pageType/codeMode/className/pageName/baseBindingGuid/variantSource` 来自真实 Binding 和 Prefab。
- `nodes` 保存对象身份、层级、sibling、activeSelf、layer、实际组件类型、嵌套源及 IEUIExposedChildProvider 槽位。
- `bindings` 保存绑定名、声明和实际完整类型、对象定位、所属 Binding、IsFramework 和所有权。
- `properties` 保存序列化字段路径、实际值及分类，覆盖 RectTransform、Canvas、TMP 字体/文本、状态、事件持久引用等。对象引用以稳定身份记录；`resources` 补资产路径、实际类型、Sprite border/尺寸及 Texture 尺寸，`dependencies` 覆盖间接资源。
- `generatedFields` 来自当前 `.Binding.cs` 的字段声明；运行时 Component 仍走 ControlMap，嵌套逻辑走命名子逻辑表。生成文件不是本工具的写入目标。
- 自定义 Settings 仅提取 Unity 可序列化字段，Odin 专有部分显式提示需要适配。无法表达的序列化字段、外部 Canvas 参考分辨率、动态资源/本地化/事件提供方进入 `unresolved`。S2 公共库信息由实际依赖的 EUILibrary 条目导出为 libraryEntries，包含 family、皮肤版本、允许覆盖和稳定槽位；未登记资源不虚构默认值。详见 [公共库](prefab-library.md)。

差异分类：appearance、resource、layout、state、hierarchy、hierarchy-reference、component-structure、binding-add/remove/rename/type、binding-config、ownership、asset-identity/path、object-identity、validation。外观/资源差异仍须视觉核验；其余默认 `blocksApply=true`。这是只读分析规则，S1 不提供自动替换接口。删除绑定、类型变化、身份变化、空引用、重复绑定/语义、不可构造 UILogic、未知关键类型和多个父级所有者必须先解决。UIContainer/TabLoader 仍未启用，不接受 Transform fallback 作为有效类型证明。

## 一次登记语义

`EUIContractSupplement` 只保存 prefabGuid、legacyNames、annotations。每条 annotation 使用 ownerId + bindingName 定位，附 semanticId 和带证据的 behavior。通过 `WriteSupplement(supplement)` 显式登记到 `Assets/Ember/Editor/UIContracts/<guid>.json`；以后 `Export(path)` 自动组合它与资产事实。导出从不自动登记。重建对象后必须更新定位；失效映射和重复 ID 为阻断项。

可给 `Export(path, annotations, legacyNames)` 显式传入本次补充；这不落盘。语义补充是唯一人工维护来源，不复制整份结构 JSON。

## 页面命名与生成

Page（包括 Popup/FullScreenPopup）的资产、根名、PrefabName/PageName 和类名对应同一个 Page 名称；弹窗种类由 PageType 表达。`EUICreationRequest` 默认 NewPage，旧 Panel 后缀请求会预检失败并提示改正。已有特殊名称 API 仍可读取；内部 Panel 容器不迁移。使用 AssetDatabase.MoveAsset 保留 GUID，再更新 Binding 并调用 `EUIBindingCodeGenUtility.TryRegenerateCode`。生成器同步生成头的 prefab name，保留用户代码和受管生命周期内容。

`GetBindingSnapshot/ApplyBindingSnapshot` 补齐代码模式、过渡/遮罩配置和条目 IsFramework/HasGameObject；新增可空配置字段保持旧调用未提供值时的兼容性。HasGameObject=false 保留缺失引用，不会把它变成根节点。快照用于绑定编辑，Odin 专有 Settings 实例不在其重建范围。

## 一个实例一个逻辑所有者

固定子 Item 以 UILogic 绑定：Bridge 按绑定名注册，生成代码调用 `GetChildLogic<T>(name)`，父逻辑负责生命周期。动态 Item 绑定宿主/RectTransform，宿主使用 `EUIItemFactory.TryCreate`，管理 BeginUse/Show/Hide/ResetForPool，并最终 Dispose。

`EUIInstanceOwnership` 在主线程占用实例：重复包装在逻辑构造前拒绝；初始化异常和 Dispose 释放令牌。固定子逻辑令牌随父级释放。Dispose 不销毁 GameObject，实例/资源仍归宿主；动态容器、实例缓存与借用取消见 [动态列表](dynamic-lists.md)。

base 可生成示例：`UI/Module/UIFoundation/Prefabs/EUIOwnershipExamplePage.prefab` 与 `EUIOwnershipLeafItem.prefab`。示例页包含同一叶子 Prefab 的两个不同实例，分别为 StaticItem/UILogic 和 DynamicItem/Extension RectTransform；通过 EUIManager.ShowFreePage(GamePages.EUIOwnershipExamplePage) 查看，不进入默认导航。用户逻辑的实例计数可检查 Init/Open/Dispose。

相关测试：EUIContractEditTests、EUIDevelopmentCenterEditTests；Loading 命名回归使用 EUITransitionBlockPreviewTests。具体本次运行结果保存在工程开发交接，不将测试存在视为已通过。



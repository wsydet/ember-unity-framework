# EmberUIBinding Odin 面板功能恢复计划

> 已放弃自定义 `EmberUIBindingEditor : OdinEditor`，改用纯 Unity 原生 Inspector + Odin 属性渲染。
> 本文档列出丢失的功能及恢复方案。

---

## 功能丢失总览

| # | 分类 | 功能 | 程度 | 原因 |
|---|------|------|------|------|
| 1 | 模板 | 加载模板（ObjectPicker 选 `EmberUIBindingTemplate`） | 🔴 全丢 | 需要 `EditorGUIUtility.ShowObjectPicker` |
| 2 | 模板 | 保存为模板（`SaveFilePanel`） | 🔴 全丢 | 需要 `AssetDatabase.CreateAsset` |
| 3 | 模板 | 复制/粘贴模板（内存剪贴板） | 🔴 全丢 | 需要 Editor 端静态状态 |
| 4 | 继承 | 基类 Prefab 选择器（ObjectField → GUID 自动转换） | 🔴 全丢 | 改为纯字符串字段，手动粘贴 GUID |
| 5 | 继承 | 基类信息只读展示（isPage / pageName / classPath） | 🔴 全丢 | 需要 `AssetDatabase` 查 Prefab |
| 6 | 继承 | 缺失字段自动检测 + "自动修复"按钮 | 🔴 全丢 | 需要比对基类的 Bindings |
| 7 | 继承 | Page/非 Page 继承冲突校验 | 🔴 全丢 | 跨对象校验 |
| 8 | 代码生成 | 逻辑实现下拉框（选择 `LogicImplementationData`） | 🔴 全丢 | 需要 `UIBindingSettingData` |
| 9 | 代码生成 | 代码生成路径预览 + 可点击跳转 | 🔴 全丢 | 需要 `AssetDatabase` |
| 10 | 代码生成 | "生成代码" / "重新生成"按钮 | 🔴 全丢 | 需要 `logic.GenerateCode()` |
| 11 | 代码生成 | NoCodeGen 模式（"生成到剪贴板"） | 🔴 全丢 | 同上 |
| 12 | 代码生成 | 自动收集子控件按钮 | 🔴 全丢 | 需要遍历 Transform + 类型检测 |
| 13 | 代码生成 | 设置齿轮按钮（跳转 Project Settings） | 🔴 全丢 | 需要 `SettingsService` |
| 14 | 自身控件 | 上下文类型下拉（仅显示 GO 上存在的组件类型） | 🟡 减配 | Odin 渲染为标准 enum 下拉（全部选项） |
| 15 | 自身控件 | 控件类型实时验证 + "自动识别类型"按钮 | 🟡 减配 | `[ValidateInput]` 仅显示错误，无自动修复 |
| 16 | 搜索 | 按名称搜索绑定条目 | 🔴 全丢 | 纯 UI 层过滤，Odin 不支持 |
| 17 | 搜索 | 按节点搜索绑定条目 | 🔴 全丢 | 同上 |
| 18 | 绑定列表 | 拖入 GameObject 自动检测组件类型 | 🔴 全丢 | 需要 `AutoSelectByObject` |
| 19 | 绑定列表 | 拖入 GameObject 自动生成变量名 | 🔴 全丢 | 需要 `logic.GetNameForCode()` |
| 20 | 绑定列表 | GameObject 层级验证（必须为子节点） | 🔴 全丢 | 无自定义回调 |
| 21 | 绑定列表 | 重复绑定检测（跨递归子 UIBinding） | 🔴 全丢 | 需要 `GatherBindingDefinitions` |
| 22 | 绑定列表 | 继承条目锁定（只读不可编辑） | 🔴 全丢 | 需要 `baseTypeFields` 状态 |
| 23 | 绑定列表 | 逐条目删除/刷新按钮 | 🔴 全丢 | Odin 用默认 +/- 按钮 |
| 24 | 绑定列表 | "添加绑定"按钮（独立于计数） | 🔴 全丢 | Odin 用默认 +/- 按钮 |

> 🔴 全丢 20 项，🟡 减配 2 项，✅ 保留 2 项（页面设置条件显示、字段验证）

---

## 恢复策略

核心思路：**不重新引入 CustomEditor**。通过以下 Odin 原生机制恢复功能：

| 机制 | 用途 | 示例 |
|------|------|------|
| `[Button]` + `#if UNITY_EDITOR` | 在 Inspector 中添加按钮 | 生成代码、收集子控件 |
| `[OnInspectorGUI]` | 在 Odin 属性树中插入自定义 GUI | 继承 Prefab 选择器、搜索 |
| `[ValueDropdown]` | 动态下拉选项 | 上下文组件类型 |
| `[OnValueChanged]` | 字段变化回调 | 自动检测类型、自动命名 |
| `[ValidateInput]` | 字段级验证 | 类型匹配校验 |
| `[ShowIf]` / `[HideIf]` / `[DisableIf]` | 条件显示/禁用 | 继承条目锁定 |
| `[InfoBox]` + `VisibleIf` | 条件提示 | 缺失字段警告 |
| `[CustomContextMenu]` | 右键菜单 | 模板加载/保存 |
| `[MenuItem]` | 顶部菜单 | 全局代码生成入口 |

---

## 一、模板操作（4 项）

### 1.1 加载模板

**原始行为**：点击按钮 → ObjectPicker 选择 `EmberUIBindingTemplate` → 弹确认框 → 应用模板数据覆盖当前 binding。

**恢复方案**：在 `EmberUIBinding` 中添加 `#if UNITY_EDITOR` 块，使用 `[Button]` + `EditorGUIUtility.ShowObjectPicker`。

```csharp
#if UNITY_EDITOR
[FoldoutGroup("$GROUP")]
[BoxGroup("$GROUP/模板", ShowLabel = false)]
[Title("模板")]
[Button("加载模板", ButtonSizes.Medium), GUIColor(0.4f, 0.6f, 0.9f)]
private void LoadTemplate()
{
    // 需要 static int templateSelector 跟踪 ObjectPicker 控制 ID
    // 需要在 OnInspectorGUI 中处理 ObjectSelectorClosed 事件
    // → 改用 [OnInspectorGUI] + ObjectPicker
}
#endif
```

**复杂度**：中。ObjectPicker 需要回调处理，纯 `[Button]` 不够，需要配合 `[OnInspectorGUI]`。

---

### 1.2 保存为模板

**原始行为**：点击按钮 → SaveFilePanel 选择路径 → 创建 `EmberUIBindingTemplate` ScriptableObject → 保存。

**恢复方案**：`[Button]` + `AssetDatabase.CreateAsset`。

```csharp
#if UNITY_EDITOR
[Button("保存为模板", ButtonSizes.Medium), GUIColor(0.3f, 0.7f, 0.3f)]
private void SaveAsTemplate()
{
    var path = EditorUtility.SaveFilePanel("保存模板", "Assets", className, "asset");
    if (string.IsNullOrEmpty(path)) return;
    path = "Assets" + path.Replace(Application.dataPath, "");
    var template = ScriptableObject.CreateInstance<EmberUIBindingTemplate>();
    template.CopyFromUIBinding(this);
    AssetDatabase.CreateAsset(template, path);
    AssetDatabase.SaveAssets();
}
#endif
```

**复杂度**：低。纯 `[Button]` 即可。

---

### 1.3 复制/粘贴模板

**原始行为**：复制 → 内存保存模板快照。粘贴 → 恢复快照（静态变量 `savedTemplate`）。

**恢复方案**：`[Button]` + static 字段。

```csharp
#if UNITY_EDITOR
private static EmberUIBindingTemplate _savedTemplate;

[Button("复制", ButtonSizes.Medium)]
private void CopyTemplate()
{
    if (_savedTemplate) DestroyImmediate(_savedTemplate);
    _savedTemplate = ScriptableObject.CreateInstance<EmberUIBindingTemplate>();
    _savedTemplate.CopyFromUIBinding(this);
}

[Button("粘贴", ButtonSizes.Medium)]
[EnableIf("@_savedTemplate != null")]
private void PasteTemplate()
{
    // ApplyBindingTemplate 逻辑内联到此处
    noCodeGen = _savedTemplate.NoCodeGeneration;
    isPage = _savedTemplate.IsPage;
    // ... 逐字段恢复
}
#endif
```

**复杂度**：低。`[Button]` + `[EnableIf]`。

---

## 二、继承管理（4 项）

### 2.1 基类 Prefab 选择器

**原始行为**：ObjectField 拖入 Prefab → 自动解析 GUID → 保存 `baseBindingUUID`。同时显示基类的只读信息。

**恢复方案**：`[OnInspectorGUI]` 在 `baseBindingUUID` 上方绘制 ObjectField，拖入后自动填 GUID。

```csharp
#if UNITY_EDITOR
[OnInspectorGUI("DrawBasePrefabPicker", append: false)]
[FoldoutGroup("$GROUP")]
[BoxGroup("$GROUP/继承")]
[SerializeField, LabelText("基类 Prefab GUID")]
private string baseBindingUUID;

private GameObject _basePrefab; // 不序列化，仅用于 Inspector 显示

private void DrawBasePrefabPicker()
{
    _basePrefab = EditorGUILayout.ObjectField("基类 Prefab", _basePrefab, typeof(GameObject), false) as GameObject;
    if (_basePrefab)
    {
        var path = AssetDatabase.GetAssetPath(_basePrefab);
        baseBindingUUID = AssetDatabase.AssetPathToGUID(path);
    }
}
#endif
```

**复杂度**：中。`[OnInspectorGUI]` 配合 `AssetDatabase`。

---

### 2.2 基类信息只读展示

**原始行为**：选择基类 Prefab 后，只读显示其 `isPage`、`pageName`、`classPath`。

**恢复方案**：在 `DrawBasePrefabPicker` 中一并处理。

```csharp
private void DrawBasePrefabPicker()
{
    var newPrefab = EditorGUILayout.ObjectField("基类 Prefab", _basePrefab, typeof(GameObject), false) as GameObject;
    if (newPrefab != _basePrefab) { _basePrefab = newPrefab; /* 更新 GUID */ }
    
    if (_basePrefab)
    {
        var baseBinding = _basePrefab.GetComponent<EmberUIBinding>();
        if (baseBinding)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Toggle("是否为 Page", baseBinding.IsPage);
            if (baseBinding.IsPage)
                EditorGUILayout.TextField("页面名", baseBinding.PageName);
            EditorGUILayout.TextField("基类路径名", baseBinding.ClassPath);
            EditorGUI.EndDisabledGroup();
        }
    }
}
```

**复杂度**：低。在 `[OnInspectorGUI]` 方法内完成。

---

### 2.3 缺失字段检测 + 自动修复

**原始行为**：比对基类 Bindings 与当前 Bindings，列出缺失条目，点击按钮自动添加。

**恢复方案**：`[Button]` + `[InfoBox]`（条件显示缺失数量）。

```csharp
#if UNITY_EDITOR
[Button("自动添加缺失的绑定"), GUIColor(0.9f, 0.6f, 0.2f)]
[ShowIf("@HasMissingFields()")]
private void AutoFixMissingBindings()
{
    // 比对基类 Bindings，添加缺失条目
}

private bool HasMissingFields()
{
    if (_basePrefab == null) return false;
    var baseBinding = _basePrefab.GetComponent<EmberUIBinding>();
    if (baseBinding?.Bindings == null) return false;
    var currentNames = new HashSet<string>(bindings.Select(b => b.Name));
    return baseBinding.Bindings.Any(b => !string.IsNullOrEmpty(b.Name) && !currentNames.Contains(b.Name));
}
#endif
```

**复杂度**：中。需要基类比对逻辑。

---

### 2.4 Page/非 Page 继承冲突校验

**原始行为**：跨对象检查基类 `isPage` 与当前 `isPage` 是否一致。

**恢复方案**：`[ValidateInput]` 或 `[InfoBox]`。

```csharp
[InfoBox("Page 和非 Page 对象无法相互继承。", 
    VisibleIf = "@CheckInheritanceMismatch()",
    InfoMessageType = InfoMessageType.Error)]
```

**复杂度**：低。`[InfoBox]` + 表达式。

---

## 三、代码生成（6 项）

### 3.1 逻辑实现下拉框

**原始行为**：从 `UIBindingSettingData.LogicImplementations` 中选择当前使用的实现。

**恢复方案**：`[ValueDropdown]` 从 Settings 拉取选项。

```csharp
#if UNITY_EDITOR
private static int _logicIndex;

[FoldoutGroup("$GROUP")]
[BoxGroup("$GROUP/代码生成", ShowLabel = false)]
[Title("代码生成")]
[ShowInInspector, ValueDropdown("GetLogicImplementationNames"), LabelText("逻辑实现")]
private int LogicImplementationIndex
{
    get => _logicIndex;
    set => _logicIndex = value;
}

private static IEnumerable<ValueDropdownItem<int>> GetLogicImplementationNames()
{
    var settings = UIBindingSettingData.GetOrCreateSettings();
    if (settings.LogicImplementations == null) yield break;
    for (int i = 0; i < settings.LogicImplementations.Length; i++)
        yield return new ValueDropdownItem<int>(settings.LogicImplementations[i].name, i);
}
#endif
```

**复杂度**：低。`[ShowInInspector]` + `[ValueDropdown]`。

---

### 3.2 代码生成路径预览

**原始行为**：显示生成路径字符串 + 可点击的 MonoScript 对象字段。

**恢复方案**：`[ShowInInspector, ReadOnly]` 属性计算路径。

```csharp
#if UNITY_EDITOR
[ShowInInspector, ReadOnly, LabelText("生成路径")]
private string GeneratedPath
{
    get
    {
        var settings = UIBindingSettingData.GetOrCreateSettings();
        if (settings.LogicImplementations == null || _logicIndex >= settings.LogicImplementations.Length)
            return "—";
        var logic = settings.LogicImplementations[_logicIndex];
        var effectivePath = string.IsNullOrEmpty(codePath) ? logic.GetCodeFilePath("").Replace("/.cs", "") : codePath;
        var relativePath = string.IsNullOrEmpty(classPath) ? className : classPath + "/" + className;
        return effectivePath + "/" + relativePath + logic.CodeFileExtension;
    }
}
#endif
```

**复杂度**：低。计算属性。

---

### 3.3 生成 / 重新生成按钮

**原始行为**：调用 `logic.GenerateCode(binding, baseClsName, declaredFields)` 并刷新资源。

**恢复方案**：`[Button]`。

```csharp
#if UNITY_EDITOR
[Button("生成代码", ButtonSizes.Large), GUIColor(0.2f, 0.7f, 0.2f)]
[EnableIf("@CanGenerateCode()")]
private void GenerateCode()
{
    var settings = UIBindingSettingData.GetOrCreateSettings();
    var logic = settings.LogicImplementations[_logicIndex];
    if (logic.CanGenerate(this))
    {
        EmberUIBinding.BindingEntry[] declared = null;
        string baseCls = null;
        // 如果设置了基类，计算 declared fields
        logic.GenerateCode(this, baseCls, declared);
    }
}

[Button("重新生成", ButtonSizes.Medium)]
[ShowIf("@File.Exists(GeneratedPath)")]
private void RegenerateCode() => GenerateCode();

private bool CanGenerateCode()
{
    var settings = UIBindingSettingData.GetOrCreateSettings();
    if (settings.LogicImplementations == null || _logicIndex >= settings.LogicImplementations.Length)
        return false;
    return settings.LogicImplementations[_logicIndex].CanGenerate(this);
}
#endif
```

**复杂度**：中。按钮逻辑 + 基类字段计算。

---

### 3.4 NoCodeGen 模式（生成到剪贴板）

**原始行为**：`noCodeGen = true` 时隐藏页面设置，底部显示剪贴板生成按钮。

**恢复方案**：`[ShowIf]` 控制按钮可见性 + `GUIUtility.systemCopyBuffer`。

```csharp
#if UNITY_EDITOR
[Button("生成到剪贴板", ButtonSizes.Large)]
[ShowIf("@noCodeGen")]
private void GenerateToClipboard()
{
    var settings = UIBindingSettingData.GetOrCreateSettings();
    var logic = settings.LogicImplementations[_logicIndex];
    logic.GenerateCodeForNoGen(this, className);
}
#endif
```

**复杂度**：低。`[Button]` + `[ShowIf]`。

---

### 3.5 自动收集子控件

**原始行为**：遍历 Transform 子节点，根据组件类型自动填充 Bindings。

**恢复方案**：`[Button]`，内联 `AutoCollect` 逻辑。

```csharp
#if UNITY_EDITOR
[Button("自动收集子控件", ButtonSizes.Medium)]
private void AutoCollectBindings()
{
    var list = new List<BindingEntry>();
    GatherBindings(transform, list, new HashSet<string>());
    bindings = list.ToArray();
}

private void GatherBindings(Transform parent, List<BindingEntry> result, HashSet<string> definedNames)
{
    foreach (Transform child in parent)
    {
        if (child.GetComponent<EmberUIBinding>()) continue; // 子 Binding 边界
        if (!IsNameSuitable(child.name)) continue;
        
        var entry = new BindingEntry
        {
            Name = GetCodeName(child.name, definedNames),
            GameObject = child.gameObject
        };
        AutoDetectType(child.gameObject, ref entry);
        definedNames.Add(entry.Name);
        result.Add(entry);
        GatherBindings(child, result, definedNames);
    }
}
#endif
```

**复杂度**：高。需要内联类型检测（`AutoDetectType`）和命名逻辑。

---

## 四、自身控件增强（2 项）

### 4.1 上下文类型下拉

**原始行为**：下拉仅显示当前 GameObject 上实际存在的组件类型。

**恢复方案**：`[ValueDropdown]` 替换 `[SerializeField]` enum。

```csharp
#if UNITY_EDITOR
private IEnumerable<ValueDropdownItem<WidgetTypes>> GetAvailableWidgetTypes()
{
    yield return new ValueDropdownItem<WidgetTypes>("Component", WidgetTypes.Component);
    foreach (var rule in EmberUIBindingEditor.BuiltInComponentTypeRules)
    {
        if (rule.Matches(gameObject))
            yield return new ValueDropdownItem<WidgetTypes>(rule.WidgetType.ToString(), rule.WidgetType);
    }
    // ... 扩展类型
}
#endif
```

**复杂度**：中。`[ValueDropdown]` + 组件检测。

---

### 4.2 拖入 GameObject 自动检测类型

**原始行为**：在绑定列表中拖入 GameObject 后自动调用 `AutoSelectByObject`。

**恢复方案**：`[OnValueChanged]` 回调。

```csharp
#if UNITY_EDITOR
// 在 BindingEntry 中无法直接使用 [OnValueChanged]（结构体限制）
// → 改用自定义 OdinAttributeDrawer<BindingEntry> 或 PropertyDrawer
#endif
```

**复杂度**：高。`BindingEntry` 是 struct，`[OnValueChanged]` 在 struct 字段上不生效。需要自定义 drawer。

---

## 五、搜索过滤（2 项）

### 5.1 名称搜索 / 节点搜索

**原始行为**：文本框搜索绑定名称 + ObjectField 搜索绑定的 GameObject。

**恢复方案**：`[OnInspectorGUI]` 在绑定列表前插入搜索区域。

```csharp
#if UNITY_EDITOR
private string _searchText;
private GameObject _searchObject;

[OnInspectorGUI("DrawBindingSearch", append: true)]
private void DrawBindingSearch()
{
    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
    {
        _searchText = EditorGUILayout.TextField("按名称搜索", _searchText);
        _searchObject = EditorGUILayout.ObjectField("按节点搜索", _searchObject, typeof(GameObject), true) as GameObject;
        if (GUILayout.Button("清除")) { _searchText = null; _searchObject = null; }
    }
}
#endif
```

**复杂度**：中。但 Odin 的绑定列表不支持客户端过滤 — 搜索结果只能用于高亮/跳转，无法动态隐藏条目。

---

## 六、绑定列表增强（6 项）

### 6.1~6.6 概览

| 子功能 | 复杂度 | 说明 |
|--------|--------|------|
| GO 层级验证 | 高 | 需自定义 `OdinAttributeDrawer<BindingEntry>` |
| 重复绑定检测 | 高 | 跨条目校验，需自定义 drawer |
| 继承条目锁定 | 中 | `[DisableIf]` + 基类字段名集合 |
| 逐条目删除/刷新 | 中 | 自定义 drawer 添加行内按钮 |
| 添加绑定（独立按钮） | 低 | `[Button]` 在数组旁 |
| 类型实时验证 | 中 | `[ValidateInput]` 或自定义 drawer |

推荐方案：编写一个 `BindingEntryDrawer : OdinValueDrawer<BindingEntry[]>`，统一处理以上所有需求。

---

## 七、恢复优先级建议

| 阶段 | 功能 | 预估工作量 | 依赖 |
|------|------|-----------|------|
| **P0（核心）** | 代码生成按钮 + 逻辑实现下拉 | 小 | 无 |
| **P0（核心）** | 自动收集子控件 | 中 | 类型检测工具方法 |
| **P1（重要）** | 模板保存/加载/复制/粘贴 | 小 | 无 |
| **P1（重要）** | NoCodeGen 剪贴板生成 | 小 | P0 完成后 |
| **P2（增强）** | 基类 Prefab 选择器 + 只读展示 | 中 | 无 |
| **P2（增强）** | 缺失字段检测 + 自动修复 | 中 | P2 完成后 |
| **P3（优化）** | 绑定列表自定义 drawer | 高 | 需要整体设计 |
| **P3（优化）** | 上下文类型下拉 | 中 | 无 |
| **P4（锦上添花）** | 搜索过滤 | 中 | P3 完成后 |
| **P4（锦上添花）** | 继承冲突校验 | 低 | 无 |

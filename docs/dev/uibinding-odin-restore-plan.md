# EmberUIBinding Odin 面板功能恢复计划

> 已放弃自定义 `EmberUIBindingEditor : OdinEditor`，改用纯 Unity 原生 Inspector + Odin 属性渲染。
> 本文档列出丢失的功能及恢复方案。

---

## 功能丢失总览

| # | 分类 | 功能 | 程度 | 原因 |
|---|------|------|------|------|
| 1 | 模板 | 加载模板（ObjectPicker 选 `EmberUIBindingTemplate`） | ✅ 已恢复 | 委托注入模式，见 [一、模板操作](#一模板操作-4-项) |
| 2 | 模板 | 保存为模板（`SaveFilePanel`） | ✅ 已恢复 | 委托注入模式，见 [一、模板操作](#一模板操作-4-项) |
| 3 | 模板 | 复制/粘贴模板（内存剪贴板） | ✅ 已恢复 | 委托注入模式，粘贴有确认弹窗防误操作 |
| 4 | 继承 | 基类 Prefab 选择器（ObjectField → GUID 自动转换） | ✅ 已恢复 | `OpenFilePanel` + `AssetDatabase.AssetPathToGUID` |
| 5 | 继承 | 基类信息只读展示（isPage / pageName / classPath） | ✅ 已恢复 | `[ShowInInspector, ReadOnly]` 计算属性 |
| 6 | 继承 | 缺失字段自动检测 + "自动修复"按钮 | ✅ 已恢复 | 比对基类 Bindings，`[Button]` + `[ShowIf("HasMissingFields")]` |
| 7 | 继承 | Page/非 Page 继承冲突校验 | ✅ 已恢复 | `[InfoBox]` on `baseBindingUUID`，`[ShowIf("HasInheritanceConflict")]` |
| 8 | 代码生成 | 逻辑实现下拉框（选择 `LogicImplementationData`） | ✅ 已恢复 | `◀ Name ▶` 按钮切换（避免 Runtime 引用 Odin Core） |
| 9 | 代码生成 | 代码生成路径预览 | ✅ 已恢复 | `[ShowInInspector, ReadOnly]` 计算属性 |
| 10 | 代码生成 | "生成代码" / "重新生成"按钮 | ✅ 已恢复 | `[Button]` + `[ShowIf("HasGeneratedFile")]` 切换显示 |
| 11 | 代码生成 | NoCodeGen 模式（"生成到剪贴板"） | ✅ 已恢复 | `[Button]` + `[ShowIf("@noCodeGen")]` |
| 12 | 代码生成 | 自动收集子控件按钮 | ✅ 已恢复 | 递归遍历 Transform + AutoSelectByObject |
| 13 | 代码生成 | 设置齿轮按钮（跳转 Project Settings） | ✅ 已恢复 | `[Button("⚙")]` → `SettingsService.OpenProjectSettings` |
| 14 | 自身控件 | 上下文类型下拉（仅显示 GO 上存在的组件类型） | ✅ 已恢复 | 只读提示行显示 GO 上实际存在的类型 |
| 15 | 自身控件 | 控件类型实时验证 + "自动识别类型"按钮 | ✅ 已恢复 | `[Button]` + `AutoSelectByObject` |
| 16 | 搜索 | 按名称搜索绑定条目 | ✅ 已恢复 | 绑定列表顶部搜索栏 + 实时过滤 |
| 17 | 搜索 | 按节点搜索绑定条目 | ✅ 已恢复 | 绑定列表顶部 ObjectField + 实时过滤 |
| 18 | 绑定列表 | 拖入 GameObject 自动检测组件类型 | ✅ 已恢复 | `EmberBindingListDrawer` 内联处理 |
| 19 | 绑定列表 | 拖入 GameObject 自动生成变量名 | ✅ 已恢复 | `EmberBindingListDrawer` 内联处理 |
| 20 | 绑定列表 | GameObject 层级验证（必须为子节点） | ✅ 已恢复 | `IsValidChild()` 检查 + 弹窗拒绝 |
| 21 | 绑定列表 | 重复绑定检测（跨递归子 UIBinding） | ✅ 已恢复 | `GatherBindingDefinitions` + HashSet 检测 |
| 22 | 绑定列表 | 继承条目锁定（只读不可编辑） | ✅ 已恢复 | `EditorGUI.BeginDisabledGroup(isInherited)` |
| 23 | 绑定列表 | 逐条目删除/刷新按钮 | ✅ 已恢复 | `×` 按钮（继承条目不显示） |
| 24 | 绑定列表 | "添加绑定"按钮（独立于计数） | ✅ 已恢复 | 列表底部独立 `GUILayout.Button` |

> ✅ 24 项功能全部恢复完成 🎉

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

### ✅ 模板操作 —— 已实施（2026-08-10）

**实施方式**：委托注入模式 —— Runtime 程序集不可引用 `UnityEditor`，故在 `EmberUIBinding` 中用 `#if UNITY_EDITOR` 声明 `public static Action<EmberUIBinding>` 委托 + `[Button]` 桩方法，真正的 Editor 逻辑放在新文件 `EmberUIBindingTemplateUtility.cs` 中，由 `[InitializeOnLoad]` 静态构造函数注册。

**文件变更**：

| 文件 | 操作 | 说明 |
|------|------|------|
| `Runtime/EmberUIBinding.cs` | 修改 | `#if UNITY_EDITOR` 添加 4 个模板按钮 + 4 个静态委托 + `HasCopiedTemplate` 状态 |
| `Editor/EmberUIBindingTemplateUtility.cs` | 新建 | 保存/加载/复制/粘贴的 Editor 端实现（`[InitializeOnLoad]` 注册） |
| `Editor/EmberUIBindingEditor.cs` → `EmberUIBindingEditorUtility.cs` | 合并 | 原内容合并入 `EmberUIBindingEditorUtility.cs`，消除 Odin 误报，旧文件删除 |

**实现细节**：

| 功能 | 交互方式 | 防误操作 |
|------|---------|---------|
| 保存为模板 | `EditorUtility.SaveFilePanel` 系统文件对话框 | — |
| 加载模板 | `EditorUtility.OpenFilePanel` 系统文件对话框（同步，过滤 `.asset`） | 确认弹窗：模板名 + 绑定数 |
| 复制 | 创建内存 `EmberUIBindingTemplate` 快照 | — |
| 粘贴 | 从内存快照恢复 | 确认弹窗：模板名 + 绑定数 |

> **注意**：加载模板放弃了原方案中的 `ObjectPicker` + `EditorApplication.update` 轮询（Odin 环境下不稳定），改用同步 `OpenFilePanel`，与保存保持一致的交互体验。

**架构示意**：

```
Inspector [Button] 点击
  → EmberUIBinding.cs (Runtime) — 委托调用，无 Editor API
    → EmberUIBindingTemplateUtility.cs (Editor) — SerializedObject 操作
```

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

### ✅ 继承管理 —— 已实施（2026-08-10）

**实施方式**：与模板操作相同的委托注入模式。5 个静态委托 + 实例桥接方法供 Odin 特性使用。

**文件变更**：

| 文件 | 操作 | 说明 |
|------|------|------|
| `Runtime/EmberUIBinding.cs` | 修改 | `#if UNITY_EDITOR` 添加 5 个继承委托 + 选择按钮 + 信息展示 + 自动修复按钮 |
| `Runtime/EmberUIBinding.cs` (baseBindingUUID) | 修改 | 添加 `[InfoBox]` 条件冲突提示 |
| `Editor/EmberUIBindingInheritanceUtility.cs` | 新建 | 所有继承 Editor 逻辑（`[InitializeOnLoad]` 注册） |

**实现细节**：

| 功能 | 实现 |
|------|------|
| 选择基类 Prefab | `[Button]` → `OpenFilePanel("*.prefab")` → `AssetPathToGUID` → 写入 `baseBindingUUID` |
| 基类信息展示 | `[ShowInInspector, ReadOnly]` 计算属性，调用 `OnGetBaseInfoSummary(GUID)` 返回多行摘要 |
| 缺失字段检测 | `HandleGetMissingFieldCount()` 比对基类/当前 Bindings，返回差异数 |
| 自动修复按钮 | `[Button]` + `[ShowIf("HasMissingFields")]` → 添加基类有而当前无的绑定条目 |
| 冲突校验 | `[InfoBox]` on `baseBindingUUID` + `[ShowIf("HasInheritanceConflict")]` → isPage 不匹配时显示红色警告 |

**Odin 表达式桥接**：

```
[ShowIf("HasInheritanceConflict")]   →  private bool HasInheritanceConflict => OnHasInheritanceConflict?.Invoke(this) ?? false;
[ShowIf("HasMissingFields")]         →  private bool HasMissingFields => (OnGetMissingFieldCount?.Invoke(this) ?? 0) > 0;
```

**架构示意**：

```
Inspector
  ├─ [选择基类 Prefab] 按钮 → OpenFilePanel → GUID 写入 baseBindingUUID
  ├─ 基类信息 只读展示 → OnGetBaseInfoSummary(GUID)
  ├─ [InfoBox] 冲突警告 → OnHasInheritanceConflict(binding)
  └─ [自动添加缺失的绑定] 按钮 → OnAutoFixMissingBindings(binding)
```

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

### ✅ 代码生成 —— 已实施（2026-08-10）

**实施方式**：委托注入模式，所有 Editor 逻辑集中在 `EmberUIBindingCodeGenUtility.cs`。

**文件变更**：

| 文件 | 操作 | 说明 |
|------|------|------|
| `Runtime/EmberUIBinding.cs` | 修改 | `#if UNITY_EDITOR` 添加 8 个委托 + 逻辑实现选择器 + 路径预览 + 生成按钮 |
| `Editor/EmberUIBindingCodeGenUtility.cs` | 新建 | 所有代码生成 Editor 逻辑（`[InitializeOnLoad]` 注册） |

**Inspector 中的"代码生成"区域**：

```
noCodeGen = false 时:

┌ 代码生成 ─────────────────────────────────┐
│  ▎代码生成                                  │
│                                            │
│  [◀]  逻辑实现  C# 实现  [▶]               │  ← #8 按钮切换
│  生成路径  Assets/.../UIMainMenu.cs         │  ← #9 路径预览
│                                            │
│  [ 自动收集子控件 ]                         │  ← #12 蓝色按钮
│  [       生成代码       ]                   │  ← #10 绿色大按钮（首次）
│  [       重新生成       ]                   │  ← #10 橙色大按钮（已有文件）
│                                       [⚙]  │  ← #13 齿轮按钮
└────────────────────────────────────────────┘

noCodeGen = true 时:

┌ 代码生成 ─────────────────────────────────┐
│  [     生成到剪贴板     ]                   │  ← #11 紫色大按钮
└────────────────────────────────────────────┘
```

**功能细节**：

| # | 功能 | 实现 |
|---|------|------|
| 8 | 逻辑实现选择 | `◀ ▶` 按钮 + 只读名称，循环切换 `CodeGenLogicIndex`（静态项目级索引） |
| 9 | 路径预览 | `HandleGetGeneratedPath()` 根据 codePath/classPath/className 拼接完整路径 |
| 10 | 生成/重新生成 | 根据 `HasGeneratedFile` 切换按钮文本，调用 `logic.GenerateCode(binding, baseCls, declaredFields)` |
| 11 | NoCodeGen 剪贴板 | `[ShowIf("@noCodeGen")]` 条件显示，调用 `logic.GenerateCodeForNoGen(binding, className)` |
| 12 | 自动收集 | 递归子节点，过滤 `m_`/`mXxx` 前缀节点，调用 `AutoSelectByObject` + `GetNameForCode` |
| 13 | 设置齿轮 | `[Button("⚙")]` → `SettingsService.OpenProjectSettings("Project/Ember UI Binding")` |

**基类继承处理**：生成代码时自动检测 `baseBindingUUID` → 加载基类 EmberUIBinding → 计算 `declaredFields`（排除基类已有字段）→ 传入 `GenerateCode`。

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

### ✅ 绑定列表增强 —— 已实施（2026-08-10）

**实施方式**：自定义 `EmberBindingListDrawer : OdinAttributeDrawer<EmberBindingListAttribute, EmberUIBinding.BindingEntry[]>`，通过 Unity `SerializedProperty` API 逐条目绘制，替代 Odin 默认的 ListDrawer。

**文件变更**：

| 文件 | 操作 | 说明 |
|------|------|------|
| `Runtime/EmberBindingListAttribute.cs` | 新建 | 标记属性（Runtime 端，无 Editor 依赖） |
| `Runtime/EmberUIBinding.cs` (bindings) | 修改 | `[ListDrawerSettings]` → `[EmberBindingList]` |
| `Editor/EmberBindingListDrawer.cs` | 新建 | 全量自定义列表渲染（~320 行） |

**每条绑定条目的渲染结构**：

```
┌─────────────────────────────────────────────┐
│ 变量名 [_______] 节点 [_______]        [×]  │  ← #23 删除按钮
│ 控件类型 [Dropdown_________________]        │  ← #18 拖入自动检测 / #22 继承禁用
│                                             │
│ ⚠ 此节点已被 "XXX" 上的 Binding 绑定...      │  ← #21 重复检测
│ 继承自 [BasePrefab]                         │  ← #22 继承来源
└─────────────────────────────────────────────┘

[ 添加绑定 ]                                    ← #24 底部独立按钮
```

**各功能对应实现**：

| # | 功能 | 实现 |
|---|------|------|
| 18 | 拖入 GO 自动检测类型 | `AutoSelectByObject(go, typeSp, cnSp)` |
| 19 | 拖入 GO 自动生成变量名 | `GenerateName(go, definedNames)`，去除 `m_` 前缀 |
| 20 | 层级验证 | `IsValidChild(binding, go)` 逐级向上检查，非法则弹窗恢复旧值 |
| 21 | 重复绑定检测 | `GatherBindingDefinitions` 全量收集 + 同列表 `HashSet` 去重 |
| 22 | 继承条目锁定 | `EditorGUI.BeginDisabledGroup(isInherited)` 禁用 Name/GO/Type，隐藏 × 按钮 |
| 23 | 逐条目删除 | 每行右侧 `×` 按钮，标记 `toRemove`，循环结束后 `DeleteArrayElementAtIndex` |
| 24 | 添加绑定按钮 | 列表底部独立 `GUILayout.Button("添加绑定")`，调用 `AddEntry(sp)` |

---

## 七、完成总结

**24 项功能全部恢复**（2026-08-10）。

### 架构模式

全部采用 **委托注入模式**：Runtime 程序集通过 `public static Action/Func` 委托 + `[Button]` / `[ShowInInspector]` Odin 特性暴露 UI，Editor 程序集通过 `[InitializeOnLoad]` 静态构造函数注册处理器。

```
Inspector UI (Runtime)
  → 静态委托 (Runtime)
    → [InitializeOnLoad] 处理器 (Editor)
      → SerializedObject / AssetDatabase / EditorUtility
```

### 文件清单

| 文件 | 角色 |
|------|------|
| `Runtime/EmberUIBinding.cs` | 主组件 + 全部 `#if UNITY_EDITOR` 委托和 Odin UI |
| `Runtime/EmberBindingListAttribute.cs` | 绑定列表标记属性 |
| `Editor/EmberUIBindingTemplateUtility.cs` | 模板：保存/加载/复制/粘贴 |
| `Editor/EmberUIBindingInheritanceUtility.cs` | 继承：Prefab 选择/信息/缺失修复/冲突校验 |
| `Editor/EmberUIBindingCodeGenUtility.cs` | 代码生成：逻辑选择/路径/生成/剪贴板/自动收集 |
| `Editor/EmberUIBindingSelfWidgetUtility.cs` | 自身控件：可用类型提示/自动识别 |
| `Editor/EmberBindingListDrawer.cs` | 绑定列表：自定义 Odin drawer + 搜索 |
| `Editor/EmberUIBindingEditorUtility.cs` | 已有工具类（合并了原 EmberUIBindingEditor 全部代码） |

### 各分类实现方式

| 分类 | 项数 | 核心机制 |
|------|------|---------|
| 模板 #1-3 | 3 | `OpenFilePanel`/`SaveFilePanel` + `EmberUIBindingTemplate` SO |
| 继承 #4-7 | 4 | `AssetDatabase.GUIDToAssetPath` + `SerializedObject` 操作 |
| 代码生成 #8-13 | 6 | `LogicImplementationData` API + `SettingsService` |
| 自身控件 #14-15 | 2 | `AutoSelectByObject` + 组件类型枚举 |
| 搜索 #16-17 | 2 | 绑定列表顶部搜索栏 + 实时文本/节点过滤 |
| 绑定列表 #18-24 | 7 | `EmberBindingListDrawer` 自定义 Odin drawer |

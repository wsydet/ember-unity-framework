# Odin Inspector 使用注意事项

> 适用环境：Unity 6000.x + Odin Inspector 4.x

---

## 一、已知兼容性问题

### 1.1 HorizontalGroup + ShowInInspector 布局错位

**现象**：在 `[HorizontalGroup]` 中混入 `[ShowInInspector]` 标注的字段/属性时，
Inspector 面板出现元素错位、重叠或超出边界。

**原因**：Unity 6 的 IMGUI 布局系统行为有破坏性变更，
Odin 4.x 的 `HorizontalGroupAttributeDrawer` 在 repaint 阶段
GUILayout 控件计数与 Layout 阶段不一致。

**影响范围**：

| 组合 | 状态 |
|------|------|
| `[HorizontalGroup]` + `public` 字段 | ✅ 正常 |
| `[HorizontalGroup]` + `[ShowInInspector] private` 字段 | ❌ 可能错位 |
| `[HorizontalGroup]` + `[ShowInInspector]` 计算属性 | ❌ 容易错位 |

**解决方案**（按优先级）：

1. **避免混用** —— 不要在 `[HorizontalGroup]` 内使用 `[ShowInInspector]`，
   将 private 字段改为 `public` 或 `[SerializeField] private`
2. **拆出计算属性** —— 将 `[ShowInInspector]` 的计算属性移出 `[HorizontalGroup]`，
   放在独立的一行
3. **放弃水平布局** —— 如果错位无法修复，改用垂直排列

**示例**：

```csharp
// ❌ 错误 —— ShowInInspector 在 HorizontalGroup 内
[HorizontalGroup("Stats")]
[ShowInInspector, MinValue(1), MaxValue(99)]
private int level = 1;

[HorizontalGroup("Stats")]
[ShowInInspector, ReadOnly]
private string Summary => $"Lv.{level}";

// ✅ 正确 —— 拆分
[HorizontalGroup("Stats")]
public int level = 1;

[HorizontalGroup("Stats")]
public int hp = 500;

// 计算属性单独一行
[ShowInInspector, ReadOnly]
private string Summary => $"Lv.{level}  HP:{hp}";
```

### 1.2 ButtonGroup 按钮布局错位

**现象**：多个按钮使用相同 `[ButtonGroup]` 名称时，不同尺寸的按钮被
挤在同一行，导致布局错乱。此外不同 section 的按钮如果用了同名 `[ButtonGroup]`，
Odin 会将它们合并到同一个组渲染。

**解决方案**：
1. **每个按钮独立一行** —— 不使用 `[ButtonGroup]`
2. 如果需要水平排列，确保同一组内所有按钮尺寸一致
3. **不同 section 的按钮不要用同名 ButtonGroup**

```csharp
// ❌ 错误 —— 同组不同尺寸，或不同 section 同名组
[ButtonGroup("Actions")]
[Button("确定", ButtonSizes.Large)]
private void Confirm() { }

[ButtonGroup("Actions")]
[Button("取消", ButtonSizes.Small)]  // 尺寸不一致导致错位
private void Cancel() { }

// ✅ 正确 —— 独立按钮，各占一行
[Button("确定", ButtonSizes.Large)]
private void Confirm() { }

[Button("取消")]
private void Cancel() { }
private void B() { }
```

### 1.3 ListDrawerSettings.Expanded 已废弃

**现象**：编译 warning `CS0618: 'ListDrawerSettingsAttribute.Expanded' is obsolete`
出现在 `[ListDrawerSettings(Expanded = true)]` 上。

**原因**：Odin 4.x 确认 `Expanded` 实际行为一直是控制 `ShowFoldout`，
命名有歧义。官方标记为废弃并替换为两个独立属性。

**修复**：

```csharp
// ❌ 旧写法 —— 编译 warning
[ListDrawerSettings(Expanded = true)]

// ✅ 新写法
[ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true)]
```

| 属性 | 作用 |
|------|------|
| `ShowFoldout` | 列表是否显示折叠箭头（false = 完全平铺） |
| `DefaultExpandedState` | 折叠默认状态（true = 展开，false = 折叠） |

**实际应用示例**：`EmberDebugConfigSO.frameworkEntries` 使用此属性让框架标签默认展开，
方便查看所有预定义标签。

### 1.4 AssetsOnly / SceneObjectsOnly 空值时可能触发 Shader 异常

**现象**：`[AssetsOnly]` 或 `[SceneObjectsOnly]` 字段未赋值时，
Odin 渲染对象字段图标时抛出 `ArgumentNullException: Value cannot be null. Parameter name: shader`。

**解决方案**：Odin 4.x 在 Unity 6 下的内部图标渲染 bug，不影响功能，
保持字段为空或有值均可，报错会自行恢复。如要彻底消除，
给字段赋默认值或等待 Odin 更新。

---

## 二、推荐实践

### 2.1 优先使用 [SerializeField] 替代 [ShowInInspector]

```csharp
// 推荐 —— 直接序列化，兼容性最好
[SerializeField]
private int level = 1;

// 可用但有兼容风险
[ShowInInspector]
private int level = 1;
```

### 2.2 复杂布局优先用 FoldoutGroup + 垂直排列

`[HorizontalGroup]` 在 Unity 6 下不稳定时，改用 `[FoldoutGroup]` + 垂直排列：

```csharp
[FoldoutGroup("角色属性")]
public int level;
[FoldoutGroup("角色属性")]
public int hp;
[FoldoutGroup("角色属性")]
public int mp;
```

### 2.3 按钮保持独立

每个 `[Button]` 独立一行，不使用 `[ButtonGroup]`。
`[ButtonGroup]` 在 Unity 6 + Odin 4.x 下容易导致按钮挤在一起或跨 section 合并。

### 2.4 参考脚本

| 脚本 | 说明 |
|------|------|
| [OdinInspectorDemo.cs](../../Assets/Tem/Examples/OdinInspectorDemo.cs) | 完整特性演示（MonoBehaviour） |
| [GameLauncher.cs](../../Assets/Ember/Core/Runtime/GameLauncher.cs) | `[FoldoutGroup]` + `[BoxGroup]` + `[Required]` + `[ShowInInspector/ReadOnly]` 实战（MonoBehaviour） |
| [EmberDebugConfigSO.cs](../../Assets/Ember/Core/Runtime/EmberDebugConfigSO.cs) | SO 继承层级 `L0/L1` + `[BoxGroup]` 无名分隔 + `[GUIColor]` + `[VisibleIf]` 实战（ScriptableObject） |
| [EmberBaseSO.cs](../../Assets/Ember/Core/Runtime/Service/EmberBaseSO.cs) | `[FoldoutGroup]` + `[BoxGroup(ShowLabel=false)]` + `[Title]` 基类面板（ScriptableObject） |
| [EmberCameraManager.cs](../../Assets/Ember/Camera/Runtime/EmberCameraManager.cs) | `[GUIColor]` 动态状态着色 + `[LabelText]` 实战 |
| [EmberDebugConfigEditor.cs](../../Assets/Ember/Core/Editor/EmberDebugConfigEditor.cs) | `OdinEditor` + `[Button]` 批量操作实战 |

### 2.5 SO 继承层级面板 —— L*N* 模式

**适用场景**：有继承链的 ScriptableObject，每层类各自占据一个独立的 `[FoldoutGroup]`，
数字层级清晰表达继承深度。

**规则**：
- 基类定义 `const string GROUP_NAME = "L0: BaseSO"`，子类定义 `"L1: SubClassName"`
- `[FoldoutGroup("$GROUP_NAME")]` 使用 `$` 语法引用常量，确保组名单一来源
- 基类内参数用 `[BoxGroup("$GROUP_NAME/子组", ShowLabel = false)]` 做无名线框

```csharp
// 基类 —— L0
public class EmberBaseSO : ScriptableObject
{
    private const string GROUP_NAME = "L0: BaseSO";

    [PropertyOrder(-1000)]
    [FoldoutGroup("$GROUP_NAME", Expanded = true)]
    [BoxGroup("$GROUP_NAME/Chain", ShowLabel = false)]
    [Title("Type Hierarchy", "自动化继承溯源")]
    [ShowInInspector, ReadOnly]
    private string InheritanceChain { get; }
}

// 子类 —— L1
public class EmberDebugConfigSO : EmberBaseSO
{
    private const string DEBUG_GROUP = "L1: DebugConfig";

    [FoldoutGroup("$DEBUG_GROUP", Expanded = true)]
    [BoxGroup("$DEBUG_GROUP/全局设置", ShowLabel = false)]
    public bool globalOpen = true;
}
```

**效果**：Inspector 中基类成员折叠在 `L0: BaseSO` 下，子类成员折叠在 `L1: DebugConfig` 下，
继承层级一目了然。普通 MonoBehaviour 不适用此模式（无继承链展示需求）。

### 2.6 BoxGroup(ShowLabel=false) —— 无名线框分隔

**适用场景**：同一个 FoldoutGroup 内，不同参数组之间用无标签的视觉线框隔开。

**规则**：
- `[BoxGroup("path", ShowLabel = false)]` 创建无标题的线框
- 框内第一个成员用 `[Title("标题")]` 作为视觉段落头
- 同组字段共享相同的 BoxGroup path

```csharp
private const string ODIN_GROUP = "Game Launcher";

[FoldoutGroup(ODIN_GROUP, Expanded = true)]
[BoxGroup(ODIN_GROUP + "/配置", ShowLabel = false)]
[Title("宿主节点")]
[SerializeField] private GameObject _uiRoot;

[BoxGroup(ODIN_GROUP + "/配置")]
[SerializeField] private GameObject _audioHost;

[BoxGroup(ODIN_GROUP + "/运行时", ShowLabel = false)]
[Title("运行时状态")]
[ShowInInspector, ReadOnly]
public bool IsInitialized { get; private set; }
```

### 2.7 GUIColor("$Property") —— 动态状态着色

**适用场景**：根据运行时状态动态改变字段/属性的文字颜色，提供一目了然的状态指示。

**规则**：
- `[GUIColor("$PropertyName")]` 引用返回 `Color` 的属性
- 也可用 `@` 表达式内联：`[GUIColor("@_active != null ? Color.green : Color.red")]`
- 预定义/系统条目用灰色，用户条目用白色，正常用绿色，异常用红色

```csharp
// 方式一：引用属性
private Color RowColor => IsPredefined
    ? new Color(0.55f, 0.55f, 0.55f)
    : Color.white;

[GUIColor("$RowColor")]
public string className;

// 方式二：内联表达式
[ShowInInspector, ReadOnly]
[GUIColor("@_active != null ? Color.green : Color.red")]
private string ActiveCamera { get; }
```

### 2.8 InfoBox + VisibleIf —— 条件提示

**适用场景**：提示信息只在条件满足时显示，避免无关状态下占用空间。

```csharp
// 仅在 autoCollect=开 且 userEntries 为空时显示提示
[InfoBox("运行时调用 EmberDebug.Log() 会自动收集新标签。",
    VisibleIf = "@autoCollect && userEntries.Count == 0",
    InfoMessageType = InfoMessageType.Info)]
public List<LoggerClassEntry> userEntries = new();
```

**规则**：`VisibleIf` 使用 `@` 前缀的 NCalc 表达式，可引用当前类的任意 public 或 private 成员。

### 2.9 LabelText —— 运行时状态中文化

**适用场景**：`[ShowInInspector, ReadOnly]` 标记的运行时属性，用中文 LabelText 替代默认的 PascalCase 变量名。

```csharp
[ShowInInspector, ReadOnly, LabelText("已初始化")]
public bool IsInitialized { get; private set; }

[ShowInInspector, ReadOnly, LabelText("当前状态")]
private string CurrentState => Fsm?.Current?.Name ?? "—";
```

### 2.10 统一 Title 规范

**适用场景**：所有 `[Title]` 使用方式。

**规则**：
- **统一左对齐** —— 不加 `titleAlignment` 参数，使用默认左对齐
- **不加横线** —— 不加 `horizontalLine: true`，用 `BoxGroup` 的分隔线替代
- **双参数形式** —— `[Title("主标题", "副标题")]` 用于需要补充说明的段落头

```csharp
// ✅ ember 规范
[Title("宿主节点")]
[Title("框架标签", "Ember 框架内置的日志标签。颜色跟随预定义，不可修改。")]

// ❌ 旧风格（已弃用）
[Title("Title", titleAlignment: TitleAlignments.Centered, horizontalLine: true)]
```

---

## 三、版本要求

| 组件 | 最低版本 |
|------|----------|
| Unity | 6000.x |
| Odin Inspector | 4.0.0+（推荐 4.0.2+） |

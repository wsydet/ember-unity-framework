# Odin Inspector 使用注意事项

> 当前项目：Unity 6000.5.4f1 + Odin Inspector 4.0.2（manifest tag）。以下布局问题是本项目历史观察，不能推断为所有 Unity 6/Odin 4 版本的通用缺陷；本轮只核对源码和引用，未重测面板。

---

## 一、历史观察与排查边界

### 水平分组和按钮布局

历史面板曾在 HorizontalGroup 与 ShowInInspector/计算属性组合、不同尺寸 ButtonGroup、Title 紧邻按钮时出现错位或重叠。未保留能证明插件或引擎根因的最小复现，不能把组合本身列为错误或跨版本禁令。

出现问题时先记录 Unity/Odin 版本、面板对象、宽度、皮肤、Layout/Repaint 和完整异常堆栈；检查动态分组路径、同名组意外合并、Drawer、控件顺序和异步状态更新。仅针对可复现问题调整分组、宽度、换行或在下一次 Layout 应用状态，之后实际验证窄窗、停靠、切页、脏状态及 Undo。垂直排列是可选布局，不是兼容性要求。

### 引用限制字段与 Shader 异常

旧记录在 AssetsOnly/SceneObjectsOnly 空引用绘制时观察到 shader 参数为空的异常。根因未确认，不能认定这些特性不支持空值；保留堆栈并在最小对象复现，不能填入无意义业务默认值掩盖错误。

### 废弃成员

历史编译曾对 ListDrawerSettings.Expanded 报 CS0618。维护时根据当前安装程序集的 Obsolete 信息和实际需求选择 ShowFoldout、DefaultExpandedState 等成员；不要机械地把旧 Expanded 值同时映射成两个新属性。是否显示折叠入口、初始展开状态和用户已有状态分别验证。

## 二、显示与序列化职责

`ShowInInspector` 用于展示，不能据此推断字段已持久化；`SerializeField` 改变 Unity 序列化范围，不能作为布局修复的等价替代。计算值、运行观察、public API 和已有序列化字段各自保持职责。改变可见性或字段/属性形式前核对调用方、资产数据、Undo/Redo、脏标记和迁移需求。

布局建议以使用任务为准：常用操作优先，配置与操作分区，诊断折叠但必要错误常显；尺寸、速度、时间等标出单位。ButtonGroup、HorizontalGroup、ShowInInspector 都可以使用，按实际宽度和交互验证。Editor 绘制不触发保存、迁移、应用或重启，Editor/Odin 编辑器 API 不进入运行程序集。

### 2.4 参考脚本

| 脚本 | 说明 |
|------|------|
| [GameLauncher.cs](../../Packages/com.ember/Core/Runtime/GameLauncher.cs) | `[FoldoutGroup]` + `[BoxGroup]` + `[Required]` + `[ShowInInspector/ReadOnly]` 实战（MonoBehaviour） |
| [EmberDebugConfigSO.cs](../../Packages/com.ember/Basic/Runtime/Debug/EmberDebugConfigSO.cs) | SO 继承层级 `L0/L1` + `[BoxGroup]` 无名分隔 + `[GUIColor]` + `[VisibleIf]` 实战（ScriptableObject） |
| [EmberBaseSO.cs](../../Packages/com.ember/Basic/Runtime/Base/EmberBaseSO.cs) | `[FoldoutGroup]` + `[BoxGroup(ShowLabel=false)]` + `[Title]` 基类面板（ScriptableObject） |
| [EUIBinding.cs](../../Packages/com.ember/UIExtension/Runtime/EUIBinding.cs) | 页面角色、绑定列表、代码生成与分组面板 |
| [EmberDebugConfigEditor.cs](../../Packages/com.ember/Basic/Editor/EmberDebugConfigEditor.cs) | `OdinEditor` + `[Button]` 批量操作实战 |
| [EmberSceneMapping.cs](../../Packages/com.ember/Core/Editor/EmberSceneMapping.cs) | `[FoldoutGroup]` + `[BoxGroup]` + `[Button]` + `EmberSceneField` + `$GROUP` 引用实战（ScriptableObject） |

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

### 2.8 $ 成员引用语法 —— 不要拼接字符串

Odin 的 `[FoldoutGroup]`、`[BoxGroup]`、`[GUIColor]` 等支持 `$` 前缀引用成员变量：

```csharp
private const string GROUP = "Scene Mapping";

// ✅ 正确 —— $"$CONSTANT_NAME" 引用成员
[FoldoutGroup("$GROUP")]
[BoxGroup("$GROUP/子组", ShowLabel = false)]

// ❌ 错误 —— 字符串拼接 → Odin 把 "Scene Mapping" 当成员名解析，失败
[BoxGroup("$" + GROUP + "/子组")]  
```

**规则**：
- `$` 后面跟**成员名**（字段/属性/常量名），不是成员值
- 字面分组路径可使用常量拼接；动态解析时 `$` 后必须仍是成员名，不能拼入该成员的值。
- 路径分隔用 `/`：`"$GROUP/子组名"`

### 2.9 InfoBox + VisibleIf —— 条件提示

**适用场景**：提示信息只在条件满足时显示，避免无关状态下占用空间。

```csharp
// 仅在 autoCollect=开 且 userEntries 为空时显示提示
[InfoBox("运行时调用 EmberDebug.Log() 会自动收集新标签。",
    VisibleIf = "@autoCollect && userEntries.Count == 0",
    InfoMessageType = InfoMessageType.Info)]
public List<LoggerClassEntry> userEntries = new();
```

**规则**：`VisibleIf` 使用 Odin 的 `@` 表达式，可引用当前类的任意 public 或 private 成员。

### 2.10 LabelText —— 运行时状态中文化

**适用场景**：`[ShowInInspector, ReadOnly]` 标记的运行时属性，用中文 LabelText 替代默认的 PascalCase 变量名。

```csharp
[ShowInInspector, ReadOnly, LabelText("已初始化")]
public bool IsInitialized { get; private set; }

[ShowInInspector, ReadOnly, LabelText("当前状态")]
private string CurrentState => Fsm?.Current?.Name ?? "—";
```

### 2.11 Title 与按钮间距的历史处理

EUIBinding 的历史面板曾在标题和按钮之间使用返回空字符串的只读展示行缓解重叠。这个局部处理不能证明所有 Title 都需要占位属性，也不能因此禁止删除任何空行。先核对当前源码和实际渲染；需要间距时使用面板已有布局机制，并验证不同宽度、皮肤和重绘。已有占位行的移除也须实际复测，不能仅凭源码认定无用。

### 2.12 统一 Title 规范

**适用场景**：所有 `[Title]` 使用方式。

**项目默认风格（不代表 Odin 兼容性限制）**：
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

## 三、项目版本记录

| 组件 | 当前项目 |
|------|----------|
| Unity | 6000.5.4f1 |
| Odin Inspector | 4.0.2（私有 UPM tag） |

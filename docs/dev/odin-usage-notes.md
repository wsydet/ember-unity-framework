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

### 1.3 AssetsOnly / SceneObjectsOnly 空值时可能触发 Shader 异常

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

完整的 Odin 特性演示和正确用法见：
`Assets/Ember/Examples/OdinInspectorDemo.cs`

---

## 三、版本要求

| 组件 | 最低版本 |
|------|----------|
| Unity | 6000.x |
| Odin Inspector | 4.0.0+（推荐 4.0.2+） |

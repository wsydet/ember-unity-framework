# Odin 面板脚本清单

> 记录所有使用 Odin Inspector 优化编辑器面板的脚本。
> 当面板风格规范更新时，按此清单逐一定位并同步。

**风格规范**：[odin-usage-notes.md](odin-usage-notes.md)
**最后全量更新**：2026-08-01

---

## 一、运行时脚本（Runtime）

### Ember/Core

| 脚本 | 类型 | Odin 模式 | 最后更新 |
|------|------|-----------|----------|
| [GameLauncher.cs](../../Assets/Ember/Core/Runtime/GameLauncher.cs) | MonoBehaviour | `FoldoutGroup` + `BoxGroup(ShowLabel=false)` + `Required` + `ShowInInspector/ReadOnly` + `LabelText` | 2026-08-01 |
| [EmberBaseSO.cs](../../Assets/Ember/Core/Runtime/Service/EmberBaseSO.cs) | ScriptableObject | `FoldoutGroup($const)` + `BoxGroup(ShowLabel=false)` + `Title` + `ShowInInspector/ReadOnly` | 2026-08-01 |
| [EmberDebugConfigSO.cs](../../Packages/com.ember.basic/Runtime/Debug/EmberDebugConfigSO.cs) | ScriptableObject | `FoldoutGroup($const)` + `BoxGroup(ShowLabel=false)` + `GUIColor($prop)` + `InfoBox(VisibleIf)` + `ListDrawerSettings` + `HorizontalGroup/HideLabel` | 2026-08-01 |

### Ember/Scene

| 脚本 | 类型 | Odin 模式 | 最后更新 |
|------|------|-----------|----------|
| — | — | — | — |

### Ember/UI

| 脚本 | 类型 | Odin 模式 | 最后更新 |
|------|------|-----------|----------|
| — | — | — | — |

### Ember/Audio

| 脚本 | 类型 | Odin 模式 | 最后更新 |
|------|------|-----------|----------|
| — | — | — | — |

### Ember/Input

| 脚本 | 类型 | Odin 模式 | 最后更新 |
|------|------|-----------|----------|
| — | — | — | — |

### Ember/Camera

| 脚本 | 类型 | Odin 模式 | 最后更新 |
|------|------|-----------|----------|
| — | — | — | — |

> ⚠️ EmberCameraManager 曾使用 Odin（FoldoutGroup + GUIColor + ShowInInspector），但因它是纯 C# 单例（不继承 MonoBehaviour），Odin 属性无法渲染，已于 2026-08-01 清理。

---

## 二、编辑器脚本（Editor）

| 脚本 | 类型 | Odin 模式 | 最后更新 |
|------|------|-----------|----------|
| [EmberDebugConfigEditor.cs](../../Packages/com.ember.basic/Editor/EmberDebugConfigEditor.cs) | OdinEditor | `PropertyOrder` + `HorizontalGroup` + `Button` | — |

---

## 三、示例 / 参考脚本

| 脚本 | 说明 | 最后更新 |
|------|------|----------|
| [OdinInspectorDemo.cs](../../Assets/Tem/Examples/OdinInspectorDemo.cs) | 完整 Odin 特性演示，仅供开发调试参考 | — |

---

## 四、未使用 Odin 的模块（待优化）

| 模块 | 脚本数 | 备注 |
|------|--------|------|
| Scene | 1 (EmberSceneManager) | 可考虑运行时状态展示（CurrentScene、Progress 等） |
| UI | — | 待开发 |
| Audio | — | 待开发 |
| Input | — | 待开发 |

---

## 更新日志

| 日期 | 变更 |
|------|------|
| 2026-08-01 | 建立清单；规范化 GameLauncher / EmberBaseSO / EmberDebugConfigSO 面板；清理 EmberCameraManager 死 Odin 代码；写入风格规范 §2.5-2.10 |

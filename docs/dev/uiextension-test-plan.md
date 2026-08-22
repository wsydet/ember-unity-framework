# UI 拓展（com.ember.uiextension）绑定系统测试计划

> 目的：验证 EUIBinding 绑定系统的**类型选择**与**参数暴露**能力——
> 1. 绑定的子组件「类型」下拉框能选到哪些类型（原生 + 增强）；
> 2. 不同组件类型通过 `[EUIExtension]` 暴露哪些相关参数（子组件）。
> 最后更新：2026-08-22
> 关联文档：[eui-reference.md](eui-reference.md)（API 明细）· [uiextension-migration-plan.md](uiextension-migration-plan.md)（迁移决策）

---

## 一、测试前置

### 1.1 GMPage 现状

| 项 | 值 |
|----|----|
| 路径 | `Assets/Game/UI/Runtime/Prefabs/GMPage.prefab`（已从框架层迁至业务层） |
| 逻辑类 | `Assets/Game/UI/Runtime/Pages/GMPage.cs`（手写 partial，命名空间 `Game.UI`）+ `GMPage.Binding.cs`（自动生成） |
| 实例化 | `GameInitState.OnEnter`（业务层 InitState 子类）—— 进主界面即创建，FreePage 常驻 |
| 当前绑定 | 13 个：`Btn_GM`(Button)、`Panel_GM`(Component)、`Pgb_TimeScale`(Slider)、`Txt_TimeScale`(TMP_Text)、`Txt_GameState`(TMP_Text)、`Tgl_Test`(Toggle)、`Scr_Test`(ScrollRect)、`Img_Test`(Image)、`Raw_Test`(RawImage)、`EUIBtn_Exit`(**EUIButtonEx**)、`EUITgl_Test`(**EUIToggleEx**)、`EUIImg_Test`(**EUIImageEx**)、`Img_Circle`(**EUICircleImage**) |

> ⚠️ 迁移原因：GMPage 使用增强组件（EUIButtonEx）后，若留在框架层 `Ember.UI.Runtime` 程序集会与 `Ember.UIExtension.Runtime`（已反向引用 UI.Runtime）形成循环依赖。迁至业务层（无 asmdef，编译进 Assembly-CSharp）后依赖方向正确：业务 → 框架 + 扩展。

> ⚠️ 预制体里有大量残留节点（`State`/`Input`/`Buttons`/`HP`/`MP`/`Jump*`/`NameInfo` 等），不在当前绑定里。
> 建议在 `Panel_GM` 下新建干净的测试容器（如 `TestArea`）放测试 UI。

### 1.2 绑定收集规则

1. 节点名以 `m_` 或 `m`+大写字母开头才会被自动收集（如 `m_Btn_Start`、`mEUIBtn`）。
2. 增强组件用 `EU` 前缀：`m_EUIBtn_` / `m_EUITgl_` / `m_EUIImg_`。
3. 装饰/布局容器不加前缀，或用 `EUIBindingExclude` 排除。

### 1.3 测试流程

```
① 在 Panel_GM（或 TestArea）下建节点，命名符合 m_ 规则，挂上增强组件
② 选中 GMPage 根节点 → EUIBinding → 「自动收集子控件」
③ 核对绑定列表里「类型」是否识别成增强类型（如 EUIButtonEx，而非 Button）
④ 「生成代码」→ 检查 GMPage.Binding.cs 是否生成强类型字段 + 暴露的子组件字段
⑤ Play Mode 验证运行时 ControlMap 能取到增强组件 + 暴露的子组件
```

---

## 二、绑定子组件可选类型清单

### 2.1 原生类型（内置规则 12 条）

`Button` /
`Text`(`Text`+`TMP_Text`) /
`Toggle` /
`InputField`(`InputField`+`TMP_InputField`) /
`ProgressBar`(`Slider`) / 
`ToggleGroup` / 
`ScrollRect` / 
`Image` / 
`RawImage` / 
`Canvas` /
 `CanvasGroup` /
  `UILogic`(`EUIBinding`)

### 2.2 增强类型（通过 `[EUIExtension]` 注册，本次新增）

| 扩展类型 | 替换原生 | 暴露子组件 | 命名建议 |
|---------|---------|-----------|---------|
| `EUIButtonEx` | `Button` | `TMP_Text`（label） | `m_EUIBtn_Xxx` |
| `EUIToggleEx` | `Toggle` | `TMP_Text`（label） | `m_EUITgl_Xxx` |
| `EUIImageEx` | `Image` | — | `m_EUIImg_Xxx` |
| `EUICircleImage` | `Image` | — | `m_Img_Xxx` |

### 2.3 不注册为可选类型（附加效果/修饰组件）

`EUIGradient` / `EUIRoundedImageModifier` / `EUIPolygonRaycast` / `EUIGraphicAnimation` ——
叠加在 Image/Text 上使用，不进「类型」下拉框。

---

## 三、子组件槽位（少绑一个）

增强组件上直接开序列化「槽位」字段，Inspector 把子组件拖入槽位后，绑定增强组件即可通过属性直接调用，
无需在顶层 EUIBinding 里额外为子组件建绑定，层级更直观。

示例（`EUIButtonEx` 带 `Label` 槽位）：

```csharp
// 业务代码：绑定 m_EUIBtn_Confirm 后直接调用子文本
EUIBtn_Confirm.Label.text = "确定";
```

> 增强组件实现 `IEUIExposedChildProvider` 接口（返回槽位持有的子组件），
> 自动收集时会跳过这些子节点——即使命名为 `m_` 前缀也不会被重复收集。

---

## 四、分项测试步骤

### 4.1 增强按钮 `EUIButtonEx`（核心验证：类型识别 + Label 槽位）

1. 新建 `m_EUIBtn_Confirm` 节点，加 `Image`（targetGraphic）+ `EUIButtonEx`，下设子节点 `Text (TMP)`（label）。
2. Inspector 把 label 拖进 `EUIButtonEx` 的 `Label` 槽位。
3. 自动收集 → 类型应识别为 **`EUIButtonEx`**（不是 `Button`），`ClassName` 自动填 `EUIButtonEx`。
4. 生成代码 → 产出 `EUIButtonEx` 强类型字段。
5. Play Mode：`ControlMap["EUIBtn_Confirm"] as EUIButtonEx` 非空；`.Label` 指向拖入的 label。

### 4.2 增强开关 `EUIToggleEx`（类型识别 + Label 槽位）

1. `m_EUITgl_Test` 挂 `EUIToggleEx`，下设 label 文本并拖进 `Label` 槽位。
2. 自动收集 → 类型识别为 `EUIToggleEx`；`.Label` 可直接访问拖入的文本。

### 4.3 增强图片 `EUIImageEx`（仅类型识别，无暴露）

1. `m_EUIImg_Test` 挂 `EUIImageEx`。
2. 自动收集 → 类型 `EUIImageEx`，生成强类型字段 `EUIImageEx`，无额外暴露字段。

### 4.4 圆形图 `EUICircleImage`（同原生 Image 多扩展的精确识别）

1. `m_Img_Circle` 挂 `EUICircleImage`。
2. 自动收集 → 类型应识别为 **`EUICircleImage`**（不能被误判成 `EUIImageEx`，两者都替换 `Image`）。

### 4.5 原生类型回归（确认没被破坏）

1. 原生 `m_Btn_X`（Button）自动收集仍识别为 `Button`；`m_Txt_X`（TMP_Text）仍识别为 `Text`。

### 4.6 组件替换（Inspector 三点菜单）

1. 选中一个原生 `Button` 组件 → Inspector 右上角三点菜单 → 应出现「替换为 EUIButtonEx」。
2. 点击替换 → 组件变成 `EUIButtonEx`，且 `targetGraphic` / `colors` / `transition` 等配置保留。
3. 已是 `EUIButtonEx` 时，该菜单项应灰置（validate）。
4. `Image` 应同时出现「替换为 EUIImageEx」「替换为 EUICircleImage」两项。

---

## 五、结果记录表

| 测试项 | 类型识别 | 生成字段 | 运行时取到 | 问题 / 备注 |
|--------|:--:|:--:|:--:|------|
| EUIButtonEx（Label 槽位） | ✅ | ✅ | ✅ | 需自定义 Editor 才能显示增强字段（见 6.1） |
| EUIToggleEx（Label 槽位） | ✅ | ✅ | ✅ | 同上 |
| EUIImageEx | ✅ | ✅ | ✅ | 类型识别正确，无暴露字段 |
| EUICircleImage（与 EUIImageEx 不混淆） | ✅ | ✅ | ✅ | 正确识别为 EUICircleImage，未误判 |
| 原生 Button / Text 回归 | ✅ | ✅ | ✅ | 原生类型不受影响 |
| 组件替换（三点菜单） | ✅ 菜单项出现 | ✅ 配置保留 | ✅ 已增强则灰置 | 修复了替换时编辑器闪退（见 6.2） |
| 槽位子节点过滤（m_ 命名不重复收集） | ✅ | ✅ | ✅ | Label 子节点进槽位后不再被重复收集 |

---

## 六、测试中发现的问题与解决（2026-08-22）

### 6.1 增强组件字段不在 Inspector 显示（已解决）

**现象**：挂上 `EUIButtonEx` 后，面板上看不到 `Label` 槽位、状态节点等增强字段。

**根因**：Unity 内置 `ButtonEditor`/`ToggleEditor`/`ImageEditor` 以 `[CustomEditor(typeof(Xxx), true)]`（`editorForChildClasses: true`）注册，会接管所有子类的 Inspector，但只绘制基类字段，子类新增字段被吞掉。

**解决**：为 4 个增强组件各写自定义 Editor，继承 Unity 内置对应 Editor，在 base 绘制后补画增强字段：

| Editor | 基类 | 补画内容 |
|--------|------|---------|
| `EUIButtonExEditor` | `ButtonEditor` | 状态节点、附加图形、Label 槽位 |
| `EUIToggleExEditor` | `ToggleEditor` | On/Off/Disable 节点、Label 槽位 |
| `EUIImageExEditor` | `ImageEditor` | 精灵数组、帧动画、点击区域 |
| `EUICircleImageEditor` | `ImageEditor` | 圆形设置（分段/填充） |

> 依赖：`Ember.UIExtension.Editor.asmdef` 需引用 `UnityEditor.UI` + `UnityEngine.UI`。

### 6.2 组件替换导致编辑器闪退（已解决）

**现象**：三点菜单点「替换为 EUIImageEx」时编辑器直接崩溃。

**根因**：菜单点击发生在 Inspector 的 GUI 绘制期间，此时 `DestroyImmediate` + `AddComponent` 修改组件列表，Unity 的 `PropertyHandler` 缓存失效检查（`TestInvalidateCache`）访问失效缓存 → 原生崩溃（堆栈见 `Crash_2026-08-19_132800891`）。

**解决**：所有替换操作经 `EditorApplication.delayCall` 延迟到 GUI 事件结束后执行（`EUIComponentReplaceMenu.ReplaceDeferred`）。

### 6.3 GMPage 放框架层导致程序集循环依赖（已解决）

**现象**：GMPage 挂增强组件后生成代码报 `CS0234: Ember.UIExtension 不存在`。

**根因**：GMPage 在 `Assets/Ember/UI/Runtime`（`Ember.UI.Runtime` 程序集），而 `Ember.UIExtension.Runtime` 已反向引用它 → 若 UI.Runtime 再引用 uiextension 即成循环依赖。

**解决**：GMPage 整体迁至 `Assets/Game/UI/Runtime/`（业务层，无 asmdef → Assembly-CSharp，可同时引用两个框架程序集）。

### 6.4 ScrollRect 配置异常导致 Prefab 永远 dirty（⚠️ 未解决，已跳过）

**现象**：`m_Scr_Test` 的 Content 位置和垂直 Scrollbar 值一直显示蓝色修改，Apply/Revert 都无法收敛。

**根因**：Viewport 的 RectTransform 配置异常（anchor `(0,0)-(0,0)` + sizeDelta `0×0`，尺寸为 0），而 ScrollRect 带 `[ExecuteAlways]` 在编辑器模式实时执行布局（`UpdateCachedData`/`UpdateScrollbars`），尺寸 0 导致计算出非确定浮点残渣（每次数值不同，实测 0.000477 → 0.000381），序列化后永远 dirty。

**结论**：根因是**该 UI 自身的布局配置错误**（非框架缺陷）。已隐藏该节点跳过测试，后续有具体需求时修正 Viewport 为标准布局（anchor `(0,0)-(1,1)` 充满父节点）即可根治。

### 6.5 时间缩放无效果（已解决）

**现象**：GM 滑块改时间倍率，游戏无变化。

**根因**：`EmberTimeManager.TimeScale` **独立于 `UnityEngine.Time.timeScale`**，只影响框架内部 `DeltaTime`；而业务代码全用 `Time.deltaTime`（Unity 原生），不受影响。

**解决**：GM 滑块同时设置 `UnityEngine.Time.timeScale`（影响所有游戏逻辑）+ `EmberTimeManager.TimeScale`（框架一致）。

### 6.6 新节点不可见（已解决）

**现象**：3 个增强 test 节点在 Play Mode 不可见。

**根因**：GMPage 根节点及 3 个 test 节点的 `m_LocalScale` 被序列化为 `(0,0,0)`（创建脚本 `SetParent(false)` 后 RectTransform scale 未初始化），整体缩放为 0。

**解决**：将 prefab 中 4 处 `m_LocalScale: {x:0,y:0,z:0}` 修正为 `(1,1,1)`。

### 6.7 绑定列表命名校验（新增能力）

在绑定列表「变量名」输入框加入命名规范校验：节点名/变量名与控件类型前缀不匹配时（如 Button 节点叫 `m_Exit` 而非 `m_Btn_Exit`），输入框变橙色高亮 + tooltip 提示，配合第二行的 `✎` 一键重命名使用（`EUIBindingListDrawer.IsBindingNameMismatched`）。

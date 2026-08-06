# com.ember.uiextension 包迁移分析方案

> 分析日期：2026-08-06
> 来源：`com.burner.uiextension@1.0.2`，已复制到 `Packages/com.ember.uiextension/`，全部 148 个 .cs 文件已注释（`////`）
> 当前 asmdef：`Ember.UIExtension.Runtime` + `Ember.UIExtension.Editor`，依赖 `Ember.Core.Runtime` + `Ember.UI.Runtime`

---

## 关键前提：Manager/Pages 目录的处理

uiextension 包中的 `Manager/` 和 `Pages/` 目录（共 10 个文件）对应的是 burner 框架层的 UI Manager 核心。
这些文件**不应迁移到 uiextension 包**，而是作为 **UIManager 结构性重写（Phase A）的参考材料**。
重写后的代码将放入 `Assets/Ember/UI/Runtime/`（即 `Ember.UI.Runtime` 程序集）。

| 文件 | 行数 | 参考价值 | 目标位置 |
|------|------|---------|---------|
| `Manager/BurnerUIManager.cs` | ~600 | 🔴 核心参考 | → `EmberUIManager.cs` (Ember.UI.Runtime) |
| `Manager/PageContext.cs` | ~1200 | 🔴 核心参考 | → `EmberPageContext.cs` (Ember.UI.Runtime) |
| `Manager/GlobalEvents.cs` | ~80 | 🟡 参考 | → `EmberUIEvents.cs` (Ember.UI.Runtime) |
| `Manager/CacheManager.cs` | ~200 | 🟢 部分参考 | → `IUIResourceProvider` 默认实现 |
| `Manager/ILogicResolver.cs` | ~15 | ⬜ 暂不做 | Phase C 绑定代码生成时再考虑 |
| `Pages/GamePage.cs` | ~2000 | 🔴 核心参考 | → `EmberPage.cs` (Ember.UI.Runtime) |
| `Pages/GameUILogic.cs` | ~700 | 🔴 核心参考 | → 合并到扩展后的 `IUIView` |
| `Pages/IUIBehaviour.cs` | ~30 | 🟡 参考 | → 合并到扩展后的 `IUIView` |
| `Pages/GameUIBinding.cs` | ~120 | 🟢 P2 | → Phase C 绑定代码生成 |
| `Pages/GameUIBindingTemplate.cs` | ~50 | 🟢 P2 | → Phase C 绑定代码生成 |

> **结论：Manager/ 和 Pages/ 的文件本次迁移跳过，保留注释状态，在 UIManager Phase A 重写时作为设计参考。**

---

## 目录结构总览（迁移范围 = 138 个文件）

```
Packages/com.ember.uiextension/
├── Runtime/
│   ├── Components/       14 文件    UI 组件封装层
│   ├── UIExt/            35 文件    UI 扩展控件 + Tweener 动画系统
│   ├── Behaviour/         7 文件    附加行为组件
│   ├── SafeArea/          1 文件    安全区域适配
│   ├── NodeScreenShot/    4 文件    节点截图/模糊
│   ├── Utils/             7 文件    工具类（池/扩展/日志）
│   ├── EmberUIBinding.cs  1 文件    绑定组件（已 ember 化）
│   └── Resources/        18 文件    Shader + 纹理资源（非 .cs）
├── Editor/
│   ├── UIExt/            23 文件    各 UIExt 组件的 Inspector
│   ├── Previews/          6 文件    UI 预览系统
│   ├── Settings/          3+4 文件  绑定代码生成配置 + 模板
│   ├── Pages/             2 文件    绑定编辑器
│   ├── Bake/              1 文件    UI 烘焙工具
│   ├── Button/            1 文件    Button Inspector
│   ├── Image/             1 文件    GraphicAnimation Inspector
│   ├── ScrollRect/        1 文件    ScrollView Inspector
│   ├── Validation/        1 文件    Prefab 校验工具
│   ├── EmberUIBindingGenerator.cs 代码生成器（已 ember 化）
│   └── Previews/          .asset + .compute 资源
└── package.json
```

---

## 一、Runtime/Components/ — UI 组件封装层（14 文件）

### 概况

burner 在 Unity 原生 UI 组件基础上封装了一层，统一生命周期（`OnInit`/`OnOpen`/`OnClose`/`OnDestroy`）、事件注入、动画控制、多语言等。

### 逐文件分析

| # | 文件 | 用途 | 必要性 | 风险 | 判定 |
|---|------|------|--------|------|------|
| C1 | **GameUIComponent.cs** | 所有 UI 组件的基类：持有 GameObject/RectTransform，统一生命周期回调，管理 Tweener/Animator/Attachment，提供 Click/LongPress/Drag 事件管道 | 🔴 必须 | 🟡 中：是整个组件体系的根，依赖 burner 的 IUIBehaviour、UITweener、Logger 等 | ✅ 迁移 |
| C2 | **GameButton.cs** | Button 封装：基于 GameUIComponent + Unity Button，统一 click/longPress/pressDown/pressUp 回调 | 🔴 必须 | 🟢 低：逻辑简单，主要是事件转发 | ✅ 迁移 |
| C3 | **GameText.cs** | Text/TMP 封装：统一文本设置、多语言 key 映射、字体样式 | 🔴 必须 | 🟢 低 | ✅ 迁移 |
| C4 | **GameImage.cs** | Image 封装：统一 sprite 设置、材质管理、灰度/圆角 | 🔴 必须 | 🟢 低 | ✅ 迁移 |
| C5 | **GameRawImage.cs** | RawImage 封装 | 🟡 建议 | 🟢 低 | ✅ 迁移 |
| C6 | **GameScrollRect.cs** | ScrollRect 封装：统一滚动事件、item 管理 | 🟡 建议 | 🟡 中：依赖 burner 的扩展方法 | ✅ 迁移 |
| C7 | **GameInputField.cs** | InputField 封装：统一文本输入、焦点管理 | 🟡 建议 | 🟢 低 | ✅ 迁移 |
| C8 | **GameProgressBar.cs** | 进度条封装：统一 value/slider/image | 🟡 建议 | 🟢 低 | ✅ 迁移 |
| C9 | **GameToggle.cs** | Toggle 封装 | 🟡 建议 | 🟢 低 | ✅ 迁移 |
| C10 | **GameToggleGroup.cs** | ToggleGroup 封装 | 🟡 建议 | 🟢 低 | ✅ 迁移 |
| C11 | **GameCanvas.cs** | Canvas 封装：统一 sortingOrder/planeDistance 管理 | 🟡 建议 | 🟢 低 | ✅ 迁移 |
| C12 | **GameTabLoader.cs** | Tab 切换加载器：管理多个 Tab 页面的加载/卸载 | 🟡 建议 | 🟡 中：依赖 PagePreloader + IResourceHandle | ✅ 迁移 |
| C13 | **GameUIContainer.cs** | UI 容器：管理动态列表/网格的 item 复用 | 🟡 建议 | 🟡 中 | ✅ 迁移 |
| C14 | **GamePagePreloader.cs** | 页面预加载器：提前加载页面资源 | 🟢 可选 | 🔴 高：深度依赖 burner 资源系统（IResourceHandle/STTask） | ⚠️ 暂缓 |
| C15 | **GameUIAttachment.cs** | 动态挂件系统：运行时挂载/卸载 UI 子元素 | 🟢 可选 | 🟡 中 | ⚠️ 暂缓 |
| C16 | **BuiTextVisualStyleRef.cs** | 文本视觉样式引用（ScriptableObject） | 🟢 可选 | 🟢 低 | ⚠️ 暂缓 |

### 依赖链

```
GameUIComponent
  ├── 依赖: IUIBehaviour → 需要先定义接口
  ├── 依赖: UITweener（来自 UIExt/Tweener/）
  ├── 依赖: EventTriggerListener（来自 UIExt/）
  ├── 依赖: ObjectPool（Utils/，与 com.ember.basic 的 MemoryPool 重复）
  └── 依赖: Logger（Utils/，需替换为 EmberDebug）

GameButton / GameText / GameImage / ... → 依赖 GameUIComponent
GameTabLoader → 依赖 PagePreloader → 依赖 IResourceHandle → 依赖 STTask
GamePagePreloader → 依赖 STTask + IResourceHandle（burner 资源系统）
```

### 关键风险

1. **IUIBehaviour 接口层**：所有 Components 依赖此接口。burner 原版定义了 `OnInit/OnOpen/OnClose/OnDestroy/OnBind` 等生命周期。ember 已有 `IUIView`（在 `Ember.UI.Runtime`），需要评估是扩展 IUIView 还是新建接口
2. **GameUIComponent 与 burner 资源系统的耦合**：预加载（`GamePagePreloader`）、动态挂件（`GameUIAttachment`）深度依赖 burner 的资源句柄系统（`IResourceHandle` + `STTask`），ember 用的是 `EmberResourceManager` + `UniTask`
3. **Utils 重复**：`ObjectPool`、`ListPool`、`BetterList` 等与 `com.ember.basic` 已有实现重复

---

## 二、Runtime/UIExt/ — UI 扩展控件 + Tweener 动画（35 文件）

### 2.1 ~~Tweener 动画系统（15 文件）~~ → ❌ 已删除

> **删除原因（2026-08-06）**：自研 Tween 引擎与业界标准 DOTween 功能重叠。DOTween 已内置 `DOTweenAnimation` 组件（Inspector 可视化配置 + 编辑模式预览），框架无需自研。框架只保留 `IUITransitionHandler` 动画钩子接口，业务层自行选择 Tween 引擎。
>
> 已删除文件：Runtime/UIExt/Tweener/ (15 .cs) + Editor/UIExt/Tweener/ (11 .cs) = 26 个文件。

### 2.2 UIExt 控件（20 文件）

| # | 文件 | 用途 | 必要性 | 风险 | 判定 |
|---|------|------|--------|------|------|
| E1 | **EventTriggerListener.cs** | 增强版 EventTrigger：统一 click/down/up/enter/exit/drag 事件，比 Unity 原生 EventTrigger 更高效（单个组件处理所有事件类型） | 🔴 必须 | 🟢 低：独立组件，无外部依赖 | ✅ 迁移 |
| E2 | **DragEventTriggerListener.cs** | 拖拽事件增强：Drag/Drop/长按拖拽 | 🟡 建议 | 🟢 低：依赖 EventTriggerListener | ✅ 迁移 |
| E3 | **ButtonEx.cs** | 增强版 Button：长按、双击、CD 冷却、音效绑定 | 🟡 建议 | 🟢 低 | ✅ 迁移 |
| E4 | **ImageEx.cs** | 增强版 Image：支持镜像、灰度、UV 动画、边框 | 🟡 建议 | 🟡 中：依赖 Mirror + Gradient 等 UIExt 组件 | ✅ 迁移 |
| E5 | **ToggleEx.cs** | 增强版 Toggle | 🟢 可选 | 🟢 低 | ✅ 迁移 |
| E6 | **ToggleGroupEx.cs** | 增强版 ToggleGroup | 🟢 可选 | 🟢 低 | ✅ 迁移 |
| E7 | **TabLoader.cs** | Tab 加载器：管理多个 Tab 页面切换 | 🟢 可选 | 🟡 中：依赖 PagePreloader 体系 | ⚠️ 暂缓 |
| E8 | **Gradient.cs** | UI 渐变组件（BaseMeshEffect）：文字/图片顶点色渐变 | 🔴 必须 | 🟢 低：纯 Shader 操作 | ✅ 迁移 |
| E9 | **AdvancedText.cs** | 高级文本：支持图文混排、超链接、表情 | 🟢 可选 | 🔴 高：~800 行，大量正则 + 字符串操作，依赖 HrefText + AdvancedTextImage | ⚠️ 暂缓 |
| E10 | **AdvancedTextImage.cs** | 内嵌图片渲染（配合 AdvancedText） | 🟢 可选 | 🟡 中 | ⚠️ 暂缓 |
| E11 | **HrefText.cs** | 超链接文本解析器：`<url>` 标签解析 | 🟢 可选 | 🟢 低 | ⚠️ 暂缓 |
| E12 | **MergedImage.cs** | 合并贴图渲染：多张小图合成一张大图渲染 | 🟢 可选 | 🔴 高：依赖大量 Shader（14 个 .shader 文件）、编辑器工具 | ⚠️ 暂缓 |
| E13 | **Mirror.cs** | 镜像效果（BaseMeshEffect）：水平/垂直/四分之一镜像 | 🟢 可选 | 🟢 低 | ⚠️ 暂缓（可被 ImageEx 替代） |
| E14 | **MirrorNew.cs** | 新版镜像（替代 Mirror） | 🟢 可选 | 🟢 低 | ❌ 删除（与 Mirror 重复，用 ImageEx 内置镜像替代） |
| E15 | **ContentSizeFitterEx.cs** | 增强版 ContentSizeFitter：支持 min/max 约束 | 🟢 可选 | 🟢 低 | ✅ 迁移 |
| E16 | **RelativeCanvasOrder.cs** | 相对 Canvas 排序：子 Canvas 相对父 Canvas 的 order 偏移 | 🟡 建议 | 🟢 低 | ✅ 迁移 |
| E17 | **UIContainer.cs** | UI 容器基类：统一 size/order/padding 管理 | 🟡 建议 | 🟢 低 | ✅ 迁移 |
| E18 | **MeshOrder.cs** | Mesh 渲染顺序控制 | 🟢 可选 | 🟢 低 | ⚠️ 暂缓 |
| E19 | **UIParticleOrder.cs** | ParticleSystem 在 UI 中的渲染顺序 | 🟢 可选 | 🟡 中：依赖 ParticleSystem | ✅ 迁移 |
| E20 | **AnimationEventReceiver.cs** | Animation 事件接收器：将 Animation Event 转为 C# 回调 | 🟢 可选 | 🟢 低 | ✅ 迁移 |
| E21 | **TransformCopier.cs** | Transform 属性复制器（Editor 辅助，Runtime 也可用） | 🟢 可选 | 🟢 低 | ✅ 迁移 |
| E22 | **BurnerBasicUIExtensions.cs** | UI 相关的 C# 扩展方法集合 | 🟡 建议 | 🟡 中：需要审计哪些方法有用 | ✅ 迁移（审计后精简） |
| E23 | **BurnerUIExtensionAttribute.cs** | UI 组件标记 Attribute | 🟢 可选 | 🟢 低 | ✅ 迁移 |
| E24 | **IBindlessUIBehaviour.cs** | 无绑定 UI 行为接口（用于 SubPage/TabLoader 嵌入） | 🟡 建议 | 🟢 低 | ✅ 迁移 |
| E25 | **ICanvasSortingOrderHandler.cs** | Canvas SortingOrder 变化回调接口 | 🟡 建议 | 🟢 低 | ✅ 迁移 |
| E26 | **PagePreloader.cs** | 页面资源预加载器（独立于 GamePagePreloader 的 MonoBehaviour 版） | 🟢 可选 | 🔴 高：深度依赖 burner 资源系统 | ⚠️ 暂缓 |
| E27 | **ImageFont.cs** | 图片字体：用 Sprite 拼出文字（位图字体） | 🟢 可选 | 🟡 中 | ⚠️ 暂缓 |
| E28 | **TMPMarquee.cs** | TMP 文字跑马灯效果 | 🟢 可选 | 🟢 低 | ⚠️ 暂缓 |

### 2.3 UIExt 子目录

| 子目录 | 文件 | 用途 | 判定 |
|--------|------|------|------|
| **ShowText/** | 5 文件 | 序列帧文字动画系统（逐字弹出/缩放/旋转） | ⚠️ 暂缓（专用功能，~600 行 + Shader） |
| **PackedTexture/** | 2 文件 | 贴图打包：运行时合并多张贴图到一张大图 | ⚠️ 暂缓（专用功能） |
| **Plaque/** | 2 文件 | 铭牌/牌匾系统：固定世界坐标的 UI 标签 | ⚠️ 暂缓（专用功能） |

---

## 三、Runtime/Behaviour/ — 附加行为组件（7 文件）

| # | 文件 | 用途 | 必要性 | 风险 | 判定 |
|---|------|------|--------|------|------|
| B1 | **GraphicAnimation.cs** | UI 图形序列帧动画：sprite 数组播放 | 🟡 建议 | 🟢 低 | ✅ 迁移 |
| B2 | **AnimationProperty.cs** | 动画属性绑定：Animator → UI 属性驱动 | 🟢 可选 | 🟢 低 | ✅ 迁移 |
| B3 | **AutoScale.cs** | 自动缩放：根据参考分辨率自动调整 RectTransform | 🟢 可选 | 🟢 低 | ⚠️ 暂缓（CanvasScaler 已覆盖大部分场景） |
| B4 | **BurnerButton.cs** | 旧版按钮组件（已被 GameButton 替代） | ❌ 删除 | — | ❌ 删除 |
| B5 | **CircleImage.cs** | 圆形/环形 Image（重写 OnPopulateMesh） | 🟡 建议 | 🟢 低 | ✅ 迁移 |
| B6 | **RoundedImageModifier.cs** | 圆角矩形 Image 修改器 | 🟡 建议 | 🟢 低 | ✅ 迁移 |
| B7 | **UIPolygonRaycast.cs** | 多边形精确 Raycast（替代矩形 Raycast） | 🟢 可选 | 🟢 低 | ✅ 迁移 |

---

## 四、Runtime/SafeArea/ — 安全区域适配（1 文件）

| # | 文件 | 用途 | 必要性 | 风险 | 判定 |
|---|------|------|--------|------|------|
| S1 | **BurnerSafeArea.cs** | iPhone X+ 刘海屏/底部横条安全区域适配，自动调整 RectTransform padding | 🔴 必须 | 🟢 低：独立组件，`ILayoutSelfController` 实现，~250 行 | ✅ 迁移 |

---

## 五、Runtime/NodeScreenShot/ — 节点截图/模糊（4 文件）

| # | 文件 | 用途 | 必要性 | 风险 | 判定 |
|---|------|------|--------|------|------|
| N1 | **NodePostProcessManager.cs** | RenderTexture 截图 + 模糊：Raycast → 特定 UI 节点 → 渲染到 RT → 模糊 | 🟢 可选 | 🔴 高：~500 行，深度依赖 burner 基础库 + Camera/RenderTexture 管理 | ❌ 删除（超出 UI 框架范围，属于渲染特效） |
| N2 | **SoftMask.cs** | UI 软遮罩（Alpha Mask） | 🟢 可选 | 🟡 中 | ❌ 删除（可用 Unity 内置 Mask/RectMask2D 替代，或推荐使用第三方 SoftMask 包） |
| N3 | **LWSoftMask.cs** | 轻量软遮罩 | 🟢 可选 | 🟡 中 | ❌ 删除 |
| N4 | **SoftMaskDisableListener.cs** | 软遮罩禁用监听器 | 🟢 可选 | 🟢 低 | ❌ 删除 |

> 注意：如果未来有强烈需求，可以引入成熟的第三方 SoftMask 方案（如 Coffee SoftMask），而不是从 burner 移植这套自研实现。

---

## 六、Runtime/Utils/ — 工具类（7 文件）

| # | 文件 | 用途 | 必要性 | 风险 | 判定 |
|---|------|------|--------|------|------|
| U1 | **ObjectPool.cs** | 通用对象池 `ObjectPool<T>` | ❌ 删除 | — | ❌ 与 `com.ember.basic` 的 `MemoryPool<T>` 重复 |
| U2 | **ListPool.cs** | `List<T>` 池 | ❌ 删除 | — | ❌ 与 `com.ember.basic` 的 `ListPool<T>` 重复 |
| U3 | **BetterList.cs** | 优化版 List（Clear 不释放 Buffer） | ❌ 删除 | — | ❌ `com.ember.basic` 的 `QuickQueue<T>` + `ListPool` 已覆盖此场景 |
| U4 | **Logger.cs** | UI 包内部日志 | ❌ 删除 | — | ❌ 全局统一用 `EmberDebug` |
| U5 | **RectTransformExtensions.cs** | RectTransform 扩展方法 | 🟡 建议 | 🟢 低：~50 行 | ✅ 迁移（审计去重后） |
| U6 | **BindingExtensions.cs** | UI 绑定相关扩展方法 | 🟢 可选 | 🟢 低 | ⚠️ 暂缓（Phase C 绑定代码生成时再处理） |
| U7 | **StringUtils.cs** | 字符串工具 | 🟢 可选 | 🟢 低 | ❌ 删除（`com.ember.basic` StringExtension 已覆盖） |

---

## 七、Runtime/EmberUIBinding.cs — 绑定组件（1 文件）

| 文件 | 用途 | 必要性 | 风险 | 判定 |
|------|------|--------|------|------|
| **EmberUIBinding.cs** | UI 控件绑定：挂载到预制体，记录控件名称→GameObject 映射。编辑器工具读取此组件自动生成 C# 访问代码 | 🟢 P2 | 🟢 低：已 ember 化（Ember.UIExtension 命名空间，中文注释），代码完整但未激活 | ⚠️ 暂缓到 Phase C |

---

## 八、Editor/ — 编辑器工具

### 8.1 概况

Editor 包含 35 个 .cs 文件 + 4 个 .tpl 模板 + 1 个 .compute + 1 个 .asset + 14 个 Shader 资源。大部分是各 Runtime 组件的 CustomEditor/PropertyDrawer。

### 8.2 逐目录分析

| 目录 | 文件数 | 用途 | 依赖的 Runtime | 判定 |
|------|--------|------|---------------|------|
| **UIExt/Tweener/** | 10 | 各 Tween 组件的 PropertyDrawer/Editor | Tweener/ | ✅ 与 Runtime Tweener 同步迁移 |
| **UIExt/ 其余** | 13 | ButtonEx/ImageEx/Gradient/MergedImage/ShowText 等的 Inspector | 对应 Runtime 组件 | ✅ 与对应 Runtime 组件同步迁移 |
| **Previews/** | 6 | Inspector/ProjectWindow 的 UI 预览面板（TMP 材质预览、UI 渲染预览） | — | ⚠️ 暂缓（需要 UIPreview.compute + UIPreviewConfig.asset） |
| **Settings/** | 3+4 | 绑定代码生成配置（ScriptableObject）+ 4 个 C# 模板文件 | EmberUIBinding | ⚠️ 暂缓到 Phase C |
| **Pages/** | 2 | 绑定编辑器核心：GameUIBindingEditor + Utility | EmberUIBinding | ⚠️ 暂缓到 Phase C |
| **Bake/** | 1 | UI 烘焙工具（Prefab → 优化后 Prefab） | — | ⚠️ 暂缓（高级优化工具） |
| **Button/** | 1 | BurnerButton 的 Inspector | BurnerButton（已删除） | ❌ 随 Runtime 删除 |
| **Image/** | 1 | GraphicAnimation 的 Inspector | GraphicAnimation | ✅ 随 GraphicAnimation 同步迁移 |
| **ScrollRect/** | 1 | GameScrollRect 的 Inspector | GameScrollRect | ✅ 随 GameScrollRect 同步迁移 |
| **Validation/** | 1 | Prefab 校验工具：检查 Prefab 上的组件配置是否合法 | — | 🟢 低优先级 | ⚠️ 暂缓 |
| **EmberUIBindingGenerator.cs** | 1 | 代码生成器入口（已 ember 化，注释状态） | EmberUIBinding | ⚠️ 暂缓到 Phase C |

---

## 九、汇总统计

### 迁移决策分布

| 决策 | Runtime | Editor | 总计 |
|------|---------|--------|------|
| ✅ 迁移 | 25 | 7 | **32** |
| ⚠️ 暂缓（P2/P3） | 19 | 13 | **32** |
| ❌ 删除 | 24 | 12 | **36** |
| 🔄 作为 UIManager 重写参考（不迁移） | 10 | — | **10** |
| Shader/资源文件 | 18 | 3 | **21** (非 .cs) |
| **合计** | **96** | **35** | **131** .cs + 21 资源 |

> 变更记录：Tweener 15 Runtime + 11 Editor 从 ✅ 迁移 → ❌ 删除。

### 分批迁移计划

```
Phase 1: 独立控件层（本次迁移，预计 18 文件）
  ├── 第 1 层：零依赖工具                     18 文件
  └── 无 Tweener，从 EventTriggerListener 开始

Phase 2: 内部依赖控件（本次迁移，预计 7 文件）```
  ├── UIExt/EventTriggerListener     事件触发器
  ├── UIExt/DragEventTriggerListener 拖拽事件
  ├── UIExt/Gradient                 渐变组件
  ├── UIExt/ContentSizeFitterEx      增强布局
  ├── UIExt/RelativeCanvasOrder      Canvas排序
  ├── UIExt/UIContainer              容器基类
  ├── UIExt/UIParticleOrder          粒子排序
  ├── UIExt/AnimationEventReceiver   动画事件
  ├── UIExt/TransformCopier          属性复制
  ├── UIExt/BurnerBasicUIExtensions  扩展方法集合
  ├── UIExt/BurnerUIExtensionAttribute 标记Attribute
  ├── UIExt/IBindlessUIBehaviour     无绑定接口
  ├── UIExt/ICanvasSortingOrderHandler 排序回调接口
  ├── Behaviour/GraphicAnimation     序列帧动画
  ├── Behaviour/AnimationProperty    动画属性绑定
  ├── Behaviour/CircleImage          圆形图
  ├── Behaviour/RoundedImageModifier 圆角矩形
  └── Behaviour/UIPolygonRaycast     多边形Raycast

Phase 3: 组件封装层（本次迁移，预计 12 文件）
  ├── Components/GameUIComponent     组件基类 ← 🔴 最高风险
  ├── Components/GameButton          按钮
  ├── Components/GameText            文本
  ├── Components/GameImage           图片
  ├── Components/GameRawImage        Raw图
  ├── Components/GameScrollRect      滚动列表
  ├── Components/GameInputField      输入框
  ├── Components/GameProgressBar     进度条
  ├── Components/GameToggle          开关
  ├── Components/GameToggleGroup     开关组
  ├── Components/GameCanvas          Canvas
  ├── Components/GameUIContainer     容器
  └── Components/GameTabLoader       Tab加载器

Phase 4: 通用组件（本次迁移，预计 8 文件）
  ├── Behaviour/GraphicAnimation     序列帧
  ├── UIExt/ButtonEx                 增强按钮
  ├── UIExt/ImageEx                  增强图片
  ├── UIExt/ToggleEx/ToggleGroupEx   增强开关
  ├── SafeArea/BurnerSafeArea        安全区域
  └── 对应的 Editor 文件（同步迁移）

Phase 5: 暂缓项（后续 Phase B/C）
  ├── AdvancedText/HrefText/ImageFont  高级文本（Phase B）
  ├── MergedImage/PackedTexture       贴图系统（Phase B）
  ├── ShowText/TMPMarquee             特效文字（Phase B）
  ├── GamePagePreloader/PagePreloader 预加载（需要资源系统配合）
  ├── GameUIAttachment                动态挂件
  ├── NodeScreenShot/                 ❌ 已删除
  ├── Mirror/MirrorNew                ❌ 已删除
  ├── EmberUIBinding + 代码生成器     Phase C
  └── Editor Previews/Settings/Pages  Phase C
```

---

## 十、风险矩阵

| 风险等级 | 文件/模块 | 具体风险 | 缓解措施 |
|----------|-----------|---------|---------|
| 🔴 高 | `GameUIComponent` | 组件体系根节点，改写 IUIBehaviour→IUIView 适配时影响所有子类 | 先定义接口再迁移实现，保持 burner 原版行为不变 |
| 🔴 高 | `GamePagePreloader` / `PagePreloader` | 深度耦合 burner 资源系统 (STTask + IResourceHandle) | Phase 5 暂缓，等 Resource 模块 Handle 系统完善后再做 |
| 🔴 高 | `NodePostProcessManager` | 渲染管线操作，跨平台兼容性未知 | 直接删除，推荐使用成熟第三方方案 |
| 🟡 中 | `UITweener` | `[ExecuteAlways]` + 协程驱动，性能敏感 | 保持原始实现，不做架构改动 |
| 🟡 中 | `GameTabLoader` / `GameUIContainer` | 依赖预加载和资源系统 | 暂时注释掉预加载相关代码，先迁移基础功能 |
| 🟡 中 | `BurnerBasicUIExtensions` | 方法数量未知，可能大量无用代码 | 逐方法审计，只保留有实际价值的部分 |
| 🟡 中 | `EventTriggerListener` → `GameUIComponent` 双向依赖 | 循环依赖风险 | 理清依赖方向：EventTriggerListener 为独立组件，GameUIComponent 可选使用它 |
| 🟢 低 | Tweener 15 文件 | 独立系统，依赖少 | 只需替换 Logger + namespace |
| 🟢 低 | 大部分 UIExt 控件 | 独立组件，MonoBehaviour | 标准迁移流程 |
| 🟢 低 | Utils 重复项 | 直接删除，ember 已有替代 | 确认无隐藏依赖后删除 |

---

## 十一、与 UIManager 重写的协调

```
推荐顺序：

1. 先迁移 uiextension Phase 1-2（Tweener + UIExt 独立控件）
   → 不依赖 UI Manager，可以独立完成

2. 再做 UIManager 重写 Phase A（参考 Manager/Pages 注释代码）
   → 此时 Tweener 系统已就绪，UIManager 可以直接使用

3. 然后迁移 uiextension Phase 3-4（Components 封装层）
   → Components 依赖 UIManager 的生命周期接口（IUIView 扩展版）

4. 最后 Phase 5 暂缓项
   → 等整体稳定后再逐步激活
```

---

## 十二、迁移规范（复用 basic/extensions 经验）

| 规则 | burner 原始 | ember 目标 |
|------|------------|-----------|
| 命名空间 | `Burner.UIExtension` | `Ember.UIExtension`（Runtime）、`Ember.UIExtension.Editor`（Editor） |
| 日志 | `Burner.Logger.*` | `EmberDebug.Log/LogWarning/LogError` |
| 资源加载 | `STTask` / `IResourceHandle` / `CacheManager` | `UniTask` / `EmberResourceManager` / `IResourceProvider`（暂缓项先注释） |
| 对象池 | `ObjectPool<T>` / `ListPool<T>` | `MemoryPool<T>` / `ListPool<T>`（来自 `com.ember.basic`） |
| 类名前缀 | `Burner*` / `Game*` | `Ember*` 或保持 `Game*`（组件类名保留，方便预制体迁移） |
| 版权头 | burner copyright | ember copyright |
| `[HasGC]` / `[NoGC]` | 来自 `Burner.Basic` | 来自 `com.ember.basic`（`Ember.Basic.Attributes`） |
| asmdef 引用 | `Burner.Basic` / `Burner.Extensions` | `com.ember.basic` / `com.ember.extensions` |
| XML 文档 | 中文注释 | 保持中文，泛型用 `《》` |

### 关于类名保留

Components 目录下的 `GameButton`、`GameText` 等类名在 burner 项目中已被广泛使用在预制体上。
为了降低业务层预制体迁移成本，**组件类保留 `Game` 前缀**，不改为 `Ember` 前缀。
其他 UI 框架特有的类（如 `BurnerUIManager`、`BurnerSafeArea`）统一改为 `Ember` 前缀。

---

## 十三、预计工作量

| 阶段 | 文件数 | 预计耗时 | 说明 |
|------|--------|---------|------|
| Phase 1: Tweener + 基础 | ~20 | 1-2 天 | 大量文件但逻辑简单，主要是 namespace + Logger 替换 |
| Phase 2: 扩展控件核心 | ~18 | 1-2 天 | 组件独立，迁移简单 |
| Phase 3: 组件封装层 | ~12 | 2-3 天 | GameUIComponent 是核心难点，需要接口适配 |
| Phase 4: 通用组件 + Editor | ~10 | 1 天 | 简单迁移 |
| Phase 5: 暂缓项 | ~30 | P2/P3 | 不在本次范围内 |
| **合计** | **~60** | **5-8 天** | |

---

> **下一步：请确认以上方案。重点关注——**
> 1. Manager/Pages 不迁移、作为 UIManager 重写参考 → 是否同意？
> 2. NodeScreenShot 直接删除 → 是否同意？
> 3. AdvancedText/MergedImage/ShowText 等暂缓 → 是否同意？
> 4. 组件类保留 `Game` 前缀 → 是否同意？
> 5. Phase 1→4 的优先级排序 → 是否同意？

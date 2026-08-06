# com.ember.uiextension 学习路径

> 按依赖深度排序，从零依赖工具开始，逐层深入。每学完一个文件来问 Claude 分析可否迁移。

---

## 使用方式

1. 在 IDE 中打开文件，阅读代码
2. 回到 Claude，告诉它你读了哪个文件
3. Claude 会分析：用途、能否迁移、风险、适配变更清单

---

## 第 1 层：零依赖工具（18 文件）

这些文件只依赖 Unity 引擎，不依赖包内任何其他文件。

> ~~Tweener 动画系统（15 文件）已删除 —— 改用 DOTween，框架只保留 `IUITransitionHandler` 动画钩子。~~

### 独立 UI 控件（11）

| # | 文件 | 一句话 |
|---|------|--------|
| 1 | `Runtime/UIExt/EventTriggerListener.cs` | 增强版 EventTrigger：单组件处理所有 UI 事件类型 |
| 2 | `Runtime/UIExt/Gradient.cs` | 文字/图片顶点色渐变（BaseMeshEffect） |
| 3 | `Runtime/UIExt/AnimationEventReceiver.cs` | Animation 事件 → C# 回调 |
| 4 | `Runtime/UIExt/TransformCopier.cs` | Transform 属性复制（对齐工具） |
| 5 | `Runtime/UIExt/ContentSizeFitterEx.cs` | 增强 ContentSizeFitter：min/max 约束 |
| 6 | `Runtime/UIExt/RelativeCanvasOrder.cs` | Canvas sortingOrder 相对父级偏移 |
| 7 | `Runtime/UIExt/UIParticleOrder.cs` | ParticleSystem 在 UI 中的渲染排序 |
| 8 | `Runtime/UIExt/UIContainer.cs` | UI 容器基类：size / order / padding |
| 9 | `Runtime/UIExt/IBindlessUIBehaviour.cs` | 无绑定 UI 行为接口（~15 行） |
| 10 | `Runtime/UIExt/ICanvasSortingOrderHandler.cs` | Canvas 排序变化回调接口（~10 行） |
| 11 | `Runtime/UIExt/BurnerUIExtensionAttribute.cs` | UI 组件标记 Attribute（~10 行） |

### 行为组件（5）

| # | 文件 | 一句话 |
|---|------|--------|
| 12 | `Runtime/Behaviour/GraphicAnimation.cs` | Image 序列帧动画播放 |
| 13 | `Runtime/Behaviour/AnimationProperty.cs` | Animator → UI 属性驱动 |
| 14 | `Runtime/Behaviour/CircleImage.cs` | 圆形/环形 Image（重写 OnPopulateMesh） |
| 15 | `Runtime/Behaviour/RoundedImageModifier.cs` | 圆角矩形 Image 修改器 |
| 16 | `Runtime/Behaviour/UIPolygonRaycast.cs` | 多边形精确 Raycast（替代矩形） |

### 适配 & 工具（2）

| # | 文件 | 一句话 |
|---|------|--------|
| 17 | `Runtime/SafeArea/BurnerSafeArea.cs` | iPhone 刘海屏安全区域适配 |
| 18 | `Runtime/Utils/RectTransformExtensions.cs` | RectTransform 扩展方法 |

---

## 第 2 层：有内部依赖的控件（7 文件）

依赖第 1 层的某些组件，需先理解被依赖方。

| # | 文件 | 依赖谁 |
|---|------|--------|
| 19 | `Runtime/UIExt/DragEventTriggerListener.cs` | EventTriggerListener (#1) |
| 20 | `Runtime/UIExt/ButtonEx.cs` | EventTriggerListener (#1) |
| 21 | `Runtime/UIExt/ToggleEx.cs` | EventTriggerListener (#1) |
| 22 | `Runtime/UIExt/ToggleGroupEx.cs` | ToggleEx (#21) |
| 23 | `Runtime/UIExt/ImageEx.cs` | Gradient (#2) |
| 24 | `Runtime/UIExt/MeshOrder.cs` | 独立但专用 |
| 25 | `Runtime/UIExt/BurnerBasicUIExtensions.cs` | 可能引用多个第 1 层组件 |

---

## 第 3 层：组件封装体系（16 文件）

所有组件继承 `GameUIComponent`，统一生命周期。**先读接口，再读基类。**

| # | 文件 | 依赖谁 |
|---|------|--------|
| 26 | `Runtime/Pages/IUIBehaviour.cs` | ⭐ 先读这个！组件生命周期接口 |
| 27 | `Runtime/Components/GameUIComponent.cs` | IUIBehaviour + EventTriggerListener |
| 28 | `Runtime/Components/GameButton.cs` | GameUIComponent |
| 29 | `Runtime/Components/GameText.cs` | GameUIComponent |
| 30 | `Runtime/Components/GameImage.cs` | GameUIComponent |
| 31 | `Runtime/Components/GameRawImage.cs` | GameUIComponent |
| 32 | `Runtime/Components/GameToggle.cs` | GameUIComponent |
| 33 | `Runtime/Components/GameToggleGroup.cs` | GameUIComponent |
| 34 | `Runtime/Components/GameInputField.cs` | GameUIComponent |
| 35 | `Runtime/Components/GameProgressBar.cs` | GameUIComponent |
| 36 | `Runtime/Components/GameScrollRect.cs` | GameUIComponent |
| 37 | `Runtime/Components/GameCanvas.cs` | GameUIComponent |
| 38 | `Runtime/Components/GameUIContainer.cs` | GameUIComponent |
| 39 | `Runtime/Components/GameTabLoader.cs` | GameUIComponent |
| 40 | `Runtime/Components/GameUIAttachment.cs` | GameUIComponent（深度依赖 burner 资源系统，了解思路即可） |
| 41 | `Runtime/Components/GamePagePreloader.cs` | GameUIComponent（深度依赖 burner 资源系统，了解思路即可） |

---

## 第 4 层：Manager + Page 核心（7 文件）

包内最复杂的部分。**不作为迁移目标**，作为 UIManager Phase A 重写的设计参考。通读理解设计思路，不必逐行深究。

| # | 文件 | 行数 | 核心关注点 |
|---|------|------|-----------|
| 42 | `Runtime/Manager/GlobalEvents.cs` | ~80 | 全局事件转发机制 |
| 43 | `Runtime/Manager/ILogicResolver.cs` | ~15 | 类型发现接口（可跳过） |
| 44 | `Runtime/Manager/CacheManager.cs` | ~200 | 资源缓存策略 |
| 45 | `Runtime/Manager/PageContext.cs` | ~1200 | MainPage + Popup 栈关系 / SortingOrder 计算 / HideLowerPage 级联 |
| 46 | `Runtime/Pages/GameUILogic.cs` | ~700 | 页面逻辑基类（对应我们的 IUIView 扩展） |
| 47 | `Runtime/Pages/GamePage.cs` | ~2000 | 页面生命周期核心：分阶段加载 / 安全遍历 / 挂起操作队列 |
| 48 | `Runtime/Manager/BurnerUIManager.cs` | ~600 | UI 管理器入口：Canvas 管理 / Update 分发 / FrameTimeBudget |

---

## 跳过区（不需要学）

以下文件确认删除或暂缓到 Phase B/C，不需要花时间：

### 确认删除（与 ember 已有实现重复）

| 文件 | 原因 |
|------|------|
| `Runtime/Utils/ObjectPool.cs` | `com.ember.basic` 已有 `MemoryPool<T>` |
| `Runtime/Utils/ListPool.cs` | `com.ember.basic` 已有 `ListPool<T>` |
| `Runtime/Utils/BetterList.cs` | `com.ember.basic` 已有 `QuickQueue<T>` |
| `Runtime/Utils/Logger.cs` | 全局统一用 `EmberDebug` |
| `Runtime/Utils/StringUtils.cs` | `com.ember.basic` 已有 `StringExtension` |
| `Runtime/Behaviour/BurnerButton.cs` | 已被 `GameButton` (#43) 替代 |
| `Runtime/UIExt/Mirror.cs` | 被 `ImageEx` (#38) 内置镜像替代 |
| `Runtime/UIExt/MirrorNew.cs` | 与 `Mirror` 重复 |
| `Runtime/NodeScreenShot/` (4 文件) | 渲染特效，超出 UI 框架范围 |

### 暂缓到未来阶段

| 文件 | 暂缓原因 | 计划 |
|------|---------|------|
| `Runtime/Utils/BindingExtensions.cs` | 绑定代码生成时才需要 | Phase C |
| `Runtime/Behaviour/AutoScale.cs` | CanvasScaler 已覆盖大部分场景 | Phase B |
| `Runtime/UIExt/AdvancedText.cs` | 图文混排 ~800 行，复杂度高 | Phase B |
| `Runtime/UIExt/AdvancedTextImage.cs` | 配合 AdvancedText | Phase B |
| `Runtime/UIExt/HrefText.cs` | 超链接解析 | Phase B |
| `Runtime/UIExt/MergedImage.cs` | 依赖 14 个 Shader | Phase B |
| `Runtime/UIExt/TabLoader.cs` | 依赖 PagePreloader | Phase B |
| `Runtime/UIExt/PagePreloader.cs` | 深度依赖 burner 资源系统 | Phase B |
| `Runtime/UIExt/ImageFont.cs` | 位图字体 | Phase B |
| `Runtime/UIExt/TMPMarquee.cs` | 跑马灯 | Phase B |
| `Runtime/UIExt/ShowText/` (5 文件) | 序列帧文字动画 | Phase B |
| `Runtime/UIExt/PackedTexture/` (2 文件) | 贴图打包 | Phase B |
| `Runtime/UIExt/Plaque/` (2 文件) | 铭牌系统 | Phase B |
| `Runtime/EmberUIBinding.cs` | 绑定代码生成 | Phase C |
| `Editor/` (35 文件) | 等 Runtime 迁移完成后按需激活 | Phase C |

---

## 学习进度追踪

```
第 1 层  ✅ 已完成 — 18 文件全部迁移激活
第 2 层  ✅ 已完成 — 7 文件全部迁移激活（含 DragEventTriggerListener）
第 3 层  ✅ 已完成 — 8 文件新建激活（精简版），原 16 中跳过 8（暂缓/太重）
第 4 层  0 /  7  ⬜ 仅参考，不迁移（UIManager Phase A 已参考重写完成）

总计    33 / 48  迁移激活 | 7 仅参考 | 8 暂缓/跳过
```

---

> **L1-L3 已完成。L4（Manager/Page 参考材料）已在 UIManager Phase A 中充分吸收。**

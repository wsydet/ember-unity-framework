# 缓冲进度条

`Ember.UIExtension.EUIBufferedProgressBar` 是展示专用 MonoBehaviour companion。主条立即表达真实数据，慢尾与白条分别反馈下降；不计算伤害，不依赖 Boss、玩家、业务模板或新的 Manager。旧 `EUIProgressBar` 包装及 `ProgressBar → Slider` Binding 映射保持原有行为。

## 制作与正式绑定

通过 `Ember/UI/UI 开发中心` 创建/维护 Page 或 Item，在适合的节点添加 companion。绑定列表显式选 `Extension / EUIBufferedProgressBar`，保存 Prefab 后通过中心重新生成。生成字段为 `Ember.UIExtension.EUIBufferedProgressBar`。它的 `AutoDetect=false`，不会抢走 Slider、Image 或 ImageEx 的旧自动绑定；不要手写 `.Binding.cs`。

每层只填 Slider 或 Image 其中之一。各层完整轨道同向对齐，内部没有相对旋转、斜切、竞争的 Layout/Animator 或覆盖排序子 Canvas。Canvas 整体缩放/旋转可以使用。推荐绘制顺序：

```text
Track（完整填充区域）
  Background
  Slow（完整 Filled Image，或独立 Slider）
  White（专用 Simple/Sliced Image）
  Main（完整 Filled Image，或独立 Slider）
  Text（可选 TMP_Text / TMPEx）
```

White Image 必须为 White Track 的直接子节点。White Track 对齐主条完整填充区域，不能把已经缩短的 fillRect 当完整轨道。组件独占 White 的矩形、enabled 和实例 color；`raycastTarget=false`、`preserveAspect=false`。白条是真实的损失矩形 `[真实进度, 白尾端]`，不会从 0 开始盖住主条。四方向通过锚点变化实现，另一轴 stretch、offset 为 0。RGB 与基础 alpha 使用作者颜色，结束后隐藏并恢复基础色。

主条/慢尾支持矩阵：

| 目标 | 支持范围 | 无效配置 |
| --- | --- | --- |
| Slider | 有限 min < max，wholeNumbers=false，interactable=false，有 fillRect；四方向；实际值映射到原 min/max，SetValueWithoutNotify。内部 Filled Image 须按下表匹配方向且 preserveAspect=false | 缺失 fillRect、非法范围、整数/交互 Slider、内部 Filled 的径向填充/方向不符/保持比例均诊断后停止写该目标 |
| Image | Filled Horizontal / Vertical，fillOrigin 0/1，preserveAspect=false | Simple、Sliced、Tiled 主条以及 radial 不支持；旧包装支持行为不变 |
| White | 独立 Simple/Sliced Image 与完整轨道 | 复用主/慢 Graphic、fillRect 或轨道，错误父子关系，布局/Animator 竞争均不支持 |

组件不订阅 Slider.onValueChanged，也不把拖动或外部赋值当业务输入。不要继续用旧包装、Animator 或其他脚本写同一目标。极大有限 Slider 范围使用 double 映射并修复原生 float 范围差溢出时的显示锚点；业务读取真实值使用组件属性。

Slider 的 fillRect 使用 Simple/Sliced Image 时沿用原生锚点伸缩；若使用 Filled Image，原生 Slider 只更新 fillAmount，不会替作者同步 fillMethod/fillOrigin，因此必须配置：

| Slider.direction | 内部 Filled fillMethod | fillOrigin |
| --- | --- | --- |
| LeftToRight | Horizontal | 0（Left） |
| RightToLeft | Horizontal | 1（Right） |
| BottomToTop | Vertical | 0（Bottom） |
| TopToBottom | Vertical | 1（Top） |

这项校验同样适用于慢尾 Slider；内部 Filled 不支持 radial 或 preserveAspect。组件仅报告诊断，不自动改作者的方向、图形类型、origin、范围或布局。主目标无效时取消两类反馈并清退已借入的白图，保留真实数据及文字，停止目标写入；仅慢尾无效时停止慢尾写入/计时，主条与有效白图仍独立工作。无效目标本身保留作者当前图形，不能把它的残留显示当有效反馈。修正配置后调用 SyncImmediate 或 ConfigureTargets，按最新真实值恢复，避免同值 Set 的去重阻止重新校验。

## 数值与配置

```csharp
bar.SetValue(100, 100, immediate: true); // 新 owner 初始化
bar.SetValue(60, 100);                  // 主条和文字立即到 60
bar.SlowBufferEnabled = true;
bar.WhiteMode = EUIProgressWhiteMode.Fade; // Disabled / Shrink / Fade
bar.TextMode = EUIProgressTextMode.CurrentOverMax;
bar.TextPrecision = 0;
bar.SyncImmediate();                    // 保留数据，结束反馈
```

| API / 属性 | 合同 |
| --- | --- |
| SetNormalized(value, immediate=false) | normalized 模式，CurrentValue=n，MaxValue=1，n 钳制 0–1 |
| SetValue(current, maximum, immediate=false) | 有效上限 >0，current 钳制到 0–maximum；用 double 除法取得 n |
| ConfigureTargets(mainSlider, mainImage, slowSlider, slowImage, whiteImage, whiteTrack, progressText) | 借入引用，清退旧白条，重绑同步；可在 inactive 时配置 |
| SyncImmediate() | 取消两效果，尾端同步 n，白条隐藏并还原色，保留真实数据 |
| Clear() | 可复用；真实值恢复 0/1，文字刷新，取消反馈 |
| CurrentValue / MaxValue / NormalizedValue | 规范化后的真实输入；不从尾条反推 |
| IsAnimating / Diagnostic | 是否仍有反馈和当前配置/格式诊断 |
| SlowBufferEnabled / WhiteMode | 只重置对应效果；打开/换模式后只对未来下降工作 |
| SlowDelay / SlowDuration / WhiteDelay / WhiteDuration | 更改只同步对应效果；负数/NaN/Infinity 按 0，有限正数最大 3600 秒 |
| UseUnscaledTime | 默认 true；改时钟同步两效果 |
| TextMode / TextPrecision / CustomTextFormat / ProgressText / RefreshText() | 改文字立即重刷，不重启动画；可不配文字 |

非有限 current/normalized 按 0；非有限或非正 maximum 得到 0/0、n=0。下次有效上限可恢复，不除零。首次输入、inactive/disabled 输入、输入模式或最大值变化都立即同步，不制造伤害反馈。最大值是业务数据，Slider 的 min/max 只是显示范围。

## 动画与六组合

默认 Slow 开启、White=Shrink，慢尾延迟 .35 秒、时长 .65 秒；白条延迟 .08 秒、时长 .22 秒，均线性、非缩放时间。每实例只有一个 LateUpdate，两套独立有限状态；没有全局更新、异步任务或完成回调。默认 timeScale=0 仍播放；UseUnscaledTime=false 时随缩放时间暂停。跨过延迟的一帧会把剩余 dt 用于动画，零时长在延迟结束立即完成，延迟和时长均为 0 时在 API 返回前结束。

100→60 的准确显示：

| Slow | White | 过程 | 终态 |
| --- | --- | --- | --- |
| 关 | Disabled | 只有主条即时 .6 | 无反馈区 |
| 开 | Disabled | 慢尾保留 1，.35 秒后收至 .6 | 1 秒后 b=.6 |
| 关 | Shrink | 白色损失矩形 [.6,1] 停留后收缩 | .30 秒后隐藏 |
| 关 | Fade | [.6,1] 保持几何，alpha 下降 | .30 秒后隐藏并恢复基础色 |
| 开 | Shrink | 白区先收，逐步露出橙色慢尾 | .30 秒后仅慢尾，1 秒后无反馈 |
| 开 | Fade | 白区淡出并露出慢尾 | 同上，两效果不争写 alpha |

连续下降从各自当前显示尾端重启独立延迟，不从历史满值重来。同帧 100→80→50 合并 hold，真实值依次立即更新；重复同值不重启。Shrink 从当前白尾继续收缩。Fade 尚在 hold 时可合并损失；一旦开始淡出再次受击，旧白区退休，只显示 `[新真实值, 旧真实值]`，不把已淡区恢复为不透明。每次回血/回满同步两效果，取消全部旧损失。

缺失可选层只停该效果；主条无效时保留真实数据与文字并停止反馈。引用冲突先校验再写，无效白条不会碰主/慢 Graphic。诊断状态变化时告警，不逐帧刷相同日志。复杂 Canvas 的实际排序仍需按真实画面核对。

## 真实进度文字

文字可选。None 不写；Percent 默认 0 位，CurrentOverMax 显示 `60 / 100`；TextPrecision 钳制 0–4，固定小数位、InvariantCulture、AwayFromZero 四舍五入。几何和业务数值不因显示精度改变。动画中仍显示最新真实数值。

Custom 使用 .NET composite format：`{0}=current`、`{1}=maximum`、`{2}=normalized`、`{3}=percent`。例如 `{0:0.0} / {1:0.0}` 或 `{3:0}%`。自定义项自行指定精度；空/空白/非法格式回退内置 Percent，并提供诊断。字符串仅在数据或文字配置变化时生成，不在尾条动画每帧格式化。

TMPEx 的 Key 必须为空；Key 非空时只提示并跳过，不自动清 Key。作者显式清 Key 后调用 RefreshText。Key 空时用 SetSource 更新实例原文，语言刷新仍保留数字；普通 TMP_Text 写 text。组件不更改字体、材质、皮肤或全局翻译源。自定义格式语言变化由页面更新配置再 RefreshText。

## 页面、池化与销毁

首次 inactive Configure/Set 不依赖 Awake；Awake/OnEnable 不覆盖已给数据。GameObject 隐藏或组件禁用时同步反馈，重新显示不重播。页面使用 CanvasGroup 隐藏不一定产生 OnDisable，因此 OnHide 显式调用 SyncImmediate。

```csharp
// 动态 Item 的 Bind 中；涵盖 never-active 回池。
lease.Track(bar.Clear);
bar.SetValue(lease.Data.Current, lease.Data.Maximum, immediate: true);
// 页面 OnHide：bar.SyncImmediate();
```

Clear 不依赖 Unity 生命周期回调；新 owner 显示前重新设置值。仅 CanvasGroup.alpha=0、Graphic.enabled=false 或被遮挡时，默认时钟继续，调用方需要结束反馈时显式同步。组件销毁取消状态并清退其白条，不销毁借入对象、字体或材质，不要求全局资源卸载。

回退时移除 companion/新增显示层、恢复旧绑定并经中心生成；没有旧字段迁移或对已有页面的自动替换。定向用例为 `EUIBufferedProgressBarEditTests` 与 `EUIBufferedProgressBarPlayTests`。PlayMode 使用隔离 Canvas/Camera/RenderTexture 与独立字体，可保存六组合分时 PNG/轨迹；临时中心 Page 存在时还通过编译后的真实生成类和 Bridge/页面生命周期执行。同程序集其他测试和全工程测试不包含在该过滤中。

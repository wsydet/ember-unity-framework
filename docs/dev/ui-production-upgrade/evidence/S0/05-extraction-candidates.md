# S0 组件提取候选与后续入口

本表是 S2 的输入，不是已经制作的公共库。S1 先完成命名与契约。

| 顺序 | 真实来源 | 建议 family / 槽位 | 归属与影响 |
| --- | --- | --- | --- |
| 1 | Main Start/Settings 与 Gameplay Back/Settings，Center 下四个按钮 | ActionButton；label、background、可选 icon | base 公共资源；保留各页面现有 Btn_* 事件身份；先证明两个实例独立覆盖 |
| 2 | Main 根 Canvas/Animator/SafeArea/Center 与标题 | StandardPage；title、content、background | base 页面骨架；需真实 Variant，不能把复制品当继承 |
| 3 | Settings 的 PanelBg/TitleText/Btn_Close/Txt_NowScene | StandardPopupPage；title、close、content、mask | base 弹窗骨架；保留 FullScreenPopup 与 Fsm.Pop，遮罩策略不凭美术推断 |
| 4 | GM 的 EUIToggleEx 与原生 Toggle | ChoiceToggle；label、on/off/disabled | 提取中性结构，去掉 GM 业务；不是将 GMPage 当公共设置页 |
| 5 | Loading fill Image 与 GM Slider | Progress；fill、track、value | 复用进度包装能力；默认不重构现有 Loading 会话 |
| 6 | 当前没有正式中性重复 Item | OptionRowItem；icon、title、state | S2 经 EUI 中心新增 UiFoundation 示例；S3 再接动态宿主 |

字体首先复用已有可分发 TMP 字体及皮肤，不引入系统字体依赖。已存在的 OverlayGradient.png 只作为 Loading 资源，不据此认定 S2 背景/空态素材已交付。S2 仍需按 default-skin 规范实际生成或明确复用背景与透明空态插画，检查 alpha、导入和槽位；本轮未生成图像。

默认视觉继续采用 contracts/default-skin.md 的浅色、圆角、细边线与轻阴影。由于现场参考画布为 2560×1440，S2 要明确设计尺度换算，不能机械套用表中的桌面像素尺寸。

## S1 可直接开始的顺序

1. 读取 10-progress.md、S1 文档、四份 contracts 及本目录；检查新工作区差异，不重载 base。
2. 复用 03-pages-and-migration.md 的六个 GUID 和 Framework 模式，预检目标冲突、FrameworkScene 根名覆盖、NewPanel 默认值及 Loading 测试路径。
3. 在 EUI 中心现有服务上补契约导出/差异能力；明确快照遗漏的模式、IsFramework、继承及资源信息。不要把本轮只读 JSON 盘点文件当成已经实现的 `.eui.contract.json` API。
4. 正式 Unity 移动 + Binding 来源维护 + 中心再生成，同步用户引用，保留 GUID/用户区/布局。
5. 执行 S1 B01—B07 的相关测试和本轮记录的导航回归，特别覆盖 Loading/GM 命名后的行为。
6. Assets 改动通过项目中心普通保存 base；派生影响单列，不自动 Bump/同步/发布。

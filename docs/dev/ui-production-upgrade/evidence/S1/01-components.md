# S1 P0 组件能力核验

以下来自有效源码，不包含注释参考类。API 位于 Ember.UIExtension（EUIItem 位于 Ember.UI）。正式契约与所有权说明见包内 Documentation~/ui/eui-contract.md。

| 实际类型 | 状态/默认值与关键序列化字段 | 对外槽位、绑定与替换入口 | 验证边界 |
| --- | --- | --- | --- |
| TMPEx : TextMeshProUGUI | _key 默认空；_fontSkinId=-1 手动，_fontSlotId=1；TMP 原生文字/字体/字号/布局 | Text 或 Extension 全名；Inspector 替换为 TMPEx；动态文字保留 text/Key/Source | 主界面/设置运行；字体皮肤与多语言本轮未全测 |
| EUIButtonEx : Button | _enableState 默认 false；无状态节点时 EnableState getter 为 true；与 interactable 独立 | Label、_enableNode/_disableNode、附加 Graphic；IEUIExposedChildProvider 排除 Label 重复绑定 | 新增槽位去重/交互状态独立测试；GM 运行 |
| EUIImageEx : Image | spriteArray/index、animated 默认 false、fps=25、delay、playOnce；继承九宫格/alpha hit | Image 或 Extension 全名，Inspector 可替换；Sprite 是资源引用 | GM 颜色保持；全部帧动画和 alpha hit 组合未专项测 |
| EUIToggleEx : Toggle | isOn/interactable 驱动 on/off/disable 节点；Label 字段 | Label 槽位与 IEUIExposedChildProvider；Toggle/Extension | GM 增强 Toggle 与标签运行；详见 runtime 记录 |
| EUIToggleGroupEx : ToggleGroup | allowSwitchOff、按 sibling 排序，当前/待选索引初始 -1；动态回调表 | ToggleGroup/Extension；ToggleGroupValueChange(new,old)、SetToggleOn(index) | 能力读源码确认；组动态增删留 S3/相关需求测试 |
| ContentSizeFitterEx : UIBehaviour, ILayoutSelfController | 两轴默认 Unconstrained；maxWidth/maxHeight<=0 不限；DrivenRectTransformTracker | 显式 Extension 全名；布局面板设置上限 | 序列化信息被导出；长文本尺寸回归在 S2 |
| EUISafeArea | 现有 Animator/EUISafeArea/Center 结构，safe/content 区域随宿主策略适配 | 保留原 Prefab GUID；契约记录组件、布局和嵌套来源；无 CanvasScaler 时显式待确认外部设计环境 | 六页迁移不改变层级；S0/S1 相同分辨率视觉检查 |
| EUIBinding/Bridge | Page/Item、Framework/Business、IsFramework、基类 GUID；UILogic 注册命名子逻辑 | 编辑器快照 + 契约服务 + 正式生成 | 契约/往返/命名与所有权测试 |
| EUIItemFactory/EUIItem | Init 一次、Open 每次借用，Hide 与 ResetForPool 分开；Dispose 幂等 | 工厂不实例化/销毁 GO，单实例令牌防重复所有者 | 父拥有与宿主拥有均有实际生成示例和计数验证 |
| EUIProgressBar | 既有逻辑包装支持 Slider 或 fill Image；WidgetTypes.ProgressBar 仍解析 Slider | Image fill 用真实 Image/Extension 绑定，不能把普通 Image 声明为 Slider | 不新建 S2 公共 Progress Prefab |

未启用：UIContainer、TabLoader、PagePreloader、TMPMarquee 等注释参考；高级图文、合并图片、图片字按后续需求处理。S1 不扩展容器或制作默认皮肤。

Burner 只读参考位置实际为 C:/Users/wuyu/Project/burner1/burner/client/game。已读取 CommonTools/InHouseTools/Editor/GameUIBindingProcessor/GameUIBindingProcessor.cs：按当前保存根检查空引用、按资产/对象/字段给出诊断，与 Ember 的只读阻断报告方向一致。未复制 GameUIBinding、产品依赖或保存回调；Rename/UXP/BUI 和完整公共库深入适配留所属阶段。

# 柔光玻璃控件

`EUIGlassImage` 是独立 MaskableGraphic，保留旧 Image、EUIImageEx 与已有圆角修改器。它使用所属 `EUIBackdropGroup` 的下层合成结果，结合 tint、边缘亮线和圆弧圆角；正文、图标和输入光标仍由独立 Graphic 绘制。安装与 Popup 默认策略见 [背景效果](popup-backdrop.md)。

圆角按短边720设计单位体系制作：Small12、Control20、Card32、Panel48，圆与胶囊取短边一半，实际半径夹限到矩形短边一半。嵌套优先采用外半径减 padding，最小12。间距采用8的倍数，卡间16、面板24/32；正文26、辅助22、标题32，常规按钮高72，命中区域至少64DU。DU 与设备物理像素或 iOS point 不等同。

`EUIGlassStyle` 保存 tint 与圆角 token，可以显式 `ApplyTo(surface, token)`；消费端可派生自己的样式资产。颜色、Radius、Label、Icon 等使用库中公开 slot，不改生成 Binding。效果不依赖位图，也不需要替换字体图集。`SoftGlassLibrary` 是 Editor 维护库；运行时选择由业务 `EUIAppearanceConfig` 承担，不引用 Editor 库类型。

一个组默认共享一个边界。`EUIBackdropGroup.RefreshSurfaces()` 在动态子 Item 创建后重新连接表面；动态宿主绑定 RectTransform，经 EUIItemFactory 管理其逻辑，不同时注册 UILogic。静态嵌套 Item 则由父 Binding 的 UILogic 持有。组件生命周期释放各自事件，GPU lease 只由组/遮罩会话释放。

Shader 支持 uGUI stencil、ColorMask、RectMask2D clip/softness、alpha clip 和 CanvasGroup。采用顶点参数传递形状和就绪状态，屏幕 UV 采样背景，不使用普通 Sprite UV；材质由活动表面共享，uGUI 负责 stencil 派生材质。圆角同时决定 raycast 覆盖。无交互装饰设置 raycastTarget=false。

`Gloss`控制软内沿与渐变高光，`Frost`控制柔和表面亮度，均按表面写入顶点，不创建独立材质。模态遮罩保持两值为0。`SquareTop`供竖向填充使用：顶边平直、底部保留圆角，命中形状同步。玻璃滑杆没有独立Handle，Track承担指针命中，Slider仍处理点击、拖动和键盘方向。

适用范围是普通轴对齐矩形、圆与胶囊。Sliced/Tiled、Sprite 任意轮廓、弯曲世界空间面仍使用原组件；不要把旧 EUIRoundedImageModifier 再叠加到玻璃 Graphic 上重复裁圆角。局部要采同页更低内容时明确拆分为独立根 Canvas/Group，并检查最终 sortingLayer/order，不能假定嵌套 overrideSorting 或任意同 Canvas 像素可反馈采样。

按钮继续使用 EUIButtonEx 的 Enable/interactable 规则，Toggle、Slider、TMP_InputField 和 EUIInputActions 保持原输入语义。禁用时不借样式恢复点击；输入的 caret、selection、只读和错误文字属于清晰前景。鼠标焦点、触摸体验、横竖屏与设备安全区应分别检查，编辑器鼠标自检不能代替真机触摸签收。

# R2 视觉返工

当前状态更新：2026-11-17用户对R2回复“可以进行收束了”，视觉确认有效；独立验收A1已完成并发现两项缺陷，交回实施定向返工。A1-RW1修复及自检见rework-a1.md/H12，新890文件指纹见evidence/rework-a1/manifest.json；R2五图及外观资源无漂移。待原独立聊天复验，AC09/AC11仍未完成，不归档清理。下文待确认等措辞为R2制作时历史。

2026-11-17，用户对R1提出四点明确修改，需求更新R4/F01—F04，五张原始反馈图见references/r2-feedback。R1没有获得视觉确认；当前R2仍待实际运行预览和用户确认。其他授权、base待回流及独立验收门槛保持。

已在evidence/r2-baseline保存R1 manifest覆盖的880份真实文件。后续只编辑既有SoftGlass资源/维护源和相关core Graphic/Shader/布局，不覆盖旧UI，不手写Prefab YAML/Binding，不保存模板。

实施目标：

1. Slider.handleRect为空，移除Handle/HandleArea；Track承担raycast/targetGraphic，填充表达数值。竖条平顶填充、底部圆角，保留鼠标/触摸拖动和键盘输入。
2. GMEntry移除Ring/Dot，复用原Ember.UI.FPSDisplay，明确指向FrameRate TMP文本；保留EUIFloatingEntry及原绑定。
3. 局部玻璃使用透色浅染色、软内沿高光、渐变表面反射；Graphic顶点传递Gloss/Frost/SquareTop，保持共享材质、Stencil、RectMask2D和CanvasGroup。MaskSession默认Gloss/Frost0，避免把控件高光加到全屏模态遮罩。示例以大色块曲面动态背景对照，浅背景明确切深色前景。
4. GM移除巨大的外框，单组全屏模糊背景上放受限大小的面板。横屏三张300DU卡；竖屏两张卡加240DU横向测试卡；快捷操作宽屏六列、竖屏四列。测试样本56DU，2/4列完整显示；FPS入口与拖动行为保持。

制作维护源新增SoftGlassUIRevisionTwo.Apply，显式编辑本库24目标，普通Binding再生成不触发布局重做；首次Build stage2在库登记前应用当前版布局，GUID/Variant/Binding保留。已实际调用并修正R1嵌套颜色覆盖，源码/资源均经MCP编译刷新。R1 implementation.md中的历史数字不能冒充R2验证结果。

## 本轮实际结果

- evidence/r2/compile-state.json和loaded-assemblies.json记录本轮完成编译/域重载及加载程序集；Console0错误/警告。
- tests.json：13/13，1.3096413秒，job1f765a967165449a8d386cbf7298fe90；圆角用例增加SquareTop上下角命中差异断言。
- runtime-validation.json：原23项重新运行全部通过，含20次开关、遮罩/输入/FullScreen、GM功能与消费扩展；未沿用R1测试结论。
- interaction.json：本轮7项通过：原FPSDisplay+实时数字、去中心装饰、无handle指针调时间2.42、键盘增至2.71、四样本完整、浅背景深色前景、7个正式滑杆实例handleRect均为空。
- portrait-layout.json：1080×1920，面板672×1120DU，两张302×300卡片及624×240横向测试卡，四样本完全落在viewport内。宽屏三张330.67×300卡片。
- R2 performance.json：四组各30预热+180帧；CPU主线程p50 A2.3852/B3.5409/C4.3501/D2.0903ms，GPU p50 .814080/.817408/.955904/.651520ms。B/C仍3/4输出RT（约1.49/1.98MiB，不含Scratch与URP目标总显存）；D捕获0/RT0/专用材质0/相机段0。与R1同Windows Editor条件，只是短定向记录，不宣称真机性能达标。
- 本轮实际图：evidence/previews/r2/01-controls-glass.png、02-popup-blur.png、03-gm-landscape.png、04-gm-portrait.png、05-fps-entry.png。均为MCP实际GameView，未生成或合成图片。manifest记录实际尺寸与对应文件指纹。
- R1未获用户视觉签收，R2仍待用户明确确认；独立验收仍须另一聊天。base未保存/未分发，未提交发布、未归档清理。

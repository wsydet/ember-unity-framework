## 12. A-FINAL7：R4正式背景独立技术验收与最终现场交还

2026-11-18，Asia/Shanghai。主对话派原独立对话完成最后一次R4定向验收，并明确转达用户“**背景效果符合要求**”；其他五子视觉已认可。**本轮独立技术范围通过，Unity已恢复topdown并交还主对话。人类视觉与技术证据分开记录；主对话可核对收束，不由独立擅自归档／清理／发布。** R2小说30/30、R3气泡12/12及其余已通过范围未重跑。

### 12.1 两模板真实成品和限定差异

| 模板 | 版本／文件 | 当前实时 | 原封存 |
| --- | --- | --- | --- |
| base | 0.11.0／889 | 9077bd5b5271898e8055d1c7cf72b0a7 | 1c5918eb6d798246aa4d080ac7b9eeee |
| ui-game | 0.2.3／943 | 44b95096df206b5a160a1853333cc498 | 0c4917852d8cf6d759e629d2b21371f3 |

中心同算法独立重算一致；接手已保存UI游戏活动与正式943文件逐SHA相同。与I-R15实际修改前存储比较，两个模板各仅GameMainState.cs、EUIBackgroundPage.prefab、FrameworkScene.unity三文件，base另EUIGamePlayPage.prefab。Main源只删OnMainExit的ClearBackground，原await等其余源保持；场景只相机clearFlags=SolidColor及(.94,.96,.98,1)；Background新增纯装饰RawImage/EnvelopeParent引用既有Background.png GUID7bcbdb…、不抢raycast。base第四文件只增加旧无Sprite BgImage的m_Enabled=0覆盖，保节点／绑定／其他控件。各自其他资源包括公共玻璃参数、Renderer、Shader、Steps Variant与共享对照全部字节保持。Background原绑定块仅多4个隐式默认值的显式序列化字段（Inherit0/false/null/schema0），删这4行后精确旧块；schema0仍保旧maskTint语义，不误称整块字节相同。

六份父快照各889，逐SHA仍等于保存前sealed base1c591…，没有同步9077…；其他五子/框架无本轮产品漂移。base当前**未封存**，后续ParentUnversioned门禁保留，不能宣称六子继承了新背景。两模板194矩阵行实际SHA匹配。30张最终三尺寸原图已核PNG实际尺寸／SHA并逐张审阅；base采用最终runtime-r3，早期NaN和旧不透明底板失败图仅历史，不混入通过。[static.json](../../evidence/acceptance-background-r4/static.json)。

### 12.2 独立UI游戏：真实路径及两项夹具补核

原ui-game0.2.3/44b…、同实例b23…，实际程序集89516c13…、OnMainExit IL16，与I-R15显式compile1794988729891→1794988734054/domain1794988740138一致，非Play/编译/更新、Scene clean、Console0，未重复Load ui-game或无变化编译。

一次Play在原QHD上实际Pointer raycast及PointerDown/Up/Click：首页→三步玩法，step1设置暂停同一Session，关闭后原Session/进度继续；三步真实结果、Restart新Session0、返回首页通过。专用三Step Variant保持。全路背景单槽位同实体，低排序0／EnvelopeParent全覆盖，正式材质乘白、不抢输入，SolidColor兜底，无默认天空/地平线，玻璃Lease Ready。

原探针**27项中20 true／7 false**如实保留，[原result.json](../../evidence/acceptance-background-r4/ui-game/runtime/result.json)，10.944038秒。六个相同背景复合断言误要求背景与内容同相机；真实背景是UICamera，内容合法地在EUI Foreground Segment相机，记录在camera诊断与修正脚本。即时CaptureScreenshotAsTexture像素全黑，而同帧正常PNG显示底图，原黑值不能证明背景不绘制。**未修改产品**，只定向补核上述两点，不重复整套三步流程：8/8，8.539779秒，[supplemental/result.json](../../evidence/acceptance-background-r4/ui-game/supplemental/result.json)。修正为合法相机关系／低层顺序，PNG落盘后读取实际像素；临时隐藏FormalBackground对照结束finally恢复。四处外缘点由背景图的不同RGB变为一致兜底RGB，至少两点差>.015，证实正式底图实际参与合成。原27与新8不虚拼成35/35，原false与未重复的业务证据全部保留，7项夹具发现已闭环。

### 12.3 独立base：16/16

正式Load base一次889，loadedAt16:18:22。原Load经MCP触发本次实际compile **1794989906885→1794989916238／domain1794989922872**，实际aee95b07…/IL16及Console0齐；没有额外源代码修改，未追加无变化第二次编译，也未用旧I-R15事件冒充本次。证据[load-compile-confirmed.json](../../evidence/acceptance-background-r4/base/load-compile-confirmed.json)。

原QHD一次Play **16/16，10.632613秒**：[base/result.json](../../evidence/acceptance-background-r4/base/runtime/result.json)。真实首页→玩法→设置→原玩法页→首页，原装饰BgImage.enabled=false；同一正式背景贯穿，低层/全覆盖/原比例/不抢输入/安全相机与玻璃Ready通过。PNG on/off诊断也确认底图实际可见，临时enabled变化在finally恢复，无产品Save。base只核同一玩法Page实例，未虚构不存在的UI游戏Session语义。

两模板各一次Play，未重跑多尺寸/设备/Player/全量。独立16张QHD原PNG全部尺寸/SHA核验，其中2张off-diagnostic仅为隐藏夹具、不能作默认视觉；14张正常图已审。三尺寸采用上述实施30张有效图，不冒称本轮重跑。独立主要观察稳定页及过渡期间背景同一性，未新增逐帧开屏观察器／强制迟到资源竞态。用户已认可视觉，此项不由技术审图代签。[independent-images.json](../../evidence/acceptance-background-r4/independent-images.json)。

### 12.4 清退、保护与topdown恢复

两模板先关闭内容保留同一背景：UI游戏capture **7635→7635**、base **2857→2857**，Boundary／前景Camera／RT全0；再ClearBackground后槽位与活动FormalBackground全0，才Stop。运行/最终Console均0warning/error；各41字体阶段文件0磁盘漂移，同步重导入5字体资产丢弃内存副作用，语言偏好与skin1保持，诊断RawImage.enabled均已还原。未进行存档操作，未写产品。[UI清退](../../evidence/acceptance-background-r4/ui-game/runtime/resource-release.json)、[base清退](../../evidence/acceptance-background-r4/base/runtime/resource-release.json)。

T01新增可量业务片段含原夹具false共 **30.116430秒**；UI游戏与base的保守Play包络上界分别183.063/105.902秒，含启动／空闲／清退／Stop后导入，非精确有效Play时长。独立累计已知片段下界230.233253秒；I-R15自检1627.104987秒下界另列，不与独立断言混计，不把编译/modal/此前未知缺口填0。[timing.json](../../evidence/acceptance-background-r4/timing.json)。

复用并逐SHA核实**I-R15真实topdown-start1351**，未重复复制完整载荷；独立仅另保护各阶段字体、产品hash及I-R15视图映射。正式回载topdown一次1251，loadedAt **2026-11-18 16:25:36**、0.1.3/8789d264…；一次显式MCP compile请求，未知时未重放，实际compile **1794990456076→1794990470995／domain1794990477650**、UI9a08d215…／Player e190c566…、Console0齐。[compile-confirmed.json](../../evidence/acceptance-background-r4/restoration/compile-confirmed.json)。

原本地DefaultLight14c9718…精确恢复并强制导入，正式字体仍97d40b…，之后无Save/Play；公开Skill Preview/Sync前后无差异、43文件精确。用I-R15原完整6e445222…布局同步原生加载一次，keepMainWindow=true，无position setter／deferred队列；立即调用内y24局部坐标原样保留，布局稳定后只读核回原停靠**(0,803.33,933,542.67)**、原十项/index5 QHD／空Selection，磁盘字节精确还原。未新增/删除尺寸，未倒灌旧身份、base/UI元数据，旧事务失败765原件及所有旧备份均未触碰。

最终独立审计exit0：[final-protection.json](../../evidence/acceptance-background-r4/restoration/final-protection.json)。1351保护有效；当前仅三合法差异：Editor新loadedAt、base与ui-game普通Save新contentHash，其他metadata字段相同；**16,578当前产品/设置0漂移**，其他五子/父快照保持，43Skill/原index07b2454…/布局6e445222…精确。活动与topdown存储唯一差异仍原DefaultLight缓存。最终state observed **1794990775495** ready/idle、非Play/待切换/编译/重载/更新/测试，FrameworkScene clean，无dirtyFont/前景Camera/RT/事务保留目录，无本任务pending操作/探针，原三输入设备。**Unity已书面交还主对话，此后独立不再调用Unity。** [unity-handoff.json](../../evidence/acceptance-background-r4/restoration/unity-handoff.json)。

### 12.5 当前AC结论

AC01—09所派R4技术范围通过，既有R2/R3/其他范围沿用，未扩测设备/Player。AC10依主对话转达用户“背景效果符合要求”及其余五子认可，视觉已通过，独立不代签。AC11技术交付与现场交还齐，可由主对话最终核收束，尚未由本独立对话归档。AC12无产品Save/Bump/Apply/父同步/提交/push/tag/发布/消费升级/清理；base9077…和ui-game44b…仅普通保存，原封存及六子旧父1c591…保持，发布仍须另行授权。没有新增确定产品缺陷或需实施返工项；所有原false、补核、失败和原图保留。

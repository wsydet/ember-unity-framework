## 11. A-FINAL6：R3原场景气泡定向独立复验与topdown交还

2026-11-18，Asia/Shanghai。主对话派原独立对话承接R3/I-R14-save-r2，当前结论以本节为准。**R3所派技术范围通过，topdown及新起始现场已恢复，Unity已书面交还；R2小说30/30与其他已收尾范围未重跑。用户新视觉与整体收束仍待主对话，未整体签收／清理。**

**基线：**同一实例ember-unity-framework@b23e9b768e85ed30、新会话f0bab1f1…，直接承接已保存source0.6.3／**ba33dd3fe620c9ec3aed3818315bd02f**，未重复Load source或无变化地编译。独立正式算法重算一致，活动／正式／保护projection均954文件逐SHA相等。原C0.6.2／80a86599d447326f049b4be3954db7cb，三个恢复文件精确C；438原GUID、两Prefab EUIBinding序列化块及原Bubble.Binding.cs保持，另951文件与实际修改前source-active一致；新父889逐字节等于base0.11.0／1c591…，原封存74704c…保持。97行source矩阵和5张新版1920原图尺寸／SHA均核，图已独立查看。[static.json](../../evidence/acceptance-source-r3/static.json)。

I-R14显式compile1794982132306→1794982138805／domain1794982154832沿用，当前重启后事件字段为空的事实保留；本轮实际读取程序集MVID **e303657a-69d6-439a-8381-0c49dc396840**、OnShow IL7／SHA10a1c2f6…与原编译记录相同，且活动源与已编译保存源精确相等、Editor idle／Scene clean／Console0，才进入Play。没有把重启事件空值或单次idle独立当编译证明。[native-start.json](../../evidence/acceptance-source-r3/native-start.json)。

**本轮一次Play，12/12，10.843628秒**（2026-11-18T14:48:59.157902+08:00—2026-11-18T14:49:10.0015302+08:00）：真实Main Start→Gameplay/SceneUI，两个原Sprite气泡注册并显示；气泡无SoftGlassSurface／BackdropGraphic、场景宿主无独立BackdropGroup。单泡隐藏至延迟回池、另一泡连续可见，再显复用原实体，global Boundary数量保持且无新增气泡专属消费者；不沿用旧“每泡Lease必须非空”的已失效断言。移动原Cube并核屏幕位移>1像素，归位误差<1像素；设置确实隐藏自有SceneUI且Close屏内可用，完全关闭后原alpha／气泡实体集合／无玻璃状态恢复；Back清退SceneUI及物体注册，当前QHD尺寸保持，临时位置在退出前还原。[runtime/result.json](../../evidence/acceptance-source-r3/runtime/result.json)。

五张本轮**2560×1440**原PNG已逐尺寸／SHA核并审图，含原气泡、跟随、设置、关回、返回首页；不是实施1920图冒充本轮输出。[independent-images.json](../../evidence/acceptance-source-r3/independent-images.json)。不做多尺寸扫、全量、设备、Player、小说或其他模板验证；本轮只一次隐藏／复用，不重复此前预隐藏等完整所有权矩阵。未新增产品或行为失败。定位脚本初次误用了不存在的备份名、把完整保护当五根投影、假定SceneUIPanel有独立.Binding.cs；均在Play前更正为实际备份／投影及两真实EUIBinding块，未执行产品修改，也不记成产品失败。

Console运行及最终均0warning/error。关闭全部消费者后Boundary／前景Camera／RT／Backdrop材质全0，capture **2156→2156**停止；确认Stop后原41份字体阶段文件0磁盘漂移并同步重导入5份字体资产，语言偏好／fontSkin1保持，source954镜像仍精确。未写用户存档。[resource-release.json](../../evidence/acceptance-source-r3/resource-release.json)、[fonts-restored.json](../../evidence/acceptance-source-r3/fonts-restored.json)。本轮业务10.843628秒、含启动/空闲/清退及Stop后导入确认的Play包络上界87.143秒；接续独立可量片段下界200.116823秒，I-R14自检12.357827秒单列，编译/模态/重启和未知缺口不算0，不报全任务完整时长。[timing.json](../../evidence/acceptance-source-r3/timing.json)。

**恢复：**按派发逐SHA核实并复用I-R14重启后新topdown-start **1351**字节保护，没有另复制这1351项；仅独立保护本次source字体/当前668c布局和当前产品hash。完成后中心正式Load topdown **一次1251成功**，loadedAt **2026-11-18 14:50:39**，0.1.3／8789d264af2650178116bbdbe8ea4c5f。一次显式compile请求；未知回执未重放，用户确认MCP后观测编译开始、域重载间隔一次30秒重连恢复，核compile **1794984753104→1794984774180**／domain **1794984785711**、UI9a08d215…／Player e190c566…及Console0。[compile-confirmed.json](../../evidence/acceptance-source-r3/restoration/compile-confirmed.json)。此次没有目录事务失败，没有移动／删除旧失败原件、修改权限或事务引擎。

原DefaultLight本地缓存恢复**14c9718…**并强制导入，正式字体仍**97d40b…**；之后无Save/Play。公开Skill Preview/Sync前后无差异，43发现文件精确。通过原生LoadWindowLayout(path,false,false,true,true)一次同步应用新保护中的原完整**6e445222…**布局，不写position setter或排队deferred callback；同调用内坐标尚为局部y24，保留原记录，正常布局完成后只读核回原停靠位置**(0,803.33,933,542.67)**、docked=true、原十项/index5 QHD／空Selection，未新增或删除分辨率项。磁盘布局随后恢复精确原字节；source接手的重启668c…字节独立保全，非混改YAML或默默丢失。原启动缓存与新版metadata未整体倒灌。

[final-protection.json](../../evidence/acceptance-source-r3/restoration/final-protection.json) 独立脚本exit0：1351原保护完整；当前仅两项合法差异——Editor新loadedAt、source模板普通Save的新contentHash（a25…→ba33…，其余metadata字段逐项相同）。16,578产品相对本轮接手只差按要求恢复的原layout，其他五子／框架／source成品／父889不漂移；43Skill、原index **07b2454ab28af72dce996b96f1ad27eba074c651c05f53f8f22a6d3a151655f0** 保持。活动topdown与正式存储唯一差异仍原本地DefaultLight缓存。原C及旧失败备份未清理。

最终state observed **1794985027375** ready／idle，非Play／切换／编译／重载／更新／测试，FrameworkScene clean，原Keyboard/Mouse/Touchscreen、无dirtyFont／前景Camera／输出RT／事务保留目录，Console0。**无本任务结果不明或待执行Load/compile/Play/布局／探针，Unity已交还主对话，此后独立不再调用Unity。** [unity-handoff.json](../../evidence/acceptance-source-r3/restoration/unity-handoff.json)。

AC01/03/04/05/06/07/08/09的R3定向技术证据齐；AC02及R2/其他范围沿既有，AC12边界保持。**AC10用户视觉仍待、AC11整体未完成**；技术审图不能代人类签收。无Save/Bump/Apply/提交/push/tag/发布/消费升级/清理，所有备份、失败和原图保留。

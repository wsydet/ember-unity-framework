## 10. A-FINAL5：R2小说原UI／仅GM独立复验完成

2026-11-18，Asia/Shanghai。原独立对话沿用完整模式和主对话派发A-FINAL5，直接读取R2/D9、真实原C、正式保存成品及实施原始回执。**本节为最新技术结论，取代前文小说旧玻璃成品与待实作状态；不代用户VP2。** 其他五子与base沿A-FINAL1—4，不重复全量或完整矩阵。未实施产品修复或改写模板成品、测试、快照；模板切换与原现场恢复按授权执行。

### 10.1 原UI真实基线与限定差异

独立脚本 [static.json](../../evidence/acceptance-novel-r2/static.json) 32/32通过：原C **1633文件／eadb06cc16d55c09718ac839ae8dd554**；正式小说 **1816文件／13f45e806dd12d33be1779b11864c40e**，版本0.22.4、封存仍b577b887cef72e3db2264e5f33dd5c3c；父base0.11.0、889文件／1c5918eb6d798246aa4d080ac7b9eeee，父快照逐字节等于当前base。不是用已父同步的S冒充原UI。

- 14个指定玩家Prefab中12个与C精确相同；Main/Setting删去仅新增的四行后逐字一致，meta全部精确C。四个字段maskEffect=0／overrideMaskTint=false／backdropSettingsOverride=null／backdropSchema=0与当前初始化默认值一致；特别核schema0令HasExplicitMaskTint=true，保留原maskColor语义，不能只凭“数值0”认定。实际Main/Setting运行属性均已独立验证。
- 906个原GUID全保持；10份重新生成Binding的声明／ControlMap忽略生成注释和空白后与C等价；原Choice用户源精确C。Narrative360、Narrative配置76、Localization6、生成FontSkin ID2及FontSkins配置精确C。两个新辅助类及meta均删除，按旧meta GUID另扫正式资源无悬留引用。
- 小说105行迁移矩阵的实际路径／SHA全部匹配。BUI Main变体本体精确C并仍引用同GUID Main；此项为静态继承核对，本轮未独立实例化该变体。
- Appearance为Legacy玩家公共弹窗／SoftGlass GM，正式ShowGM接线保留。场景相对C仅源库GO与4个PrefabInstance文档不同，原相机／Launcher其余对象保持，源库inactive；不整场景回退。其他五子、框架和父快照最终零漂移。

另见 [additional-static.json](../../evidence/acceptance-novel-r2/additional-static.json)、[matrix-and-variant.json](../../evidence/acceptance-novel-r2/matrix-and-variant.json)。实施44张新PNG逐尺寸／SHA通过，1历史夹具图排除，43张非历史图以5张可追溯拼图逐项查看，含三实际尺寸原首页、设置、阅读、菜单、槽位和GM。旧I-R11小说玻璃玩家图不再作当前视觉依据；字体/皮肤夹具图不是默认外观。

### 10.2 本轮新独立行为与编译证据

正式Load小说一次1816成功，loadedAt12:53:27。用户正常处理本次弹窗后取得原回执，没有重放；显式MCP编译一次，连接经30秒间隔第2次恢复。核compile **1794977859854→1794977878105**／domain **1794977886905**，实际NovelSession MVID01ae51b0…／UI32bcb5ed…／Binding19493bf5…，非编译／更新、Scene clean，编译Console0warning/error。recovered回执没有直接当完成证明。[compile-confirmed.json](../../evidence/acceptance-novel-r2/compile-confirmed.json)。

**2026-11-18T13:00:24.7138841+08:00—2026-11-18T13:00:54.8190796+08:00，30.105秒，30/30**，见 [runtime/result.json](../../evidence/acceptance-novel-r2/runtime/result.json)。仅一次Play，无失败断言／补跑：

1. 原Main→Settings，实际七Slider契约、TextSpeed写隔离账户；英语／简中实际切换、一套字体皮肤与原映射保持；原Main/Setting新增默认字段的实际遮罩语义。
2. GM入口采用原Floating的PointerDown／PointerUp，中心Close用Button Pointer事件处理，打开／关闭／复开唯一实例。事件由EventSystem ExecuteEvents投递，留实际屏幕位置与raycast结果，**不是硬件鼠标或完整InputSystem设备测试**。
3. 在原Main仍打开时经API关闭实际GM FreePage，等待ActivePages移除；Boundary0、capture停止；原Main相机／层仍可见，实际Pointer点击Settings成功。然后经正式ShowGM恢复唯一实例。整页释放不冒称用户按钮功能。
4. 公共Confirm经原API路由到Legacy类型，Cancel后等待实际移除且回调恰好一次；不是声称小说业务必经该全局弹窗。
5. 真实新游戏、名字Ada、原Title推进到对白、ReadingMenu自有暂停；QuickSave实际槽6／当前Schema与Command；原八槽页面及自动槽只读；QuickLoad真实替换并Dispose旧Session，等Loading揭幕结束、Reader Menu实际可交互、暂停清空后再操作，恢复Command和名字一致。
6. 原Font页Small／Large修改预览与偏好，原Image皮肤A／B／base可见且恢复Sprite／Color；ReturnMenu释放阅读Session、真实隔离存档令Continue出现；源库仍inactive。没有重新跑全剧情、全语言字形、所有槽覆盖／取消矩阵或Choice路径，相关实施自检来源保持区分。

本轮11张原始1920×1080 PNG已核实际尺寸／SHA并逐张看图，见 [independent-images.json](../../evidence/acceptance-novel-r2/independent-images.json)。原字体面板实测屏幕点(81,672)：A像素(63,33,63)、B(29,64,63)、base(47,45,35)，证明原Image皮肤视觉生效；不是只核属性。新原图未编辑，审阅拼图另存。三尺寸范围采用上述43张新实施原图，**不冒称本轮重新跑三尺寸**。

结束Console **0错误／6警告**，均为原DingTalk缺“體、語”字形，旧原资源／映射完整且I-R13已记录此限制，不当新增迁移缺陷，也不宣称四语字形全覆盖。[runtime-console.json](../../evidence/acceptance-novel-r2/runtime-console.json)。关闭消费者后Boundary／前景Camera／RT／Backdrop材质全0、capture **2336→2336**。确认退出Play并同步重导入6个字体/配置资产丢弃运行内存副作用；42份字体阶段保护0磁盘漂移。39份用户原存档前后逐SHA相同，两个语言偏好和活动字体皮肤finally还原；所有业务写入任务隔离store。

计时 [timing.json](../../evidence/acceptance-novel-r2/timing.json)：本轮业务30.105秒；从Play请求返回到退出并完成字体导入确认的保守包络上界 **128.215秒**，包含启动、空闲、清退和Stop后导入，非精确有效Play时间。原A-FINAL1—4可量片段159.168秒下界保留，加本轮为189.273秒独立可量片段下界；I-R13自检70.635秒另记，不混为本轮独立结果。切换、编译、模态等待及未知间隔不算零，不宣称任务完整总耗时。

### 10.3 本轮新保护与正式现场恢复

先于Load建立自己的 [1351文件起始载荷](../../backups/acceptance-novel-r2-start/manifest.json)，逐SHA有效，含实际default-6000布局字节、43份.agents发现文件、原index和所有受管现场；只借用I-R13路径清单，未借旧备份字节／hash冒充本轮起始。新起始Editor/布局证据在evidence/acceptance-novel-r2。

正式回载topdown一次1251成功，loadedAt **2026-11-18 13:03:43**，身份0.1.3／8789d264af2650178116bbdbe8ea4c5f；原请求未重放。本次显式compile **1794978721355→1794978737358**／domain **1794978744526**，实际程序集及Console齐。随后不再Play，不SaveAssets/Template。

原DefaultLight本地缓存精确恢复14c9718d55d832593c689643cd752a277e0915fe418bb71f8f85c883653c0e4d，并强制同步导入；正式存储仍97d40b…，不把缓存写进成品。正式Skill Preview／Sync前后Differences空，发现文件43份精确。Unity原生加载本轮1351备份中精确原布局工作副本成功，磁盘布局恢复原SHA **6e4452224022c3df9a1bb4aa357caff8028e06c4d84e882d155394010787d3c5**；原10项／顺序、index5 QHD、位置(0,803.33,933,542.67)及空Selection均核回。本轮未新增分辨率项，不移除任何用户项。

恢复工具过程另留两项非产品失败：首次CodeDom查询因6000.5的GetInstanceID弃用而未执行，已改用名称记录空Selection；随后给已停靠GameView写position使它变成浮窗，已保留异常现场，重新通过同一精确原布局恢复停靠，删除position setter并核原位置。原布局延迟回调曾仍在队列，确认唯一且未执行后移除并调用原回调一次；第二次布局加载是修正已知浮窗副作用，非重放结果不明操作。最终本任务待执行布局回调为0，原三个输入设备、无脏字体／前景Camera／RT。见restoration/view-query-first-attempt.json、editor-restored-before-dock-correction.json、layout-queued-callback-confirmed.json、layout-dock-correction.json；首次漂移审计因尚无editor-restored.json而中止，最终补齐证据后exit0。小说30/30用例未重跑，无产品修复／新增编译请求。

[final-protection.json](../../evidence/acceptance-novel-r2/restoration/final-protection.json) 独立脚本exit0：1351备份有效，当前1350份字节相同，仅正式Load合法新Editor loadedAt不同；16,578份框架／模板／ProjectSettings／UserSettings零漂移，原index 07b2454ab28af72dce996b96f1ad27eba074c651c05f53f8f22a6d3a151655f0 保持。当前活动与存储唯一差异仍原DefaultLight缓存。用户原39文件、原C／原成品／父快照／其他模板未被验收改写。

最终MCP state observed **1794979159966** ready／idle、非Play／暂停／切换／编译／重载／更新／测试，FrameworkScene clean；没有结果不明或待执行操作，没有运行探针、合成设备或Backdrop输出。见 [书面Unity交还](../../evidence/acceptance-novel-r2/restoration/unity-handoff.json)。此后独立不再调用Unity，交主对话收取；后续接手仍先核实际状态。

### 10.4 当前逐AC与边界

| AC | 最新结论 |
| --- | --- |
| AC01 | 原其余矩阵沿用；R2小说105行全部实际SHA匹配，当前静态通过 |
| AC02 | 沿用A-BASE1／VP1，base封存保持 |
| AC03/04 | R2原UI／仅GM及其他五子所派最小风险独立范围均完成；仍不代VP2或全玩法 |
| AC05 | 本轮GM最后消费者与全部资源清退独立通过；既有base/source等风险结果沿用，非全排列 |
| AC06 | 新43图三尺寸原证据审阅＋本轮11图1920原图／像素通过；未新增设备／Player测试 |
| AC07 | 14Prefab、906GUID、10生成绑定／Choice／配置及运行路径通过；BUI仅静态核继承，不冒称全Prefab原生重扫 |
| AC08 | **本轮新1351保护与正式返回／精确现场恢复通过，Unity已交还** |
| AC09 | 本轮小说与返回topdown的显式MCP编译／域重载／实际程序集／Console齐；原字体缺字限制保留 |
| AC10 | **用户新视觉评估仍待**，旧小说玻璃图被R2取代，其他五子也不据技术通过自动签收 |
| AC11 | **所派独立技术验收完成；整体任务仍待必要人类视觉结果及主对话收束，不标完成／不清理** |
| AC12 | 无产品／测试改动，无Save/Bump/Apply／提交/push/tag／框架发布／消费升级／清理；当前六子仍普通Save未最终追加封存 |

没有新增确定产品缺陷、没有需要实施返工的技术失败；恢复工具错误与修正另如实保留。所有原图、备份、旧夹具失败和证据保留；不因本轮通过删除材料或扩大到发布／消费端验收。

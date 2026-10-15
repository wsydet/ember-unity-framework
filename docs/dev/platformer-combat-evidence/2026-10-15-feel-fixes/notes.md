# 体验修正基线与验证

## 三连击再次扩大一倍

用户要求范围再扩大一倍。仅调校副本三刀全部Box尺寸(2.8,2)→(5.6,4)，偏移(1.6,.1)→(3.2,.2)，前沿3→6u；刀光比例1.6→3.2、偏移(1.2,.1)→(2.4,.2)。保存并通过方案校验。默认PlayerDefaultScheme未改，Options=1，窗口/草稿未操作。同步范围测试目标至5.4u、范围外6.6u。Unity Console0errors，PlayMode job d3a38ac1899447e4aa0cd1622095bf0c为8/8 Passed，1.0232941秒（六项左右范围、空冲倍率、受击恢复）；本轮未重新截图或实播，旧3u截图不作为6u效果证据。

## T03重新启用与原窗口恢复完成

本轮MCP成功：Player实际Enabled=true，原生菜单Enabled=true，Options=1/Enabled=true，Console0errors，非编译中。t03-enabled.json保存实时证据。与t03-draft.json比较_attack完整一致；窗口业务字段（除瞬态候选对象句柄）逐项比较，唯一差异为_section由方案与保存变成动作与机制，Combo、来源、素材ID/Clip列表、baseline及选择均一致。默认config与after.json完整一致。

按用户要求以after.json恢复原窗口序列化状态，先检查当前候选仍是测试.47标记，再移除该测试候选并CacheDraft，未应用到方案。t03-restored.json的window与after.json原window字符串完全一致，candidateCleared=true、默认PlayerDefaultScheme、Options=1。没有再次关闭Player、没有切换模板。模块关闭/重新启用及候选保留验证完成；不据此放行跨模板旧草稿恢复或其他未测身份。旧的阻塞段落保留为历史记录，以本节为当前状态。

## T03禁用状态已取得实时证据，恢复待确认

本轮重连发现源码Enabled=true但加载的PlayerModule特性仍为false；Options=1/Enabled=true。实际原生菜单存在但GetEnabled=false，PlayerEditorAccess.Available=false、Require抛出“Player 模块已关闭。”，旧PlayerSchemeWindow仍有1个。只读输出保存于t03-disabled-state.json。候选_attack与t03-before-disable.json解析后完整JSON逐字一致（包含第一段.47和全部攻击配置）；整个Feel JSON不相同是RestoreCombatDraft只恢复攻击候选，不携带非候选movement/jump/input引用，符合该候选设计，不是丢失攻击数据。

随后ForceUpdate导入已恢复true的PlayerModule.cs，20秒后首次Console查询ping not answered，即停止自动验证。无再次关闭，无新业务改动，源码保持true，默认仍PlayerDefaultScheme。重新启用后的菜单/窗口及草稿尚未确认，原窗口状态尚未恢复，测试候选仍保留待完成恢复。下一轮只检查启用/候选并还原原窗口，不必重复已验证的关闭流程。完整Combo/素材清单等窗口字段也需在还原前与t03-draft.json复核；本轮完整一致结论仅限_attack。

本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；若仍报错请提供首条错误及完整堆栈。

## T03模块关闭尝试（用户授权代执行）

已只读导出t03-before-disable.json，确认测试候选存在、Options=1。仓库真实模块开关为PlayerModule的EmberModuleAttribute.Enabled（启动装配开关，非动态启停API），未找到独立面板。临时将该属性true→false，MCP编译请求成功；20秒后首次Console查询ping not answered。立即停止Unity调用，禁用菜单/旧窗口/草稿跨重载均未取得证据，不记通过。为不遗留禁用源码，将同一属性增量恢复true，不调用替代编译，也不关闭窗口或清空候选。编辑器当前加载的Enabled状态及恢复编译待重连确认。原源码SHA256为0D2BDCA53632B10DE494AD56CE89830226106BA56B97C9316046A1FE13793885，恢复后已再次读取哈希核对。未修改默认方案。

本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；若仍有错误请提供首条错误及完整堆栈。

## 2026-10-15 后续验证通过

本轮Console0errors且3个FeelFix测试方法已加载，Unity编译阻塞解除。PlayMode job 5e3da8de474248878991b789fe48df90：8/8 Passed，0失败/跳过，1.1265873秒；包含三段×左右6例、空冲倍率1例、短受击恢复1例。

正式Main→新游戏使用调校副本和新隔离库：虚拟键盘三次J输入得到first→second→third→recovery→idle，起点(.7,.9)面向x=-2沙袋，HP30→23，见combo-live.json。刀光实时scale.x=-1.6，截图slash-left.png；帧记录未采齐Slash_3，不宣称本轮全六帧截图。空冲第一次实播因墙面中断失败，dash-interrupted.json保留；无遮挡高空复测dash-live.json显示air-dash速度16，EndReason.Completed进入combat.dash-recovery瞬间4，然后按30u/s²继续减速。hit-live.json显示.15秒硬直期间Sprite y升至.214，约.146秒采到idle且y=0，之后持续为0；不是只看编译或配置数值。

已停止Play、清理本次虚拟键盘，after.json记录Options=1/Enabled=true、devices空、sceneDirty=false，默认配置JSON与原基线相同，白盒选择不变，窗口选中与攻击候选状态和基线相同。SessionState选中项从原空值变为窗口原选中GUID，是域重载时窗口正常缓存，不能声称缓存字节完全不变。

继续T03准备：在已确认无原攻击候选的既有窗口内，选择调校副本，读取独立候选，将第一段Duration只在候选改为.47（资产仍.45），未应用未保存。窗口原状态已在baseline.json/after.json备份，测试候选全文在t03-draft.json。该候选仅用于验证关闭/重开模块后的草稿保留，完成后需恢复原窗口选中与空候选；不能将.47应用到方案。Player未关闭，等用户通过正常入口关闭后再核对禁用与草稿。T03未通过。

基线见baseline.json：实时Options=1另已查询确认；窗口无攻击候选，缓存候选为空。默认方案及白盒身份保持原值，不推断切换前旧草稿恢复成功。

调校副本：Assets/GameResource/Resources/Config/Player/通用攻击模板1-体验调校.asset。通过Copy正式接口创建；原内置方案只读保留。三段Box偏移(.9,.1)→(1.6,.1)，大小(1.4,1.5)→(2.8,2)，前沿1.6→3u；刀光原比例乘1.6，偏移(.65,.1)→(1.2,.1)；空冲退出倍率1→.25。

受击复现：运行隔离正式会话，在实际Animator上播放Hit至.1秒得到Sprite localPosition.y=.2142857，再切idle，缩放复原但y仍为.2142857。单次受击状态可退出，并非已证实FSM永久卡死。修复只给副本使用的21个基础动画复制片段增加Sprite局部位置归零曲线（原片段没有位置曲线），放在Combat/FeelFix；保留其原有缩放/其他曲线、Controller和图，不修改共享原片段。修复后隔离Animator实测Hit→idle/move/fall的y均由.2142857回0。

新增8项PlayMode专项：三刀左右扩展范围命中/范围外不命中6项、空冲退出1/4速度1项、短受击回idle无位移残留1项。当前已请求Unity编译，测试待执行，不标通过。

刷新返回成功，随后首次Console编译确认返回ping not answered；立即停止自动Unity验证，不重试、不用替代编译。8项尚未运行，候选有效距离/实时刀光观感及空冲退出测量仍未通过。当前Play已在资源修复前主动停止，未关闭Player。未修改生产C#；新增测试需要编译验证。本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；若仍报错请提供首条错误及完整堆栈。

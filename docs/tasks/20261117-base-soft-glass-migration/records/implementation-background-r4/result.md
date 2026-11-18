# I-R15 R4 最终实施回交：base / ui-game 已普通保存

2026-11-18，Asia/Shanghai。有效R4/纠正D10及主对话对base第四文件的明确补充授权。本轮实施与相关自检完成，**待主对话安排独立技术复验、R4人工视觉和最终topdown恢复**；整体任务未归档/清理，不代签AC10/11。

## 保存结果与范围

| 模板 | 版本 | 新实时hash | 原封存hash | 正式保存 |
|---|---|---|---|---|
| base | 0.11.0 | 9077bd5b5271898e8055d1c7cf72b0a7 | 1c5918eb6d798246aa4d080ac7b9eeee | 889文件一次成功 |
| ui-game | 0.2.3 | 44b95096df206b5a160a1853333cc498 | 0c4917852d8cf6d759e629d2b21371f3 | 943文件一次成功 |

两份新candidate与正式镜像逐SHA及中心算法hash相等；普通Save只改metadata的contentHash。base为未封存草稿，原sealed889完整保护；六子父1c591.../889没有同步，未来新派生/父同步会受ParentUnversioned门禁，不能宣称继承了本次背景。

两模板各三文件：GameMainState仅删OnMainExit里的ClearBackground、保原await；EUIBackgroundPage新增纯装饰RawImage/EnvelopeParent使用原Background.png、关闭旧fallback raycast、Binding/GUID保持；FrameworkScene只有MainCamera clearFlags/color两个字段。base第四文件EUIGamePlayPage.prefab由主对话明确授权：旧Animator/BgImage仅是无Sprite纯色装饰，空Binding无业务消费者，唯一新增m_Enabled0覆盖，保节点/五Binding/GUID和按钮/Loading。未改玻璃参数、Shader/BackdropSettings/Renderer、Steps Variant、共享对照或其他五子。

## 编译与真实运行

base修改编译1794987607485→1794987611107/domain1794987619716，新MVIDaee95b07-3a30-4da8-80a5-dd1c17b48700、OnMainExit IL16；ui-game修改编译1794988729891→1794988734054/domain1794988740138，新MVID89516c13-a846-45b6-b6ef-d6b1df0cb30e、IL16。均为本轮MCP显式请求后的实际完成证据，最终Console0错误0警告；所有Prefab/Scene修改经原生API及MCP刷新核实。

base第一轮45/57揭示AspectRatioFitter None模式在零尺寸编辑画布重算ratio为NaN，已正式API修正初始化顺序，保留失败；第二轮57/57结构通过而原图发现旧不透明内容底图，按明确追加授权修正第四文件；最终定向42/42（省去未受第四文件影响的MainSettings重复），三尺寸9张新页面原图可见正式浅蓝曲面。原r2未变MainSettings证据保留。ui-game一次127/127，三尺寸21原图，真实绑定按钮/原生Pointer射线可达、三步Selected/Progress、设置暂停/同局恢复、结果/Restart新Session/Home通过。背景跨状态同实体/排序0/不抢输入/EnvelopeParent比例覆盖，Lease Ready记录齐。没有运行全量、设备或Player。

清退分别先关闭内容保留同一背景，再收Background：最终base边界/Camera/RT0、capture2428→2428；ui-game边界/Camera/RT0、capture3713→3713；Background slot0，Play明确退出。原临时尺寸项10已移除，原十项/index5QHD/停靠窗口/空Selection恢复，字体disk/dirty均0漂移。所有启动观察器在Stop后已写终态并解除回调，无pending探针。

## 失败与恢复没有隐去

首次base Load目录移动拒绝和回滚不完整，原生Game765与新保护相等，copy-only恢复1351及显式编译。原件三方SHA/目录/ACL/安全门禁后按主新增授权可逆移入独立备份，未删原件。真实新进程101864/15:28:27和全部保护门禁后仅受控重试一次Load成功；新/旧错误回执保持。后续加载/编译确认均核原结果，不重放未知请求。

夹具预flight Camera JSON外包装差异、CodeDom变量作用域/裁剪漏局部声明、一次UTF8复制失败导致未安装探针的短Play均保留，修正后只针对新问题复验。没有把这些声明为已运行行为通过。动态字体和重启布局副作用先另存再按本阶段原保护恢复，不写入产品。

## 最终保护与回交

final-protection.json exit0：16,578产品/设置仅预期base4+metadata、ui-game3+metadata共9路径变化，其余产品/六父快照0漂移；七新保护payload全部有效，原生765归档有效；原index07b2454...及HEAD16cff...保持。687矩阵全部actual SHA匹配，更新base95行/ui-game99行保存状态与3个变动Prefab行；不把其他模板内容回退。

当前应保持已保存ui-game0.2.3/44b...活动，非Play/空闲/干净；最终原生状态以unity-handoff.json为准。未为交还再次Load topdown，独立后统一恢复说明见topdown-restoration.md，必须使用本轮新i-r15-topdown-start1351，区分原本地DefaultLight14c...与正式97d...，不整树倒灌旧metadata。

## 文件索引

- r4-preview.html / r4-preview-index.json：30张最终候选页面原图，含三尺寸，未图像编辑；两模板raw-image-index共150张页面/启动关键帧含历史失败，明确区分。
- base/saved-summary.json、ui-game/saved-summary.json：逐项运行、保存及失败结果。
- 两模板author-compile.json、save-result.json、final-console.json、runtime*/resource-release.json、startup/result.json。
- final-protection.json、validation-timing.json、topdown-restoration.md、checkpoint.md。

无Bump/Apply/父同步/提交/发布/push/tag/消费升级/清理。后续独立验收仍需自己的新保护、确认实际活动现场并按R4范围短核，不以实施自检代替独立/人工签收。

本轮可成对相关探针含失败共 179.572549 秒；结合4组实际Play观测帧与探针区间取并集，本轮可证明下界 422.413908 秒，沿用此前1204.691079秒后实施累计下界 **1627.104987秒（约27.12分钟）**。一次无观察器短Play及首尾未观测帧、外部编译/modal/空闲缺口仍列为未知，不记零，不冒称完整总时长。

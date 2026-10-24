# 验收、交接与执行记录

本文件是待执行清单与记录模板。本次只完成策划、源码核对及文档静态检查，没有创建 UI 模板、部署游戏或执行下列 Unity 验收。不得把“要求通过”转写为“已经通过”。

## 1. 三个交付关口

| 关口 | 交付物 | 放行依据 |
| --- | --- | --- |
| G1 框架模板 | 通用 UI Demo、正式模板说明、项目中心保存/封存结果、实际包交付来源 | 独立于 ColorOrder 也能运行；父链和内容一致；消费项目能获得该内容 |
| G2 产品原型 | 新工程部署证据、三短关、产品规则实现、相关测试与可玩入口 | 逻辑/输入/存档正确；可实际试玩；玩法观察单独报告 |
| G3 内容扩展 | 九关 36 段、全星路线、完整流程，随后六站巡游 | 内容与规则一起验收，技术检查和真人体验均有实际记录 |

G1 中若未授权发布，可先交付本地完成结果及发布准备；阶段二安装实际发布包前补齐交付来源。不要为跨过关口伪造 tag、hash 或测试结果。用户已经授权的动作直接执行，无需因清单再次申请同样授权。

## 2. 编译与测试的统一执行方式

执行前完整阅读实际 Ember 包内 `Documentation~/maintenance/unity-validation.md`；框架侧入口见[公共规范](../../../Packages/com.ember/Documentation~/maintenance/unity-validation.md)。消费侧依据 UPM 真实包路径读取，不写 PackageCache。

1. 先确认 MCP 指向当前任务的 Unity 实例，尤其两工程同时打开时。
2. 每批 C# 或程序集改动后，主动通过 Unity MCP 请求脚本编译。资源修改也要刷新并验证；生成代码后继续执行编译要求。
3. 请求成功只是已发起。取得本轮编译/域重载完成证据，确认非编译/更新，再读取 Console；空 Console 或一次 idle 不能单独证明新程序集已加载。
4. 只运行本次新增或直接相关测试，提前记录类名/过滤条件。不要默认跑整个程序集、全量框架或大规模种子扫描。全量或预计较长测试依公共规范取得明确授权，不能拆批绕过。
5. MCP 断连先核查待编译改动及已提交请求。尚未请求时尝试提交编译；无法送达立即提示用户“请在 Unity 中手动触发编译，以恢复 MCP 连接”。请求结果不明先查状态，不反复触发。
6. 随后每隔 30 秒重连，最多 10 次，首次失败不计重试。恢复后继续本轮验证；十次失败停止依赖 MCP 的工作，请用户确认状态。不得用 Editor.log 轮询、BatchMode、dotnet build 或自动刷新代替。
7. 若最终未验证，明确写“本次未完成 Unity 编译验证。请在 Unity 中手动触发编译”；仍有错误时要求首条错误及完整堆栈。静态检查可以继续，但不能填编译通过。

纯文档修改不需要 Unity 编译或测试。本包的链接检查和夹具推演只证明文档一致性，不代表运行代码已存在。

## 3. 框架模板最小验收

| ID | 操作 | 期望 |
| --- | --- | --- |
| T01 | 冷启动 FrameworkScene→主页→玩法 | 正式 SceneFlow 成功，唯一启用 Scope，Demo 可操作 |
| T02 | 完成→结果→重开，连续三次 | 一次输入只结束一局，旧订阅/回调不作用新局 |
| T03 | 玩法打开设置，再关闭 | 业务输入与推进暂停，恢复只解除对应暂停原因；状态栈与 Popup 一致 |
| T04 | 准备/动画/结果期间连点或返回 | 无多局并发、重复导航或销毁后重新打开页面 |
| T05 | 主页/玩法往返三次 | 页面/Item/事件释放正确，实例计数不增长 |
| T06 | 窄/长两种竖屏比例和安全区检查 | 文本无遮挡，按钮可达，遮罩不穿透；记录实际尺寸 |
| T07 | 检查 EUI 生成物与模块启用 | 目录/注册/Binding 一致，没有产品规则；Module Enabled 显式 |
| T08 | 项目中心内容/资源/谱系校验、保存与封存 | 无阻断项，内容与父快照 hash 自洽；状态各自记录 |
| T09 | 从实际交付包首次部署到消费工程 | 直接部署最终模板可完成 T01～T05；记录部署身份/原稿 |

T09 可在阶段二开始时完成，未执行前留“待验证”，不能阻止阶段一保存已经验证的工作，也不能据此声称已通过消费验证。

## 4. 产品规则增量验收

下面是 B0～B2 各阶段合计的用例清单，每批只执行实际新增或受影响部分。旋转、污染、背景覆盖等到 B1 引入时执行，巡游到 B2 执行，不能把整个清单当作 B0 实现范围。实际测试名由实现者填写，执行前列具体过滤条件。判定规则见 [01](01-product-rules.md)，原型配置见 [03 §5](03-consumer-project.md#5-b0-三个可直接搭建的原型夹具)。

| ID | 用例 | 明确期望 |
| --- | --- | --- |
| R01 | 同一冲突目标分别按文字/按颜色 | 使用独立冲突夹具，分别命中真实绿块/蓝块，不读污染或字体表现 |
| R02 | 普通/双目标及不合法配置 | 实际合法块数分别为 1/2；重复选项、缺色、重复标签拒绝加载 |
| R03 | 9 个点击格的翻转与旋转 | 范围、方向与位置排列正确；tileId/真实色/字义不丢不重，空槽守恒 |
| R04 | 固定互换与预告交换 | 配对按格位一次同时执行；预告与实际相同，错误不重抽 |
| R05 | 正确末步 | 计步分/奖励星/完美段后结束，不执行新机关、不扣定格 |
| R06 | 错误及重复错误回调 | 只扣 1，保留步骤/棋盘/得分/星，断连击/取消完美；游标不动 |
| R07 | 定格切换、错误、静态/末步 | 可撤销；正确才扣次数；错误取消待生效不扣次数；非法时不可启用 |
| R08 | 定格跳过交替机关、None→翻转 | 每个正确非末步都消费槽；None 不动棋盘不耗能力；方向不补放，实际动画次数不决定方向 |
| R09 | 奖励星结算时点 | 按点击前格位发一次；移动后站到星框不补发；错误/暂停不刷新星 |
| R10 | 污染与定格/末步 | 旧污染按正确点击到期；定格只取消新污染；角标/真实色判定一致 |
| R11 | 幂等计分/重开 | step/reward/segment/run 结算重复不加分；重开新局清空本局账本 |
| R12 | 分数样例 | P01 1100、P02 1000、P03 1100；16 步/2 星/4 完美段为 2600，一段出错为 2400 |
| R13 | 评级与纪录资格 | 失败只存未通关记录；练习不解锁/发评级；定格不降资格，提示影响资格 |
| R14 | 完整关卡分支和共享资源 | 所有合法分支可通关；不用定格可通关；≤配置次数至少有一条全星零错路线 |
| R15 | 总评级/里程碑 | 取各关最佳合计；反复重玩不累计多发 9/18/27 奖励 |
| R16 | 配置版本/种子 | 同内容同种子复现预告；不同规则/辅助/内容纪录不混比 |
| R17 | 巡游（B2 才执行） | 两处分叉符合明示条件；六站共享 3 容错/1 定格，站间不恢复，终点和失败仅结算一次 |

纯规则全星枚举可限定为当前实际关卡和合法决策；不要无边界穷举任意机关组合。若实现规模导致预计耗时长，按测试授权规则处理。

## 5. 产品 Unity、存储与手工验收

| ID | 场景 | 期望 |
| --- | --- | --- |
| U01 | 快速连点、同帧多指、动画/暂停输入 | 只接受当前一步一个有效事件，其他丢弃，不缓存下一步 |
| U02 | 空槽、缝隙、Popup 背后触摸 | 不扣容错、不推进、不穿透 |
| U03 | 返回/重试与旧动画同时完成 | 旧代次不能改新局、开旧页或发奖 |
| U04 | 设置、失焦/后台、恢复 | 停有效计时并遮目标/棋盘；恢复确认后继续，无后台扣命 |
| U05 | 第三次失误、成功、退出 | 跳转一致，免费重试/练习可用，下一关只在正式通关后解锁 |
| U06 | 配置数据族/槽位、首次读档、保存成功后重启 | 显式读取；仅 NotFound 新建默认，Loaded 使用进度；未结束局只恢复为练习 |
| U07 | 保存失败、提交未知、重复重试、坏档/未来版本 | 区分未提交与 CommitUncertain；后者停止写入并保留文件核对，不盲目重试或写默认覆盖 |
| U08 | 文字/颜色/污染/白黑块 | 固定底板和角标可辨；两种目标选项都可读，减少动画仍表达移动 |
| U09 | 声音关闭、图案辅助、设置变化 | 规则不依赖声音；图案跟真实色；辅助从新局生效并分开记录 |
| U10 | 选定设备比例/真实触屏/目标构建 | 分开填写实际覆盖；编辑器鼠标不代表手机、多指或构建通过 |
| U11 | 正常退出与取消退出 | 最终快照先入队再 Drain；检查失败/未完成请求；恢复请求入口符合契约，销毁不代替保存 |

终局保存应核对一次快照中分数、最佳、解锁及里程碑一致，不能只检查文件生成。后台/强杀因设备条件无法自动验证时，交付具体人工步骤与未验证状态。

## 6. 三短关的真人观察记录

邀请尚未读过策划的少量玩家，单目标/有选择版使用接近操作量和同一视觉风格，轮换顺序。记录实际人数与原话，不把目标 30～60 秒或小样本百分比当市场结论。

| 观察项 | 记录 | 用于决定什么 |
| --- | --- | --- |
| 能否说明“或” | 是否误以为要点两块、是否无需解释即可完成 | 目标栏与教学是否需要改 |
| 是否主动看后续目标 | 分支前看哪里，是否能说出下一块去哪 | 路线是否可规划 |
| 定格为何使用 | 第几段第几步、理由、忘用/随手用 | 能力是否提供可理解的取舍 |
| 漏星后是否主动重玩 | 是否换分支、是否只在提示后重玩 | 是否值得扩展路线题 |
| 错误与等待 | 误触/规则误读/动画难追踪、段用时 | 调规则还是调界面/节奏 |

技术正确后仍可能缺乏复玩意愿。若分支无区别、定格无价值或规划过重，先改短关并复测，再投入九关内容与正式美术。记录“需要人工试玩”并给出可运行入口即可如实交付自动工作，不能代替用户填写体验结论。

## 7. 框架交接单模板

执行者复制到本次实施记录并填写，未完成写明原因：

```yaml
status: planned / assets-ready / saved / sealed / published / consumer-verified
date:
workspace:
unityVersion:
frameworkVersion:
frameworkSource:
frameworkGitTag:
frameworkCommit:
packageGitUrl:
templateId:
templateVersion:
templateChannel:
templateFrameworkVersion:
templateContentHash:
templateVersionedContentHash:
computedContentHash:
parentId:
parentVersion:
parentContentHash:
computedParentSnapshotHash:
templateDocumentationEntry:
demoReplacementEntry:
actualModuleAndPageTypes:
dependencyDeclaration:
compileAndReloadEvidence:
relatedTestFilterAndResult:
playEvidence:
projectValidationEvidence:
freshConsumerDeploymentEvidence:
saved:
bumped:
committed:
published:
originalDraftBackupAndRestore:
remainingItems:
```

必须说明每条证据路径/工具结果、日期、目标工程和覆盖范围。填写多个真假状态时，以各项记录为准，不能用单一 `status` 掩盖未完成消费验证。

## 8. 产品交接单模板

```yaml
stage: B0 / B1 / B2
projectRoot:
productName:
unityVersion:
targetPlatform: undecided
installedFrameworkVersionAndSource:
activeTemplateIdVersionHash:
deploymentRecordAndBaseline:
rulesVersion: V2
contentVersion:
dataVersion:
implementedLevelsAndSegments:
entrySceneAndPlaySteps:
newAndChangedFiles:
compileAndReloadEvidence:
relatedTestFilterAndResult:
verifiedPrototypeRoutesAndScores:
fullStarReachabilityEvidence:
storageAndRecoveryEvidence:
uiAndDeviceEvidence:
humanPlaytestFindings:
buildEvidence:
knownIssues:
nextAction:
```

每次对话结束更新已有记录，避免下一对话根据“代码已经写了”推定模板保存、部署或测试也完成。若发现与本文不同的真实 API/规则，应先更新唯一正文与依赖任务，再继续实现。

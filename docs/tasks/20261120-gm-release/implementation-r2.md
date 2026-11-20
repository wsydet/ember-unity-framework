# R2：Ember 0.27.0 本地候选准备完成

2026-11-20，Asia/Shanghai。沿用完整模式及用户发布授权，承接[implementation-r1.md](implementation-r1.md)。**本地候选与相关自检完成，待主聊天独立验收；未commit/tag/push，不代表远端已经发布。** 本轮没有新建聊天或反向消息，没有消费升级、任务归档或清理。

## 最终结果与AC

| AC | 结果 | 依据 |
| --- | --- | --- |
| MA01 | 通过 | R1修正四个region，运行文本除region标题外不变；R2正式模板保存/继承及返回base后内容保持 |
| MA02 | 通过 | Apply/Inspect同一中性唯一回执路径策略；最终九目标回执已复制到final-authoring-receipt.json，原路径在final-confirmed.json，历史归档未覆盖 |
| MA03 | 通过 | R1维护编译证据保留，R2两启动合并及返回base均主动MCP请求、确认编译/域重载/已加载程序集；最终Console空 |
| TP01 | 通过/沿用 | base0.12.0、lane0.1.6沿用已封存结果，没有重复Save/Bump；base实际hash及活动记录确认一致 |
| TP02 | 通过 | fresh计划完成五剩余父同步；platformer/topdown经正式Load→Assets各补一行→编译→Save/Bump保留自有启动；七模板正式实际内容及父快照/指针校验通过 |
| RC01 | 通过 | 新CHANGELOG/发布说明/当前源码版本指针与0.27声明一致；正式脚本预览后write，最终声明从实际七模板结果生成；历史发布不改，57依赖仅框架tag变化 |
| RC02 | 满足候选检查，保留CHECK-N01 | 666路径选择性暂存、cached差异检查、共享二进制/字体通过。bundle34原字节及源提交溯源通过，直接-Check的换行退出限制保留，不伪称直接命令通过 |
| PR01 | 通过 | 四个R2进场保护文件原字节、三项用户修改未暂存、915受管项目文件恢复、原域重载选项/正常确认保留；无消费升级 |
| PB01 | 后续P1 | 独立验收后由主聊天派发布；HEAD仍3c83986，未commit/tag/push |

## 正式模板封存

| 模板 | 最终版本 | 实际/实时/封存hash |
| --- | --- | --- |
| base | 0.12.0 | cad30f499213dc7c946e32ae45dfffc7 |
| lane-defense-2d | 0.1.6 | 1d718d86736e081a29fc142ab62f194c |
| source3d-2p5d | 0.6.6 | e35e08a49f46c5e376b5f921c6ea2886 |
| ui-game | 0.2.6 | 5030a03ea18cd59fced92d37da85d5c8 |
| visual-novel | 0.22.7 | 69bb5bc67310964b04b996fb950f3103 |
| platformer-2d | 0.13.7 | 361466b7ac703e8f392a7f3d7ebc81eb |
| topdown-2d | 0.1.7 | 7c33795be43bed897056e1daaa07a3d4 |

全部frameworkVersion=0.27.0，六派生父版本均base0.12.0，父快照实际hash及parentContentHash均为根cad30f…；活动base记录未过期。`final-template-*.json`逐项使用项目中心同一TryValidateTemplateAssets后端（内部类只读反射调用），核对资源/.meta/GUID、真实hash、封存hash以及**当前父**指针；汇总templates-final-validation.json。不是只读metadata或以空graph替代最新父对齐。

所有快照变更只用正式ApplyParentSync/LoadTemplate/SaveTemplate/BumpTemplateVersion。最初尝试父同步后暂不Bump的计划被正式API拒绝（内容变更必须同时Bump），两份错误留存，没有绕过。故platformer0.13.5→0.13.6为父同步封存，再0.13.7为启动融合封存；topdown0.1.5→0.1.6→0.1.7同理。base/lane不重复升版。

## 覆盖保留与启动接线

source3d/ui-game/novel用新计划逐项选择：Binding仅生成时间不同的冲突AcceptParent；文件夹GUID、子设置页、小说字体/皮肤等KeepChild。平台/俯视的GameInit冲突先KeepChild保留业务，再正式加载到项目Assets各补显式InitializeUI，未增加ShowGM安装回退。

两份startup-merge.diff均仅新增一行，platformer保留PlayerLab准备/观察、PlayerConfig通道、PlatformerUITransition；topdown保留试玩存储注入和TopdownUITransitionHandler。编译后的实际OnEnter IL都含InitializeUI调用。子覆盖最终审计child-overrides-final.json共2918个文件：除两份授权启动增量外原字节保持。该数字是静态保留核对，**不是2918个Unity运行测试**。

正式加载前已告知目标及可能Skill/场景/未保存确认；预览Differences/Errors为空，没有绕过任何弹窗。保存使用原完整displayName/description。最后正式Load返回base，project-returned-base-differences.json为[]，915个受管文件与加载前逐字节一致。

## 编译、MCP恢复与日志边界

- 本轮Unity PID48700、工程root和6000.5.4f1经MCP确认。重启后对新PID只读句柄查询：R1三目录的旧四句柄已消失；五剩余正式同步全部成功。R1-B01已解除，无框架事务后端或第三方PackageCache修改。
- 平台MVID **75dff62f-a120-4be6-b0d5-4b33416d7017**；俯视 **cf4c57a9-0758-46a9-994d-6f638e0b47cd**；返回base业务 **26a2edc8-4c5e-45c5-bcd0-710c80cf6f0e**、Editor **58e99bc9-23f3-49b3-8881-4dbd816761db**。base源码未变，MVID与R1相同正常；本轮compile finished/domain reload after、新加载base类型而无两子专有类型共同证明返回已完成，不能仅以MVID相同倒推。
- 每批源码改动都主动MCP请求；返回base在Load同一RPC内明确调用CompilationPipeline.RequestScriptCompilation，写load-base-and-compile-request.json。结果未知时未重新编译或Load覆盖合并。断连具体为get_editor_state的ping not answered，代码探针有success=false/data=null。实例重新核对/选择，主进程到现有MCP的连接与导入worker已区分；没有凭UI可操作推断编译完成。
- 用户两次协助确认MCP/框架窗口状态。平台恢复后先见明确compiling，再等待完成；俯视状态第6次检查恢复（另有一次视图恢复探针未返回）；返回base恢复检查合计6次（5次资源状态重试及一次只读程序集辅助探针），均未达到10次上限。主状态重连循环按至少30秒间隔。早期同批查询在切换中返回空，正式加载/同步以实际回执核对后没有重复；后续按解码后的success控制依赖操作，失败查询本身不作为通过。
- 窗口工具不能枚举目标Unity6窗口只是自动化访问限制，未据此否定用户已打开/可操作的确认。只读查看插件现有机制：域重载用SessionState恢复，AutoStartOnLoad另为启动逻辑；实际读取该值true，未改偏好、端口、安全或凭据。
- 平台编译留下一次SourceAssetDB旧时间戳Import Error Code4及既有Consumer ActionContractProbe UAC1001警告。原platformer-console-confirmed.json完整保留；同步精确重导入源脚本，后续俯视和base完成状态/IL/Console分别确认。重导入后的早期Console查询曾断连，其文件不是“空Console通过”依据。最终base-console-confirmed.json及console-final.json均为空；不宣称全程零警告。
- 没有全量Test Runner、旧24AC/123项、六模板Play或长时测试；原GM独立验收由旧任务记录承接。实际覆盖为维护/Inspect、两模板编译及IL、正式封存与静态保留/谱系、候选完整性。

## 发布资料与候选

release-0.27.0.md、CHANGELOG、包README、Dependencies README、framework-progress及upm-migration-plan当前源码版本指针已对齐。公开说明使用发布身份可核验的中性表述；本地未发布的阶段状态只由本交接说明，未声称远端已存在tag。

release_metadata.py从真实封存模板先预览再--write。最终七项与后端校验记录匹配，57依赖manifest相对0.26.0仅com.ember tag改变，第三方/Unity基线不变。首次本地声明生成在最终base确认之前，随后只将**本任务新建且HEAD尚不存在**的两声明备份至metadata-before-final-validation，再用脚本重新预览/write补全实际验证事实和中性发布身份文字；manifest原字节不变。没有编辑模板metadata/hash，旧0.26发布文件保持原状。

原160条部分暂存完整保留，本轮509路径更新后合计**666个候选文件**（有重叠），精确清单candidate-paths.json，指纹candidate-fingerprints.json。按NUL pathspec逐项暂存，不git add .、不reset无关index。三项R2进场已有修改明确排除：DefaultLightSans、共享DingTalk SDF缓存、QualitySettings。未纳入其它文件。

shared-binaries --revision : --checkout通过（四TTF、两种autocrlf检出）；shared-font-config --revision :通过；cached --check通过。CHECK-N01沿用R1：直接bundle -Check未得到0退出；R2重验34原始bundle字节匹配manifest、源提交/HEAD/工作区与bundle仅文本换行等价。sourceCommit仍fcc06dcb65404036eb2c6908fcd68379f3306239，未改公共技能源/生成bundle或伪造溯源。限制与替代证据按release参考保留，不写成直接命令通过。

## 保护、最终现场与回交

R2进场新备份独立于R1，未用旧备份覆盖新缓存。最终四保护文件与R2进场一致：DefaultLightSans SHA256 **14C9718D55D832593C689643CD752A277E0915FE418BB71F8F85C883653C0E4D**，新共享SDF缓存19c5e337…、QualitySettings90a6246b…、EditorSettingsb4c08fea…。原三项未暂存修改保留；正式模板没有收进无关缓存。原DisableDomainReload、偏好与GameView保持，无全局SaveAssets。

final-confirmed.json/editor-final.json：base0.12.0/cad30f…/FrameworkScene、非Play/非编译/非更新，editingStale=false，graph=[]，本轮MCPDynamicCode pendingCallbacks=0，最终Console空。没有改变AutoStartOnLoad或其它连接设置，没有新增开放端口。

本轮本地准备无阻塞；主聊天下一步核对666条完整候选、CHECK-N01与验证边界，派原独立验收聊天。通过后再派P1选择性commit、annotated v0.27.0、origin/main和单tag atomic非force推送及远端peeled确认。用户原发布授权继续有效，无需重复审批；本阶段不代做P1。

本页证据简写均位于[evidence/r2/](../../../.utmp/ember-task-archives/20261120-gm-release/materials/evidence/r2/)，R1原始失败及备份保持不变。待验证边界：Player/VSync性能、移动/XR、真实触摸/多指/安全区/硬件切Hz、其它消费项目；不将七模板静态封存验证等同七模板运行验收。任务保留等待独立验收和发布，不归档、不清理。

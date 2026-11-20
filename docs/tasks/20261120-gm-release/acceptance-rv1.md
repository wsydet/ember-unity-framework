# RV1 独立验收：本地候选可进入 P1

2026-11-20，Asia/Shanghai。任务20261120-gm-release；完整模式，独立验收聊天01a1f39c-e305-7490-93a6-2f8bafb4524c / local。输入为当前授权、R1失败历史、R2实施及实际666条候选；未代改产品、封存结果或暂存区。

**结论：MA01—MA03、TP01/02、RC01、PR01通过；RC02满足候选检查，但继续保留CHECK-N01直接bundle命令限制。没有新增阻塞问题，R1-B01已解除。PB01远端发布仍待P1，不提前通过。** 主聊天可核对后按原授权派P1，无需重复发布审批。

本结论仅涉及当前维护、正式模板封存/同步及本地发布候选；不是已经commit/tag/push，也不是六派生Play、Player、硬件/设备或消费项目验收。

## 基线、编译证据与现场

- HEAD仍为 **3c83986075001f3b117c0d34f96429096fa701b1**，分支codex/release-0.26.0。先前FPS、GM和原任务记录的三个提交保持；本轮未提交。
- 新鲜MCP身份：Unity6000.5.4f1，PID **48700**，工程C:/Users/wuyu/My/ember-unity-framework；UPM实际Embedded com.ember **0.27.0**，解析位置为本仓库Packages/com.ember。
- 当前正式编辑 **base0.12.0 / cad30f499213dc7c946e32ae45dfffc7**，FrameworkScene，editingStale=false，非Play、非编译、非更新，MCPDynamicCode pendingCallbacks为空。
- 当前已加载Runtime MVID **26a2edc8-4c5e-45c5-bcd0-710c80cf6f0e**、Editor MVID **58e99bc9-23f3-49b3-8881-4dbd816761db**，分别匹配R2返回base的实际记录。当前已退出平台/俯视专有类型。

本轮没有修改代码或资源，也没有再触发编译或模板切换。按派发要求，复用仍与候选对应的R1维护编译及R2两启动合并/返回base的主动MCP编译证据。独立解码platformer/topdown/base的 **compile-confirmed** 记录，均含本轮start→finished→domain reload after，且均非编译/域重载待定；见evidence/rv1/reused-compile-evidence.json。没有把platformer-compile-completed.json中的失败ping当完成证据。

R2平台MVID75dff62f-a120-4be6-b0d5-4b33416d7017、俯视cf4c57a9-0758-46a9-994d-6f638e0b47cd及实际IL含InitializeUI的记录与最终单行合并源码匹配。base源码返回后未再变，MVID与R1相同正常，不能仅凭相同MVID推断没有重载。

本轮初次Console为空；七模板只读校验时发生一次工具空响应/传输中断，最终Console保留两条MCP WebSocket keep-alive/close警告，**没有编译错误**。后续状态查询恢复，校验原调用已完整写出七模板结果；没有重复提交校验、编译或Load。期间按有界规则间隔至少30秒再查询，静态检查继续进行。最终状态正常，不把空响应或传输警告写成产品编译失败，也不声称最终零警告。

## 逐项 AC

| AC | RV1结论 | 独立依据 |
| --- | --- | --- |
| MA01 | 通过 | 当前GMCenter四region为“内部参数/生命周期/内部方法/外部方法”；与HEAD比较，去掉region标题后源码一致。七模板共同维护文件均与当前Assets相同，未改运行行为。maintenance-static.json。 |
| MA02 | 通过 | 独立调用Inspect两次，均落`.utmp/ember-ui/gm-control-center/`，UTC时间+GUID+inspect路径不同、九目标绑定JSON相同。Apply/Inspect源码共用WriteReceipt，窗口显示LastReceiptPath；没有调用Apply重做视觉，没有写旧workflow/归档。inspect-receipts.json及实际新回执。 |
| MA03 | 通过 | R1维护和R2最后三组主动编译/重载证据仍匹配当前候选；本轮新鲜项目/包/模板/MVID/状态与Console核对完成。无新编译请求、无全量/旧123项测试。 |
| TP01 | 通过 | 正式Save/Bump回执和当前base实际资产/meta/GUID校验对应0.12.0；原完整displayName/description保持，actual=content=sealed=cad30f…，活动记录不陈旧。未重复保存/Bump或加载覆盖当前Assets。 |
| TP02 | 通过 | 七模板正式校验实际文件、版本、0.27兼容、封存及当前父对齐均真；六份ParentSnapshot逐文件原始SHA256与base全等。两子启动文件独立对照HEAD，恰好只加一行InitializeUI，原业务完整保留。2918个覆盖文件无额外变化。 |
| RC01 | 通过 | package/CHANGELOG/当版说明/四当前源码指针/新release与manifest一致。重跑元数据脚本只预览、退出0，两个输出JSON与当前文件完全相等。57依赖仅com.ember tag变化，历史发布、第三方和公共技能无无关暂存。 |
| RC02 | 满足候选检查，保留CHECK-N01 | 候选666路径/index blob/工作区SHA256与R2逐项一致；cached --check、shared-binaries index+两种checkout、shared-font-config均退出0。bundle直接-Check仍退出1；34原始bundle字节/manifest及真实源提交/HEAD/index/工作区的严格文本溯源独立通过，按release参考保留该限制，不称直接命令通过。 |
| PR01 | 通过 | R2两字体缓存、QualitySettings、EditorSettings均与本轮进场/R2记录原字节一致；三项用户修改未暂存。915受管项目文件与R2切换前逐字节相同，正常确认历史保留，无本轮模板切换、偏好/自动刷新配置修改或消费升级。 |
| PB01 | **待P1执行** | 当前HEAD未变，本地/远端v0.27.0不存在；未commit/tag/push。需要P1发布提交、annotated单tag、atomic非force推送并核对远端branch/tag peeled后再判定。 |

## 两维护修复与回执

maintenance-static.json对实际文件进行UTF-8读取，四标题正常；region标题之外与HEAD的源码一致。对七模板的GMCenter、GMControlCenterAuthoring、SoftGlassUIAuthoringWindow共同文件进行实际比较，均一致。

Inspect本轮实际输出：

- `.utmp/ember-ui/gm-control-center/20261120T041630954Z-1248b2228afc42b3b8c684bb27f09032-inspect.json`
- `.utmp/ember-ui/gm-control-center/20261120T041630956Z-a80ab1a4c05942d597832a73ca6b3053-inspect.json`

两文件存在、路径不同、内容相等、九目标原GUID/绑定条目对应当前正式资源。回执完整复制在inspect-receipts.json。Apply经源码确认使用同一写入函数；为保护已认可视觉，没有运行Apply。新落点无20261119历史任务常量，也不覆盖R1/R2已有回执。

原GM功能继续沿用docs/tasks/20261119-gm-control-center中的V2独立验收。当前业务源只改region注释，正式制作源只改回执/窗口接线；发布时的框架与模板复制/版本变更不构成重新运行原24AC/123项的理由。本轮未把历史GM通过结论冒充本轮模板Play。

## 七模板实际封存及谱系

本轮调用项目中心实际TryValidateTemplateAssets后端读取每份Assets，独立计算父快照hash，不只读取metadata。原调用因传输空响应未直接返回成功，但同一调用随后完整写出templates-live-validation.json；文件中七项actual/封存/当前父对齐全部成立，后续新鲜MCP状态恢复。未重复该请求或借机改metadata。

| 模板 | 当前版本 | actual/content/sealed hash |
| --- | --- | --- |
| base | 0.12.0 | cad30f499213dc7c946e32ae45dfffc7 |
| lane-defense-2d | 0.1.6 | 1d718d86736e081a29fc142ab62f194c |
| platformer-2d | 0.13.7 | 361466b7ac703e8f392a7f3d7ebc81eb |
| source3d-2p5d | 0.6.6 | e35e08a49f46c5e376b5f921c6ea2886 |
| topdown-2d | 0.1.7 | 7c33795be43bed897056e1daaa07a3d4 |
| ui-game | 0.2.6 | 5030a03ea18cd59fced92d37da85d5c8 |
| visual-novel | 0.22.7 | 69bb5bc67310964b04b996fb950f3103 |

七份兼容声明均0.27.0；六派生parentId=base、parentVersion=0.12.0、parentContentHash与父快照实际hash均cad30f…。parent-snapshot-raw-comparison.json进一步独立枚举六份父快照与当前base的文件集合/原始SHA256，全部逐文件相等；没有通过统一换行掩盖快照差异。graph=[]只是辅助结构检查，不作为父对齐的替代。

startup-merges-independent.json证明平台/俯视当前GameInit相对HEAD只多一行InitializeUI。平台保留PlayerLab准备/观察、PlayerConfig通道和PlatformerUITransition；俯视保留试玩存储注入和TopdownUITransitionHandler，没有AcceptParent丢子逻辑，也没有只KeepChild漏安装。其他子设置与小说皮肤/字体GUID等原覆盖通过child-overrides-independent.json的2918路径实际HEAD/工作区比较保持；它是静态保存核对，不是2918运行测试。

正式操作路径由R1/R2Save/Bump/ApplyParentSync/Load回执交叉核对；平台/俯视同步和单行融合分别为两次正式内容事务，因此各升两个patch，未重复升base/lane。R1失败及回滚记录保留；R2新PID句柄查询为空、正式source3d同步成功、其余完成封存，当前无残留模板staging/backup目录或pending delayCall。**R1-B01关闭**，没有修事务后端或手改快照绕过锁。

## 发布声明、暂存边界及CHECK-N01

candidate-boundary.json/candidate-final-protection.json逐项核对：666路径集合与R2相同，666个index blob相同，工作区原始SHA256相同；没有执行git add/reset或修改暂存区。非模板部分仅14个明确维护/发布资料/活动记录文件，其余为正式模板/父快照结果。字体和QualitySettings用户缓存不在候选中。

元数据脚本只预览、没有--write；metadata-preview-equality.json两项均true。release七模板字段逐项与真实template.json相同。schema/Unity基线/第三方仓库、标签、包列表、可选开发依赖等保留，manifest57依赖仅框架Git tag从v0.26.0改v0.27.0；未暂存旧发布文件、公共技能源或bundle变化。包版本脚本-Check退出0并显示已为0.27.0，未写入文件；R1原正式版本写入证据继续保留。

本轮规定检查真实结果：

| 检查 | 真实退出结果 |
| --- | --- |
| `python scripts/check-shared-binaries.py --revision : --checkout` | 0；四TTF原始完整性及core.autocrlf=false/true两个干净检出通过 |
| `python scripts/check-shared-font-config.py --revision :` | 0；workspace/index动态SDF配置/回退/受保护配方静态策略通过，不等于Unity渲染或TMP打包测试 |
| `git diff --cached --check` | 0 |
| `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/build-ai-skill-bundle.ps1 -Check` | **1；Bundle source differs. Regenerate before publishing.** |

CHECK-N01保持：当前34个bundle目标原始SHA256逐个匹配bundle.json。sourceCommit仍 **fcc06dcb65404036eb2c6908fcd68379f3306239**；HEAD源与记录源提交原始字节相同、index源与HEAD原始字节相同。每个文件先明确UTF-8文本/无NUL，再只做CRLF→LF对比，工作区源、源提交与bundle文本相同。没有对任何二进制做归一化，没有伪造sourceCommit、改源、重建bundle或把这组替代溯源说成直接命令成功。详细34项在bundle-independent-provenance.json。

release-workflow.md明确允许在证明bundle原字节符合manifest、源提交与工作区仅文本换行不同后如实保留此检查限制。因此本地候选可进入P1，但发布说明/最终交付继续保留CHECK-N01。首次Windows PowerShell启动还遇到执行策略、旧默认解码问题，原输出分别保留；随后用PowerShell7单进程参数执行同一-Check，得到了上述真实换行退出，不改全局策略或脚本来过关。

## PB01边界与P1交接

新鲜`git ls-remote`：origin/main=65ae680732323d83de9f898f89e9e8cc576ee4c8，v0.26.0 annotated对象864a7a9f9b5e821f6ccaf11a8ee4b1c9fad6b334、peeled同main；本地及远端v0.27.0不存在。origin仍https://github.com/wsydet/ember-unity-framework.git，远端main是当前HEAD祖先。

P1应先重新核对远端与这666条候选边界，选择性提交本次发布，创建不可变annotated v0.27.0，仅atomic推送`HEAD:refs/heads/main`和`refs/tags/v0.27.0`，不force、不批量tags、不切main覆盖Assets。推送后核对branch与tag peeled均为真实发布commit。该动作由主聊天派P1，本阶段没有执行，**PB01不能提前通过**。

## 保护与未覆盖范围

最终四项原字节保持且未混入暂存：

- DefaultLightSans：14c9718d55d832593c689643cd752a277e0915fe418bb71f8f85c883653c0e4d。
- **R2新共享SDF缓存**：19c5e3374e0b338ced5bced370471e86592fb761858ef4727eee414980209546。
- **R2原QualitySettings修改**：90a6246b84df30886fb817936db3934edd391241d003785606137c0e27014856。
- EditorSettings：b4c08fea9c6db7ad67634a84c258569354f4faacca43fefc16f9f6f1c18349a1。

没有以R1“共享字体clean”覆盖R2新字节。915项目文件返回基线无差异，Git路径/状态与RV1入场一致。没有更改偏好、自动刷新/连接设置或GameView，没有重做制作、Play、模板加载/切换、保存/Bump或新编译。本轮仅Inspect写两份中性回执和RV1证据/交接资料。

尚未覆盖硬件切Hz、Player/VSync性能、移动/XR、物理触摸/多指/真机安全区、六派生Play或消费项目升级；七模板静态/封存校验不等于七模板运行验收。未归档清理任务，未commit/tag/push，正常结束回交主聊天。

# UI 运行时扩展独立定向复验 A2

- 任务：20261116-ui-runtime-extensions；2026-11-17，Asia/Shanghai。
- 有效需求：requirements.md R1；参考 design.md D1、implementation.md S2、handoff.md H5。
- 阶段：实施返工后的独立定向复验。本阶段未实现产品修复，未改长期测试或需求；临时重放 A1 原探针并完成移除后的编译验证。
- **结论：UIRT-A01 通过独立复验，关闭；AC06 在原验收及本轮定向范围通过。UIRT-A02 / AC10 继续待用户决策，任务整体未完成，不能归档清理。**
- acceptance.md A1 保留失败历史，SHA256 仍为 `0BFF7FD325B2267DC52B8A49F91DFE897FC7E6613F01CE25A2896579C4F22761`。A2 是增补，不能将 A1 改写为实施自检通过。

## 1. 独立依据与实际基线

先读仓库 AGENTS.md、CLAUDE.md、实际包内 ai-workflow、架构、隔离、编码、公共 glossary、活动 topdown-2d glossary 和完整 unity-validation；先取得 R1 及当前 EUIAssetImageSlot / 两种 loader 源码并独立判断必要合同，再读 H5、A1、S2、D1 定位证据。使用 unity-mcp-orchestrator 技能；编译触发以仓库更严格的显式 compile=request 要求为准。

复用当前本地工作区：分支 `codex/local-snapshot-20261111-162819`，HEAD `16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2`。MCP 确认 Unity 6000.5.4f1，com.ember Embedded 0.25.5，实际源为 Packages/com.ember；uGUI 2.5.0。正式编辑身份 topdown-2d 0.1.2 / `d83863b16bda8c2655f21d6aa3d55693` 未变。

进入及结束都核对 owned-files.json 61 项、protected-baseline.json 中 ownedChange=false 的 13 项，全部匹配。独立比较 S1 清单，仍只存在 S2 声明的三文件变化：EUIAssetImageSlot.cs、EUIRuntimeExtensionsPlayTests.cs、EUIRuntimeMotionEditTests.cs；其余 58 owned 和 13 protected 保持。已列既有 .meta 不变，按钮、列表、Bridge、资源 Manager、模板身份和 package.json 保护指纹保持。Git 前后状态无新增差异。证据均在 evidence/recheck/：workspace-before.json、workspace-after.json、s1-changes-audit.json、protection-summary.json、git-before.txt、git-after.txt。

验证前重新复制全部共享字体目录 **34 个文件的完整字节**，包含 TTF、SDF .asset 内嵌 atlas、.meta，保存在 evidence/recheck/font-before/；font-manifest.json 记录路径、长度、源与备份 SHA256。font-after.json 确认结束时全部源与备份仍匹配，不仅记录了指纹。

## 2. 实际执行、精确过滤与结果

执行前已声明只运行受影响部分，预计用例数秒、PlayMode 连同启动不超过约 30–60 秒，编译/域重载另计；未跑全程序集、全工程或长时测试，未机械重复历史 98 项。

| 范围 | 精确过滤（公共前缀见下文） | 本轮结果 |
| --- | --- | --- |
| 图片 EditMode | `Ember.UI.Tests.EUIAsyncImageEditTests` | 14/14；两类型 ABA/反序、同地址 pending/成功、失败重试、直接与显式同值接管、同步 inactive/NativeSize/Clear/回调重入、真实 ResourcesProvider 与 SceneUI adapter、借入显示策略 |
| 隔离跑马灯 EditMode | `Ember.UI.Tests.TMPMarqueeEditTests` | 5/5；短文本、长文本复位及三个非法配置情景 |
| 原独立失败探针最小重放 | `Ember.UIExtension.Tests.EUIRuntimeAcceptanceProbe.RemovingNeverActiveLoader_UnmountsOwnedGraphicBeforeRelease` | 1/1；内含 Sprite/Texture 两条原失败路径，当前均通过 |
| 长期销毁回归 | `NeverActiveDestroyOrComponentRemoval_ReleasesPendingAndSynchronousTokens` | 1/1；内含 Sprite/Texture × pending/同步成功 × GO/仅组件销毁 8 组合 |
| 借入及目标丢失回归 | `NeverActiveRemoval_PreservesBorrowedOverrideAndDestroyedGraphicIsSafe` | 1/1；两类型借入图保留及目标已毁共 4 路径 |
| 隔离字体渲染 PlayMode | `MarqueeRealTime_AllMaterialMeshesMoveTogetherAndMasksRender` | 1/1；中文、独立 Symbols fallback、sprite glyph、两种裁剪和独立字体切换 |

后三项长期 Play 方法的完整公共前缀为 `Ember.UIExtension.Tests.EUIRuntimeExtensionsPlayTests.`，完整请求保存在 play-request.json。没有执行旧的共享动态 SDF / 全局 SetFontSkin、SetSkin 段。

**本轮合计 19/19 EditMode（3.216337 秒）与 4/4 PlayMode（1.1635876 秒）通过，0 失败、0 跳过。** Edit job `430205dfac12486cbd5a0655e56a04ec`，Play job `91c5c3c2a136401299cf1f49e097993c`；原始完整结果为 edit-result.json、play-result.json，不能把内部组合再次计成额外测试方法。图片测试的有意抛错回调被框架隔离并留下 warning，非失败用例或编译错误。

## 3. UIRT-A01 / AC06 的独立结论

**状态：已独立复验通过，关闭 UIRT-A01。** 原 A1 的 P2 缺陷和失败证据继续保留，不否定当时失败。

源码复核：UnmountOwned 先提取并清空 `_displayOwned`，再无 owner 门控地调用 detach；EUIImageLoader.Detach 依实际 Image 存活及 base/override 是否等于拥有资源决定卸载，EUIRawImageLoader.Detach 同样检查实际 RawImage 和 texture。Destroyed 仍有 `_dead` 幂等门控，显示卸载发生在 Slot.Clear/Dispose 之前。借入图不进入 Slot，目标已毁时不访问 Graphic。改动位于共享生命周期，无游戏或模板依赖。

直接重放 A1 已保存源码，未改变预期或断言。两种类型都观察到：

| 原失败路径 | MountedBeforeDestroy | LoaderDestroyed | GraphicAlive | MountedAtRelease | MountedAfterDestroy | Releases |
| --- | --- | --- | --- | --- | --- | --- |
| Sprite / never-active / 同步成功 / 仅移除 loader | true | true | true | **false** | **false** | 1 |
| Texture / never-active / 同步成功 / 仅移除 loader | true | true | true | **false** | **false** | 1 |

证据为 evidence/recheck/component-removal.json；与 evidence/acceptance/component-removal.json 中两条 MountedAtRelease=true、MountedAfterDestroy=true 的原失败观测对照。不是仅统计 release 次数。

长期 8 组合本轮原始观测在 loader-removal.json：同步四条均先确认已挂图；pending 四条未挂拥有资源；全部 ReleaseObserved=true，MountedAtRelease=false、MountedAfterDestroy=false，仅移除组件时 GraphicAlive=true，销毁 GO 时为 false。长期断言同时检查每请求释放一次、pending 取消一次 / 成功不取消、终态一次、迟到完成被拒绝。补充方法确认借入 override/texture 保留且借入对象仍存活，以及 required loader 移除后、Graphic 在 monitor 下一帧前已毁的安全路径。结合本轮图片 14 项和 A1 已通过的池化/隐藏/回调竞态证据，关闭该问题；未把测试 Provider 等同所有第三方资源后端。

## 4. 字体隔离复核及 UIRT-A02 待决

S2 夹具现从只读 TTF 通过 TMP_FontAsset.CreateFontAsset 创建新 FontAsset，独立字符/字形表、atlas 和 material；主字体与 Symbols fallback 为不同新建对象，另建 sprite glyph。文本/子网格先销毁，随后字体对象清理。EditMode 不再克隆共享 SDF；PlayMode 不执行全局 EmberFontSkins.SetSkin，teardown 也已移除全局 SetSkin。当前使用独立手动字体切换；这不构成本轮全局皮肤集成验证。

本轮两张 PNG 已独立查看：marquee-rectmask.png 可见中文、白色三角 fallback、青色 sprite 及右端文本裁剪；marquee-mask.png 同样可见并受黑色 Mask 区域裁剪。marquee-mesh.json 记录 3 个材质网格在 0.135089 秒内都向左移动约 5.22232 UI 单位，y/z 未变；结合公开网格断言，隔离渲染行为通过。S1/A1 的 4 材质历史不能被误报为本轮 3 材质观测。

验证前后 34 个字体文件及备份均匹配。钉钉源当前仍为 **`1869CB8EFA4FBA1D7AD971639D8E2262BC125528A2D112C48982F1117AB42719`**。没有覆盖或接受当前字体，没有以 HEAD、猜测补丁或 EA416A… 备份冒充 31A020…。

**UIRT-A02 / AC10 仍待判断。** 31A020… 真实旧字节仍缺失；A1 的 EA416A… → 1869CB… 变化只有单 atlas 4,281 字节的事实与两份完整 asset 证据继续保留。动态延后写回仍仅为可能解释，没有确证写入归属。本对话未收到用户保留或恢复决策。本轮隔离夹具没有新增磁盘写回，只能证明本轮保护结果，不能抹去旧争议。

解决路径保持 A1：用户明确接受当前 1869CB… 与早期增量无法精确还原的限制，或提供匹配目标的真实旧字节并明确授权恢复。EA416A… 只覆盖验收期间已知变化，不自动成为更早基线或覆盖授权。

## 5. 逐项 AC 当前状态

| AC | A2 当前结论 | 来源及本轮边界 |
| --- | --- | --- |
| AC01 | 维持 A1 通过 | 滚动源码与长期滚动测试未变；本轮未重跑几何，仍以 A1 的独立 17 情景和历史 PNG 为据。 |
| AC02 | 维持 A1 通过 | 动画/拖拽/惯性/吸附实现未变；本轮未重跑事件输入，A1 自动事件范围保持，不提升为人工设备签收。 |
| AC03 | 通过（行为范围） | 本轮 5 项隔离 EditMode 及真实 Play 渲染通过；没有修改共享字体/布局/材质真源的新增磁盘变化。旧字体保护独立计 AC10。 |
| AC04 | 维持 A1 的历史支持范围通过，隔离夹具补证通过 | 本轮验证改文本/宽度/独立字体、TMPEx、独立 fallback/sprite、Mask/RectMask2D；**没有新增全局皮肤覆盖**。A1 的历史 SetFontSkin/SetSkin 结果保留且受字体保护争议约束。3D/RTL/多行/多位置效果仍为 D1 非支持范围。 |
| AC05 | 通过 | 本轮图片 14 项含同步、真实 ResourcesProvider、借入显示策略；Binding 本轮未改，保留 A1 正式生成验证。 |
| AC06 | **通过；UIRT-A01 关闭** | 原探针两条、长期 8 组合和借入/已毁 Graphic 4 路径通过；同时核对实际引用和释放次序，见第 3 节。其他既有竞态范围由本轮图片测试及 A1 证据承接。 |
| AC07 | 维持 A1 通过并有本轮相关补证 | Resource 实现未变；本轮实际 ResourcesProvider / SceneUI HandleAdapter 及一次释放相关断言通过。未交付第三方 Provider。 |
| AC08 | 维持 A1 通过 | ImageEx/圆角实现未变，A1 独立验证及 PNG 保持，本轮未重新渲染圆角。仍仅 Simple。 |
| AC09 | 维持 A1 通过 | Binding/依赖方向、按钮/列表源码等指纹保持；本轮未跑全量回归。S2 夹具去全局皮肤 teardown 不改变这些产品源码。 |
| AC10 | **待用户决策；UIRT-A02 未关闭** | 本轮主动编译、域重载、Console、独立测试、完整字体保护和文档记录满足；旧字体字节归属/保留仍未决，不能整体通过。 |

未把未重跑项称为本轮新增覆盖，未将任何必要 AC 改为不适用以规避未完成项。

## 6. 编译、临时探针清理与最终编辑器

探针加入后主动 MCP all/force、compile=request：本轮开始 `1794846997374`、结束 `1794847004586`、domainReloadAfter `1794847012960`；Play 测试程序集新 MVID `b886b6bc-ce59-4df9-822d-e9238374eaaf`，ProbeLoaded=true，非编译/更新，Console 错误 0 后才运行测试。证据 compile-request.json、compile-completion.json、compile-assemblies.json、compile-console.json。compile-state.json 和名为 compile-ready.json 的文件实际记录短暂 ping 失败，不能单凭文件名当完成证据。

清理指纹保护首次中止删除：探针出现 UTF-8 BOM，5,614 → 5,617 字节，解码后源码完全相同，未归属写入者。原始与实际编译字节、.meta、probe-byte-diff.json 全部保留。首次名为 cleanup-compile-request.json 的请求发生于删除中止后，仍含探针，**不计移除完成**；其完成状态在 pre-removal-compile-completion.json。本阶段没有将该请求当最终清理成功。

核对文本相同并保存实际字节后，精确删除本轮临时 `.cs` 和 `.meta`，再次主动请求编译。最终开始 `1794847201661`、结束 `1794847208337`、domainReloadAfter `1794847215933`；证据 removal-compile-request.json、removal-compile-completion.json。最终 Play 程序集恢复 MVID `31251a40-03b5-49a2-b0e7-03718b52bdea`，ProbeLoaded=false；Runtime `3b807385-71e5-4803-80f8-1253147aee77`，Edit `f923448d-4624-4d2a-9fae-3af5a86bb08f`。程序集恢复为原 MVID 与本轮移除后编译事件、类型消失共同证明清理，不能仅因 MVID 相同忽略本次域重载。

短暂状态查询失败后按 30 秒间隔做一次恢复重试；首次阶段重试的状态仍未回答，但随后 MCP 只读程序集查询成功，未重提未知编译请求；后续两次域重载各一次恢复查询成功。没有 10 次耗尽、盲目重复测试、Editor.log 轮询、BatchMode 或 dotnet 替代验证。

final-editor.json / final-console.json：FrameworkScene，非 Play/编译/更新，timeScale=1，无测试 ResourceManager / IResourceProvider，ProbeLoaded=false，Console 错误 0。protection-summary.json 确认探针 `.cs/.meta` 已不存在。最终编译后不再更改代码或运行新测试。

## 7. 回入口的真实状态

独立定向复验结束，不等于任务整体完成。H6 为当前交接，H5 完整快照在 evidence/recheck/handoff-H5.md；A1 和原失败原始证据保持。未新建 worktree/分支，未操作模板/版本/提交/推送/发布/部署，未创建新对话或向其他对话发消息，未做入口归档或清理任务目录。

回入口可复制：

> 接续任务 20261116-ui-runtime-extensions，请读取 .utmp/ember-workflow/20261116-ui-runtime-extensions/handoff.md H6、requirements.md R1、acceptance-recheck.md A2，并保留 acceptance.md A1 历史。独立复验 19/19 EditMode + 4/4 短 PlayMode 通过，原失败探针两类型已确认先卸实际 Graphic 引用再释放一次，UIRT-A01 关闭；34 个共享字体完整备份及源文件本轮未变。UIRT-A02 / AC10 尚无用户决策，当前钉钉 SHA256 必须保持 1869CB8EFA4FBA1D7AD971639D8E2262BC125528A2D112C48982F1117AB42719，31A020… 真实旧字节仍缺失。请先承接待决状态；现阶段不标整体完成、不归档清理、不覆盖字体、不提交或发布。用户明确决定后再按真实剩余工作核对收束条件。

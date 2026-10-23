# Loading 解耦复审、术语字典与 MCP 更新

2026-10-23。本记录接续 [首次交付](loading-curtain-delivery-20261023.md)。仅修改框架工程；Windward、Mountain 与 ember-thirdparty-upm 只读；没有提交、推送、发布、消费项目升级或部署。框架包仍为 0.21.3，此工作区新增 API 不代表已发布的 0.21.3 已拥有它们。

## 复审结论与修复

通用会话直接打开传入的 Loading 页面，可在没有场景请求时覆盖、保持、暂停、揭幕。场景事务仅在 EUIManager.SceneLoading 适配器关联请求与 HoldPresentation。旧 TransitionOperation 输入保留给已有进度展示兼容，通用会话不通过它决定揭幕。没有引入 Windward 类型或产品时长到通用运行时。

本轮修复：

1. 资源提供者同步抛错：显示队列处理标志在 finally 复位，所属会话明确失败，下一次请求可以继续。
2. OnPageCreated 扩展点中取消：先绑定所属页面再调用扩展，取消后不继续路由/打开。尚未初始化但已经绑定的逻辑也执行 Dispose。
3. 重复与迟到资源回调：一次资源获取只消费首次回调；取消后的首次迟到获取归还自己的引用；重复通知不能创建第二个页面或释放后续请求的引用。
4. Cancel 的取消注册或 HideAllImmediate 抛异常：继续清理其他动画、隐藏页面、移除活动/关闭追踪、释放对象、通知全部展示持有并结束阶段任务。首个错误保留在任务结果中，幂等释放不再次抛出。
5. 失败的兄弟阶段任务：Covered 失败后调用方通常不会再等待 RevealFinished。内部观察这两个可重复等待的完成源，避免无人消费的兄弟任务在 GC 时制造未观察异常；外部 await 仍能取得同一错误。
6. 外部 ClosePage/ClosePageByDef 取消会话；直接经 ViewEngine 关闭时也检测页面状态并终止凭证，不等延迟销毁超时。覆盖渲染屏障后还会检查页面仍为 Opened。
7. 受控动画按 IEUIControlledTransitionEffect 直接发现，不再隐含要求实现旧 IEUITransitionHandler。新增测试组件只实现受控接口，验证取消注册及清理异常路径。

公开 API、取消策略、完成阶段与接入代码以 [Loading 会话契约](../../Packages/com.ember/Documentation~/ui/loading-sessions.md) 为准。普通自动 Loading 保留。外部拥有遮幕时允许内层 Completion 先结束，WhenInteractive 继续等待表现成功；业务决定揭幕的代码不能先等待 WhenInteractive。

## Windward 示例核对

只读核对 WindwardLife、WindwardFlowModule、GameSettingsState、PlayerCheckpointRecovery、PlayerModule 和复生状态。正式指南已替换原先 WaitForRevealPermission 等占位调用，补齐具体字段、Recover 方法、ReleaseSession 清理、用户暂停 setter 和 Settings Push/Pop 的接线位置。

- 保留真实 WorldResetScope → RestoreCheckpoint → RequestInPlace 链，恢复成功返回 Completion 后内部 Pause 释放，外层仍 Covered。
- 使用现有 DeathElapsed、RebirthElapsed、RebirthPending 等字段；0.5 秒隐藏和 0.53333336 秒动作仍由原复生状态驱动，约复生 0.2 秒触发揭幕，动作许可不交给遮幕。
- 独立记录用户暂停，避免用 timeScale 或包含事务暂停的 PausePresentation 猜测用户意图；同时修正说明中运行计时器的暂停来源。
- RecoverAsync 返回 false 时原组件保留 Pause。示例明确采用终止策略：Faulted、禁用恢复组件触发其现有 OnDisable、停止玩家会话，再清除本次遮幕。没有调用不存在的公开释放暂停方法，也不保存半恢复快照。
- 删除旧遮幕时仍保留 Flow 的 AdvancePresentation 与动作/音频业务，并修改旧页面就绪门槛。

这是按真实源码编写的消费修改说明，不是已应用的 Windward 补丁；尚未在 Windward 编译或人工接入验收。消费项目仍等待独立升级授权和正式发布。

## 重编译规则与字典

- [Unity 验证规则](../../Packages/com.ember/Documentation~/maintenance/unity-validation.md) 在原重连规范旁明确：每批代码修改后主动通过 MCP 请求重编译，确认本轮编译/域重载与新程序集加载，再读 Console，随后运行增量验证。纯文档不要求编译。
- 根 AGENTS.md、CLAUDE.md、包 README 和 EmberProjectRulesSync 消费入口同步此要求；安装/升级后的各模板项目通过同一公共入口读取。
- [通用字典](../../Packages/com.ember/Documentation~/maintenance/glossary.md)：框架、模板、双方版本、保存/Bump/部署/升级、Loading/遮幕/场景、完成阶段和暂停等。
- platformer-2d 字典（工作区维护源 `Assets/Game/Documentation/platformer-2d/glossary.md`，未封存分发）：玩家/实体、Scheme/Feel/Powers、冻结 A、动作所有权、外部运动、检查点和原位恢复等；模板 README 加入导航。

字典只解释用语，不扩大操作授权。公共正文随框架；专属词汇维护在对应模板 Assets 文档。platformer-2d 字典及导航已写入维护源并由 Unity 生成 meta，**本轮没有中心保存或 Bump**：当前 SaveTemplate 是整份业务层复制，工作区存在大量任务前模板改动，不能仅为字典把它们一起收进模板；也没有直接编辑 Templates~/ParentSnapshot~/hash。后续普通模板保存时纳入这份维护源，再单独决定 Bump/发布。

## 编译、增量测试与首次失败

已在每批代码修改后主动 refresh_unity(compile=request)，通过 execute_code 确认非编译/非更新状态、新程序集 MVID/新增成员，再读 Console。最终运行时 MVID：`9a7c18dc-4d43-4345-8970-1eef3ebc99a4`。可选目录变更后的 UPM 编辑器程序集 MVID：`a39b0a6a-81e4-4b28-b5df-2079f33ad300`，已加载目录版本 10.3.0。

| 范围 | 结果 | MCP job |
|---|---|---|
| 遮幕生命周期/场景适配/暂停/进度（最终版本） | 32/32 | 53ad958b6e214790a59f500003208de3 |
| 公共规则入口同步 | 18/18 | 7d2c474cc072445690c6071fd11e2001 |
| Inspector/Prefab contents 预览及参数实帧 | 4/4 | f19bcf367d5447ac8e1b8d0a7ebc162c |
| 可选第三方包目录与现有安装保护 | 9/9 | 08b53a70e41940fa84e9c75562f8698f |
| 新服务端重启后的关键 PlayMode 冒烟 | 3/3（上述范围的重复验证） | afa730f23ea84b24904cd08c3ba7ef9e |

原始测试详情与环境备份在 [证据目录](evidence/loading-curtain-review-20261023)。只跑新增/直接影响的类，没有全量测试。

合计 63 个不同用例通过，另有新服务端 3 个重复冒烟。最终读取 Console 无错误或警告。

首次失败及处理：

- job 4855189504334cbba39df26d2aa24f6d：新增三个资源/创建取消复现全部失败（队列同步异常、活动页面 1 而非 0、重复回调页面 2 而非 1），修复后均通过。
- job a5a10aaeea294d6a833348155ab73d98：31 项中两个新增测试失败。一个夹具把页面放入预加载字典但未加入路由，按定义关闭找不到它；改用明确页面关闭，再补 ViewEngine 路径。另一失败进一步暴露受控接口仍依赖旧 Handler 的真实限制，最终改生产发现逻辑，让仅实现受控接口的组件通过。测试残留的未观察阶段错误也促成兄弟完成源清理修复。
- job cd5f0be0bc2b48ef803c80ec91d06791：23/31 后 Unity Test Runner PlayModeRunTask.cs:52 自身空引用，未获得最终结果，不记通过。清理孤立 job 后，63116c5abcf64bf990fee96635e7b61e 未能初始化；没有用空 Console 替代通过结论。
- MCP 10.3.0 安装、主动编译和重连后，相同范围 31/31 通过；补上底层视图关闭测试后最终 32/32 通过。
- 编译/更新重连均执行 30 秒间隔；检测断线后首轮重试恢复。服务端重启产生的 WebSocket 警告/短暂连接失败已保存到 mcp-restart-console.json，重新连接后再清 Console 并做新服务端冒烟。

参数预览复用了正式 Loading Prefab 的临时 contents/克隆，未写默认样式。新一轮生成 [预览图库](evidence/loading-curtain-20261023/preview.html) 和 samples.csv：进入末帧黑色 518400/518400 像素，过程形成横向错开条带；16:9 裁切的条带并非等高。原先记录的 3 列、0.75/0.6666667 秒、0.08 错开、X EaseInOut 3→0 参数与限制继续成立。

## MCP 更新与消费边界

官方稳定发布：[Unity MCP v10.3.0](https://github.com/CoplayDev/unity-mcp/releases/tag/v10.3.0)。

- 框架工程 UPM 从 10.1.2/main 更新到官方 `#v10.3.0`，安装 job 47e6a106cf5542c08d86bcfcac6fcd5b 成功；lock commit 为 aa5fc638d623f56178d50329d9ad8c541a57fe66。与本轮备份比较，manifest/lock 只有 MCP 项变化。
- 官方 Python 服务端 10.3.0 已通过 uv 安装；最初沙箱缓存写入被 Windows 拒绝，随后获自动审核允许在用户缓存完成安装。
- 原服务器来源覆盖固定为 mcpforunityserver==10.1.2，已改为 ==10.3.0。延迟回调未实际重启，直接调用官方 StartLocalHttpServer 后完成重启；通过当前 8080 监听进程的虚拟环境元数据核实实际服务端为 10.3.0（不是把 FastMCP 协议自身的 3.4.11 当作产品版本）。Unity 端运行时也读取到 10.3.0，服务 managed/alive 均为 true。
- 本地 ember-thirdparty-upm 没有 MCP 包；MCP 原本就直接取官方 Git。框架可选包目录另行补至 10.3.0，并保持已安装包不自动替换的现有策略。
- **其他项目升级 Ember 不会自动更新 MCP**。包含本改动的正式框架版本只更新可选安装入口，已装项目仍需单独显式 UPM 更新并核对服务端配置。共享服务器升级不等于 Windward Unity 包升级；本轮没有修改 Windward 文件。
- 旧 Dependencies~/manifest-0.21.3.json 是历史发布基线，未改写；没有发布第三方镜像、框架 tag 或模板版本。

消费安装/更新说明也写入 [包 README](../../Packages/com.ember/README.md#unity-mcp-的安装与更新)。

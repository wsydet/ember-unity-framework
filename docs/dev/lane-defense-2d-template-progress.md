# lane-defense-2d 实施记录

> 2026-10-29 发行续接：用户已授权发布当前框架及全部已保存模板。最终版本、模板封存 hash、验证与限制以 [Ember 0.25.0 发行记录](release-0.25.0.md) 为准；下方未发布/未 Bump 表述保留实施当时的历史语义。

日期：2026-10-29。执行依据：[F0～F7 执行文档](lane-defense-2d-template-execution.md)。

## 当前结果

基础模板候选已实现并通过项目中心普通保存，正式编辑身份为 **lane-defense-2d 0.1.0（preview）**。保存后项目中心比较为 0 错误、0 警告、0 差异，父同步 Synced；编辑器已退出 Play，停留 FrameworkScene，无未保存场景。

本轮唯一执行的 Bump 是用户单独授权的 **base 0.10.0 → 0.10.1**。新模板自身没有 Bump，包仍为 0.24.3，没有 Git 提交、推送、tag 或正式发布。开发验证与独立消费验证分别记账。

完整消费交接、版本/hash、接口签名、路径及待发布清单见 [交接记录](lane-defense-2d-template-handoff.md)。

## 阶段实绩

| 阶段 | 实际完成 | 验证和下一入口 |
| --- | --- | --- |
| F0 | 保护原 topdown 现场、核对 base 实时与封存差异、经授权中心 Bump base；中心创建直接继承 base 的新模板；用户人工确认加载 | 原工作备份 1,111 文件经 SHA-256 校验；正式新身份有效且未过期。历史见 [F0 记录](lane-defense-2d/evidence/F0/record-before-implementation-completion.md) |
| F1 | 固定逻辑二维坐标、会话/实体 ID、原子放置命令、移除原因、时间与终局优先级，冻结少数消费策略接口 | 随模板 RuntimeContracts / ExtensionGuide；产品弹道由唯一会话调度，不附带产品装置 |
| F2 | Gameplay Module、SceneParticipant、正交棋盘、拾取/预览/提交、拆除和 EUI HUD/卡牌 | Input System 注入点击实际放置；资源 650→600；UI 点击不提交棋盘；配置和表现准备校验 |
| F3 | 固定 lane 敌人、同路默认射手、阻挡攻击、二维扫掠投射物、去重、TTL/边界/终局清理 | 规则及扩展测试通过；场景内按实际时间完成战斗，独立 URP UI 栈所有权已验证 |
| F4 | 波次/空波/波间、胜败、暂停/恢复、重开和返回；5×9/两波与3×6/三波两套配置 | TrainingYard 7 击杀、0 逃逸胜利；CompactYard 5 逃逸、基地 0 失败；结果各一份；重开新代次清空实体 |
| F5 | 工作台、只读校验、波次/棋盘预览、正式启动试跑、Inspector 及菜单/操作身份守卫；扩展指南与中性测试策略 | 当前模板真实菜单可打开；捕获窗口身份失配时停止操作并保留所选配置。完整跨模板/消费身份矩阵仍待真实工程 |
| F6 | 所有代码批次主动 Unity MCP 编译和新程序集加载确认；35/35 新增 EditMode 用例；正式 Play 闭环及两比例画面检查 | 独立消费 T12 留消费 C1；完整故障注入、设备/压力/构建验收未执行，不能称全量通过 |
| F7 | 项目中心普通保存 795 文件，保存后磁盘/编辑副本/metadata 一致，父基线一致；整理发行准备 | 内容已保存，未封存/未发布；建议候选包 0.24.4、模板 0.1.1，均仅供后续确认 |

## 实际身份

| 字段 | 值 |
| --- | --- |
| 当前包 | com.ember 0.24.3，本轮未发布 |
| Unity / URP | 6000.5.4f1 / 17.5.0 |
| 模板 ID / version / channel | lane-defense-2d / 0.1.0 / preview |
| 实时 contentHash | 8ee687e49bda1147509317eb2177fec3 |
| versionedContentHash | e9d789311cad169a19b6a76331a2877a（创建时基线，新实现未 Bump） |
| 父 ID / version / hash | base / 0.10.1 / e9d789311cad169a19b6a76331a2877a |
| ParentSnapshot 实测 hash | e9d789311cad169a19b6a76331a2877a |
| 保存时间 | 2026-10-29 14:47:29 |
| Git HEAD | b04da0de17617a1a38685b0d2aebb557b05405b7，工作区未提交，HEAD 不包含本轮实现 |

## 验证证据

- [最终编译程序集 MVID](lane-defense-2d/evidence/F6/compile-final.json)、[最终 Console](lane-defense-2d/evidence/F6/console-final.json)。
- [35 项测试详细结果](lane-defense-2d/evidence/F6/editmode-final-35.json)，仅 LaneDefenseTests 与 LaneDefenseAssetTests；早期24项/34项结果不替代最终35项。
- [胜利](lane-defense-2d/evidence/F6/runtime-won.json)、[失败](lane-defense-2d/evidence/F6/runtime-lost.json)、[重开](lane-defense-2d/evidence/F6/runtime-restart.json)、[退出](lane-defense-2d/evidence/F6/runtime-exit.json)。
- [EUI 中心创建](lane-defense-2d/evidence/F6/eui-created.json)、[场景与配置生成](lane-defense-2d/evidence/F6/assets-created.json)、[窗口失效保护](lane-defense-2d/evidence/F6/tool-scope.json)、[UI 遮挡与编辑器内存样本](lane-defense-2d/evidence/F6/runtime-ui-block-and-profiler-sample.json)。
- [中心保存](lane-defense-2d/evidence/F6/template-save.json)、[保存后校验](lane-defense-2d/evidence/F6/post-save-validation.json)。
- 最终画面：[16:9 棋盘](lane-defense-2d/evidence/F6/final-ready-2560.png)、[4:3 棋盘](lane-defense-2d/evidence/F6/ready-1280x960.png)、[暂停](lane-defense-2d/evidence/F6/pause-2560.png)、[胜利](lane-defense-2d/evidence/F6/won-2560.png)、[失败](lane-defense-2d/evidence/F6/lost-1280x960.png)。画面为 MCP 采集与检查，不是用户人工签收。

最初试跑的抽象 TMP 文本绑定、Single Sprite 导入和 URP UI 栈问题均已修复。证据目录保留中间截图，不能把黑屏等中间失败截图当作最终验收图。开发过程中有内存工具查询编译错误，未写入项目 C#；实际项目编译缺陷已修复并主动重新验证。

## 现场与边界

原 topdown 备份为 `Library/EmberBackups/lane-defense-2d-F0-20261029/`；恢复时先保护当前 lane-defense 工作，再经项目中心加载原模板并保留人工弹窗。禁止 reset/clean 或用备份目录覆盖其他会话后续工作。

当前新增业务全部在 Assets 开发；Templates~ 和 ParentSnapshot~ 只由中心创建/保存，未直接编辑或手改 hash。GameGameplayState 两个 EmberManaged 块与父快照逐块比较一致。没有引入 Timid Fish 代码、产品命名或特殊装置，未修改 Windward 等无关工程。

独立消费部署、完整工具身份矩阵、准备失败/取消故障注入、强制失焦、实机/Player 构建和压力性能仍待后续验证。模板自身 Bump、提交、推送、发布均等待独立授权，当前不执行。
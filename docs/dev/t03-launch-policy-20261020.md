# T03 弹射策略维护记录（2026-10-20）

状态：Assets 实现已完成 Unity 编译、27/27 专项、679/679 PlayMode 交叉回归及 76/76 Editor 回归，均无跳过。随后按用户发布授权封存 platformer-2d 0.9.0，纳入 Ember 0.20.4，见[发布说明](release-0.20.4.md)。本记录不签收 Windward S3。

需求来源：`C:\Users\wuyu\My\Windward\docs\implementation\S3-T03-axis-wall-qualification-gap.md`；本轮读取用户粘贴的同名交接内容。P02/P03 的旧项目证据属于交接材料，本轮未运行 Mountain，未操作 Windward。

## 归属与基线

- 分类：模板升级。Entity 弹射契约和 Player 运动适配均属于 platformer-2d，不提升为框架 Manager。正式分发仍需后续框架发布。
- Unity 实例：`ember-unity-framework@b23e9b768e85ed30`，Unity 6000.5.4f1，Edit/FrameworkScene。
- 当前框架 0.20.3；编辑模板及已封存模板 0.8.0，contentHash/versionedContentHash 均为 `003c4429f265bbcd8d377bf375c2c15c`。
- 上述版本/hash 是修改前的正式基线，不代表包含此次修改。没有生成新的正式版本/hash。
- 工作区原有大量 Assets 增删改；本轮受影响旧文件先按工作区现状复制至 `Library/T03ContractBackup/`。未提交、推送、发布、部署或修改消费项目。

## 实现

权威 API、默认值、配置、冻结与清理语义见 [模板说明](../../Assets/Game/Documentation/platformer-2d/launch-policy.md)。

- LaunchRequest2D 增加 Horizontal 和 WallQualification 两个可选枚举；旧调用和资产默认值兼容。
- PreserveControl 仅保持 Y，继续原水平控制，保留 AddVelocity 和支撑的贡献分解；行动输入继续抑制。
- 新策略生效时阻止墙滑/挂边自动入口抢占首个下落物理步；默认动作规则不改。
- 刷新首次墙跳资格在登记成功后、Accepted 回调前执行；默认不刷新。
- 载具公开保留水平驱动入口，使用当前 Y，支持消费方继续计算水平风驱。未导入产品公式。
- 待执行的 StopVertical 纳入 HasTransientMotion，避免到期和实际停速之间采集不完整快照。
- 没有修改 PlayerFeel、候选方案、全局重力或动作参数。新配置是请求值和机关序列化字段，不进入方案复制/比较链。

## 前两次验证历史（已由后续重连继续）

1. Unity MCP 资源查询成功，连接已明确固定在框架仓库实例。
2. `refresh_unity(mode=force, scope=scripts, compile=request)` 返回 compile_requested=true；随后状态明确 is_compiling=true。
3. 随后 Console error 查询返回 0 条。该查询不能单独证明最终代码或新增测试编译通过。
4. 调用 `run_tests(mode=PlayMode, group_names=["LaunchPolicy_"], init_timeout=120000)` 未取得作业结果：
   `Unity plugin session dea700fe-5a0f-44cd-beec-04acff718aea disconnected while awaiting command_result`。
5. 按仓库规则停止自动验证，没有重连重试、日志轮询、BatchMode 或 dotnet 替代。断开后又补充了边界实现和测试，因此最终代码未完成 Unity 编译验证。
6. 静态检查只核对本轮文件差异、策略默认值、调用签名、链接和 .meta；没有自动回归通过证据，也没有实际帧率数据。

当时未完成 Unity 编译验证；后续在用户明确允许等待域重载并重连后继续，见下文。

### 用户授权重连后的第二次尝试

2026-10-20，用户要求重新连接框架 Unity MCP，继续编译、专项和交叉回归，禁止提交、推送、发布或升级 Windward。

- 实例列表确认框架重新连接，会话为 `a603420c-3619-48d3-a022-3cd2851a7740`；明确选中 `ember-unity-framework@b23e9b768e85ed30`。
- 首次编辑器状态查询成功：Edit/FrameworkScene，没有运行中的测试，没有遗留 current_job_id。
- `refresh_unity(mode=force, scope=all, compile=request, wait_for_ready=false)` 成功返回 `refresh_triggered=true`、`compile_requested=true`。
- 两次状态返回 `is_compiling=true`，编译开始时间戳 `1792474454431`。随后等待15秒，再读取状态失败：
  `Unity session not ready for 'get_editor_state' (ping not answered); please retry`。
- 按仓库 Unity 验证规则在本轮首次查询失败后停止；没有继续重试，没有使用日志、BatchMode 或 dotnet 替代。
- 本次没有启动专项测试或交叉回归，没有取得编译完成、编译错误或测试断言结果，不能据此判定编译成功、代码故障或 Unity 崩溃。
- 本轮只补充验证记录，未改运行代码、测试源码、模板版本或消费项目。后续仍需先完成编译，再执行上述测试。

## 第三次验证：等待域重载并重连

用户明确允许编译域重载期间等待后重新连接框架项目，不因首次 ping 失败停止。重新连接会话 `33f4bc9b-cb6b-44a5-a1ab-f7f0572c318a`，再次固定框架实例；通过 Unity MCP 刷新及编译，编辑器恢复 idle，Console 没有编译错误。未使用外部编译器、BatchMode 或日志轮询。

| 运行 | MCP job | 结果 | 原始证据 |
| --- | --- | --- | --- |
| 首轮专项 | `e07f5f584a094c43b7d895bb2887bfc2` | 25 项中 6 失败 | [MCP 失败列表](evidence/t03-launch-policy/special-01-mcp.json) |
| 修复后专项 | `112373151f6a404a8c1861227c28c46f` | 25/25 通过，0 跳过 | [NUnit XML](evidence/t03-launch-policy/special-02.xml) |
| 扩展专项 | `3ed305b2d0c64c5b868d7f6610e94d08` | 27/27 通过，0 跳过 | [NUnit XML](evidence/t03-launch-policy/special-03.xml)、[MCP](evidence/t03-launch-policy/special-03-mcp.json) |
| 完整 Player PlayMode | `f4b4df0de1dd4ecfbc6647841c71eb1c` | 679/679 通过，0 跳过，1906.562 秒 | [NUnit XML](evidence/t03-launch-policy/playmode-full-01.xml)、[MCP](evidence/t03-launch-policy/playmode-full-01-mcp.json) |
| 完整 Player Editor | `2720ee72cb01455a9de8d47836d6b001` | 76/76 通过，0 跳过，427.963 秒 | [NUnit XML](evidence/t03-launch-policy/editor-full-01.xml)、[MCP](evidence/t03-launch-policy/editor-full-01-mcp.json) |

首轮没有取得完整 XML，仅保留 MCP 返回的失败列表，不将缺失的运行输出补造为证据。之后增加显式启用的 Editor 测试证据回调，在域重载后继续注册，并将 Unity Test Runner 的完整结果保存到项目内；未设置 SessionState 输出路径时不写文件。

完整 PlayMode 作业执行到 303 项后，状态查询遇到会话 `1026ff0e-3949-46d8-8fd0-32eeb90f2c48` 断开。重新读取实例列表并指定同一框架实例后，原 job 仍在运行（306/679，无失败）。483 项后又遇到一次断开，以同样方式恢复到 485/679。原 job 最终正常结束；没有重复启动测试，也没有切换到 Windward。

Editor 作业也出现断开及连续查询超时；保持同一 job 等待并重连后，状态从 49/76 恢复到 74/76，最终 76/76 通过。没有因通信异常重启 Unity 或重新运行作业。只读窗口诊断未找到框架窗口，没有对其他项目进行 UI 操作。

最终 MCP 检查：`Application.dataPath` 为框架 Assets；`isCompiling=false`，`isPlaying=false`，活动场景 `Assets/Game/Scenes/FrameworkScene.unity`、`isDirty=false`，测试证据 SessionState 键已清空。Console 返回一条 `S03-runtime-combat-fault`，对应 `PlayerCombatRuntimeFaultTests.cs` 的 `LogAssert.Expect(LogType.Error, ...)` 故障注入，所属测试通过；未发现非预期错误或编译错误。原始 Console 返回保存在 [final-console-mcp.json](evidence/t03-launch-policy/final-console-mcp.json)。

发现及修复：

- 运行代码：`PlayerLifeSession.BeginReaction` 在受击提交的同一步清理 ExternalMotion，解决弹射凭证和输入锁延后一物理步骤才释放的问题；清理发生在 Reaction.Begin 前。
- 测试采样：反向输入断言移到 Tick 后、FixedTick 前，避免在凭证到期后清理输入的阶段误读 0；仍逐步比较水平速度和普通空中基线。
- 测试输入：跨真实渲染帧的合成键盘每帧重置按键，避免原生 Input System 更新清掉合成状态；未削弱负向速度断言。

修复后首轮真实渲染采样（每组约 0.4 秒；真实渲染帧累加器驱动玩家 FixedTick 和局部 PhysicsScene2D.Simulate，未修改全局 fixedDeltaTime；目标值不是实际 FPS，也不是性能基准）：

| targetFrameRate | 局部物理步长 | observedMeanFPS | 渲染帧 / 物理步 |
| --- | --- | --- | --- |
| 30 | 0.02 | 29.9445 | 12 / 20 |
| 30 | 1/60 | 30.0778 | 13 / 24 |
| 120 | 0.02 | 127.2962 | 52 / 20 |
| 120 | 1/60 | 119.6999 | 48 / 24 |
| 240 | 0.02 | 233.8765 | 94 / 20 |
| 240 | 1/60 | 229.1928 | 92 / 24 |

专项 XML 包含每个固定步的输入、速度及普通空中对照轨迹。无风反向输入从 ±10 在 0.18 秒到达 ∓0.7999986；Y15/Y20 均保持请求值。有风比较使用扣除环境贡献的自身水平速度，不把世界速度当作自身速度。

## 覆盖边界

新增 `PlayerLaunchPolicyTests.cs`：

- P02 左右初速、反向/松键，与同配置普通空中水平曲线逐步比较；Y15/Y20，无风/单风/相反风/多来源。
- 支撑/离台与风只合成一次；ContinueVelocity/StopVertical。
- P03 左右、上/侧发射，先真实按键消耗资格，回调可见性，空中再次真实墙跳。
- Inactive/Busy、禁用和销毁 owner、同一步替换顺序、旧 owner 隔离、暂停、快照、死亡/受击、顶棚、退出。
- 载具水平驱动和真实固定步到期独立用例。
- 目标 30/120/240 与 .02/1⁄60 固定步的真实渲染采样，输出 observedMeanFPS。

扩展专项另覆盖左右墙、上/侧发射各三轮不落地“消耗→弹射→再消耗”，机关修改仅影响下一请求，销毁 owner 的接受拒绝，以及载具 Y15/Y20 × Continue/Stop 下的暂停、Busy、替换、旧 owner 隔离和新 owner 销毁。载具测试的水平值由宿主提供，用于验证契约，不代表已经运行产品风公式。

普通长短跳、墙滑/墙跳、项目动作和飞行策略的模板交叉回归不能代替 Windward 当前认可方案的地面旗跳及空中准备→自动回弹→自有风场真实路线。本次未升级或运行消费项目，不作产品验收结论。

完整 PlayMode XML 已包含并通过：

- `OrdinaryRhythm_*`：普通跳完整轨迹、水平加速/刹车/转向、外部旗跳与回弹轨迹、普通跳切墙跳、受击/死亡取消策略。
- `WallFlight_*`：左右分支、资格、早期反向转换开关、长短曲线与顶棚、`air-auto` / `ground-auto` / `ground-flag-jump` 动作退出；`WallFlightBoundary_*` 覆盖生命周期与边界。
- `Support_*`：平移不重复合成、离台继承一次、风只加一次、不同物理步、单向平台下穿、载具顶棚净空、禁用与瞬移。
- 既有生命、受击、攻击、冲刺、输入、快照和恢复测试；本次 27 项专项也包含在 679 项中，不能重复累加为不同用例数量。

## 保存与安全消费

验证结束时：项目 Assets 修改完成；项目中心保存、Bump、发布及消费升级均未执行。随后用户明确授权发布，正式 SaveTemplate 保存 1442 文件并 minor Bump 至 0.9.0，封存 hash 为 `f640fe07081a1958665c2ed9194196ea`，框架分发版本为 0.20.4。原运行代码与测试源码未再修改，项目中心校验 0 错误、0 警告、0 差异。消费升级仍未执行。

完成回归后，通过项目中心预览全部受管差异，确认本轮与原有草稿边界，再执行正式 SaveTemplate；按授权 Bump 后记录 contentHash/versionedContentHash。不得直接补写 Templates~、ParentSnapshot~ 或版本/hash。新框架 tag 的发布必须另按授权进行。

消费方收到正式版本后保留本地方案和所有修改，走差异预览与逐项合并，不全量覆盖。先验证 P02/P03，再回归认可方案和原路线。S2 听感仍待确认，手柄暂缓；S3 没有提前接线或签收。

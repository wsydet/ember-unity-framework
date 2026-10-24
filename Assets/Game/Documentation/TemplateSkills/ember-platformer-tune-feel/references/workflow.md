# 操作与验证

## 前置与来源

项目相对源码定位：`Assets/Game/Module/Player/Editor/PlayerSchemeWorkflow.cs`、`PlayerSchemeLibrary.cs`、`PlayerEditorAccess.cs`、`PlayerDevelopmentEnvironment.cs`；运行装配见 `Assets/Game/Module/Player/PlayerScheme.cs`、`PlayerConfig.cs`、`PlayerSchemeCapture.cs`。通过实际项目根解析，不依赖本技能的发现副本所在目录。

在 Unity 内先检查执行阻断原因，再确认 `EmberProjectSetup.IsTemplateActive("platformer-2d")`、PlayerModule 的 `EmberModuleAttribute.Enabled`，以及 Player、GameInput、存储等本次流程需要的实际模块和资产。Player 专属工作流也会执行统一入口保护；不要绕过它或强开关闭模块。缺少类型/方法时停止相关写入，说明实际缺失能力，不以版本号推断已安装。至少需要：

- `PlayerSchemeWorkflow.Configuration/Copy/Validate/Compare/Save/Use`。
- `PlayerSchemeOwnership.IsReadOnly/RequireWritable`。
- `PlayerScheme` 的 Feel、Powers、Animation、ActionEffects、RuleMode、Rules。
- `PlayerDevelopmentEnvironment.SetSchemeParameters/Play/Restart` 及独立开发库存储。

模板执行前置检查核对正式编辑/部署及安装记录。无 MCP 时只读核对项目、正式身份和安装记录；schema v3 的业务基线与技能源基线可以不同，不伪造记录来解除阻断。来源模板版本不是派生模板版本，旧业务缺少能力不能仅更新技能后假称可执行。

## 基线与完整副本

入口 `Ember/Player/玩家开发工具`，保留“手感白盒／玩家配置”两页。读取窗口当前所选方案；`PlayerDevelopmentEnvironment.SelectedScheme` 是下次白盒选择，`Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH).Scheme` 是项目默认。二者可能都不同于当前编辑项。窗口的运行来源/捕获标识用于解释冻结状态，不能拿实时 SO 数值冒充运行值。

修改前记录方案 GUID、路径、配置闭包每项的类型/路径/本地文件 ID、序列化值与 dirty 状态，记录场景 dirty、白盒设置和布局。用 `Configuration(scheme)` 得到可变配置闭包，用 `AssetDatabase.GetAssetPath` 和稳定资产身份定位；图/Definition 与 Controller/Clip 不属于要复制的手感数值。自定义也可能共享可变配置：列出实际引用该资产的方案；未授权影响其他方案时使用完整 Copy 隔离。

`Copy(source, newAssetPath, blank:false)` 创建并保存完整方案与可变配置子资产，保留共享基础图及美术引用；包括方案规则，不能仅复制 Feel 后丢失其余装配。`blank:true` 另有关闭机制与重置附加条件的语义，普通调参不用。读取当前 Rules 即使本次不编辑，避免将规则造成的动作拒绝误判成运动参数失效。旧规则模式不自动迁移。

先在内存独立副本或新自定义资产构造候选并完整 Validate，才能修改用户已有方案。对持久化对象逐个 RequireWritable，以一个 Undo 组应用已校验字段；修改前再次核对基线，期间有变化时展示差异，不覆盖用户新草稿。不清空全局 Undo。失败撤回本次组，不回写旧全量 JSON 覆盖其他工作。

`Compare(current, reference)` 输出 current→reference，用时说明方向；记录候选前后差异。`Save(scheme)` 校验并仅保存配置闭包，避免全局 `AssetDatabase.SaveAssets()` 顺带写入无关草稿。保存前再次检查全部目标路径和闭包所有权。实际 API/字段与参考不符时以源码为准，未知字段报出，不静默跳过。

## 应用与测量

`PlayerSchemeWorkflow.Use(scheme)` 选择下次白盒参数，不改变项目默认。也可 `SetSchemeParameters(scheme, position, velocity)` 明确新游戏起点。白盒设置 `UseWhitebox=true` 后正常 Play，仍从 Main 点击新游戏；没有直接启动第二套玩家驱动。Play 前选择冻结，同一 Gameplay 恢复使用本会话冻结 A，源资产修改不改变旧会话。更新体验需停止再 Play 或显式 Restart；中断用户正在进行的试跑前先说明。开发库每次 Play 独立，不接触真实存档。

在 FSM 蓝图选择运行实例，通过“运动曲线与实测（所选运行实例）”查看曲线。`controller.Context.MotionTrace` 是编辑器记录，读取不驱动业务；保留最多 512 个物理步。样本包含 Time、Position、Velocity、Gravity、Buffer、Coyote、State。位置是观察值，速度/重力是提交值，比较时保持相同采样相位，不当成完全同一时刻的物理求解结果。

比较基线和候选：相同出生点/初速、固定步长、输入幅度与持续时间、地面/空中条件，记录实际生效开关、效果、规则。启动到稳态速度、松键到停止的时间/距离、反向响应；跳跃分长按/短按，测起跳到峰值与落回同高度/支撑的全过程。需要精确输入时沿实际绑定设备的 InputAction 注入测试输入，不绕过规则直接改速度或调用状态 Enter。结束后释放测试输入并恢复本机开发设置。

`JumpRise/JumpPeakTime/JumpDuration` 只统计最近普通 Jump 状态片段，切到 Fall 即结束，不能当完整滞空。要测滞空，用完整样本的实际离地与着地边界；检查碰顶、墙面、二段跳、冲刺、平台高度差及样本截断。记录单位和观测误差，必要时分离受干扰的试次。暂停期间不应推进游戏秒。

体验覆盖本次改动及紧密依赖：水平改动含地面/空中继承和转向；重力改动含长短跳与落地；开启动作检查能力开放/资源/几何/附加规则；效果改动检查对应动作提交与清理。编译、自动测试、实测和用户观感分开报告。

`SetDefault` 是另一个有 Undo 的项目配置动作，只有用户明确要设为默认时执行；它不等于保存，也不替换运行实例。测试后恢复用户原窗口选择、白盒设置与场景，核对未保存草稿和布局未改变；仅清理自己创建且确认不被使用的测试资产。

## 网页预览评估

当前采用 Unity 工作流。现有窗口已支持按机制定位、参数过滤、完整方案复制/比较和正常白盒试跑，曲线直接取真实固定步记录。基础调参只需选方案、改相关组、应用并试跑；浏览器采样页另需导出与重新同步，不能免去 Unity 最终验证。即时网页模拟还要维护水平继承、跳跃策略与重力组合的第二份计算，更不能准确替代 Physics2D、墙面/挂边、效果和规则争用。

因此本版没有浏览器候选导入协议或“精确手感”模拟。需要远程看结果可输出实际样本及图表，并注明它是历史采样。将来若用户明确要求网页交互，应重新评估便利性、参数基线冲突和固定步误差；不能把本技能正文的估算公式扩成未经验证的运行实现。

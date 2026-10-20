# Ember 0.20.3 发布说明

交付 platformer-2d **0.8.0** 的可选墙跳与单次飞行下降策略。能力归属模板，框架包升 patch 用于分发；框架内核、其他模板、依赖及公共 Skill bundle 不变。项目中心正式保存 1436 文件并 minor Bump，contentHash/versionedContentHash 均为 `003c4429f265bbcd8d377bf375c2c15c`。父基线 base 0.8.3 / `1270ce6dfcae3c01d050c09ecfd60337`，兼容声明保留 0.20.0。

## 能力与默认兼容

- `PlayerJumpFeel.ConfigureWallJump(PlayerWallJumpSpec)`：首次向墙直上资格、落地恢复、早期反向转换、独立保持/释放与水平超速减速；正式方案复制、校验、比较和运行冻结接通。默认关闭，原墙跳通路保留。
- `PlayerActionLease.LaunchVertical(velocity, gravityScale, owner, sourceId, PlayerFlightSpec)` 与 `PlayerExternalMotionRequest(..., flight: spec)`（仅 InitialVelocity）：单次飞行下降加速度、终速及可选低速持键规则，保留上升。动作释放或结束后继续有效，`PlayerFlightLease` 按 owner、实例和会话代次隔离；来源 ID 由业务明确区分。
- 新飞行、真实落地、受击死亡、owner 禁用销毁和会话退出清理；旧凭证不清理新策略。暂停不续接旧持键优惠。活跃策略和可选墙跳空中资格拒绝即时快照，使用检查点恢复。
- 内置方案资产、普通跳、地空惯性及风/动画/音频/镜头接线未修改。Mountain 数值没有硬编码为模板默认。

公开 API 和边界见随模板 `Assets/Game/Documentation/platformer-2d/wall-flight-policy.md`；完整实现、旧代码语义、失败证据和轨迹见 [交付记录](wall-flight-delivery-20261020.md)。

## 验证与限制

Unity 6000.5.4f1，相关回归 87/87，生命周期增量 11/11，编辑工作流 2/2，更新来源测试 14/14，最终运动边界 17/17，左右墙曲线 4/4，最终时间标签曲线 7/7，均通过；集合重叠，不相加。最终运行逻辑由 17 项边界和 7 项曲线覆盖，本次封存不再改变运行代码。未重跑 Player 全集，不沿用上一版本全集结果冒充本版结果。合成输入全部标记 SYNTHETIC。

来源代理相对上升高度约 3.190927、到顶约 0.46s；独立地面旗跳保留共享下降，空中/地面自动回弹策略在动作退出后继续生效。代理不是 Windward 业务接线或手感签收。固定步 .02s，最新目标30/60/240的墙跳实测平均FPS为30.022/61.834/187.978，外部飞行为30.019/60.057/238.914；不宣称稳定120/240fps。

项目中心只读校验与模板谱系见 [发布校验](evidence/wall-flight-20261020/release-validation.json)。共享字体、二进制及干净检出检查和 bundle 原始字节核对纳入本次发布检查。常规 bundle Check 被既有未跟踪模板技能目录拦截；保留这些目录，不重建未改动的 bundle，另核对 manifest SHA-256 与 sourceCommit 对应的公共源文本。

未验证：Windward 正式消费、完整挥旗业务来源接线、真实转场与原路线、人工键盘观感/动画听感/镜头、手柄、发行构建/IL2CPP/其他平台。**本版不签收 Windward S2，不启动 S3。**

## 正式消费

通过 **Ember/UPM Manager** 升级至 v0.20.3，等待解析编译，再处理 platformer-2d 0.7.0 → 0.8.0 的跨 minor 模板迁移。不能走同 minor 的 patch 增量入口；先保护精确旧基线、部署记录、本地差异、GUID、方案、输入和存档，按正式流程分批迁移与恢复定制。不得直接整体覆盖 Windward、修改 PackageCache 或手改消费 manifest/lock。

保留 WindwardWallRestoreCandidate 的完整17项闭包与默认方案，独立候选接入新策略。空中挥旗自动反弹、地面挥旗自动反弹、独立窗口地面旗跳必须使用明确不同来源；已认可地面旗跳不启用自动反弹下降策略。继续保护普通跳、移动惯性、特殊跳上升及动作时序/风/音画基线，完成来源、生命周期和完整曲线 A/B 后再由 Windward 对话判断 S2。

包地址：`https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.20.3`。依赖声明：[release-0.20.3.json](../../Packages/com.ember/Dependencies~/release-0.20.3.json)。

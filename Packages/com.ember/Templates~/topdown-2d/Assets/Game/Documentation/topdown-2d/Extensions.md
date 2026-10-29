# 扩展动作与产品接入

`Game.Player.Examples` 是独立程序集的 Focus 示例。它通过强类型 `ExampleLocomotionDefinition` 增加一个入口段，继承基础 PlayerState 的 Locomotion 行为并复用 `PlayerStateDefinition.DeclarationsFor`；未复制或修改 PlayerModule 的核心循环。对应 Focus Definition/State、Skill、输入与动画均在独立方案中装配。

Focus 使用 F/手柄北键，资源预检只查询，Enter 才提交 Skill；锁向、停止移动、持有 Owned 外观，0.5秒后回到 Locomotion，受击/死亡优先打断并释放效果和运动所有权。当前只在竞技场方案启用，默认训练场不加载此动作。

新增动作的最小步骤：

1. 在项目独立程序集定义 SO 与运行状态，使用 PlayerContext，声明强类型 E01 转换和 RequestSite。
2. 明确开始条件、输入新按下/外部请求、资源提交点、持续/完成条件、打断、运动/朝向拥有权及清理。
3. 用 `PlayerContext.CreateInputButton` 接入已声明输入；按钮由状态和 Context 回收。产品也可用 `PlayerModule.RequestAction(owner, intent)` 发起当前武器攻击/冲刺/交互，保留返回句柄，取消只影响自己的待处理请求。下一物理步被考虑不代表预检必然通过；禁止/暂停清除旧请求。不能直接 Enable/Disable ActionMap 或自行 Tick。
4. 用 `AcquireActionControl(owner)` 在状态 FixedUpdate 中提交平面速度意图，Exit/Dispose 释放租约；刚体仍由 PlayerModule 统一提交。
5. 用 `CanUseSkill` 预检、`TryCommitSkill` 提交；持续效果使用 EffectHandle 的独立所有权。
6. 复制方案后加入节点/目标绑定、Skill 和必要动作方向映射，再从工具校验、正式试玩及蓝图观察。

新动作默认不允许即时保存，因为默认只接受 idle/move。需要恢复的产品动作必须明确扩展快照/恢复协议；不能把任意 FSM 叶子及时间直接序列化。持续 Buff、背包、光区与剧情进度不进入互斥动作图。

`PlayerModule.CaptureState` 返回普通只读 PlayerStatusView。`SetInteractionResolverForNextSession` 装配 IPlayerInteractionResolver，接收平面位置/朝向并解析产品目标；返回 true 表示已处理，false 才进入模板最近检查点样例。产品交互数据仍需加入自己的完整世界快照，不会自动持久化。

`SetCombatRulesForNextSession` 装配 IPlayerCombatRules。规则在目标 HP、硬直和新保护写入前收到稳定目标 ID、队伍、位置及冻结命中，返回修正后的 EntityHit 或 null 拒绝。不要在这里改 HP、重入 FSM、推进时钟，或在事后补扣/回填生命；异常发生在本击写入前并触发会话故障处理，不承诺整批回滚。规则与交互解析器在会话准备时捕获引用，活动会话不能替换；产品负责它们的生命周期及恢复语义。

世界扩展以 PlayerWorldSession 的样例组合位置为参照，明确持久对象 ID、完整冻结片段、准备验证与提交。模板内仅示范检查点布尔值和战斗对象，不建立产品通用对象数据库。

相机通过独立 PlayerCamera 参与者接线；玩家只接收普通 Camera 的瞄准投影。暂停使用所有权租约。蓝图通过正式模板与模块条件登记，不能依据资源/脚本残留推断模板身份。

新增武器攻击使用[武器独立图与WeaponContext](Weapons.md)，不向玩家互斥图增加Attack节点。Focus是角色自身动作的扩展示例，仍属于玩家图。

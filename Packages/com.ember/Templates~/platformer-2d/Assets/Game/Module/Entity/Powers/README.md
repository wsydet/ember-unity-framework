# Entity Ability / Skill 公共基础

归属 platformer-2d 的 Game.Entity.Runtime，不依赖 Player、输入、FSM 或存档程序集。可选装配，不要求每个 Entity2D 持有能力。

- EntityAbilityDefinition / EntitySkillDefinition 是可派生的配置 SO 基类；分别定义稳定 ID、默认开放、次数及 Skill CD。
- EntityPowerConfig 分别装配两类配置。Enabled 是项目装配条件，开放状态由每个宿主独立持有。
- EntityPowerHost.Create 显式创建一组独立资源与开放数据；不表示新游戏，也不自动注册 Update。宿主明确调用 Tick(gameSeconds) 并负责 Dispose。
- Abilities / Skills 提供独立 ID 空间与查询入口；EntityPowerRuntime 共享小型资源规则，不执行动作。
- Query 只读。动作保存一个 EntityUseAttempt，在确定提交点 TryCommit；同一尝试最多成功一次，其他资源/宿主的尝试会被拒绝。
- SetOpen 只改变开放，不补充次数、不清 CD，不绕过环境或临时禁止。Boss 阶段控制器可显式调用，但公共层不认识阶段类型。
- RefillUses 是宿主显式补到上限的接口；公共层不知道地面支撑。它不清 CD 或改变开放状态。
- Availability 是独立普通数据，资源 Dispose 后仍可读取；保存和重置语义由宿主业务决定。旧资源不可更新新实例。

配置创建时捕获并校验，运行时不读取可变 SO。ID 区分类别及大小写，拒绝空白、重复、缺失引用；CD 使用秒和 1 微秒浮点边界容差。实际技能提交点、共享 CD、打断/返还不在此指定。

Player/Powers 为派生配置和玩家业务适配。旧玩家配置脚本及三个测试 SO 的 GUID 保留，公共默认字段使用 FormerlySerializedAs 迁移。迁移后的 Unity 编译、资产重载及 107/107 PlayMode 回归已通过，此为历史提取验证，不代表本轮重跑；玩家使用与保存边界见[玩家架构](../../../Documentation/platformer-2d/PlayerArchitecture.md)。

Tests/Game.Entity.Tests 仅引用 Game.Entity.Runtime，四项测试验证阶段开放、宿主资源与计时隔离、销毁重建及程序集依赖。四项均已通过；组合回归包括 Player 79 项和 SceneFlow 24 项。


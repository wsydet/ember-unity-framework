# 消费项目扩展

扩展留在消费项目业务模块中，不修改包缓存，不继承兄弟玩法模板。接口定义位于 `Assets/Game/Module/LaneDefense/Runtime/LaneDefenseContracts.cs`。

| 接口 | 实际签名 | 默认实现 |
| --- | --- | --- |
| IPlacementRule | `bool Allows(in PlacementQuery query)` | 不附加产品限制 |
| IAttackPolicy | `bool TryGetVelocity(in AttackQuery query, out Vector2 velocity)` | 前方、射程内、同 lane 时向右发射 |
| IProjectilePolicy | `ProjectileStepResult Step(ProjectileContext context, in ProjectileSnapshot projectile, float delta)` | 上一位置至新位置连续扫掠，命中一次后终止 |
| IHitRule | `HitDecision Evaluate(in HitRequest request)` | 基础伤害 |
| ILaneDefensePolicyProvider | `LaneDefensePolicies CreatePolicies(LaneLevelSnapshot level)` | 场景无 provider 时使用全部默认策略 |

在消费项目实现一个 MonoBehaviour 与 ILaneDefensePolicyProvider，把组件拖到 LaneDefenseSceneParticipant 的 Policy Provider。每次候选或重开均调用 CreatePolicies，返回本会话独立策略。需要释放的产品资源应由消费参与者租约持有，不由静态策略缓存场景对象。

```csharp
public sealed class ProjectPolicies : UnityEngine.MonoBehaviour,
    Game.LaneDefense.ILaneDefensePolicyProvider
{
    public Game.LaneDefense.LaneDefensePolicies CreatePolicies(
        Game.LaneDefense.LaneLevelSnapshot level)
    {
        return new Game.LaneDefense.LaneDefensePolicies(
            placement: new ProjectPlacementRule(),
            attack: new ProjectAttackPolicy(),
            projectile: new ProjectProjectilePolicy(),
            hit: new ProjectHitRule());
    }
}
```

上面 Project* 类型由消费项目实现，示意不携带产品装置实现。策略读取不可变查询，不在查询回调中重入 Place/Advance/Dispose，不以 UI、Sprite 或 Tag 修改权威状态。

## 跨行投射物的所有权

ProjectileSnapshot.Position/Velocity 是逻辑二维 Vector2，没有 lane 字段约束。产品攻击策略可返回斜向/竖向速度；产品 Step 可通过 `context.Move(position, velocity)` 更新二维运动，用 `TrySweep(from,to,radius,out target)` 查询所有敌人的二维几何，或向合法 EntityKey 调用 `SubmitHit(target)`。默认攻击策略的同 lane 条件不会进入这些底层接口。

**唯一更新者是 BattleSession 调度器；唯一当前处理者是选定的 IProjectilePolicy；唯一命中提交者是当前同步 Step 的 ProjectileContext；唯一清理者是 BattleSession。** 替换策略后默认直线处理器不再运行。产品不得给同一弹丸再挂一个独立 Update/Trigger 扣血器。

Step 返回 Continue 或 Consume。TTL、全局边界、预算和终局清理由会话强制执行。Context 只在本弹丸当前同步调用内有效，返回后立即失效，不得保存供异步回调或后续实体使用。SubmitHit 对同一弹丸/目标身份去重，命中结果区分 Miss、Blocked、Damaged、Killed；IHitRule 只返回决定，实际扣血与移除仍由会话一次提交。

产品如需派生弹丸，必须调用当前 `context.Spawn(position,velocity,damage,lifetime)`，沿相同会话 ID、预算和释放入口登记；新弹丸从后续模拟步开始处理，不能递归重复消费本步。模板本身不实现任何分裂、反射或传送策略。

## 经济和结果

`Credit(long session,long eventId,int amount,string source)` 校验当前代次、正数、来源、资源上限与事件去重；事件编号由消费项目在本会话统一分配。不要在 OnDestroy 返款。终局观察 `BattleSession.Completed`，使用 BattleResult.Id 作为本次运行结果的去重标识；跨进程奖励幂等和稳定存档 ID 由消费项目定义，不能把进程内 Generation 作为永久奖励账本。

表现可替换场景 View/Prefab 与 EUI 用户逻辑；权威规则仍以逻辑坐标和会话身份为准。首版未提供池复用或中途快照契约；如引入对象池，表现租约和逻辑 EntityKey 必须分离。

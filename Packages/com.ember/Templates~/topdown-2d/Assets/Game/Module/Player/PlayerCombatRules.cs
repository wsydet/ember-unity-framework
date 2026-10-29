using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    public readonly struct PlayerCombatTarget
    {
        public string Id {get;}
        public int Team {get;}
        public Vector2 Position {get;}
        public PlayerCombatTarget(string id,int team,Vector2 position){Id=id;Team=team;Position=position;}
    }
    /// <summary>结算前的产品规则。返回 null 拒绝；只能计算命中值，不在这里改 HP、重入 FSM 或推进时钟。</summary>
    public interface IPlayerCombatRules
    {
        EntityHit? Filter(PlayerCombatTarget target,in EntityHit incoming);
    }
    internal sealed class PlayerBoundHitRule:IEntityHitRule
    {
        private readonly IPlayerCombatRules _rule;
        private readonly string _id;
        private readonly int _team;
        private readonly Rigidbody2D _body;
        internal PlayerBoundHitRule(IPlayerCombatRules rule,string id,int team,Rigidbody2D body){_rule=rule;_id=id;_team=team;_body=body;}
        public EntityHit? Filter(in EntityHit incoming)=>_rule.Filter(new PlayerCombatTarget(_id,_team,_body.position),incoming);
    }
}

using UnityEngine;

namespace Game.Player
{
    public readonly struct PlayerStatusView
    {
        public string ActionId {get;}
        public string SchemeId {get;}
        public Vector2 Position {get;}
        public Vector2 Facing {get;}
        public float Health {get;}
        public float MaximumHealth {get;}
        public bool Paused {get;}
        public bool ControlsAvailable {get;}
        public string WeaponId {get;}
        public string WeaponState {get;}
        internal PlayerStatusView(PlayerController player,bool paused)
        {var c=player.Context;ActionId=player.CurrentState;SchemeId=c.Scheme.Id;Position=player.Body.position;Facing=c.Facing;Health=c.Health?.Current??0;MaximumHealth=c.Health?.Maximum??0;Paused=paused;ControlsAvailable=c.ControlsAvailable;WeaponId=c.Weapons.Current?.Spec.Id;WeaponState=c.Weapons.Current?.CurrentState;}
    }
    public readonly struct PlayerInteractionIntent
    {
        public Vector2 Position {get;}
        public Vector2 Facing {get;}
        public PlayerInteractionIntent(Vector2 position,Vector2 facing){Position=position;Facing=facing;}
    }
    /// <summary>项目解析目标并执行自己的交互。true 表示已处理；false 才使用模板最近检查点样例。</summary>
    public interface IPlayerInteractionResolver { bool TryInteract(in PlayerInteractionIntent intent); }
}

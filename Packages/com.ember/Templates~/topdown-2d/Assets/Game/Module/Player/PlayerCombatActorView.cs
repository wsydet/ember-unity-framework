using UnityEngine;

namespace Game.Player
{
    public readonly struct PlayerCombatActorView
    {
        public string Id {get;}
        public float Health {get;}
        public float Maximum {get;}
        public string Phase {get;}
        public Vector2 Position {get;}
        internal PlayerCombatActorView(string id,float health,float maximum,string phase,Vector2 position)
        {Id=id;Health=health;Maximum=maximum;Phase=phase;Position=position;}
    }
}

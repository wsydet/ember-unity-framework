using System;

namespace Game.Entity
{
    /// <summary>可选运行生命值。不查玩家状态，不自行推进，不承担死亡恢复。</summary>
    public sealed class EntityHealth
    {
        public float Maximum { get; }
        public float Current { get; private set; }
        public event Action<float, float> Changed;
        public EntityHealth(float maximum, float initial)
        {
            if (!float.IsFinite(maximum) || maximum <= 0 || !float.IsFinite(initial) || initial < 0 || initial > maximum)
                throw new ArgumentOutOfRangeException(nameof(initial));
            Maximum = maximum; Current = initial;
        }
        public void Restore(float value)
        {
            if (!float.IsFinite(value) || value < 0 || value > Maximum) throw new ArgumentOutOfRangeException(nameof(value));
            float previous = Current; Current = value;
            if (previous != Current && Changed != null)
                foreach (Action<float, float> handler in Changed.GetInvocationList())
                    try { handler(previous, Current); }
                    catch (Exception error) { Ember.Basic.EmberDebug.LogError("Game.Entity", "生命观察者失败：" + error); }
        }
        internal float Damage(float value, bool nonLethal)
        {
            float previous = Current;
            Restore(Math.Min(Current, Math.Max(nonLethal ? Math.Min(1, Maximum) : 0, Current - value)));
            return previous - Current;
        }
    }
}

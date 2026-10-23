using UnityEngine;

namespace Game.Entity
{
    /// <summary>最近一次传感采样的真实、启用且法线可承重的接触；速度单位 u/s，法线朝向角色。</summary>
    public readonly struct EntityGroundContact2D
    {
        public Collider2D Collider { get; }
        public Vector2 Point { get; }
        public Vector2 Normal { get; }
        public Vector2 SurfaceVelocity { get; }
        public Vector2 ActorVelocity { get; }
        public bool HasContact => Collider != null && Normal.y > 0 &&
            float.IsFinite(SeparationSpeed);
        public float SeparationSpeed => Vector2.Dot(ActorVelocity - SurfaceVelocity, Normal);

        public EntityGroundContact2D(Collider2D collider, Vector2 point, Vector2 normal,
            Vector2 actorVelocity, Vector2 surfaceVelocity)
        {
            Collider = collider; Point = point; Normal = normal.normalized;
            ActorVelocity = actorVelocity; SurfaceVelocity = surfaceVelocity;
        }
        public EntityGroundContact2D WithSurfaceVelocity(Vector2 velocity)
            => new(Collider, Point, Normal, ActorVelocity, velocity);
    }
}

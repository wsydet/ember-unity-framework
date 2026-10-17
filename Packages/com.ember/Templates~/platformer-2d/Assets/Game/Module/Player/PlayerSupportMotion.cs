using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    /// <summary>玩家固定步中的可选平移承载。没有支撑适配器时保持原地面行为。</summary>
    public sealed class PlayerSupportMotion
    {
        #region 内部参数
        private readonly PlayerContext _context;
        private EntityTranslationSupport2D _source;
        private Vector2 _position, _lastVelocity, _inherited;
        private float _rotation;
        private uint _revision;
        public EntityTranslationSupport2D Source => _source;
        public Vector2 Velocity { get; private set; }
        public Vector2 InheritedVelocity => _inherited;
        public bool HasTransientMotion => _source != null || _inherited != Vector2.zero;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerSupportMotion(PlayerContext context) => _context = context;
        internal void Observe(float dt)
        {
            bool validPrevious = _source != null && _source.IsAvailable && _source.Revision == _revision &&
                Mathf.Abs(Mathf.DeltaAngle(_rotation, _source.Body.rotation)) < .01f &&
                Vector2.Distance(_position, _source.Body.position) <= Mathf.Max(_source.TeleportDistance, _lastVelocity.magnitude * dt * 3);
            var ground = _context.Sensors.Ground;
            var next = ground != null ? ground.GetComponentInParent<EntityTranslationSupport2D>() : null;
            if (next != null && (!next.IsAvailable || !next.Owns(ground) ||
                _context.Motor.Velocity.y > next.Velocity.y + .1f || _context.DropThrough.IsActive)) next = null;
            if (_source != null && !validPrevious)
            {
                // 禁用、瞬移或旋转不产生离开惯性。本步也不重取同一失效支撑。
                if (next == _source) next = null;
                _inherited = Vector2.zero;
            }
            else if (_source != null && next == null)
                _inherited = _source.Departure switch
                {
                    SupportDepartureVelocity.Horizontal => new Vector2(_lastVelocity.x, 0),
                    SupportDepartureVelocity.Full => _lastVelocity,
                    _ => Vector2.zero
                };
            if (next != null || _context.Sensors.IsGrounded && _context.Motor.Velocity.y <= 0)
                _inherited = Vector2.zero;
            _source = next;
            if (_source != null)
            {
                _source.ConstrainForRider(_context.Sensors.Shape, dt);
                _revision = _source.Revision; _position = _source.Body.position; _rotation = _source.Body.rotation;
                _lastVelocity = _source.Velocity;
            }
            Velocity = _source != null ? _lastVelocity : _inherited;
        }
        internal void Clear()
        { _source = null; _lastVelocity = _inherited = Velocity = Vector2.zero; }
        #endregion
    }
}

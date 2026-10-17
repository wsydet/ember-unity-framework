using UnityEngine;

namespace Game.Player
{
    /// <summary>无产品语义的恒定环境速度示例；只登记来源，不驱动物理。</summary>
    public sealed class PlayerConstantVelocitySource : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private PlayerController _player;
        [SerializeField] private Vector2 _velocity = new(2, 0);
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private PlayerExternalMotionLease _lease;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void Update()
        {
            if (_lease?.IsActive == true || _player == null || !_player.IsRunning || _player.Context.Life?.IsDead == true) return;
            _lease = _player.Context.ExternalMotion.Register(this,
                new PlayerExternalMotionRequest(name, PlayerExternalMotionMode.AddVelocity, _velocity));
        }
        private void OnDisable() { _lease?.Dispose(); _lease = null; }
        private void OnDestroy() { _lease?.Dispose(); _lease = null; }
        #endregion
    }
}

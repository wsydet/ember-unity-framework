using UnityEngine;

namespace Game.Player
{
    public sealed class PlayerCheckpointZone2D : MonoBehaviour
    {
        #region 编辑器面板参数

        [SerializeField] private string _checkpointId;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void OnTriggerStay2D(Collider2D other)
        {
            var recovery = other.GetComponentInParent<PlayerCheckpointRecovery>();
            if (recovery == null || recovery.Checkpoint?.CheckpointId == _checkpointId || recovery.IsBlocked) return;
            var player = recovery.GetComponent<PlayerController>();
            if (player.IsRunning && player.Context.JumpWindow.HasGroundSupport && !player.Context.ExternalMotion.HasTransientMotion)
                recovery.TryCapture(_checkpointId);
        }

        #endregion
    }
}

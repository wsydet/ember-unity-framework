using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    /// <summary>可选危险区示例。只请求检查点世界恢复；死亡统计和表现由项目订阅/实现。</summary>
    public sealed class PlayerRecoveryHazard2D : MonoBehaviour
    {
        #region 内部参数

        private bool _pending;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_pending || !isActiveAndEnabled) return;
            var recovery = other.GetComponentInParent<PlayerCheckpointRecovery>();
            if (recovery == null || recovery.IsBlocked || !EmberModuleCollector.Instance.TryGetModule<PlayerModule>(out var module) ||
                module.Controller != recovery.GetComponent<PlayerController>()) return;
            Recover(recovery, module).Forget();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private async UniTask Recover(PlayerCheckpointRecovery recovery, PlayerModule module)
        {
            _pending = true;
            try { await recovery.RecoverAsync(module, GameLauncher.Instance.Fsm, WorldResetReason2D.Death, this.GetCancellationTokenOnDestroy()); }
            finally { _pending = false; }
        }

        #endregion
    }
}

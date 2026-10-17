using System;
using UnityEngine;

namespace Game.Entity
{
    [RequireComponent(typeof(EntityTranslationSupport2D))]
    public sealed class TranslationSupportReset2D : WorldResetParticipant2D
    {
        #region 内部方法

        [Serializable] private struct State { public int version; public Vector2 position; public Vector2 velocity; }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public override string CaptureState()
        {
            var support = GetComponent<EntityTranslationSupport2D>();
            return JsonUtility.ToJson(new State { version = 1, position = support.Body.position, velocity = support.Velocity });
        }
        public override void RestoreState(string state, WorldResetReason2D reason)
        {
            var value = JsonUtility.FromJson<State>(state);
            if (value.version != 1) throw new ArgumentException("平台复位记录版本无效。");
            var support = GetComponent<EntityTranslationSupport2D>();
            support.Teleport(value.position); support.SetVelocity(value.velocity);
        }

        #endregion
    }
}

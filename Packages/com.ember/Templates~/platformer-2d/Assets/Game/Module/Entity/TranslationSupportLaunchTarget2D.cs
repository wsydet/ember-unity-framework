using UnityEngine;

namespace Game.Entity
{
    /// <summary>平移载具弹射适配；宿主在 IsLaunching 时应停止普通路径发令。</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(EntityTranslationSupport2D))]
    public sealed class TranslationSupportLaunchTarget2D : MonoBehaviour, ILaunchTarget2D
    {
        #region 内部参数
        private EntityTranslationSupport2D _support;
        private Behaviour _owner;
        private float _remaining;
        private LaunchEnd2D _end;
        public bool IsLaunching { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void FixedUpdate()
        {
            if (!IsLaunching) return;
            if (_owner == null || !_owner.isActiveAndEnabled || !_support.IsAvailable || _support.Velocity.y <= 0)
            { Finish(); return; }
            _remaining -= Time.fixedDeltaTime;
            if (_remaining <= .000001f) Finish();
        }
        private void OnDisable() => Finish();
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Finish()
        {
            if (IsLaunching && _support != null && _support.IsAvailable && _end == LaunchEnd2D.StopVertical)
                _support.SetVelocity(new Vector2(_support.Velocity.x, 0));
            IsLaunching = false; _owner = null; _remaining = 0;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public LaunchResult2D TryLaunch(Behaviour owner, LaunchRequest2D request)
        {
            _support ??= GetComponent<EntityTranslationSupport2D>();
            if (!isActiveAndEnabled || !_support.IsAvailable || owner == null || !owner.isActiveAndEnabled ||
                string.IsNullOrWhiteSpace(request.SourceId)) return LaunchResult2D.Inactive;
            if (IsLaunching && request.Interrupt == LaunchInterrupt2D.RejectWhileBusy) return LaunchResult2D.Busy;
            Finish(); _support.SetVelocity(request.Velocity);
            IsLaunching = request.Mode == LaunchMode2D.ConstantRise;
            _remaining = request.Duration; _owner = owner; _end = request.End;
            return LaunchResult2D.Accepted;
        }
        #endregion
    }
}

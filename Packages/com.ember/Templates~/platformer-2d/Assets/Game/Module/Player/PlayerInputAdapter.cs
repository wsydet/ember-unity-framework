using System;
using Game.Input;
using UnityEngine;

namespace Game.Player
{
    public sealed class PlayerInputAdapter : IDisposable
    {
        #region 内部参数
        private readonly GameInputChannel _channel;
        private readonly string _move;
        private readonly GameInputButton _jump;
        private readonly GameInputButton _dash;
        private readonly float _bufferDuration;
        private float _bufferRemaining;
        public float BufferRemaining => _bufferRemaining;
        public float Horizontal { get; private set; }
        public float Vertical { get; private set; }
        public bool JumpHeld => _jump.IsHeld;
        public bool JumpRequested { get; private set; }
        public bool FreshJumpRequested { get; private set; }
        public ulong JumpPressVersion { get; private set; }
        public ulong ClearVersion { get; private set; }
        public bool DashRequested { get; private set; }
        public ulong DashPressVersion { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void OnAvailability(GameInputChannel channel)
        { if (!channel.IsAvailable) Clear(); }
        private void CapturePress()
        {
            if (!_jump.ConsumePress()) return;
            JumpRequested = true; FreshJumpRequested = true;
            _bufferRemaining = _bufferDuration;
            JumpPressVersion++;
        }
        private void CaptureDashPress()
        {
            if (_dash == null || !_dash.ConsumePress()) return;
            DashRequested = true; DashPressVersion++;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public PlayerInputAdapter(GameInputChannel channel, string move, string jump, float bufferDuration = 0,
            string dash = null)
        {
            if (!float.IsFinite(bufferDuration) || bufferDuration < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferDuration));
            _channel = channel; _move = move;
            _bufferDuration = bufferDuration;
            channel.GetAction(move);
            if (!string.IsNullOrEmpty(dash)) channel.GetAction(dash);
            _jump = new GameInputButton(channel, jump);
            try { if (!string.IsNullOrEmpty(dash)) _dash = new GameInputButton(channel, dash); }
            catch { _jump.Dispose(); throw; }
            channel.AvailabilityChanged += OnAvailability;
        }
        public void Sample()
        {
            var direction = _channel.ReadContinuous<Vector2>(_move);
            Horizontal = Mathf.Clamp(direction.x, -1, 1);
            Vertical = Mathf.Clamp(direction.y, -1, 1);
            CapturePress();
            CaptureDashPress();
        }
        public bool ConsumeJump()
        {
            CapturePress();
            bool requested = JumpRequested;
            JumpRequested = false; FreshJumpRequested = false;
            _bufferRemaining = 0;
            return requested;
        }
        public bool ConsumeDash()
        {
            CaptureDashPress();
            bool requested = DashRequested; DashRequested = false; return requested;
        }
        // 只按角色实际执行的物理步计时；零窗口仍保留首次物理决策机会。
        public void EndPhysicsStep(float dt)
        {
            _bufferRemaining = Mathf.Max(0, _bufferRemaining - dt);
            if (_bufferRemaining <= 0.000001f) JumpRequested = false;
            FreshJumpRequested = false; // 下穿只使用首个物理决策，不继承跳跃缓冲。
            // 冲刺无额外缓冲窗口：只保留到首个物理决策，拒绝后不在 CD 结束时补发。
            DashRequested = false;
        }
        public void Clear()
        {
            Horizontal = 0; Vertical = 0; JumpRequested = false; FreshJumpRequested = false; _bufferRemaining = 0;
            DashRequested = false;
            ClearVersion++; _jump.ClearPending(); _dash?.ClearPending();
        }
        public void Dispose() { _channel.AvailabilityChanged -= OnAvailability; _jump.Dispose(); _dash?.Dispose(); Clear(); }
        #endregion
    }
}

#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>仅编辑器的每会话物理步环形记录；读取不采样、不驱动业务。</summary>
    public sealed class PlayerMotionTrace
    {
        #region 内部参数
        public const int CAPACITY = 512;
        private readonly Sample[] _samples = new Sample[CAPACITY];
        private int _next;
        private bool _jump;
        private double _jumpStart;
        private float _jumpY;
        public int Count { get; private set; }
        public double Time { get; private set; }
        public float JumpRise { get; private set; }
        public double JumpPeakTime { get; private set; }
        public double JumpDuration { get; private set; }
        public bool HasJump { get; private set; }
        public bool JumpComplete => HasJump && !_jump;
        public string JumpEndState { get; private set; }
        public readonly struct Sample
        {
            public readonly double Time;
            public readonly Vector2 Position, Velocity;
            public readonly float Gravity, Buffer, Coyote;
            public readonly string State;
            internal Sample(double time, Vector2 position, Vector2 velocity, float gravity, float buffer, float coyote, string state)
            { Time = time; Position = position; Velocity = velocity; Gravity = gravity; Buffer = buffer; Coyote = coyote; State = state; }
        }
        public Sample this[int index] => index >= 0 && index < Count
            ? _samples[(_next - Count + index + CAPACITY) % CAPACITY] : throw new ArgumentOutOfRangeException(nameof(index));
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal void Record(float dt, Vector2 observedPosition, Vector2 velocity, float gravity, float buffer, float coyote, string state)
        {
            if (!float.IsFinite(dt) || dt <= 0) return;
            if (state == "jump" && !_jump)
            {
                _jump = true; HasJump = true; _jumpStart = Time; _jumpY = observedPosition.y;
                JumpRise = 0; JumpPeakTime = 0; JumpDuration = 0; JumpEndState = null;
            }
            if (_jump)
            {
                float rise = observedPosition.y - _jumpY;
                if (rise > JumpRise) { JumpRise = rise; JumpPeakTime = Time - _jumpStart; }
                JumpDuration = Time - _jumpStart;
                if (state != "jump") { _jump = false; JumpEndState = state; }
            }
            _samples[_next] = new Sample(Time, observedPosition, velocity, gravity, buffer, coyote, state);
            _next = (_next + 1) % CAPACITY; Count = Math.Min(Count + 1, CAPACITY); Time += dt;
        }
        internal void Clear()
        {
            Array.Clear(_samples, 0, _samples.Length); _next = Count = 0; Time = 0;
            _jump = HasJump = false; JumpRise = 0; JumpPeakTime = JumpDuration = 0; JumpEndState = null;
        }
        #endregion
    }
}
#endif

using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine;

namespace Game.Input
{
    /// <summary>
    /// 显式选择“新按下触发/蓄力起始”语义的独立适配器；不根据 Button/Value 自动推断业务。
    /// 一次待消费按下可跨 Update/FixedUpdate 保留；玩法时限缓冲由角色适配器扩展。
    /// </summary>
    public sealed class GameInputButton : IDisposable
    {
        #region 内部参数
        private readonly GameInputChannel _channel;
        private readonly InputAction _action;
        private bool _available;
        private bool _waitForRelease;
        private bool _physicalHeld;
        private bool _pending;
        private bool _disposed;
        public bool IsHeld { get; private set; }
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private bool ReadPhysicalHeld(InputEventPtr inputEvent = default, InputDevice eventDevice = null)
        {
            // Action Map 禁止期间 action.IsPressed 为 false；controls 仍能读取设备当前状态。
            // 恢复必须检查物理状态，否则 Enable 后的初始状态检查会泄漏旧按下。
            foreach (var control in _action.controls)
            {
                if (control is not AxisControl axis) continue;
                float threshold = control is ButtonControl button
                    ? button.pressPointOrDefault : InputSystem.settings.defaultButtonPressPoint;
                float value = axis.ReadValue();
                if (eventDevice == control.device && axis.ReadValueFromEvent(inputEvent, out float eventValue))
                    value = eventValue;
                if (Mathf.Abs(value) >= threshold) return true;
            }
            return false;
        }

        private void OnAvailabilityChanged(GameInputChannel channel)
        {
            if (channel.IsStopped)
            {
                Dispose();
                return;
            }
            _available = channel.IsAvailable;
            _pending = false;
            IsHeld = false;
            _physicalHeld = ReadPhysicalHeld();
            _waitForRelease = _physicalHeld;
        }

        private void OnInputEvent(InputEventPtr inputEvent, InputDevice device)
        {
            if (_disposed || !_channel.IsAvailable || !_available) return;
            if (inputEvent.handled || (!inputEvent.IsA<StateEvent>() && !inputEvent.IsA<DeltaStateEvent>())) return;
            // onEvent 在设备状态写入前调用；用本事件值覆盖对应控件，以免吞掉同次更新内的短按/释放。
            SampleHeld(ReadPhysicalHeld(inputEvent, device));
        }

        private void Sample()
        {
            if (_disposed || !_channel.IsAvailable || !_available) return;
            SampleHeld(ReadPhysicalHeld());
        }

        private void SampleHeld(bool held)
        {
            if (_disposed) return;
            // 禁止期间不累积玩法事件；关闭通知也会同步清除所有语义状态。
            if (!_channel.IsAvailable || !_available)
            {
                _pending = false;
                IsHeld = false;
                return;
            }
            if (!held)
            {
                _waitForRelease = false;
                _physicalHeld = false;
                IsHeld = false;
                return;
            }
            if (!_waitForRelease && !_physicalHeld)
            {
                _pending = true;
                IsHeld = true;
            }
            _physicalHeld = true;
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public GameInputButton(GameInputChannel channel, string actionName)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _action = channel.GetAction(actionName);
            foreach (var binding in _action.bindings)
                if (binding.isComposite || binding.isPartOfComposite)
                    throw new ArgumentException("新按下示例只支持直接控件绑定；组合按键请提供项目适配器。", nameof(actionName));
            foreach (var control in _action.controls)
                if (control is not AxisControl)
                    throw new ArgumentException("新按下示例需要直接的 Button/Axis 控件绑定。", nameof(actionName));
            OnAvailabilityChanged(channel);
            _channel.AvailabilityChanged += OnAvailabilityChanged;
            InputSystem.onEvent += OnInputEvent;
            // 补充设备移除/复位后的状态观察；不依赖 Button 的初始检查或 canceled。
            InputSystem.onAfterUpdate += Sample;
        }

        public bool ConsumePress()
        {
            if (_disposed || !_channel.IsAvailable || !_pending) return false;
            _pending = false;
            return true;
        }

        public void ClearPending() => _pending = false;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _channel.AvailabilityChanged -= OnAvailabilityChanged;
            InputSystem.onEvent -= OnInputEvent;
            InputSystem.onAfterUpdate -= Sample;
            _pending = false;
            IsHeld = false;
        }
        #endregion
    }
}

// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using Ember.Basic;

namespace Ember.UI
{
    /// <summary>页面与 Item 共用的数据绑定。登记、绑定和结果写回都在 Unity 主线程进行。</summary>
    public sealed class EUIDataBinding
    {
        #region 内部参数

        private List<Action> _resets;
        private long _version;
        private bool _active;
        private bool _preparing;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal EUIDataBinding() { }

        /// <summary>登记随数据重置的字段。建议在 Logic.OnInit 中登记一次。</summary>
        public void RegisterReset(Action reset)
        {
            if (reset == null) throw new ArgumentNullException(nameof(reset));
            _resets ??= new List<Action>();
            if (!_resets.Contains(reset))
                _resets.Add(reset);
        }

        /// <summary>取消登记，例如数据绑定控件被替换时。</summary>
        public void UnregisterReset(Action reset) => _resets?.Remove(reset);

        /// <summary>在发起异步请求前捕获当前绑定。请求完成后用 TryApply 写回。</summary>
        public Version Capture() => _active ? new Version(this, _version) : default;

        internal bool Begin()
        {
            if (_preparing) return false;

            End();
            var version = _version;
            if (_resets is { Count: > 0 })
            {
                _preparing = true;
                try
                {
                    using var pool = UnityEngine.Pool.ListPool<Action>.Get(out var resets);
                    resets.AddRange(_resets);
                    foreach (var reset in resets)
                    {
                        if (_version != version) return false;
                        try { reset(); }
                        catch (Exception ex)
                        {
                            EmberDebug.LogError(LogTags.UIManager, $"EUI 数据字段重置失败: {ex}");
                        }
                    }
                }
                finally { _preparing = false; }
            }

            _active = _version == version;
            return _active;
        }

        internal void End(bool forgetRegistrations = false)
        {
            _active = false;
            _version++;
            if (forgetRegistrations)
                _resets?.Clear();
        }

        /// <summary>异步操作捕获的数据绑定版本。</summary>
        public readonly struct Version
        {
            private readonly EUIDataBinding _owner;
            private readonly long _version;

            internal Version(EUIDataBinding owner, long version)
            {
                _owner = owner;
                _version = version;
            }

            public bool IsCurrent => _owner != null && _owner._active && _owner._version == _version;

            /// <summary>当前绑定仍有效时写回结果。</summary>
            public bool TryApply(Action apply)
            {
                if (!IsCurrent) return false;
                apply();
                return true;
            }
        }

        #endregion
    }
}

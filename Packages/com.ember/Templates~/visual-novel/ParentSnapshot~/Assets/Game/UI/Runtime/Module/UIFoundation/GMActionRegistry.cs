using System;
using System.Collections.Generic;
using Ember.Basic;

namespace Game.UI
{
    public sealed class GMActionDescriptor
    {
        public string Key, Group, Title, Icon;
        public int Order;
        public bool Dangerous;
        public Func<bool> IsAvailable;
        public Action Execute;
        public Func<bool> ReadToggle;
        public Action<bool> WriteToggle;
    }

    public interface IGMActionProvider { IReadOnlyList<GMActionDescriptor> Actions { get; } }

    /// <summary>业务模块持有注册句柄；按钮只保存key，不捕获可失效的业务对象。</summary>
    public static class GMActionRegistry
    {
        #region 内部参数
        private static readonly List<Registration> Providers = new();
        public static int Version { get; private set; }
        public static bool ConfirmationActive { get; private set; }
        public static event Action<bool> ConfirmationVisibilityChanged;
        private sealed class Registration : IDisposable
        {
            public IGMActionProvider Provider;
            public void Dispose() { if (Provider == null) return; Providers.Remove(this); Provider = null; Version++; }
        }
        #endregion
        #region 外部方法
        [HasGC]
        public static IDisposable Register(IGMActionProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (provider.Actions == null) throw new ArgumentException("GM action list is required");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var action in provider.Actions)
            {
                if (action == null || string.IsNullOrEmpty(action.Key)) throw new ArgumentException("Every GM action requires a stable key");
                if (!keys.Add(action.Key) || TryFind(action.Key, out _)) throw new ArgumentException("Duplicate GM action key: " + action.Key);
            }
            var registration = new Registration { Provider = provider }; Providers.Add(registration); Version++; return registration;
        }

        [HasGC]
        public static List<GMActionDescriptor> Snapshot()
        {
            var actions = new List<GMActionDescriptor>();
            foreach (var registration in Providers) foreach (var action in registration.Provider.Actions) actions.Add(action);
            actions.Sort((a,b) => { int group = string.CompareOrdinal(a.Group, b.Group); return group != 0 ? group : a.Order != b.Order ? a.Order.CompareTo(b.Order) : string.CompareOrdinal(a.Key,b.Key); });
            return actions;
        }

        [NoGC]
        public static bool TryFind(string key, out GMActionDescriptor action)
        {
            foreach (var registration in Providers)
            {
                var items = registration.Provider.Actions;
                for (int i = 0; i < items.Count; i++)
                    if (items[i].Key == key) { action = items[i]; return true; }
            }
            action = null; return false;
        }

        [HasGC]
        public static bool TryExecute(string key)
        {
            if (!TryFind(key, out var action) || action.IsAvailable?.Invoke() == false) return false;
            if (action.Dangerous)
            {
                if(ConfirmationActive)return false;
                ConfirmationActive=true;ConfirmationVisibilityChanged?.Invoke(true);
                EUICommonDialogs.ShowConfirm(new EUIDialogRequest {
                    Title = "确认操作", Message = action.Title, AcceptText = "确认", CancelText = "取消",
                    Completed = result => { try { if (result == EUIDialogResult.Accepted) ExecuteConfirmed(key); } finally { ConfirmationActive=false;ConfirmationVisibilityChanged?.Invoke(false); } }
                });
                return true;
            }
            return ExecuteConfirmed(key);
        }

        private static bool ExecuteConfirmed(string key)
        {
            if (!TryFind(key, out var action) || action.IsAvailable?.Invoke() == false) return false;
            if (action.ReadToggle != null && action.WriteToggle != null) action.WriteToggle(!action.ReadToggle()); else action.Execute?.Invoke();
            return true;
        }
        #endregion
    }
}

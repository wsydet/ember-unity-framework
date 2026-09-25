using System;
using Ember.Basic;
using UnityEngine;

namespace Game.Narrative
{
    /// <summary>宿主显式发布诊断源，窗口只读订阅。注册句柄由宿主释放；不控制运行器。</summary>
    public static class NarrativeObservation
    {
        #region 内部参数
        private static Registration _registration;
        public static INarrativeDiagnostics Current => _registration?.Source;
        public static event Action SourceChanged;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { _registration = null; Notify(); }

        private static void Notify()
        {
            if (SourceChanged == null) return;
            foreach (Action handler in SourceChanged.GetInvocationList())
                try { handler(); }
                catch (Exception ex) { EmberDebug.LogError("Game.Narrative", "Narrative observation: " + ex); }
        }

        private sealed class Registration : IDisposable
        {
            internal readonly INarrativeDiagnostics Source;
            internal Registration(INarrativeDiagnostics source) { Source = source; }
            public void Dispose()
            {
                if (_registration != this) return;
                _registration = null;
                Notify();
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [HasGC]
        public static IDisposable Register(INarrativeDiagnostics source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            _registration = new Registration(source);
            var result = _registration;
            Notify();
            return result;
        }
        #endregion
    }
}

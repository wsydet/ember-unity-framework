using System;
using Ember.Basic;
using Game.Module;
using UnityEngine;

namespace Game.UI
{
    public enum UIFrameRateMode { Economy, Balanced, Responsive }

    /// <summary>base 桌面帧率策略。设置与 GM 共用；只在用户修改或自动刷新率改变时写引擎。</summary>
    public static class UIFrameRatePolicy
    {
        #region 内部参数
        private static readonly double[] Rates = { 60, 90, 120, 144, 165, 180, 240, 360 };
        private static readonly int[] Margins = { 6, 5, 4, 3, 3, 2, 2, 1 };
        private static bool _initialized;
        private static double _refreshRate;
        private static int _writtenTarget, _writtenVSync;
        private static string _storageKey = "Ember.UI.FrameRate.v1." + Application.identifier;
        public static UIFrameRateMode Mode { get; private set; } = UIFrameRateMode.Responsive;
        public static bool Automatic { get; private set; } = true;
        public static int CustomTarget { get; private set; } = 180;
        public static bool IsDesktop => !Application.isMobilePlatform && !UnityEngine.XR.XRSettings.enabled;
        public static double RefreshRate => Screen.currentResolution.refreshRateRatio.value;
        public static event Action Changed;
        #endregion
        #region 内部方法
        // Unity 在禁用域重载时仍调用此入口。新运行重新读取偏好；同一运行 Initialize 保持幂等。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void BeginRun()
        {
            _initialized = false; _refreshRate = 0;
            _writtenTarget = _writtenVSync = 0;
            Mode = UIFrameRateMode.Responsive; Automatic = true; CustomTarget = 180;
            Changed = null;
        }
        private static void Load()
        {
            Mode = UIFrameRateMode.Responsive; Automatic = true; CustomTarget = 180;
            // 严格校验整条记录，缺失字段、未知版本或非法值均回退，读取时不覆盖损坏记录。
            string[] fields = PlayerPrefsModule.Instance.GetString(_storageKey, "").Split('|');
            if (fields.Length != 4 || fields[0] != "1"
                || !int.TryParse(fields[1], out int mode) || mode < 0 || mode > 2
                || (fields[2] != "0" && fields[2] != "1")
                || !int.TryParse(fields[3], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int target) || target < 1) return;
            Mode = (UIFrameRateMode)mode; Automatic = fields[2] == "1"; CustomTarget = target;
        }
        private static void Save()
        {
            var prefs = PlayerPrefsModule.Instance;
            prefs.SetString(_storageKey, "1|" + (int)Mode + "|" + (Automatic ? "1" : "0") + "|" + CustomTarget.ToString(System.Globalization.CultureInfo.InvariantCulture));
            prefs.Save();
        }
        private static void Apply()
        {
            if (!IsDesktop) { Changed?.Invoke(); return; }
            QualitySettings.vSyncCount = Mode == UIFrameRateMode.Economy ? 2 : Mode == UIFrameRateMode.Balanced ? 1 : 0;
            if (Mode == UIFrameRateMode.Responsive) Application.targetFrameRate = Automatic ? AutomaticTarget(RefreshRate) : CustomTarget;
            _writtenTarget = Application.targetFrameRate; _writtenVSync = QualitySettings.vSyncCount;
            _refreshRate = RefreshRate;
            Changed?.Invoke();
        }
        #endregion
        #region 外部方法
        [HasGC]
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true; Load(); Apply();
        }
        [NoGC]
        public static int AutomaticTarget(double hz)
        {
            if (double.IsNaN(hz) || double.IsInfinity(hz) || hz <= 0) hz = 60;
            for (int i = 0; i < Rates.Length; i++) if (hz <= Rates[i]) return (int)Math.Ceiling(hz + Margins[i]);
            return (int)Math.Min(int.MaxValue, Math.Ceiling(hz + 1));
        }
        [HasGC]
        public static void SetMode(UIFrameRateMode mode)
        {
            if (mode < UIFrameRateMode.Economy || mode > UIFrameRateMode.Responsive) return;
            Initialize(); Mode = mode; Save(); Apply();
        }
        [HasGC]
        public static void SetAutomatic(bool automatic) { Initialize(); Automatic = automatic; Save(); if (Mode == UIFrameRateMode.Responsive) Apply(); else Changed?.Invoke(); }
        [HasGC]
        public static bool SetCustom(string text)
        {
            if (!int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int value) || value < 1) return false;
            Initialize(); CustomTarget = value; Save(); if (!Automatic && Mode == UIFrameRateMode.Responsive) Apply(); else Changed?.Invoke(); return true;
        }
        [HasGC]
        public static void CheckRefreshRate()
        {
            if (!_initialized || Mode != UIFrameRateMode.Responsive || !Automatic || Math.Abs(_refreshRate - RefreshRate) < .01) return;
            // Respect a consumer that has taken control since our last explicit selection.
            if (Application.targetFrameRate == _writtenTarget && QualitySettings.vSyncCount == _writtenVSync) Apply();
            else _refreshRate = RefreshRate;
        }
        [HasGC]
        public static string DescribeActual()
        {
            double hz = RefreshRate; int sync = QualitySettings.vSyncCount;
            string target = sync > 0 ? (hz / sync).ToString("0.#") + " FPS · VSync " + sync : Application.targetFrameRate > 0 ? Application.targetFrameRate + " FPS" : "不限帧";
            return "屏幕 " + hz.ToString("0.#") + " Hz  ·  目标 " + target;
        }
        #endregion
    }
}

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Ember.Basic
{
    /// <summary>
    /// 框架日志工具 —— Unity Debug.Log 的增强封装。
    ///
    /// 特性：
    /// - <b>彩色标签</b>：每个类名 hash 生成专属颜色，Console 中一眼识别
    /// - <b>按类过滤</b>：运行时开启/关闭特定类的日志
    /// - <b>全局开关</b>：一键静默所有 Info/Warning，Error 始终输出
    /// - <b>精准跳转</b>：每条日志附 (at path:line)，Console Pro 自动识别
    /// - <b>调试友好</b>：F11 不步入 Logger，堆栈不显示 Logger 方法
    ///
    /// 用法：
    /// <code>
    /// private const string TAG = nameof(AudioManager);
    ///
    /// EmberDebug.Log(TAG, "BGM loaded.");
    /// EmberDebug.LogWarning(TAG, "Mixer not found.");
    /// EmberDebug.LogError(TAG, "Load failed.");
    ///
    /// // 过滤
    /// EmberDebug.Disable(TAG);
    /// EmberDebug.Enable(TAG);
    /// EmberDebug.SetGlobalOpen(false);  // 全关
    /// </code>
    /// </summary>
    [System.Diagnostics.DebuggerStepThrough]
    public static class EmberDebug
    {
        #region 参数

        private static readonly Dictionary<string, ClassEntry> _entries = new();
        private static EmberDebugConfigSO _config;
        private static bool _globalOpen = true;
        private static bool _loaded;

        private class ClassEntry
        {
            public bool Enabled = true;
            public Color Color = Color.white;
        }

        #endregion

        // ============================================================

        #region 外部方法

        // ======== 加载 ========

        /// <summary>
        /// 从 Resources 加载 SO 配置并应用。
        /// 不调用也会在首次 Log 时自动加载（延迟加载）。
        /// </summary>
        public static void LoadConfig()
        {
            if (_loaded) return;

            _config = Resources.Load<EmberDebugConfigSO>("EmberDebugConfig");

            if (_config != null)
            {
                _globalOpen = _config.globalOpen;

                foreach (var entry in _config.frameworkEntries)
                {
                    _entries[entry.className] = new ClassEntry
                    {
                        Enabled = entry.enabled,
                        Color = entry.color
                    };
                }
                foreach (var entry in _config.userEntries)
                {
                    _entries[entry.className] = new ClassEntry
                    {
                        Enabled = entry.enabled,
                        Color = entry.color
                    };
                }
            }
            else
            {
                Debug.LogWarning("[Ember] EmberDebugConfig.asset not found. Using default config.");
            }

            _loaded = true;
        }

        /// <summary>
        /// 获取底层 SO 配置（编辑器面板用）。
        /// </summary>
        public static EmberDebugConfigSO ConfigSO => _config;

        // ======== 全局开关 ========

        /// <summary>
        /// 全局开关。关闭后所有非 Error 日志静默。
        /// 读取时优先从 SO 获取（反映 Inspector 实时修改），
        /// 写入时同步更新 SO 和内存缓存。
        /// </summary>
        public static bool GlobalOpen
        {
            get => _config != null ? _config.globalOpen : _globalOpen;
            set
            {
                _globalOpen = value;
                if (_config != null) _config.globalOpen = value;
            }
        }

        // ======== 按类过滤 ========

        /// <summary>关闭指定标签的日志输出（同步更新 SO 和缓存）。</summary>
        public static void Disable(string tag)
        {
            GetOrCreate(tag).Enabled = false;
            SyncTagToConfig(tag, enabled: false);
        }

        /// <summary>开启指定标签的日志输出（同步更新 SO 和缓存）。</summary>
        public static void Enable(string tag)
        {
            GetOrCreate(tag).Enabled = true;
            SyncTagToConfig(tag, enabled: true);
        }

        /// <summary>设置指定标签的专属颜色（同步更新 SO 和缓存）。</summary>
        public static void SetColor(string tag, Color color)
        {
            GetOrCreate(tag).Color = color;
            SyncTagToConfig(tag, color: color);
        }

        /// <summary>将标签状态同步回 SO（使代码中的 Disable/Enable 即时生效）。</summary>
        private static void SyncTagToConfig(string tag, bool? enabled = null, Color? color = null)
        {
            if (_config == null) return;
            var entry = _config.GetOrCreate(tag);
            if (enabled.HasValue) entry.enabled = enabled.Value;
            if (color.HasValue) entry.color = color.Value;
        }

        /// <summary>
        /// 标签当前是否允许打印。先从 SO 读（反映 Inspector 实时修改），
        /// SO 中没有时回退内存缓存。先查自身，再查父级。
        /// 父标签关闭 → 所有子标签都静默。
        /// </summary>
        public static bool IsEnabled(string tag)
        {
            if (_config != null)
            {
                // 自身：SO 优先，缓存回退
                if (!IsEnabledFromSOOrCache(tag))
                    return false;

                // 父级：SO 优先，缓存回退
                var parent = LogTags.GetParent(tag);
                if (parent != null && !IsEnabledFromSOOrCache(parent))
                    return false;

                return true;
            }

            // 回退：无 SO 时使用内存缓存
            if (_entries.TryGetValue(tag, out var ce) && !ce.Enabled)
                return false;

            var cachedParent = LogTags.GetParent(tag);
            if (cachedParent != null && _entries.TryGetValue(cachedParent, out var cpe) && !cpe.Enabled)
                return false;

            return true;
        }

        /// <summary>SO 中有条目则用 SO 值，否则回退缓存。找不到视为允许。</summary>
        private static bool IsEnabledFromSOOrCache(string tag)
        {
            if (_config.TryGet(tag, out var e))
                return e.enabled; // SO 中有 → 用 SO 值

            // SO 中没有 → 回退缓存（autoCollect=false 时 Disable 的 tag 只存在于缓存）
            if (_entries.TryGetValue(tag, out var ce))
                return ce.Enabled;

            return true; // 都没有 → 默认允许
        }

        // ======== Info（白色） ========

        [HideInCallstack]
        public static void Log(string tag, string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!CanLog(tag)) return;
            Debug.Log(FormatMsg(tag, message, LogColors.Info, filePath, lineNumber));
        }

        [HideInCallstack]
        public static void Log(string tag, string message, Object context,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!CanLog(tag)) return;
            Debug.Log(FormatMsg(tag, message, LogColors.Info, filePath, lineNumber), context);
        }

        // ======== Init（绿色） ========

        [HideInCallstack]
        public static void LogInit(string tag, string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!CanLog(tag)) return;
            Debug.Log(FormatMsg(tag, message, LogColors.Init, filePath, lineNumber));
        }

        [HideInCallstack]
        public static void LogInit(string tag, string message, Object context,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!CanLog(tag)) return;
            Debug.Log(FormatMsg(tag, message, LogColors.Init, filePath, lineNumber), context);
        }

        // ======== Event（紫色） ========

        [HideInCallstack]
        public static void LogEvent(string tag, string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!CanLog(tag)) return;
            Debug.Log(FormatMsg(tag, message, LogColors.Event, filePath, lineNumber));
        }

        [HideInCallstack]
        public static void LogEvent(string tag, string message, Object context,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!CanLog(tag)) return;
            Debug.Log(FormatMsg(tag, message, LogColors.Event, filePath, lineNumber), context);
        }

        // ======== Cleanup（灰色） ========

        [HideInCallstack]
        public static void LogCleanup(string tag, string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!CanLog(tag)) return;
            Debug.Log(FormatMsg(tag, message, LogColors.Cleanup, filePath, lineNumber));
        }

        [HideInCallstack]
        public static void LogCleanup(string tag, string message, Object context,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!CanLog(tag)) return;
            Debug.Log(FormatMsg(tag, message, LogColors.Cleanup, filePath, lineNumber), context);
        }

        // ======== Shutdown（淡紫色） ========

        [HideInCallstack]
        public static void LogShutdown(string tag, string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!CanLog(tag)) return;
            Debug.Log(FormatMsg(tag, message, LogColors.Shutdown, filePath, lineNumber));
        }

        [HideInCallstack]
        public static void LogShutdown(string tag, string message, Object context,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!CanLog(tag)) return;
            Debug.Log(FormatMsg(tag, message, LogColors.Shutdown, filePath, lineNumber), context);
        }

        // ======== Warning（橙色） ========

        [HideInCallstack]
        public static void LogWarning(string tag, string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!GlobalOpen || !IsEnabled(tag)) return;
            Debug.LogWarning(FormatMsg(tag, message, LogColors.Warning, filePath, lineNumber));
        }

        [HideInCallstack]
        public static void LogWarning(string tag, string message, Object context,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!GlobalOpen || !IsEnabled(tag)) return;
            Debug.LogWarning(FormatMsg(tag, message, LogColors.Warning, filePath, lineNumber), context);
        }

        // ======== Error（红色） ========

        [HideInCallstack]
        public static void LogError(string tag, string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Debug.LogError(FormatMsg(tag, message, LogColors.Error, filePath, lineNumber));
        }

        [HideInCallstack]
        public static void LogError(string tag, string message, Object context,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Debug.LogError(FormatMsg(tag, message, LogColors.Error, filePath, lineNumber), context);
        }

        [HideInCallstack]
        public static void LogException(string tag, System.Exception ex,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Debug.LogError(FormatMsg(tag, ex.Message, LogColors.Error, filePath, lineNumber));
        }

        #endregion

        // ============================================================

        #region 内部方法

        private static bool CanLog(string tag)
        {
            // 直接从 SO 读取（反映 Inspector 实时修改），SO 不可用时回退缓存
            bool global = _config != null ? _config.globalOpen : _globalOpen;
            if (!global) return false;
            return IsEnabled(tag);
        }

        private static string FormatMsg(string tag, string message, string msgColor,
            string filePath, int lineNumber)
        {
            // 预定义标签动态计算颜色（子标签继承父标签，代码修改实时生效）
            // 非预定义标签回退缓存颜色（用户可通过 SO 自定义）
            Color tagColor = LogTagColors.GetColor(tag) ?? GetOrCreate(tag).Color;
            string tagHex = ColorUtility.ToHtmlStringRGB(tagColor);

            return $"<color=#{tagHex}><b>[{tag}]</b></color> <color={msgColor}>{message}</color>\n"
                 + $"<color={LogColors.FileInfo}><i>(at {filePath}:{lineNumber})</i></color>";
        }

        private static ClassEntry GetOrCreate(string tag)
        {
            // 延迟加载：首次调用时自动从 Resources 加载 SO
            if (!_loaded) LoadConfig();

            if (!_entries.TryGetValue(tag, out var entry))
            {
                entry = new ClassEntry
                {
                    Enabled = true,
                    Color = LogTagColors.GetColor(tag) ?? HashColor(tag)
                };
                _entries[tag] = entry;

                // 自动收集：新类写入 SO（仅编辑模式下持久化）
#if UNITY_EDITOR
                if (_config != null && _config.autoCollect)
                {
                    if (!_config.TryGet(tag, out _))
                    {
                        _config.GetOrCreate(tag); // 自动选择 frameworkEntries 或 userEntries
                        UnityEditor.EditorUtility.SetDirty(_config);
                    }
                }
#endif
            }

            return entry;
        }

        /// <summary>从字符串 hash 生成稳定颜色。</summary>
        private static Color HashColor(string str)
        {
            int hash = 0;
            foreach (char c in str)
            {
                hash = c + (hash << 6) + (hash << 16) - hash;
            }

            float hue = Mathf.Abs(hash) % 1000 / 1000f;
            return Color.HSVToRGB(hue, 0.6f, 0.9f);
        }

        #endregion
    }
}

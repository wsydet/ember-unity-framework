using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ember.Core
{
    /// <summary>
    /// 日志配置项。
    /// </summary>
    [Serializable]
    public class LoggerClassEntry
    {
        [Tooltip("类名 / 标签")]
        public string className;

        [Tooltip("是否允许此类的日志输出")]
        public bool enabled = true;

        [Tooltip("Console 中显示的标签颜色")]
        public Color color = Color.white;
    }

    /// <summary>
    /// EmberDebug 的配置 SO —— 在 Inspector 中可视化操作日志开关和颜色。
    ///
    /// 存放路径：Assets/Ember/Core/Runtime/Resources/EmberDebugConfig.asset
    /// 启动时由 EmberDebug 自动从 Resources 加载。
    /// </summary>
    public class EmberDebugConfigSO : ScriptableObject
    {
        [Header("全局")]
        [Tooltip("编辑器和 Development Build 下默认开启，Release 时建议关闭")]
        public bool globalOpen = true;

        [Header("收集")]
        [Tooltip("开启后，新的类首次调用日志时自动加入列表")]
        public bool autoCollect = true;

        [Header("类配置")]
        public List<LoggerClassEntry> classEntries = new();

        // ============================================================

        public LoggerClassEntry GetOrCreate(string className)
        {
            foreach (var entry in classEntries)
            {
                if (entry.className == className)
                    return entry;
            }

            var newEntry = new LoggerClassEntry
            {
                className = className,
                enabled = true,
                color = HashColor(className)
            };
            classEntries.Add(newEntry);
            return newEntry;
        }

        public bool TryGet(string className, out LoggerClassEntry entry)
        {
            foreach (var e in classEntries)
            {
                if (e.className == className)
                {
                    entry = e;
                    return true;
                }
            }

            entry = null;
            return false;
        }

        public void EnableAll()
        {
            foreach (var e in classEntries) e.enabled = true;
        }

        public void DisableAll()
        {
            foreach (var e in classEntries) e.enabled = false;
        }

        private static Color HashColor(string str)
        {
            int hash = 0;
            foreach (char c in str)
                hash = c + (hash << 6) + (hash << 16) - hash;
            float hue = Mathf.Abs(hash) % 1000 / 1000f;
            return Color.HSVToRGB(hue, 0.6f, 0.9f);
        }
    }
}

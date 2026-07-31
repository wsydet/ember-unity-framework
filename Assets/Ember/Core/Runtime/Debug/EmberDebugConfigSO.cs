using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Ember.Core
{
    /// <summary>
    /// 日志配置项 —— 单条标签的开关和颜色。
    /// </summary>
    [Serializable]
    public class LoggerClassEntry
    {
        [HorizontalGroup("Row", Width = 20)]
        [HideLabel]
        public bool enabled = true;

        [HorizontalGroup("Row", Width = 40)]
        [HideLabel]
        [DisableIf("IsPredefined")]
        [LabelText("")]
        public Color color = Color.white;

        [HorizontalGroup("Row")]
        [HideLabel]
        [ReadOnly]
        public string className;

        /// <summary>是否为预定义标签（不可修改颜色）。</summary>
        private bool IsPredefined => !string.IsNullOrEmpty(className) && LogTagColors.IsPredefined(className);

        /// <summary>是否为子标签（带缩进）。</summary>
        private bool IsChild => className != null && className.Contains('.');
    }

    /// <summary>
    /// EmberDebug 的配置 SO —— 在 Inspector 中可视化操作日志开关和颜色。
    ///
    /// 存放路径：Assets/Ember/Core/Runtime/Resources/EmberDebugConfig.asset
    /// 启动时由 EmberDebug 自动从 Resources 加载。
    ///
    /// <b>双列表设计：</b>
    /// - frameworkEntries：框架内置标签（EmberCore、EmberAudio 等），颜色预锁不可改
    /// - userEntries：使用者自己的标签，运行时自动收集，自由修改颜色
    /// </summary>
    public class EmberDebugConfigSO : EmberBaseSO
    {
        [Title("全局设置")]
        [Tooltip("编辑器和 Development Build 下默认开启，Release 时建议关闭")]
        public bool globalOpen = true;

        [Tooltip("开启后，新的类首次调用日志时自动加入列表")]
        public bool autoCollect = true;

        [Title("框架标签", "Ember 框架内置的日志标签。颜色跟随预定义，不可修改。")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true)]
        public List<LoggerClassEntry> frameworkEntries = new();

        [Title("用户标签", "使用者自己的日志标签。运行时自动收集，可自由修改颜色。")]
        [InfoBox("运行时调用 EmberDebug.Log() 会自动收集新标签（如果开启了自动收集）。", InfoMessageType.Info)]
        public List<LoggerClassEntry> userEntries = new();

        // ============================================================

        /// <summary>
        /// 按 className 查找或创建条目。预定义标签进入 frameworkEntries，其余进入 userEntries。
        /// </summary>
        public LoggerClassEntry GetOrCreate(string className)
        {
            var list = LogTagColors.IsPredefined(className) ? frameworkEntries : userEntries;
            foreach (var entry in list)
            {
                if (entry.className == className)
                    return entry;
            }

            var newEntry = new LoggerClassEntry
            {
                className = className,
                enabled = true,
                color = LogTagColors.GetColor(className) ?? HashColor(className)
            };
            list.Add(newEntry);
            return newEntry;
        }

        /// <summary>
        /// 按 className 查找条目，同时搜索两个列表。
        /// </summary>
        public bool TryGet(string className, out LoggerClassEntry entry)
        {
            foreach (var e in frameworkEntries)
            {
                if (e.className == className)
                {
                    entry = e;
                    return true;
                }
            }
            foreach (var e in userEntries)
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

        /// <summary>开启所有标签（框架 + 用户）。</summary>
        public void EnableAll()
        {
            foreach (var e in frameworkEntries) e.enabled = true;
            foreach (var e in userEntries) e.enabled = true;
        }

        /// <summary>关闭所有标签（框架 + 用户）。Error 级别不受影响。</summary>
        public void DisableAll()
        {
            foreach (var e in frameworkEntries) e.enabled = false;
            foreach (var e in userEntries) e.enabled = false;
        }

        /// <summary>清理空项（两个列表都清理）。</summary>
        public void CleanEmpty()
        {
            frameworkEntries.RemoveAll(e => string.IsNullOrEmpty(e.className));
            userEntries.RemoveAll(e => string.IsNullOrEmpty(e.className));
        }

        /// <summary>预填充框架标签列表。仅在 SO 创建时调用一次。</summary>
        public void PopulateFrameworkEntries()
        {
            frameworkEntries.Clear();
            foreach (var tag in LogTags.All)
            {
                frameworkEntries.Add(new LoggerClassEntry
                {
                    className = tag,
                    enabled = true,
                    color = LogTagColors.GetColor(tag) ?? Color.white
                });
            }
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

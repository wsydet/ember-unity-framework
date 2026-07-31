using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ember.Core
{
    // ============================================================
    // 日志标签常量（两级分层：父标签.子标签）
    // ============================================================

    /// <summary>
    /// 日志标签常量表 —— 两级分层（Parent.Child）。
    /// 父标签可一键关闭所有子标签，子标签也可单独开关。
    /// 用法：<c>EmberDebug.Log(LogTags.CoreEventBus, "msg");</c>
    /// </summary>
    public static class LogTags
    {
        // 父标签
        public const string EmberCore     = nameof(EmberCore);
        public const string EmberResource = nameof(EmberResource);
        public const string EmberUI       = nameof(EmberUI);
        public const string EmberScene    = nameof(EmberScene);
        public const string EmberAudio    = nameof(EmberAudio);
        public const string EmberInput    = nameof(EmberInput);
        public const string Game     = nameof(Game);

        // Core 子标签
        public const string CoreEventBus      = EmberCore + ".EventBus";
        public const string CoreServiceLocator = EmberCore + ".ServiceLocator";
        public const string CoreSingleton      = EmberCore + ".Singleton";
        public const string CoreObjectPool     = EmberCore + ".ObjectPool";
        public const string CoreManagerCollector = EmberCore + ".ManagerCollector";
        public const string CoreUpdateManager  = EmberCore + ".UpdateManager";
        public const string CoreStateMachine   = EmberCore + ".StateMachine";

        // Resource 子标签
        public const string ResourceManager = EmberResource + ".Manager";
        public const string ResourceProvider = EmberResource + ".Provider";

        // UI 子标签
        public const string UIManager = EmberUI + ".Manager";

        // Scene 子标签
        public const string SceneManager = EmberScene + ".Manager";

        // Audio 子标签
        public const string AudioManager = EmberAudio + ".Manager";

        // Input 子标签
        public const string InputManager = EmberInput + ".Manager";

        /// <summary>所有预定义标签的集合，用于编辑器判断是否锁住</summary>
        public static readonly HashSet<string> All = new()
        {
            // 父标签
            EmberCore, EmberResource, EmberUI, EmberScene, EmberAudio, EmberInput, Game,
            // Core 子
            CoreEventBus, CoreServiceLocator, CoreSingleton, CoreObjectPool,
            CoreManagerCollector, CoreUpdateManager, CoreStateMachine,
            // 其他子
            ResourceManager, ResourceProvider, UIManager, SceneManager, AudioManager, InputManager,
        };

        /// <summary>获取父标签名（无父级返回 null）</summary>
        public static string GetParent(string tag)
        {
            int dot = tag.LastIndexOf('.');
            return dot > 0 ? tag[..dot] : null;
        }
    }

    // ============================================================
    // 标签颜色（子标签继承父级颜色）
    // ============================================================

    /// <summary>
    /// 预定义标签的专属颜色。子标签继承父标签颜色。
    /// </summary>
    public static class LogTagColors
    {
        public static readonly Color Core     = new(0.25f, 0.85f, 0.40f); // 绿色
        public static readonly Color Resource = new(0.42f, 0.65f, 0.85f); // 蓝色
        public static readonly Color UI       = new(0.90f, 0.55f, 0.30f); // 橙色
        public static readonly Color Scene    = new(0.65f, 0.50f, 0.85f); // 紫色
        public static readonly Color Audio    = new(0.90f, 0.75f, 0.25f); // 金色
        public static readonly Color Input    = new(0.45f, 0.80f, 0.80f); // 青色
        public static readonly Color Game     = new(0.80f, 0.40f, 0.60f); // 粉色

        /// <summary>
        /// 获取标签的预定义颜色。
        /// 先精确匹配，没有则查父级，都没有返回 null。
        /// </summary>
        public static Color? GetColor(string tag)
        {
            if (TryGetExact(tag, out var color))
                return color;

            var parent = LogTags.GetParent(tag);
            if (parent != null)
                return GetColor(parent);

            return null;
        }

        /// <summary>检查是否为预定义标签（含子标签）</summary>
        public static bool IsPredefined(string tag)
        {
            return LogTags.All.Contains(tag) || LogTags.All.Contains(LogTags.GetParent(tag) ?? "");
        }

        private static bool TryGetExact(string tag, out Color color)
        {
            color = tag switch
            {
                nameof(LogTags.EmberCore)     => Core,
                nameof(LogTags.EmberResource) => Resource,
                nameof(LogTags.EmberUI)       => UI,
                nameof(LogTags.EmberScene)    => Scene,
                nameof(LogTags.EmberAudio)    => Audio,
                nameof(LogTags.EmberInput)    => Input,
                nameof(LogTags.Game)     => Game,
                _                         => default
            };
            return color != default;
        }
    }

    // ============================================================
    // 消息级别颜色
    // ============================================================

    /// <summary>
    /// 日志消息文字颜色。
    /// Warning/Error 用白色：Unity Console 自带黄/红背景。
    /// Init/Event 用彩色：普通 Log 无背景色，靠文字颜色区分。
    /// </summary>
    public static class LogColors
    {
        public static string Info     = "#FFFFFF";
        public static string Init     = "#66FF66";
        public static string Event    = "#CC88FF";
        public static string Cleanup  = "#999999";
        public static string Warning  = "#FFFFFF";
        public static string Error    = "#FFFFFF";
        public static string FileInfo = "#888888";
    }
}

// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

using System;
using UnityEngine;

namespace Ember.Basic
{
    /// <summary>
    /// 应用退出工具 —— 在编辑器中停止播放，在运行平台上退出应用。
    ///
    /// Unity 的 <see cref="Application.Quit"/> 在部分 Android 机型上不会立即杀死进程，
    /// App 可能挂在后台。此工具在 Android 上直接调用系统 Process.killProcess，
    /// 确保进程彻底终止。
    ///
    /// 用法：
    /// <code>
    /// ApplicationQuitUtil.Quit(); // 替代 Application.Quit()
    /// </code>
    /// </summary>
    public static class ApplicationQuitUtil
    {
        #region 内部参数

        private const string TAG = LogTags.BasicAppQuit;

        #endregion

        // ============================================================

        #region 外部方法

        /// <summary>
        /// 当前平台退出时实际会走的实现分支名，用于退出前的日志记录
        /// （排查「真机退出露场景」一类问题时，先确认走的是哪条退出路径）。
        /// </summary>
        /// <returns><c>EditorStopPlayMode</c> / <c>AndroidKillProcess</c> / <c>ApplicationQuit</c></returns>
        [NoGC]
        public static string DescribeBranch()
        {
#if UNITY_EDITOR
            return "EditorStopPlayMode";
#elif UNITY_ANDROID
            return "AndroidKillProcess";
#else
            return "ApplicationQuit";
#endif
        }

        /// <summary>
        /// 退出应用。
        /// 编辑器中停止 Play Mode；
        /// Android 上先通过 android.os.Process.killProcess 杀进程，
        /// 失败时回退到 <see cref="Application.Quit"/>；
        /// 其他平台直接调用 Application.Quit()。
        /// </summary>
        public static void Quit()
        {
#if UNITY_EDITOR
            EmberDebug.LogShutdown(TAG, "Quit: stopping editor Play Mode.");
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_ANDROID
            try
            {
                using (var process = new AndroidJavaClass("android.os.Process"))
                {
                    int pid = process.CallStatic<int>("myPid");
                    EmberDebug.LogShutdown(TAG, $"Quit: killProcess({pid}) requested.");
                    process.CallStatic("killProcess", pid);
                    // killProcess 后进程应立即终止，以下代码通常不会执行
                    Application.Quit();
                }
            }
            catch (Exception ex)
            {
                EmberDebug.LogError(TAG, $"Quit via killProcess failed: {ex.Message}");
                Application.Quit();
            }
#else
            EmberDebug.LogShutdown(TAG, "Quit: Application.Quit() requested.");
            Application.Quit();
#endif
        }

        #endregion
    }
}

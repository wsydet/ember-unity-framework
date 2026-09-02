// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Linq;

using Ember.Basic;

using UnityEditor;
using UnityEditor.Compilation;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// 跨脚本域重载保存“一键创建后打开 Prefab”的续接状态。
    /// 只有编译与 AssetDatabase 更新都结束且无编译错误时才打开 Prefab Mode。
    /// </summary>
    [InitializeOnLoad]
    public static class EUICreationCompilationContinuation
    {
        private const string TAG = LogTags.EmberUI;
        private const string PendingPathKey = "Ember.UI.Creation.PendingPrefabPath";
        private const string CompileStartedKey = "Ember.UI.Creation.CompileStarted";
        private const string CompileErrorKey = "Ember.UI.Creation.CompileError";
        private const string ScheduledTicksKey = "Ember.UI.Creation.ScheduledUtcTicks";

        static EUICreationCompilationContinuation()
        {
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            EditorApplication.update += TryComplete;
        }

        public static string PendingPrefabPath => SessionState.GetString(PendingPathKey, string.Empty);

        public static bool IsPending => !string.IsNullOrEmpty(PendingPrefabPath);

        /// <summary>必须在触发本批唯一一次 AssetDatabase.Refresh 之前调用。</summary>
        public static void Schedule(string prefabPath)
        {
            if (string.IsNullOrWhiteSpace(prefabPath)) return;
            SessionState.SetString(PendingPathKey, prefabPath);
            SessionState.SetBool(CompileStartedKey, false);
            SessionState.SetBool(CompileErrorKey, false);
            SessionState.SetString(ScheduledTicksKey, DateTime.UtcNow.Ticks.ToString());
        }

        public static void Cancel()
        {
            SessionState.EraseString(PendingPathKey);
            SessionState.EraseBool(CompileStartedKey);
            SessionState.EraseBool(CompileErrorKey);
            SessionState.EraseString(ScheduledTicksKey);
        }

        private static void OnCompilationStarted(object context)
        {
            if (!IsPending) return;
            SessionState.SetBool(CompileStartedKey, true);
            SessionState.SetBool(CompileErrorKey, false);
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath,
            CompilerMessage[] messages)
        {
            if (!IsPending || messages == null) return;
            if (messages.Any(message => message.type == CompilerMessageType.Error))
                SessionState.SetBool(CompileErrorKey, true);
        }

        private static void OnCompilationFinished(object context)
        {
            if (!IsPending) return;
            EditorApplication.delayCall += TryComplete;
        }

        private static void TryComplete()
        {
            var prefabPath = PendingPrefabPath;
            if (string.IsNullOrEmpty(prefabPath)
                || EditorApplication.isCompiling
                || EditorApplication.isUpdating)
                return;

            var compileStarted = SessionState.GetBool(CompileStartedKey, false);
            if (!compileStarted && GetElapsedSinceSchedule().TotalSeconds < 2d)
                return;

            var hadErrors = SessionState.GetBool(CompileErrorKey, false);
            Cancel();
            if (hadErrors)
            {
                EmberDebug.LogError(TAG,
                    $"UI 已创建，但脚本编译存在错误，已停止自动打开 Prefab：{prefabPath}");
                var failedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (failedPrefab)
                {
                    Selection.activeObject = failedPrefab;
                    EditorGUIUtility.PingObject(failedPrefab);
                }
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (!prefab)
            {
                EmberDebug.LogError(TAG, $"编译完成，但无法加载新 UI Prefab：{prefabPath}");
                return;
            }

            var assetBinding = prefab.GetComponent<EUIBinding>();
            if (assetBinding && assetBinding.GenerateCustomSettings
                && !TryCompleteCustomSettings(prefabPath, out var settingsError))
            {
                EmberDebug.LogError(TAG, settingsError);
                EditorUtility.DisplayDialog("自定义 Settings 续接失败",
                    settingsError + "\n\nPrefab 与已生成脚本已保留，请修复后重试。", "确定");
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                return;
            }

            AssetDatabase.OpenAsset(prefab);
            EmberDebug.Log(TAG, $"编译完成，已打开新 UI Prefab：{prefabPath}");
        }

        private static bool TryCompleteCustomSettings(string prefabPath, out string error)
        {
            error = null;
            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(prefabPath);
                if (!contents)
                {
                    error = $"无法加载新 UI Prefab 以创建自定义 Settings：{prefabPath}";
                    return false;
                }

                var binding = contents.GetComponent<EUIBinding>();
                if (!binding)
                {
                    error = $"新 UI Prefab 缺少 EUIBinding：{prefabPath}";
                    return false;
                }

                if (!EUIBindingCodeGenUtility.TryCreateCustomSettingsAfterCompile(binding,
                        out error))
                    return false;

                var savedPrefab = PrefabUtility.SaveAsPrefabAsset(contents, prefabPath,
                    out var saveSucceeded);
                if (!saveSucceeded || !savedPrefab)
                {
                    error = $"自定义 Settings 已创建，但 Prefab 保存失败：{prefabPath}";
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                error = $"续接自定义 Settings 时处理 Prefab 失败：{prefabPath}\n{exception.Message}";
                return false;
            }
            finally
            {
                if (contents)
                {
                    try
                    {
                        PrefabUtility.UnloadPrefabContents(contents);
                    }
                    catch (Exception exception)
                    {
                        EmberDebug.LogError(TAG,
                            $"卸载 Prefab 续接内容失败：{prefabPath}\n{exception}");
                    }
                }
            }
        }

        private static TimeSpan GetElapsedSinceSchedule()
        {
            var raw = SessionState.GetString(ScheduledTicksKey, string.Empty);
            return long.TryParse(raw, out var ticks)
                ? DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)
                : TimeSpan.MaxValue;
        }
    }
}

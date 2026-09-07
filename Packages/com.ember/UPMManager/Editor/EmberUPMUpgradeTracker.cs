// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Linq;

using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Ember.UPMManager.Editor
{
    internal enum EmberUPMUpgradePhase
    {
        None = 0,
        Preparing = 1,
        Resolving = 2,
        Registering = 3,
        Verifying = 4,
        Succeeded = 5,
        Failed = 6,
    }

    internal readonly struct EmberUPMUpgradeSnapshot
    {
        internal EmberUPMUpgradeSnapshot(EmberUPMUpgradePhase phase, string sourceVersion,
            string targetVersion, string error, DateTime startedUtc, DateTime nowUtc,
            string currentVersion, bool isCompiling, bool isUpdating)
        {
            Phase = phase;
            SourceVersion = sourceVersion ?? string.Empty;
            TargetVersion = targetVersion ?? string.Empty;
            Error = error ?? string.Empty;
            StartedUtc = startedUtc;
            CurrentVersion = currentVersion ?? string.Empty;
            IsCompiling = isCompiling;
            IsUpdating = isUpdating;

            var elapsed = startedUtc == DateTime.MinValue ? TimeSpan.Zero : nowUtc - startedUtc;
            Elapsed = elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
        }

        internal EmberUPMUpgradePhase Phase { get; }
        internal string SourceVersion { get; }
        internal string TargetVersion { get; }
        internal string Error { get; }
        internal DateTime StartedUtc { get; }
        internal TimeSpan Elapsed { get; }
        internal string CurrentVersion { get; }
        internal bool IsCompiling { get; }
        internal bool IsUpdating { get; }

        internal bool HasState => Phase != EmberUPMUpgradePhase.None;

        internal bool IsActive => Phase >= EmberUPMUpgradePhase.Preparing
                                  && Phase <= EmberUPMUpgradePhase.Verifying;

        internal bool IsSucceeded => Phase == EmberUPMUpgradePhase.Succeeded;
        internal bool IsFailed => Phase == EmberUPMUpgradePhase.Failed;

        internal bool IsSlow => IsActive
                                && Elapsed.TotalSeconds
                                >= EmberUPMUpgradeTracker.SlowThresholdSeconds;

        internal bool IsVerySlow => IsActive
                                    && Elapsed.TotalSeconds
                                    >= EmberUPMUpgradeTracker.VerySlowThresholdSeconds;

        internal int StageIndex => EmberUPMUpgradeTracker.GetStageIndex(Phase);
        internal float StageProgress => EmberUPMUpgradeTracker.GetStageProgress(Phase);
        internal string StageTitle => EmberUPMUpgradeTracker.GetPhaseTitle(Phase);

        internal string BuildDiagnostics()
        {
            return
                $"Ember UPM upgrade diagnostics\n" +
                $"phase: {Phase}\n" +
                $"source: {SourceVersion}\n" +
                $"target: {TargetVersion}\n" +
                $"current: {CurrentVersion}\n" +
                $"elapsed: {Elapsed:c}\n" +
                $"isCompiling: {IsCompiling}\n" +
                $"isUpdating: {IsUpdating}\n" +
                $"error: {Error}";
        }
    }

    /// <summary>
    /// 跟踪 com.ember 自身的 UPM 升级。
    /// SessionState 负责跨脚本域重载续接；PackageManager 事件负责在旧程序集卸载前、
    /// 新程序集载入后推进阶段，最终以实际安装版本作为成功依据。
    /// </summary>
    [InitializeOnLoad]
    internal static class EmberUPMUpgradeTracker
    {
        internal const double SlowThresholdSeconds = 90d;
        internal const double VerySlowThresholdSeconds = 300d;

        private const string PackageName = "com.ember";
        private const string KeyPrefix = "Ember.UPMManager.Upgrade.";
        private const string PhaseKey = KeyPrefix + "Phase";
        private const string SourceVersionKey = KeyPrefix + "SourceVersion";
        private const string TargetVersionKey = KeyPrefix + "TargetVersion";
        private const string StartedUtcTicksKey = KeyPrefix + "StartedUtcTicks";
        private const string ErrorKey = KeyPrefix + "Error";
        private const string ProgressIdKey = KeyPrefix + "ProgressId";

        private static AddRequest _request;
        private static double _lastProgressReportTime;

        static EmberUPMUpgradeTracker()
        {
            Events.registeringPackages -= OnRegisteringPackages;
            Events.registeringPackages += OnRegisteringPackages;
            Events.registeredPackages -= OnRegisteredPackages;
            Events.registeredPackages += OnRegisteredPackages;
            EditorApplication.update -= Tick;

            if (IsActive)
            {
                SubscribeTick();
                EnsureProgressItem();
                EditorApplication.delayCall += VerifyInstalledTarget;
            }
        }

        internal static bool IsActive => GetSnapshot().IsActive;

        internal static EmberUPMUpgradeSnapshot GetSnapshot()
        {
            var phase = (EmberUPMUpgradePhase)SessionState.GetInt(PhaseKey, 0);
            var startedUtc = ReadStartedUtc();
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForPackageName(PackageName);

            return new EmberUPMUpgradeSnapshot(
                phase,
                SessionState.GetString(SourceVersionKey, string.Empty),
                SessionState.GetString(TargetVersionKey, string.Empty),
                SessionState.GetString(ErrorKey, string.Empty),
                startedUtc,
                DateTime.UtcNow,
                packageInfo?.version,
                EditorApplication.isCompiling,
                EditorApplication.isUpdating);
        }

        internal static bool BeginUpgrade(string packageUrl, string sourceVersion,
            string targetVersion, out string error)
        {
            if (IsActive)
            {
                error = "已有框架升级正在执行。";
                return false;
            }

            ClearStoredState();
            SessionState.SetString(SourceVersionKey, sourceVersion ?? string.Empty);
            SessionState.SetString(TargetVersionKey, targetVersion ?? string.Empty);
            SessionState.SetString(StartedUtcTicksKey, DateTime.UtcNow.Ticks.ToString());
            SessionState.SetString(ErrorKey, string.Empty);
            SubscribeTick();
            SetPhase(EmberUPMUpgradePhase.Preparing);

            try
            {
                _request = Client.Add(packageUrl);
                SetPhase(EmberUPMUpgradePhase.Resolving);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = "无法启动 Package Manager 升级：" + exception.Message;
                Fail(error);
                return false;
            }
        }

        internal static void ClearTerminalState()
        {
            if (IsActive) return;
            ClearStoredState();
        }

        internal static int GetStageIndex(EmberUPMUpgradePhase phase)
        {
            switch (phase)
            {
                case EmberUPMUpgradePhase.Preparing: return 1;
                case EmberUPMUpgradePhase.Resolving: return 2;
                case EmberUPMUpgradePhase.Registering: return 3;
                case EmberUPMUpgradePhase.Verifying: return 4;
                case EmberUPMUpgradePhase.Succeeded: return 4;
                default: return 0;
            }
        }

        internal static float GetStageProgress(EmberUPMUpgradePhase phase)
        {
            switch (phase)
            {
                case EmberUPMUpgradePhase.Preparing: return 0.125f;
                case EmberUPMUpgradePhase.Resolving: return 0.375f;
                case EmberUPMUpgradePhase.Registering: return 0.625f;
                case EmberUPMUpgradePhase.Verifying: return 0.875f;
                case EmberUPMUpgradePhase.Succeeded: return 1f;
                default: return 0f;
            }
        }

        internal static string GetPhaseTitle(EmberUPMUpgradePhase phase)
        {
            switch (phase)
            {
                case EmberUPMUpgradePhase.Preparing: return "校验升级信息";
                case EmberUPMUpgradePhase.Resolving: return "下载与依赖解析";
                case EmberUPMUpgradePhase.Registering: return "注册新包与编译";
                case EmberUPMUpgradePhase.Verifying: return "验证安装版本";
                case EmberUPMUpgradePhase.Succeeded: return "升级完成";
                case EmberUPMUpgradePhase.Failed: return "升级失败";
                default: return string.Empty;
            }
        }

        internal static bool VersionsMatch(string currentVersion, string targetVersion)
        {
            return !string.IsNullOrWhiteSpace(currentVersion)
                   && !string.IsNullOrWhiteSpace(targetVersion)
                   && string.Equals(currentVersion.Trim(), targetVersion.Trim(),
                       StringComparison.OrdinalIgnoreCase);
        }

        internal static bool HasReachedSlowThreshold(DateTime startedUtc, DateTime nowUtc)
        {
            return startedUtc != DateTime.MinValue
                   && nowUtc >= startedUtc
                   && (nowUtc - startedUtc).TotalSeconds >= SlowThresholdSeconds;
        }

        private static void Tick()
        {
            var snapshot = GetSnapshot();
            if (!snapshot.IsActive) return;

            if (_request != null && _request.IsCompleted)
            {
                var completedRequest = _request;
                _request = null;

                if (completedRequest.Status == StatusCode.Failure)
                {
                    Fail(completedRequest.Error?.message ?? "Package Manager 返回未知错误。");
                    return;
                }

                if (completedRequest.Status == StatusCode.Success)
                {
                    var resultVersion = completedRequest.Result?.version;
                    if (!string.IsNullOrEmpty(resultVersion)
                        && !VersionsMatch(resultVersion, snapshot.TargetVersion))
                    {
                        Fail($"Package Manager 返回版本 v{resultVersion}，目标版本为 v{snapshot.TargetVersion}。");
                        return;
                    }

                    SetPhase(EmberUPMUpgradePhase.Verifying);
                }
            }

            VerifyInstalledTarget();
            ReportProgress(false);
        }

        private static void OnRegisteringPackages(PackageRegistrationEventArgs args)
        {
            if (!IsActive || !ContainsTargetPackage(args)) return;
            SetPhase(EmberUPMUpgradePhase.Registering);
        }

        private static void OnRegisteredPackages(PackageRegistrationEventArgs args)
        {
            if (!IsActive || !ContainsTargetPackage(args)) return;
            SetPhase(EmberUPMUpgradePhase.Verifying);
            EditorApplication.delayCall += VerifyInstalledTarget;
        }

        private static bool ContainsTargetPackage(PackageRegistrationEventArgs args)
        {
            if (args == null) return false;
            var targetVersion = SessionState.GetString(TargetVersionKey, string.Empty);
            return args.changedTo.Any(info => IsTargetPackage(info, targetVersion))
                   || args.added.Any(info => IsTargetPackage(info, targetVersion));
        }

        private static bool IsTargetPackage(
            UnityEditor.PackageManager.PackageInfo info, string targetVersion)
        {
            return info != null
                   && string.Equals(info.name, PackageName, StringComparison.Ordinal)
                   && VersionsMatch(info.version, targetVersion);
        }

        private static void VerifyInstalledTarget()
        {
            var snapshot = GetSnapshot();
            if (!snapshot.IsActive || snapshot.IsCompiling || snapshot.IsUpdating) return;
            if (snapshot.Phase == EmberUPMUpgradePhase.Resolving && _request != null) return;
            if (!VersionsMatch(snapshot.CurrentVersion, snapshot.TargetVersion)) return;

            Complete(snapshot.TargetVersion);
        }

        private static void Complete(string targetVersion)
        {
            _request = null;
            EditorApplication.update -= Tick;
            SessionState.SetString(ErrorKey, string.Empty);
            SessionState.SetInt(PhaseKey, (int)EmberUPMUpgradePhase.Succeeded);
            ReportProgress(true);
            FinishProgressItem(Progress.Status.Succeeded);
        }

        private static void Fail(string error)
        {
            _request = null;
            EditorApplication.update -= Tick;
            SessionState.SetString(ErrorKey, error ?? "未知错误");
            SessionState.SetInt(PhaseKey, (int)EmberUPMUpgradePhase.Failed);
            FinishProgressItem(Progress.Status.Failed);
        }

        private static void SetPhase(EmberUPMUpgradePhase phase)
        {
            SessionState.SetInt(PhaseKey, (int)phase);
            ReportProgress(true);
        }

        private static void SubscribeTick()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void EnsureProgressItem()
        {
            var progressId = SessionState.GetInt(ProgressIdKey, -1);
            if (progressId >= 0 && Progress.Exists(progressId)) return;

            var snapshot = GetSnapshot();
            progressId = Progress.Start(
                "Ember 框架升级",
                BuildProgressDescription(snapshot),
                Progress.Options.None,
                -1);
            Progress.SetTimeDisplayMode(progressId, Progress.TimeDisplayMode.ShowRunningTime);
            SessionState.SetInt(ProgressIdKey, progressId);
        }

        private static void ReportProgress(bool force)
        {
            var snapshot = GetSnapshot();
            if (!snapshot.IsActive && !snapshot.IsSucceeded) return;

            var now = EditorApplication.timeSinceStartup;
            if (!force && now - _lastProgressReportTime < 0.25d) return;
            _lastProgressReportTime = now;

            EnsureProgressItem();
            var progressId = SessionState.GetInt(ProgressIdKey, -1);
            if (progressId < 0 || !Progress.Exists(progressId)) return;
            Progress.Report(progressId, snapshot.StageProgress, BuildProgressDescription(snapshot));
        }

        private static string BuildProgressDescription(EmberUPMUpgradeSnapshot snapshot)
        {
            var route = string.IsNullOrEmpty(snapshot.SourceVersion)
                ? $"目标 v{snapshot.TargetVersion}"
                : $"v{snapshot.SourceVersion} → v{snapshot.TargetVersion}";
            return $"{route} · 阶段 {snapshot.StageIndex}/4：{snapshot.StageTitle}";
        }

        private static void FinishProgressItem(Progress.Status status)
        {
            var progressId = SessionState.GetInt(ProgressIdKey, -1);
            if (progressId >= 0 && Progress.Exists(progressId))
                Progress.Finish(progressId, status);
            SessionState.EraseInt(ProgressIdKey);
        }

        private static DateTime ReadStartedUtc()
        {
            var raw = SessionState.GetString(StartedUtcTicksKey, string.Empty);
            if (!long.TryParse(raw, out var ticks) || ticks <= 0) return DateTime.MinValue;

            try
            {
                return new DateTime(ticks, DateTimeKind.Utc);
            }
            catch (ArgumentOutOfRangeException)
            {
                return DateTime.MinValue;
            }
        }

        private static void ClearStoredState()
        {
            _request = null;
            EditorApplication.update -= Tick;
            var progressId = SessionState.GetInt(ProgressIdKey, -1);
            if (progressId >= 0 && Progress.Exists(progressId))
                Progress.Remove(progressId);

            SessionState.EraseInt(PhaseKey);
            SessionState.EraseString(SourceVersionKey);
            SessionState.EraseString(TargetVersionKey);
            SessionState.EraseString(StartedUtcTicksKey);
            SessionState.EraseString(ErrorKey);
            SessionState.EraseInt(ProgressIdKey);
        }
    }
}

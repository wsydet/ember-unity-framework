using System;
using System.IO;
using System.Reflection;
using System.Text;
using Ember.Basic;
using Ember.Core;
using Ember.Core.Editor;
using Game.Player;
using Game.Player.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.State.Editor
{
    /// <summary>冷启动及非Development构建验收入口。构建专用define不写入项目设置。</summary>
    public static class PlayerColdAcceptance
    {
        #region 内部参数
        private const string ROOT = "Assets/Game/Development/LevelAcceptance/";
        public static string BuildStatus { get; private set; } = "Idle";
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static void Require()
        {
            if (!EmberProjectSetup.IsTemplateActive("platformer-2d") ||
                typeof(PlayerModule).GetCustomAttribute<EmberModuleAttribute>()?.Enabled != true ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("仅活动platformer-2d编辑模式可执行。");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("请先保留未保存场景。");
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static string CreateBoot()
        {
            Require(); PlayerLevelAcceptance.CreateScenes();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ROOT + "ColdBoot.unity") != null) return "Retained ColdBoot";
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/Game/Scenes/FrameworkScene.unity");
                var runner = new GameObject("ColdAcceptance").AddComponent<PlayerColdAcceptanceRunner>();
                using var data = new SerializedObject(runner);
                data.FindProperty("_configA").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PlayerConfig>(ROOT + "ConfigA.asset");
                data.FindProperty("_configB").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PlayerConfig>(ROOT + "ConfigB.asset");
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene, ROOT + "ColdBoot.unity", true);
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
            return "Created ColdBoot";
        }
        public static void Launch(string root, string phase)
        {
            Require();
            if (!Path.IsPathFullyQualified(root) || (phase != "save" && phase != "resume")) throw new ArgumentException("需要绝对隔离库路径和save/resume。");
            SessionState.SetString("PlayerCold.Root", root); SessionState.SetString("PlayerCold.Phase", phase);
            EditorSceneManager.OpenScene(ROOT + "ColdBoot.unity");
            PlayerFormalAcceptance.Launch();
        }
        public static void Build(string outputDirectory) => Build(outputDirectory, false);

        /// <summary>cleanBuild=true时由Unity清理构建缓存并重新构建；不删除项目Library或资源。</summary>
        public static void Build(string outputDirectory, bool cleanBuild)
        {
            Require();
            if (BuildStatus == "Queued" || BuildStatus == "Building" || BuildPipeline.isBuildingPlayer ||
                EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("已有构建请求，或Unity正在编译/导入，请完成后再试。");
            if (!Path.IsPathFullyQualified(outputDirectory)) throw new ArgumentException("需要绝对构建目录。");
            var library = Path.GetFullPath("Library") + Path.DirectorySeparatorChar;
            if ((Path.GetFullPath(outputDirectory) + Path.DirectorySeparatorChar).StartsWith(library, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Unity禁止输出到Library，请使用.utmp或独立构建目录。");
            Directory.CreateDirectory(outputDirectory);
            BuildStatus = "Queued";
            File.WriteAllText(Path.Combine(outputDirectory, "build-report.txt"), BuildStatus);
            bool executed = false;
            void ExecuteOnUpdate()
            {
                if (executed) return;
                executed = true;
                EditorApplication.update -= ExecuteOnUpdate;
                var diagnostics = new StringBuilder();
                int asserts = 0, errors = 0;
                void Capture(string message, string stack, LogType type)
                {
                    if (type == LogType.Log) return;
                    diagnostics.AppendLine(type + ": " + message).AppendLine(stack);
                    if (type == LogType.Assert) asserts++;
                    if (type == LogType.Error || type == LogType.Exception) errors++;
                }
                Application.logMessageReceived += Capture;
                try
                {
                    Require();
                    if (EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
                        throw new InvalidOperationException("排队后Unity状态改变，构建未执行，请就绪后重试。");
                    BuildStatus = "Building";
                    File.WriteAllText(Path.Combine(outputDirectory, "build-entry-stack.txt"), Environment.StackTrace);
                    var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                    {
                        scenes = new[] { ROOT + "ColdBoot.unity", "Assets/Game/Scenes/MainScene.unity", "Assets/Game/Scenes/SettingsScene.unity", ROOT + "LevelA.unity", ROOT + "LevelB.unity" },
                        locationPathName = Path.Combine(outputDirectory, "EmberAcceptance.exe"), target = BuildTarget.StandaloneWindows64,
                        options = cleanBuild ? BuildOptions.CleanBuildCache : BuildOptions.None,
                        extraScriptingDefines = new[] { "EMBER_PLAYER_ACCEPTANCE" }
                    });
                    if (report == null) throw new InvalidOperationException("Unity未返回BuildReport，请检查Console中的构建错误。");
                    var summary = report.summary;
                    BuildStatus = summary.result + " errors=" + summary.totalErrors + " warnings=" + summary.totalWarnings + " guid=" + summary.guid + " options=" + summary.options;
                    var messages = new StringBuilder();
                    foreach (var step in report.steps)
                        foreach (var message in step.messages)
                            messages.AppendLine(step.name + " | " + message.type + " | " + message.content);
                    File.WriteAllText(Path.Combine(outputDirectory, "build-messages.txt"), messages.ToString());
                }
                catch (Exception error)
                {
                    BuildStatus = "FAILED " + error;
                    EmberDebug.LogError(LogTags.Game, BuildStatus);
                }
                finally
                {
                    Application.logMessageReceived -= Capture;
                    File.WriteAllText(Path.Combine(outputDirectory, "build-report.txt"), BuildStatus);
                    File.WriteAllText(Path.Combine(outputDirectory, "build-diagnostics.txt"),
                        "asserts=" + asserts + " errors=" + errors + Environment.NewLine + diagnostics);
                }
            }
            // MCP可能从UnitySynchronizationContext的Pump调用入口；先返回，再由真正的Editor update构建。
            // 不使用delayCall：后台编辑器中该回调可能迟迟不触发。执行前退订，防止BuildPlayer泵消息时重入。
            EditorApplication.update += ExecuteOnUpdate;
        }
        #endregion
    }
}

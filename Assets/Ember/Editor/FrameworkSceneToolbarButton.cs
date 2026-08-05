using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>
    /// Toolbar 按钮：
    /// - 左侧按钮：指示是否在 FrameworkScene 中，点击跳转
    /// - 右侧按钮：快速打开场景（主场景 + 叠加场景选择）
    /// </summary>
    [InitializeOnLoad]
    public static class FrameworkSceneToolbarButton
    {
        private const string ElementId = "Ember/FrameworkScene";
        private const string QuickOpenId = "Ember/QuickOpenScene";
        private const string ScenePath = "Assets/Game/Scenes/FrameworkScene.unity";

        static FrameworkSceneToolbarButton()
        {
            EditorSceneManager.activeSceneChangedInEditMode += (_, _) =>
            {
                MainToolbar.Refresh(ElementId);
                MainToolbar.Refresh(QuickOpenId);
            };
            EditorSceneManager.sceneOpened += (_, _) =>
            {
                MainToolbar.Refresh(ElementId);
                MainToolbar.Refresh(QuickOpenId);
            };
        }

        /// <summary>跳转 FrameworkScene 按钮（保留原功能）</summary>
        [MainToolbarElement(ElementId)]
        public static MainToolbarElement CreateFrameworkButton()
        {
            var inFramework = IsFrameworkSceneLoaded();
            var iconName = inFramework ? "SceneAsset Icon" : "d_console.warnicon";
            var icon = EditorGUIUtility.IconContent(iconName).image as Texture2D;

            return new MainToolbarButton(
                new MainToolbarContent
                {
                    image = icon,
                    tooltip = inFramework
                        ? "已在 FrameworkScene 中"
                        : "⚠ 当前不在 FrameworkScene！点击跳转",
                },
                () =>
                {
                    if (EditorApplication.isPlaying) return;
                    EditorApplication.delayCall += () =>
                    {
                        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            EditorSceneManager.OpenScene(ScenePath);
                    };
                });
        }

        /// <summary>快速打开场景按钮（新增）</summary>
        [MainToolbarElement(QuickOpenId)]
        public static MainToolbarElement CreateQuickOpenButton()
        {
            return new MainToolbarButton(
                new MainToolbarContent
                {
                    text = "快速打开场景",
                    tooltip = "选择主场景 + 叠加场景，一键打开 Framework + 目标场景",
                },
                () =>
                {
                    if (EditorApplication.isPlaying) return;
                    EmberSceneQuickOpener.Open();
                });
        }

        private static bool IsFrameworkSceneLoaded()
        {
            var active = EditorSceneManager.GetActiveScene();
            return active.path == ScenePath;
        }

        [MenuItem("Ember/跳转到 FrameworkScene %#F", false, 0)]
        private static void JumpToFrameworkScene()
        {
            if (EditorApplication.isPlaying) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }
    }
}

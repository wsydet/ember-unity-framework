using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>
    /// Toolbar 按钮：Play 按钮左侧，显示当前是否在 FrameworkScene 中。
    /// </summary>
    [InitializeOnLoad]
    public static class FrameworkSceneToolbarButton
    {
        private const string ElementId = "Ember/FrameworkScene";
        private const string ScenePath = "Assets/Game/Scenes/FrameworkScene.unity";

        static FrameworkSceneToolbarButton()
        {
            // 场景变化时刷新按钮状态
            EditorSceneManager.activeSceneChangedInEditMode += (_, _) =>
                MainToolbar.Refresh(ElementId);
            EditorSceneManager.sceneOpened += (_, _) =>
                MainToolbar.Refresh(ElementId);
        }

        [MainToolbarElement(ElementId, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement CreateButton()
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

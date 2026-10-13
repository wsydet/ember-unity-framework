using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Player.Editor
{
    /// <summary>本机 Gameplay 选择与试跑参数；运行、存取和暂停交给正常 Gameplay。</summary>
    public sealed class PlayerFeelLabWindow : EditorWindow
    {
        #region 编辑器面板参数
        [SerializeField] private PlayerScheme _scheme;
        [SerializeField] private Vector2 _position, _velocity;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public const string LAB_PATH = PlayerDevelopmentEnvironment.LAB_PATH;
        public static string Status => PlayerDevelopmentEnvironment.Status;
        public static string StorageRoot => PlayerDevelopmentEnvironment.StorageRoot;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void OnEnable()
        {
            if (_scheme != null) return;
            _scheme = PlayerDevelopmentEnvironment.SelectedScheme;
            _position = PlayerDevelopmentEnvironment.SelectedPosition;
            _velocity = PlayerDevelopmentEnvironment.SelectedVelocity;
        }
        private void OnInspectorUpdate() => Repaint();
        private void OnGUI()
        {
            if (PlayerEditorAccess.DrawUnavailable()) return;
            EditorGUILayout.HelpBox("此入口已整合到玩家开发工具；原草稿保留。", MessageType.Info);
            if (GUILayout.Button("打开手感白盒页")) { PlayerSchemeWindow.OpenTab(0, _scheme); Close(); }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void ShowWindow() => PlayerSchemeWindow.OpenTab(0);
        public static void OpenLab()
        {
            PlayerEditorAccess.Require();
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetSceneByPath(LAB_PATH);
            if (!scene.IsValid()) scene = EditorSceneManager.OpenScene(LAB_PATH, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
        }
        public static void Launch(PlayerFeelProfile feel, Vector2 position, Vector2 velocity, bool effectPreview = false)
        {
            PlayerEditorAccess.Require();
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先停止播放。");
            PlayerDevelopmentEnvironment.SetParameters(feel, position, velocity, effectPreview);
            PlayerDevelopmentEnvironment.UseWhitebox = true;
            PlayerDevelopmentEnvironment.Play();
        }
        public static void Restart(PlayerFeelProfile feel, Vector2 position, Vector2 velocity)
        {
            PlayerEditorAccess.Require();
            PlayerDevelopmentEnvironment.SetParameters(feel, position, velocity);
            PlayerDevelopmentEnvironment.UseWhitebox = true;
            PlayerDevelopmentEnvironment.Restart();
        }
        #endregion
    }
}

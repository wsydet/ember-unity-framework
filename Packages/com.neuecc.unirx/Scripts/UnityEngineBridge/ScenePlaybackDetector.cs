#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Callbacks;

namespace UniRx
{
    [InitializeOnLoad]
    public class ScenePlaybackDetector
    {
        // Read by worker threads too; UnityEditor APIs stay on the editor thread.
        private static volatile bool _isPlaying;

        public static bool IsPlaying
        {
            get { return _isPlaying; }
            set { _isPlaying = value; }
        }

        [DidReloadScripts]
        public static void OnDidReloadScripts()
        {
            IsPlaying = EditorApplication.isPlaying;
        }

        static ScenePlaybackDetector()
        {
            IsPlaying = EditorApplication.isPlaying;
#if UNITY_2017_2_OR_NEWER
            EditorApplication.playModeStateChanged += state =>
            {
                // Keep runtime dispatch active during OnApplicationQuit/OnDestroy.
                // SubsystemRegistration sets true before scene Awake without reload.
                if (state == PlayModeStateChange.EnteredPlayMode) IsPlaying = true;
                else if (state == PlayModeStateChange.EnteredEditMode) IsPlaying = false;
            };
#else
            EditorApplication.playmodeStateChanged += () =>
            {
                IsPlaying = EditorApplication.isPlaying;
            };
#endif
        }
    }
}
#endif

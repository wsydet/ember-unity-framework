#if UNITY_EDITOR
using System;

namespace Game.Player
{
    /// <summary>仅编辑器编译的环境装配接线，不加载场景或启动玩家会话。</summary>
    public static class PlayerLabEditorBridge
    {
        #region 内部参数
        public const string SceneName = "PlayerFeelLab";
        public static Action Prepare;
        public static Action ObserveStorage;
        public static string GameplayScenePath { get; set; }
        public static bool Active => !string.IsNullOrEmpty(GameplayScenePath);
        #endregion
    }
}
#endif

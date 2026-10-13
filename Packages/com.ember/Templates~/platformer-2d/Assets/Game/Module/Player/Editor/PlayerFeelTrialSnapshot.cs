using System;
using UnityEngine;

namespace Game.Player.Editor
{
    /// <summary>仅用于跨编辑器域重载传递一次已接受的试跑请求，不写资产或玩家存档。</summary>
    [Serializable]
    public sealed class PlayerFeelTrialSnapshot
    {
        #region 编辑器面板参数
        [SerializeField] private string _movement, _jump, _input;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static string Capture(PlayerFeelProfile feel)
        {
            _ = feel.Capture(Physics2D.gravity);
            return JsonUtility.ToJson(new PlayerFeelTrialSnapshot {
                _movement = JsonUtility.ToJson(feel.Movement),
                _jump = JsonUtility.ToJson(feel.Jump), _input = JsonUtility.ToJson(feel.Input) });
        }
        public static PlayerTrialStart Restore(string json, string scene, Vector2 position, Vector2 velocity)
        {
            var data = JsonUtility.FromJson<PlayerFeelTrialSnapshot>(json);
            if (data == null || string.IsNullOrEmpty(data._movement) || string.IsNullOrEmpty(data._jump) || string.IsNullOrEmpty(data._input))
                throw new InvalidOperationException("缺少已接受的试跑参数，请重新应用。");
            var profile = ScriptableObject.CreateInstance<PlayerFeelProfile>();
            var movement = ScriptableObject.CreateInstance<PlayerMoveFeel>();
            var jump = ScriptableObject.CreateInstance<PlayerJumpFeel>();
            var input = ScriptableObject.CreateInstance<PlayerInputFeel>();
            try
            {
                JsonUtility.FromJsonOverwrite(data._movement, movement);
                JsonUtility.FromJsonOverwrite(data._jump, jump);
                JsonUtility.FromJsonOverwrite(data._input, input);
                profile.Configure(movement, jump, input);
                return new PlayerTrialStart(scene, position, velocity, profile);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile); UnityEngine.Object.DestroyImmediate(movement);
                UnityEngine.Object.DestroyImmediate(jump); UnityEngine.Object.DestroyImmediate(input);
            }
        }
        #endregion
    }
}

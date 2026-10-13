using UnityEditor;

namespace Game.Player.Editor
{
    /// <summary>正式流程验收仅选择本次独立库，沿用相同环境装配及框架入口。</summary>
    public static class PlayerFormalAcceptance
    {
        public static string StorageRoot => PlayerDevelopmentEnvironment.StorageRoot;
        public static void Launch() => PlayerDevelopmentEnvironment.Play(formalAcceptance: true);
    }
}

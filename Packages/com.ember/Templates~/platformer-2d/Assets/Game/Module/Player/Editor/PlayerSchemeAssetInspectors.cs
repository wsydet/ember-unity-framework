using Game.Entity;
using UnityEditor;

namespace Game.Player.Editor
{
    internal class PlayerSchemeAssetInspector : UnityEditor.Editor
    { public override void OnInspectorGUI() => PlayerSchemeWindow.DrawAsset((UnityEngine.ScriptableObject)target); }
    [CustomEditor(typeof(PlayerFeelProfile))] internal sealed class PlayerFeelProfileInspector : PlayerSchemeAssetInspector { }
    [CustomEditor(typeof(PlayerMoveFeel))] internal sealed class PlayerMoveFeelInspector : PlayerSchemeAssetInspector { }
    [CustomEditor(typeof(PlayerJumpFeel))] internal sealed class PlayerJumpFeelInspector : PlayerSchemeAssetInspector { }
    [CustomEditor(typeof(PlayerInputFeel))] internal sealed class PlayerInputFeelInspector : PlayerSchemeAssetInspector { }
    [CustomEditor(typeof(PlayerPowerConfig))] internal sealed class PlayerPowerConfigInspector : PlayerSchemeAssetInspector { }
    [CustomEditor(typeof(PlayerAnimationProfile))] internal sealed class PlayerAnimationProfileInspector : PlayerSchemeAssetInspector { }
    [CustomEditor(typeof(PlayerActionEffects))] internal sealed class PlayerActionEffectsInspector : PlayerSchemeAssetInspector { }
    // Player 的配置入口不可阻止其他模板编辑通用 Entity 资产。
    internal class PlayerEntityAssetInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (PlayerEditorAccess.Available) PlayerSchemeWindow.DrawAsset((UnityEngine.ScriptableObject)target);
            else using (new EditorGUI.DisabledScope(PlayerSchemeOwnership.IsReadOnly(target))) DrawDefaultInspector();
        }
    }
    [CustomEditor(typeof(EntityAbilityDefinition), true)] internal sealed class PlayerAbilityAssetInspector : PlayerEntityAssetInspector { }
    [CustomEditor(typeof(EntitySkillDefinition), true)] internal sealed class PlayerSkillAssetInspector : PlayerEntityAssetInspector { }
    [CustomEditor(typeof(EntityEffectDefinition), true)] internal sealed class PlayerEffectAssetInspector : PlayerEntityAssetInspector { }
    [CustomEditor(typeof(PlayerSchemeLibrary))] internal sealed class PlayerSchemeLibraryInspector : UnityEditor.Editor
    { public override void OnInspectorGUI() { using (new EditorGUI.DisabledScope(true)) DrawDefaultInspector(); } }
}

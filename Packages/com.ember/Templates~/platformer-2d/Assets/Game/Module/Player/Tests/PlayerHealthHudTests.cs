#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        [TestCase(false, false)] [TestCase(true, false)] [TestCase(true, true)]
        public void HealthHudUsesGeneratedBindingAndFrozenIndependentSwitches(bool life, bool hud)
        {
            WithThreeHit((player, scheme) =>
            {
                Assert.That(player.Context.ShowHealth, Is.EqualTo(hud));
                PlayerDashEffectTestAssets.Set(scheme.Life, "_showHealth", !hud);
                Assert.That(player.Context.ShowHealth, Is.EqualTo(hud), "HUD使用冻结开关");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameResource/Resources/UI/Module/Player/Prefabs/EUIPlayerHealthItem.prefab");
                var root = Keep(UnityEngine.Object.Instantiate(prefab)); root.SetActive(false);
                var type = Type.GetType("Game.UI.EUIGamePlayPage, Assembly-CSharp", true);
                var page = Activator.CreateInstance(type);
                type.GetProperty("ControlMap").SetValue(page, new Dictionary<string, Component> { ["HealthRoot"] = root.GetComponent<RectTransform>() });
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var refresh = type.GetMethod("RefreshHealthItem", flags);
                try
                {
                    refresh.Invoke(page, new object[] { player });
                    Assert.That(root.activeSelf, Is.EqualTo(hud));
                    if (!hud) return;
                    var fill = (RectTransform)root.transform.Find("Fill");
                    player.Context.Health.Restore(4);
                    Assert.That(fill.anchorMax.x, Is.EqualTo(.4f).Within(.001f));
                    type.GetMethod("ReleaseHealthItem", flags).Invoke(page, new object[] { player });
                    Assert.That(root.activeSelf, Is.False);
                    player.Context.Health.Restore(7);
                    Assert.That(fill.anchorMax.x, Is.Zero, "释放后不保留旧生命订阅");
                    refresh.Invoke(page, new object[] { player });
                    Assert.That(root.activeSelf, Is.True);
                    Assert.That(fill.anchorMax.x, Is.EqualTo(.7f).Within(.001f));
                }
                finally { type.GetMethod("UnbindLifeNotice", flags).Invoke(page, null); }
            }, scheme =>
            {
                if (life) InstallLifeTest(scheme);
                PlayerDashEffectTestAssets.Set(scheme.Life, "_showHealth", hud);
            });
        }
    }
}
#endif

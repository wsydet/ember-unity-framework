using Ember.UIExtension;
using Ember.UIExtension.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Ember.UI.Tests
{
    public sealed class EUIBackdropPolicyEditTests
    {
        [TestCase(EUIMaskEffect.Inherit, EUIMaskEffect.Inherit, EUIMaskEffect.Blur)]
        [TestCase(EUIMaskEffect.Inherit, EUIMaskEffect.Blur, EUIMaskEffect.Blur)]
        [TestCase(EUIMaskEffect.Inherit, EUIMaskEffect.Solid, EUIMaskEffect.Solid)]
        [TestCase(EUIMaskEffect.Solid, EUIMaskEffect.Blur, EUIMaskEffect.Solid)]
        [TestCase(EUIMaskEffect.Blur, EUIMaskEffect.Solid, EUIMaskEffect.Blur)]
        public void EffectPriority_ExplicitPageThenGlobalThenBuiltIn(EUIMaskEffect page, EUIMaskEffect global, EUIMaskEffect expected)
        { Assert.That(EUIBackdrop.ResolveEffect(page, global), Is.EqualTo(expected)); }

        [Test]
        public void LegacyBinding_PreservesTintButInheritsNewBlurDefault()
        {
            var root = new GameObject("Legacy", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(EUIBinding));
            try
            {
                var binding = root.GetComponent<EUIBinding>();
                var so = new SerializedObject(binding);
                so.FindProperty("className").stringValue = "";
                so.FindProperty("maskColor").colorValue = new Color(.2f, .3f, .4f, 0);
                so.ApplyModifiedPropertiesWithoutUndo();
                var page = new EUIPage(root); EUIBindingBridge.Attach(page, binding);
                Assert.That(page.UseMask, Is.True);
                Assert.That(page.MaskColorOverride, Is.EqualTo(binding.MaskColor));
                Assert.That(EUIBackdrop.ResolveEffect(page.MaskEffectOverride, EUIMaskEffect.Blur), Is.EqualTo(EUIMaskEffect.Blur));
                so.FindProperty("backdropSchema").intValue = 1; so.ApplyModifiedPropertiesWithoutUndo();
                EUIBindingBridge.Attach(page, binding);
                Assert.That(page.MaskColorOverride.HasValue, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void Snapshot_RoundTripPreservesMaskPolicyAndNullablePatchDoesNotClearIt()
        {
            var root = new GameObject("Snapshot", typeof(RectTransform), typeof(EUIBinding));
            var settings = ScriptableObject.CreateInstance<EUIBackdropSettings>();
            try
            {
                var binding = root.GetComponent<EUIBinding>();
                var original = EUIBindingEditorUtility.GetBindingSnapshot(binding);
                original.MaskEffect = EUIMaskEffect.Solid; original.OverrideMaskTint = true;
                original.BackdropSchema = 1; original.HasBackdropSettingsOverride = true; original.BackdropSettingsOverride = settings;
                EUIBindingEditorUtility.ApplyBindingSnapshot(binding, original);
                var snapshot = EUIBindingEditorUtility.GetBindingSnapshot(binding);
                Assert.That(snapshot.MaskEffect, Is.EqualTo(EUIMaskEffect.Solid));
                Assert.That(snapshot.BackdropSettingsOverride, Is.SameAs(settings));
                snapshot.MaskEffect = null; snapshot.OverrideMaskTint = null; snapshot.HasBackdropSettingsOverride = null; snapshot.BackdropSchema = null;
                EUIBindingEditorUtility.ApplyBindingSnapshot(binding, snapshot);
                Assert.That(binding.MaskEffect, Is.EqualTo(EUIMaskEffect.Solid));
                Assert.That(binding.OverrideMaskTint, Is.True);
                Assert.That(binding.BackdropSettingsOverride, Is.SameAs(settings));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(settings); }
        }
    }
}

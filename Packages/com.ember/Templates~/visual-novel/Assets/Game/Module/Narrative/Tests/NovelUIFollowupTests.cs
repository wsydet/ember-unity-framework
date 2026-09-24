using System;
using System.Linq;
using System.Reflection;
using Ember.UI;
using Ember.UIExtension;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Narrative.Tests
{
    public sealed class NovelUIFollowupTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void SettingsAreAnOverlayAndValuesTrackTheirControls()
        {
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/GameResource/Resources/UI/Common/Prefabs/EUISettingPanel.prefab"));
            var page = new EUIPage(root);
            try
            {
                var binding = root.GetComponent<EUIBinding>();
                Assert.AreEqual(2, new SerializedObject(binding).FindProperty("pageType").intValue);
                Assert.IsFalse(root.transform.Find("NovelBackdrop").gameObject.activeSelf);
                Assert.IsFalse(root.transform.Find("NovelBackdropShade").gameObject.activeSelf);
                EUIBindingBridge.Attach(page, binding);
                page.Logic.OnInit();
                var module = new Game.NovelSave.NovelSaveModule();
                var account = new Game.NovelSave.NovelAccountData { ShakePreference = NovelEffectPreference.Reduced, FlashPreference = NovelEffectPreference.Off };
                typeof(Game.NovelSave.NovelSaveModule).GetProperty(nameof(module.Account)).SetValue(module, account);
                page.Logic.GetType().GetField("_novelPreferences", PrivateInstance).SetValue(page.Logic, module);
                var speed = (Slider)page.Logic.ControlMap["NovelTextSpeed"];
                var interval = (Slider)page.Logic.ControlMap["NovelAutoInterval"];
                var music = (Slider)page.Logic.ControlMap["NovelBgmVolume"];
                speed.SetValueWithoutNotify(45); interval.SetValueWithoutNotify(2.5f); music.SetValueWithoutNotify(.37f);
                page.Logic.GetType().GetMethod("RefreshNovelPreferences", PrivateInstance).Invoke(page.Logic, null);
                Assert.AreEqual("45 字/秒", ((TMP_Text)page.Logic.ControlMap["NovelTextSpeedValue"]).text);
                Assert.AreEqual("2.5 秒", ((TMP_Text)page.Logic.ControlMap["NovelAutoIntervalValue"]).text);
                Assert.AreEqual(.37f.ToString("P0"), ((TMP_Text)page.Logic.ControlMap["NovelBgmVolumeValue"]).text);
                Assert.AreEqual("减弱", ((TMP_Text)page.Logic.ControlMap["NovelShakePreferenceValue"]).text);
                Assert.AreEqual("关闭", ((TMP_Text)page.Logic.ControlMap["NovelFlashPreferenceValue"]).text);
                music.value = .62f;
                Assert.AreEqual(.62f.ToString("P0"), ((TMP_Text)page.Logic.ControlMap["NovelBgmVolumeValue"]).text);
            }
            finally { page.Logic?.OnDispose(); UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void NewGameShowsOwnedProgressWhileQuickRestoreStaysCovered(bool showProgress)
        {
            var prefab = AssetDatabase.FindAssets("EUILoadingPanel t:Prefab").Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>).First();
            var root = UnityEngine.Object.Instantiate(prefab); var page = new EUIPage(root);
            try
            {
                EUIBindingBridge.Attach(page, root.GetComponent<EUIBinding>());
                page.Logic.OnInit();
                var owner = page.Logic.GetType().Assembly.GetType("Game.UI.NovelSaveUI");
                var type = owner.GetNestedType("RestoreLoadingRequest", BindingFlags.NonPublic);
                var request = Activator.CreateInstance(type, PrivateInstance, null, new object[] { showProgress }, null);
                type.GetProperty("Progress", PrivateInstance).SetValue(request, .45f);
                page.Logic.OnOpen(request);
                var logic = page.Logic.GetType();
                logic.GetMethod("HideProgressVisuals", PrivateInstance).Invoke(page.Logic, null);
                logic.GetMethod("RefreshNovelLoadingProgress", PrivateInstance).Invoke(page.Logic, null);
                var bar = (Image)page.Logic.ControlMap["Img_ProgressBar"];
                var number = (TMP_Text)page.Logic.ControlMap["Txt_ProgressNum"];
                Assert.AreEqual(showProgress, bar.gameObject.activeSelf);
                Assert.AreEqual(showProgress, number.gameObject.activeSelf);
                if (showProgress) { Assert.AreEqual(.45f, bar.fillAmount); Assert.AreEqual("45%", number.text); }
                page.Logic.OnClose();
            }
            finally { page.Logic?.OnDispose(); UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}

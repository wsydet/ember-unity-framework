using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ember.Basic;
using Ember.Core;
using Ember.UIExtension;
using Ember.UIExtension.Editor;
using Game.Narrative;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Editor
{
    /// <summary>One-time E2 upgrade through the same binding/validation/generation APIs as the EUI center.</summary>
    internal static class NovelScreenSettingsMigration
    {
        #region 内部参数
        private const string PREFAB = "Assets/GameResource/Resources/UI/Common/Prefabs/EUISettingPanel.prefab";
        private const string BINDING = "Assets/Game/UI/Runtime/SettingScene/EUISettingPage.Binding.cs";
        [Serializable] private sealed class Identity { public string templateId; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        [InitializeOnLoadMethod]
        private static void Schedule() => EditorApplication.delayCall += Upgrade;
        private static void Upgrade()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                typeof(NarrativeModule).GetCustomAttribute<EmberModuleAttribute>()?.Enabled != true) return;
            const string editing = "Assets/Editor/EmberEditingTemplate.json";
            if (!File.Exists(editing) || JsonUtility.FromJson<Identity>(File.ReadAllText(editing))?.templateId != "visual-novel") return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB);
            if (!prefab) return;
            bool hasBinding = prefab.GetComponent<EUIBinding>().Bindings.Any(e => e.Name == "NovelShakePreference");
            if (hasBinding && File.Exists(BINDING) && File.ReadAllText(BINDING).Contains("NovelFlashPreference = ControlMap")) return;
            GameObject root = null;
            try
            {
                if (!hasBinding)
                {
                    const string backup = ".utmp/visual-novel-e2/settings-before";
                    Directory.CreateDirectory(backup);
                    foreach (var path in new[] { PREFAB, PREFAB + ".meta", BINDING })
                        if (File.Exists(path) && !File.Exists(backup + "/" + Path.GetFileName(path))) File.Copy(path, backup + "/" + Path.GetFileName(path));
                    root = PrefabUtility.LoadPrefabContents(PREFAB);
                    var binding = root.GetComponent<EUIBinding>();
                    var entries = EUIBindingEditorUtility.GetBindingSnapshot(binding).Entries;
                    var prototype = binding.Bindings.Single(e => e.Name == "NovelVoiceVolume").GameObject;
                    var panel = prototype.transform.parent;
                    var label = panel.Find("NovelVoiceVolumeLabel");
                    foreach (var name in new[] { "NovelShakePreference", "NovelFlashPreference" })
                    {
                        var go = UnityEngine.Object.Instantiate(prototype, panel); go.name = name;
                        var slider = go.GetComponent<Slider>(); slider.minValue = 0; slider.maxValue = 2; slider.wholeNumbers = true; slider.SetValueWithoutNotify(0);
                        var text = UnityEngine.Object.Instantiate(label.gameObject, panel); text.name = name + "Label";
                        text.GetComponent<TMP_Text>().text = name == "NovelShakePreference" ? "震动强度" : "闪光强度";
                        var source = entries.Single(e => e.Name == "NovelVoiceVolume");
                        entries.Add(new EUIBindingEntrySnapshot { Name = name, WidgetType = source.WidgetType,
                            ClassName = source.ClassName, GameObjectPath = source.GameObjectPath.Replace("NovelVoiceVolume", name) });
                    }
                    string[] rows = { "NovelTextSpeed", "NovelAutoInterval", "NovelBgmVolume", "NovelSfxVolume", "NovelVoiceVolume", "NovelShakePreference", "NovelFlashPreference" };
                    for (int i = 0; i < rows.Length; i++)
                    {
                        float y = .78f - i * .085f;
                        Place((RectTransform)panel.Find(rows[i]), new Vector2(.44f, y), new Vector2(.92f, y + .05f));
                        Place((RectTransform)panel.Find(rows[i] + "Label"), new Vector2(.06f, y), new Vector2(.44f, y + .06f));
                    }
                    Place((RectTransform)panel.Find("NovelPreferenceStatus"), new Vector2(.06f, .02f), new Vector2(.94f, .25f));
                    EUIBindingEditorUtility.SetBindings(binding, entries);
                    if (EUIBindingEditorUtility.ValidateBinding(binding).HasError) throw new InvalidOperationException("E2 设置绑定未通过 UI 中心校验");
                    PrefabUtility.SaveAsPrefabAsset(root, PREFAB, out bool saved);
                    if (!saved) throw new IOException("E2 设置 Prefab 保存失败");
                    PrefabUtility.UnloadPrefabContents(root); root = null;
                }
                var savedBinding = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB).GetComponent<EUIBinding>();
                if (!EUIBindingCodeGenUtility.TryRegenerateCode(savedBinding, out string error)) throw new InvalidOperationException(error);
                AssetDatabase.SaveAssetIfDirty(savedBinding);
                AssetDatabase.Refresh();
                EmberDebug.Log("Narrative.E2", "震动/闪光偏好已通过 EUI 开发中心 API 更新。仍需完成 Unity 编译和画面验收。");
            }
            catch (Exception ex) { EmberDebug.LogError("Narrative.E2", "设置升级未完成，请在 EUI 开发中心检查：" + ex); }
            finally { if (root) PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void Place(RectTransform rect, Vector2 min, Vector2 max)
        { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        #endregion
    }
}

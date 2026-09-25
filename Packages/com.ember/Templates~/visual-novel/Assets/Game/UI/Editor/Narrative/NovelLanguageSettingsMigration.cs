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
    /// <summary>
    /// 给设置面板补一行「语言」选择（简体中文 / 繁體中文 / 日本語 / English）。
    ///
    /// <para>一次性、可重入的模板升级：缺 <c>LanguageZhHans</c> 绑定才动手，做完就跳过
    /// （与 <see cref="NovelScreenSettingsMigration"/> 同一套路）。这样已部署旧模板的工程
    /// 加载时也能自动补上这一行。</para>
    ///
    /// <para><b>绑定走精确追加，不用 SetBindings：</b>那个 API 会整表重建（<c>ClearArray</c>），
    /// 而它的快照类型没有 <c>IsFramework</c> 字段，会把现有 17 条的框架保护位全抹掉。</para>
    ///
    /// <para><b>语言清单来自配表：</b>按钮文案用各语言自己的名字（不挂 Key，切语言时不该被翻译），
    /// 高亮由 <c>EUISettingPage.Language.cs</c> 按当前语言刷新；默认中文是现状——
    /// 未设置语言时解析器回退源语言列 <c>zh_Hans</c>。</para>
    /// </summary>
    internal static class NovelLanguageSettingsMigration
    {
        #region 内部参数

        private const string TAG = "Novel.Language";
        private const string PREFAB = "Assets/GameResource/Resources/UI/Common/Prefabs/EUISettingPanel.prefab";
        private const string BINDING = "Assets/Game/UI/Runtime/SettingScene/EUISettingPage.Binding.cs";
        private const string FIRST_BINDING = "LanguageZhHans";

        /// <summary>按钮注册名 → 语言标识（与 novel_languages 的 id 一致）。</summary>
        private static readonly (string Name, string Language, string Label)[] Buttons =
        {
            ("LanguageZhHans", "zh_Hans", "简体中文"),
            ("LanguageZhHant", "zh_Hant", "繁體中文"),
            ("LanguageJa", "ja", "日本語"),
            ("LanguageEn", "en", "English")
        };

        [Serializable] private sealed class Identity { public string templateId; }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        [InitializeOnLoadMethod]
        private static void Schedule() => EditorApplication.delayCall += Upgrade;

        private static void Place(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>按同名追加一条绑定；已存在同名条目则跳过（不碰其它条目）。</summary>
        private static bool AppendBinding(EUIBinding binding, string name, GameObject target, string className)
        {
            using var serialized = new SerializedObject(binding);
            var entries = serialized.FindProperty("bindings");
            for (int i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("Name").stringValue == name) return false;

            int index = entries.arraySize;
            entries.InsertArrayElementAtIndex(index);
            var entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("Name").stringValue = name;
            entry.FindPropertyRelative("GameObject").objectReferenceValue = target;
            entry.FindPropertyRelative("Type").intValue = (int)EUIBinding.WidgetTypes.Extension;
            entry.FindPropertyRelative("ClassName").stringValue = className;
            entry.FindPropertyRelative("IsFramework").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(binding);
            return true;
        }

        private static void Upgrade()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (typeof(NarrativeModule).GetCustomAttribute<EmberModuleAttribute>()?.Enabled != true) return;

            const string editing = "Assets/Editor/EmberEditingTemplate.json";
            if (!File.Exists(editing) || JsonUtility.FromJson<Identity>(File.ReadAllText(editing))?.templateId != "visual-novel") return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB);
            if (!prefab) return;
            var existing = prefab.GetComponent<EUIBinding>();
            if (existing == null || existing.Bindings.Any(e => e.Name == FIRST_BINDING)) return;

            GameObject root = null;
            try
            {
                const string backup = ".utmp/visual-novel-language/settings-before";
                Directory.CreateDirectory(backup);
                foreach (var path in new[] { PREFAB, PREFAB + ".meta", BINDING })
                    if (File.Exists(path) && !File.Exists(backup + "/" + Path.GetFileName(path)))
                        File.Copy(path, backup + "/" + Path.GetFileName(path));

                root = PrefabUtility.LoadPrefabContents(PREFAB);
                var binding = root.GetComponent<EUIBinding>();
                var panel = root.transform.Find("Animator/EUISafeArea/NovelPreferences");
                if (panel == null) throw new InvalidOperationException("找不到 NovelPreferences 节点");

                // 标签：复用现有行标签（已是 TMPEx），改 Key 与原文即可。
                var labelPrototype = panel.Find("NovelFlashPreferenceLabel");
                var languageLabel = UnityEngine.Object.Instantiate(labelPrototype.gameObject, panel);
                languageLabel.name = "LanguageLabel";
                var labelText = languageLabel.GetComponent<TMP_Text>();
                labelText.text = "语言";
                if (labelText is TMPEx labelEx) labelEx.Key = "ui.setting.Language.Label";
                Place((RectTransform)languageLabel.transform, new Vector2(0f, .124f), new Vector2(.30f, .204f));

                // 按钮：拿面板上唯一的按钮当原型（ColorTint + 选中色），文案用语言自己的名字，并清掉继承来的 Key。
                var buttonPrototype = root.transform.Find("Animator/EUISafeArea/Center/PanelBg/m_Btn_Close");
                if (buttonPrototype == null) throw new InvalidOperationException("找不到按钮原型 m_Btn_Close");
                const float left = .32f, right = 1f, gap = .02f;
                float cell = (right - left - gap * (Buttons.Length - 1)) / Buttons.Length;
                for (int i = 0; i < Buttons.Length; i++)
                {
                    float min = left + i * (cell + gap);
                    var go = UnityEngine.Object.Instantiate(buttonPrototype.gameObject, panel);
                    go.name = Buttons[i].Name;
                    Place((RectTransform)go.transform, new Vector2(min, .124f), new Vector2(min + cell, .194f));

                    var buttonText = go.GetComponentInChildren<TMP_Text>(true);
                    if (buttonText is TMPEx buttonEx) buttonEx.Key = string.Empty;
                    if (buttonText != null) buttonText.text = Buttons[i].Label;

                    if (AppendBinding(binding, Buttons[i].Name, go, "UnityEngine.UI.Button") == false)
                        EmberDebug.LogWarning(TAG, "绑定 " + Buttons[i].Name + " 已存在，未重复追加。");
                }

                // 让出位置：状态行原本顶到 0.140，会压住新行（新行 0.124–0.204）。
                var status = panel.Find("NovelPreferenceStatus") as RectTransform;
                if (status != null && status.anchorMax.y > .10f)
                {
                    var max = status.anchorMax; max.y = .10f; status.anchorMax = max;
                }

                if (EUIBindingEditorUtility.ValidateBinding(binding).HasError)
                    throw new InvalidOperationException("语言行绑定未通过 UI 中心校验");

                PrefabUtility.SaveAsPrefabAsset(root, PREFAB, out bool saved);
                if (!saved) throw new IOException("设置 Prefab 保存失败");
                PrefabUtility.UnloadPrefabContents(root);
                root = null;

                AssetDatabase.ImportAsset(PREFAB, ImportAssetOptions.ForceUpdate);
                var savedBinding = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB).GetComponent<EUIBinding>();
                if (!EUIBindingCodeGenUtility.TryRegenerateCode(savedBinding, out string error))
                    throw new InvalidOperationException(error);
                AssetDatabase.SaveAssetIfDirty(savedBinding);

                EmberDebug.LogInit(TAG, "语言选择行已加到设置面板，并重新生成了 EUISettingPage.Binding.cs；请编译后验收画面。");
            }
            catch (Exception ex)
            {
                EmberDebug.LogError(TAG, "语言行升级未完成，请在 EUI 开发中心检查：" + ex);
            }
            finally
            {
                if (root) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        #endregion
    }
}

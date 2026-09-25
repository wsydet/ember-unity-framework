using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ember.Basic;
using Ember.Core;
using Ember.UIExtension;
using Ember.UIExtension.Editor;
using Game.Narrative;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Editor
{
    /// <summary>Repairs the invisible choice background override on the center-authored reader prefab.</summary>
    internal static class NovelChoiceBackgroundMigration
    {
        #region 内部参数
        private const string PREFAB = NovelGameplayLayoutWindow.PrefabPath;
        [Serializable] private sealed class Identity { public string templateId; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        [InitializeOnLoadMethod]
        private static void Schedule()
        {
            EditorApplication.delayCall += Upgrade;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Upgrade;
        }

        private static void Upgrade()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                typeof(NarrativeModule).GetCustomAttribute<EmberModuleAttribute>()?.Enabled != true) return;
            const string editing = "Assets/Editor/EmberEditingTemplate.json";
            GameObject root = null;
            try
            {
                if (!File.Exists(editing) || JsonUtility.FromJson<Identity>(File.ReadAllText(editing))?.templateId != "visual-novel") return;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB);
                if (!prefab) return;
                // Only repair the old fully transparent override; preserve later authored styles.
                if (ChoiceBackground(prefab).color.a > 0) return;
                const string backup = ".utmp/visual-novel-choice-background/before";
                Directory.CreateDirectory(backup);
                foreach (var path in new[] { PREFAB, PREFAB + ".meta" })
                    if (File.Exists(path) && !File.Exists(backup + "/" + Path.GetFileName(path)))
                        File.Copy(path, backup + "/" + Path.GetFileName(path));

                root = PrefabUtility.LoadPrefabContents(PREFAB);
                var background = ChoiceBackground(root);
                background.color = new Color(.035f, .045f, .06f, .55f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(background);
                // Binding paths and generated code remain unchanged; validate using the EUI center API.
                var binding = root.GetComponent<EUIBinding>();
                var item = binding.Bindings.Single(e => e.Name == "ChoiceTemplate").GameObject.GetComponent<EUIBinding>();
                if (EUIBindingEditorUtility.ValidateBinding(binding).HasError ||
                    EUIBindingEditorUtility.ValidateBinding(item).HasError)
                    throw new InvalidOperationException("选项背景修复未通过 EUI 开发中心绑定校验");
                PrefabUtility.SaveAsPrefabAsset(root, PREFAB, out bool saved);
                if (!saved) throw new IOException("选项背景 Prefab 保存失败");
                EmberDebug.Log("Narrative.UI", "选项已恢复独立半透明深色底框，请在 LastLight 分支选择处检查画面。");
            }
            catch (Exception ex) { EmberDebug.LogError("Narrative.UI", "选项背景升级未完成：" + ex); }
            finally { if (root) PrefabUtility.UnloadPrefabContents(root); }
        }

        private static Image ChoiceBackground(GameObject root)
        {
            var template = root.GetComponent<EUIBinding>().Bindings.Single(e => e.Name == "ChoiceTemplate").GameObject;
            var select = template.GetComponent<EUIBinding>().Bindings.Single(e => e.Name == "Select").GameObject.GetComponent<Button>();
            if (!(select.targetGraphic is Image image)) throw new InvalidOperationException("选项 Select 缺少 Image 背景");
            return image;
        }
        #endregion
    }
}

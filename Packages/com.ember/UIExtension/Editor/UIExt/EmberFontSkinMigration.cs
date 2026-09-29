using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension.Editor
{
    /// <summary>原位切换脚本，保留组件 FileID、输入框/Binding 等序列化引用。</summary>
    public static class EmberFontSkinMigration
    {
        #region 内部方法
        private static TMPEx ConvertUnreferencedLegacy(Text old, GameObject root)
        {
            // Text 与 TMPEx 不是继承关系：不能复制 Unity 内部对象/Prefab 身份字段。
            // 若存在外部组件引用，交由调用方先调整引用类型，不能静默丢失接线。
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (!component || component == old) continue;
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == old)
                        throw new InvalidOperationException(old.name + " 仍被 " + component.GetType().Name + "." + property.propertyPath + " 引用，请先迁移该引用类型。");
            }
            var go = old.gameObject;
            string content = old.text;
            Color color = old.color;
            int size = old.fontSize;
            int min = old.resizeTextMinSize, max = old.resizeTextMaxSize;
            bool auto = old.resizeTextForBestFit, rich = old.supportRichText;
            bool raycast = old.raycastTarget, mask = old.maskable, enabled = old.enabled;
            int anchor = (int)old.alignment;
            FontStyle style = old.fontStyle;
            UnityEngine.Object.DestroyImmediate(old);
            var text = go.AddComponent<TMPEx>();
            text.SetSource(content);
            text.color = color;
            text.fontSize = size;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
            text.enableAutoSizing = auto;
            text.richText = rich;
            text.raycastTarget = raycast;
            text.maskable = mask;
            text.enabled = enabled;
            var alignments = new[] { TextAlignmentOptions.TopLeft, TextAlignmentOptions.Top, TextAlignmentOptions.TopRight,
                TextAlignmentOptions.Left, TextAlignmentOptions.Center, TextAlignmentOptions.Right,
                TextAlignmentOptions.BottomLeft, TextAlignmentOptions.Bottom, TextAlignmentOptions.BottomRight };
            text.alignment = alignments[anchor];
            text.fontStyle = style == FontStyle.Bold ? FontStyles.Bold : style == FontStyle.Italic ? FontStyles.Italic :
                style == FontStyle.BoldAndItalic ? FontStyles.Bold | FontStyles.Italic : FontStyles.Normal;
            return text;
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public static string LastBackupPath { get; private set; }

        public static int ConvertHierarchy(GameObject root, bool enrollExisting = false)
        {
            var catalog = EmberFontSkins.Catalog;
            if (!catalog || !catalog.Validate(out var error))
                throw new InvalidOperationException("请先配置完整字体皮肤库。");
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Packages/com.ember/UIExtension/Runtime/UIExt/TMPEx.cs");
            if (!script) throw new InvalidOperationException("TMPEx 脚本未找到。");
            int count = 0;
            var originalTexts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            int defaultSlot = catalog.FindSkin(catalog.DefaultSkinId).Fonts[0].SlotId;
            foreach (var legacy in root.GetComponentsInChildren<Text>(true))
            {
                var extended = ConvertUnreferencedLegacy(legacy, root);
                foreach (var skin in catalog.Skins) extended.SetFontForSkin(skin.Id, skin.Fonts[0].SlotId);
                extended.SetFontSkin(0, defaultSlot);
                EditorUtility.SetDirty(extended);
                count++;
            }
            foreach (var text in originalTexts)
            {
                var go = text.gameObject;
                bool converted = text.GetType() == typeof(TextMeshProUGUI);
                if (converted)
                {
                    var serialized = new SerializedObject(text);
                    serialized.FindProperty("m_Script").objectReferenceValue = script;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                var extended = go.GetComponent<TMPEx>();
                if (!extended || (!converted && !enrollExisting)) continue;
                // 新增的组件字段由 Unity 初始化；原位转换不改变原有 TMP 字段及组件引用。
                foreach (var skin in catalog.Skins) extended.SetFontForSkin(skin.Id, skin.Fonts[0].SlotId);
                extended.SetFontSkin(0, defaultSlot);
                EditorUtility.SetDirty(extended);
                count++;
            }
            return count;
        }

        public static int ConvertPrefabs(string folder, bool enrollExisting = false)
        {
            folder = folder.Replace('\\', '/').TrimEnd('/');
            string full = Path.GetFullPath(folder);
            string assets = Path.GetFullPath("Assets") + Path.DirectorySeparatorChar;
            if (!folder.StartsWith("Assets/", StringComparison.Ordinal) || !full.StartsWith(assets, StringComparison.OrdinalIgnoreCase) || !AssetDatabase.IsValidFolder(folder))
                throw new ArgumentException("仅允许转换 Assets 下的项目 UI 目录。");
            int count = 0;
            LastBackupPath = Path.GetFullPath("Library/EmberFontSkinBackups/" + Guid.NewGuid().ToString("N"));
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int changed = ConvertHierarchy(root, enrollExisting);
                    if (changed == 0) continue;
                    string backup = Path.Combine(LastBackupPath, path);
                    Directory.CreateDirectory(Path.GetDirectoryName(backup));
                    File.Copy(path, backup);
                    File.Copy(path + ".meta", backup + ".meta");
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    count += changed;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var binding in saved.GetComponentsInChildren<EUIBinding>(true))
                    if (!EUIBindingCodeGenUtility.TryRegenerateCode(binding, out var error))
                        throw new InvalidOperationException(path + ": " + error);
            }
            AssetDatabase.SaveAssets();
            return count;
        }
        #endregion
    }
}

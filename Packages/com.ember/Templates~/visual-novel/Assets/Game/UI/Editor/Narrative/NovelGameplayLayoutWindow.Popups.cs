using System;
using System.Collections.Generic;
using System.IO;
using Ember.UIExtension;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Editor
{
    public sealed partial class NovelGameplayLayoutWindow
    {
        #region 内部参数
        private static readonly Dictionary<string, string> PopupPaths = new()
        {
            { "FontPanel", "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelFontPage.prefab" },
            { "HistoryPanel", "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelHistoryPage.prefab" }
        };
        private readonly Dictionary<string, GameObject> _popupContents = new();
        private readonly Dictionary<string, GameObject> _popupPreviews = new();
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private static void LoadPopups(Dictionary<string, GameObject> popups)
        {
            foreach (var pair in PopupPaths) popups.Add(pair.Key, PrefabUtility.LoadPrefabContents(pair.Value));
        }

        private static void ReleasePopups(Dictionary<string, GameObject> popups)
        {
            foreach (var root in popups.Values)
            {
                if (!root) continue;
                foreach (var component in root.GetComponentsInChildren<Component>(true)) if (component) Undo.ClearUndo(component);
                PrefabUtility.UnloadPrefabContents(root);
            }
            popups.Clear();
        }

        private static void AddPopupTargets(Dictionary<string, GameObject> popups, Dictionary<string, RectTransform> targets)
        {
            foreach (var pair in popups)
            {
                var binding = pair.Value.GetComponent<EUIBinding>();
                var close = Array.Find(binding.Bindings, b => b.Name == "Close").GameObject;
                targets[pair.Key] = (RectTransform)close.transform.parent;
                AddDescendants(pair.Value.transform, "popup/" + pair.Key, targets);
            }
        }

        private void BuildPopupPreviews(Transform parent)
        {
            _popupPreviews.Clear();
            foreach (var pair in _popupContents)
            {
                var root = Instantiate(pair.Value, parent, false);
                DestroyImmediate(root.GetComponent<GraphicRaycaster>());
                DestroyImmediate(root.GetComponent<CanvasScaler>());
                DestroyImmediate(root.GetComponent<Canvas>());
                _popupPreviews.Add(pair.Key, root);
            }
            AddPopupTargets(_popupPreviews, _previewTargets);
        }

        private void SyncPopupPreviews()
        {
            foreach (var pair in _popupPreviews)
            {
                var rect = (RectTransform)pair.Value.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.sizeDelta = Size; rect.localPosition = new Vector3(0, 0, -2);
                rect.localScale = Vector3.one;
                pair.Value.SetActive(Keys[_selected] == pair.Key);
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            }
        }

        private void SavePopups(string folder)
        {
            foreach (var pair in _popupContents)
            {
                string path = PopupPaths[pair.Key];
                File.Copy(path, Path.Combine(folder, Path.GetFileName(path)));
                File.Copy(path + ".meta", Path.Combine(folder, Path.GetFileName(path) + ".meta"));
                PrefabUtility.SaveAsPrefabAsset(pair.Value, path, out bool success);
                if (!success) throw new IOException("弹窗保存失败：" + path);
            }
        }
        #endregion
    }
}

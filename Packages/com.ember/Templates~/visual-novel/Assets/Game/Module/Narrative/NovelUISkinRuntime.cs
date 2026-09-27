using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Ember.Basic;
using Ember.UI;
using Ember.UIExtension;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Narrative
{
    /// <summary>皮肤编辑器与正式页面共用的图片应用路径。不修改布局、绑定或源 Prefab。</summary>
    public static class NovelUISkinRuntime
    {
        #region 内部参数
        private sealed class Baseline
        {
            internal readonly Dictionary<Image, NovelUISkinImage> Images = new();
            internal readonly Dictionary<Image, Sprite> Overrides = new();
        }
        private static readonly ConditionalWeakTable<GameObject, Baseline> Baselines = new();
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static void Set(Image image, NovelUISkinImage value)
        {
            image.overrideSprite = null;
            image.sprite = value.Sprite; image.color = value.Color; image.type = value.Type;
            image.preserveAspect = value.PreserveAspect;
            image.pixelsPerUnitMultiplier = Mathf.Max(.01f, value.PixelsPerUnitMultiplier);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [HasGC]
        public static Image Resolve(GameObject root, string control, string node)
        {
            if (!root) return null;
            Transform anchor = root.transform;
            if (!string.IsNullOrEmpty(control))
            {
                anchor = null;
                var binding = root.GetComponent<EUIBinding>();
                if (binding && binding.Bindings != null)
                    foreach (var entry in binding.Bindings)
                        if (entry.Name == control && entry.GameObject) { anchor = entry.GameObject.transform; break; }
                if (!anchor) anchor = root.transform.Find(control);
            }
            if (!anchor) return null;
            if (!string.IsNullOrEmpty(node)) anchor = anchor.Find(node);
            return anchor ? anchor.GetComponent<Image>() : null;
        }

        [HasGC]
        public static NovelUISkinImage Capture(Image image) => new()
        {
            Sprite = image.sprite, Color = image.color, Type = image.type,
            PreserveAspect = image.preserveAspect, PixelsPerUnitMultiplier = image.pixelsPerUnitMultiplier
        };

        /// <summary>首次调用记录基础外观；每次应用先恢复上次覆盖过的图片，支持 A → B → 基础。</summary>
        [HasGC]
        public static int Apply(GameObject root, string page, NovelUISkin skin)
        {
            if (!root) return 0;
            var baseline = Baselines.GetValue(root, _ => new Baseline());
            foreach (var pair in baseline.Images)
                if (pair.Key) { Set(pair.Key, pair.Value); pair.Key.overrideSprite = baseline.Overrides[pair.Key]; }
            int count = 0;
            if (!skin) return count;
            foreach (var entry in skin.Images)
            {
                if (entry == null || entry.Page != page) continue;
                var image = Resolve(root, entry.Control, entry.Node);
                if (!image) continue;
                if (!baseline.Images.ContainsKey(image))
                { baseline.Images.Add(image, Capture(image)); baseline.Overrides.Add(image, image.overrideSprite); }
                Set(image, entry); count++;
            }
            return count;
        }

        /// <summary>返回 true 表示剧情明确选择了编辑器皮肤或基础外观，调用方应跳过旧配表皮肤。</summary>
        [HasGC]
        public static bool ApplyCurrent(EUILogic logic, string page)
        {
            var library = Resources.Load<NarrativeLibrarySO>(NarrativeLibrarySO.RESOURCE_PATH);
            var assignments = Resources.Load<NovelUISkinAssignments>(NovelUISkinAssignments.RESOURCE_PATH);
            if (!library || !library.Current || !assignments) return false;
            foreach (var entry in assignments.Stories)
            {
                if (entry == null || entry.Story != library.Current) continue;
                var root = logic?.Page?.GameObject ?? logic?.Item?.GameObject;
                Apply(root, page, entry.Skin);
                return true;
            }
            return false;
        }
        #endregion
    }
}

using System.Collections.Generic;
using Ember.Basic;
using TMPro;
using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>全局字体皮肤。未启用的 TMPEx 在下次 OnEnable 时读取最新皮肤。</summary>
    public static class EmberFontSkins
    {
        #region 内部参数
        public const string ProjectResourcePath = "Config/FontSkins/EmberFontSkins";
        public const string DefaultResourcePath = "Ember/FontSkins/DefaultFontSkins";
        private static readonly HashSet<TMPEx> Targets = new HashSet<TMPEx>();
        private static EmberFontSkinCatalog _catalog;
        private static int _activeSkinId;
        public static EmberFontSkinCatalog Catalog => _catalog ? _catalog :
            (_catalog = Resources.Load<EmberFontSkinCatalog>(ProjectResourcePath) ??
                        Resources.Load<EmberFontSkinCatalog>(DefaultResourcePath));
        public static int ActiveSkinId => _activeSkinId > 0 ? _activeSkinId : Catalog ? Catalog.DefaultSkinId : 0;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            _catalog = null;
            _activeSkinId = 0;
            // 不清空注册：关闭 Domain/Scene Reload 时现有对象不会重新 OnEnable。
            Targets.RemoveWhere(t => !t);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyInitialSkin() => RefreshAll();
        internal static void Register(TMPEx text) => Targets.Add(text);
        internal static void Unregister(TMPEx text) => Targets.Remove(text);
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        /// <summary>切换跟随全局的文本；固定皮肤的文本保留其选择。不会改写项目配置或玩家存档。</summary>
        [HasGC]
        public static bool SetSkin(int skinId)
        {
            if (!Catalog || Catalog.FindSkin(skinId) == null) return false;
            _activeSkinId = skinId;
            RefreshAll();
            return true;
        }

        [HasGC]
        public static void Reload()
        {
            _catalog = null;
            _activeSkinId = 0;
            RefreshAll();
        }

        [HasGC]
        public static void RefreshAll()
        {
            Targets.RemoveWhere(t => !t);
            foreach (var target in Targets) target.ApplyFontSkin();
        }

        [HasGC]
        public static bool TryGetFont(int skinId, int slotId, out TMP_FontAsset font)
        {
            font = null;
            return skinId >= 0 && Catalog && Catalog.TryGetFont(skinId == 0 ? ActiveSkinId : skinId, slotId, out font);
        }
        #endregion
    }
}

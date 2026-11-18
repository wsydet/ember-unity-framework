using Ember.Basic;
using UnityEngine;

namespace Game.UI
{
    public static partial class EUIAppearanceBootstrap
    {
        private static EUIAppearanceConfig _config;
        private static bool _loaded;
        public static EUIAppearanceConfig Config
        {
            get { if (!_loaded) { _loaded=true; _config=Resources.Load<EUIAppearanceConfig>("Config/UIFoundation/EUIAppearance"); } return _config; }
        }
        [NoGC]
        public static void ReloadConfiguration() { _loaded=false; _config=null; }
    }
}

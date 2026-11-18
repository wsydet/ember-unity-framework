using Ember.Basic;
using UnityEngine;

namespace Game.UI
{
    public enum EUICommonSkin { Legacy, SoftGlass }
    public enum EUIGmPresentation { Legacy, SoftGlass }

    [CreateAssetMenu(menuName = "Ember/UI/Appearance")]
    public sealed class EUIAppearanceConfig : EmberBaseSO
    {
        [SerializeField] private EUICommonSkin _commonSkin;
        [SerializeField] private EUIGmPresentation _gmPresentation;
        public EUICommonSkin CommonSkin => _commonSkin;
        public EUIGmPresentation GmPresentation => _gmPresentation;
    }
}

using Ember.Basic;
using Ember.UI;
namespace Game.UI
{
    public static class EUICommonDialogs
    {
        [HasGC]
        public static void ShowAlert(EUIDialogRequest request)
        {
            bool glass=EUIAppearanceBootstrap.Config && EUIAppearanceBootstrap.Config.CommonSkin==EUICommonSkin.SoftGlass;
            EUIManager.Instance.ShowPopup(glass?GamePages.EUISoftGlassAlertPage:GamePages.EUIAlertPage,request);
        }
        [HasGC]
        public static void ShowConfirm(EUIDialogRequest request)
        {
            bool glass=EUIAppearanceBootstrap.Config && EUIAppearanceBootstrap.Config.CommonSkin==EUICommonSkin.SoftGlass;
            EUIManager.Instance.ShowPopup(glass?GamePages.EUISoftGlassConfirmPage:GamePages.EUIConfirmPage,request);
        }
    }
}

namespace Game.UI
{
    public partial class EUISoftGlassPanelItem
    {
        public UnityEngine.RectTransform ContentRoot => Content;
        public void SetTitle(string title) => Header.text = title ?? string.Empty;
        public void SetStyle(Ember.UIExtension.EUIGlassStyle style) { if(style) style.ApplyTo(Surface, Ember.UIExtension.EUIGlassRadius.Card); }
    }
}
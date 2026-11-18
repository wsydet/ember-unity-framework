using System;
namespace Game.UI
{
    public partial class EUISoftGlassGMEntryItem
    {
        public event Action Activated;
        public override void OnInit() { base.OnInit(); Floating.Activated += OnActivated; }
        public override void OnDispose() { Floating.Activated -= OnActivated; Activated = null; base.OnDispose(); }
        private void OnActivated() => Activated?.Invoke();
        public void SetVisible(bool visible) => Floating.gameObject.SetActive(visible);
    }
}
namespace Game.UI
{
    public partial class EUISoftGlassGMActionItem
    {
        private string _key;
        public override void OnInit() { base.OnInit(); Button.onClick.AddListener(Execute); }
        public override void OnDispose() { Button.onClick.RemoveListener(Execute); _key=null; base.OnDispose(); }
        public void BindAction(string key) { _key=key; Refresh(); }
        public void Refresh()
        {
            if(!GMActionRegistry.TryFind(_key,out var action)) { Button.interactable=false; return; }
            Button.interactable=action.IsAvailable?.Invoke()!=false;
            bool selected=action.ReadToggle?.Invoke()==true;
            Label.text=action.Title + (action.ReadToggle!=null ? (selected ? "\nON" : "\nOFF") : "");
            Icon.text=string.Empty;Icon.gameObject.SetActive(false);Glyph.SetKey(action.Icon);
            bool light=selected||_key=="ember.gm.reset-time";
            Surface.color=action.Dangerous?new UnityEngine.Color(.72f,.1f,.17f,.36f):light?new UnityEngine.Color(.91f,.96f,1,.92f):new UnityEngine.Color(.38f,.62f,.92f,.2f);
            Glyph.color=light?new UnityEngine.Color(.08f,.34f,.86f):UnityEngine.Color.white;
        }
        private void Execute() { GMActionRegistry.TryExecute(_key); Refresh(); }
    }
}

namespace Game.UI
{
    public partial class EUISoftGlassGMActionItem
    {
        private string _key;
        private GMActionGesture _gesture;
        private bool _shortcut;
        public override void OnInit()
        {
            base.OnInit(); _gesture = Button.GetComponent<GMActionGesture>();
            _gesture.ShortPressed = Execute; _gesture.FavoriteRequested = Favorite;
            GMFavorites.Changed += Refresh;
        }
        public override void OnDispose()
        {
            GMFavorites.Changed -= Refresh;
            if (_gesture) { _gesture.ShortPressed = null; _gesture.FavoriteRequested = null; }
            _key=null; base.OnDispose();
        }
        private void Favorite() { if (!_shortcut) GMFavorites.Toggle(_key); }
        public void BindAction(string key) { _key=key; Refresh(); }
        public void Refresh()
        {
            if(!GMActionRegistry.TryFind(_key,out var action)) { Button.interactable=false; return; }
            Button.interactable=action.IsAvailable?.Invoke()!=false;
            bool selected=action.ReadToggle?.Invoke()==true;
            Label.text=action.Title + (action.ReadToggle!=null ? (selected ? "\nON" : "\nOFF") : "");
            var favorite = Button.transform.Find("Favorite"); if (favorite) favorite.gameObject.SetActive(!_shortcut && GMFavorites.Contains(_key));
            Icon.text=string.Empty;Icon.gameObject.SetActive(false);Glyph.SetKey(action.Icon);
            bool light=selected||_key=="ember.gm.reset-time";
            Surface.color=action.Dangerous?new UnityEngine.Color(.45f,.03f,.07f,.8f):light?new UnityEngine.Color(.91f,.96f,1,.92f):new UnityEngine.Color(.025f,.07f,.11f,.82f);
            Label.color=light?new UnityEngine.Color(.035f,.1f,.18f):UnityEngine.Color.white;
            Glyph.color=light?new UnityEngine.Color(.08f,.34f,.86f):UnityEngine.Color.white;
        }
        public void SetShortcut(float visibleDiameter, float hitDiameter)
        {
            _shortcut = true;
            var root = (UnityEngine.RectTransform)Item.GameObject.transform; root.sizeDelta = UnityEngine.Vector2.one * hitDiameter;
            var button = (UnityEngine.RectTransform)Button.transform; button.anchorMin = button.anchorMax = button.pivot = UnityEngine.Vector2.one * .5f;
            button.anchoredPosition = UnityEngine.Vector2.zero; button.sizeDelta = UnityEngine.Vector2.one * visibleDiameter;
            Label.gameObject.SetActive(false); Glyph.rectTransform.anchorMin=Glyph.rectTransform.anchorMax=Glyph.rectTransform.pivot=UnityEngine.Vector2.one*.5f;Glyph.rectTransform.anchoredPosition=UnityEngine.Vector2.zero;Glyph.rectTransform.sizeDelta = UnityEngine.Vector2.one * visibleDiameter * .48f;
            Surface.Radius=visibleDiameter*.5f;
            var hit = Item.GameObject.GetComponent<GMCircleHit>(); if (hit) hit.Configure(Button, hitDiameter, visibleDiameter);
            Refresh();
        }
        private void Execute() { if (Button.interactable) GMActionRegistry.TryExecute(_key); Refresh(); }
    }
}

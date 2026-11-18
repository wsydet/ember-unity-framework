using System;
using Ember.Basic;
using Ember.Resource;
using Ember.UI;
using Ember.UIExtension;
using UnityEngine;
namespace Game.UI
{
    public partial class EUISoftGlassExtensionExamplePage
    {
        private EUIItem _dynamic;
        private EmberAssetHandle<GameObject> _prefab;
        private Action<GameObject> _loaded;
        private int _generation;
        private SoftGlassBackdropAttachment _backgroundAttachment;
        private UnityEngine.UI.Image[] _backgrounds;
        private SoftGlassDemoContrast _contrast;
        public override bool NeedUpdate=>true;
        public override void OnUpdate()
        {
            base.OnUpdate();_backgroundAttachment?.SyncVisibility();SoftGlassDemoActions.AnimateBackground(_backgrounds);
            _contrast?.Refresh();
        }
        public override void OnInit()
        {
            base.OnInit();_backgroundAttachment=new SoftGlassBackdropAttachment(Page,Backdrop);_contrast=new SoftGlassDemoContrast(Page.Transform);_backgrounds=Backdrop.GetComponentsInChildren<UnityEngine.UI.Image>(true);ShowPopup.onClick.AddListener(Confirm);StaticExample.Clicked+=Confirm;BackgroundMode.onClick.AddListener(ChangeBackground);
            var style=Resources.Load<EUIGlassStyle>("Config/UIFoundation/SoftGlassAccent");if(style)StaticExample.SetStyle(style);SoftGlassDemoActions.AnimateBackground(_backgrounds);
        }
        public override void OnOpen(object param)
        {
            base.OnOpen(param);int generation=++_generation;
            _prefab=EmberResourceManager.Instance.LoadAssetHandle<GameObject>("UI/Module/UIFoundation/Prefabs/EUISoftGlassExtensionExampleItem");
            _loaded=asset=>{
                if(generation!=_generation||!asset)return;
                var instance=UnityEngine.Object.Instantiate(asset,DynamicHost,false);
                if(!EUIItemFactory.TryCreate(instance,out _dynamic,out var error)){UnityEngine.Object.Destroy(instance);EmberDebug.LogError(LogTags.Game,error);return;}
                _dynamic.Show();var logic=(EUISoftGlassExtensionExampleItem)_dynamic.Logic;logic.SetTitle("Dynamic instance");logic.Clicked+=Confirm;
                _contrast?.Invalidate();
                Page.GameObject.GetComponent<EUIBackdropGroup>()?.RefreshSurfaces();
            };_prefab.Completed+=_loaded;
        }
        public override void OnDispose()
        {
            _generation++;
            _backgroundAttachment?.Dispose();_backgroundAttachment=null;_contrast=null;
            ShowPopup.onClick.RemoveListener(Confirm);StaticExample.Clicked-=Confirm;BackgroundMode.onClick.RemoveListener(ChangeBackground);
            if(_dynamic!=null){((EUISoftGlassExtensionExampleItem)_dynamic.Logic).Clicked-=Confirm;var go=_dynamic.GameObject;_dynamic.Dispose();if(go)UnityEngine.Object.Destroy(go);_dynamic=null;}
            if(_prefab!=null){_prefab.Completed-=_loaded;_prefab.Dispose();_prefab=null;}_loaded=null;base.OnDispose();
        }
        private void Confirm()=>EUICommonDialogs.ShowConfirm(new EUIDialogRequest{Title="Your extension",Message="The static Item belongs to the parent Binding. The dynamic Item belongs to EUIItemFactory and this page. Both use the same public library.",AcceptText="Understood"});
        public override void OnShow(){base.OnShow();_backgroundAttachment?.SetVisible(true);}
        public override void OnHide(){_backgroundAttachment?.SetVisible(false);base.OnHide();}
        private void ChangeBackground(){SoftGlassDemoActions.CycleBackground();SoftGlassDemoActions.AnimateBackground(_backgrounds);Title.color=SoftGlassDemoActions.BackgroundMode==1?new Color(.04f,.06f,.1f):new Color(.94f,.96f,1);}
    }
}

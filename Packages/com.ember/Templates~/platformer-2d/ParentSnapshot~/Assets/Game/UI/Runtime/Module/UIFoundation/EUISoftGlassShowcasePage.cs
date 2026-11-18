using System.Collections.Generic;
using Ember.UI;
using Ember.UIExtension;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public partial class EUISoftGlassShowcasePage
    {
        #region 内部参数
        private readonly List<EUIItem> _items=new();
        private Image[] _backgrounds;
        private SoftGlassBackdropAttachment _backgroundAttachment;
        private SoftGlassDemoContrast _contrast;
        public override bool NeedUpdate=>true;
        #endregion
        #region 生命周期
        public override void OnInit()
        {
            base.OnInit();ShowPopup.onClick.AddListener(OpenDialog);BackgroundMode.onClick.AddListener(SoftGlassDemoActions.CycleBackground);StaticExample.Clicked+=OpenDialog;
            _backgroundAttachment=new SoftGlassBackdropAttachment(Page,Backdrop);
            _contrast=new SoftGlassDemoContrast(Page.Transform);
            _backgrounds=Backdrop.GetComponentsInChildren<Image>(true);
            for(int i=0;i<Content.childCount;i++)
            {
                var child=Content.GetChild(i).gameObject;
                if(!child.GetComponent<EUIBinding>()||EUIInstanceOwnership.IsOwned(child))continue;
                if(EUIItemFactory.TryCreate(child,out var item,out _)){item.Show();_items.Add(item);}
            }
            var disabled=Content.GetComponentInChildren<EUIButtonEx>(true);
            foreach(var button in Content.GetComponentsInChildren<EUIButtonEx>(true))
                if(button.name.Contains("Secondary"))button.SetEnabled(false);
            foreach(var toggle in Content.GetComponentsInChildren<EUIToggleEx>(true))toggle.isOn=true;
            var inputs=Content.GetComponentsInChildren<TMPro.TMP_InputField>(true);
            if(inputs.Length>0)inputs[0].text="Glass, with clear text";
            if(inputs.Length>1)inputs[1].text="Search";
            Page.GameObject.GetComponent<EUIBackdropGroup>()?.RefreshSurfaces();
        }
        public override void OnUpdate()
        {
            base.OnUpdate();_backgroundAttachment?.SyncVisibility();SoftGlassDemoActions.AnimateBackground(_backgrounds);
            _contrast?.Refresh();
        }
        public override void OnDispose()
        {
            _backgroundAttachment?.Dispose();_backgroundAttachment=null;
            ShowPopup.onClick.RemoveListener(OpenDialog);BackgroundMode.onClick.RemoveListener(SoftGlassDemoActions.CycleBackground);StaticExample.Clicked-=OpenDialog;
            foreach(var item in _items)item.Dispose();_items.Clear();_backgrounds=null;_contrast=null;base.OnDispose();
        }
        public override void OnShow() { base.OnShow();_backgroundAttachment?.SetVisible(true); }
        public override void OnHide() { _backgroundAttachment?.SetVisible(false);base.OnHide(); }
        #endregion
        #region 内部方法
        private void OpenDialog()=>EUICommonDialogs.ShowConfirm(new EUIDialogRequest {Title="Keep the moment",Message="The scene and lower UI are blurred together. This title, message and actions remain clear.",AcceptText="Keep it",CancelText="Go back"});
        #endregion
    }
}

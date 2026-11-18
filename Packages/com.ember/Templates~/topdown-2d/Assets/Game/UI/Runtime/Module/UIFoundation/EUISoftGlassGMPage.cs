using Ember.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.UI
{
    public partial class EUISoftGlassGMPage
    {
        #region 内部参数
        private float _nextRefresh;
        public override bool NeedUpdate=>true;
        public bool IsExpanded=>CenterHost.gameObject.activeSelf;
        #endregion
        #region 生命周期
        public override void OnInit()
        {
            base.OnInit();Entry.Activated+=ToggleCenter;Center.CloseRequested+=Collapse;
            SetExpanded(false);
        }
        public override void OnOpen(object param) { base.OnOpen(param); if(param is bool expanded)SetExpanded(expanded); }
        public override void OnUpdate()
        {
            base.OnUpdate();
            if(IsExpanded&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)SetExpanded(false);
            if(IsExpanded&&UnityEngine.Time.unscaledTime>=_nextRefresh){_nextRefresh=UnityEngine.Time.unscaledTime+.2f;Center.RefreshInfo();}
        }
        public override void OnDispose()
        {
            Entry.Activated-=ToggleCenter;Center.CloseRequested-=Collapse;Center.ClosePanel();base.OnDispose();
        }
        #endregion
        #region 内部方法
        private void ToggleCenter()=>SetExpanded(!IsExpanded);
        private void Collapse()=>SetExpanded(false);
        #endregion
        #region 外部方法
        public void SetExpanded(bool expanded)
        {
            if(expanded){CenterHost.gameObject.SetActive(true);Center.OpenPanel();Entry.SetVisible(false);}
            else{Center.ClosePanel();CenterHost.gameObject.SetActive(false);Entry.SetVisible(true);}
            EUIBackdrop.MarkOrderDirty();
            Page.GameObject.GetComponent<EUIBackdropGroup>()?.RefreshSurfaces();
        }
        #endregion
    }
}

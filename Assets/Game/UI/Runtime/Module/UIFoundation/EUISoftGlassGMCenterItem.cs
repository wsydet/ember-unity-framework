using System;
using System.Collections.Generic;
using Ember.Basic;
using Ember.Core;
using Ember.Resource;
using Ember.UI;
using Ember.UIExtension;
using UnityEngine;

namespace Game.UI
{
    public partial class EUISoftGlassGMCenterItem
    {
        #region 内部参数
        private readonly List<EUIItem> _actions=new();
        private EmberAssetHandle<GameObject> _prefab;
        private Action<GameObject> _loaded;
        private bool _panelOpen;
        private int _generation,_registryVersion;
        public event Action CloseRequested;
        public bool IsPanelOpen=>_panelOpen;
        #endregion
        #region 生命周期
        public override void OnInit() { base.OnInit(); Close.onClick.AddListener(RequestClose); }
        public override void OnDispose() { Close.onClick.RemoveListener(RequestClose); ClosePanel(); CloseRequested=null; base.OnDispose(); }
        #endregion
        #region 内部方法
        private void RequestClose()=>CloseRequested?.Invoke();
        private void ClearItems()
        {
            foreach(var item in _actions) {var go=item.GameObject;item.Dispose();if(go)UnityEngine.Object.Destroy(go);}
            _actions.Clear();
        }
        private void BuildActions(GameObject prefab)
        {
            if(!_panelOpen||!prefab)return;ClearItems();
            foreach(var action in GMActionRegistry.Snapshot())
            {
                var instance=UnityEngine.Object.Instantiate(prefab,Actions,false);
                if(!EUIItemFactory.TryCreate(instance,out var item,out var error)) {UnityEngine.Object.Destroy(instance);EmberDebug.LogError(LogTags.Game,error);continue;}
                ((EUISoftGlassGMActionItem)item.Logic).BindAction(action.Key);item.Show();_actions.Add(item);
            }
            _registryVersion=GMActionRegistry.Version;
            Page?.GameObject.GetComponent<EUIBackdropGroup>()?.RefreshSurfaces();
            Actions.GetComponentInParent<EUIBackdropGroup>()?.RefreshSurfaces();
            Actions.GetComponentInParent<GMControlLayout>()?.RefreshActions();
        }
        #endregion
        #region 外部方法
        public void OpenPanel()
        {
            if(_panelOpen)return;_panelOpen=true;
            int generation=++_generation;
            _prefab=EmberResourceManager.Instance.LoadAssetHandle<GameObject>("UI/Module/UIFoundation/Prefabs/EUISoftGlassGMActionItem");
            _loaded=asset=>{if(generation==_generation&&_panelOpen)BuildActions(asset);};
            _prefab.Completed+=_loaded;RefreshInfo();Scroll.verticalNormalizedPosition=1;
        }
        public void ClosePanel()
        {
            if(!_panelOpen&&_prefab==null)return;_panelOpen=false;_generation++;
            if(_prefab!=null){_prefab.Completed-=_loaded;_prefab.Dispose();_prefab=null;}_loaded=null;
            ClearItems();
        }
        public void RefreshInfo()
        {
            if(!_panelOpen)return;State.Refresh();Time.RefreshValue();
            if(_registryVersion!=GMActionRegistry.Version&&_prefab?.Asset)BuildActions(_prefab.Asset);
            foreach(var action in _actions)((EUISoftGlassGMActionItem)action.Logic).Refresh();
            var notice=Close.transform.parent.Find("Notice")?.GetComponent<TMPro.TMP_Text>();
            if(notice)notice.text=GMFavorites.Notice??(_actions.Count==0?"项目尚未注册扩展操作":"");
        }
        #endregion
    }
}

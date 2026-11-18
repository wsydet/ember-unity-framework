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
        private IDisposable _coreActions;
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
        private sealed class CoreActions : IGMActionProvider
        {
            public IReadOnlyList<GMActionDescriptor> Actions {get;}
            public CoreActions(EUISoftGlassGMCenterItem center)
            {
                Actions=new [] {
                    new GMActionDescriptor {Key="ember.gm.reset-time",Group="0 Core",Order=0,Title="Reset time",Icon="↻",Execute=center.ResetTime},
                    new GMActionDescriptor {Key="ember.gm.components",Group="0 Core",Order=1,Title="Enhanced",Icon="◇",ReadToggle=()=>center.Tests.IsEnhanced,WriteToggle=center.Tests.SetEnhanced},
                    new GMActionDescriptor {Key="ember.gm.showcase",Group="0 Core",Order=2,Title="UI library",Icon="▦",Execute=center.ShowLibrary},
                    new GMActionDescriptor {Key="ember.gm.exit",Group="0 Core",Order=99,Title="Exit game",Icon="×",Dangerous=true,Execute=()=>GameLauncher.Instance.Quit()}
                };
            }
        }
        private void ResetTime() { UnityEngine.Time.timeScale=1;EmberTimeManager.Instance.TimeScale=1;Time.RefreshValue(); }
        private void ShowLibrary() { EUIManager.Instance.ShowMainPage(GamePages.EUISoftGlassShowcasePage);RequestClose(); }
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
            Content.GetComponent<EUIResponsiveCardGrid>()?.Refresh();
        }
        #endregion
        #region 外部方法
        public void OpenPanel()
        {
            if(_panelOpen)return;_panelOpen=true;_coreActions=GMActionRegistry.Register(new CoreActions(this));
            int generation=++_generation;
            _prefab=EmberResourceManager.Instance.LoadAssetHandle<GameObject>("UI/Module/UIFoundation/Prefabs/EUISoftGlassGMActionItem");
            _loaded=asset=>{if(generation==_generation&&_panelOpen)BuildActions(asset);};
            _prefab.Completed+=_loaded;RefreshInfo();Scroll.verticalNormalizedPosition=1;
        }
        public void ClosePanel()
        {
            if(!_panelOpen&&_prefab==null)return;_panelOpen=false;_generation++;
            if(_prefab!=null){_prefab.Completed-=_loaded;_prefab.Dispose();_prefab=null;}_loaded=null;
            _coreActions?.Dispose();_coreActions=null;ClearItems();
        }
        public void RefreshInfo()
        {
            if(!_panelOpen)return;State.Refresh();Time.RefreshValue();
            if(_registryVersion!=GMActionRegistry.Version&&_prefab?.Asset)BuildActions(_prefab.Asset);
            foreach(var action in _actions)((EUISoftGlassGMActionItem)action.Logic).Refresh();
        }
        #endregion
    }
}

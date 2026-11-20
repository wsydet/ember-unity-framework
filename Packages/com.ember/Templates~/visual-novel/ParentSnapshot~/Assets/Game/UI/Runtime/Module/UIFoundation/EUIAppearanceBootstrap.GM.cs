using System;
using Cysharp.Threading.Tasks;
using Ember.Basic;
using Ember.Core;
using Ember.UI;
using UnityEngine;

namespace Game.UI
{
    public static partial class EUIAppearanceBootstrap
    {
        #region 内部参数
        private static EUIPage _gm;
        private static IDisposable _samples;
        private static IDisposable _abort;
        private static int _gmVersion;
        private static bool _opening;
        #endregion
        #region 内部方法
        private static void ReleaseSamples() { _samples?.Dispose(); _samples=null; _abort=null; _gm=null; _opening=false; _gmVersion++; }
        private static async UniTask SwitchAsync(EUIGmPresentation presentation)
        {
            int version=++_gmVersion;_opening=false;
            float scale=Time.timeScale;
            var old=_gm;_gm=null;
            if(old!=null&&old.GameObject)
            {
                EUIManager.Instance.ClosePage(old);
                while(old.GameObject&&old.State!=PageState.Closed&&old.State!=PageState.Unloaded)
                { if(version!=_gmVersion)return;await UniTask.NextFrame(); }
            }
            if(version!=_gmVersion)return;
            OpenGM(presentation,scale,version);
        }
        private static void OpenGM(EUIGmPresentation presentation,float scale,int version)
        {
            _opening=true;
            var definition=presentation==EUIGmPresentation.SoftGlass?GamePages.EUISoftGlassGMPage:GamePages.GMPage;
            EUIManager.Instance.ShowFreePage(definition,null,page=>
            {
                _opening=false;
                if(version!=_gmVersion){EUIManager.Instance.ClosePage(page);return;}
                _gm=page;Time.timeScale=scale;EmberTimeManager.Instance.TimeScale=scale;
            });
        }
        #endregion
        #region 外部方法
        [HasGC]
        public static void ShowGM()
        {
            var launcher=GameLauncher.Instance;
            if(_abort==null)_abort=GameLauncher.CurrentLifetime?.RegisterAbort(ReleaseSamples);
            if(_opening||(_gm!=null&&_gm.GameObject&&_gm.IsOpened))return;
            var selected=Config?Config.GmPresentation:EUIGmPresentation.Legacy;
            OpenGM(selected,Time.timeScale,++_gmVersion);
        }
        [HasGC]
        public static void SwitchGM(EUIGmPresentation presentation)=>SwitchAsync(presentation).Forget();
        #endregion
    }
}

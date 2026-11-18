using System;
using System.Collections.Generic;
using Ember.Basic;
using UnityEngine;
using UnityEngine.UI;
namespace Game.UI
{
    public static class SoftGlassDemoActions
    {
        private static int _backgroundMode;
        private static bool _verbose;
        public static int BackgroundMode=>_backgroundMode;
        private sealed class Provider : IGMActionProvider
        {
            public IReadOnlyList<GMActionDescriptor> Actions { get; }=new[] {
                new GMActionDescriptor{Key="sample.background",Group="1 Consumer sample",Order=0,Title="Background",Icon="◇",Execute=CycleBackground},
                new GMActionDescriptor{Key="sample.verbose",Group="1 Consumer sample",Order=1,Title="Verbose log",Icon="≡",ReadToggle=()=>_verbose,WriteToggle=value=>{_verbose=value;EmberDebug.LogEvent(LogTags.Game,"Sample verbose logging: "+value);}}
            };
        }
        [HasGC]
        public static IDisposable RegisterSamples()=>GMActionRegistry.Register(new Provider());
        [NoGC]
        public static void CycleBackground()=>_backgroundMode=(_backgroundMode+1)%3;
        [NoGC]
        public static void SetBackground(int mode)=>_backgroundMode=Mathf.Clamp(mode,0,2);
        [NoGC]
        public static void AnimateBackground(Image[] backgrounds)
        {
            if(backgrounds==null||backgrounds.Length==0)return;
            var root=backgrounds[0].name=="Backdrop"?backgrounds[0].rectTransform:backgrounds[0].rectTransform.parent as RectTransform;
            float width=root?root.rect.width:1280,height=root?root.rect.height:720;
            for(int i=0;i<backgrounds.Length;i++)
            {
                var image=backgrounds[i];if(!image)continue;
                if(image.name=="Backdrop") {image.color=_backgroundMode==1?new Color(.76f,.82f,.9f):new Color(.035f,.075f,.14f);continue;}
                int index=i-(backgrounds[0].name=="Backdrop"?1:0);
                float hue=Mathf.Repeat((_backgroundMode==2?.02f:.5f)+index*.095f+Mathf.Sin(Time.unscaledTime*.13f+index)*.025f,1);
                var tint=Color.HSVToRGB(hue,_backgroundMode==1?.16f:.86f,_backgroundMode==1?.97f:_backgroundMode==2?.6f:.72f);tint.a=.78f;image.color=tint;
                float size=Mathf.Max(width,height)*(.8f+index*.06f);image.rectTransform.sizeDelta=Vector2.one*size;
                image.rectTransform.anchoredPosition=new Vector2((index%2==0?-.32f:.32f)*width+Mathf.Sin(Time.unscaledTime*.18f+index)*width*.07f,(index<2?.32f:-.32f)*height+Mathf.Cos(Time.unscaledTime*.14f+index)*height*.08f);
            }
        }
    }
}

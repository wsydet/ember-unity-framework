using System;
using System.Linq;
using Ember.UI;
using Ember.UIExtension;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Editor
{
    /// <summary>显式更新本库既有资源，保留GUID、Binding和Variant；普通重生不重做布局。</summary>
    public static class SoftGlassUIRevisionTwo
    {
        #region 内部参数
        private const string Root=SoftGlassUILibraryBuilder.ROOT;
        private static readonly Color Ink=new(.035f,.075f,.14f);
        private static readonly Color White=new(.96f,.98f,1);
        #endregion
        #region 外部方法
        public static string Apply()
        {
            var paths=AssetDatabase.FindAssets("EUISoftGlass t:Prefab",new[]{Root.TrimEnd('/')}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            if(paths.Length!=24)throw new InvalidOperationException("Expected the existing 24 SoftGlass targets");
            foreach(var path in paths)Edit(path,go=>{
                foreach(var g in go.GetComponentsInChildren<EUIGlassImage>(true))
                {
                    var c=g.color;
                    if(c.r<.27f&&c.g<.3f&&c.b<.4f)g.color=new Color(.42f,.66f,.92f,.18f);
                    else if(c.b>.8f&&c.r<.25f)g.color=new Color(.06f,.36f,.96f,.46f);
                    g.Gloss=c.a>.94f?.08f:.42f;g.Frost=c.a>.94f?.01f:.065f;g.EdgeLight=.52f;
                }
                var backdrop=go.transform.Find("Animator/EUISafeArea/Center/Backdrop");
                if(backdrop)
                {
                    var image=backdrop.GetComponent<Image>()??backdrop.gameObject.AddComponent<Image>();image.raycastTarget=false;
                    foreach(var block in backdrop.GetComponentsInChildren<Image>(true))if(block!=image)
                    {var rounded=block.GetComponent<EUIRoundedImageModifier>()??block.gameObject.AddComponent<EUIRoundedImageModifier>();rounded.Radius=10000;rounded.TriangleNum=16;}
                }
            });
            Edit("SliderItem",go=>Slider(go,false));Edit("VerticalSliderItem",go=>Slider(go,true));
            Edit("GMEntryItem",Entry);Edit("GMStateItem",State);Edit("GMTimeItem",TimeCard);Edit("GMTestItem",Tests);Edit("GMActionItem",Action);Edit("GMCenterItem",Center);Edit("GMPage",Page);
            // R1 contains explicit nested color overrides: update those same authored instances too.
            foreach(var path in paths)Edit(path,go=>{
                foreach(var binding in go.GetComponentsInChildren<EUIBinding>(true))
                {
                    if(binding.name=="EUISoftGlassGMStateItem")State(binding.gameObject);
                    else if(binding.name=="EUISoftGlassGMTimeItem")TimeCard(binding.gameObject);
                    else if(binding.name=="EUISoftGlassGMTestItem")Tests(binding.gameObject);
                    else if(binding.name=="EUISoftGlassGMEntryItem")Entry(binding.gameObject);
                }
            });
            foreach(var styleName in new[]{"SoftGlassNeutral","SoftGlassLight","SoftGlassAccent"})
            {
                var style=AssetDatabase.LoadAssetAtPath<EUIGlassStyle>("Assets/GameResource/Resources/Config/UIFoundation/"+styleName+".asset");if(!style)continue;
                var so=new SerializedObject(style);so.FindProperty("_tint").colorValue=styleName=="SoftGlassLight"?new Color(.93f,.97f,1,.9f):styleName=="SoftGlassAccent"?new Color(.06f,.38f,.96f,.46f):new Color(.42f,.66f,.92f,.18f);so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(style);
            }
            AssetDatabase.SaveAssets();
            if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Ember/Editor/UILibrary/SoftGlassLibrary.asset"))SoftGlassUILibraryBuilder.RefreshLibrary();
            return "Updated 24 existing targets: handleless bars, FPS entry, optical glass and compact responsive GM";
        }
        #endregion
        #region 资源编辑
        private static void Edit(string name,Action<GameObject> edit)
        {
            string path=name.StartsWith("Assets/")?name:Root+"EUISoftGlass"+name+".prefab";
            var go=PrefabUtility.LoadPrefabContents(path);
            try{edit(go);PrefabUtility.SaveAsPrefabAsset(go,path);}finally{PrefabUtility.UnloadPrefabContents(go);}
        }
        private static RectTransform Rect(string name,Transform parent)
        {var child=parent.Find(name);if(child)return (RectTransform)child;var go=new GameObject(name,typeof(RectTransform));go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);return (RectTransform)go.transform;}
        private static void Box(RectTransform r,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
        {r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=position;r.sizeDelta=size;}
        private static void Stretch(RectTransform r,Vector2 min,Vector2 max)
        {r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=min;r.offsetMax=-max;}
        private static void Top(RectTransform r,float top,float height,float left=24,float right=24)
        {r.anchorMin=Vector2.up;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,1);r.offsetMin=new Vector2(left,-top-height);r.offsetMax=new Vector2(-right,-top);}
        private static TMP_Text Text(Transform parent,string name,string value,float size,Color color)
        {
            var rect=Rect(name,parent);var text=rect.GetComponent<TMP_Text>()??rect.gameObject.AddComponent<TMPEx>();
            text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/GameResource/Resources/UI/Common/Fonts/DefaultLight/DefaultLightSans.asset");
            text.text=value;text.fontSize=size;text.color=color;text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.NoWrap;text.alignment=TextAlignmentOptions.MidlineLeft;return text;
        }
        private static void Style(EUIBackdropGraphic g,Color tint,float gloss=.42f,float frost=.065f,float edge=.52f)
        {g.color=tint;g.Gloss=gloss;g.Frost=frost;g.EdgeLight=edge;}
        private static void Slider(GameObject go,bool vertical)
        {
            var slider=go.GetComponent<Slider>();var track=(RectTransform)go.transform.Find("Track");var graphic=track.GetComponent<EUIGlassImage>();
            if(slider.handleRect){var old=slider.handleRect;slider.handleRect=null;UnityEngine.Object.DestroyImmediate(old.gameObject);}
            var oldArea=track.Find("HandleArea");if(oldArea)UnityEngine.Object.DestroyImmediate(oldArea.gameObject);
            slider.targetGraphic=graphic;slider.transition=Selectable.Transition.None;graphic.raycastTarget=true;
            if(vertical){track.anchorMin=Vector2.zero;track.anchorMax=Vector2.one;track.offsetMin=new Vector2(16,12);track.offsetMax=new Vector2(-16,-12);}
            Style(graphic,new Color(.2f,.46f,.86f,.3f),.36f,.04f,.5f);graphic.Radius=32;
            var fill=slider.fillRect.GetComponent<EUIGlassImage>();Style(fill,new Color(.88f,.97f,1,.98f),.08f,.01f,.04f);fill.Radius=vertical?32:8;fill.SquareTop=vertical;
            var fillArea=(RectTransform)fill.transform.parent;Stretch(fillArea,Vector2.zero,Vector2.zero);
        }
        private static void Entry(GameObject go)
        {
            var ring=go.transform.Find("Ring");if(ring)UnityEngine.Object.DestroyImmediate(ring.gameObject);
            Style(go.GetComponent<EUIGlassImage>(),new Color(.05f,.1f,.18f,.55f),.38f,.04f,.6f);
            var number=Text(go.transform,"FrameRate","60",24,White);number.alignment=TextAlignmentOptions.Center;
            Box(number.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,5),new Vector2(60,32));
            var caption=Text(go.transform,"FPSLabel","FPS",10,White);caption.alignment=TextAlignmentOptions.Center;
            Box(caption.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,-17),new Vector2(44,14));
            var fps=go.GetComponent<FPSDisplay>()??go.AddComponent<FPSDisplay>();var so=new SerializedObject(fps);so.FindProperty("_fpsText").objectReferenceValue=number;so.ApplyModifiedPropertiesWithoutUndo();
            go.GetComponent<EUIButtonEx>().Label=(TMPEx)number;
        }
        private static void State(GameObject go)
        {
            ((RectTransform)go.transform).sizeDelta=new Vector2(340,300);Style(go.GetComponent<EUIGlassImage>(),new Color(.93f,.97f,1,.9f),.16f,.04f,.62f);
            var eyebrow=go.transform.Find("Eyebrow").GetComponent<TMP_Text>();eyebrow.color=new Color(.28f,.39f,.52f);eyebrow.fontSize=15;Top(eyebrow.rectTransform,24,24);
            var state=go.transform.Find("State").GetComponent<TMP_Text>();state.color=Ink;state.fontSize=38;Top(state.rectTransform,65,64);
            foreach(var item in new[]{("UnityTime",148f),("EmberTime",188f)}){var t=go.transform.Find(item.Item1).GetComponent<TMP_Text>();t.color=Ink;t.fontSize=21;Top(t.rectTransform,item.Item2,32);}
            var foot=Text(go.transform,"Caption","Live diagnostics",15,new Color(.3f,.42f,.55f));Top(foot.rectTransform,248,28);
        }
        private static void Toggle(Transform parent,string title)
        {
            var toggle=parent.GetComponentInChildren<EUIToggleEx>(true);var r=(RectTransform)toggle.transform;Top(r,64,44);
            Style(toggle.GetComponent<EUIGlassImage>(),new Color(.42f,.66f,.92f,.18f),.02f,.01f,0);
            toggle.Label.text=title;toggle.Label.fontSize=20;toggle.Label.color=White;Stretch(toggle.Label.rectTransform,Vector2.zero,new Vector2(96,0));
            var track=(RectTransform)r.Find("Track");Box(track,new Vector2(1,.5f),new Vector2(1,.5f),Vector2.zero,new Vector2(72,40));
            Box((RectTransform)track.Find("On"),new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(16,0),new Vector2(30,30));
            Box((RectTransform)track.Find("Off"),new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-16,0),new Vector2(30,30));
        }
        private static void TimeCard(GameObject go)
        {
            ((RectTransform)go.transform).sizeDelta=new Vector2(340,300);Style(go.GetComponent<EUIGlassImage>(),new Color(.06f,.36f,.96f,.32f),.45f,.07f,.62f);
            var title=go.transform.Find("Title").GetComponent<TMP_Text>();title.fontSize=24;Top(title.rectTransform,22,32);Toggle(go.transform,"Time scale");
            var slider=go.GetComponentInChildren<Slider>(true);Box((RectTransform)slider.transform,Vector2.one,Vector2.one,new Vector2(-24,-122),new Vector2(80,160));
            var track=(RectTransform)slider.transform.Find("Track");Stretch(track,new Vector2(8,0),new Vector2(8,0));
            var value=go.transform.Find("Value").GetComponent<TMP_Text>();value.fontSize=38;Box(value.rectTransform,Vector2.up,Vector2.up,new Vector2(24,-145),new Vector2(165,60));
            Box((RectTransform)go.transform.Find("Reset"),Vector2.up,Vector2.up,new Vector2(24,-227),new Vector2(128,48));
            var reset=go.transform.Find("Reset").GetComponent<EUIButtonEx>();reset.Label.fontSize=20;Stretch(reset.Label.rectTransform,new Vector2(8,0),new Vector2(8,0));
        }
        private static void Tests(GameObject go)
        {
            ((RectTransform)go.transform).sizeDelta=new Vector2(340,300);Style(go.GetComponent<EUIGlassImage>(),new Color(.42f,.66f,.92f,.18f));
            var title=go.transform.Find("Title").GetComponent<TMP_Text>();title.fontSize=24;Top(title.rectTransform,22,32);Toggle(go.transform,"Enhanced");
            var viewport=(RectTransform)go.transform.Find("Viewport");Stretch(viewport,new Vector2(24,16),new Vector2(24,112));
            var content=(RectTransform)viewport.Find("Content");content.anchorMin=Vector2.up;content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,168);content.anchoredPosition=Vector2.zero;
            var keys=new[]{"Img_Test","Raw_Test","EUIImg_Test","Img_Circle"};var captions=new[]{"Image","Raw image","Enhanced","Circle"};
            for(int i=0;i<keys.Length;i++)
            {
                var graphic=content.GetComponentsInChildren<RectTransform>(true).First(t=>t.name==keys[i]);var cell=Rect("Sample"+i,content);graphic.SetParent(cell,false);
                Box(graphic,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-2),new Vector2(56,56));
                var text=Text(cell,"Caption",captions[i],12,White);text.alignment=TextAlignmentOptions.Center;Top(text.rectTransform,60,16,0,0);
                var layout=cell.GetComponent<LayoutElement>()??cell.gameObject.AddComponent<LayoutElement>();layout.preferredHeight=76;
            }
            var flow=content.GetComponent<EUIResponsiveFlowLayout>()??content.gameObject.AddComponent<EUIResponsiveFlowLayout>();var so=new SerializedObject(flow);so.FindProperty("_minimumCellWidth").floatValue=100;so.FindProperty("_maximumColumns").intValue=4;so.FindProperty("_spacing").floatValue=8;so.ApplyModifiedPropertiesWithoutUndo();
            go.GetComponent<ScrollRect>().movementType=ScrollRect.MovementType.Clamped;
        }
        private static void Action(GameObject go)
        {
            Box((RectTransform)go.transform.Find("Action"),new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,20),new Vector2(84,84));
            var label=go.transform.Cast<Transform>().First(t=>t.name=="Label").GetComponent<TMP_Text>();label.fontSize=16;label.textWrappingMode=TextWrappingModes.Normal;label.alignment=TextAlignmentOptions.Center;
            var rect=label.rectTransform;rect.anchorMin=new Vector2(0,.5f);rect.anchorMax=new Vector2(1,.5f);rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(-8,40);rect.anchoredPosition=new Vector2(0,-46);
        }
        private static void Center(GameObject go)
        {
            var surface=go.GetComponent<EUIGlassImage>();if(surface)UnityEngine.Object.DestroyImmediate(surface);
            if(!go.GetComponent<EUIConstrainedPanel>())go.AddComponent<EUIConstrainedPanel>();
            var title=go.transform.Find("Title").GetComponent<TMP_Text>();title.fontSize=34;Top(title.rectTransform,32,52,24,96);
            var subtitle=Text(go.transform,"Eyebrow","RUNTIME TOOLS",13,new Color(.7f,.83f,.97f));Top(subtitle.rectTransform,10,22);
            Box((RectTransform)go.transform.Find("Close"),Vector2.one,Vector2.one,new Vector2(-24,-24),new Vector2(52,52));
            var close=go.transform.Find("Close").GetComponent<EUIButtonEx>();Stretch(close.Label.rectTransform,Vector2.zero,Vector2.zero);close.Label.fontSize=28;
            var viewport=(RectTransform)go.transform.Find("Viewport");Stretch(viewport,new Vector2(24,24),new Vector2(24,104));
            var content=viewport.Find("Content");var heading=Text(content,"ActionHeading","QUICK ACTIONS",14,new Color(.73f,.84f,.97f));
            var layout=content.GetComponent<EUIResponsiveCardGrid>();var so=new SerializedObject(layout);so.FindProperty("_sectionHeading").objectReferenceValue=heading.rectTransform;so.FindProperty("_cardHeight").floatValue=300;so.ApplyModifiedPropertiesWithoutUndo();
            go.GetComponent<ScrollRect>().movementType=ScrollRect.MovementType.Clamped;
        }
        private static void Page(GameObject go)
        {
            var host=go.transform.Find("Animator/EUISafeArea/Center/CenterHost");Stretch((RectTransform)host,Vector2.zero,Vector2.zero);var backdrop=Rect("Backdrop",host);backdrop.SetAsFirstSibling();Stretch(backdrop,Vector2.zero,Vector2.zero);
            var surface=backdrop.GetComponent<EUIGlassImage>()??backdrop.gameObject.AddComponent<EUIGlassImage>();Style(surface,new Color(.12f,.16f,.22f,.64f),0,0,0);surface.raycastTarget=true;
            var panel=host.GetComponentInChildren<EUIConstrainedPanel>(true);panel.Refresh();
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ember.Core.Editor;
using Ember.UI;
using Ember.UIExtension;
using Ember.UIExtension.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Editor
{
    /// <summary>正式base页面原位制作；不重建玻璃库，不修改页面逻辑或Binding所有权。</summary>
    public static class BaseSoftGlassPageAuthoring
    {
        #region 内部参数
        private const string Evidence = ".utmp/ember-workflow/20261117-base-soft-glass-migration/evidence/base/";
        private const string Common = "Assets/GameResource/Resources/UI/Common/Prefabs/";
        private static readonly Color Ink = new Color(.035f,.075f,.14f);
        private static readonly Color Muted = new Color(.28f,.39f,.52f);
        private static readonly List<object> Receipts = new List<object>();
        #endregion

        #region 外部方法
        public static string Apply()
        {
            if (!EmberProjectSetup.IsTemplateActive("base") || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Requires formal base edit mode");
            Directory.CreateDirectory(Evidence);
            Receipts.Clear();
            Edit(Common+"EUIMainPage.prefab", Main);
            Edit(Common+"EUISettingPage.prefab", Settings);
            Edit(Common+"EUIGamePlayPage.prefab", Gameplay);
            Edit(Find("EUISettingsContextItem"), Context);
            Edit(Find("EUILoadingProgressItem"), Progress);
            Edit(Common+"EUILoadingPage.prefab", Loading);
            Edit(Common+"EUISettingSliderRowItem.prefab", Rows);
            Edit(Common+"EUISettingToggleRowItem.prefab", Rows);
            AssetDatabase.SaveAssets();
            string result=Newtonsoft.Json.JsonConvert.SerializeObject(Receipts,Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(Evidence+"page-authoring-receipt.json",result);
            return result;
        }
        #endregion

        #region 内部方法
        private static string Find(string name)
        {
            var paths=AssetDatabase.FindAssets(name+" t:Prefab",new[]{"Assets/GameResource/Resources/UI"})
                .Select(AssetDatabase.GUIDToAssetPath).Where(p=>Path.GetFileNameWithoutExtension(p)==name).ToArray();
            if(paths.Length!=1)throw new InvalidOperationException("Ambiguous resource: "+name);
            return paths[0];
        }
        private static object Snapshot(GameObject root)
        {
            return root.GetComponentsInChildren<EUIBinding>(true).Select(b=>new {
                path=AnimationUtility.CalculateTransformPath(b.transform,root.transform),b.ClassName,
                pathMode=b.PathMode.ToString(),bindings=b.Bindings.Select(e=>new {
                    e.Name,e.IsFramework,type=e.Type.ToString(),e.ClassName,
                    target=e.GameObject?AnimationUtility.CalculateTransformPath(e.GameObject.transform,root.transform):null
                }).ToArray()
            }).ToArray();
        }
        private static void Edit(string path,Action<GameObject> action)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                string before=Newtonsoft.Json.JsonConvert.SerializeObject(Snapshot(root));
                action(root);
                foreach(var component in root.GetComponentsInChildren<Component>(true))
                    if(component&&PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                string after=Newtonsoft.Json.JsonConvert.SerializeObject(Snapshot(root));
                if(before!=after)throw new InvalidOperationException("Unexpected Binding change: "+path);
                PrefabUtility.SaveAsPrefabAsset(root,path);
                Receipts.Add(new {path,guid=AssetDatabase.AssetPathToGUID(path),bindingUnchanged=true,binding=Snapshot(root)});
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        private static GameObject Target(GameObject root,string name)
        {
            var entry=root.GetComponent<EUIBinding>().Bindings.FirstOrDefault(e=>e.Name==name);
            if(!entry.GameObject)throw new InvalidOperationException("Missing binding "+root.name+"."+name);
            return entry.GameObject;
        }
        private static RectTransform Rect(Transform parent,string name)
        {
            var found=parent.Find(name);if(found)return (RectTransform)found;
            var go=new GameObject(name,typeof(RectTransform));go.layer=parent.gameObject.layer;
            go.transform.SetParent(parent,false);return (RectTransform)go.transform;
        }
        private static void Box(RectTransform r,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
        {r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=position;r.sizeDelta=size;}
        private static void Stretch(RectTransform r,float inset=0)
        {r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*inset;r.offsetMax=-Vector2.one*inset;}
        private static void Top(RectTransform r,float y,float height,float inset=32)
        {r.anchorMin=Vector2.up;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,1);r.offsetMin=new Vector2(inset,-y-height);r.offsetMax=new Vector2(-inset,-y);}
        private static EUIGlassImage Surface(Transform parent,bool accent=false,float radius=24,bool raycast=false)
        {
            var rect=Rect(parent,"SoftGlassSurface");rect.SetAsFirstSibling();Stretch(rect);
            var image=rect.GetComponent<EUIGlassImage>()??rect.gameObject.AddComponent<EUIGlassImage>();
            var style=AssetDatabase.LoadAssetAtPath<EUIGlassStyle>("Assets/GameResource/Resources/Config/UIFoundation/"+(accent?"SoftGlassAccent":"SoftGlassNeutral")+".asset");
            if(!style)throw new InvalidOperationException("Missing existing R2 glass style");
            style.ApplyTo(image,radius>=40?EUIGlassRadius.Panel:EUIGlassRadius.Control);
            image.Gloss=.42f;image.Frost=.065f;image.EdgeLight=.52f;image.raycastTarget=raycast;
            return image;
        }
        private static TMP_Text Label(Transform parent,string name,string value,float size)
        {
            var rect=Rect(parent,name);var label=rect.GetComponent<TMP_Text>()??rect.gameObject.AddComponent<TMPEx>();
            label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/GameResource/Resources/UI/Common/Fonts/DefaultLight/DefaultLightSans.asset");
            label.text=value;label.fontSize=size;label.color=Ink;label.alignment=TextAlignmentOptions.Center;
            label.fontStyle=FontStyles.Normal;
            label.raycastTarget=false;label.textWrappingMode=TextWrappingModes.Normal;
            return label;
        }
        private static void Boundary(GameObject root)
        {if(!root.GetComponent<EUIBackdropGroup>())root.AddComponent<EUIBackdropGroup>();}
        private static void Panel(RectTransform rect,float width,float height)
        {
            Box(rect,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(width,height));
            var legacy=rect.GetComponent<Image>();if(legacy){legacy.enabled=false;legacy.raycastTarget=false;}
            var constrained=rect.GetComponent<EUIConstrainedPanel>()??rect.gameObject.AddComponent<EUIConstrainedPanel>();
            using(var so=new SerializedObject(constrained))
            {so.FindProperty("_maximumSize").vector2Value=new Vector2(width,height);so.FindProperty("_portraitHeight").floatValue=height;so.FindProperty("_inset").floatValue=28;so.ApplyModifiedPropertiesWithoutUndo();}
            Surface(rect,false,40,true);constrained.Refresh();
        }
        private static Button Button(GameObject go,bool accent=false)
        {
            var button=go.GetComponent<Button>();if(!button)throw new InvalidOperationException("Expected existing Button: "+go.name);
            var old=go.GetComponent<Image>();if(old){old.enabled=false;old.raycastTarget=false;}
            button.targetGraphic=Surface(go.transform,accent,22,true);
            var colors=ColorBlock.defaultColorBlock;colors.highlightedColor=new Color(1.12f,1.14f,1.18f);
            colors.pressedColor=new Color(.72f,.8f,.96f);colors.selectedColor=new Color(.82f,.92f,1);
            colors.disabledColor=new Color(.55f,.6f,.7f,.65f);colors.fadeDuration=.08f;button.colors=colors;
            foreach(var label in go.GetComponentsInChildren<TMP_Text>(true))
            {label.color=Ink;label.fontSize=26;label.fontStyle=FontStyles.Normal;label.alignment=TextAlignmentOptions.Center;Stretch(label.rectTransform,12);}
            return button;
        }
        private static void Main(GameObject root)
        {
            Boundary(root);var center=root.transform.Find("Animator/EUISafeArea/Center");
            var background=root.transform.Find("Animator/BuiBackground");
            if(background)
            {
                var image=background.GetComponent<RawImage>();
                var fitter=background.GetComponent<AspectRatioFitter>()??background.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectRatio=image&&image.texture?(float)image.texture.width/image.texture.height:16f/9f;
                fitter.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
            }
            foreach(var path in new[]{"Animator/EUISafeArea/TopCenter/TitleText","Animator/EUISafeArea/Center/BuiSurface","Animator/EUISafeArea/Center/BuiSubtitle"})
            {var old=root.transform.Find(path);if(old)old.gameObject.SetActive(false);}
            var panel=Rect(center,"SoftGlassMainCard");panel.SetAsFirstSibling();Panel(panel,600,520);
            Top(Label(panel,"Eyebrow","EMBER FRAMEWORK",15).rectTransform,36,28);
            Top(Label(panel,"Title","EMBER",62).rectTransform,86,88);
            var subtitle=Label(panel,"Subtitle","主界面",22);subtitle.color=Muted;Top(subtitle.rectTransform,180,42);
            var start=Button(Target(root,"Btn_Start"),true);var settings=Button(Target(root,"Btn_Settings"));
            Box((RectTransform)start.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,-68),new Vector2(400,72));
            Box((RectTransform)settings.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,-158),new Vector2(400,72));
        }
        private static void Settings(GameObject root)
        {
            Boundary(root);var close=Target(root,"Btn_Close");var panel=(RectTransform)close.transform.parent;
            Panel(panel,620,440);
            var oldTitle=panel.Find("TitleText");if(oldTitle)oldTitle.gameObject.SetActive(false);
            var title=Label(panel,"SoftGlassTitle","设置",40);Top(title.rectTransform,54,64,80);
            var eyebrow=Label(panel,"SoftGlassEyebrow","PREFERENCES",14);eyebrow.color=Muted;Top(eyebrow.rectTransform,26,24,80);
            Button(close);Box((RectTransform)close.transform,Vector2.one,Vector2.one,new Vector2(-24,-24),new Vector2(52,52));
            foreach(var label in close.GetComponentsInChildren<TMP_Text>(true)){label.text="×";label.fontSize=28;}
            var context=Target(root,"Txt_NowScene");Top((RectTransform)context.transform,166,72);
            foreach(var text in context.GetComponentsInChildren<TMP_Text>(true)){text.fontSize=26;text.fontStyle=FontStyles.Normal;text.color=Ink;text.alignment=TextAlignmentOptions.Center;}
            var foot=Label(panel,"SoftGlassFooter","返回后继续当前流程",18);foot.color=Muted;Top(foot.rectTransform,320,36);
        }
        private static void Gameplay(GameObject root)
        {
            Boundary(root);var back=Button(Target(root,"Btn_Back"));var settings=Button(Target(root,"Btn_Settings"));
            Box((RectTransform)back.transform,Vector2.up,Vector2.up,new Vector2(28,-28),new Vector2(220,64));
            Box((RectTransform)settings.transform,Vector2.one,Vector2.one,new Vector2(-28,-28),new Vector2(220,64));
        }
        private static void Context(GameObject root)
        {foreach(var label in root.GetComponentsInChildren<TMP_Text>(true)){label.color=Ink;label.fontSize=26;label.fontStyle=FontStyles.Normal;label.alignment=TextAlignmentOptions.Center;}}
        private static void Loading(GameObject root)
        {
            // Keep curtain, progress visibility and real preparation references untouched.
            var host=Target(root,"Cg_Progress");
            var rect=(RectTransform)host.transform;rect.anchorMin=new Vector2(.15f,.5f);rect.anchorMax=new Vector2(.85f,.5f);rect.sizeDelta=new Vector2(0,92);rect.anchoredPosition=new Vector2(0,24);
            Progress(host);
        }
        private static void Progress(GameObject root)
        {
            foreach(var slider in root.GetComponentsInChildren<Slider>(true)) StyleSlider(slider);
            foreach(var label in root.GetComponentsInChildren<TMP_Text>(true)){label.color=Ink;label.fontSize=22;}
        }
        private static void StyleSlider(Slider slider)
        {
            if(slider.handleRect){slider.handleRect.gameObject.SetActive(false);slider.handleRect=null;}
            var track=slider.transform.Find("Track")??slider.transform.Find("Background")??slider.transform;
            if(track){var old=track.GetComponent<Image>();if(old){old.enabled=false;old.raycastTarget=false;}slider.targetGraphic=Surface(track,false,16,true);}
            if(slider.fillRect){var graphic=slider.fillRect.GetComponent<Graphic>();if(graphic)graphic.color=new Color(.88f,.97f,1,.98f);}
        }
        private static void Rows(GameObject root)
        {
            foreach(var slider in root.GetComponentsInChildren<Slider>(true)) StyleSlider(slider);
            foreach(var toggle in root.GetComponentsInChildren<Toggle>(true))
            {
                var target=toggle.targetGraphic;
                if(target&&target!=toggle.graphic){target.enabled=false;target.raycastTarget=false;toggle.targetGraphic=Surface(target.transform,false,20,true);}
                if(toggle.graphic)toggle.graphic.color=new Color(.88f,.97f,1);
            }
            foreach(var label in root.GetComponentsInChildren<TMP_Text>(true)){label.color=Ink;label.fontSize=Mathf.Min(label.fontSize,24);}
        }
        #endregion
    }
}

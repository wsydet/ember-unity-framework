// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Linq;
using System.Collections.Generic;
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
    /// <summary>S2 公共控件的显式制作入口，仅创建缺失资产；不运行动态容器。</summary>
    public static class ExpandedUILibraryBuilder
    {
        #region 内部参数
        private const string COMMON = "Assets/GameResource/Resources/UI/Common/Prefabs/";
        private const string MODULE = "Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/";
        private const string ATLAS = "Assets/GameResource/Resources/UI/Common/Atlas/DefaultLight/";
        private static readonly Color Ink = new Color32(32,35,42,255), Blue = new Color32(10,100,232,255), Muted = new Color32(100,106,118,255), Pale = new Color32(237,240,245,255);
        private static TMP_FontAsset _font;
        private static Sprite _round;
        private static readonly string[] Names = { "SecondaryButton", "DestructiveButton", "IconButton", "CloseButton", "BackButton", "AmountButton", "Tab", "TabGroup", "SegmentedControl", "Switch", "SettingToggleRow", "Slider", "SettingSliderRow", "Stepper", "EmptyState", "Badge", "IconCard", "HorizontalListHost", "GridHost", "TitleBar", "SectionTitle", "Input", "SearchField", "Toast", "Tooltip", "Dropdown", "AlertPage", "ConfirmPage", "ExpandedLibraryPage" };
        #endregion
        #region 外部方法
        public static string Build()
        {
            if (!EmberProjectSetup.IsTemplateActive("base") || EditorApplication.isPlaying) throw new InvalidOperationException("需要 base 编辑态。");
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/GameResource/Resources/UI/Common/Fonts/DefaultLight/DefaultLightSans.asset");
            _round = AssetDatabase.LoadAssetAtPath<Sprite>(ATLAS + "RoundedSurfaceHiDPI.png");
            foreach (var icon in new[]{"Close","Back","Search","Help","Plus","Minus","Chevron","Lock"})
            {
                var path = ATLAS + icon + ".png"; AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 256; importer.filterMode = FilterMode.Bilinear; importer.SaveAndReimport();
            }
            foreach (var name in Names)
            {
                var page = name.EndsWith("Page"); var full = "EUI" + name + (page ? "" : "Item");
                var path = (page ? MODULE : COMMON) + full + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path))
                {
                    var existing = PrefabUtility.LoadPrefabContents(path);
                    try { UniqueChildren(existing.transform); Record(existing); PrefabUtility.SaveAsPrefabAsset(existing,path); }
                    finally { PrefabUtility.UnloadPrefabContents(existing); }
                    Register(AssetDatabase.LoadAssetAtPath<GameObject>(path),name); continue;
                }
                var request = new EUICreationRequest { PrefabName = full, ClassName = full, PageName = full, ClassPath = "Module/UIFoundation", Role = page ? EUIBindingRole.Page : EUIBindingRole.Item,
                    CodePathMode = page ? EUIBinding.CodePathMode.Business : EUIBinding.CodePathMode.Framework, StructureOnly = !page,
                    PageType = name == "ExpandedLibraryPage" ? PageType.MainPage : PageType.Popup, UseMask = page && name != "ExpandedLibraryPage", ClickMaskToClose = true,
                    ReferenceResolution = new Vector2(1280,720), ContentSlotPath = "Animator/EUISafeArea/Center/Content" };
                var source = Source(name); if (source != null) request.SourceSkeleton = AssetDatabase.LoadAssetAtPath<GameObject>(COMMON + source + ".prefab");
                if (!EUICreationService.TryBuildPlan(request, out _, out var preflight)) throw new InvalidOperationException(preflight.Error);
                var result = EUICreationService.Create(request); if (!result.Success) throw new InvalidOperationException(result.Error);
                var root = PrefabUtility.LoadPrefabContents(result.PrefabPath);
                try { Decorate(root, name); UniqueChildren(root.transform); Record(root); PrefabUtility.SaveAsPrefabAsset(root, result.PrefabPath); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(result.PrefabPath);
                if (page && !EUIBindingCodeGenUtility.TryRegenerateCode(saved.GetComponent<EUIBinding>(), out var error)) throw new InvalidOperationException(error);
                Register(saved, name);
            }
            var lib = EUILibraryService.LoadDefault(); lib.SkinVersion = "1.1.0"; EditorUtility.SetDirty(lib); AssetDatabase.SaveAssets();
            EUILibraryService.GenerateCatalog(lib); return string.Join(", ", Names);
        }
        #endregion
        #region 内部方法
        private static string Source(string name) => name switch {
            "SecondaryButton" or "DestructiveButton" or "AmountButton" => "EUIActionButtonItem",
            "CloseButton" or "BackButton" => "EUIIconButtonItem", "SegmentedControl" => "EUITabGroupItem",
            "HorizontalListHost" or "GridHost" => "EUIContentHostItem", "SearchField" => "EUIInputItem",
            "AlertPage" or "ConfirmPage" => "EUIStandardPopupPage", "ExpandedLibraryPage" => "EUIStandardPage", _ => null };
        private static RectTransform Rect(string name, Transform parent, float w, float h, float x=0, float y=0)
        {
            var g = new GameObject(name, typeof(RectTransform)); g.layer = LayerMask.NameToLayer("UI");
            var r = (RectTransform)g.transform; r.SetParent(parent,false); r.sizeDelta = new Vector2(w,h); r.anchoredPosition = new Vector2(x,y); return r;
        }
        private static void Stretch(RectTransform r, float left=0, float bottom=0, float right=0, float top=0)
        { r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=new Vector2(left,bottom); r.offsetMax=new Vector2(-right,-top); }
        private static Image Surface(RectTransform r, Color color, float radius=10, bool hit=false)
        {
            var i=r.gameObject.AddComponent<EUIImageEx>(); i.sprite=_round; i.type=Image.Type.Sliced; i.pixelsPerUnitMultiplier=16/radius; i.color=color; i.raycastTarget=hit; return i;
        }
        private static Image Icon(string name,Transform parent,string sprite,float size=24,float x=0,float y=0)
        { var i=Rect(name,parent,size,size,x,y).gameObject.AddComponent<EUIImageEx>(); i.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(ATLAS+sprite+".png"); i.preserveAspect=true; i.color=Ink; i.raycastTarget=false; return i; }
        private static TMPEx Text(string name,Transform parent,string value,float w,float h,float x=0,float y=0,float size=16)
        {
            var t=Rect(name,parent,w,h,x,y).gameObject.AddComponent<TMPEx>(); t.font=_font; t.SetFontForSkin(4,1);t.SetFontSkin(4,1);t.fontSize=size;t.color=Ink;t.text=value;
            t.alignment=TextAlignmentOptions.MidlineLeft;t.textWrappingMode=TextWrappingModes.Normal;t.raycastTarget=false;return t;
        }
        private static EUIButtonEx Button(string name,Transform parent,string label,float w=120,float x=0,float y=0,bool primary=false)
        {
            var r=Rect(name,parent,w,40,x,y);var i=Surface(r,primary?Blue:Pale,8,true);var b=r.gameObject.AddComponent<EUIButtonEx>();b.targetGraphic=i;
            b.Label=Text("Label",r,label,w-20,36);b.Label.alignment=TextAlignmentOptions.Center;b.Label.color=primary?Color.white:Ink;return b;
        }
        private static EUIButtonEx IconButton(string name,Transform parent,string icon,float x=0,float y=0)
        { var b=Button(name,parent,"",40,x,y);Icon("Icon",b.transform,icon);return b; }
        private static GameObject Nest(string name,Transform parent,float x,float y,float w=0,float h=0)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(COMMON+"EUI"+name+"Item.prefab");var g=(GameObject)PrefabUtility.InstantiatePrefab(source,parent);
            var r=(RectTransform)g.transform;r.anchoredPosition=new Vector2(x,y);if(w>0)r.sizeDelta=new Vector2(w,h);return g;
        }
        private static void Wire(UnityEngine.Object target,string field,UnityEngine.Object value)
        { using var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void WireArray(UnityEngine.Object target,string field,UnityEngine.Object[] values)
        { using var so=new SerializedObject(target);var p=so.FindProperty(field);p.arraySize=values.Length;for(var i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Bind(GameObject root,params (string name,Component component)[] values)
        { EUIBindingEditorUtility.SetBindings(root.GetComponent<EUIBinding>(),values.Select(v=>new EUIBindingEntrySnapshot {Name=v.name,GameObjectPath=AnimationUtility.CalculateTransformPath(v.component.transform,root.transform),WidgetType=EUIBinding.WidgetTypes.Extension,ClassName=v.component.GetType().FullName}).ToList()); }
        private static void Record(GameObject root)
        { foreach(var c in root.GetComponentsInChildren<Component>(true))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c); }
        private static void UniqueChildren(Transform parent)
        {
            var used = new HashSet<string>();
            foreach(Transform child in parent)
            {
                var original=child.name;var index=1;
                while(!used.Add(child.name))child.name=original+index++;
                UniqueChildren(child);
            }
        }
        private static void Decorate(GameObject root,string name)
        {
            var r=(RectTransform)root.transform;if(!name.EndsWith("Page"))r.sizeDelta=new Vector2(320,48);
            switch(name)
            {
                case "SecondaryButton":case "DestructiveButton":
                    root.GetComponent<Image>().color=name=="SecondaryButton"?Pale:new Color32(211,47,55,255);
                    root.GetComponent<EUIButtonEx>().Label.color=name=="SecondaryButton"?Ink:Color.white;
                    root.GetComponent<EUIButtonEx>().Label.text=name=="SecondaryButton"?"Cancel":"Delete";break;
                case "IconButton":
                    r.sizeDelta=new Vector2(40,40);var ib=root.AddComponent<EUIButtonEx>();ib.targetGraphic=Surface(r,Pale,8,true);Icon("Icon",r,"Help");Bind(root,("Button",ib));break;
                case "CloseButton":case "BackButton":
                    r.sizeDelta=new Vector2(40,40);root.transform.Find("Icon").GetComponent<Image>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>(ATLAS+(name=="CloseButton"?"Close":"Back")+".png");break;
                case "AmountButton":
                    r.sizeDelta=new Vector2(240,48);var ab=root.GetComponent<EUIButtonEx>();ab.Label.text="Unlock";ab.Label.rectTransform.sizeDelta=new Vector2(110,40);ab.Label.rectTransform.anchorMin=ab.Label.rectTransform.anchorMax=new Vector2(.25f,.5f);ab.Label.rectTransform.anchoredPosition=Vector2.zero;
                    Icon("AmountIcon",r,"Plus",22,38);var amount=Text("Amount",r,"120",50,36,82);amount.color=Color.white;break;
                case "Tab":BuildTab(root);break;
                case "TabGroup":
                    r.sizeDelta=new Vector2(450,44);Surface(r,Pale);var group=root.AddComponent<EUIToggleGroupEx>();group.allowSwitchOff=false;
                    for(var i=0;i<3;i++){var tab=Nest("Tab",r,(i-1)*148,0,144,40);var toggle=tab.GetComponent<EUIToggleEx>();toggle.group=group;toggle.Label.text=new[]{"General","Display","Advanced"}[i];toggle.SetIsOnWithoutNotify(i==0);toggle.Refresh();}group.SetToggleOn(0);Bind(root,("Group",group));break;
                case "SegmentedControl":r.sizeDelta=new Vector2(450,44);break;
                case "Switch":BuildSwitch(root);break;
                case "SettingToggleRow":
                    r.sizeDelta=new Vector2(440,76);Surface(r,Color.white);Text("Title",r,"Notifications",280,24,-62,14);Text("Description",r,"Allow activity updates",280,20,-62,-12,13).color=Muted;
                    var sw=Nest("Switch",r,176,0);Bind(root,("Toggle",sw.GetComponent<EUIToggleEx>()));break;
                case "Slider":BuildNumber(root,true);break;
                case "Stepper":BuildNumber(root,false);break;
                case "SettingSliderRow":
                    r.sizeDelta=new Vector2(440,110);Surface(r,Color.white);Text("Title",r,"Volume",390,24,0,32);var slider=Nest("Slider",r,0,-12,400,48);Bind(root,("Value",slider.GetComponent<EUINumericControl>()));break;
                case "EmptyState":
                    r.sizeDelta=new Vector2(320,270);Icon("Illustration",r,"EmptyState",130,0,57).color=Color.white;
                    Text("Title",r,"Nothing here yet",290,30,0,-20,20).alignment=TextAlignmentOptions.Center;Text("Description",r,"Add your first item to get started.",290,40,0,-58,14).alignment=TextAlignmentOptions.Center;
                    var action=Button("Action",r,"Add item",140,0,-110,true);Bind(root,("Action",action));break;
                case "Badge":
                    r.sizeDelta=new Vector2(54,24);var dot=Rect("Dot",r,10,10);Surface(dot,new Color32(220,54,65,255),5);dot.gameObject.SetActive(false);
                    var tag=Rect("Tag",r,48,24);Surface(tag,new Color32(220,54,65,255),12);var label=Text("Label",tag,"12",44,24,0,0,12);label.color=Color.white;label.alignment=TextAlignmentOptions.Center;
                    var badge=root.AddComponent<EUIBadgeView>();Wire(badge,"_dot",dot.gameObject);Wire(badge,"_tag",tag.gameObject);Wire(badge,"_label",label);Bind(root,("Badge",badge));break;
                case "IconCard":BuildCard(root);break;
                case "HorizontalListHost":case "GridHost":BuildHost(root,name);break;
                case "TitleBar":case "SectionTitle":
                    r.sizeDelta=new Vector2(480,name=="TitleBar"?60:42);Text("Title",r,name=="TitleBar"?"Library":"Collection",340,40,-20,0,22);
                    var help=IconButton("Help",r,"Help",210);if(name=="TitleBar") {var back=IconButton("Back",r,"Back",-218);root.transform.Find("Title").GetComponent<TMP_Text>().rectTransform.anchoredPosition=new Vector2(8,0);Bind(root,("Back",back),("Help",help));}else Bind(root,("Help",help));break;
                case "Input":BuildInput(root);break;
                case "SearchField":
                    var input=root.GetComponent<TMP_InputField>();input.placeholder.GetComponent<TMP_Text>().text="Search items";Icon("SearchIcon",r,"Search",24,-138);input.textViewport.offsetMin=new Vector2(42,8);break;
                case "Toast":
                    r.sizeDelta=new Vector2(420,60);var content=Rect("Notice",r,420,60);Surface(content,new Color32(40,44,52,255),12);
                    var ti=Icon("Icon",content,"Checkmark",24,-180);ti.color=Color.white;var tl=Text("Message",content,"Changes saved",340,48,20);tl.color=Color.white;
                    var toast=root.AddComponent<EUITimedNotice>();Wire(toast,"_content",content.gameObject);Wire(toast,"_label",tl);Wire(toast,"_icon",ti);content.gameObject.SetActive(false);Bind(root,("Notice",toast));break;
                case "Tooltip":BuildTip(root);break;
                case "Dropdown":BuildDropdown(root);break;
                case "AlertPage":case "ConfirmPage":BuildDialog(root,name=="ConfirmPage");break;
                case "ExpandedLibraryPage":BuildGallery(root);break;
            }
        }
        private static void BuildTab(GameObject root)
        {
            var r=(RectTransform)root.transform;r.sizeDelta=new Vector2(144,40);var toggle=root.AddComponent<EUIToggleEx>();toggle.targetGraphic=Surface(r,Pale,8,true);
            var selected=Rect("Selected",r,140,36);Stretch(selected,2,2,2,2);Surface(selected,Color.white,7);toggle.OnNode=selected.gameObject;
            toggle.Label=Text("Label",r,"General",130,36);Stretch(toggle.Label.rectTransform,7,2,7,2);toggle.Label.alignment=TextAlignmentOptions.Center;toggle.isOn=true;toggle.Refresh();Bind(root,("Toggle",toggle));
        }
        private static void BuildSwitch(GameObject root)
        {
            var r=(RectTransform)root.transform;r.sizeDelta=new Vector2(52,32);var toggle=root.AddComponent<EUIToggleEx>();toggle.targetGraphic=Surface(r,new Color32(185,190,200,255),16,true);
            var on=Rect("On",r,52,32);Surface(on,Blue,16);Surface(Rect("Knob",on,26,26,10),Color.white,13);
            var off=Rect("Off",r,52,32);Surface(Rect("Knob",off,26,26,-10),Color.white,13);toggle.OnNode=on.gameObject;toggle.OffNode=off.gameObject;toggle.isOn=true;toggle.Refresh();Bind(root,("Toggle",toggle));
        }
        private static void BuildNumber(GameObject root,bool withSlider)
        {
            var r=(RectTransform)root.transform;r.sizeDelta=new Vector2(withSlider?400:180,48);
            var minus=IconButton("Decrease",r,"Minus",withSlider?-180:-66);var plus=IconButton("Increase",r,"Plus",withSlider?180:66);
            var value=Text("Value",r,"50",60,30,withSlider?118:0);value.alignment=TextAlignmentOptions.Center;
            var presenter=root.AddComponent<EUINumericControl>();Wire(presenter,"_decrease",minus);Wire(presenter,"_increase",plus);Wire(presenter,"_label",value);
            if(withSlider){var track=Rect("Track",r,220,8,-40);Surface(track,Pale,4,true);var fill=Rect("Fill",track,220,8);Stretch(fill);Surface(fill,Blue,4);
                var handles=Rect("HandleArea",track,220,0);handles.anchorMin=new Vector2(0,.5f);handles.anchorMax=new Vector2(1,.5f);handles.sizeDelta=Vector2.zero;var handle=Rect("Handle",handles,22,22);var hi=Surface(handle,Color.white,11,true);var outline=handle.gameObject.AddComponent<Outline>();outline.effectColor=new Color(0,0,0,.18f);outline.effectDistance=new Vector2(1,-1);
                var slider=track.gameObject.AddComponent<Slider>();slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=hi;slider.maxValue=100;slider.value=50;Wire(presenter,"_slider",slider);Bind(root,("Value",presenter),("Slider",slider));}
            else Bind(root,("Value",presenter));
        }
        private static void BuildCard(GameObject root)
        {
            var r=(RectTransform)root.transform;r.sizeDelta=new Vector2(150,172);var button=root.AddComponent<EUIButtonEx>();button.targetGraphic=Surface(r,Color.white,12,true);
            var icon=Icon("Icon",r,"EmptyState",100,0,28);icon.color=Color.white;var title=Text("Title",r,"Untitled",126,26,0,-40);title.alignment=TextAlignmentOptions.Center;
            var qty=Text("Quantity",r,"12",126,24,0,-66,13);qty.alignment=TextAlignmentOptions.Center;
            var selected=Icon("Selected",r,"Checkmark",24,54,62);selected.color=Blue;selected.gameObject.SetActive(false);
            var locked=Icon("Locked",r,"Lock",24,-54,62);locked.gameObject.SetActive(false);
            var view=root.AddComponent<EUIIconCardView>();Wire(view,"_icon",icon);Wire(view,"_title",title);Wire(view,"_quantity",qty);Wire(view,"_selected",selected.gameObject);Wire(view,"_locked",locked.gameObject);Wire(view,"_button",button);Bind(root,("Card",view));
        }
        private static void BuildHost(GameObject root,string name)
        {
            var r=(RectTransform)root.transform;r.sizeDelta=new Vector2(480,220);var scroll=root.GetComponent<ScrollRect>();var content=scroll.content;
            UnityEngine.Object.DestroyImmediate(content.GetComponent<VerticalLayoutGroup>());var fitter=content.GetComponent<ContentSizeFitterEx>();
            if(name=="HorizontalListHost") {var layout=content.gameObject.AddComponent<HorizontalLayoutGroup>();layout.spacing=12;layout.padding=new RectOffset(12,12,12,12);layout.childControlWidth=false;layout.childControlHeight=false;layout.childForceExpandWidth=false;layout.childForceExpandHeight=false;fitter.HorizontalFit=ContentSizeFitter.FitMode.PreferredSize;fitter.VerticalFit=ContentSizeFitter.FitMode.Unconstrained;scroll.horizontal=true;scroll.vertical=false;content.anchorMin=Vector2.zero;content.anchorMax=new Vector2(0,1);content.pivot=new Vector2(0,.5f);content.sizeDelta=Vector2.zero;}
            else {var grid=content.gameObject.AddComponent<GridLayoutGroup>();grid.cellSize=new Vector2(150,172);grid.spacing=new Vector2(12,12);grid.padding=new RectOffset(12,12,12,12);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=2;fitter.VerticalFit=ContentSizeFitter.FitMode.PreferredSize;}
        }
        private static void BuildInput(GameObject root)
        {
            var r=(RectTransform)root.transform;r.sizeDelta=new Vector2(320,48);var bg=Surface(r,Color.white,8,true);var outline=root.AddComponent<Outline>();outline.effectColor=new Color(0,0,0,.15f);outline.effectDistance=new Vector2(1,-1);
            var viewport=Rect("Viewport",r,260,32);Stretch(viewport,12,8,48,8);viewport.gameObject.AddComponent<RectMask2D>();
            var text=Text("Text",viewport,"",260,32);Stretch(text.rectTransform);var ph=Text("Placeholder",viewport,"Enter text",260,32);Stretch(ph.rectTransform);ph.color=Muted;
            var input=root.AddComponent<TMP_InputField>();input.targetGraphic=bg;input.textViewport=viewport;input.textComponent=text;input.placeholder=ph;input.fontAsset=_font;input.pointSize=16;
            var clear=IconButton("Clear",r,"Close",136);var error=Text("Error",r,"",320,24,0,-40,12);error.color=new Color32(195,35,48,255);error.gameObject.SetActive(false);
            var actions=root.AddComponent<EUIInputActions>();Wire(actions,"_input",input);Wire(actions,"_clear",clear);Wire(actions,"_error",error);Bind(root,("Input",input),("Actions",actions));
        }
        private static void BuildTip(GameObject root)
        {
            var r=(RectTransform)root.transform;r.sizeDelta=new Vector2(40,40);var trigger=IconButton("Trigger",r,"Help");var panel=Rect("Panel",r,300,150,0,-104);Surface(panel,Color.white,10,true);
            var label=Text("Description",panel,"A short explanation. Tap to open; close when finished.",248,104,-10,-8,15);var close=IconButton("Close",panel,"Close",126,48);
            var tip=root.AddComponent<EUIAnchoredTip>();Wire(tip,"_panel",panel);Wire(tip,"_label",label);Wire(tip,"_trigger",trigger);Wire(tip,"_close",close);panel.gameObject.SetActive(false);Bind(root,("Tip",tip));
        }
        private static void BuildDropdown(GameObject root)
        {
            var r=(RectTransform)root.transform;r.sizeDelta=new Vector2(280,44);var trigger=Button("Trigger",r,"Choose an option",280);Icon("Arrow",trigger.transform,"Chevron",20,116);trigger.Label.rectTransform.sizeDelta=new Vector2(216,36);trigger.Label.rectTransform.anchoredPosition=new Vector2(-16,0);
            var blocker=Button("Blocker",r,"",10000);var br=(RectTransform)blocker.transform;br.sizeDelta=new Vector2(10000,10000);blocker.GetComponent<Image>().color=Color.clear;
            var panel=Rect("Options",r,280,144,0,-98);Surface(panel,Color.white,10,true);var buttons=new Button[3];var labels=new TMP_Text[3];
            for(var i=0;i<3;i++){var b=Button("Option"+i,panel,new[]{"Automatic","Manual","Unavailable"}[i],264,0,48-i*48);buttons[i]=b;labels[i]=b.Label;b.interactable=i<2;}
            var menu=root.AddComponent<EUIStaticChoiceMenu>();Wire(menu,"_trigger",trigger);Wire(menu,"_blocker",blocker);Wire(menu,"_panel",panel);Wire(menu,"_caption",trigger.Label);WireArray(menu,"_options",buttons);WireArray(menu,"_labels",labels);
            panel.gameObject.SetActive(false);blocker.gameObject.SetActive(false);Bind(root,("Selection",menu));
        }
        private static void BuildDialog(GameObject root,bool confirm)
        {
            var center=root.transform.Find("Animator/EUISafeArea/Center");var content=(RectTransform)center.Find("Content");content.sizeDelta=new Vector2(500,230);
            var title=center.Find("Title").GetComponent<TMP_Text>();title.text=confirm?"Confirm action":"Information";
            var close=center.Find("Close").GetComponent<EUIButtonEx>();close.Label.text="";Icon("Icon",close.transform,"Close");
            var viewport=Rect("BodyViewport",content,480,150,0,32);viewport.gameObject.AddComponent<RectMask2D>();Surface(viewport,Color.clear,2,true);
            var body=Text("Body",viewport,"This is a reusable dialog. Content and actions are supplied by the caller.",460,144,0,0,18);body.alignment=TextAlignmentOptions.TopLeft;
            body.rectTransform.anchorMin=new Vector2(0,1);body.rectTransform.anchorMax=Vector2.one;body.rectTransform.pivot=new Vector2(.5f,1);body.rectTransform.sizeDelta=new Vector2(-16,144);body.rectTransform.anchoredPosition=Vector2.zero;
            var fit=body.gameObject.AddComponent<ContentSizeFitterEx>();fit.VerticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=body.rectTransform;scroll.horizontal=false;
            var accept=Button("Accept",content,confirm?"Confirm":"OK",140,confirm?90:0,-92,true);var cancel=Button("Cancel",content,"Cancel",140,-90,-92);cancel.gameObject.SetActive(confirm);
            Bind(root,("Title",title),("Body",body),("Accept",accept),("Cancel",cancel),("Close",close));
        }
        private static void BuildGallery(GameObject root)
        {
            var center=root.transform.Find("Animator/EUISafeArea/Center");center.Find("Title").GetComponent<TMP_Text>().text="Ember • Public components";
            var content=(RectTransform)center.Find("Content");var tabs=Rect("Tabs",content,1000,44,0,230);var group=tabs.gameObject.AddComponent<EUIToggleGroupEx>();
            var panels=new RectTransform[4];var captions=new[]{"Controls","Forms","Feedback","Layout"};
            for(var i=0;i<4;i++){var g=Nest("Tab",tabs,-360+i*240,0,224,40);var t=g.GetComponent<EUIToggleEx>();t.group=group;t.Label.text=captions[i];t.SetIsOnWithoutNotify(i==0);t.Refresh();panels[i]=Rect(captions[i],content,1100,420,0,-12);panels[i].gameObject.SetActive(i==0);}
            var p=panels[0];Nest("SecondaryButton",p,-360,152,220,48);Nest("DestructiveButton",p,-100,152,220,48);Nest("AmountButton",p,180,152);Nest("CloseButton",p,380,152);Nest("BackButton",p,440,152);
            Nest("TabGroup",p,-220,60);Nest("SegmentedControl",p,260,60);Nest("SettingToggleRow",p,-240,-36);Nest("SettingSliderRow",p,240,-48);Nest("Stepper",p,-300,-150);Nest("Badge",p,-100,-150);Nest("ChoiceToggle",p,180,-150);
            p=panels[1];Nest("Input",p,-270,142);Nest("SearchField",p,230,142);Nest("Dropdown",p,-270,44);Nest("Slider",p,250,44);Nest("Switch",p,-410,-55);Text("Hint",p,"Type, clear, choose, drag or step.",600,34,0,-100,20);Nest("TitleBar",p,0,-165);
            p=panels[2];Nest("EmptyState",p,-340,-10);var openConfirm=Button("OpenConfirm",p,"Confirm dialog",200,0,110,true);var openAlert=Button("OpenAlert",p,"Information",200,250,110);var notify=Button("Notify",p,"Show toast",200,0,44);Nest("Tooltip",p,250,44);var notice=Nest("Toast",p,120,-104).GetComponent<EUITimedNotice>();Nest("SectionTitle",p,80,-175);
            p=panels[3];var horizontal=Nest("HorizontalListHost",p,-260,50,480,220);var grid=Nest("GridHost",p,270,15,360,330);
            for(var i=0;i<4;i++){var a=Nest("IconCard",horizontal.transform.Find("Viewport/Content"),0,0);a.name="Card"+i;var b=Nest("IconCard",grid.transform.Find("Viewport/Content"),0,0);b.name="Card"+i;}
            Text("LayoutNote",p,"Static nested cards. No data container or pooling.",800,40,0,-190,16);
            Bind(root,("Tabs",group),("Controls",panels[0]),("Forms",panels[1]),("Feedback",panels[2]),("Layout",panels[3]),("OpenConfirm",openConfirm),("OpenAlert",openAlert),("Notify",notify),("Notice",notice));
        }
        private static void Register(GameObject prefab,string name)
        {
            var lib=EUILibraryService.LoadDefault();var id="ember."+name.ToLowerInvariant();lib.Entries.RemoveAll(e=>e.Id==id);
            var e=new EUILibraryEntry {Id=id,Family=name,Category=name.EndsWith("Page")?"Page":"Component",Prefab=prefab,ReferenceSize=((RectTransform)prefab.transform).sizeDelta};
            foreach(var c in prefab.GetComponentsInChildren<Component>(true))
            {
                if(!(c is TMP_Text)&&!(c is Image)&&!(c is Selectable)&&!(c is ScrollRect)&&!c.GetType().Name.StartsWith("EUI"))continue;
                if(c is EUIBinding || c is EUISafeArea)continue;
                var path=AnimationUtility.CalculateTransformPath(c.transform,prefab.transform);var slot=new EUILibrarySlot {Id=(path.Length==0?"root":path)+"/"+c.GetType().Name,Target=c,ComponentType=c.GetType().FullName,Role=c is TMP_Text?"label":c is Image?"image":c is Selectable?"control":"behaviour",State=c is Selectable?"interactable / pressed / selected":"default",Properties=c is TMP_Text?new[]{"m_text","m_fontColor","m_fontSize"}:c is Image?new[]{"m_Sprite","m_Color"}:c is Selectable?new[]{"m_Interactable"}:Array.Empty<string>()};e.Slots.Add(slot);
            }
            var content=prefab.transform.Find("Viewport/Content");if(content)e.Slots.Add(new EUILibrarySlot{Id="content",Role="content",Target=content,ComponentType="UnityEngine.RectTransform",Properties=new[]{"children"}});
            e.RequiredRoles=e.Slots.Select(s=>s.Role).Distinct().ToArray();lib.Entries.Add(e);EditorUtility.SetDirty(lib);
        }
        #endregion
    }
}



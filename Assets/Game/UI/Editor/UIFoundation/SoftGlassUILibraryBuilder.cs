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
    /// <summary>只首次创建本任务明确的24个目标；再次生成只走公开Binding生成入口。</summary>
    public static class SoftGlassUILibraryBuilder
    {
        #region 内部参数
        public const string ROOT = "Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/";
        private const string PREFIX = "EUISoftGlass";
        private const string LIBRARY = "Assets/Ember/Editor/UILibrary/SoftGlassLibrary.asset";
        private const string RECEIPT = ".utmp/ember-workflow/20261117-soft-glass-ui-gm/evidence/eui-build-receipt.json";
        private static TMP_FontAsset _font;
        private static readonly Color Ink = new Color(.94f, .96f, 1);
        private static readonly Color Muted = new Color(.67f, .74f, .85f);
        private static readonly Color Neutral = new Color(.1f, .14f, .23f, .58f);
        private static readonly Color Accent = new Color(.13f, .4f, .92f, .7f);
        private static readonly string[] Names = {
            "StandardPage", "StandardPopupPage", "PanelItem", "PrimaryButtonItem", "SecondaryButtonItem", "IconButtonItem", "CloseButtonItem",
            "SwitchItem", "SliderItem", "VerticalSliderItem", "InputItem", "SearchItem", "ExtensionExampleItem",
            "GMEntryItem", "GMStateItem", "GMTimeItem", "GMActionItem", "GMTestItem", "GMCenterItem",
            "AlertPage", "ConfirmPage", "ShowcasePage", "ExtensionExamplePage", "GMPage"
        };
        [Serializable] private sealed class Receipt { public string task = "20261117-soft-glass-ui-gm", date; public List<string> preflight = new(), assets = new(), bindings = new(); }
        #endregion

        #region 外部方法
        public static string Preflight()
        {
            if (!EmberProjectSetup.IsTemplateActive("base")) throw new InvalidOperationException("需要正式活动base或合法派生身份。");
            return string.Join("\n", Names.Select(n => {
                if (!EUICreationService.TryBuildPlan(Request(n), out var plan, out var result)) throw new InvalidOperationException(n + ": " + result.Error);
                return plan.PrefabPath;
            }));
        }

        public static string Build(int stage = 0, bool resume = false)
        {
            if (stage < 0 || stage > 2) throw new ArgumentOutOfRangeException(nameof(stage));
            var preflight = stage == 0 && !resume ? Preflight() : "See initial preflight and task receipt";
            if (AssetDatabase.LoadAssetAtPath<EUILibrary>(LIBRARY)) throw new InvalidOperationException("SoftGlassLibrary已存在；拒绝整体重建。");
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/GameResource/Resources/UI/Common/Fonts/DefaultLight/DefaultLightSans.asset");
            if (!_font) throw new InvalidOperationException("项目既有DefaultLightSans字体缺失。");
            var receipt = stage == 0 && !resume ? new Receipt { date = DateTime.UtcNow.ToString("O"), preflight = preflight.Split('\n').ToList() } : JsonUtility.FromJson<Receipt>(File.ReadAllText(RECEIPT));
            if (receipt.task != "20261117-soft-glass-ui-gm") throw new InvalidOperationException("Receipt ownership mismatch");
            var batch = stage == 0 ? Names.Take(18) : stage == 1 ? Names.Skip(18).Take(5) : Names.Skip(23);
            foreach (var name in batch)
            {
                if (receipt.assets.Contains(PathFor(name))) continue;
                var request = Request(name);
                string sourceName = name == "SecondaryButtonItem" || name == "ExtensionExampleItem" ? "PrimaryButtonItem" : name == "CloseButtonItem" ? "IconButtonItem" : name == "SearchItem" ? "InputItem" : null;
                if (sourceName != null) request.SourceSkeleton = AssetDatabase.LoadAssetAtPath<GameObject>(PathFor(sourceName));
                var result = EUICreationService.Create(request);
                if (!result.Success) throw new InvalidOperationException(result.Error);
                receipt.assets.Add(result.PrefabPath);
                Edit(result.PrefabPath, root => { if (sourceName != null) Variant(root, name); else Decorate(root, name); });
                if (!request.StructureOnly) Regenerate(result.PrefabPath);
                receipt.bindings.Add(result.PrefabPath + " : " + JsonUtility.ToJson(EUIContractService.Export(result.PrefabPath)));
                Directory.CreateDirectory(Path.GetDirectoryName(RECEIPT)); File.WriteAllText(RECEIPT, JsonUtility.ToJson(receipt, true));
            }
            if (stage < 2) return "Stage " + stage + " assets created. Explicit compilation required before next stage. Receipt: " + RECEIPT;
            SoftGlassUIRevisionTwo.Apply();
            var library = ScriptableObject.CreateInstance<EUILibrary>(); library.SkinId = "EmberSoftGlass"; library.SkinVersion = "1.0.0";
            foreach (var name in Names) library.Entries.Add(Entry(AssetDatabase.LoadAssetAtPath<GameObject>(PathFor(name))));
            AssetDatabase.CreateAsset(library, LIBRARY); AssetDatabase.SaveAssets();
            var errors = EUILibraryService.Validate(library); if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            EUILibraryService.GenerateCatalog(library, "Assets/Ember/Editor/UILibrary/SoftGlassLibrary.catalog.json");
            return JsonUtility.ToJson(receipt, true);
        }

        public static string RegenerateAll()
        {
            foreach (var name in Names.Where(n => n != "StandardPage" && n != "StandardPopupPage")) Regenerate(PathFor(name));
            return "Regenerated 22 bindings through EUI development center";
        }
        public static string RefreshLibrary()
        {
            var library=AssetDatabase.LoadAssetAtPath<EUILibrary>(LIBRARY);
            if(!library || library.SkinId!="EmberSoftGlass")throw new InvalidOperationException("Task library missing");
            library.Entries.Clear();
            foreach(var name in Names)library.Entries.Add(Entry(AssetDatabase.LoadAssetAtPath<GameObject>(PathFor(name))));
            EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
            var errors=EUILibraryService.Validate(library);
            if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));
            EUILibraryService.GenerateCatalog(library,"Assets/Ember/Editor/UILibrary/SoftGlassLibrary.catalog.json");
            return "Refreshed 24 existing entries and current slots; GUIDs preserved";
        }
        #endregion

        #region 内部方法
        private static string PathFor(string name) => ROOT + PREFIX + name + ".prefab";
        private static EUICreationRequest Request(string name) => new() {
            PrefabName = PREFIX + name, ClassName = PREFIX + name, PageName = PREFIX + name, ClassPath = "Module/UIFoundation",
            Role = name.EndsWith("Item") ? EUIBindingRole.Item : EUIBindingRole.Page,
            StructureOnly = name == "StandardPage" || name == "StandardPopupPage", CodePathMode = EUIBinding.CodePathMode.Business,
            PageType = name == "GMPage" ? PageType.FreePage : name.Contains("Popup") || name == "AlertPage" || name == "ConfirmPage" ? PageType.Popup : PageType.MainPage,
            FixedSortingOrder = 30000, GenerateCustomSettings = false, UseUIUpdate = name == "GMPage" || name == "ShowcasePage",
            MaskEffect = EUIMaskEffect.Inherit, OverrideMaskTint = false, ReferenceResolution = new Vector2(1280, 720),
            TransitionMode = EUIBinding.RegularTransitionMode.PresetFade, FadeInTime = .16f, FadeOutTime = .12f
        };

        private static void Edit(string path, Action<GameObject> action)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.SetActive(false); action(root);
                foreach (var c in root.GetComponentsInChildren<Component>(true))
                    if (c && PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                root.SetActive(true); PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void Regenerate(string path)
        {
            var binding = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<EUIBinding>();
            if (!EUIBindingCodeGenUtility.TryRegenerateCode(binding, out var error)) throw new InvalidOperationException(error);
        }
        private static RectTransform Rect(string name, Transform parent, float w, float h, float x = 0, float y = 0)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = LayerMask.NameToLayer("UI");
            var r = (RectTransform)go.transform; r.SetParent(parent, false); r.sizeDelta = new Vector2(w, h); r.anchoredPosition = new Vector2(x, y); return r;
        }
        private static void Stretch(RectTransform r, float inset = 0) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.one * inset; r.offsetMax = -Vector2.one * inset; }
        private static EUIGlassImage Glass(RectTransform r, Color tint, float radius = 20, bool raycast = false)
        { var g = r.gameObject.AddComponent<EUIGlassImage>(); g.color = tint; g.Radius = radius; g.raycastTarget = raycast; return g; }
        private static TMPEx Text(string name, Transform parent, string text, float w, float h, float x = 0, float y = 0, float size = 26)
        {
            var r = Rect(name, parent, w, h, x, y); var t = r.gameObject.AddComponent<TMPEx>();
            t.font = _font; t.text = text; t.fontSize = size; t.color = Ink; t.alignment = TextAlignmentOptions.MidlineLeft;
            t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; return t;
        }
        private static EUIButtonEx Button(string name, Transform parent, string label, float w, float h, float x = 0, float y = 0, bool primary = false, float radius = 20)
        {
            var r = Rect(name, parent, w, h, x, y); var glass = Glass(r, primary ? Accent : Neutral, radius, true);
            var button = r.gameObject.AddComponent<EUIButtonEx>(); button.targetGraphic = glass;
            button.Label = Text("Label", r, label, w - 24, h - 8, 0, 0, 24); button.Label.alignment = TextAlignmentOptions.Center;
            var colors = ColorBlock.defaultColorBlock; colors.highlightedColor = new Color(1.12f, 1.14f, 1.18f); colors.pressedColor = new Color(.72f, .8f, .96f);
            colors.selectedColor = new Color(.82f, .92f, 1); colors.disabledColor = new Color(.55f, .6f, .7f, .65f); colors.fadeDuration = .08f; button.colors = colors;
            return button;
        }
        private static void Bind(GameObject root, params (string name, Component target)[] entries)
        {
            EUIBindingEditorUtility.SetBindings(root.GetComponent<EUIBinding>(), entries.Select(e => new EUIBindingEntrySnapshot {
                Name = e.name, GameObjectPath = AnimationUtility.CalculateTransformPath(e.target.transform, root.transform),
                WidgetType = e.target is EUIBinding ? EUIBinding.WidgetTypes.UILogic : EUIBinding.WidgetTypes.Extension,
                ClassName = e.target is EUIBinding child ? child.ClassName : e.target.GetType().FullName
            }).ToList());
        }
        private static GameObject Nest(string name, Transform parent, float w, float h, float x = 0, float y = 0)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(PathFor(name));
            if (!source) throw new InvalidOperationException("Missing prerequisite " + name);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(source, parent); var r = (RectTransform)go.transform;
            r.sizeDelta = new Vector2(w, h); r.anchoredPosition = new Vector2(x, y); return go;
        }

        private static void Decorate(GameObject root, string name)
        {
            var r = (RectTransform)root.transform; r.sizeDelta = new Vector2(360, 72);
            if (name.EndsWith("Page")) { Page(root, name); return; }
            if (name == "GMActionItem")
            {
                r.sizeDelta = new Vector2(152,132);
                var actionButton = Button("Action",r,"",80,80,0,20,false,40);
                var glyph=Rect("Glyph",actionButton.transform,40,40).gameObject.AddComponent<EUIControlGlyph>();glyph.raycastTarget=false;
                var actionLabel=Text("Label",r,"Action",148,36,0,-44,20);actionLabel.alignment=TextAlignmentOptions.Center;
                Bind(root,("Button",actionButton),("Label",actionLabel),("Icon",actionButton.Label),("Glyph",glyph),("Surface",actionButton.GetComponent<EUIGlassImage>()));return;
            }
            if (name.Contains("ButtonItem") || name == "ExtensionExampleItem" || name == "GMActionItem")
            {
                bool circle = name == "IconButtonItem" || name == "CloseButtonItem" || name == "GMActionItem";
                r.sizeDelta = new Vector2(circle ? 80 : 320, 80);
                var surface = Glass(r, name == "PrimaryButtonItem" ? Accent : Neutral, circle ? 40 : 20, true);
                var button = root.AddComponent<EUIButtonEx>(); button.targetGraphic = surface;
                var label = Text("Label", r, name == "CloseButtonItem" ? "×" : circle ? "+" : name == "ExtensionExampleItem" ? "Custom action" : name == "PrimaryButtonItem" ? "Continue" : "Details", circle ? 70 : 272, 64);
                label.alignment = TextAlignmentOptions.Center; button.Label = label;
                var icon = Text("Icon", r, "◇", 40, 40, -112); icon.gameObject.SetActive(false);
                Bind(root, ("Button", button), ("Label", label), ("Icon", icon), ("Surface", surface)); return;
            }
            if (name == "SwitchItem")
            {
                Glass(r, Neutral, 20, true); var toggle = root.AddComponent<EUIToggleEx>(); toggle.targetGraphic = root.GetComponent<EUIGlassImage>();
                var label = Text("Label", r, "Enhanced option", 240, 64, -40); toggle.Label = label;
                var track = Rect("Track", r, 80, 44, 120); Glass(track, new Color(.15f,.4f,.9f,.8f),22);
                var on = Rect("On", track, 32,32,18); var off = Rect("Off",track,32,32,-18);
                Glass(on,new Color(.9f,.95f,1,1),16); Glass(off,new Color(.6f,.68f,.8f,1),16);
                toggle.OnNode = on.gameObject; toggle.OffNode = off.gameObject; toggle.isOn = false; toggle.Refresh();
                Bind(root,("Toggle",toggle),("Label",label)); return;
            }
            if (name == "SliderItem" || name == "VerticalSliderItem") { Slider(root, name == "VerticalSliderItem"); return; }
            if (name == "InputItem" || name == "SearchItem") { Input(root, name == "SearchItem"); return; }
            if (name == "GMEntryItem")
            {
                r.sizeDelta = new Vector2(64,64); var bg = Glass(r,new Color(.2f,.25f,.34f,.62f),32,true);
                var ring = Rect("Ring",r,28,28); Glass(ring,new Color(.92f,.96f,1,.85f),14);
                var dot = Rect("Dot",ring,18,18); Glass(dot,Neutral,9);
                var button = root.AddComponent<EUIButtonEx>(); button.targetGraphic = bg;
                var floating = root.AddComponent<EUIFloatingEntry>(); Bind(root,("Button",button),("Floating",floating)); return;
            }
            if (name == "GMStateItem")
            {
                r.sizeDelta = new Vector2(340,300); Glass(r,Neutral,32);
                Text("Eyebrow",r,"RUNTIME STATUS",280,32,0,105,18).color=Muted;
                var state=Text("State",r,"—",280,70,0,45,36);
                var unity=Text("UnityTime",r,"Unity  1.00×",280,42,0,-35,24);
                var ember=Text("EmberTime",r,"Ember  1.00×",280,42,0,-84,24);
                Bind(root,("State",state),("UnityTime",unity),("EmberTime",ember)); return;
            }
            if (name == "GMTimeItem")
            {
                r.sizeDelta=new Vector2(340,380); Glass(r,Neutral,32); Text("Title",r,"Time control",280,44,0,146,28);
                var toggle=Nest("SwitchItem",r,280,64,0,80).GetComponent<EUIToggleEx>(); toggle.Label.text="Enable";
                var slider=Nest("VerticalSliderItem",r,96,190,90,-55).GetComponent<Slider>(); slider.minValue=.1f;slider.maxValue=3;slider.value=1;
                var value=Text("Value",r,"1.00×",160,64,-65,-35,38);
                var reset=Button("Reset",r,"1×",120,64,-65,-112);
                Bind(root,("Enable",toggle),("Slider",slider),("Value",value),("Reset",reset));return;
            }
            if (name == "GMTestItem") { Tests(root);return; }
            if (name == "GMCenterItem") { Center(root);return; }
            r.sizeDelta = new Vector2(460,300); var panel=Glass(r,Neutral,32);
            var header=Text("Header",r,"Glass panel",400,50,0,100,30);
            header.rectTransform.anchorMin=new Vector2(0,1);header.rectTransform.anchorMax=Vector2.one;header.rectTransform.offsetMin=new Vector2(24,-74);header.rectTransform.offsetMax=new Vector2(-24,-24);
            var content=Rect("Content",r,412,200,0,-25);Bind(root,("Surface",panel),("Header",header),("Content",content));
        }

        private static void Variant(GameObject root, string name)
        {
            if (name == "SearchItem")
            {
                root.GetComponent<TMP_InputField>().placeholder.GetComponent<TMP_Text>().text = "Search controls";
                root.GetComponent<EUIGlassImage>().Radius = 36;
                return;
            }
            var button = root.GetComponent<EUIButtonEx>();
            button.Label.text = name == "CloseButtonItem" ? "×" : name == "ExtensionExampleItem" ? "Custom action" : "Details";
            root.GetComponent<EUIGlassImage>().color = name == "ExtensionExampleItem" ? new Color(.08f,.48f,.4f,.7f) : Neutral;
        }

        private static void Slider(GameObject root, bool vertical)
        {
            var r=(RectTransform)root.transform;r.sizeDelta=vertical?new Vector2(96,240):new Vector2(360,72);
            var track=Rect("Track",r,vertical?64:340,vertical?216:16);Glass(track,new Color(.12f,.18f,.29f,.65f),32);
            var area=Rect("FillArea",track,1,1);Stretch(area,4);var fill=Rect("Fill",area,1,1);Stretch(fill);Glass(fill,new Color(.72f,.84f,1,.8f),28);
            var handleArea=Rect("HandleArea",track,1,1);Stretch(handleArea,8);var handle=Rect("Handle",handleArea,vertical?72:32,32);var h=Glass(handle,new Color(.94f,.97f,1,1),16,true);
            if(vertical)handle.sizeDelta=new Vector2(0,24);
            var slider=root.AddComponent<Slider>();slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=h;slider.direction=vertical?UnityEngine.UI.Slider.Direction.BottomToTop:UnityEngine.UI.Slider.Direction.LeftToRight;slider.minValue=0;slider.maxValue=1;slider.value=.62f;
            var value=Text("Value",r,"62%",80,40,vertical?0:140,vertical?-140:-30,18);value.color=Muted;
            Bind(root,("Slider",slider),("Value",value));
        }
        private static void Input(GameObject root,bool search)
        {
            var r=(RectTransform)root.transform;r.sizeDelta=new Vector2(360,72);var surface=Glass(r,Neutral,search?36:20,true);
            var viewport=Rect("Viewport",r,280,56,-16);viewport.gameObject.AddComponent<RectMask2D>();
            var text=Text("Text",viewport,"",270,54);var placeholder=Text("Placeholder",viewport,search?"Search controls":"Enter a value",270,54);placeholder.color=Muted;
            var input=root.AddComponent<TMP_InputField>();input.targetGraphic=surface;input.textViewport=viewport;input.textComponent=text;input.placeholder=placeholder;input.fontAsset=_font;input.pointSize=24;input.caretColor=Ink;input.customCaretColor=true;
            var clear=Button("Clear",r,"×",48,48,148);var error=Text("Error",r,"",340,32,0,-56,18);error.color=new Color(1,.55f,.5f);error.gameObject.SetActive(false);
            var actions=root.AddComponent<EUIInputActions>();using(var so=new SerializedObject(actions)){so.FindProperty("_input").objectReferenceValue=input;so.FindProperty("_clear").objectReferenceValue=clear;so.FindProperty("_error").objectReferenceValue=error;so.ApplyModifiedPropertiesWithoutUndo();}
            Bind(root,("Input",input),("Actions",actions));
        }
        private static void Tests(GameObject root)
        {
            var r=(RectTransform)root.transform;r.sizeDelta=new Vector2(340,380);Glass(r,Neutral,32);Text("Title",r,"Component tests",280,44,0,145,28);
            var toggle=Nest("SwitchItem",r,280,64,0,80).GetComponent<EUIToggleEx>();toggle.Label.text="Enhanced toggle";
            var viewport=Rect("Viewport",r,280,160,0,-70);viewport.gameObject.AddComponent<RectMask2D>();var content=Rect("Content",viewport,280,300,0,-70);
            var scroll=root.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;
            var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameResource/Resources/UI/Common/Prefabs/GMPage.prefab").GetComponent<EUIBinding>();
            int index=0;foreach(var key in new[]{"Img_Test","Raw_Test","EUIImg_Test","Img_Circle"})
            {
                var source=original.Bindings.FirstOrDefault(e=>e.Name==key).GameObject;if(!source)continue;
                var go=UnityEngine.Object.Instantiate(source,content,false);go.name=key;var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2((index%2-.5f)*130,90-(index/2)*125);rect.sizeDelta=new Vector2(104,104);rect.localScale=Vector3.one;index++;
            }
            Bind(root,("Enhanced",toggle),("Scroll",scroll),("Content",content));
        }
        private static void Center(GameObject root)
        {
            var r=(RectTransform)root.transform;r.sizeDelta=new Vector2(1200,660);var bg=Glass(r,new Color(.05f,.08f,.14f,.38f),48,true);
            var title=Text("Title",r,"Control Center",700,64,-160,272,38);var close=Button("Close",r,"×",64,64,520,272,false,32);
            title.rectTransform.anchorMin=new Vector2(0,1);title.rectTransform.anchorMax=Vector2.one;title.rectTransform.offsetMin=new Vector2(32,-88);title.rectTransform.offsetMax=new Vector2(-112,-24);
            close.GetComponent<RectTransform>().anchorMin=close.GetComponent<RectTransform>().anchorMax=Vector2.one;close.GetComponent<RectTransform>().anchoredPosition=new Vector2(-64,-56);
            var viewport=Rect("Viewport",r,1100,500,0,-32);viewport.gameObject.AddComponent<RectMask2D>();Stretch(viewport,28);viewport.offsetMax=new Vector2(-28,-112);
            var content=Rect("Content",viewport,1100,700);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.anchoredPosition=Vector2.zero;content.sizeDelta=new Vector2(0,700);
            var scroll=root.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;
            var state=Nest("GMStateItem",content,340,380,-360,-200);var time=Nest("GMTimeItem",content,340,380,0,-200);var tests=Nest("GMTestItem",content,340,380,360,-200);
            var actions=Rect("Actions",content,1040,160,0,-510);var grid=actions.gameObject.AddComponent<GridLayoutGroup>();grid.cellSize=new Vector2(152,132);grid.spacing=new Vector2(16,16);grid.childAlignment=TextAnchor.MiddleCenter;
            var adaptive=content.gameObject.AddComponent<EUIResponsiveCardGrid>();using(var so=new SerializedObject(adaptive)){var cards=so.FindProperty("_cards");cards.arraySize=3;cards.GetArrayElementAtIndex(0).objectReferenceValue=state.transform;cards.GetArrayElementAtIndex(1).objectReferenceValue=time.transform;cards.GetArrayElementAtIndex(2).objectReferenceValue=tests.transform;so.FindProperty("_footer").objectReferenceValue=actions;so.ApplyModifiedPropertiesWithoutUndo();}
            Bind(root,("Close",close),("Title",title),("State",state.GetComponent<EUIBinding>()),("Time",time.GetComponent<EUIBinding>()),("Tests",tests.GetComponent<EUIBinding>()),("Actions",actions),("Scroll",scroll),("Content",content));
        }
        private static void Page(GameObject root,string name)
        {
            var center=root.transform.Find("Animator/EUISafeArea/Center");if(!center)throw new InvalidOperationException("EUI center skeleton missing");
            for(int i=center.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(center.GetChild(i).gameObject);
            root.AddComponent<EUIBackdropGroup>();
            if(name=="GMPage")
            {
                var entry=Nest("GMEntryItem",center,64,64,560,130);
                var host=Rect("CenterHost",center,1200,660);Stretch(host,24);
                var panel=Nest("GMCenterItem",host,1200,660);Stretch((RectTransform)panel.transform);host.gameObject.SetActive(false);
                Bind(root,("Entry",entry.GetComponent<EUIBinding>()),("Center",panel.GetComponent<EUIBinding>()),("CenterHost",host));return;
            }
            bool popup=name.Contains("Popup")||name=="AlertPage"||name=="ConfirmPage";
            if(popup)
            {
                var panel=Rect("Panel",center,620,440);Glass(panel,new Color(.08f,.12f,.21f,.72f),48,true);
                var title=Text("Title",panel,"A quiet moment",500,64,-14,150,32);
                var viewport=Rect("BodyViewport",panel,520,180,0,10);viewport.gameObject.AddComponent<RectMask2D>();
                var body=Text("Body",viewport,"Your scene and lower UI stay softly blurred. This text stays clear.",500,180,0,0,26);
                var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=body.rectTransform;scroll.horizontal=false;
                var accept=Button("Accept",panel,"Continue",244,72,name=="ConfirmPage"?130:0,-154,true);
                var cancel=Button("Cancel",panel,"Cancel",244,72,-130,-154);cancel.gameObject.SetActive(name=="ConfirmPage");
                var close=Button("Close",panel,"×",64,64,254,158,false,32);
                if(name!="StandardPopupPage")Bind(root,("Title",title),("Body",body),("Accept",accept),("Cancel",cancel),("Close",close),("BodyScroll",scroll));return;
            }
            if(name=="StandardPage")
            {
                Text("Header",center,"Page title",1000,72,0,280,38);
                var slot=Rect("Content",center,1160,480,0,-30);
                return;
            }
            var background=Rect("Backdrop",center,1280,720);Stretch(background);var backgroundCanvas=background.gameObject.AddComponent<Canvas>();backgroundCanvas.overrideSorting=true;backgroundCanvas.sortingOrder=998;
            for(int i=0;i<4;i++){var color=Color.HSVToRGB(.52f+i*.1f,.62f,.46f);var block=Rect("Color"+i,background,400,800,-480+i*320,0);var image=block.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;}
            var titleText=Text("Title",center,name=="ExtensionExamplePage"?"Make it yours":"Soft Glass",840,76,-160,280,48);
            titleText.rectTransform.anchorMin=titleText.rectTransform.anchorMax=new Vector2(0,1);titleText.rectTransform.pivot=new Vector2(0,1);titleText.rectTransform.anchoredPosition=new Vector2(32,-28);titleText.rectTransform.sizeDelta=new Vector2(300,76);
            var viewportRoot=Rect("Viewport",center,1180,480);Stretch(viewportRoot,32);viewportRoot.offsetMin=new Vector2(32,104);viewportRoot.offsetMax=new Vector2(-32,-128);viewportRoot.gameObject.AddComponent<RectMask2D>();
            var contentRoot=Rect("Content",viewportRoot,1180,480);contentRoot.anchorMin=new Vector2(0,1);contentRoot.anchorMax=Vector2.one;contentRoot.pivot=new Vector2(.5f,1);contentRoot.anchoredPosition=Vector2.zero;contentRoot.sizeDelta=new Vector2(0,480);var flow=contentRoot.gameObject.AddComponent<EUIResponsiveFlowLayout>();using(var so=new SerializedObject(flow)){so.FindProperty("_minimumCellWidth").floatValue=340;so.ApplyModifiedPropertiesWithoutUndo();}
            var galleryScroll=viewportRoot.gameObject.AddComponent<ScrollRect>();galleryScroll.viewport=viewportRoot;galleryScroll.content=contentRoot;galleryScroll.horizontal=false;
            var names=name=="ExtensionExamplePage"?new[]{"ExtensionExampleItem","PanelItem"}:new[]{"PrimaryButtonItem","SecondaryButtonItem","SwitchItem","SliderItem","InputItem","SearchItem","PanelItem","IconButtonItem","VerticalSliderItem"};
            var nodes=new List<GameObject>();for(int i=0;i<names.Length;i++){float height=names[i]=="PanelItem"?200:names[i]=="VerticalSliderItem"?240:names[i]=="IconButtonItem"?80:96;var node=Nest(names[i],contentRoot,340,height);var layout=node.AddComponent<LayoutElement>();layout.preferredHeight=height;if(names[i]=="IconButtonItem")layout.preferredWidth=80;if(names[i]=="VerticalSliderItem")layout.preferredWidth=96;nodes.Add(node);}
            var popupButton=Button("ShowPopup",center,"Open confirmation",300,64,440,282,true);
            popupButton.GetComponent<RectTransform>().anchorMin=popupButton.GetComponent<RectTransform>().anchorMax=Vector2.one;popupButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(-182,-64);
            var mode=Button("BackgroundMode",center,"Background  ◇",240,60,-450,-300);
            mode.GetComponent<RectTransform>().anchorMin=mode.GetComponent<RectTransform>().anchorMax=Vector2.zero;mode.GetComponent<RectTransform>().anchoredPosition=new Vector2(152,48);
            var dynamicHost=Rect("DynamicHost",contentRoot,340,100,0,-200);
            if(name!="StandardPage")Bind(root,("Title",titleText),("ShowPopup",popupButton),("BackgroundMode",mode),("Content",contentRoot),("Backdrop",background),("DynamicHost",dynamicHost),("StaticExample",nodes[0].GetComponent<EUIBinding>()));
        }
        private static EUILibraryEntry Entry(GameObject prefab)
        {
            var result=new EUILibraryEntry{Id="ember.softglass."+prefab.name.Substring(PREFIX.Length).ToLowerInvariant(),Family="SoftGlass",Category=prefab.name.EndsWith("Item")?"Component":"Page",Prefab=prefab,AutoMatch=false,ReferenceSize=((RectTransform)prefab.transform).sizeDelta,AllowedOverrides=new[]{"root.position","root.size","slot.text","slot.tint","slot.radius"}};
            var targets=new List<Component>();targets.AddRange(prefab.GetComponentsInChildren<TMP_Text>(true));targets.AddRange(prefab.GetComponentsInChildren<EUIGlassImage>(true));targets.AddRange(prefab.GetComponentsInChildren<Selectable>(true));targets.AddRange(prefab.GetComponentsInChildren<EUIControlGlyph>(true));
            for(int i=0;i<targets.Count;i++){var t=targets[i];result.Slots.Add(new EUILibrarySlot{Id="slot-"+i,Role=t is TMP_Text?"text":t is EUIGlassImage?"surface":t is EUIControlGlyph?"icon":"control",Target=t,ComponentType=t.GetType().FullName,Properties=t is TMP_Text?new[]{"m_text","m_fontColor","m_fontSize"}:t is EUIGlassImage?new[]{"m_Color","_radius"}:t is EUIControlGlyph?new[]{"_key","m_Color"}:new[]{"m_Interactable"},State=t is Selectable?"normal / pressed / selected / disabled":"default"});}
            result.RequiredRoles=result.Slots.Select(s=>s.Role).Distinct().ToArray();return result;
        }
        #endregion
    }
}



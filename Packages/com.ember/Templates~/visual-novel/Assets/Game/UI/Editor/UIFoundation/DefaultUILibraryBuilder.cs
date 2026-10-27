// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
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
    /// <summary>base 默认库的显式首次制作入口。已有资产拒绝覆盖；不会在域重载时执行。</summary>
    public static class DefaultUILibraryBuilder
    {
        #region 内部参数
        private const string Common = "Assets/GameResource/Resources/UI/Common/Prefabs/";
        private const string Module = "Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/";
        private const string Atlas = "Assets/GameResource/Resources/UI/Common/Atlas/DefaultLight/";
        private const string Background = "Assets/GameResource/Resources/UI/Common/Textures/DefaultLight/Background.png";
        private static TMP_FontAsset _font;
        private static Sprite _round;
        private static readonly Color Ink = new Color32(29,29,31,255);
        private static readonly Color Blue = new Color32(10,100,232,255);
        private static readonly Color Line = new Color32(213,216,222,255);
        #endregion

        #region 外部方法
        public static string Build()
        {
            if (!EmberProjectSetup.IsTemplateActive("base")) throw new InvalidOperationException("仅适用于活动 base 体系。");
            var names = new[] { "EUIStandardPage", "EUIStandardPopupPage", "EUIActionButtonItem", "EUIChoiceToggleItem", "EUIProgressItem", "EUIContentHostItem", "EUIOptionRowItem", "EUILibraryPreviewPage", "EUILibraryPopupPage" };
            var requests = names.Select(name => Request(name)).ToArray();
            foreach (var request in requests)
                if (!EUICreationService.TryBuildPlan(request, out _, out var preflight)) throw new InvalidOperationException(preflight.Error);
            if (AssetDatabase.LoadAssetAtPath<EUILibrary>(EUILibraryService.DefaultPath)) throw new InvalidOperationException("默认库已存在；不重复制作。");
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/GameResource/Resources/UI/Common/Fonts/DefaultLight/DefaultLightSans.asset");
            ImportImage(Background, false);
            ImportImage(Atlas + "EmptyState.png", true);
            MakeRound();
            var paths = new List<string>();
            foreach (var request in requests.Take(7))
            {
                var created = EUICreationService.Create(request);
                if (!created.Success) throw new InvalidOperationException(created.Error);
                paths.Add(created.PrefabPath);
                Edit(created.PrefabPath, root => Decorate(root, request.PrefabName));
                if (!request.StructureOnly) Regenerate(created.PrefabPath);
            }
            var library = ScriptableObject.CreateInstance<EUILibrary>();
            foreach (var path in paths) library.Entries.Add(MakeEntry(AssetDatabase.LoadAssetAtPath<GameObject>(path)));
            Directory.CreateDirectory(Path.GetDirectoryName(EUILibraryService.DefaultPath));
            AssetDatabase.CreateAsset(library, EUILibraryService.DefaultPath);
            foreach (var request in requests.Skip(7))
            {
                request.SourceSkeleton = AssetDatabase.LoadAssetAtPath<GameObject>(Common + (request.PrefabName.Contains("Popup") ? "EUIStandardPopupPage" : "EUIStandardPage") + ".prefab");
                request.ContentSlotPath = "Animator/EUISafeArea/Center/Content";
                var created = EUICreationService.Create(request);
                if (!created.Success) throw new InvalidOperationException(created.Error);
                paths.Add(created.PrefabPath);
                Edit(created.PrefabPath, root => Preview(root, request.PrefabName.Contains("Popup")));
                Regenerate(created.PrefabPath);
            }
            AssetDatabase.SaveAssets();
            var errors = EUILibraryService.Validate(library);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            EUILibraryService.GenerateCatalog(library);
            return string.Join("\n", paths);
        }

        public static string AdaptMain()
        {
            if (!EmberProjectSetup.IsTemplateActive("base")) throw new InvalidOperationException("需要 base。");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Common + "EUIActionButtonItem.prefab");
            Edit(Common + "EUIMainPage.prefab", root => {
                var binding = root.GetComponent<EUIBinding>();
                var snapshot = EUIBindingEditorUtility.GetBindingSnapshot(binding);
                foreach (var name in new[] { "Btn_Start", "Btn_Settings" })
                {
                    var entry = snapshot.Entries.Single(e => e.Name == name);
                    var old = root.transform.Find(entry.GameObjectPath);
                    if (PrefabUtility.GetCorrespondingObjectFromSource(old.gameObject) == source) continue;
                    var rect = (RectTransform)old;
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, old.parent);
                    instance.name = old.name;
                    var target = (RectTransform)instance.transform;
                    target.SetSiblingIndex(old.GetSiblingIndex());
                    target.anchorMin = rect.anchorMin; target.anchorMax = rect.anchorMax; target.pivot = rect.pivot;
                    target.anchoredPosition = rect.anchoredPosition; target.sizeDelta = new Vector2(Mathf.Max(320, rect.sizeDelta.x), Mathf.Max(88, rect.sizeDelta.y));
                    target.localScale = rect.localScale;
                    var button = instance.GetComponent<EUIButtonEx>();
                    button.Label.text = old.GetComponentInChildren<TMP_Text>(true).text;
                    button.Label.fontSize = 30;
                    if (name == "Btn_Settings") { instance.GetComponent<Image>().color = Color.white; button.Label.color = Ink; }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(button.Label);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(instance.GetComponent<Image>());
                    UnityEngine.Object.DestroyImmediate(old.gameObject);
                }
                EUIBindingEditorUtility.ApplyBindingSnapshot(binding, snapshot);
            });
            Regenerate(Common + "EUIMainPage.prefab");
            return "主界面两个按钮已嵌套复用；原绑定快照和 Framework 标记保持。";
        }
        #endregion

        #region 内部方法
        private static EUICreationRequest Request(string name)
        {
            var isPage = name.EndsWith("Page", StringComparison.Ordinal);
            var generated = name == "EUIOptionRowItem" || name.StartsWith("EUILibrary", StringComparison.Ordinal);
            return new EUICreationRequest { PrefabName = name, ClassName = name, PageName = name,
                ClassPath = "Module/UIFoundation", Role = isPage ? EUIBindingRole.Page : EUIBindingRole.Item,
                CodePathMode = generated ? EUIBinding.CodePathMode.Business : EUIBinding.CodePathMode.Framework,
                StructureOnly = !generated, PageType = name.Contains("Popup") ? PageType.Popup : PageType.MainPage,
                ReferenceResolution = new Vector2(1280,720), UseMask = name.Contains("Popup"), ClickMaskToClose = true };
        }

        private static void Edit(string path, Action<GameObject> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try { edit(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void Regenerate(string path)
        {
            var binding = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<EUIBinding>();
            if (!EUIBindingCodeGenUtility.TryRegenerateCode(binding, out var error)) throw new InvalidOperationException(error);
        }
        private static void ImportImage(string path, bool alpha)
        {
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = alpha; importer.mipmapEnabled = false; importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100; importer.SaveAndReimport();
        }
        private static void MakeRound()
        {
            // 确定性可伸缩几何，不替代背景/插画的 imagegen 素材。
            var path = Atlas + "RoundedSurface.png";
            var texture = new Texture2D(64,64,TextureFormat.RGBA32,false);
            for (var y=0;y<64;y++) for (var x=0;x<64;x++)
            {
                var dx = Mathf.Max(Mathf.Abs(x-31.5f)-15.5f,0); var dy = Mathf.Max(Mathf.Abs(y-31.5f)-15.5f,0);
                texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(16-Mathf.Sqrt(dx*dx+dy*dy))));
            }
            texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            ImportImage(path,true);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.spriteBorder=new Vector4(18,18,18,18); importer.SaveAndReimport();
            _round=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static RectTransform Rect(string name, Transform parent, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform; rect.SetParent(parent,false); rect.sizeDelta = new Vector2(w,h); return rect;
        }
        private static void Stretch(RectTransform rect, float inset=0)
        { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=Vector2.one*inset; rect.offsetMax=-Vector2.one*inset; }
        private static Image Surface(RectTransform rect, Color color, bool raycast=false)
        {
            var image=rect.gameObject.AddComponent<EUIImageEx>(); image.sprite=_round; image.type=Image.Type.Sliced; image.color=color;
            image.raycastTarget=raycast; return image;
        }
        private static TMPEx Text(string name, Transform parent, string value, float size=16)
        {
            var rect=Rect(name,parent,240,44); Stretch(rect,12);
            var text=rect.gameObject.AddComponent<TMPEx>(); text.font=_font; text.SetFontForSkin(4,name=="Title"?2:1); text.SetFontSkin(4,name=="Title"?2:1); text.text=value; text.fontSize=size;
            text.color=Ink; text.alignment=TextAlignmentOptions.MidlineLeft; text.raycastTarget=false;
            text.textWrappingMode=TextWrappingModes.Normal; text.overflowMode=TextOverflowModes.Overflow;
            return text;
        }
        private static void Bind(GameObject root, params (string name, Component target)[] entries)
        {
            EUIBindingEditorUtility.SetBindings(root.GetComponent<EUIBinding>(), entries.Select(e=>new EUIBindingEntrySnapshot {
                Name=e.name, GameObjectPath=AnimationUtility.CalculateTransformPath(e.target.transform,root.transform),
                WidgetType=EUIBinding.WidgetTypes.Extension, ClassName=e.target.GetType().FullName }).ToList());
        }
        private static void Tint(Selectable selectable)
        {
            var colors=ColorBlock.defaultColorBlock; colors.normalColor=Color.white; colors.highlightedColor=new Color(.93f,.96f,1);
            colors.pressedColor=new Color(.68f,.77f,.88f); colors.selectedColor=new Color(.82f,.9f,1);
            colors.disabledColor=new Color(.58f,.6f,.65f,.65f); colors.fadeDuration=.08f; selectable.colors=colors;
        }
        private static void Decorate(GameObject root,string name)
        {
            var rect=(RectTransform)root.transform;
            if(name.EndsWith("Page",StringComparison.Ordinal)) { Skeleton(root,name.Contains("Popup")); return; }
            rect.sizeDelta=new Vector2(280,48);
            if(name=="EUIActionButtonItem")
            {
                var image=Surface(rect,Blue,true); var button=root.AddComponent<EUIButtonEx>(); button.targetGraphic=image; Tint(button);
                button.Label=Text("Label",rect,"Continue"); button.Label.color=Color.white; button.Label.alignment=TextAlignmentOptions.Center;
                var icon=Surface(Rect("Icon",rect,20,20),Color.white); icon.rectTransform.anchoredPosition=new Vector2(-110,0); icon.gameObject.SetActive(false);
                Bind(root,("Button",button));
            }
            else if(name=="EUIChoiceToggleItem" || name=="EUIOptionRowItem")
            {
                rect.sizeDelta=new Vector2(name=="EUIOptionRowItem"?440:280,56);
                var image=Surface(rect,Color.white,true); var toggle=root.AddComponent<EUIToggleEx>(); toggle.targetGraphic=image; Tint(toggle);
                var mark=Text("Selected",rect,"✓",22); mark.color=Blue; mark.rectTransform.anchorMin=new Vector2(0,0); mark.rectTransform.anchorMax=new Vector2(0,1); mark.rectTransform.sizeDelta=new Vector2(32,-8); mark.rectTransform.anchoredPosition=new Vector2(22,0);
                var off=Surface(Rect("Unselected",rect,18,18),Line); off.rectTransform.anchorMin=off.rectTransform.anchorMax=new Vector2(0,.5f); off.rectTransform.anchoredPosition=new Vector2(22,0);
                toggle.Label=Text("Label",rect,name=="EUIOptionRowItem"?"An editable option":"Enable option"); toggle.Label.rectTransform.offsetMin=new Vector2(48,4); toggle.Label.rectTransform.offsetMax=new Vector2(-16,-4);
                using(var so=new SerializedObject(toggle)) { so.FindProperty("_onNode").objectReferenceValue=mark.gameObject; so.FindProperty("_offNode").objectReferenceValue=off.gameObject; so.ApplyModifiedPropertiesWithoutUndo(); }
                toggle.graphic=mark; toggle.isOn=false; mark.gameObject.SetActive(false);
                Bind(root,("Toggle",toggle));
            }
            else if(name=="EUIProgressItem")
            {
                rect.sizeDelta=new Vector2(360,40);
                var track=Rect("Track",rect,360,10); Stretch(track); track.offsetMin=new Vector2(0,14); track.offsetMax=new Vector2(-70,-14); Surface(track,new Color32(232,235,241,255));
                var fill=Rect("Fill",track,100,10); Stretch(fill); Surface(fill,Blue);
                var slider=root.AddComponent<Slider>(); slider.fillRect=fill; slider.minValue=0; slider.maxValue=1; slider.value=.5f; slider.interactable=false;
                var label=Text("Value",rect,"50%",14); label.rectTransform.anchorMin=new Vector2(1,0); label.rectTransform.anchorMax=Vector2.one; label.rectTransform.sizeDelta=new Vector2(60,40); label.rectTransform.anchoredPosition=new Vector2(-30,0);
                Bind(root,("Progress",slider),("Value",label));
            }
            else
            {
                rect.sizeDelta=new Vector2(440,260); Surface(rect,Color.white,true);
                var viewport=Rect("Viewport",rect,420,240); Stretch(viewport,8); viewport.gameObject.AddComponent<RectMask2D>();
                var content=Rect("Content",viewport,420,240); content.anchorMin=new Vector2(0,1); content.anchorMax=Vector2.one; content.pivot=new Vector2(.5f,1); content.anchoredPosition=Vector2.zero; content.sizeDelta=new Vector2(0,240);
                var layout=content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing=8; layout.padding=new RectOffset(8,8,8,8); layout.childControlHeight=true; layout.childForceExpandHeight=false;
                var fitter=content.gameObject.AddComponent<ContentSizeFitterEx>(); fitter.VerticalFit=ContentSizeFitter.FitMode.PreferredSize;
                var scroll=root.AddComponent<ScrollRect>(); scroll.viewport=viewport; scroll.content=content; scroll.horizontal=false;
                Bind(root,("Content",content),("Scroll",scroll));
            }
        }
        private static void Skeleton(GameObject root,bool popup)
        {
            var center=root.transform.Find("Animator/EUISafeArea/Center");
            var surface=Rect("Surface",center,popup?580:1160,popup?400:640);
            if(!popup) { Stretch(surface,24); var image=Surface(surface,Color.white); image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Background); image.type=Image.Type.Simple; }
            else { Surface(surface,Color.white,true); var shadow=surface.gameObject.AddComponent<Shadow>(); shadow.effectColor=new Color(0,0,0,.12f); shadow.effectDistance=new Vector2(0,-4); }
            var title=Text("Title",center,popup?"A quiet moment":"Component library",28); title.rectTransform.anchorMin=new Vector2(.1f,.84f); title.rectTransform.anchorMax=new Vector2(.9f,.94f); title.rectTransform.offsetMin=title.rectTransform.offsetMax=Vector2.zero;
            var content=Rect("Content",center,popup?500:1100,popup?230:500);
            if(!popup) { content.anchorMin=new Vector2(.06f,.09f); content.anchorMax=new Vector2(.94f,.82f); content.offsetMin=content.offsetMax=Vector2.zero; }
            if(popup)
            {
                var close=Rect("Close",center,44,44); close.anchoredPosition=new Vector2(244,154); var image=Surface(close,new Color32(236,238,242,255),true);
                var button=close.gameObject.AddComponent<EUIButtonEx>(); button.targetGraphic=image; Tint(button); button.Label=Text("Label",close,"×",24); button.Label.alignment=TextAlignmentOptions.Center;
                Bind(root,("Title",title),("Content",content),("Close",button));
            }
            else Bind(root,("Title",title),("Content",content));
        }
        private static GameObject Nest(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var path=(name=="EUIOptionRowItem"?Module:Common)+name+".prefab";
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent);
            var rect=(RectTransform)instance.transform; rect.anchoredPosition=position; rect.sizeDelta=size;
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect); return instance;
        }
        private static void Preview(GameObject root,bool popup)
        {
            var content=root.transform.Find("Animator/EUISafeArea/Center/Content");
            if(popup) { Text("Description",content,"Reusable popup. Close with × or click the mask.",20); return; }
            var first=Nest("EUIActionButtonItem",content,new Vector2(-370,180),new Vector2(280,48)); first.name="Primary";
            var second=Nest("EUIActionButtonItem",content,new Vector2(-370,116),new Vector2(280,64)); second.name="Secondary";
            second.GetComponent<Image>().color=Color.white; second.GetComponent<EUIButtonEx>().Label.text="A longer label that wraps onto two lines"; second.GetComponent<EUIButtonEx>().Label.color=Ink;
            var disabled=Nest("EUIActionButtonItem",content,new Vector2(-370,44),new Vector2(280,48)); disabled.name="Disabled"; disabled.GetComponent<EUIButtonEx>().interactable=false; disabled.GetComponent<EUIButtonEx>().Label.text="Unavailable";
            var toggle=Nest("EUIChoiceToggleItem",content,new Vector2(-370,-28),new Vector2(280,56)); toggle.name="Choice"; toggle.GetComponent<Toggle>().isOn=true;
            var progress=Nest("EUIProgressItem",content,new Vector2(-350,-105),new Vector2(320,40));
            var empty=Rect("EmptyState",content,210,150); empty.anchoredPosition=new Vector2(330,110); var image=empty.gameObject.AddComponent<Image>(); image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Atlas+"EmptyState.png"); image.preserveAspect=true; image.raycastTarget=false;
            var host=Nest("EUIContentHostItem",content,new Vector2(100,-70),new Vector2(440,240));
            for(var i=0;i<6;i++) { var row=Nest("EUIOptionRowItem",host.transform.Find("Viewport/Content"),Vector2.zero,new Vector2(400,56)); row.name="Option"+i; row.GetComponent<EUIToggleEx>().Label.text="Option "+(i+1); var layout=row.AddComponent<LayoutElement>(); layout.preferredHeight=56; }
            foreach(var component in root.GetComponentsInChildren<Component>(true)) if(component && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        private static EUILibraryEntry MakeEntry(GameObject prefab)
        {
            var name=prefab.name; var family=name.Replace("EUI","").Replace("Item","").Replace("Page","");
            var entry=new EUILibraryEntry { Id="ember."+family.ToLowerInvariant(), Family=family, Category=prefab.GetComponent<EUIBinding>().IsPage?"Skeleton":"Component", Prefab=prefab, ReferenceSize=((RectTransform)prefab.transform).sizeDelta };
            foreach(var text in prefab.GetComponentsInChildren<TMP_Text>(true)) Slot(entry,text.name.ToLowerInvariant(),text,"text", "m_text","m_fontColor","m_fontSize");
            foreach(var image in prefab.GetComponentsInChildren<Image>(true)) Slot(entry,image.name.ToLowerInvariant()+"-image",image,"image","m_Sprite","m_Color");
            foreach(var selectable in prefab.GetComponentsInChildren<Selectable>(true)) Slot(entry,"control",selectable,"control","m_Interactable");
            var content=prefab.transform.Find("Animator/EUISafeArea/Center/Content") ?? prefab.transform.Find("Viewport/Content");
            if(content) Slot(entry,"content",content,"content","children");
            entry.RequiredRoles=entry.Slots.Select(s=>s.Role).Distinct().ToArray(); return entry;
        }
        private static void Slot(EUILibraryEntry entry,string id,Component target,string role,params string[] properties)
        { entry.Slots.Add(new EUILibrarySlot { Id=id,Target=target,Role=role,ComponentType=target.GetType().FullName,State=target is Selectable?"Selectable.interactable / pressed / focus":"default",Properties=properties }); }
        #endregion
    }
}



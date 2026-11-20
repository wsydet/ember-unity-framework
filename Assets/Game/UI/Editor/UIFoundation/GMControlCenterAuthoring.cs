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
    /// <summary>D10 的有界维护源；通过 EUI 中心绑定及生成，保留正式资源 GUID。</summary>
    public static class GMControlCenterAuthoring
    {
        #region 内部参数
        private const string Root = SoftGlassUILibraryBuilder.ROOT;
        private const string Common = "Assets/GameResource/Resources/UI/Common/Prefabs/";
        private const string Config = "Assets/GameResource/Resources/Config/UIFoundation/";
        public const string ReceiptDirectory = ".utmp/ember-ui/gm-control-center/";
        public static string LastReceiptPath { get; private set; }
        private static readonly Color GlassTint = new(.025f, .07f, .11f, .82f);
        private static readonly Color White = new(.95f, .98f, 1);
        private static TMP_FontAsset _font;
        private static readonly List<object> Receipt = new();
        #endregion
        #region 外部方法
        public static string Preflight()
        {
            if (!EmberProjectSetup.IsTemplateActive("base") || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires formal base edit mode");
            foreach (string name in new[] { "GMStateItem", "GMTimeItem", "GMActionItem", "GMCenterItem", "GMPage", "AlertPage", "ConfirmPage" })
                if (!AssetDatabase.LoadAssetAtPath<GameObject>(Root + "EUISoftGlass" + name + ".prefab")) throw new InvalidOperationException("Missing " + name);
            foreach (string name in new[] { "GMControlLayout", "GMPanelDragHandle", "GMActionGesture", "GMCircleHit", "GMHudController", "UIFrameRateControls" }) RuntimeType(name);
            return "Existing D10 targets and EUI code-generation API available; no writes";
        }
        public static string Apply()
        {
            var originals=AssetDatabase.FindAssets("t:TMP_FontAsset",new[]{"Assets/GameResource/Resources/UI/Common/Fonts","Packages/com.ember/SharedAssets/Fonts"}).Select(AssetDatabase.GUIDToAssetPath).Where(File.Exists).ToDictionary(path=>path,File.ReadAllBytes);
            try { return ApplyContents(); }
            finally
            {
                // TMP may populate a dynamic atlas during authoring. Preserve the user's exact preexisting asset bytes.
                foreach(var pair in originals)if(!pair.Value.SequenceEqual(File.ReadAllBytes(pair.Key))) {File.WriteAllBytes(pair.Key,pair.Value);AssetDatabase.ImportAsset(pair.Key,ImportAssetOptions.ForceUpdate);}
            }
        }
        private static string ApplyContents()
        {
            Preflight(); Receipt.Clear();
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/GameResource/Resources/UI/Common/Fonts/DefaultLight/DefaultLightSans.asset");
            var dialogStyle=AssetDatabase.LoadAssetAtPath<EUIGlassStyle>(Config+"SoftGlassDialog.asset");
            if(!dialogStyle)
            {
                dialogStyle=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<EUIGlassStyle>(Config+"SoftGlassNeutral.asset"));
                dialogStyle.name="SoftGlassDialog"; var styleSO=new SerializedObject(dialogStyle);styleSO.FindProperty("_tint").colorValue=new Color(.42f,.66f,.92f,.14f);styleSO.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(dialogStyle,Config+"SoftGlassDialog.asset");
            }
            Edit(Root + "EUISoftGlassGMStateItem.prefab", State);
            Edit(Root + "EUISoftGlassGMTimeItem.prefab", TimeCard);
            Edit(Root + "EUISoftGlassGMActionItem.prefab", ActionCard);
            Edit(Root + "EUISoftGlassGMCenterItem.prefab", Center);
            Edit(Root + "EUISoftGlassGMPage.prefab", Page);
            Edit(Common + "EUISettingPage.prefab", Settings, false);
            Edit(Common + "EUILoadingPage.prefab", go => { if (!go.GetComponent<EUIBackdropGroup>()) go.AddComponent<EUIBackdropGroup>(); }, false);
            foreach (string name in new[] { "AlertPage", "ConfirmPage" }) Edit(Root + "EUISoftGlass" + name + ".prefab", Popup, false);
            // Prefabs were saved explicitly. Avoid a global SaveAssets that could persist unrelated dynamic fonts or editor migrations.
            foreach(string name in new[]{"GMBackdropSettings","SoftGlassDialog"})AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Config+name+".asset"));
            string result = Newtonsoft.Json.JsonConvert.SerializeObject(Receipt, Newtonsoft.Json.Formatting.Indented);
            WriteReceipt("apply", result); return result;
        }
        public static string Inspect()
        {
            var paths=new[]{"GMStateItem","GMTimeItem","GMActionItem","GMCenterItem","GMPage","AlertPage","ConfirmPage"}.Select(n=>Root+"EUISoftGlass"+n+".prefab").Concat(new[]{Common+"EUISettingPage.prefab",Common+"EUILoadingPage.prefab"});
            var report=paths.Select(path=>new {path,guid=AssetDatabase.AssetPathToGUID(path),binding=Snapshot(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<EUIBinding>())}).ToArray();
            string result=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);WriteReceipt("inspect",result);return result;
        }
        #endregion
        #region 内部方法
        private static void WriteReceipt(string operation, string result)
        {
            Directory.CreateDirectory(ReceiptDirectory);
            string path = ReceiptDirectory + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ", System.Globalization.CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N") + "-" + operation + ".json";
            File.WriteAllText(path, result);
            LastReceiptPath = path;
        }
        private static Type RuntimeType(string name) => Type.GetType("Game.UI." + name + ", Assembly-CSharp", true);
        private static object Snapshot(EUIBinding binding) => new {
            binding.ClassName,binding.ClassPath,pathMode=binding.PathMode.ToString(),binding.BackdropSchema,binding.OverrideMaskTint,
            maskTint=new[]{binding.MaskColor.r,binding.MaskColor.g,binding.MaskColor.b,binding.MaskColor.a},
            entries=binding.Bindings.Select(e=>new{e.Name,e.IsFramework,type=e.Type.ToString(),e.ClassName,node=e.GameObject?AnimationUtility.CalculateTransformPath(e.GameObject.transform,binding.transform):null}).ToArray()
        };
        private static Component Component(GameObject go, string name) => go.GetComponent(RuntimeType(name)) ?? go.AddComponent(RuntimeType(name));
        private static void Assign(Component component, params (string field, UnityEngine.Object value)[] entries)
        { var so = new SerializedObject(component); foreach (var e in entries) so.FindProperty(e.field).objectReferenceValue = e.value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Edit(string path, Action<GameObject> action, bool regenerate = true)
        {
            var go = PrefabUtility.LoadPrefabContents(path);
            try
            {
                go.SetActive(false); action(go);
                foreach (var component in go.GetComponentsInChildren<Component>(true)) if (component && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                go.SetActive(true); PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(go); }
            var binding = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<EUIBinding>();
            if (regenerate && !EUIBindingCodeGenUtility.TryRegenerateCode(binding, out string error)) throw new InvalidOperationException(path + ": " + error);
            Receipt.Add(new { path, guid = AssetDatabase.AssetPathToGUID(path), generated = regenerate, binding = Snapshot(binding) });
        }
        private static void Clear(Transform parent)
        { for (int i = parent.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject); }
        private static RectTransform Rect(string name, Transform parent)
        { var found = parent.Find(name); if (found) return (RectTransform)found; var go = new GameObject(name, typeof(RectTransform)); go.layer = parent.gameObject.layer; go.transform.SetParent(parent, false); return (RectTransform)go.transform; }
        private static void Stretch(RectTransform r, float inset = 0)
        { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.one * inset; r.offsetMax = -Vector2.one * inset; }
        private static void Box(RectTransform r, float x, float y, float w, float h)
        { r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); }
        private static void Top(RectTransform r, float top, float height, float inset = 16)
        { r.anchorMin = Vector2.up; r.anchorMax = Vector2.one; r.pivot = new Vector2(.5f, 1); r.offsetMin = new Vector2(inset, -top - height); r.offsetMax = new Vector2(-inset, -top); }
        private static EUIGlassImage Glass(RectTransform r, float radius = 24, bool raycast = false)
        { var g = r.GetComponent<EUIGlassImage>() ?? r.gameObject.AddComponent<EUIGlassImage>(); g.color = GlassTint; g.Radius = radius; g.Gloss = .38f; g.Frost = .04f; g.EdgeLight = .5f; g.raycastTarget = raycast; return g; }
        private static TMPEx Text(string name, Transform parent, string text, float size = 22)
        { var r = Rect(name, parent); var t = r.GetComponent<TMPEx>() ?? r.gameObject.AddComponent<TMPEx>(); t.font = _font; t.text = text; t.fontSize = size; t.fontStyle=FontStyles.Normal;t.color = White; t.raycastTarget = false; t.alignment = TextAlignmentOptions.Center; t.textWrappingMode = TextWrappingModes.Normal; return t; }
        private static EUIButtonEx Button(string name, Transform parent, string text)
        { var r = Rect(name, parent); var b = r.GetComponent<EUIButtonEx>() ?? r.gameObject.AddComponent<EUIButtonEx>(); b.targetGraphic = Glass(r, 100, true); b.Label = Text("Label", r, text, 19); Stretch(b.Label.rectTransform, 4); b.transition = Selectable.Transition.None; return b; }
        private static EUIControlGlyph Glyph(Transform parent, string key)
        { var r = Rect("Glyph", parent); Stretch(r, 18); var g = r.GetComponent<EUIControlGlyph>() ?? r.gameObject.AddComponent<EUIControlGlyph>(); g.SetKey(key); g.color = White; g.raycastTarget = false; return g; }
        private static void Bind(GameObject root, params (string name, Component component)[] entries)
        {
            // Precise serialized update preserves IsFramework on retained entries.
            var so = new SerializedObject(root.GetComponent<EUIBinding>()); var bindings = so.FindProperty("bindings");
            for (int i = bindings.arraySize - 1; i >= 0; i--) if (!entries.Any(e => e.name == bindings.GetArrayElementAtIndex(i).FindPropertyRelative("Name").stringValue)) bindings.DeleteArrayElementAtIndex(i);
            foreach (var entry in entries)
            {
                SerializedProperty p = null;
                for (int i = 0; i < bindings.arraySize; i++) { var test = bindings.GetArrayElementAtIndex(i); if (test.FindPropertyRelative("Name").stringValue == entry.name) { p = test; break; } }
                if (p == null) { int index = bindings.arraySize++; p = bindings.GetArrayElementAtIndex(index); p.FindPropertyRelative("IsFramework").boolValue = false; }
                p.FindPropertyRelative("Name").stringValue = entry.name; p.FindPropertyRelative("GameObject").objectReferenceValue = entry.component.gameObject;
                p.FindPropertyRelative("Type").intValue = (int)(entry.component is EUIBinding ? EUIBinding.WidgetTypes.UILogic : EUIBinding.WidgetTypes.Extension);
                p.FindPropertyRelative("ClassName").stringValue = entry.component is EUIBinding child ? child.ClassName : entry.component.GetType().FullName;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static GameObject Nest(string name, Transform parent)
        { return (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "EUISoftGlass" + name + ".prefab"), parent); }
        private static void State(GameObject go)
        {
            Clear(go.transform); Glass((RectTransform)go.transform, 26); var eyebrow = Text("Eyebrow", go.transform, "TOP LEVEL FSM", 14); Top(eyebrow.rectTransform, 24, 22);
            var state = Text("State", go.transform, "—", 30); Top(state.rectTransform, 60, 78); state.enableAutoSizing = true; state.fontSizeMin = 16; state.fontSizeMax = 30;
            var unity = Text("UnityTime", go.transform, "", 12); unity.gameObject.SetActive(false); var ember = Text("EmberTime", go.transform, "", 12); ember.gameObject.SetActive(false);
            var caption = Text("Caption", go.transform, "GameLauncher", 14); caption.rectTransform.anchorMin = Vector2.zero; caption.rectTransform.anchorMax = Vector2.right; caption.rectTransform.offsetMin = new Vector2(12, 16); caption.rectTransform.offsetMax = new Vector2(-12, 40);
            Bind(go, ("State", state), ("UnityTime", unity), ("EmberTime", ember));
        }
        private static void TimeCard(GameObject go)
        {
            Clear(go.transform); var old = go.GetComponent<EUIGlassImage>(); if (old) UnityEngine.Object.DestroyImmediate(old);
            var enableRect = Rect("Enable", go.transform); var enable = enableRect.gameObject.AddComponent<EUIToggleEx>(); enable.targetGraphic = Glass(enableRect, 100, true); enable.transition = Selectable.Transition.None;
            Glyph(enableRect, "power"); var enabledGlyph = Rect("Enabled", enableRect); Stretch(enabledGlyph, 6); var enabledSurface = Glass(enabledGlyph, 100); enabledSurface.color = new Color(.16f, .48f, .95f, .65f); enabledGlyph.SetAsFirstSibling(); enable.OnNode = enabledGlyph.gameObject;
            var off = Rect("Disabled", enableRect); off.gameObject.SetActive(true); enable.OffNode = off.gameObject; enable.isOn = false; enable.Refresh();
            var reset = Button("Reset", go.transform, ""); Glyph(reset.transform, "reset");
            var slider = Nest("VerticalSliderItem", go.transform).GetComponent<Slider>(); slider.name = "TimeScale"; slider.minValue = .1f; slider.maxValue = 3; slider.value = 1;
            Stretch((RectTransform)slider.transform.Find("Track"));slider.transform.Find("Track").GetComponent<EUIGlassImage>().color=GlassTint; var nested = slider.transform.Find("Value"); if (nested) nested.gameObject.SetActive(false);
            var value = Text("Value", slider.transform, "1.00×", 17); value.rectTransform.anchorMin = Vector2.zero; value.rectTransform.anchorMax = Vector2.right; value.rectTransform.offsetMin = new Vector2(0, 14); value.rectTransform.offsetMax = new Vector2(0, 40);
            value.gameObject.SetActive(true);
            value.color=new Color(.035f,.12f,.2f);
            Bind(go, ("Enable", enable), ("Slider", slider), ("Value", value), ("Reset", reset));
        }
        private static void ActionCard(GameObject go)
        {
            Clear(go.transform); var hit = go.GetComponent<Image>() ?? go.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = false; Component(go, "GMCircleHit");
            var button = Button("Action", go.transform, ""); Stretch((RectTransform)button.transform); button.GetComponent<EUIGlassImage>().Radius = 20; Component(button.gameObject, "GMActionGesture");
            var glyph = Glyph(button.transform, "grid"); Box(glyph.rectTransform, 16, 23, 36, 36);
            button.Label.name="Icon";
            var label = Text("Label", button.transform, "项目操作", 18); label.alignment = TextAlignmentOptions.MidlineLeft; label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.offsetMin = new Vector2(66, 6); label.rectTransform.offsetMax = new Vector2(-10, -6);
            var favorite = Text("Favorite", button.transform, "★", 14); Box(favorite.rectTransform, 8, 4, 20, 20); favorite.color = new Color(1, .8f, .2f); favorite.gameObject.SetActive(false);
            Bind(go, ("Button", button), ("Label", label), ("Icon", button.Label), ("Glyph", glyph), ("Surface", button.GetComponent<EUIGlassImage>()));
        }
        private static void Frame(RectTransform root, bool full)
        {
            Clear(root); Glass(root, 22); var controls = Component(root.gameObject, "UIFrameRateControls");
            var buttons = new[] { Button("Economy", root, "节能"), Button("Balanced", root, "平衡"), Button("Responsive", root, "高响应") };
            for (int i = 0; i < 3; i++) { var r = (RectTransform)buttons[i].transform; r.anchorMin = new Vector2(i / 3f, 0); r.anchorMax = new Vector2((i + 1) / 3f, 0); r.pivot = Vector2.one * .5f; r.offsetMin = new Vector2(5, 8); r.offsetMax = new Vector2(-5, 50); }
            var summary = Text("Summary", root, "屏幕 — Hz · 目标 — FPS", 16); Top(summary.rectTransform, 8, 30, 70);
            var fps = Text("FPS", root, "—", 22); Box(fps.rectTransform, 5, 4, 58, 32);
            var sampler = fps.gameObject.AddComponent<FPSDisplay>(); Assign(sampler, ("_fpsText", fps));
            var fpsCaption=Text("FPSCaption",root,"FPS",10);Box(fpsCaption.rectTransform,5,29,58,14);
            if (full)
            {
                var controlsSO=new SerializedObject(controls);controlsSO.FindProperty("_darkSurface").boolValue=false;controlsSO.ApplyModifiedPropertiesWithoutUndo();
                for (int i = 0; i < 3; i++) { var r = (RectTransform)buttons[i].transform;r.anchorMin=new Vector2(i/3f,1);r.anchorMax=new Vector2((i+1)/3f,1);r.offsetMin = new Vector2(5, -94); r.offsetMax = new Vector2(-5, -50); }
                var toggleRect = Rect("Automatic", root); Top(toggleRect, 106, 44, 18); var toggle = toggleRect.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = Glass(toggleRect, 16, true);
                var check = Rect("Check", toggleRect); Box(check, 10, 12, 24, 24); var checkGraphic = check.gameObject.AddComponent<Image>(); checkGraphic.color = new Color(.4f, .8f, 1); toggle.graphic = checkGraphic;
                var text = Text("Label", toggleRect, "自动跟随屏幕", 21); Stretch(text.rectTransform, 4); toggle.isOn = true;
                var customLabel=Text("CustomLabel",root,"自定义目标 FPS",18);customLabel.rectTransform.anchorMin=new Vector2(0,1);customLabel.rectTransform.anchorMax=new Vector2(.4f,1);customLabel.rectTransform.offsetMin=new Vector2(18,-208);customLabel.rectTransform.offsetMax=new Vector2(0,-160);
                var inputRoot = Rect("CustomTarget", root);inputRoot.anchorMin=new Vector2(.42f,1);inputRoot.anchorMax=Vector2.one;inputRoot.offsetMin=new Vector2(0,-208);inputRoot.offsetMax=new Vector2(-18,-160);Glass(inputRoot, 14, true);
                var viewport = Rect("Viewport", inputRoot); Stretch(viewport, 8); viewport.gameObject.AddComponent<RectMask2D>();
                var inputText = Text("Text", viewport, "180", 24); Stretch(inputText.rectTransform); inputText.raycastTarget = true;
                var input = inputRoot.gameObject.AddComponent<TMP_InputField>(); input.textViewport = viewport; input.textComponent = inputText; input.contentType = TMP_InputField.ContentType.IntegerNumber; input.text = "180";
                var error = Text("Error", root, "", 15); Top(error.rectTransform, 212, 24); error.color = new Color(1, .65f, .55f);
                var hint = Text("Hint", root, "同步模式保留目标设置；高响应自定义不加余量", 15); Top(hint.rectTransform, 238, 40);
                Assign(controls, ("_automatic", toggle), ("_custom", input), ("_error", error));
                foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))label.color=new Color(.035f,.1f,.18f);
                foreach(var surface in root.GetComponentsInChildren<EUIGlassImage>(true))AssetDatabase.LoadAssetAtPath<EUIGlassStyle>(Config+"SoftGlassDialog.asset").ApplyTo(surface,EUIGlassRadius.Control);
            }
            Assign(controls, ("_economy", buttons[0]), ("_balanced", buttons[1]), ("_responsive", buttons[2]), ("_summary", summary));
        }
        private static void Center(GameObject go)
        {
            Clear(go.transform); var constrained = go.GetComponent<EUIConstrainedPanel>(); if (constrained) UnityEngine.Object.DestroyImmediate(constrained);
            var oldGrid = go.GetComponent<EUIResponsiveCardGrid>(); if (oldGrid) UnityEngine.Object.DestroyImmediate(oldGrid);
            Glass((RectTransform)go.transform, 36, true);
            var title = Text("Title", go.transform, "控制中心", 25); var drag = Button("Drag", go.transform, "");Glyph(drag.transform,"drag"); var close = Button("Close", go.transform, "×");
            var state = Nest("GMStateItem", go.transform); var time = Nest("GMTimeItem", go.transform); var frame = Rect("FrameRate", go.transform); Frame(frame, false);
            var viewport = Rect("Viewport", go.transform); viewport.gameObject.AddComponent<RectMask2D>(); var blocker = viewport.gameObject.AddComponent<Image>(); blocker.color = Color.clear;
            var actions = Rect("Actions", viewport); actions.anchorMin = Vector2.up; actions.anchorMax = Vector2.one; actions.pivot = new Vector2(.5f, 1); actions.sizeDelta = new Vector2(0, 0);
            var grid = actions.gameObject.AddComponent<GridLayoutGroup>(); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2; grid.spacing = new Vector2(12, 12); grid.childAlignment = TextAnchor.UpperLeft;
            var scroll = go.GetComponent<ScrollRect>() ?? go.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = actions; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
            var notice = Text("Notice", go.transform, "项目尚未注册扩展操作", 14);
            var layout = Component(go, "GMControlLayout"); Assign(layout, ("_title", title.rectTransform), ("_drag", drag.transform), ("_close", close.transform), ("_state", state.transform), ("_time", time.transform), ("_frame", frame), ("_viewport", viewport), ("_actions", actions), ("_notice", notice.rectTransform));
            var handle = Component(drag.gameObject, "GMPanelDragHandle"); Assign(handle, ("_panel", go.transform), ("_surface", drag.targetGraphic));
            Bind(go, ("Close", close), ("Title", title), ("State", state.GetComponent<EUIBinding>()), ("Time", time.GetComponent<EUIBinding>()), ("Actions", actions), ("Scroll", scroll), ("Content", actions));
        }
        private static void Page(GameObject go)
        {
            var center = go.transform.Find("Animator/EUISafeArea/Center"); Clear(center);
            var outside = Button("HUDOutside", center, ""); Stretch((RectTransform)outside.transform); var glass = outside.GetComponent<EUIGlassImage>(); UnityEngine.Object.DestroyImmediate(glass); var transparent = outside.gameObject.AddComponent<Image>(); transparent.color = Color.clear; outside.targetGraphic = transparent;
            var hud = Rect("FavoriteHUD", center); Stretch(hud);
            var emptyCard=Rect("EmptyCard",hud);emptyCard.sizeDelta=new Vector2(260,60);Glass(emptyCard,22);
            var empty = Text("Empty", emptyCard, "长按或右键项目操作来收藏", 17); Stretch(empty.rectTransform,8);
            var entry = Nest("GMEntryItem", center); ((RectTransform)entry.transform).sizeDelta = Vector2.one * 76;
            var host = Rect("CenterHost", center); Stretch(host); var panel = Nest("GMCenterItem", host);
            var controller = Component(go, "GMHudController"); Assign(controller, ("_entry", entry.GetComponent<EUIFloatingEntry>()), ("_container", hud), ("_outside", outside), ("_empty", empty), ("_actionPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(Root + "EUISoftGlassGMActionItem.prefab")));
            Bind(go, ("Entry", entry.GetComponent<EUIBinding>()), ("Center", panel.GetComponent<EUIBinding>()), ("CenterHost", host)); host.gameObject.SetActive(false); hud.gameObject.SetActive(false); outside.gameObject.SetActive(false);
            var settings = AssetDatabase.LoadAssetAtPath<EUIBackdropSettings>(Config + "GMBackdropSettings.asset");
            if (!settings) { settings = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<EUIBackdropSettings>(Config + "EUIBackdropSettings.asset")); settings.name = "GMBackdropSettings"; AssetDatabase.CreateAsset(settings, Config + "GMBackdropSettings.asset"); }
            var group = go.GetComponent<EUIBackdropGroup>() ?? go.AddComponent<EUIBackdropGroup>(); Assign(group, ("_settings", settings));
        }
        private static void Mask(EUIBinding binding)
        { var so = new SerializedObject(binding); so.FindProperty("backdropSchema").intValue = 1; so.FindProperty("overrideMaskTint").boolValue = true; so.FindProperty("maskColor").colorValue = new Color(.025f, .06f, .1f, .18f); so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Settings(GameObject go)
        {
            var binding = go.GetComponent<EUIBinding>(); var close = binding.Bindings.First(e => e.Name == "Btn_Close").GameObject; var panel = (RectTransform)close.transform.parent;
            var constrained = panel.GetComponent<EUIConstrainedPanel>(); var so = new SerializedObject(constrained); so.FindProperty("_maximumSize").vector2Value = new Vector2(760, 620); so.FindProperty("_portraitHeight").floatValue = 680; so.ApplyModifiedPropertiesWithoutUndo();
            var context = binding.Bindings.First(e => e.Name == "Txt_NowScene").GameObject; Top((RectTransform)context.transform, 130, 60);
            var footer = panel.Find("SoftGlassFooter"); if (footer) footer.gameObject.SetActive(false);
            var frame = Rect("FrameRateSettings", panel); Top(frame, 210, 290, 30); Frame(frame, true);
            // Controls use serialized references in the authored component. Existing Framework bindings retain their ownership.
            Mask(binding); constrained.Refresh();
            foreach(var surface in panel.GetComponentsInChildren<EUIGlassImage>(true))
                if(surface.name=="SoftGlassSurface")AssetDatabase.LoadAssetAtPath<EUIGlassStyle>(Config+"SoftGlassDialog.asset").ApplyTo(surface,EUIGlassRadius.Panel);
        }
        private static void Popup(GameObject go)
        {
            Mask(go.GetComponent<EUIBinding>());
            foreach (var image in go.GetComponentsInChildren<EUIGlassImage>(true))
            {
                var style = AssetDatabase.LoadAssetAtPath<EUIGlassStyle>(Config + (image.name == "Accept" ? "SoftGlassAccent" : "SoftGlassDialog") + ".asset");
                style.ApplyTo(image, image.name == "Panel" ? EUIGlassRadius.Panel : EUIGlassRadius.Control);
            }
            foreach(var label in go.GetComponentsInChildren<TMP_Text>(true)) {label.color=new Color(.035f,.1f,.18f);label.fontStyle=FontStyles.Normal;}
        }
        #endregion
    }
}

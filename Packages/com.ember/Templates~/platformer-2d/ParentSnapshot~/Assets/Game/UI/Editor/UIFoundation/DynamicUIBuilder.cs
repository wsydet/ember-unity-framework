using System;
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
    /// <summary>显式 S3 制作入口；只制作缺失资产，不在域重载或运行时搭建 UI。</summary>
    public static class DynamicUIBuilder
    {
        #region 内部参数
        private const string COMMON = "Assets/GameResource/Resources/UI/Common/Prefabs/";
        private const string MODULE = "Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/";
        #endregion
        #region 外部方法
        public static string Build()
        {
            if (!EmberProjectSetup.IsTemplateActive("base")) throw new InvalidOperationException("需要活动 base 模板。");
            Create("EUIDynamicOptionRowItem", MODULE + "EUIOptionRowItem.prefab", true, false, Row);
            Create("EUIDynamicListItem", COMMON + "EUIContentHostItem.prefab", false, true, Host);
            Create("EUIDynamicListPage", COMMON + "EUIStandardPage.prefab", false, false, Page);
            AssetDatabase.SaveAssets();
            return "S3 Prefab/Binding 已通过创建中心生成。";
        }
        #endregion
        #region 内部方法
        private static void Create(string name, string source, bool inherit, bool structureOnly, Action<GameObject> edit)
        {
            var target = MODULE + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(target)) return;
            var page = name.EndsWith("Page", StringComparison.Ordinal);
            var request = new EUICreationRequest { PrefabName = name, ClassName = name, PageName = name,
                ClassPath = "Module/UIFoundation", Role = page ? EUIBindingRole.Page : EUIBindingRole.Item,
                CodePathMode = EUIBinding.CodePathMode.Business, StructureOnly = structureOnly,
                SourceSkeleton = AssetDatabase.LoadAssetAtPath<GameObject>(source), InheritBinding = inherit,
                ContentSlotPath = page ? "Animator/EUISafeArea/Center/Content" : "",
                PageType = PageType.MainPage, ReferenceResolution = new Vector2(1280,720) };
            if (!EUICreationService.TryBuildPlan(request, out _, out var report)) throw new InvalidOperationException(report.Error);
            var result = EUICreationService.Create(request);
            if (!result.Success) throw new InvalidOperationException(result.Error);
            var root = PrefabUtility.LoadPrefabContents(result.PrefabPath);
            try
            {
                edit(root);
                foreach (var c in root.GetComponentsInChildren<Component>(true))
                    if (c && PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                PrefabUtility.SaveAsPrefabAsset(root, result.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            if (!structureOnly && !EUIBindingCodeGenUtility.TryRegenerateCode(AssetDatabase.LoadAssetAtPath<GameObject>(result.PrefabPath).GetComponent<EUIBinding>(), out var error))
                throw new InvalidOperationException(error);
        }

        private static void Bind(GameObject root, params (string name, Component target)[] entries)
        {
            var binding = root.GetComponent<EUIBinding>();
            var snapshot = EUIBindingEditorUtility.GetBindingSnapshot(binding);
            foreach (var entry in entries)
            {
                snapshot.Entries.RemoveAll(e => e.Name == entry.name);
                snapshot.Entries.Add(new EUIBindingEntrySnapshot { Name = entry.name,
                    GameObjectPath = AnimationUtility.CalculateTransformPath(entry.target.transform, root.transform),
                    WidgetType = EUIBinding.WidgetTypes.Extension, ClassName = entry.target.GetType().FullName });
            }
            EUIBindingEditorUtility.ApplyBindingSnapshot(binding, snapshot);
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform; rect.SetParent(parent, false); return rect;
        }

        private static void Row(GameObject root)
        {
            var label = root.GetComponent<EUIToggleEx>().Label;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(92,20); label.rectTransform.offsetMax = new Vector2(-36,-12);
            label.fontSize = 18; label.textWrappingMode = TextWrappingModes.Normal;
            var iconRect = (RectTransform)root.transform.Find("Icon"); iconRect.anchorMin = iconRect.anchorMax = new Vector2(0,.5f);
            iconRect.sizeDelta = new Vector2(28,28); iconRect.anchoredPosition = new Vector2(38,0);
            var icon = iconRect.GetComponent<Image>(); icon.preserveAspect = true; icon.raycastTarget = false;
            icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameResource/Resources/UI/Common/Atlas/DefaultLight/Help.png");
            var alternate = Rect("AlternateIcon", root.transform).gameObject.AddComponent<Image>();
            alternate.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameResource/Resources/UI/Common/Atlas/DefaultLight/Search.png");
            alternate.raycastTarget = false; alternate.gameObject.SetActive(false);
            var progressRect = Rect("Progress", root.transform); progressRect.anchorMin = new Vector2(0,0); progressRect.anchorMax = new Vector2(1,0);
            progressRect.offsetMin = new Vector2(92,8); progressRect.offsetMax = new Vector2(-36,12);
            var progress = progressRect.gameObject.AddComponent<Image>(); progress.color = new Color32(10,100,232,255);
            progress.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameResource/Resources/UI/Common/Atlas/DefaultLight/RoundedSurfaceHiDPI.png");
            progress.type = Image.Type.Filled; progress.fillMethod = Image.FillMethod.Horizontal; progress.fillAmount = 0; progress.raycastTarget = false;
            var layout = root.AddComponent<EUITextRowLayout>();
            using (var so = new SerializedObject(layout)) { so.FindProperty("_text").objectReferenceValue = label; so.ApplyModifiedPropertiesWithoutUndo(); }
            Bind(root, ("Icon",icon), ("Progress",progress));
        }

        private static void Host(GameObject root)
        {
            var layout = root.GetComponent<ScrollRect>().content.GetComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true; layout.childForceExpandWidth = true;
            var host = root.AddComponent<EUIDynamicListHost>();
            using (var so = new SerializedObject(host))
            {
                so.FindProperty("_scroll").objectReferenceValue = root.GetComponent<ScrollRect>();
                so.FindProperty("_itemTemplate").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(MODULE + "EUIDynamicOptionRowItem.prefab");
                so.FindProperty("_dataBinding").stringValue = "Game.UI.EUIDynamicOptionRowItem.Bind(EUIItemLease<EUIOptionRowData>); key=Id; index=display position";
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            Bind(root,("List",host));
        }

        private static void Page(GameObject root)
        {
            var center = root.transform.Find("Animator/EUISafeArea/Center");
            center.Find("Title").GetComponent<TMP_Text>().text = "Dynamic collection";
            var content = center.Find("Content");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODULE + "EUIDynamicListItem.prefab"), content);
            instance.name = "List";
            var rect = (RectTransform)instance.transform;
            rect.anchorMin = new Vector2(.28f,0); rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var names = new[] { "Empty", "One", "Three", "Twenty", "Reverse", "Remove", "Back" };
            for (var i=0;i<names.Length;i++)
            {
                var button = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(COMMON + "EUISecondaryButtonItem.prefab"),content);
                button.name = names[i];
                var r = (RectTransform)button.transform; r.anchorMin = r.anchorMax = new Vector2(.12f,1);
                r.sizeDelta = new Vector2(220,44); r.anchoredPosition = new Vector2(0,-30-i*58);
                var view = button.GetComponent<EUIButtonEx>(); view.Label.text = names[i];
                Bind(root,(names[i],view));
            }
            Bind(root,("List",instance.GetComponent<EUIDynamicListHost>()));
        }
        #endregion
    }
}

using System;
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
    /// <summary>经 EUI 中心创建/绑定正式资源；首次执行后保留用户布局，拒绝重复覆盖。</summary>
    public static class TopdownHudAssets
    {
        #region 内部参数
        private const string MAIN="Assets/GameResource/Resources/UI/Common/Prefabs/EUIMainPage.prefab";
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));var rt=(RectTransform)go.transform;rt.SetParent(parent,false);
            rt.anchorMin=rt.anchorMax=anchor;rt.pivot=new Vector2(0,1);rt.anchoredPosition=position;rt.sizeDelta=size;return rt;
        }
        private static TMP_Text Text(Transform parent,string name,string text,TMP_FontAsset font,Vector2 anchor,Vector2 position,Vector2 size,float fontSize=40)
        {
            var rt=Rect(parent,name,anchor,position,size);var label=rt.gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.fontSize=fontSize;label.text=text;
            label.color=new Color(.86f,.94f,1);label.raycastTarget=false;label.textWrappingMode=TextWrappingModes.Normal;return label;
        }
        private static void Button(Transform parent,string name,string text,TMP_FontAsset font,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var rt=Rect(parent,name,anchor,position,size);var image=rt.gameObject.AddComponent<Image>();image.color=new Color(.13f,.23f,.32f,.98f);
            var button=rt.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;button.navigation=new Navigation{mode=Navigation.Mode.None};
            var label=Text(rt,"Label",text,font,new Vector2(0,1),Vector2.zero,size,38);label.alignment=TextAlignmentOptions.Center;
        }
        private static void Bind(EUIBinding binding,string name,EUIBinding.WidgetTypes type,GameObject target)
        {
            using var so=new SerializedObject(binding);var items=so.FindProperty("bindings");
            for(var i=0;i<items.arraySize;i++)if(items.GetArrayElementAtIndex(i).FindPropertyRelative("Name").stringValue==name)throw new InvalidOperationException("绑定已存在："+name);
            var index=items.arraySize;items.InsertArrayElementAtIndex(index);var item=items.GetArrayElementAtIndex(index);
            item.FindPropertyRelative("Name").stringValue=name;item.FindPropertyRelative("GameObject").objectReferenceValue=target;
            item.FindPropertyRelative("Type").intValue=(int)type;item.FindPropertyRelative("ClassName").stringValue="";item.FindPropertyRelative("IsFramework").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Regenerate(string path)
        {
            var binding=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<EUIBinding>();
            if(!EUIBindingCodeGenUtility.TryRegenerateCode(binding,out var error))throw new InvalidOperationException(error);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Create()
        {
            if(!EmberProjectSetup.IsTemplateActive("topdown-2d")||EditorApplication.isPlayingOrWillChangePlaymode||
                ((Ember.Core.EmberModuleAttribute)Attribute.GetCustomAttribute(typeof(Game.Player.PlayerModule),typeof(Ember.Core.EmberModuleAttribute)))?.Enabled!=true)
                throw new InvalidOperationException("正式俯视模板、启用模块的空闲编辑状态才可制作。");
            var request=new EUICreationRequest{PrefabName="EUITopdownHudPage",PageName="EUITopdownHudPage",ClassName="EUITopdownHudPage",ClassPath="Module/Player",
                PageType=PageType.FreePage,UseUIUpdate=true,UseMask=false,GenerateCustomSettings=false,FixedSortingOrder=100};
            if(!EUICreationService.TryBuildPlan(request,out var plan,out var preflight))throw new InvalidOperationException(preflight.Error);
            var backup="Library/EmberBackups/topdown-2d-t0-20261028/t5-ui-before";Directory.CreateDirectory(backup);
            File.Copy(MAIN,Path.Combine(backup,"EUIMainPage.prefab"),false);File.Copy(MAIN+".meta",Path.Combine(backup,"EUIMainPage.prefab.meta"),false);
            var creation=EUICreationService.Create(request);if(!creation.Success)throw new InvalidOperationException(creation.Error);
            var font=AssetDatabase.LoadAssetAtPath<GameObject>(MAIN).GetComponentsInChildren<TMP_Text>(true).First().font;
            var hud=PrefabUtility.LoadPrefabContents(creation.PrefabPath);
            try
            {
                var binding=hud.GetComponent<EUIBinding>();
                var panel=Rect(hud.transform,"StatusPanel",new Vector2(0,1),new Vector2(38,-36),new Vector2(1080,310));
                var bg=panel.gameObject.AddComponent<Image>();bg.color=new Color(.03f,.07f,.12f,.85f);bg.raycastTarget=false;
                var stats=Text(panel,"Txt_Stats","生命与动作",font,new Vector2(0,1),new Vector2(28,-20),new Vector2(1024,80),48);
                var status=Text(panel,"Txt_Status","进度就绪",font,new Vector2(0,1),new Vector2(28,-105),new Vector2(1024,160),36);
                Bind(binding,"Txt_Stats",EUIBinding.WidgetTypes.Text,stats.gameObject);Bind(binding,"Txt_Status",EUIBinding.WidgetTypes.Text,status.gameObject);
                var names=new[]{"Save","SaveExit","Load","Retry","Pause","Acknowledge"};var labels=new[]{"保存 · 槽位 0","保存并退出","读取槽位 0","检查点重试","暂停 / 继续","确认写入错误"};
                for(var i=0;i<names.Length;i++)
                {var name="Btn_"+names[i];Button(hud.transform,name,labels[i],font,new Vector2(0,0),new Vector2(38+i*360,170),new Vector2(340,90));Bind(binding,name,EUIBinding.WidgetTypes.Button,hud.transform.Find(name).gameObject);}
                Text(hud.transform,"Help","WASD / 左摇杆 移动   鼠标 / 右摇杆 瞄准   J / 左键 近战   Shift 冲刺   E 检查点   Esc 暂停",font,
                    new Vector2(0,0),new Vector2(38,65),new Vector2(2400,55),34);
                PrefabUtility.SaveAsPrefabAsset(hud,creation.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(hud);}
            Regenerate(creation.PrefabPath);
            var main=PrefabUtility.LoadPrefabContents(MAIN);
            try
            {
                Button(main.transform,"Btn_Continue","继续 · 槽位 0",font,new Vector2(.5f,.5f),new Vector2(-280,-300),new Vector2(560,90));
                Bind(main.GetComponent<EUIBinding>(),"Btn_Continue",EUIBinding.WidgetTypes.Button,main.transform.Find("Btn_Continue").gameObject);
                var status=Text(main.transform,"Txt_Progress","没有存档时请开始新游戏",font,new Vector2(.5f,.5f),new Vector2(-650,-410),new Vector2(1300,120),34);
                status.alignment=TextAlignmentOptions.Center;Bind(main.GetComponent<EUIBinding>(),"Txt_Progress",EUIBinding.WidgetTypes.Text,status.gameObject);
                PrefabUtility.SaveAsPrefabAsset(main,MAIN);
            }
            finally{PrefabUtility.UnloadPrefabContents(main);}
            Regenerate(MAIN);AssetDatabase.SaveAssets();
            File.WriteAllText("docs/dev/topdown-2d-evidence/t5-eui-creation.json",UnityEngine.JsonUtility.ToJson(creation,true));
        }
        #endregion
    }
}

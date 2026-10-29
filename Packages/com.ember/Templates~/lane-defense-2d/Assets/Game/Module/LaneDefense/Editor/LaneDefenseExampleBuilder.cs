using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ember.Core;
using Ember.Core.Editor;
using Ember.UI;
using Ember.UIExtension;
using Ember.UIExtension.Editor;
using Game.Input;
using Game.SceneFlow;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Game.LaneDefense.Editor
{
    /// <summary>显式制作操作；不在域重载或运行时自动重建用户资产。</summary>
    public static class LaneDefenseExampleBuilder
    {
        #region 内部参数
        private const string CONFIG="Assets/GameResource/Resources/Config/LaneDefense/";
        private const string PREFABS="Assets/GameResource/Prefabs/LaneDefense/";
        private const string UI="Assets/GameResource/Resources/UI/Module/LaneDefense/Prefabs/";
        private static readonly Color PANEL=new Color(.055f,.09f,.13f,.97f);
        private static readonly Color ACCENT=new Color(.1f,.46f,.5f,1);
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
            var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=anchor;rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=position;rt.sizeDelta=size;return rt;
        }
        private static TMP_Text Text(string name,Transform parent,string value,Vector2 position,Vector2 size,float fontSize=38)
        {
            var rt=Rect(name,parent,new Vector2(.5f,.5f),position,size);var text=rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/GameResource/Resources/UI/Common/Fonts/DefaultLightSans.asset");
            if(text.font==null)text.font=TMP_Settings.defaultFontAsset;
            text.fontSize=fontSize;text.text=value;text.alignment=TextAlignmentOptions.MidlineLeft;text.color=new Color(.88f,.94f,.97f);text.raycastTarget=false;return text;
        }
        private static Button Button(string name,Transform parent,string label,Vector2 position,Vector2 size)
        {
            var rt=Rect(name,parent,new Vector2(.5f,.5f),position,size);var image=rt.gameObject.AddComponent<Image>();image.color=ACCENT;
            var button=rt.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var text=Text("Label",rt,label,Vector2.zero,size-new Vector2(20,8),34);text.alignment=TextAlignmentOptions.Center;return button;
        }
        private static EUIBindingEntrySnapshot Bind(GameObject root,Component component,string name,EUIBinding.WidgetTypes type,string className=null)
            =>new EUIBindingEntrySnapshot{Name=name,GameObjectPath=AnimationUtility.CalculateTransformPath(component.transform,root.transform),WidgetType=type,ClassName=className};
        private static void SaveUI(string name,Action<GameObject,List<EUIBindingEntrySnapshot>> layout)
        {
            string path=UI+name+".prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                if(root.transform.Find("LaneContent")!=null||root.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="LaneContent"))throw new InvalidOperationException("Already authored; refusing destructive rebuild: "+name);
                var entries=new List<EUIBindingEntrySnapshot>();layout(root,entries);
                EUIBindingEditorUtility.SetBindings(root.GetComponent<EUIBinding>(),entries);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            var saved=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<EUIBinding>();
            if(!EUIBindingCodeGenUtility.TryRegenerateCode(saved,out string error))throw new InvalidOperationException(error);
        }
        private static RectTransform Panel(GameObject root,Vector2 anchor,Vector2 position,Vector2 size,bool image=true)
        {
            var parent=root.transform.Find("Animator/EUISafeArea/Center")??root.transform;
            var rt=Rect("LaneContent",parent,anchor,position,size);
            if(image)rt.gameObject.AddComponent<Image>().color=PANEL;return rt;
        }
        private static void SetObject(UnityEngine.Object obj,string field,UnityEngine.Object value)
        {var so=new SerializedObject(obj);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        private static GameObject SpritePrefab(string name,Sprite sprite,Vector3 scale,Color color,int order)
        {
            string path=PREFABS+name+".prefab";var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing!=null)return existing;
            var go=new GameObject(name,typeof(SpriteRenderer));
            try
            {
                go.transform.localScale=scale;var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.color=color;renderer.sortingOrder=order;
                if(name=="Unit"||name=="Enemy")go.AddComponent<SortingGroup>();
                return PrefabUtility.SaveAsPrefabAsset(go,path);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        private static void Level(string file,int rows,int columns,int resource,WaveDefinition[] waves)
        {
            string path=CONFIG+file+".asset";if(AssetDatabase.LoadAssetAtPath<LaneDefenseLevel>(path)!=null)return;
            var level=ScriptableObject.CreateInstance<LaneDefenseLevel>();
            level.Configure(file,rows,columns,new Vector2(1.2f,1),resource,5,
                new[]{new UnitDefinition("sentry","SENTRY",50,100),new UnitDefinition("barrier","BARRIER",30,350,false)},
                new[]{new EnemyDefinition("walker"),new EnemyDefinition("scout",50,.8f,10,1,1)},waves);
            if(level.Validate().Count!=0)throw new InvalidOperationException(string.Join("\n",level.Validate()));
            AssetDatabase.CreateAsset(level,path);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static bool CanAuthor=>EmberProjectSetup.IsTemplateActive("lane-defense-2d")
            &&((EmberModuleAttribute)Attribute.GetCustomAttribute(typeof(LaneDefenseModule),typeof(EmberModuleAttribute)))?.Enabled==true;
        public static void RequireAccess()
        {if(!CanAuthor||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Requires active lane-defense template and enabled module in Edit mode.");}

        public static void BuildResourcesAndScene()
        {
            RequireAccess();Directory.CreateDirectory(CONFIG);Directory.CreateDirectory(PREFABS);
            string texturePath=PREFABS+"NeutralSquare.png";
            if(!File.Exists(texturePath))
            {
                var texture=new Texture2D(16,16,TextureFormat.RGBA32,false);var pixels=Enumerable.Repeat(Color.white,256).ToArray();texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(texturePath,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(texturePath);var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=16;importer.filterMode=FilterMode.Point;importer.SaveAndReimport();
            }
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            var cell=SpritePrefab("Cell",sprite,Vector3.one,Color.white,0);
            var unit=SpritePrefab("Unit",sprite,new Vector3(.65f,.65f,1),new Color(.2f,.8f,.8f),100);
            var enemy=SpritePrefab("Enemy",sprite,new Vector3(.65f,.65f,1),new Color(.9f,.4f,.4f),100);
            var projectile=SpritePrefab("Projectile",sprite,new Vector3(.16f,.1f,1),new Color(1,.9f,.65f),1500);
            Level("TrainingYard",5,9,650,new[]{new WaveDefinition("first",new SpawnEntry("walker",0,1),new SpawnEntry("walker",2,3),new SpawnEntry("walker",4,5)),new WaveDefinition("second",new SpawnEntry("scout",1,1),new SpawnEntry("walker",3,2),new SpawnEntry("walker",0,4),new SpawnEntry("scout",4,5))});
            Level("CompactYard",3,6,450,new[]{new WaveDefinition("compact-a",new SpawnEntry("walker",0,1),new SpawnEntry("scout",2,3)),new WaveDefinition("empty-interlude"),new WaveDefinition("compact-b",new SpawnEntry("walker",1,1),new SpawnEntry("scout",0,2),new SpawnEntry("walker",2,3))});
            // Dedicated input asset, consumed by the existing base input service and transition gate.
            const string actionsPath=CONFIG+"LaneDefenseActions.asset";
            var actions=AssetDatabase.LoadAssetAtPath<InputActionAsset>(actionsPath);
            if(actions==null)
            {
                actions=ScriptableObject.CreateInstance<InputActionAsset>();var map=actions.AddActionMap("Gameplay");
                var point=map.AddAction("Point",InputActionType.Value,"<Pointer>/position");point.expectedControlType="Vector2";
                map.AddAction("Place",InputActionType.Button,"<Pointer>/press");var cancel=map.AddAction("Cancel",InputActionType.Button,"<Keyboard>/escape");cancel.AddBinding("<Mouse>/rightButton");
                AssetDatabase.CreateAsset(actions,actionsPath);
            }
            var input=AssetDatabase.LoadAssetAtPath<GameInputConfig>("Assets/GameResource/Resources/Config/GameInput/GameInputConfig.asset");
            if(input==null)throw new InvalidOperationException("Base input config missing.");
            input.Configure(actions,new GameInputChannelConfig("gameplay","Gameplay",true));EditorUtility.SetDirty(input);
            string scenePath="Assets/Game/Scenes/GameplayScene.unity";
            var scene=SceneManager.GetSceneByPath(scenePath);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
            try
            {
                if(scene.GetRootGameObjects().Any(g=>g.name=="LaneDefenseBoard"))throw new InvalidOperationException("Scene already authored; use Inspector instead of rebuilding.");
                var root=new GameObject("LaneDefenseBoard");SceneManager.MoveGameObjectToScene(root,scene);
                var cameraObject=new GameObject("LaneDefenseCamera",typeof(Camera));cameraObject.transform.SetParent(root.transform,false);var camera=cameraObject.GetComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=5;camera.depth=-.5f;camera.cullingMask=~(1<<LayerMask.NameToLayer("UI"));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.055f,.08f);camera.transform.position=new Vector3(0,0,-10);
                var view=root.AddComponent<LaneDefenseBoardView>();SetObject(view,"_camera",camera);SetObject(view,"_cellPrefab",cell);SetObject(view,"_unitPrefab",unit);SetObject(view,"_enemyPrefab",enemy);SetObject(view,"_projectilePrefab",projectile);
                var participant=root.AddComponent<LaneDefenseSceneParticipant>();SetObject(participant,"_level",AssetDatabase.LoadAssetAtPath<LaneDefenseLevel>(CONFIG+"TrainingYard.asset"));SetObject(participant,"_view",view);
                var scope=GameSceneScope.Require(scene);var scopeData=new SerializedObject(scope);var items=scopeData.FindProperty("_participants");int count=items.arraySize;items.arraySize=count+1;items.GetArrayElementAtIndex(count).objectReferenceValue=participant;scopeData.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            AssetDatabase.SaveAssets();
        }

        public static void BuildUILayout()
        {
            RequireAccess();
            string backup="Library/EmberBackups/lane-defense-ui-before-layout";Directory.CreateDirectory(backup);
            foreach(string file in Directory.GetFiles(UI))File.Copy(file,Path.Combine(backup,Path.GetFileName(file)),false);
            SaveUI("EUILaneBattleHUD",(root,b)=>
            {
                var top=Panel(root,new Vector2(.5f,1),new Vector2(0,-100),new Vector2(2300,150));
                var stats=Text("Stats",top,"LANE DEFENSE",new Vector2(-300,24),new Vector2(1500,60));b.Add(Bind(root,stats,"Txt_Stats",EUIBinding.WidgetTypes.Extension,"TMPro.TextMeshProUGUI"));
                var feedback=Text("Feedback",top,"Select a card, then an empty cell.",new Vector2(-300,-35),new Vector2(1500,48),30);b.Add(Bind(root,feedback,"Txt_Feedback",EUIBinding.WidgetTypes.Extension,"TMPro.TextMeshProUGUI"));
                b.Add(Bind(root,Button("Pause",top,"PAUSE",new Vector2(970,0),new Vector2(260,74)),"Btn_Pause",EUIBinding.WidgetTypes.Button));
                b.Add(Bind(root,Button("Start",top,"START WAVE",new Vector2(640,0),new Vector2(340,74)),"Btn_Start",EUIBinding.WidgetTypes.Button));
                var bottom=Rect("Cards",root.transform.Find("Animator/EUISafeArea/Center"),new Vector2(.5f,0),new Vector2(-270,150),new Vector2(1300,190));
                var layout=bottom.gameObject.AddComponent<HorizontalLayoutGroup>();layout.spacing=24;layout.childControlWidth=false;layout.childControlHeight=false;layout.childForceExpandWidth=false;layout.childForceExpandHeight=false;layout.childAlignment=TextAnchor.MiddleCenter;
                b.Add(Bind(root,bottom,"Root_Cards",EUIBinding.WidgetTypes.Extension,"UnityEngine.RectTransform"));
                var controls=Rect("Controls",root.transform.Find("Animator/EUISafeArea/Center"),new Vector2(.5f,0),new Vector2(800,150),new Vector2(500,190));
                b.Add(Bind(root,Button("Cancel",controls,"CANCEL",new Vector2(0,50),new Vector2(380,70)),"Btn_Cancel",EUIBinding.WidgetTypes.Button));
                b.Add(Bind(root,Button("Dismantle",controls,"DISMANTLE",new Vector2(0,-45),new Vector2(380,70)),"Btn_Demolish",EUIBinding.WidgetTypes.Button));
            });
            SaveUI("EUILaneCardItem",(root,b)=>
            {
                var rt=(RectTransform)root.transform;rt.sizeDelta=new Vector2(430,170);
                var panel=Panel(root,new Vector2(.5f,.5f),Vector2.zero,new Vector2(430,170));
                b.Add(Bind(root,Button("Select",panel,"",Vector2.zero,new Vector2(430,170)),"Btn_Select",EUIBinding.WidgetTypes.Button));
                b.Add(Bind(root,Text("Card",panel,"CARD",new Vector2(0,35),new Vector2(390,60)),"Txt_Card",EUIBinding.WidgetTypes.Extension,"TMPro.TextMeshProUGUI"));
                b.Add(Bind(root,Text("State",panel,"READY",new Vector2(0,-40),new Vector2(390,65),28),"Txt_State",EUIBinding.WidgetTypes.Extension,"TMPro.TextMeshProUGUI"));
            });
            foreach(string name in new[]{"EUILanePausePopup","EUILaneResultPopup"})SaveUI(name,(root,b)=>
            {
                var panel=Panel(root,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1000,670));
                b.Add(Bind(root,Text("Title",panel,name.Contains("Pause")?"BATTLE PAUSED":"BATTLE RESULT",new Vector2(0,240),new Vector2(880,75),56),"Txt_Title",EUIBinding.WidgetTypes.Extension,"TMPro.TextMeshProUGUI"));
                b.Add(Bind(root,Text("Detail",panel,"",new Vector2(0,130),new Vector2(880,90),32),"Txt_Detail",EUIBinding.WidgetTypes.Extension,"TMPro.TextMeshProUGUI"));
                if(name.Contains("Pause"))b.Add(Bind(root,Button("Resume",panel,"RESUME",new Vector2(0,10),new Vector2(760,80)),"Btn_Resume",EUIBinding.WidgetTypes.Button));
                b.Add(Bind(root,Button("Restart",panel,"RESTART",new Vector2(0,-100),new Vector2(760,80)),"Btn_Restart",EUIBinding.WidgetTypes.Button));
                b.Add(Bind(root,Button("Menu",panel,"MAIN MENU",new Vector2(0,-210),new Vector2(760,80)),"Btn_Menu",EUIBinding.WidgetTypes.Button));
            });
            AssetDatabase.SaveAssets();
        }
        #endregion
    }
}

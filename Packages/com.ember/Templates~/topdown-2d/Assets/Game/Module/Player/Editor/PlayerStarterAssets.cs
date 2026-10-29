using System;
using System.IO;
using System.Linq;
using Ember.Camera;
using Ember.Core;
using Ember.Core.Editor;
using Game.Input;
using Game.Module;
using Game.PlayerCamera;
using Game.SceneFlow;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Editor
{
    /// <summary>一次性制作起点；只写活动项目 Assets，不写模板管理快照。</summary>
    [InitializeOnLoad]
    public static class PlayerStarterAssets
    {
        #region 内部参数
        private const string PLAYER_PATH = "Assets/GameResource/Player/Topdown/Player.prefab";
        private static readonly IDisposable Menu;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        static PlayerStarterAssets()
        {
            Menu = EmberConditionalMenu.Register("Ember/Player/创建俯视起点",
                () => EmberProjectSetup.IsTemplateActive("topdown-2d"), Create,
                () => PlayerEditorAccess.ModuleEnabled&&!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling);
        }

        private static void Reference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            using var data = new SerializedObject(target);
            data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite CreateSprite()
        {
            const string path = "Assets/GameResource/Player/Topdown/Square.png";
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var pixels = Enumerable.Repeat(Color.white, 1024).ToArray(); texture.SetPixels(pixels); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point; importer.alphaIsTransparency = true; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path)
                ?? throw new InvalidOperationException("占位图未导入为 Sprite，不能制作带缺失表现引用的起点。");
        }

        private static GameObject SpriteObject(string name, Sprite sprite, Vector2 position, Vector2 size, Color color, int order)
        {
            var root = new GameObject(name); root.transform.position = position; root.transform.localScale = new Vector3(size.x,size.y,1);
            var renderer = root.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order;
            return root;
        }

        private static void CreateInput()
        {
            const string path = "Assets/GameResource/Resources/Config/GameInput/Topdown.inputactions";
            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = actions.AddActionMap("Gameplay");
            var move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");
            map.AddAction("Aim", InputActionType.Value, "<Gamepad>/rightStick", expectedControlLayout: "Vector2");
            map.AddAction("Point", InputActionType.Value, "<Mouse>/position", expectedControlLayout: "Vector2");
            map.AddAction("Attack", InputActionType.Button, "<Mouse>/leftButton").AddBinding("<Keyboard>/j");
            map.FindAction("Attack").AddBinding("<Gamepad>/buttonWest");
            map.AddAction("Dash", InputActionType.Button, "<Keyboard>/leftShift").AddBinding("<Gamepad>/rightShoulder");
            map.AddAction("Interact", InputActionType.Button, "<Keyboard>/e").AddBinding("<Gamepad>/buttonSouth");
            map.AddAction("NextWeapon",InputActionType.Button,"<Keyboard>/q").AddBinding("<Gamepad>/leftShoulder");
            var ui = actions.AddActionMap("UI");
            ui.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape").AddBinding("<Gamepad>/start");
            File.WriteAllText(path, actions.ToJson()); UnityEngine.Object.DestroyImmediate(actions);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var config = AssetDatabase.LoadAssetAtPath<GameInputConfig>("Assets/GameResource/Resources/Config/GameInput/GameInputConfig.asset");
            config.Configure(AssetDatabase.LoadAssetAtPath<InputActionAsset>(path),
                new GameInputChannelConfig("gameplay", "Gameplay"), new GameInputChannelConfig("ui", "UI", true));
            EditorUtility.SetDirty(config);
            var storage = AssetDatabase.LoadAssetAtPath<PlayerDataConfig>("Assets/GameResource/Resources/Config/PlayerData/PlayerDataConfig.asset");
            storage.Configure("TopdownProgress", "ember.topdown2d.progress", new[] { 0, 1, 2 }); EditorUtility.SetDirty(storage);
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public static void Create()
        {
            PlayerEditorAccess.RequireWrite();
            if (!EmberProjectSetup.IsTemplateActive("topdown-2d") || EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("仅活动俯视模板的空闲编辑模式可制作起点。");
            if (File.Exists(PLAYER_PATH)) throw new InvalidOperationException("俯视起点已存在，禁止覆盖已有玩家或关卡。");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PLAYER_PATH)); AssetDatabase.Refresh();
                var sprite = CreateSprite(); CreateInput();
                var framework = EditorSceneManager.OpenScene("Assets/Game/Scenes/FrameworkScene.unity");
                foreach (var root in framework.GetRootGameObjects())
                {
                    foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                    {
                        if (camera.cullingMask == (1 << 5)) continue;
                        camera.orthographic = true; camera.orthographicSize = 8;
                        camera.clearFlags = CameraClearFlags.SolidColor;
                        camera.transform.SetPositionAndRotation(new Vector3(0, 0, -10), Quaternion.identity);
                        camera.backgroundColor = new Color(.035f, .045f, .07f);
                    }
                    foreach (var camera in root.GetComponentsInChildren<CinemachineCamera>(true))
                    {
                        camera.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic; camera.Lens.OrthographicSize = 8;
                        camera.transform.SetPositionAndRotation(new Vector3(0, 0, -10), Quaternion.identity);
                    }
                }
                EditorSceneManager.SaveScene(framework);
                var scene = EditorSceneManager.OpenScene("Assets/Game/Scenes/GameplayScene.unity");
                var scope = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<GameSceneScope>(true)).Single();
                var player = new GameObject("Topdown Player") { layer = 2 };
                var body = player.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
                body.constraints = RigidbodyConstraints2D.FreezeRotation; body.simulated = false;
                var shape = player.AddComponent<BoxCollider2D>(); shape.size = new Vector2(.7f, .7f);
                var visual = SpriteObject("Body", sprite, Vector2.zero, new Vector2(.7f,.7f), new Color(.2f,.85f,1), 10);
                visual.transform.SetParent(player.transform, false);
                var facing = SpriteObject("Facing", sprite, new Vector2(.5f,0), new Vector2(.35f,.12f), new Color(1,.85f,.25f), 11);
                facing.transform.SetParent(player.transform, true);
                player.AddComponent<PlayerController>(); player.AddComponent<PlayerSceneParticipant>();
                var prefab = PrefabUtility.SaveAsPrefabAsset(player, PLAYER_PATH); UnityEngine.Object.DestroyImmediate(player);
                player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var cameraRoot = new GameObject("Topdown Camera"); cameraRoot.transform.position = new Vector3(0,0,-10);
                var vcam = cameraRoot.AddComponent<CinemachineCamera>();
                vcam.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic; vcam.Lens.OrthographicSize = 8;
                var follow = cameraRoot.AddComponent<CinemachineFollow>();
                follow.TrackerSettings.BindingMode = BindingMode.WorldSpace; follow.FollowOffset = new Vector3(0,0,-10);
                var registration = cameraRoot.AddComponent<EmberCameraRegistration>();
                using (var data = new SerializedObject(registration))
                { data.FindProperty("_cameraKey").stringValue="topdown.player"; data.FindProperty("_activateOnAwake").boolValue=false; data.ApplyModifiedPropertiesWithoutUndo(); }
                var cameraParticipant = scope.gameObject.AddComponent<PlayerCameraParticipant>();
                Reference(cameraParticipant,"_player",player.GetComponent<PlayerController>());
                Reference(cameraParticipant,"_camera",vcam); Reference(cameraParticipant,"_registration",registration);
                using (var data = new SerializedObject(scope))
                {
                    var participants=data.FindProperty("_participants"); participants.arraySize=2;
                    participants.GetArrayElementAtIndex(0).objectReferenceValue=cameraParticipant;
                    participants.GetArrayElementAtIndex(1).objectReferenceValue=player.GetComponent<PlayerSceneParticipant>();
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                SpriteObject("Arena Floor",sprite,Vector2.zero,new Vector2(24,18),new Color(.09f,.13f,.17f),-10);
                var walls = new[] {new Vector4(-12,0,1,19),new Vector4(12,0,1,19),new Vector4(0,-9,25,1),new Vector4(0,9,25,1),new Vector4(3,2,1,5)};
                for(var i=0;i<walls.Length;i++)
                {
                    var wall=walls[i]; var root=SpriteObject("Wall "+i,sprite,new Vector2(wall.x,wall.y),new Vector2(wall.z,wall.w),new Color(.25f,.32f,.4f),1);
                    root.AddComponent<BoxCollider2D>();
                }
                EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        #endregion
    }
}

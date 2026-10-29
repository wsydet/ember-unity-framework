using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Player.Editor
{
    public static class PlayerProgressExampleAssets
    {
        #region 外部方法
        public static void Create()
        {
            PlayerEditorAccess.RequireWrite();var path="Assets/GameResource/Player/Topdown/Checkpoint.prefab";
            if(File.Exists(path))throw new InvalidOperationException("进度示例已存在，禁止覆盖。");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            try
            {
                var root=new GameObject("Checkpoint");GameObject prefab;
                try
                {
                    root.AddComponent<TopdownInteraction>();var renderer=root.AddComponent<SpriteRenderer>();
                    renderer.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameResource/Player/Topdown/Square.png");renderer.color=new Color(.1f,.65f,.75f);renderer.sortingOrder=8;
                    root.transform.localScale=new Vector3(.7f,.7f,1);prefab=PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
                var scene=EditorSceneManager.OpenScene("Assets/Game/Scenes/GameplayScene.unity");
                var scope=Game.SceneFlow.GameSceneScope.Require(scene);if(scope.GetComponent<TopdownLevelIdentity>()!=null)throw new InvalidOperationException("关卡身份已存在。");
                scope.gameObject.AddComponent<TopdownLevelIdentity>();var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);instance.transform.position=new Vector2(0,-2);
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        #endregion
    }
}

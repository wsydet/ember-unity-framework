#if UNITY_EDITOR
using Game.Module;
using UnityEngine;

namespace Game.Player
{
    /// <summary>Editor 适配器明确装配的单次试玩数据；Player 构建不包含此入口。</summary>
    public static class PlayerTrialBridge
    {
        #region 内部参数
        public static bool Active {get;private set;}
        public static string ScenePath {get;private set;}
        public static PlayerScheme Scheme {get;private set;}
        public static Vector2? Position {get;private set;}
        public static PlayerDataConfig Storage {get;private set;}
        public static string StorageRoot {get;private set;}
        public static System.Action StartGameplay {get;set;}
        public static bool AutoEnterPending {get;set;}
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [Ember.Basic.NoGC]
        public static void Configure(string scene,PlayerScheme scheme,Vector2? position,PlayerDataConfig storage,string root)
        {Active=true;AutoEnterPending=true;ScenePath=scene;Scheme=scheme;Position=position;Storage=storage;StorageRoot=root;}
        [Ember.Basic.NoGC]
        public static void Clear(){Active=false;AutoEnterPending=false;ScenePath=null;Scheme=null;Position=null;Storage=null;StorageRoot=null;StartGameplay=null;}
        #endregion
    }
}
#endif

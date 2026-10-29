using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Player
{
    /// <summary>明确的世界组合位置；仅拥有本模板交互样例，不建立通用对象数据库。</summary>
    internal sealed class PlayerWorldSession:IDisposable
    {
        #region 内部参数
        private readonly PlayerContext _player;
        private readonly TopdownInteraction[] _objects;
        private readonly bool[] _values;
        private readonly Color[] _baseColors;
        private bool _started;
        internal string Scene {get;}
        internal string LevelId {get;}
        internal string SpawnId {get;}
        internal Vector2 Checkpoint {get;private set;}
        internal bool Persistable {get;}
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Show(int i)
        {
            if(_objects[i]==null)return;
            var sprite=_objects[i].GetComponentInChildren<SpriteRenderer>();
            if(sprite!=null)sprite.color=_values[i]?new Color(.25f,1,.65f):_baseColors[i];
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerWorldSession(PlayerContext player,TopdownSnapshot restore)
        {
            _player=player;var scene=player.Controller.gameObject.scene;TopdownLevelIdentity identity=null;
            var objects=new List<TopdownInteraction>();
            foreach(var root in scene.GetRootGameObjects())
            {
                foreach(var found in root.GetComponentsInChildren<TopdownLevelIdentity>(true))
                {if(identity!=null)throw new InvalidOperationException("俯视关卡身份必须唯一。");identity=found;}
                foreach(var item in root.GetComponentsInChildren<TopdownInteraction>(true))if(item.isActiveAndEnabled)objects.Add(item);
            }
            Persistable=identity!=null;
            Scene=identity==null?scene.path:identity.ScenePath;LevelId=identity==null?scene.name:identity.LevelId;SpawnId=identity==null?"start":identity.SpawnId;
            if(identity!=null)
            {
                TopdownSnapshot.ValidateId(Scene);TopdownSnapshot.ValidateId(LevelId);TopdownSnapshot.ValidateId(SpawnId);
                if(!string.IsNullOrEmpty(scene.path)&&scene.path!=Scene)throw new InvalidOperationException("关卡声明的场景路径与实际场景不一致。");
                if(identity.Configuration!=null&&(identity.Configuration is not IPlayerLevelConfiguration config||config.Id!=LevelId||config.ScenePath!=Scene))
                    throw new InvalidOperationException("关卡配置引用或身份不一致。");
            }
            Checkpoint=restore?.Checkpoint??player.Controller.Body.position;
            objects.Sort((a,b)=>string.CompareOrdinal(a.StableId,b.StableId));_objects=objects.ToArray();_values=new bool[_objects.Length];_baseColors=new Color[_objects.Length];
            var ids=new HashSet<string>(StringComparer.Ordinal);
            for(var i=0;i<_objects.Length;i++)
            {
                var item=_objects[i];TopdownSnapshot.ValidateId(item.StableId);
                if(!ids.Add(item.StableId)||!float.IsFinite(item.Radius)||item.Radius<=0)throw new InvalidOperationException("交互身份重复或半径无效。");
                _baseColors[i]=item.InitialColor;
            }
            if(restore!=null)
            {
                if(!Persistable||restore.Scene!=Scene||restore.LevelId!=LevelId||restore.SpawnId!=SpawnId||restore.InteractionCount!=_objects.Length)
                    throw new InvalidOperationException("快照场景、出生或世界交互装配不匹配。");
                for(var i=0;i<_objects.Length;i++)
                {var record=restore.GetInteraction(i);if(record.Id!=_objects[i].StableId)throw new InvalidOperationException("快照交互身份不匹配。");_values[i]=record.Activated;}
            }
        }
        internal void ValidateSources(){for(var i=0;i<_objects.Length;i++)if(_objects[i]==null||!_objects[i].isActiveAndEnabled)throw new InvalidOperationException("待交接的世界对象已失效。");}
        internal void Start(){ValidateSources();_started=true;for(var i=0;i<_objects.Length;i++)Show(i);}
        internal bool Interact()
        {
            if(!_started||_player.IsDead)return false;
            var best=-1;var distance=float.PositiveInfinity;
            for(var i=0;i<_objects.Length;i++)
            {
                var item=_objects[i];if(item==null||!item.isActiveAndEnabled)continue;
                var d=((Vector2)item.transform.position-_player.Controller.Body.position).sqrMagnitude;
                if(d<=item.Radius*item.Radius&&d<distance){distance=d;best=i;}
            }
            if(best<0)return false;
            _values[best]=!_values[best];Show(best);
            if(_objects[best].IsCheckpoint){Checkpoint=_player.Controller.Body.position;return true;}return false;
        }
        internal TopdownInteractionRecord[] Capture()
        {
            var records=new TopdownInteractionRecord[_objects.Length];
            for(var i=0;i<records.Length;i++){if(_objects[i]==null)throw new InvalidOperationException("交互对象失效，不能遗漏世界进度。");records[i]=new TopdownInteractionRecord(_objects[i].StableId,_values[i]);}return records;
        }
        public void Dispose(){if(!_started)return;_started=false;for(var i=0;i<_objects.Length;i++){if(_objects[i]==null)continue;var sprite=_objects[i].GetComponentInChildren<SpriteRenderer>();if(sprite!=null)sprite.color=_baseColors[i];}}
        #endregion
    }
}

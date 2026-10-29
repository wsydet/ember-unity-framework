using System;
using System.Collections.Generic;
using Game.Entity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Player
{
    /// <summary>选择性适配平台原生 Overlap/Cast 流程，所有姿态由同一平面方向变换产生。</summary>
    internal sealed class PlayerHitQuery:IDisposable
    {
        #region 内部参数
        private readonly GameObject _object;
        private readonly BoxCollider2D _box;
        private readonly List<Collider2D> _overlaps=new(32);
        private readonly List<RaycastHit2D> _casts=new(32);
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerHitQuery(Scene scene)
        {
            _object=new GameObject("Topdown combat query"){hideFlags=HideFlags.HideAndDontSave,layer=2};
            SceneManager.MoveGameObjectToScene(_object,scene);
            var body=_object.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.gravityScale=0;
            _box=_object.AddComponent<BoxCollider2D>();_box.isTrigger=true;_box.excludeLayers=-1;_box.enabled=false;
        }
        internal void Query(Vector2 offset,Vector2 size,Vector2 forward,Vector2 from,Vector2 to,Action<Collider2D> found)
        {
            _box.size=size;_box.enabled=true;
            try
            {
                var filter=new ContactFilter2D{useTriggers=true};filter.SetLayerMask(-1);
                var worldOffset=EntityPlaneFrame.Transform(offset,forward);from+=worldOffset;to+=worldOffset;
                var angle=EntityPlaneFrame.Angle(forward);
                _overlaps.Clear();_box.Overlap(from,angle,filter,_overlaps);foreach(var hit in _overlaps)found(hit);
                var delta=to-from;
                if(delta.sqrMagnitude>.00000001f)
                {_casts.Clear();_box.Cast(from,angle,delta.normalized,filter,_casts,delta.magnitude,true);foreach(var hit in _casts)found(hit.collider);}
                _overlaps.Clear();_box.Overlap(to,angle,filter,_overlaps);foreach(var hit in _overlaps)found(hit);
            }
            finally{_box.enabled=false;}
        }
        internal IReadOnlyList<RaycastHit2D> Ray(Vector2 origin,Vector2 direction,float range)
        {
            _casts.Clear();var filter=new ContactFilter2D{useTriggers=true};filter.SetLayerMask(-1);
            _object.scene.GetPhysicsScene2D().Raycast(origin,direction,range,filter,_casts);return _casts;
        }
        public void Dispose(){if(_object!=null)UnityEngine.Object.DestroyImmediate(_object);}
        #endregion
    }
}

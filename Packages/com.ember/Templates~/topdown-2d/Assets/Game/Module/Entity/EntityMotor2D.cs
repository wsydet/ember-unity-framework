using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Entity
{
    /// <summary>零重力平面扫掠与滑动。统一物理步只提交一次 MovePosition，不承接平台地空规则。</summary>
    public sealed class EntityMotor2D
    {
        #region 内部参数
        private readonly Rigidbody2D _body;
        private readonly BoxCollider2D _shape;
        private readonly float _skin;
        private readonly List<RaycastHit2D> _hits=new(16);
        private readonly List<Collider2D> _overlaps=new(16);
        private ContactFilter2D _filter;
        public Vector2 Destination {get;private set;}
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EntityMotor2D(Rigidbody2D body,BoxCollider2D shape,LayerMask obstacles,float skin)
        {
            _body=body;_shape=shape;_skin=skin;
            _filter=new ContactFilter2D{useTriggers=false};_filter.SetLayerMask(obstacles);
            if(body==null||!float.IsFinite(skin)||skin<=0)throw new ArgumentException("俯视运动需要刚体和正有限 Skin。");
            if(_shape==null||_shape.isTrigger||_shape.offset!=Vector2.zero||Mathf.Abs(_body.rotation)>.01f)
                throw new InvalidOperationException("俯视身体要求根上的零偏移 BoxCollider 与零旋转刚体。");
        }
        [Ember.Basic.HasGC]
        public bool CanOccupy(Vector2 position)
        {
            if(!float.IsFinite(position.x)||!float.IsFinite(position.y))return false;
            _overlaps.Clear();_shape.Overlap(position,0,_filter,_overlaps);
            foreach(var hit in _overlaps)if(hit!=null&&hit.attachedRigidbody!=_body)return false;
            return true;
        }
        [Ember.Basic.HasGC]
        public Vector2 Step(Vector2 velocity,float dt)
        {
            if(!float.IsFinite(dt)||dt<=0||!float.IsFinite(velocity.x)||!float.IsFinite(velocity.y))
                throw new ArgumentOutOfRangeException(nameof(dt));
            var start=_body.position;var position=start;var remaining=velocity*dt;
            for(var iteration=0;iteration<4&&remaining.sqrMagnitude>.00000001f;iteration++)
            {
                var distance=remaining.magnitude;var direction=remaining/distance;_hits.Clear();
                _shape.Cast(position,0,direction,_filter,_hits,distance+_skin,true);
                var travel=distance;var normal=Vector2.zero;
                foreach(var hit in _hits)
                {
                    if(hit.collider==null||hit.rigidbody==_body||Vector2.Dot(direction,hit.normal)>=-.0001f)continue;
                    var allowed=Mathf.Max(0,hit.distance-_skin);
                    if(allowed<travel){travel=allowed;normal=hit.normal;}
                }
                position+=direction*travel;
                if(normal==Vector2.zero)break;
                remaining-=direction*travel;
                remaining-=normal*Mathf.Min(0,Vector2.Dot(remaining,normal));
            }
            Destination=position;_body.MovePosition(position);
            return (position-start)/dt;
        }
        #endregion
    }
}

using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>俯视角色根。场景准备拥有会话，PlayerModule 是唯一运行驱动。</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        #region 内部参数
        private PreparedPlayerSession _owner;
        private Rigidbody2D _body;
        internal Color InitialBodyColor {get;private set;}=Color.white;
        public bool IsPrepared => _owner != null;
        public bool IsRunning => _owner != null && _owner.Started && !_owner.Disposed;
        public Rigidbody2D Body => _body != null ? _body : _body = GetComponent<Rigidbody2D>();
        public PlayerContext Context => _owner?.Runtime.Context;
        public string CurrentState => _owner?.Runtime.Fsm.CurrentLeafId;
        public Ember.Core.Fsm.EmberFsmGraph ObservationGraph=>_owner?.Runtime.SourceGraph;
        public Ember.Core.Fsm.EmberFsmObservation CaptureFsmObservation()=>_owner==null?null:
            Ember.Core.Fsm.EmberFsmObservation.Capture(_owner.Runtime.Fsm,_owner.Runtime.Origin);
        public event Action Invalidated;
        #endregion

        // --------------------------------------------------------
        #region 生命周期
        private void Awake()
        {var sprite=transform.Find("Body")?.GetComponent<SpriteRenderer>();if(sprite!=null)InitialBodyColor=sprite.color;Freeze();}
        private void OnDisable() { Freeze(); Invalidated?.Invoke(); }
        private void OnDestroy() => Invalidated?.Invoke();
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal void Reserve(PreparedPlayerSession owner)
        {
            if (_owner != null) throw new InvalidOperationException("俯视角色已由另一候选或会话拥有。");
            if (!isActiveAndEnabled || Body.gravityScale != 0 || Body.bodyType != RigidbodyType2D.Kinematic)
                throw new InvalidOperationException("俯视角色需要启用的零重力 Kinematic Rigidbody2D。");
            _owner = owner;
        }

        internal void Begin(PreparedPlayerSession owner)
        {
            if (!ReferenceEquals(_owner, owner) || owner.Disposed || owner.Started)
                throw new InvalidOperationException("角色候选已失效或重复启动。");
            Body.simulated = true;
        }

        internal void Release(PreparedPlayerSession owner)
        {
            if (!ReferenceEquals(_owner, owner)) return;
            Freeze(); _owner = null;
        }

        internal void Freeze()
        {
            if (Body == null) return;
            Body.linearVelocity = Vector2.zero;
            Body.angularVelocity = 0;
            Body.simulated = false;
        }

        internal void SetPaused(bool paused)
        { _owner?.Runtime.SetPaused(paused); Freeze(); if(!paused&&IsRunning)Body.simulated=true; }

        public void SetAimCamera(Camera camera)
        { if(Context==null)throw new InvalidOperationException("镜头只能绑定已经准备的玩家。"); Context.AimCamera=camera; }
        #endregion
    }
}

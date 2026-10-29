using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>扩展动作在自身 FixedUpdate 中提交意图；物理仍由 PlayerModule 唯一驱动，Exit/打断释放自己的租约。</summary>
    public sealed class PlayerActionControl:IDisposable
    {
        #region 内部参数
        private PlayerContext _context;
        public object Owner {get;}
        public bool LocksFacing {get;}
        public bool IsValid=>_context!=null;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerActionControl(PlayerContext context,object owner,bool locksFacing){_context=context;Owner=owner;LocksFacing=locksFacing;}
        public void SetVelocity(Vector2 velocity)
        {
            if(_context==null)throw new ObjectDisposedException(nameof(PlayerActionControl));
            if(!float.IsFinite(velocity.x)||!float.IsFinite(velocity.y))throw new ArgumentException("动作平面速度必须有限。");
            _context.Velocity=velocity;
        }
        public void Dispose(){var context=_context;_context=null;context?.ReleaseActionControl(this);}
        #endregion
    }
}

using System;

namespace Game.Player
{
    public enum PlayerActionIntent { Attack, Dash, Interact }
    /// <summary>一次有所有权的请求；到下一物理步被考虑，不承诺通过动作预检。Dispose 只取消尚未被考虑的自己。</summary>
    public sealed class PlayerActionRequest:IDisposable
    {
        #region 内部参数
        private PlayerContext _context;
        public object Owner {get;}
        public PlayerActionIntent Intent {get;}
        public bool IsPending=>_context!=null;
        public bool WasConsidered {get;private set;}
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerActionRequest(PlayerContext context,object owner,PlayerActionIntent intent){_context=context;Owner=owner;Intent=intent;}
        internal void End(bool considered){_context=null;WasConsidered=considered;}
        public void Dispose(){var context=_context;_context=null;context?.CancelActionRequest(this);}
        #endregion
    }
}

using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>稳定动作 ID 到方向序列。帧按方向连续排列：右起逆时针，方向数只影响表现。</summary>
    [Serializable]
    public sealed class PlayerAnimationMapping
    {
        #region 编辑器面板参数
        [SerializeField] private string _actionId;
        [SerializeField] private int _directions=1;
        [SerializeField] private Sprite[] _frames=Array.Empty<Sprite>();
        [SerializeField,Min(.01f)] private float _fps=8;
        [SerializeField] private bool _loop=true;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string ActionId=>_actionId;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public PlayerAnimationMapping(string actionId,Sprite[] frames,int directions=1,float fps=8,bool loop=true)
        {_actionId=actionId;_frames=frames==null?Array.Empty<Sprite>():(Sprite[])frames.Clone();_directions=directions;_fps=fps;_loop=loop;}
        internal PlayerAnimationMapping Capture()
        {
            if(string.IsNullOrWhiteSpace(_actionId)||(_directions!=1&&_directions!=4&&_directions!=8)||_frames==null||
                _frames.Length==0||_frames.Length%_directions!=0||!float.IsFinite(_fps)||_fps<=0)
                throw new InvalidOperationException("动作方向映射无效："+_actionId);
            foreach(var frame in _frames)if(frame==null)throw new InvalidOperationException("动作映射缺少 Sprite："+_actionId);
            return new PlayerAnimationMapping(_actionId,_frames,_directions,_fps,_loop);
        }
        internal Sprite Evaluate(Vector2 facing,float seconds)
        {
            var count=_frames.Length/_directions;var frame=Mathf.FloorToInt(seconds*_fps);
            frame=_loop?frame%count:Mathf.Min(frame,count-1);
            var angle=Mathf.Repeat(Mathf.Atan2(facing.y,facing.x)*Mathf.Rad2Deg,360);
            var direction=Mathf.RoundToInt(angle*_directions/360)%_directions;
            return _frames[direction*count+frame];
        }
        #endregion
    }
}

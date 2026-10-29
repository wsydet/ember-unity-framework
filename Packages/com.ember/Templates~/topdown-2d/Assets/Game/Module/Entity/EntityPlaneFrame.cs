using Ember.Basic;
using UnityEngine;

namespace Game.Entity
{
    /// <summary>运行查询、击退和编辑预览共享的攻击本地→世界变换。局部 X 为前，Y 为左。</summary>
    public static class EntityPlaneFrame
    {
        #region 外部方法
        [NoGC]
        public static Vector2 Transform(Vector2 local,Vector2 forward)
        {forward=forward.sqrMagnitude>.000001f?forward.normalized:Vector2.right;return forward*local.x+new Vector2(-forward.y,forward.x)*local.y;}
        [NoGC]
        public static float Angle(Vector2 forward)=>Mathf.Atan2(forward.y,forward.x)*Mathf.Rad2Deg;
        #endregion
    }
}

using Ember.Basic;
using UnityEngine;

namespace Game.Entity
{
    /// <summary>实体身份与根节点。不持有会话、输入、状态机或可选战斗能力。</summary>
    [DisallowMultipleComponent]
    public sealed class Entity2D : MonoBehaviour
    {
        #region 外部方法
        public Transform Root => transform;

        [NoGC]
        public static Entity2D Resolve(Component component)
            => component == null ? null : component.GetComponentInParent<Entity2D>();

        public bool Owns(Component component) => Resolve(component) == this;
        #endregion
    }
}

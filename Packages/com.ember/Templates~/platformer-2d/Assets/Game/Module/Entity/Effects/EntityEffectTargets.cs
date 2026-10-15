using System;
using UnityEngine;

namespace Game.Entity
{
    /// <summary>可选显式效果目标。身体碰撞与角色外观分别登记，刀光/提示不会自动加入。</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Entity2D))]
    public sealed class EntityEffectTargets : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private Collider2D[] _body = Array.Empty<Collider2D>();
        [SerializeField] private SpriteRenderer[] _appearance = Array.Empty<SpriteRenderer>();
        #endregion
        #region 外部方法
        public EntityEffectBinding Bind(EntityEffectContainer effects)
        {
            var entity = GetComponent<Entity2D>();
            foreach (var collider in _body)
                if (collider == null || !entity.Owns(collider) || collider.isTrigger)
                    throw new InvalidOperationException("身体效果目标必须是本实体的非 Trigger Collider。");
            foreach (var sprite in _appearance)
                if (sprite == null || !entity.Owns(sprite)) throw new InvalidOperationException("外观效果目标必须属于本实体。");
            return new EntityEffectBinding(effects, _body, _appearance);
        }
        #endregion
    }
}

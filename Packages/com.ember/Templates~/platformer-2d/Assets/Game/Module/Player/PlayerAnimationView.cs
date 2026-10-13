using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>仅持有 Prefab 组件接线；不保存方案或驱动状态机。</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Animator))]
    public sealed class PlayerAnimationView : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private SpriteRenderer _sprite;
        #endregion
        #region 外部方法
        public Transform VisualRoot => _visualRoot;
        public SpriteRenderer Sprite => _sprite;
        internal Animator Validate(PlayerController owner)
        {
            var animator = GetComponent<Animator>();
            if (_visualRoot == null || _visualRoot.parent != owner.transform || transform.parent != _visualRoot ||
                _sprite == null || !_sprite.transform.IsChildOf(transform) || _sprite.transform == transform ||
                _visualRoot.GetComponentsInChildren<Collider2D>(true).Length != 0 || animator.applyRootMotion)
                throw new InvalidOperationException("玩家动画需要独立 VisualRoot/Animation/Sprite 接线且禁用 Root Motion。");
            return animator;
        }
        #endregion
    }
}

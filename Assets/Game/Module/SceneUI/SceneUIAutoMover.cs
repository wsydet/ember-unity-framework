// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using UnityEngine;

namespace Game.Module
{
    /// <summary>沿指定本地轴往复移动，用于验证动态物体的 SceneUI 跟随效果。</summary>
    [DisallowMultipleComponent]
    public sealed class SceneUIAutoMover : MonoBehaviour
    {
        [SerializeField, Tooltip("相对于启用位置的本地移动方向。")]
        private Vector3 _localDirection = Vector3.right;

        [SerializeField, Min(0f), Tooltip("从初始位置到单侧端点的距离。")]
        private float _distance = 3f;

        [SerializeField, Min(0f), Tooltip("每秒完成的往复循环次数。")]
        private float _cyclesPerSecond = 0.15f;

        private Vector3 _originLocalPosition;
        private float _elapsed;

        private void OnEnable()
        {
            _originLocalPosition = transform.localPosition;
            _elapsed = 0f;
        }

        private void Update()
        {
            if (_distance <= 0f
                || _cyclesPerSecond <= 0f
                || _localDirection.sqrMagnitude < 0.0001f)
                return;

            _elapsed += Time.deltaTime;
            float phase = _elapsed * _cyclesPerSecond * Mathf.PI * 2f;
            float offset = Mathf.Sin(phase) * _distance;
            transform.localPosition = _originLocalPosition
                                      + _localDirection.normalized * offset;
        }

        private void OnValidate()
        {
            _distance = Mathf.Max(0f, _distance);
            _cyclesPerSecond = Mathf.Max(0f, _cyclesPerSecond);
            if (_localDirection.sqrMagnitude < 0.0001f)
                _localDirection = Vector3.right;
        }
    }
}

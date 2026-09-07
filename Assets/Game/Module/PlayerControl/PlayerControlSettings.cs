using Ember.Basic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Module
{
    /// <summary>PlayerControlModule 的运行时可调参数。</summary>
    [CreateAssetMenu(
        fileName = "PlayerControlSettings",
        menuName = "Game/Player Control Settings")]
    public sealed class PlayerControlSettings : EmberBaseSO
    {
        private const string INSPECTOR_GROUP = "Player Control Settings";

        #region 编辑器面板参数

        [FoldoutGroup("$INSPECTOR_GROUP", Expanded = true)]
        [BoxGroup("$INSPECTOR_GROUP/移动", ShowLabel = false)]
        [Title("移动", "纯 C# 玩家控制模块会在每帧直接读取该值。")]
        [MinValue(0f), LabelText("WASD 移动速度")]
        [SerializeField] private float _keyboardMoveSpeed = 8f;

        [FoldoutGroup("$INSPECTOR_GROUP", Expanded = true)]
        [BoxGroup("$INSPECTOR_GROUP/缩放", ShowLabel = false)]
        [Title("滚轮缩放", "滚轮向上减小正交尺寸，让画面放大。")]
        [MinValue(0f), LabelText("每格正交尺寸变化")]
        [SerializeField] private float _orthographicSizePerScroll = 1f;

        [FoldoutGroup("$INSPECTOR_GROUP", Expanded = true)]
        [BoxGroup("$INSPECTOR_GROUP/缩放")]
        [MinValue(0.01f), LabelText("最小正交尺寸")]
        [SerializeField] private float _minOrthographicSize = 5f;

        [FoldoutGroup("$INSPECTOR_GROUP", Expanded = true)]
        [BoxGroup("$INSPECTOR_GROUP/缩放")]
        [MinValue(0.01f), LabelText("最大正交尺寸")]
        [SerializeField] private float _maxOrthographicSize = 20f;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void OnValidate()
        {
            _keyboardMoveSpeed = Mathf.Max(0f, _keyboardMoveSpeed);
            _orthographicSizePerScroll = Mathf.Max(0f, _orthographicSizePerScroll);
            _minOrthographicSize = Mathf.Max(0.01f, _minOrthographicSize);
            _maxOrthographicSize = Mathf.Max(_minOrthographicSize, _maxOrthographicSize);
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public float KeyboardMoveSpeed => Mathf.Max(0f, _keyboardMoveSpeed);
        public float OrthographicSizePerScroll => Mathf.Max(0f, _orthographicSizePerScroll);
        public float MinOrthographicSize => Mathf.Max(0.01f, _minOrthographicSize);
        public float MaxOrthographicSize => Mathf.Max(MinOrthographicSize, _maxOrthographicSize);

        #endregion
    }
}

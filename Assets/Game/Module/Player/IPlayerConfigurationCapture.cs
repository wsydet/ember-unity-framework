using UnityEngine;

namespace Game.Player
{
    /// <summary>在私有 SO 副本上冻结项目配置。所有可变 SO 依赖必须经 capture 递归捕获，不能保存源引用。</summary>
    public interface IPlayerConfigurationCapture
    {
        void CapturePlayerConfiguration(PlayerConfigurationCapture capture);
    }

    /// <summary>准备期间有效的捕获接合点；由方案拥有全部副本，恢复候选沿用同一冻结方案。</summary>
    public sealed class PlayerConfigurationCapture
    {
        #region 内部参数
        private readonly System.Func<ScriptableObject, ScriptableObject> _copy;
        private bool _active = true;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerConfigurationCapture(System.Func<ScriptableObject, ScriptableObject> copy) => _copy = copy;
        public T Capture<T>(T source) where T : ScriptableObject
        {
            if (!_active) throw new System.InvalidOperationException("配置捕获仅在准备回调内有效。");
            if (source is PlayerConfig || source is PlayerScheme || source is Ember.Core.Fsm.EmberFsmGraph)
                throw new System.InvalidOperationException("节点配置不能反向引用完整玩家装配或图。");
            return (T)_copy(source);
        }
        internal void Close() => _active = false;
        #endregion
    }
}

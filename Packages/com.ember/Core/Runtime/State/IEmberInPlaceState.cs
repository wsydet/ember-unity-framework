using Cysharp.Threading.Tasks;

namespace Ember.Core
{
    /// <summary>当前状态内的资源交接协议。不退出/重进状态，不改变覆盖栈；须显式声明到自身的边。</summary>
    public interface IEmberInPlaceState
    {
        /// <summary>准备隔离候选；失败或取消前不得破坏当前运行资源。</summary>
        UniTask PrepareInPlaceAsync(SceneTransitionContext request);
        /// <summary>准备成功后的同步提交。实现负责交接与旧资源清理；异常不宣称自动回滚。</summary>
        void CommitInPlace(SceneTransitionContext request);
        /// <summary>未开始提交时清理部分/完整候选，不能释放旧运行资源。</summary>
        UniTask AbortInPlaceAsync(SceneTransitionContext request);
    }
}

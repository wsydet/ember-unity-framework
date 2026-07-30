namespace Ember.Core
{
    /// <summary>
    /// 实现此接口的类将自动接收 Update 帧回调。
    ///
    /// 配合 <see cref="EmberUpdateManager"/> 使用——
    /// 不需要继承 MonoBehaviour，只需实现此接口 + 继承单例基类，
    /// UpdateManager 通过反射自动发现并每帧调用 Update()。
    ///
    /// 优势：
    /// - 无需挂载 GameObject，零 Inspector 配置
    /// - 统一由一处驱动，可控制执行顺序
    /// - 比几十个 MonoBehaviour.Update 性能更好（减少 C++ 跨语言调用）
    ///
    /// 参考 burner 的 <c>IGameUpdate</c>。
    /// </summary>
    public interface IEmberUpdate
    {
        void Update();
    }

    /// <summary>
    /// 实现此接口的类将自动接收 LateUpdate 帧回调。
    /// </summary>
    public interface IEmberLateUpdate
    {
        void LateUpdate();
    }

    /// <summary>
    /// 实现此接口的类将自动接收 FixedUpdate 帧回调。
    /// </summary>
    public interface IEmberFixedUpdate
    {
        void FixedUpdate();
    }
}

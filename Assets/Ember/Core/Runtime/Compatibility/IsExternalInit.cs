// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// C# 9 init-only setter 的 polyfill（编译兼容填充）。
    ///
    /// <b>背景</b>
    /// C# 9 引入 <c>init</c> 访问器，编译器遇到它时会引用
    /// <c>System.Runtime.CompilerServices.IsExternalInit</c> 类型。
    /// Unity 当前自带的 Roslyn 版本不包含此类型，编译会报 CS0518 错误。
    ///
    /// <b>作用</b>
    /// 这是一个<b>纯标记类</b>，没有任何逻辑。编译器只需要它存在于正确
    /// 的命名空间即可让 <c>init</c> 关键字正常编译。
    ///
    /// <b>项目的实际依赖</b>
    /// <see cref="TransitionDescriptor"/> 在 TargetState / Condition / Guard
    /// 属性上使用了 <c>init</c> 访问器。如果删除本文件，所有带 <c>init</c>
    /// 的类型都会编译失败。
    ///
    /// <b>移除时机</b>
    /// Unity 升级到自带此类型的 Roslyn 版本后，删除本文件即可。
    ///
    /// 参考：https://github.com/dotnet/runtime/issues/48416
    /// </summary>
    internal static class IsExternalInit { }
}

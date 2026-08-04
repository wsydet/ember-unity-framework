// Modelled after: https://github.com/dotnet/corert/blob/master/src/Runtime.Base/src/System/Runtime/CompilerServices/EagerStaticClassConstructionAttribute.cs
//
// When applied to a type this custom attribute will cause any static class constructor to be run eagerly
// at module load time rather than deferred till just before the class is used.

using System;

namespace Unity.IL2CPP.CompilerServices
{
    /// <summary>
    /// 应用于类型时，使静态构造函数在模块加载时立即执行，而非延迟到首次使用前。
    /// IL2CPP polyfill —— Unity 部分版本缺少此 Attribute。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public class Il2CppEagerStaticClassConstructionAttribute : Attribute
    {
    }
}

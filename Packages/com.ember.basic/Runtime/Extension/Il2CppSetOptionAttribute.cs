using System;

namespace Unity.IL2CPP.CompilerServices
{
    /// <summary>
    /// IL2CPP 代码生成选项。
    /// </summary>
    public enum Option
    {
        /// <summary>空引用检查。禁用后不会抛出 NullReferenceException。</summary>
        NullChecks = 1,
        /// <summary>数组边界检查。禁用后可读写越界内存，极其危险。</summary>
        ArrayBoundsChecks = 2,
        /// <summary>除零检查。默认关闭。</summary>
        DivideByZeroChecks = 3,
    }

    /// <summary>
    /// 应用于程序集/类型/方法，覆盖 IL2CPP 的全局代码生成选项。
    /// IL2CPP polyfill —— Unity 部分版本缺少此 Attribute。
    ///
    /// 示例：
    /// <code>
    /// [Il2CppSetOption(Option.NullChecks, false)]
    /// public static string MethodWithNullChecksDisabled()
    /// {
    ///     var tmp = new Object();
    ///     return tmp.ToString();
    /// }
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Struct | AttributeTargets.Class |
                    AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public class Il2CppSetOptionAttribute : Attribute
    {
        public Option Option { get; }
        public object Value { get; }

        public Il2CppSetOptionAttribute(Option option, object value)
        {
            Option = option;
            Value = value;
        }
    }
}

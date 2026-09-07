using System;

namespace Ember.Core
{
    /// <summary>
    /// 业务模块的类型级元数据。
    /// 收集器会先读取此特性，仅在模块启用时才访问其静态 Instance。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class EmberModuleAttribute : Attribute
    {
        /// <summary>模块所属生命周期阶段。</summary>
        public int Phase { get; }

        /// <summary>
        /// 是否允许收集器在启动扫描时创建并登记该模块。
        /// 这是类型级装配开关，不是运行时动态启停 API。
        /// </summary>
        public bool Enabled { get; set; } = true;

        public EmberModuleAttribute(int phase)
        {
            Phase = phase;
        }
    }
}

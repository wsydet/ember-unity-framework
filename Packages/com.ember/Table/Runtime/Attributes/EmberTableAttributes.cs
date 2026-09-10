// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;

namespace Ember.Table
{
    /// <summary>把不可变 Row 类型绑定到稳定、大小写敏感的 Table ID。</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class EmberTableAttribute : Attribute
    {
        public string TableId { get; }

        public EmberTableAttribute(string tableId)
        {
            TableId = tableId;
        }
    }

    /// <summary>标记 Row 中唯一的字符串主键成员。</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    public sealed class EmberTableKeyAttribute : Attribute
    {
    }

    /// <summary>覆盖字段或构造参数默认使用的列名。</summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter,
        Inherited = false)]
    public sealed class EmberTableColumnAttribute : Attribute
    {
        public string Name { get; }

        public EmberTableColumnAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>声明字符串列引用的目标 Table ID；引用完整性只在 Editor 校验。</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    public sealed class EmberTableReferenceAttribute : Attribute
    {
        public string TargetTableId { get; }

        public EmberTableReferenceAttribute(string targetTableId)
        {
            TargetTableId = targetTableId;
        }
    }

    /// <summary>明确指定生成 Binding 用来创建不可变 Row 的构造函数。</summary>
    [AttributeUsage(AttributeTargets.Constructor, Inherited = false)]
    public sealed class EmberTableConstructorAttribute : Attribute
    {
    }
}

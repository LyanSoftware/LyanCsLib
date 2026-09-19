using System;

namespace Lytec.Common.Serialization;

/// <summary>控制未显式标记成员的默认包含方式。</summary>
public enum BinaryMemberInclusion
{
    /// <summary>默认包含具有实际存储的成员，使用 <see cref="BinaryIgnoreAttribute"/> 排除。</summary>
    OptOut,

    /// <summary>默认排除成员，仅包含显式标记的成员。</summary>
    OptIn,
}

/// <summary>为固定长度二进制对象配置成员包含规则。</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
public sealed class BinaryObjectAttribute : Attribute
{
    public BinaryMemberInclusion MemberInclusion { get; }

    public BinaryObjectAttribute(BinaryMemberInclusion memberInclusion = BinaryMemberInclusion.OptOut)
        => MemberInclusion = memberInclusion;
}

/// <summary>显式包含一个具有实际存储的字段或自动实现属性。</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class BinaryMemberAttribute : Attribute
{
}

/// <summary>显式排除一个具有实际存储的字段或自动实现属性。</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class BinaryIgnoreAttribute : Attribute
{
}

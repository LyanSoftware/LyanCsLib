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

/// <summary>固定长度二进制对象的布局。</summary>
public enum BinaryLayoutKind
{
    /// <summary>成员按照源码顺序连续排列。</summary>
    Sequential,

    /// <summary>成员使用显式二进制偏移。</summary>
    Explicit,
}

/// <summary>固定宽度标量在二进制格式中的数据类型。</summary>
public enum BinaryDataType
{
    /// <summary>根据 CLR 类型自动推断。仅用于固定数组元素。</summary>
    Automatic,

    Int8,
    UInt8,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Float32,
    Float64,
    Boolean8,
    Boolean16,
    Boolean32,
}

/// <summary>启用固定长度二进制代码生成并配置二进制布局。</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class FixedBinarySerializableAttribute : Attribute
{
    public BinaryLayoutKind LayoutKind { get; }

    public BinaryMemberInclusion MemberInclusion { get; set; } = BinaryMemberInclusion.OptOut;

    /// <summary>显式二进制数据总大小；零表示使用成员计算出的自然大小。</summary>
    public int Size { get; set; }

    public FixedBinarySerializableAttribute(BinaryLayoutKind layoutKind)
        => LayoutKind = layoutKind;
}

/// <summary>声明一个由用户实现、可作为内联值使用的固定长度二进制契约。</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class FixedBinaryContractAttribute : Attribute
{
    public int Size { get; }

    public Type CodecProviderType { get; }

    public FixedBinaryContractAttribute(int size, Type codecProviderType)
        => (Size, CodecProviderType) = (size, codecProviderType);
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

/// <summary>指定 Explicit 二进制布局中的成员偏移。</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class BinaryFieldOffsetAttribute : Attribute
{
    public int Offset { get; }

    public BinaryFieldOffsetAttribute(int offset) => Offset = offset;
}

/// <summary>声明一个固定元素数量的内联数组。</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class BinaryFixedArrayAttribute : Attribute
{
    public int Length { get; }

    public BinaryDataType ElementType { get; set; } = BinaryDataType.Automatic;

    public BinaryFixedArrayAttribute(int length) => Length = length;
}

/// <summary>指定标量成员在二进制格式中的固定宽度数据类型。</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class BinaryDataTypeAttribute : Attribute
{
    public BinaryDataType DataType { get; }

    public BinaryDataTypeAttribute(BinaryDataType dataType) => DataType = dataType;
}

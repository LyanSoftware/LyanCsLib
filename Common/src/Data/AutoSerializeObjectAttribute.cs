namespace Lytec.Common.Data;

/// <summary>
/// 生成自动处理字节序的序列化与反序列化方法
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class AutoSerializeObjectAttribute : Attribute
{
    public const string DefaultSerializeMethodName = "Serialize";
    public string? SerializeMethodName { get; set; } = DefaultSerializeMethodName;
}

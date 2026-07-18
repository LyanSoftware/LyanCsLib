[AttributeUsage(
    AttributeTargets.Method | AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = false)]
public class GenerateHashAlgorithmExtensionsAttribute
    : Attribute
{
    public delegate T ConvertHashResult<T>(byte[] bytes);

    public string? ConvertResultFuncName { get; set; }
    public string? ExtensionMethodName { get; set; }
    public GenerateHashAlgorithmExtensionsAttribute() { }
    public GenerateHashAlgorithmExtensionsAttribute(string convertResultFuncName) => ConvertResultFuncName = convertResultFuncName;
}

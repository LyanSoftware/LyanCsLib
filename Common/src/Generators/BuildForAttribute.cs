namespace Lytec.Common.Generators;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public class BuilderForAttribute : Attribute
{
    public Type Type { get; }
    public bool IncludeInternals { get; } = false;
    public bool IncludeObsolete { get; } = false;
    public BuilderForAttribute(Type type) => Type = type;
}

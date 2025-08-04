namespace System
{
    public interface ICloneable<out T> : ICloneable
    {
        new T Clone();
#if NET || NETSTANDARD2_1_OR_GREATER
        object ICloneable.Clone() => Clone()!;
#endif
    }
}

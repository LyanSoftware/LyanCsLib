namespace System
{
    public interface ICloneable<out T> : ICloneable
    {
        new T Clone();
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        object ICloneable.Clone() => Clone();
#endif
    }
}

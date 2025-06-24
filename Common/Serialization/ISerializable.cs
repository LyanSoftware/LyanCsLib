namespace Lytec.Common.Serialization
{
    public interface ISequenceDeserializer<out T> : IDeserializer<T>
    {
        T Deserialize(byte data, out bool ok);
        T Deserialize(IEnumerable<byte> data, out int DeserializedLength, out bool ok);
    }

    public interface ISerializable
    {
        byte[] Serialize();
    }

    public interface ISerializable<out TImpl> : ISerializable, IFactory<IDeserializer<TImpl>>
        where TImpl : ISerializable<TImpl>, new()
    { }

}

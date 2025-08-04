namespace Lytec.Common.Serialization
{
    public interface ISerializable
    {
        byte[] Serialize();
    }

    public interface ISerializable<out TImpl> : ISerializable, IFactory<IDeserializer<TImpl>>
        where TImpl : ISerializable<TImpl>, new()
    { }

}

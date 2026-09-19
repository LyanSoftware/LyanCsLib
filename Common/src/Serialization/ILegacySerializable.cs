namespace Lytec.Common.Serialization
{
    public interface ILegacySerializable
    {
        byte[] Serialize();
    }

    public interface ILegacySerializable<out TImpl> : ILegacySerializable, IFactory<ILegacyDeserializer<TImpl>>
        where TImpl : ILegacySerializable<TImpl>, new()
    { }

}

using Lytec.Common.Serialization;

namespace Lytec.Common.Data;

public static class AutoSerializeUtils
{
    public static class Deserializers<T> where T : IAutoSerializeObject<T>, new()
    {
        public static IDeserializer<T> Deserializer { get; set; } = FactoryCache<T, IDeserializer<T>>.Create();
    }

    public static IDeserializer<T> GetDeserializer<T>() where T : IAutoSerializeObject<T>, new()
    => Deserializers<T>.Deserializer;
}

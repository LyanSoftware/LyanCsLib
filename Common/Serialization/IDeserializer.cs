using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.Common.Serialization
{
    public interface IDeserializer<out T>
    {
        T Deserialize(IEnumerable<byte> b, out bool ok);
    }

    public static class DeserializerUtils
    {
        public static T Deserialize<T>(this IDeserializer<T> d, IEnumerable<byte> data)
        => d.Deserialize(data, out _);
        public static T Deserialize<T>(this ISequenceDeserializer<T> d, byte data)
        => d.Deserialize(data, out _);
        public static T Deserialize<T>(this ISequenceDeserializer<T> d, out int DeserializedLength, IEnumerable<byte> data)
        => d.Deserialize(data, out DeserializedLength, out _);
    }
}

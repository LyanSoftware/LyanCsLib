using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.Common.Serialization
{
    public interface IDeserializer<out T>
    {
        T? Deserialize(IEnumerable<byte> b);
    }

    public interface ISequenceDeserializer<out T> : IDeserializer<T>
    {
        T? Deserialize(byte data, out bool ok);
        T? Deserialize(IEnumerable<byte> data, out int DeserializedLength, out bool ok);
    }

}

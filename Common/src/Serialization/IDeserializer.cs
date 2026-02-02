using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.Common.Serialization
{
    public interface IDeserializer<out T>
    {
        T? Deserialize(IEnumerable<byte> data);
        T? Deserialize(ReadOnlySpan<byte> data);
    }

    public interface IVariableLengthDeserializer<out T> : IDeserializer<T>
    {
        T? Deserialize(IEnumerable<byte> data, out int DeserializedLength);
        T? Deserialize(ReadOnlySpan<byte> data, out int DeserializedLength);
    }

    public interface ISequenceDeserializer<out T> : IDeserializer<T>
    {
        T? Deserialize(byte data);
        void Reset();
    }

    public interface ISequenceVLDeserializer<out T> : ISequenceDeserializer<T>, IVariableLengthDeserializer<T> { }
}

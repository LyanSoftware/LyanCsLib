using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.Common.Serialization
{
    public interface ILegacyDeserializer<out T>
    {
        T? Deserialize(IEnumerable<byte> data);
        T? Deserialize(ReadOnlySpan<byte> data);
    }

    public interface ILegacyVariableLengthDeserializer<out T> : ILegacyDeserializer<T>
    {
        T? Deserialize(IEnumerable<byte> data, out int DeserializedLength);
        T? Deserialize(ReadOnlySpan<byte> data, out int DeserializedLength);
    }

    public interface ILegacySequenceDeserializer<out T> : ILegacyDeserializer<T>
    {
        T? Deserialize(byte data);
        void Reset();
    }

    public interface ILegacySequenceVariableLengthDeserializer<out T> : ILegacySequenceDeserializer<T>, ILegacyVariableLengthDeserializer<T> { }
}

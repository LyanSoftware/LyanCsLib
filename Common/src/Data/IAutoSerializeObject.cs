using System;
using System.Collections.Generic;
using System.Text;
using Lytec.Common.Serialization;

namespace Lytec.Common.Data;

public interface IAutoSerializeObject
{
    byte[] Serialize();
    bool Serialize(Span<byte> buffer);
}

public interface IAutoSerializeObjectDeserializer<T, TDeserializer> where TDeserializer : IAutoSerializeObjectDeserializer<T, TDeserializer>, new()
{
    bool TryDeserialize(byte[] data, int offset, out T obj, int deserializedLength);
    bool TryDeserialize(ReadOnlySpan<byte> data, out T obj, int deserializedLength);
}


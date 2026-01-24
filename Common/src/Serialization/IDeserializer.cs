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
        T? Deserialize(IEnumerable<byte> data, out int DeserializedLength, out bool ok)
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        {
            var len = 0;
            T? p = default;
            ok = false;
            foreach (var b in data)
            {
                len++;
                p = Deserialize(b, out ok);
                if (ok)
                    break;
            }
            DeserializedLength = len;
            return p;
        }
#else
        ;
#endif

        void Reset();
    }

}

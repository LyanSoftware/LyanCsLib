using System;
using System.Collections.Generic;
using System.Text;
using Lytec.Common.Serialization;

namespace Lytec.Common.Data;

public interface IAutoSerializeObject
{
    byte[] Serialize();
}

public interface IAutoSerializeObject<T> : IAutoSerializeObject, IFactory<IVariableLengthDeserializer<T>> where T : new() { }


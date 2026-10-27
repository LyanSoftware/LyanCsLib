using System;
using System.Collections.Generic;
using System.Text;
using Lytec.Common.Serialization;

namespace Lytec.Common.Communication
{
    public interface ILegacyPackage
    {
        byte[] Serialize();
    }

    public interface ILegacyPackage<out T> : ILegacyPackage where T : ILegacyPackage<T>
    {
        ILegacyDeserializer<T> CreateDeserializer();
    }

    public interface ILegacyPackage<in TPack, TAnswer> : ILegacyPackage where TPack : ILegacyPackage<TPack, TAnswer>
    {
        bool IsMyAnswer(TAnswer answer);

        ILegacyDeserializer<TAnswer> CreateDeserializer();
    }
}

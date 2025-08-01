using System;
using System.Collections.Generic;
using System.Text;
using Lytec.Common.Serialization;

namespace Lytec.Common.Communication
{
    public interface IPackage
    {
        byte[] Serialize();
    }

    public interface IPackage<out T> : IPackage where T : IPackage<T>
    {
        IDeserializer<T> CreateDeserializer();
    }

    public interface IPackage<in TPack, TAnswer> : IPackage where TPack : IPackage<TPack, TAnswer>
    {
        bool IsMyAnswer(TAnswer answer);

        IDeserializer<TAnswer> CreateDeserializer();
    }
}

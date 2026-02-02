using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.Common
{
    public interface IFactory<out T>
    {
        T Create();
#if NET6_0_OR_GREATER
        static abstract T CreateInstance();
#endif
    }

    public static class FactoryCache<TFactory, TResult> where TFactory : IFactory<TResult>, new()
    {
        public static TFactory Factory { get; set; } = new();
        public static TResult Create() => Factory.Create();
    }

}

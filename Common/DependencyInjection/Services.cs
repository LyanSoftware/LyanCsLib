using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Lytec.Common.DependencyInjection
{
    public abstract class Services<TImpl> : IServiceProvider where TImpl : Services<TImpl>, new()
    {
        private readonly Lazy<ServiceProvider> _ServiceProvider;
        public ServiceProvider ServiceProvider => _ServiceProvider.Value;

        public object? GetService(Type serviceType) => ServiceProvider.GetService(serviceType);

        public static Func<TImpl> CreateInstance { get; set; } = () => new();

        private static readonly Lazy<TImpl> _Instance = new(() => CreateInstance());
        public static TImpl Instance => _Instance.Value;

        protected Services()
        {
            _ServiceProvider = new(() =>
            {
                var collection = new ServiceCollection();
                return OnCreating(collection)
                    .BuildServiceProvider();
            });
        }

        protected virtual IServiceCollection OnCreating(IServiceCollection collection) => collection;
    }

}

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Lytec.Common
{
    public static class Reflection
    {
        private static class DefaultProvider<T>
        {
            public static T? Value => default;
        }

        public static object? GetDefaultValue(this Type type)
        {
            if (type.IsValueType)
            {
                var defaultType = typeof(DefaultProvider<>).MakeGenericType(type);
                return defaultType.InvokeMember("Value", BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty, binder: null, target: null, args: null, culture: null);
            }

            return null;
        }
    }
}

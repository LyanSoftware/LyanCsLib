using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Lytec.Common;
using Lytec.Common.Data;

namespace Lytec.Common.Algorithm;

public static partial class HashAlgorithmExtensions
{
    static Dictionary<Type, Func<HashAlgorithm?>> HashAlgorithmFactoryCache { get; } = [];
    public static HashAlgorithm? CreateHashAlgorithm([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
        if (!HashAlgorithmFactoryCache.TryGetValue(type, out var factory))
        {
            factory = null;
            var ctor = type.GetConstructor(System.Reflection.BindingFlags.Public, []);
            if (ctor != null)
                factory = () => (HashAlgorithm)ctor.Invoke(null);
            if (factory == null)
            {
                var method = type.GetMethod("Create", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, []);
                if (method != null)
                    factory = () => (HashAlgorithm?)method.Invoke(null, null);
            }
            if (factory == null)
                factory = () => null;
            HashAlgorithmFactoryCache[type] = factory;
        }
        return factory();
    }
    public static byte[] GetHash<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TAlgorithm>(this byte[] bytes, int offset, int count) where TAlgorithm : HashAlgorithm
    => CreateHashAlgorithm(typeof(TAlgorithm))?.ComputeHash(bytes, offset, count) ?? throw new NotSupportedException();
    public static byte[] GetHash<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TAlgorithm>(this byte[] bytes) where TAlgorithm : HashAlgorithm
    => GetHash<TAlgorithm>(bytes, 0, bytes.Length);
    public static byte[] GetHash<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TAlgorithm>(this byte[] bytes, int len) where TAlgorithm : HashAlgorithm
    => GetHash<TAlgorithm>(bytes, 0, len);
    public static byte[] GetHash<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TAlgorithm>(this Stream stream) where TAlgorithm : HashAlgorithm
    => CreateHashAlgorithm(typeof(TAlgorithm))?.ComputeHash(stream) ?? throw new NotSupportedException();
    public static byte[] GetHash<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TAlgorithm>(this IEnumerable<byte> bytes) where TAlgorithm : HashAlgorithm
    => CreateHashAlgorithm(typeof(TAlgorithm))?.ComputeHash(bytes) ?? throw new NotSupportedException();
    
}

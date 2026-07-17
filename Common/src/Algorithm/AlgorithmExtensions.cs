using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Lytec.Common.Data;

namespace Lytec.Common.Algorithm
{
    public static class AlgorithmExtensions
    {
        static IDictionary<Type, Func<HashAlgorithm?>> HashAlgorithmFactoryCache { get; } = new Dictionary<Type, Func<HashAlgorithm?>>();
        public static HashAlgorithm? CreateHashAlgorithm(Type type)
        {
            if (!HashAlgorithmFactoryCache.TryGetValue(type, out var factory))
            {
                factory = null;
                var ctor = type.GetConstructor(System.Reflection.BindingFlags.Public, Array.Empty<Type>());
                if (ctor != null)
                    factory = () => (HashAlgorithm)ctor.Invoke(null);
                if (factory == null)
                {
                    var method = type.GetMethod("Create", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, Array.Empty<Type>());
                    if (method != null)
                        factory = () => (HashAlgorithm?)method.Invoke(null, null);
                }
                if (factory == null)
                    factory = () => null;
                HashAlgorithmFactoryCache[type] = factory;
            }
            return factory();
        }
        public static byte[] GetHash<TAlgorithm>(this byte[] bytes, int offset, int count) where TAlgorithm : HashAlgorithm
        => CreateHashAlgorithm(typeof(TAlgorithm))?.ComputeHash(bytes, offset, count) ?? throw new NotSupportedException();
        public static byte[] GetHash<TAlgorithm>(this byte[] bytes) where TAlgorithm : HashAlgorithm
        => GetHash<TAlgorithm>(bytes, 0, bytes.Length);
        public static byte[] GetHash<TAlgorithm>(this byte[] bytes, int len) where TAlgorithm : HashAlgorithm
        => GetHash<TAlgorithm>(bytes, 0, len);
        public static byte[] GetHash<TAlgorithm>(this Stream stream) where TAlgorithm : HashAlgorithm
        => CreateHashAlgorithm(typeof(TAlgorithm))?.ComputeHash(stream) ?? throw new NotSupportedException();
        public static byte[] GetHash<TAlgorithm>(this IEnumerable<byte> bytes) where TAlgorithm : HashAlgorithm
        => CreateHashAlgorithm(typeof(TAlgorithm))?.ComputeHash(bytes) ?? throw new NotSupportedException();

        public static MD5 GetMd5() => MD5.Create();

        public static byte[] GetMd5(this byte[] bytes)
        => GetMd5(bytes, 0, bytes.Length);
        public static byte[] GetMd5(this byte[] bytes, int count)
        => GetMd5(bytes, 0, count);
        public static byte[] GetMd5(this byte[] bytes, int offset, int count)
        => MD5.Create().ComputeHash(bytes, offset, count);
        public static byte[] GetMd5(this Stream stream)
        => MD5.Create().ComputeHash(stream);
        public static byte[] GetMd5(this IEnumerable<byte> bytes)
        => MD5.Create().ComputeHash(bytes);

        public static ushort GetCrc16_CCITT_XMODEM(this byte[] bytes, ushort init = 0)
        => GetCrc16_CCITT_XMODEM(bytes, 0, bytes.Length, init);
        public static ushort GetCrc16_CCITT_XMODEM(this byte[] bytes, int count, ushort init = 0)
        => GetCrc16_CCITT_XMODEM(bytes, 0, count, init);
        public static ushort GetCrc16_CCITT_XMODEM(this byte[] bytes, int offset, int count, ushort init = 0)
        => new CheckSum.CRC16.CCITT_XMODEM(init).ComputeHash(bytes, offset, count).ToStruct<ushort>();
        public static ushort GetCrc16_CCITT_XMODEM(this Stream stream, ushort init = 0)
        => new CheckSum.CRC16.CCITT_XMODEM(init).ComputeHash(stream).ToStruct<ushort>();
        public static ushort GetCrc16_CCITT_XMODEM(this IEnumerable<byte> bytes, ushort init = 0)
        => new CheckSum.CRC16.CCITT_XMODEM(init).ComputeHash(bytes).ToStruct<ushort>();

    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lytec.Common.Data
{
    public static partial class StructHelper
    {
        public static IDictionary<Type, int> SizeCaches { get; set; } = new Dictionary<Type, int>();

        public static class SizeCache<T>
        {
            public static readonly int Size = Marshal.SizeOf<T>();
            public static readonly Type Type = typeof(T);

            static SizeCache() => SizeCaches[Type] = Size;
        }

        public static int GetStructSize<T>(this T _)
        {
            return SizeCache<T>.Size;
        }

        public static int GetStructSize<T>()
        {
            return SizeCache<T>.Size;
        }

        public static int GetStructSize(this Type type)
        {
            if (!SizeCaches.TryGetValue(type, out var sz))
                SizeCaches[type] = sz = Marshal.SizeOf(type);
            return sz;
        }

        #region 结构体与byte[]互转

        /// <summary>
        /// 结构体转byte[]
        /// </summary>
        /// <param name="t"></param>
        /// <param name="defaultEndian">未指定目标字节序时的默认字节序</param>
        /// <returns></returns>
        [return: NotNull]
        public static byte[] ToBytes<T>(this T t, Endian? defaultEndian = null) => t!.ToBytes(t!.GetType(), defaultEndian);

        /// <summary>
        /// 结构体转byte[]
        /// </summary>
        /// <param name="t"></param>
        /// <param name="defaultEndian">未指定目标字节序时的默认字节序</param>
        /// <returns></returns>
        [return: NotNull]
        public static byte[] ToBytes(this object t, Type type, Endian? defaultEndian = null)
        {
            switch (t)
            {
                case IPAddress ip:
                    return ip.GetAddressBytes();
                case IEnumerable objs:
                    return (from object o in objs
                            from b in o.ToBytes(o.GetType())
                            select b).ToArray();
            }
            object to = t;
            if (type.IsEnum)
            {
                type = type.GetEnumUnderlyingType();
                to = Convert.ChangeType(t, type);
            }
            int size = type.GetStructSize();
            var bytes = new byte[size];

            IntPtr p = default;
            try
            {
                p = Marshal.AllocHGlobal(bytes.Length);
                Marshal.StructureToPtr(to, p, false);
                Marshal.Copy(p, bytes, 0, bytes.Length);
            }
            finally
            {
                if (p != default)
                    Marshal.FreeHGlobal(p);
            }
            return bytes.FixEndian(type, defaultEndian);
        }

        /// <summary>
        /// byte[]转结构体
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset">数据的起始位置</param>
        /// <param name="defaultEndian">未指定目标字节序时的默认字节序</param>
        /// <returns></returns>
        [return: NotNull]
        public static T ToStruct<T>(this ReadOnlySpan<byte> bytes, Endian? defaultEndian = null)
        {
            var t = typeof(T);
            var obj = bytes.ToStruct(t, defaultEndian);
            if (t.IsArray)
            {
                var arr = (object[])obj;
                var ts = Array.CreateInstance(t.GetElementType()!, arr.Length);
                Array.Copy(arr, ts, arr.Length);
                return (T)(object)ts;
            }
            return (T)obj;
        }

        /// <summary>
        /// byte[]转结构体
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset">数据的起始位置</param>
        /// <param name="defaultEndian">未指定目标字节序时的默认字节序</param>
        /// <returns></returns>
        [return: NotNull]
        public static T ToStruct<T>(this Span<byte> bytes, Endian? defaultEndian = null)
        => ToStruct<T>(bytes.AsReadOnly(), defaultEndian);

        /// <summary>
        /// byte[]转结构体
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset">数据的起始位置</param>
        /// <param name="defaultEndian">未指定目标字节序时的默认字节序</param>
        /// <returns></returns>
        [return: NotNull]
        public static T ToStruct<T>(this byte[] bytes, Endian? defaultEndian = null)
        => ToStruct<T>(bytes.AsReadOnlySpan(), defaultEndian);

        /// <summary>
        /// byte[]转结构体
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset">数据的起始位置</param>
        /// <param name="defaultEndian">未指定目标字节序时的默认字节序</param>
        /// <returns></returns>
        [return: NotNull]
        public static T ToStruct<T>(this byte[] bytes, int offset, Endian? defaultEndian = null)
        => ToStruct<T>(bytes.AsReadOnlySpan()[offset..], defaultEndian);

        /// <summary>
        /// byte[]转结构体<br/>
        /// 需要为<typeparamref name="T"/>定义无参构造函数
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset">数据的起始位置</param>
        /// <param name="defaultEndian">未指定目标字节序时的默认字节序</param>
        /// <returns></returns>
        [return: NotNull]
        public static object ToStruct(this ReadOnlySpan<byte> bytes, Type type, Endian? defaultEndian = null)
        {
            if (type.IsArray)
            {
                var elt = type.GetElementType()!;
                var elSize = (elt.IsEnum ? elt.GetEnumUnderlyingType() : elt).GetStructSize();
                var arr = new object[bytes.Length / elSize];
                for (var i = 0; i < arr.Length; i++)
                    arr[i] = bytes[(i * elSize)..].ToStruct(elt, defaultEndian);
                return arr;
            }
            if (type == typeof(IPAddress))
            {
#if NET || NETSTANDARD2_1_OR_GREATER
                return new IPAddress(bytes);
#else
                return new IPAddress(bytes.ToArray());
#endif
            }
            if (type.IsEnum)
                type = type.GetEnumUnderlyingType();
            var size = (type.IsEnum ? type.GetEnumUnderlyingType() : type).GetStructSize();
            if (bytes.Length < size)
                throw new IndexOutOfRangeException();

            object t;
            IntPtr p = default;
            try
            {
                p = Marshal.AllocHGlobal(size);
                var buf = new byte[size];
                bytes.CopyTo(buf);
                Marshal.Copy(buf.FixEndian(type, defaultEndian), 0, p, buf.Length);
                t = Marshal.PtrToStructure(p, type)!;
            }
            finally
            {
                if (p != default)
                    Marshal.FreeHGlobal(p);
            }
            return t;
        }

        /// <summary>
        /// byte[]转结构体<br/>
        /// 需要为<typeparamref name="T"/>定义无参构造函数
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset">数据的起始位置</param>
        /// <param name="defaultEndian">未指定目标字节序时的默认字节序</param>
        /// <returns></returns>
        [return: NotNull]
        public static object ToStruct(this Span<byte> bytes, Type type, Endian? defaultEndian = null)
        => ToStruct(bytes.AsReadOnly(), type, defaultEndian);

        /// <summary>
        /// byte[]转结构体<br/>
        /// 需要为<typeparamref name="T"/>定义无参构造函数
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset">数据的起始位置</param>
        /// <param name="defaultEndian">未指定目标字节序时的默认字节序</param>
        /// <returns></returns>
        [return: NotNull]
        public static object ToStruct(this byte[] bytes, Type type, Endian? defaultEndian = null)
        => ToStruct(bytes.AsReadOnlySpan(), type, defaultEndian);

        /// <summary>
        /// byte[]转结构体<br/>
        /// 需要为<typeparamref name="T"/>定义无参构造函数
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset">数据的起始位置</param>
        /// <param name="defaultEndian">未指定目标字节序时的默认字节序</param>
        /// <returns></returns>
        [return: NotNull]
        public static object ToStruct(this byte[] bytes, Type type, int offset, Endian? defaultEndian = null)
        => ToStruct(bytes.AsReadOnlySpan()[offset..], type, defaultEndian);

        public static bool TryToStruct(this ReadOnlySpan<byte> bytes, Type type, [NotNullWhen(true)] out object? t, Endian? defaultEndian = null)
        {
            try
            {
                t = bytes.ToStruct(type, defaultEndian);
                return true;
            }
            catch (Exception)
            {
                t = default;
                return false;
            }
        }

        public static bool TryToStruct(this Span<byte> bytes, Type type, [NotNullWhen(true)] out object? t, Endian? defaultEndian = null)
        => TryToStruct(bytes.AsReadOnly(), type, out t, defaultEndian);

        public static bool TryToStruct(this byte[] bytes, Type type, [NotNullWhen(true)] out object? t, Endian? defaultEndian = null)
        => TryToStruct(bytes.AsReadOnlySpan(), type, out t, defaultEndian);

        public static bool TryToStruct(this byte[] bytes, Type type, [NotNullWhen(true)] out object? t, int offset, Endian? defaultEndian = null)
        => TryToStruct(bytes.AsReadOnlySpan()[offset..], type, out t, defaultEndian);

        public static bool TryToStruct<T>(this ReadOnlySpan<byte> bytes, [NotNullWhen(true)] out T? t, Endian? defaultEndian = null)
        {
            if (bytes.TryToStruct(typeof(T), out var obj, defaultEndian))
            {
                t = (T)obj;
                return true;
            }
            t = default;
            return false;
        }

        public static bool TryToStruct<T>(this Span<byte> bytes, [NotNullWhen(true)] out T? t, Endian? defaultEndian = null)
        => TryToStruct(bytes.AsReadOnly(), out t, defaultEndian);

        public static bool TryToStruct<T>(this byte[] bytes, [NotNullWhen(true)] out T? t, Endian? defaultEndian = null)
        => TryToStruct(bytes.AsReadOnlySpan(), out t, defaultEndian);

        public static bool TryToStruct<T>(this byte[] bytes, [NotNullWhen(true)] out T? t, int offset, Endian? defaultEndian = null)
        => TryToStruct(bytes.AsReadOnlySpan()[offset..], out t, defaultEndian);

#endregion

        [return: NotNull]
        public static byte[] PrimitiveToBytes<T>(ref T data, Endian? endian = null) where T : unmanaged
        {
            if (!typeof(T).IsPrimitiveOrEnum())
                throw new NotSupportedException();
            var buf = new byte[data.GetStructSize()];
            MemoryMarshal.Write(buf, ref data);
            if (buf.Length > 1 && (endian ?? EndianUtils.LocalEndian) != EndianUtils.LocalEndian)
                Array.Reverse(buf);
            return buf;
        }

        [return: NotNull]
        public static T ToPrimitive<T>(this ReadOnlySpan<byte> data, Endian? endian = null) where T : unmanaged
        {
            if (!typeof(T).IsPrimitiveOrEnum())
                throw new NotSupportedException();
            var buf = new byte[GetStructSize<T>()];
            if (buf.Length < 2 && (endian ?? EndianUtils.LocalEndian) == EndianUtils.LocalEndian)
                return MemoryMarshal.Read<T>(data);
            for (var i = 0; i < buf.Length; i++)
                buf[i] = data[buf.Length - 1 - i];
            return MemoryMarshal.Read<T>(buf);
        }

        [return: NotNull]
        public static byte[] PrimitiveToBytes<T>(ref T[] data, Endian? endian = null) where T : unmanaged
        {
            if (!typeof(T).IsPrimitiveOrEnum())
                throw new NotSupportedException();
            var elsize = data.GetStructSize();
            var buf = new byte[elsize * data.Length];
            var span = new Span<byte>(buf);
            for (var i = 0; i < data.Length; i++)
                MemoryMarshal.Write(span[(i * elsize)..], ref data[i]);
            if (elsize > 1 && (endian ?? EndianUtils.LocalEndian) != EndianUtils.LocalEndian)
                for (var offset = 0; offset < data.Length; offset += elsize)
                    span[offset..elsize].Reverse();
            return buf;
        }

        [return: NotNull]
        public static T[] ToPrimitiveArray<T>(this ReadOnlySpan<byte> data, Endian? endian = null) where T : unmanaged
        {
            if (!typeof(T).IsPrimitiveOrEnum())
                throw new NotSupportedException();
            var elsize = GetStructSize<T>();
            var list = new List<T>();
            var buf = new byte[elsize];
            var needfix = buf.Length > 1 && (endian ?? EndianUtils.LocalEndian) != EndianUtils.LocalEndian;
            for (var offset = 0; offset + elsize <= data.Length; offset += elsize)
            {
                for (var i = 0; i < buf.Length; i++)
                    buf[i] = needfix ? data[buf.Length - 1 - i + offset] : data[offset + i];
                list.Add(MemoryMarshal.Read<T>(data[offset..elsize]));
            }
            return list.ToArray();
        }

        public static int SizeAlignTo(this int size, int align) => (size + align - 1) / align * align;
        public static uint SizeAlignTo(this uint size, uint align) => (size + align - 1) / align * align;
        public static long SizeAlignTo(this long size, long align) => (size + align - 1) / align * align;
        public static ulong SizeAlignTo(this ulong size, ulong align) => (size + align - 1) / align * align;

        public static T[] LengthAlignTo<T>(this T[] src, int align, T fill)
        {
            var flen = src.Length.SizeAlignTo(align) - src.Length;
            return flen > 0 ? src.Concat(Enumerable.Repeat(fill, flen)).ToArray() : src;
        }

        public static T[] LengthAlignTo<T>(this T[] src, int align, Func<T> fill)
        {
            var flen = src.Length.SizeAlignTo(align) - src.Length;
            return flen > 0 ? src.Concat(Enumerable.Repeat(0, flen).Select(_ => fill())).ToArray() : src;
        }

        public static IEnumerable<T> LengthAlignTo<T>(this IReadOnlyCollection<T> src, int align, T fill)
        {
            var flen = src.Count.SizeAlignTo(align) - src.Count;
            return flen > 0 ? src.Concat(Enumerable.Repeat(fill, flen)) : src;
        }

        public static IEnumerable<T> LengthAlignTo<T>(this IReadOnlyCollection<T> src, int align, Func<T> fill)
        {
            var flen = src.Count.SizeAlignTo(align) - src.Count;
            return flen > 0 ? src.Concat(Enumerable.Repeat(0, flen).Select(_ => fill())) : src;
        }
    }

}

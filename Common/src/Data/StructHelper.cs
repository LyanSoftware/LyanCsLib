using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace Lytec.Common.Data
{
    public static partial class StructHelper
    {
        #region 结构体与byte[]互转

        /// <summary>
        /// 结构体转byte[]
        /// </summary>
        /// <param name="t"></param>
        /// <param name="defaultEndian">未指定目标字节序时的默认字节序</param>
        /// <returns></returns>
        public static byte[] ToBytes<T>(this T t, Endian? defaultEndian = null) => t!.ToBytes(t!.GetType(), defaultEndian);

        /// <summary>
        /// 结构体转byte[]
        /// </summary>
        /// <param name="t"></param>
        /// <param name="defaultEndian">未指定目标字节序时的默认字节序</param>
        /// <returns></returns>
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
            int size = Marshal.SizeOf(type);
            var bytes = new byte[size];
            return ToBytes(bytes, to, type, defaultEndian);
        }

        /// <summary>
        /// byte[]转结构体
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset">数据的起始位置</param>
        /// <param name="defaultEndian">未指定目标字节序时的默认字节序</param>
        /// <returns></returns>
        public static T ToStruct<T>(this byte[] bytes, int offset = 0, Endian? defaultEndian = null)
        {
            var t = typeof(T);
            var obj = bytes.ToStruct(t, offset, defaultEndian);
            if (t.IsArray)
            {
                var arr = (object[])obj;
                var ts = Array.CreateInstance(t.GetElementType(), arr.Length);
                Array.Copy(arr, ts, arr.Length);
                return (T)(object)ts;
            }
            return (T)obj;
        }

        public static T ToStruct<T>(this byte[] bytes, Endian? endian)
        => bytes.ToStruct<T>(0, endian);

        /// <summary>
        /// byte[]转结构体<br/>
        /// 需要为<typeparamref name="T"/>定义无参构造函数
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset">数据的起始位置</param>
        /// <param name="defaultEndian">未指定目标字节序时的默认字节序</param>
        /// <returns></returns>
        public static object ToStruct(this byte[] bytes, Type type, int offset = 0, Endian? defaultEndian = null)
        {
            if (type.IsArray)
            {
                var elt = type.GetElementType();
                var elSize = Marshal.SizeOf(elt.IsEnum ? elt.GetEnumUnderlyingType() : elt);
                var arr = new object[(bytes.Length - offset) / elSize];
                for (var i = 0; i < arr.Length; i++)
                    arr[i] = bytes.ToStruct(elt, offset + i * elSize, defaultEndian);
                return arr;
            }
            if (type == typeof(IPAddress)) return new IPAddress(bytes);
            if (type.IsEnum)
                type = type.GetEnumUnderlyingType();
            var size = Marshal.SizeOf(type.IsEnum ? type.GetEnumUnderlyingType() : type);
            if (offset < 0)
            {
                var buf = new byte[-offset + Math.Min(bytes.Length, size)];
                Array.Copy(bytes, 0, buf, -offset, buf.Length + offset);
                offset = 0;
                bytes = buf;
            }
            if (bytes.Length - offset < size)
                throw new IndexOutOfRangeException();
            return ToStruct(bytes, offset, size, type, defaultEndian);
        }

        public static object ToStruct(this byte[] bytes, Type type, Endian? endian)
        => bytes.ToStruct(type, 0, endian);

        public static byte[] ToBytes(byte[] buf, object to, Type type, Endian? defaultEndian = null)
        {
            IntPtr p = default;
            try
            {
                p = Marshal.AllocHGlobal(buf.Length);
                Marshal.StructureToPtr(to, p, false);
                Marshal.Copy(p, buf, 0, buf.Length);
            }
            finally
            {
                if (p != default)
                    Marshal.FreeHGlobal(p);
            }
            return buf.FixEndian(type, defaultEndian);
        }

        public static object ToStruct(byte[] bytes, int offset, int size, Type type, Endian? defaultEndian)
        {
            object t;
            IntPtr p = default;
            try
            {
                p = Marshal.AllocHGlobal(size);
                Marshal.Copy(bytes.FixEndian(type, defaultEndian), offset, p, size);
                t = Marshal.PtrToStructure(p, type);
            }
            finally
            {
                if (p != default)
                    Marshal.FreeHGlobal(p);
            }
            return t;
        }

        #endregion

        public static T ToStruct<T>(this IEnumerable<byte> data, Endian? defaultEndian = null)
        => data.Take(Marshal.SizeOf(typeof(T))).ToArray().ToStruct<T>(defaultEndian);

        public static object ToStruct(this IEnumerable<byte> data, Type type, Endian? defaultEndian)
        => data.Take(Marshal.SizeOf(type)).ToArray().ToStruct(type, defaultEndian);

        public static T ToStruct<T>(this IEnumerable<byte> data, int offset, Endian? defaultEndian = null)
        => data.Skip(offset).Take(Marshal.SizeOf(typeof(T))).ToArray().ToStruct<T>(defaultEndian);

        public static object ToStruct(this IEnumerable<byte> data, int offset, Type type, Endian? defaultEndian)
        => data.Skip(offset).Take(Marshal.SizeOf(type)).ToArray().ToStruct(type, defaultEndian);

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

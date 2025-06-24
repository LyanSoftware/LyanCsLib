using System.Reflection;
using System.Runtime.InteropServices;

namespace Lytec.Common.Data
{
    public static class Endians
    {
        /// <summary>
        /// 运行时的本地字节序
        /// </summary>
        public static Endian LocalEndian { get; } = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;

        public static EndianAttribute GetEndianAttribute(this MemberInfo info, bool inherit = true) => info.GetCustomAttribute<EndianAttribute>(inherit);

        /// <summary>
        /// 修正字节序
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="defaultEndian">未指定目标字节序时的默认字节序</param>
        /// <returns></returns>
        public static byte[] FixEndian(this byte[] data, Type type, Endian? defaultEndian = null)
        {
            if (defaultEndian == null)
                defaultEndian = LocalEndian;
            var newdata = new byte[data.Length];
            Array.Copy(data, newdata, data.Length);

            var typeEndian = type.GetEndianAttribute(true) is EndianAttribute attr1
                ? attr1.Endian : defaultEndian;

            if (type.IsEnum)
            {
                if (typeEndian != LocalEndian)
                    Array.Reverse(newdata);
            }
            else
            {
                for (; type != null; type = type.BaseType)
                {
                    foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        var subType = false;
                        if (f.GetEndianAttribute(false) is EndianAttribute attr2)
                        {
                            // 在字段/属性上标注的
                            if (attr2.Endian == LocalEndian)
                                continue;
                        }
                        else if (f.FieldType.GetEndianAttribute(true) is EndianAttribute attr3)
                        {
                            // 在字段/属性的类型上标注的
                            if (attr3.Endian == LocalEndian)
                                continue;
                            subType = true;
                        }
                        else if (typeEndian == LocalEndian) // 在类里标注的
                            continue;
                        void proc(int offset, int size)
                        {
                            if (subType)
                            {
                                var arr = new byte[size];
                                Array.Copy(data, offset, arr, 0, arr.Length);
                                Array.Copy(arr.FixEndian(f.FieldType, defaultEndian), 0, newdata, offset, arr.Length);
                            }
                            else Array.Reverse(newdata, offset, size);
                        }
                        var fieldOffset = Marshal.OffsetOf(type, f.Name).ToInt32();
                        if (f.FieldType.IsArray)
                        {
                            if (f.GetCustomAttribute<MarshalAsAttribute>() is MarshalAsAttribute marshalAs)
                            {
                                if (marshalAs.Value == UnmanagedType.ByValArray)
                                {
                                    var es = Marshal.SizeOf(f.FieldType.GetElementType());
                                    for (var i = 0; i < marshalAs.SizeConst; i++)
                                        proc(fieldOffset + i * es, es);
                                }
                                else proc(fieldOffset, Marshal.SizeOf<IntPtr>());
                            }
                        }
                        else proc(fieldOffset, Marshal.SizeOf(f.FieldType));
                    }
                }
            }

            return newdata;
        }

        /// <summary>
        /// 修正字节序
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="defaultEndian">未指定目标字节序时的默认字节序</param>
        /// <returns></returns>
        public static byte[] FixEndian<T>(this byte[] data, Endian? defaultEndian = null) => data.FixEndian(typeof(T), defaultEndian);

        /// <summary>
        /// 翻转字节序
        /// </summary>
        /// <param name="i"></param>
        public static void ReverseEndian(ref ushort i)
            => i = (ushort)((i & 0xff) << 8 | i >> 8);

        /// <summary>
        /// 翻转字节序
        /// </summary>
        /// <param name="i"></param>
        public static void ReverseEndian(ref uint i)
            => i =
                (i & 0xff) << 24
                | (i & 0xff00) << 8
                | (i & 0xff0000) >> 8
                | i >> 24
            ;

        /// <summary>
        /// 翻转字节序
        /// </summary>
        /// <param name="i"></param>
        public static void ReverseEndian(ref ulong i)
            => i =
                (i & 0xff) << 56
                | (i & 0xff00) << 40
                | (i & 0xff0000) << 24
                | (i & 0xff000000) << 8
                | (i & 0xff00000000) >> 8
                | (i & 0xff0000000000) >> 24
                | (i & 0xff000000000000) >> 40
                | i >> 56
            ;

        /// <summary>
        /// 修正字节序
        /// </summary>
        /// <param name="i"></param>
        /// <param name="endian">目标字节序</param>
        /// <returns></returns>
        public static ushort FixEndian(this ushort i, Endian endian)
            => endian == LocalEndian ? i : (ushort)((i & 0xff) << 8 | i >> 8);

        /// <summary>
        /// 修正字节序
        /// </summary>
        /// <param name="i"></param>
        /// <param name="endian">目标字节序</param>
        /// <returns></returns>
        public static uint FixEndian(this uint i, Endian endian)
            => endian == LocalEndian ? i :
                (i & 0xff) << 24
                | (i & 0xff00) << 8
                | (i & 0xff0000) >> 8
                | i >> 24
            ;

        /// <summary>
        /// 修正字节序
        /// </summary>
        /// <param name="i"></param>
        /// <param name="endian">目标字节序</param>
        /// <returns></returns>
        public static ulong FixEndian(this ulong i, Endian endian)
        {
            return endian == LocalEndian ? i :
                       (i & 0xff) << 56
                       | (i & 0xff00) << 40
                       | (i & 0xff0000) << 24
                       | (i & 0xff000000) << 8
                       | (i & 0xff00000000) >> 8
                       | (i & 0xff0000000000) >> 24
                       | (i & 0xff000000000000) >> 40
                       | i >> 56
                   ;
        }
    }

}

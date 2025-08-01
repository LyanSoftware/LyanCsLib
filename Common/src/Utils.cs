using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Lytec.Common.Data;

namespace Lytec.Common
{

    /// <summary>
    /// 布尔运算模式
    /// </summary>
    public enum BoolMode
    {
        /// <summary>
        /// 交集（与）
        /// </summary>
        And = 0,

        /// <summary>
        /// 并集（或）
        /// </summary>
        Or,

        /// <summary>
        /// 差集（异或）
        /// </summary>
        Xor
    }

    public static partial class Utils
    {
        public static int ToInt(this bool b) => b ? 1 : 0;

        public static int Add(this bool b, bool v) => b.ToInt() + v.ToInt();

        public static int Add(this int v, bool b) => v + b.ToInt();

        public static string GetFileVersion(this Assembly assembly)
        => GetFileVersion(assembly.Location);
        public static string GetFileVersion(string filePath)
        => FileVersionInfo.GetVersionInfo(filePath).FileVersion;
        public static string? GetShortFileVersion(this Assembly assembly)
        => GetShortFileVersion(assembly.Location);
        private static readonly Regex FileVersionShorterRegex = new Regex(@"^(\d+\.\d.*?)\.0+$", RegexOptions.Compiled);
        public static string? GetShortFileVersion(string filePath)
        {
            var v = GetFileVersion(filePath);
            if (v.IsNullOrEmpty())
                return null;
            for (var m = FileVersionShorterRegex.Match(v); m.Success; m = FileVersionShorterRegex.Match(v))
                v = m.Groups[1].Value;
            return v;
        }

        public static void Write(this Stream st, byte[] buf) => st.Write(buf, 0, buf.Length);
        public static Task WriteAsync(this Stream st, byte[] buf, CancellationToken cancellationToken) => st.WriteAsync(buf, 0, buf.Length, cancellationToken);

        public static bool Is8BitAnsi(this Encoding encoding)
        => !CanEncode(encoding, '　');

        public static bool CanEncode(this Encoding encoding, int chr)
        => CanEncode(encoding.CodePage, char.ConvertFromUtf32(chr));
        public static bool CanEncode(this Encoding encoding, char chr)
        => CanEncode(encoding.CodePage, chr.ToString());
        public static bool CanEncode(this Encoding encoding, string s)
        => CanEncode(encoding.CodePage, s);
        public static bool CanEncode(int codepage, string s)
        {
            try
            {
                Encoding.GetEncoding(codepage,
                                     EncoderFallback.ExceptionFallback,
                                     DecoderFallback.ExceptionFallback).GetBytes(s);
                return true;
            }
            catch (EncoderFallbackException)
            {
                return false;
            }
        }

        public static int GetUtf32Char(this string str, int index = 0) => char.IsSurrogate(str, index) ? char.ConvertToUtf32(str, index) : str[index];

        public static int GetGCD(int x, int y)
        {
            static int __gcd(int a, int b)
            {
                if (b == 0)
                    return a;
                return __gcd(b, a % b);
            }
            return x > y ? __gcd(x, y) : __gcd(y, x);
        }

        public static int ToInt(this IPAddress ip, Endian endian = Endian.Big)
        {
            var bytes = ip.GetAddressBytes();
            return bytes.ToStruct<int>(bytes.Length - 4, endian);
        }

        public static void TriggerEvent(this object obj, string name, EventArgs? e = null)
        {
            var handlers = ((MulticastDelegate)obj.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj))?.GetInvocationList();
            if (handlers != null)
                foreach (Delegate dlg in handlers)
                    dlg.Method.Invoke(dlg.Target, new object?[] { obj, e });
        }

        public static Type[] GetGenericArguments(this Type t, Type baseType)
        => t.IsGenericType && t.GetGenericTypeDefinition() == baseType ? t.GetGenericArguments() : Array.Empty<Type>();

        public static bool IsDecChar(this char c) => (c >= '0' && c <= '9');

        public static bool IsHexChar(this char c) => c.IsDecChar() || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');

        public static bool IsAsciiChar(this char c) => c >= 0x0020 && c <= 0x007E;

        public static bool IsDecChar(this byte c) => ((char)c).IsDecChar();

        public static bool IsHexChar(this byte c) => ((char)c).IsDecChar();

        public static bool IsAsciiChar(this byte c) => ((char)c).IsAsciiChar();

        public static int HexCharToInt(this char c) => (c >= 'A' && c <= 'F') ? (c - 'A' + 10) : (c >= 'a' && c <= 'f') ? (c - 'a' + 10) : (c - '0');

        public static decimal ParseAnyFormatNumber(this string str)
        {
            if (str.IsNullOrWhiteSpace())
                throw new FormatException();
            if (str.StartsWith("0x", true, null))
                return Convert.ToUInt64(str.Substring(2), 16);
            if (str.StartsWith("0b", true, null))
                return Convert.ToUInt64(str.Substring(2), 2);
            return decimal.Parse(str);
        }

        public static bool TryParseAnyFormatNumber(this string str, out decimal value)
        {
            value = default;
            try
            {
                value = str.ParseAnyFormatNumber();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsNullOrEmpty([AllowNull][NotNullWhen(false)] this string str) => string.IsNullOrEmpty(str);
        public static bool IsNullOrWhiteSpace([AllowNull][NotNullWhen(false)] this string str) => string.IsNullOrWhiteSpace(str);

        public static void ForceLoadClass<T>() => typeof(T).ForceLoadClass();
        public static void ForceLoadClass(this Type t) => System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(t.TypeHandle);

        public static void ForceLoadBaseClass<T>() => typeof(T).BaseType?.ForceLoadClass();
        public static void ForceLoadBaseClass(this Type t) => t.BaseType?.ForceLoadClass();

        public static IDictionary<Type, string> NestedClassNames { get; set; } = new Dictionary<Type, string>();
        public static string GetNestedClassName(this Type t)
        {
            if (!NestedClassNames.ContainsKey(t))
            {
                var names = new List<string>() { t.Name };
                while (t.IsNested)
                {
                    t = t.DeclaringType;
                    names.Add(t.Name);
                }
                NestedClassNames[t] = names.AsEnumerable().Reverse().JoinToString(sep: ".");
            }
            return NestedClassNames[t];
        }

        public static bool IsRTLChar(this char chr)
        {
            var c = char.ConvertToUtf32('\0', chr);
            if (c >= 0x5BE && c <= 0x10B7F)
            {
                if (c <= 0x85E)
                {
                    if (c == 0x5BE) return true;
                    else if (c == 0x5C0) return true;
                    else if (c == 0x5C3) return true;
                    else if (c == 0x5C6) return true;
                    else if (0x5D0 <= c && c <= 0x5EA) return true;
                    else if (0x5F0 <= c && c <= 0x5F4) return true;
                    else if (c == 0x608) return true;
                    else if (c == 0x60B) return true;
                    else if (c == 0x60D) return true;
                    else if (c == 0x61B) return true;
                    else if (0x61E <= c && c <= 0x64A) return true;
                    else if (0x66D <= c && c <= 0x66F) return true;
                    else if (0x671 <= c && c <= 0x6D5) return true;
                    else if (0x6E5 <= c && c <= 0x6E6) return true;
                    else if (0x6EE <= c && c <= 0x6EF) return true;
                    else if (0x6FA <= c && c <= 0x70D) return true;
                    else if (c == 0x710) return true;
                    else if (0x712 <= c && c <= 0x72F) return true;
                    else if (0x74D <= c && c <= 0x7A5) return true;
                    else if (c == 0x7B1) return true;
                    else if (0x7C0 <= c && c <= 0x7EA) return true;
                    else if (0x7F4 <= c && c <= 0x7F5) return true;
                    else if (c == 0x7FA) return true;
                    else if (0x800 <= c && c <= 0x815) return true;
                    else if (c == 0x81A) return true;
                    else if (c == 0x824) return true;
                    else if (c == 0x828) return true;
                    else if (0x830 <= c && c <= 0x83E) return true;
                    else if (0x840 <= c && c <= 0x858) return true;
                    else if (c == 0x85E) return true;
                }
                else if (c == 0x200F) return true;
                else if (c >= 0xFB1D)
                {
                    if (c == 0xFB1D) return true;
                    else if (0xFB1F <= c && c <= 0xFB28) return true;
                    else if (0xFB2A <= c && c <= 0xFB36) return true;
                    else if (0xFB38 <= c && c <= 0xFB3C) return true;
                    else if (c == 0xFB3E) return true;
                    else if (0xFB40 <= c && c <= 0xFB41) return true;
                    else if (0xFB43 <= c && c <= 0xFB44) return true;
                    else if (0xFB46 <= c && c <= 0xFBC1) return true;
                    else if (0xFBD3 <= c && c <= 0xFD3D) return true;
                    else if (0xFD50 <= c && c <= 0xFD8F) return true;
                    else if (0xFD92 <= c && c <= 0xFDC7) return true;
                    else if (0xFDF0 <= c && c <= 0xFDFC) return true;
                    else if (0xFE70 <= c && c <= 0xFE74) return true;
                    else if (0xFE76 <= c && c <= 0xFEFC) return true;
                    else if (0x10800 <= c && c <= 0x10805) return true;
                    else if (c == 0x10808) return true;
                    else if (0x1080A <= c && c <= 0x10835) return true;
                    else if (0x10837 <= c && c <= 0x10838) return true;
                    else if (c == 0x1083C) return true;
                    else if (0x1083F <= c && c <= 0x10855) return true;
                    else if (0x10857 <= c && c <= 0x1085F) return true;
                    else if (0x10900 <= c && c <= 0x1091B) return true;
                    else if (0x10920 <= c && c <= 0x10939) return true;
                    else if (c == 0x1093F) return true;
                    else if (c == 0x10A00) return true;
                    else if (0x10A10 <= c && c <= 0x10A13) return true;
                    else if (0x10A15 <= c && c <= 0x10A17) return true;
                    else if (0x10A19 <= c && c <= 0x10A33) return true;
                    else if (0x10A40 <= c && c <= 0x10A47) return true;
                    else if (0x10A50 <= c && c <= 0x10A58) return true;
                    else if (0x10A60 <= c && c <= 0x10A7F) return true;
                    else if (0x10B00 <= c && c <= 0x10B35) return true;
                    else if (0x10B40 <= c && c <= 0x10B55) return true;
                    else if (0x10B58 <= c && c <= 0x10B72) return true;
                    else if (0x10B78 <= c && c <= 0x10B7F) return true;
                }
            }
            return false;
        }

        public static string JoinToString<T>(this IEnumerable<T> list, string sep)
        => list.JoinToString(null, sep);

        public static string JoinToString<T>(this IEnumerable<T> list, Func<T, string>? toString = null, string sep = ",")
        {
            if (toString == null)
                toString = t => t?.ToString() ?? "";
            var isFirst = true;
            var sb = new StringBuilder();
            var addSep = !sep.IsNullOrEmpty();
            foreach (var str in list.Select(toString))
            {
                if (isFirst)
                    isFirst = false;
                else if (addSep)
                    sb.Append(sep);
                sb.Append(str);
            }
            return sb.ToString();
        }

        public static string CamelCase2SnakeCase(this string input)
        => Regex.Replace(input, @"(?<=[a-z0-9])[A-Z]", m => "_" + m.Value).ToLower();

#if !NETCOREAPP3_2_OR_GREATER && !NET6
        public static bool IsAssignableTo(this Type t, Type c) => c.IsAssignableFrom(t);
#endif

        public static IEnumerable<object> GetPredefineObjects(this Type t)
            => from field in t.GetFields(BindingFlags.Public | BindingFlags.Static)
               where field.FieldType.IsAssignableFrom(t)
               select field.GetValue(null);

        public static IEnumerable<T> GetPredefineObjects<T>()
            => from field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static)
               where field.FieldType.IsAssignableFrom(typeof(T))
               select (T)field.GetValue(null);

        public static T GetCustomAttribute<TEnum, T>(this TEnum e) where TEnum : Enum where T : Attribute
        => typeof(TEnum).GetField(Enum.GetName(typeof(TEnum), e)).GetCustomAttributes<T>().FirstOrDefault();

        public static string Encrypt(this System.Security.Cryptography.HashAlgorithm algorithm, string source, string? salt = null, Encoding? encode = default)
        {
            encode = encode ?? Encoding.UTF8;
            var buf = algorithm.ComputeHash(encode.GetBytes(source));
            if (!string.IsNullOrEmpty(salt))
                algorithm.ComputeHash(buf.Concat(encode.GetBytes(salt)).ToArray());
            return algorithm.Hash.ToHex("");
        }

        public static void Add(this MultipartFormDataContent content, string name, string value)
        {
            var str = new StringContent(value);
            content.Add(str, name);
        }

        //public static IReadOnlyDictionary<string, string> DecodeHttpArgs(this string data, CaseSensitivity caseopt = CaseSensitivity.Sensitive)
        //{
        //    var args = new Dictionary<string, string>();
        //    foreach (var kv in data.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
        //    {
        //        var arg = kv.Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
        //        string key;
        //        switch (caseopt)
        //        {
        //            case CaseSensitivity.ConvertToUpperCase: key = arg[0].ToUpper(); break;
        //            case CaseSensitivity.ConvertToLowerCase: key = arg[0].ToLower(); break;
        //            default: key = arg[0]; break;
        //        }
        //        if (arg.Length == 1)
        //            args.Add(key, "");
        //        else if (arg.Length > 1)
        //            args.Add(key, arg[1]);
        //    }
        //    return args;
        //}

        public static string GetDescription(this object obj)
        {
            var type = obj.GetType();
            var member = type.IsEnum ? (MemberInfo)type.GetField(obj.ToString()) : obj.GetType();
            if (member == null)
                return obj.ToString();
            return member.GetCustomAttributes<System.ComponentModel.DescriptionAttribute>().FirstOrDefault()?.Description ?? member.Name;
        }

        public class EnumDataWithDescription : EnumDataWithDescription<Enum> { }
        public class EnumDataWithDescription<T> where T : Enum
        {
            public string? Name { get; }
            public T? Value { get; }
            public string? Description { get; }

            protected EnumDataWithDescription() { }
            public EnumDataWithDescription(string name, T value, string description)
            {
                Name = name;
                Value = value;
                Description = description;
            }
            public void Deconstruct(out string? Name, out T? Value, out string? Description)
            {
                Name = this.Name;
                Value = this.Value;
                Description = this.Description;
            }
        }
        public static IEnumerable<EnumDataWithDescription<TEnum>> GetEnumDatasWithDescription<TEnum>() where TEnum : Enum
        => from TEnum Value in Enum.GetValues(typeof(TEnum))
           select new EnumDataWithDescription<TEnum>(Value.ToString(), Value, Value.GetDescription());

        public static string GetInnerMessage(this Exception err)
        {
            while (err.InnerException != null)
                err = err.InnerException;
            return err.Message;
        }

        public static string? GetGuid(this Type t)
        {
            var guid = (GuidAttribute[])t.Assembly.GetCustomAttributes(typeof(GuidAttribute), true);
            return guid.Length > 0 ? guid[0].Value : null;
        }
        public static string? GetGuid<T>() => GetGuid(typeof(T));

        public static void SleepOrTimeout(this DateTime timeout, int sleepMs = 10)
        {
            if (DateTime.Now > timeout)
                throw new TimeoutException();
            Thread.Sleep(sleepMs);
        }

        public static void YieldWait(this Task task)
        {
            while (!task.IsCompleted)
                Thread.Yield();
        }

        public static T YieldWait<T>(this Task<T> task)
        {
            while (!task.IsCompleted)
                Thread.Yield();
            return task.Result;
        }

        public static void SleepWait(this Task task)
        {
            while (!task.IsCompleted)
                Thread.Sleep(0);
        }

        public static T SleepWait<T>(this Task<T> task)
        {
            while (!task.IsCompleted)
                Thread.Sleep(0);
            return task.Result;
        }

        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kv, out TKey Key, out TValue Value)
        {
            Key = kv.Key;
            Value = kv.Value;
        }

        public static string MakeRelativePath(this string path, string basePath) => new Uri(basePath).MakeRelativeUri(new Uri(path)).ToString();

        public static string MakeRelativePath(this string path, Uri baseUri) => baseUri.MakeRelativeUri(new Uri(path)).ToString();

        public static string MakeRelativePath(this Uri uri, string basePath) => new Uri(basePath).MakeRelativeUri(uri).ToString();

        public static T JsonClone<T>(this T obj) => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Newtonsoft.Json.JsonConvert.SerializeObject(obj))!;

        public static bool JsonEquals<T>(this T a, T b)
        => Newtonsoft.Json.JsonConvert.SerializeObject(a, Newtonsoft.Json.Formatting.None) == Newtonsoft.Json.JsonConvert.SerializeObject(b, Newtonsoft.Json.Formatting.None);

        public static void Clear<T>(this ConcurrentQueue<T> queue)
        {
            while (queue.Count > 0)
                queue.TryDequeue(out _);
        }

        public static string GetRtfUnicodeEscapedString(this string str)
        {
            var sb = new StringBuilder();
            foreach (var c in str)
            {
                if (c <= 0x7f)
                    sb.Append(c);
                else
                    sb.Append("\\u" + Convert.ToUInt32(c) + "?");
            }
            return sb.ToString();
        }

        public static bool CompareTo(this string src, string dst, int srcOffset, int dstOffset = 0)
        {
            for (int i = 0, cmplen = Math.Min(src.Length + srcOffset, dst.Length + dstOffset); i < cmplen; i++)
                if (src[srcOffset + i] != dst[dstOffset + i])
                    return false;
            return true;
        }

        public static string GetClosedSubstring(this string source, string startText, string endText, int start = 0, int end = -1)
        {
            if (start < 0)
                start += source.Length;
            if (end < 0)
                end += source.Length + 1;
            if (end < start)
                (start, end) = (end, start);
            else if (end == start)
                return "";
            while (!source.CompareTo(startText, start))
                start++;
            var current = start + startText.Length;
            for (var sub = 0; current < end;)
            {
                if (source.CompareTo(startText, current))
                {
                    sub++;
                    current += startText.Length;
                }
                else if (source.CompareTo(endText, current))
                {
                    sub--;
                    current += endText.Length;
                    if (sub < 0)
                        break;
                }
                else current++;
            }
            return current < end ? source.Substring(start, current - start) : "";
        }

        /// <summary>
        /// 设置尝试加载Resources内dll
        /// </summary>
        /// <param name="appDomain"></param>
        /// <param name="Namespace">Resources所属命名空间</param>
        public static void SetLoadResourcesLibraries(this AppDomain appDomain, string Namespace)
        => appDomain.AssemblyResolve += (sender, args) =>
        {
            string dllName = args.Name.Contains(",") ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name.Replace(".dll", "");
            dllName = dllName.Replace(".", "_");
            if (dllName.EndsWith("_resources")) return null;
            ResourceManager rm = new ResourceManager(Namespace + ".Properties.Resources", Assembly.GetExecutingAssembly());
            byte[] bytes = (byte[])rm.GetObject(dllName);
            return Assembly.Load(bytes);
        };

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<(TKey Key, TValue Value)> origin)
        => origin.ToDictionary(kv => kv.Key, kv => kv.Value);

        public static (TKey Key, TValue Value)[] ToArray<TKey, TValue>(this IDictionary<TKey, TValue> dic)
        => dic.Select(kv => (kv.Key, kv.Value)).ToArray();

        /// <summary>
        /// 交换两个对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static void Swap<T>(ref T a, ref T b)
        => (b, a) = (a, b);

        /// <summary>
        /// 截取子数组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr"></param>
        /// <param name="start"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static T[] Subarray<T>(this T[] arr, int start = 0, int length = -1)
        {
            if (start < 0)
                start += arr.Length;
            if (length < 0)
                length = arr.Length - start;
            var sub = new T[length];
            Array.Copy(arr, start, sub, 0, length);
            return sub;
        }

        /// <summary>
        /// 转换为IPAddress
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static IPAddress? ToIPAddress(this string str) => (str.Length > 0 && IPAddress.TryParse(str, out var ip)) ? ip : null;

        /// <summary>
        /// 转换为IPAddress
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool ToIPAddress(this string str, out IPAddress? ip)
        {
            ip = str.ToIPAddress();
            return ip != null;
        }

        /// <summary>
        /// 获取所有本地网卡及对应IP列表
        /// </summary>
        /// <returns></returns>
        public static (NetworkInterface, IPAddress[])[] GetAllLocalNetIPAddresses()
        => (from ni in NetworkInterface.GetAllNetworkInterfaces()
            select (
                ni,
                ni.GetIPProperties().UnicastAddresses.Select(i => i.Address).ToArray()
            )).ToArray();

        /// <summary>
        /// 获取Tcp连接状态
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static TcpState GetState(this TcpClient client)
            => IPGlobalProperties.GetIPGlobalProperties()
                                 .GetActiveTcpConnections()
                                 .SingleOrDefault(info => info.LocalEndPoint.Equals(client.Client.LocalEndPoint) && info.RemoteEndPoint.Equals(client.Client.RemoteEndPoint))
                                 ?.State ?? TcpState.Unknown;

        /// <summary>
        /// 检查Tcp连接状态是否为TcpState.Established
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static bool IsEstablished(this TcpClient client) => client.GetState() == TcpState.Established;

        /// <summary>
        /// 翻转数组
        /// </summary>
        /// <param name="i"></param>
        public static T[] Reversed<T>(this T[] data)
        {
            T[] ts = new T[data.Length];
            Array.Copy(data, ts, ts.Length);
            Array.Reverse(ts);
            return ts;
        }

        /// <summary>
        /// 模糊二分搜索
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">数据源</param>
        /// <param name="target">搜索目标</param>
        /// <param name="comparison">比较器，为null时使用Comparer<see cref="{T}" />.Default.Compare</param>
        /// <returns></returns>
        public static int FuzzyBinarySearch<T>(this IList<T> list, T target, Comparison<T>? comparison = null)
        {
            if (comparison == null)
                comparison = Comparer<T>.Default.Compare;
            int right = list.Count - 1;
            if (comparison(target, list[0]) <= 0)
                return 0;
            else if (comparison(target, list[right]) >= 0)
                return right;
            int left = 0;
            for (int index, cmp; left + 1 < right;)
            {
                index = left + (right - left) / 2;
                cmp = comparison(target, list[index]);
                if (cmp > 0)
                    left = index;
                else if (cmp < 0)
                    right = index;
                else break;
            }
            return left;
        }

        /// <summary>
        /// 将Flags转换为Flag[]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static T[] GetFlags<T>(this T flags) where T : Enum
        {
            List<T> list = new List<T>();
            foreach (T flag in Enum.GetValues(typeof(T)))
                if (flags.HasFlag(flag))
                    list.Add(flag);
            return list.ToArray();
        }

        public static bool HasFlags<T>(this T flags, T flag) where T : struct, Enum => flags.HasFlag(flag);

        /// <summary>
        /// 修改Flag
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="flags"></param>
        /// <param name="flag"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T SetFlag<T>(this T flags, T flag, bool value) where T : struct, Enum
        {
            long c, t;
            try
            {
                c = Convert.ToInt64(flags);
                t = Convert.ToInt64(flag);
            }
            catch (OverflowException)
            {
                c = (long)Convert.ToUInt64(flags);
                t = (long)Convert.ToUInt64(flag);
            }
            return (T)Enum.ToObject(typeof(T), value ? (c | t) : (c & (~t)));
        }

        /// <summary>
        /// 修改Flag
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="flags"></param>
        /// <param name="flag"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T SetFlag_safe<T>(this T flags, T flag, bool value) where T : struct, Enum
        {
            var fs = (ulong)(object)flags;
            var f = (ulong)(object)flag;
            if (typeof(T).GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0
                && Enum.TryParse<T>((value ? (fs | f) : (fs & (~f))).ToString(), out var t))
                return t;
            throw new ArgumentException();
        }

        /// <summary>
        /// 按顺序依次执行委托中所有函数，并返回最终结果
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="funcs"></param>
        /// <param name="arg"></param>
        /// <param name="stopAtNull">是否在函数输出为null时停止</param>
        /// <returns></returns>
        public static T? SequentialExecute<T>(this Func<T, T> funcs, T arg, bool stopAtNull = false)
        {
            foreach (Func<T, T> f in funcs.GetInvocationList())
            {
                if (stopAtNull && arg == null)
                    return default;
                arg = f(arg);
            }
            return arg;
        }

        /// <summary>
        /// 按顺序依次执行委托中所有函数（结果作为第二个参数），并返回最终结果
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="funcs"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        public static T? SequentialExecute<T>(this Func<T, T, T> funcs, T arg1, T arg2, bool stopAtNull = false)
        {
            foreach (Func<T, T, T> f in funcs.GetInvocationList())
            {
                if (stopAtNull && arg2 == null)
                    return default;
                arg2 = f(arg1, arg2);
            }
            return arg2;
        }

        /// <summary>
        /// 按顺序依次判断是否符合条件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="funcs"></param>
        /// <param name="item"></param>
        /// <param name="mode">函数返回值的运算方式</param>
        /// <returns></returns>
        public static bool SequentialExecute<T>(this Predicate<T> funcs, T item, BoolMode mode = BoolMode.And)
        {
            bool ret = false;
            switch (mode)
            {
                case BoolMode.And:
                    ret = true;
                    foreach (Predicate<T> predicate in funcs.GetInvocationList())
                    {
                        ret = ret && predicate(item);
                        if (!ret)
                            break;
                    }
                    break;
                case BoolMode.Or:
                    foreach (Predicate<T> predicate in funcs.GetInvocationList())
                    {
                        ret = ret || predicate(item);
                        if (ret)
                            break;
                    }
                    break;
                case BoolMode.Xor:
                    bool find = false;
                    foreach (Predicate<T> predicate in funcs.GetInvocationList())
                    {
                        if (find)
                        {
                            if (predicate(item))
                            {
                                find = false;
                                break;
                            }
                        }
                        else find = predicate(item);
                    }
                    ret = find;
                    break;
            }
            return ret;
        }

        /// <summary>
        /// 将函数添加到委托最前面
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="funcs">原委托</param>
        /// <param name="func">要添加的函数</param>
        /// <returns></returns>
        public static Func<T, T> AddToTop<T>(this Func<T, T> funcs, Func<T, T> func)
        {
            foreach (Func<T, T> f in funcs.GetInvocationList())
                func += f;
            return func;
        }

        /// <summary>
        /// 将函数添加到委托最前面
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="funcs">原委托</param>
        /// <param name="func">要添加的函数</param>
        /// <returns></returns>
        public static Func<T, T, T> AddToTop<T>(this Func<T, T, T> funcs, Func<T, T, T> func)
        {
            foreach (Func<T, T, T> f in funcs.GetInvocationList())
                func += f;
            return func;
        }

        public static IEnumerable<T> GetOrInherited<T>(this Type t, Func<Type, T> func, Type? top = null)
        {
            for (var t1 = t; t1 != null; t1 = t1.BaseType)
            {
                yield return func(t1);
                if (t1 == top)
                    break;
            }
            foreach (var t1 in t.GetInterfaces())
                yield return func(t1);
        }

        public static bool IsSubtypeOf(this Type t, Type baseType)
        {
            if (baseType.IsEnum || t.IsEnum)
                return t == baseType;
            if (!baseType.IsGenericType)
                return baseType.IsAssignableFrom(t);
            return t.GetGenericArgumentsOf(baseType).Any();
        }

        public static IEnumerable<Type[]> GetGenericArgumentsOf(this Type t, Type baseType)
        {
            if (!baseType.IsGenericType)
                yield break;
            Type[]? get(Type t1) => t1.IsGenericType && t1.GetGenericTypeDefinition() == baseType ? t1.GetGenericArguments() : null;
            if (baseType.IsInterface)
            {
                foreach (var args in t.GetInterfaces().Select(get))
                {
                    if (args != null)
                        yield return args;
                }
            }
            else
            {
                for (; t != null; t = t.BaseType)
                {
                    var args = get(t);
                    if (args != null)
                        yield return args;
                }
            }
        }

        public static bool IsSubtypeOf<T>(this Type t) => t.IsSubtypeOf(typeof(T));

        public static bool IsSubtypeObjectOf<T>(this T t, Type baseType) => t!.GetType().IsSubtypeOf(baseType);

        public static bool IsSubtypeObjectOf<T, TBase>(this T t) => t.IsSubtypeObjectOf(typeof(T));
    }

}

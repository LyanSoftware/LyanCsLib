using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace Lytec.Common.Converters
{
    using static ConvertUtils;

    public class CannotConvertException : Exception
    {
        public CannotConvertException() : base("Cannot convert") { }
        public CannotConvertException(string msg) : base(msg) { }
        public CannotConvertException(string msg, Exception inner) : base(msg, inner) { }
    }

    public static class ConvertUtils
    {
        public static MethodInfo? GetParseMethod<Input>(this Type type) => GetParseMethod(type, typeof(Input));
        public static MethodInfo? GetParseMethod(this Type type, Type input)
        {
            var parse = type.GetMethod("Parse", new Type[] { input });
            return parse != null && parse.IsStatic && parse.ReturnType == type ? parse : null;
        }

        public static MethodInfo? GetTryParseMethod<Input>(this Type type) => GetTryParseMethod(type, typeof(Input));
        public static MethodInfo? GetTryParseMethod(this Type type, Type input)
        {
            var tryparse = type.GetMethod("TryParse", new Type[] { input, type });
            return tryparse != null && tryparse.IsStatic && tryparse.ReturnType == typeof(bool) && tryparse.GetParameters()[1].IsOut ? tryparse : null;
        }

        public static bool TryParse<Result>(object input, [NotNullWhen(true)] out Result? result)
        {
            if (TryParse(typeof(Result), input, out var obj))
            {
                result = (Result)obj;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        public static T? Parse<T>(this Type type, object input) => (T?)Parse(type, input);

        public static object? Parse(this Type type, object input)
        {
            var inputType = input.GetType();
            var parse = type.GetTryParseMethod(inputType);
            var args = new object?[] { input, null };
            if (parse != null && (bool)parse.Invoke(null, args))
                return args[1];
            parse = type.GetParseMethod(inputType);
            if (parse != null)
                return parse.Invoke(null, new object[] { input });
            throw new CannotConvertException($"The {type.Name} type does not have a public static Parse({inputType.Name}) method that returns a {type.Name} or a public static TryParse({inputType.Name}, out {type.Name}) method that returns a bool");
        }

        public static bool TryParse<T>(this Type type, object input, [NotNullWhen(true)] out T? result)
        {
            var ret = TryParse(type, input, out var obj);
            result = ret ? (T?)obj : default;
            return ret;
        }

        public static bool TryParse(this Type type, object input, [NotNullWhen(true)] out object? result)
        {
            try
            {
                result = Parse(type, input);
                return result != null;
            }
            catch (Exception)
            {
                result = default;
                return false;
            }
        }

        public static bool TryParse(string s, [NotNullWhen(true)] out IPEndPoint? result)
        {
#if NET || NETCOREAPP3_1 || NETCOREAPP3_0
            return IPEndPoint.TryParse(s, out result);
#else
            int addressLength = s.Length;  // If there's no port then send the entire string to the address parser
            int lastColonPos = s.LastIndexOf(':');

            // Look to see if this is an IPv6 address with a port.
            if (lastColonPos > 0)
            {
                if (s[lastColonPos - 1] == ']')
                {
                    addressLength = lastColonPos;
                }
                // Look to see if this is IPv4 with a port (IPv6 will have another colon)
                else if (s.Substring(0, lastColonPos).LastIndexOf(':') == -1)
                {
                    addressLength = lastColonPos;
                }
            }

            if (IPAddress.TryParse(s.Substring(0, addressLength), out var address))
            {
                uint port = 0;
                if (addressLength == s.Length ||
                    (uint.TryParse(s.Substring(addressLength + 1), NumberStyles.None, CultureInfo.InvariantCulture, out port) && port <= ushort.MaxValue))

                {
                    result = new IPEndPoint(address, (int)port);
                    return true;
                }
            }

            result = null;
            return false;
#endif
        }

        public static IPEndPoint? ParseIPEndPoint(string s)
        {
            if (TryParse(s, out IPEndPoint? result))
                return result;
            throw new FormatException();
        }
    }

    public class IPEndPointStringTypeConverter : StringTypeConverter<IPEndPoint>
    {
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return value is string str && TryParse(str, out var ep) ? ep : value;
        }
    }

    public class StringTypeConverter<T> : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return value is string str && typeof(T).TryParse(str, out var obj) ? obj : value;
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string)) return value.ToString();
            throw new NotSupportedException();
        }
    }

    public class StringJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => true;

        [return: MaybeNull]
        public override object ReadJson(JsonReader reader, Type objectType, [AllowNull] object existingValue, JsonSerializer serializer)
        {
            return reader.Value is string str ? objectType.Parse(str) : default;
        }

        public override void WriteJson(JsonWriter writer, [AllowNull] object value, JsonSerializer serializer)
        {
            if (value != null)
                writer.WriteValue(value.ToString());
        }
    }

    public class IPEndPointJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(IPEndPoint);

        [return: MaybeNull]
        public override object ReadJson(JsonReader reader, Type objectType, [AllowNull] object existingValue, JsonSerializer serializer)
        {
            return reader.Value is string str ? ParseIPEndPoint(str) : default;
        }

        public override void WriteJson(JsonWriter writer, [AllowNull] object value, JsonSerializer serializer)
        {
            if (value != null)
                writer.WriteValue(value.ToString());
        }
    }

}

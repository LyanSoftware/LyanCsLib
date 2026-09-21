using Lytec.Common.Localization.Extensions;
using Org.BouncyCastle.Crypto.Tls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Lytec.Common
{
    public static class EnumPolyfill
    {
#if NETSTANDARD2_0 || NETSTANDARD2_1
        extension(Enum)
        {
            [RequiresDynamicCode("直接调用Enum.GetValues(typeof(T))")]
            public static IEnumerable<T> GetValues<T>() where T : struct, Enum
            => Enum.GetValues(typeof(T)).Cast<T>();
        }
#endif
    }

    public static class EnumUtils
    {
#if !NET6_0_OR_GREATER
        [RequiresDynamicCode("直接调用Enum.GetValues(typeof(T))")]
        public static IEnumerable<T> GetValues<T>() where T : struct, Enum
        => Enum.GetValues(typeof(T)).Cast<T>();
#endif

        /// <summary>
        /// 将Flags转换为Flag[]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static T[] GetFlags<T>(this T flags) where T : struct, Enum
        {
            var list = new List<T>();
            foreach (T flag in Enum.GetValues<T>())
                if (flags.HasFlag(flag))
                    list.Add(flag);
            return [.. list];
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
            throw new ArgumentException($"Type {typeof(T).Name} is not marked as Flags (System.FlagsAttribute)")
                .Localize(
                    "Lytec.Common.Utils.SetFlag_safe.NotFlagEnumError",
                    "Type {{Type}} is not marked as Flags (System.FlagsAttribute)",
                    ("Type", typeof(T).Name)
                    );
        }

        public static string GetDescription<T>(this T obj) where T : struct, Enum
        => obj.GetEnumFieldInfo()?.GetCustomAttributes<DescriptionAttribute>().FirstOrDefault()?.Description ?? obj.ToString()!;

        public static T GetDefault<T>() where T : Enum => (T)Enum.ToObject(typeof(T), 0);

        public class EnumDataWithDescription : EnumDataWithDescription<Enum> { }
        public class EnumDataWithDescription<T> where T : Enum
        {
            public string Name { get; } = "";
            public T Value { get; } = (T)Activator.CreateInstance(typeof(T))!;
            public string Description { get; } = "";

            protected EnumDataWithDescription() { }
            public EnumDataWithDescription(string name, T value, string description)
            {
                Name = name;
                Value = value;
                Description = description;
            }
            public void Deconstruct(out string Name, out T Value, out string Description)
            {
                Name = this.Name;
                Value = this.Value;
                Description = this.Description;
            }
        }
        public static IEnumerable<EnumDataWithDescription<TEnum>> GetEnumDatasWithDescription<TEnum>() where TEnum : struct, Enum
        => from TEnum Value in Enum.GetValues<TEnum>()
           select new EnumDataWithDescription<TEnum>(Value.ToString(), Value, Value.GetDescription());

    }
}

using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Lytec.Common.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Lytec.Common.Localization
{
    public interface ILocalization : DI.IService
    {
        /// <summary>
        /// 查询指定键名的本地化字符串
        /// </summary>
        string Q(string key, string defaultValue);
    }

    public class Localization : ILocalization
    {
        protected virtual ILocalizationDataSource DataSource { get; set; }

        public Localization(ILocalizationDataSource dataSource)
        {
            DataSource = dataSource;
        }

        public virtual string Q(string key, string defaultValue)
        {
            var ori = defaultValue;
            if (DataSource != null && DataSource.TryGetValue(key, out var v))
                ori = v;
            var sb = new StringBuilder(Regex.Unescape(ori));
            sb.Replace("\r\n", "\n");
            sb.Replace("\n\r", "\n");
            sb.Replace("\r", "\n");
            sb.Replace("\n", Environment.NewLine);
            return sb.ToString();
        }

        /// <summary>
        /// 查询指定键名的本地化字符串
        /// </summary>
        public virtual string Q(string key) => Q(key, key);
        /// <summary>
        /// 查询指定键名的本地化字符串
        /// </summary>
        public virtual string Q(string key, string defaultValue, params (string Placeholder, object Value)[] args)
        {
            var template = Q(key, defaultValue);
            if (string.IsNullOrEmpty(template))
                return template;
            var sb = new StringBuilder(template);
            foreach (var (p, v) in args)
                sb.Replace(p, v?.ToString() ?? "");
            return sb.ToString();
        }

        /// <summary>
        /// 查询指定枚举值的本地化字符串
        /// </summary>
        public virtual string Q<T>(T value, string? defaultValue = null) where T : Enum
        {
            var path = $"{value.GetType().GetNestedClassName()}.{value}";
            return Q(path, defaultValue ?? path);
        }

    }

}

namespace Lytec.Common.Localization.Extensions
{
    public static class LocalizationUtils
    {
        public static void AddI18N(this IServiceCollection collection) => collection.AddSingleton<ILocalization, Localization>();
    }
}

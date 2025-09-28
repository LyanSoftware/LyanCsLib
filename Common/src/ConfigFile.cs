using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lytec.Common
{
    /// <summary>
    /// 配置文件
    /// </summary>
    /// <typeparam name="TImpl">需要使用子类本身</typeparam>
    [JsonObject]
    public class ConfigFile<TImpl> where TImpl : ConfigFile<TImpl>, new()
    {
        public static Encoding DefaultEncode { get; set; } = Encoding.UTF8;
        [JsonIgnore]
        public virtual Encoding Encode { get; protected set; } = Encoding.UTF8;

        public static CultureInfo DefaultLanguage { get; set; } = Thread.CurrentThread.CurrentCulture;
        public static CultureInfo BuiltInLanguage { get; set; } = CultureInfo.GetCultureInfo("en");

        public class LanguageIds
        {
            public const string Auto = nameof(Auto);
            public const string Default = nameof(Default);
            public const string BuiltIn = "Built-In";
        }

        public static TImpl? Current { get; set; }

        /// <summary>
        /// 语言
        /// </summary>
        [DefaultValue(LanguageIds.Auto)]
        public virtual string Language
        {
            get => CultureInfo == null ? LanguageIds.Auto : (CultureInfo.Name == BuiltInLanguage.Name ? LanguageIds.BuiltIn : CultureInfo.Name);
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var lo = value.ToLower();
                    switch (lo)
                    {
                        case var _ when lo == LanguageIds.Auto.ToLower():
                        case var _ when lo == LanguageIds.Default.ToLower():
                            CultureInfo = null;
                            break;
                        case var _ when lo == LanguageIds.BuiltIn.ToLower():
                            CultureInfo = BuiltInLanguage;
                            break;
                        default:
                            try
                            {
                                CultureInfo = CultureInfo.GetCultureInfo(value);
                            }
                            catch (Exception)
                            {
                                CultureInfo = DefaultLanguage;
                            }
                            break;
                    }
                }
                else CultureInfo = DefaultLanguage;
            }
        }

        [JsonIgnore]
        public CultureInfo? CultureInfo
        {
            get => _CultureInfo;
            set
            {
                _CultureInfo = value;
                value = value ?? DefaultLanguage;
                CultureInfo.DefaultThreadCurrentCulture = value;
                CultureInfo.DefaultThreadCurrentUICulture = value;
                Thread.CurrentThread.CurrentCulture = value;
                Thread.CurrentThread.CurrentUICulture = value;
                OnLanguageChanged(value);
            }
        }
        private CultureInfo? _CultureInfo = null;

        public event Action<CultureInfo>? LanguageChanged;

        protected virtual void OnLanguageChanged(CultureInfo info) => LanguageChanged?.Invoke(info);

        /// <summary>
        /// 日志级别
        /// </summary>
        [JsonProperty]
        [DefaultValue(typeof(LogLevel), nameof(LogLevel.Information))]
        [JsonConverter(typeof(StringEnumConverter))]
        public virtual LogLevel LogLevel
        {
            get => _LogLevel;
            set
            {
                _LogLevel = value;
                OnLogLevelChanged(value);
            }
        }
        private LogLevel _LogLevel = LogLevel.Information;


        public event Action<LogLevel>? LogLevelChanged;
        public static event Action<ConfigFile<TImpl>, LogLevel>? LogLevelChangedStatic;

        protected virtual void OnLogLevelChanged(LogLevel value)
        {
            LogLevelChanged?.Invoke(value);
            LogLevelChangedStatic?.Invoke(this, value);
        }

        /// <summary>
        /// 日志保留天数
        /// </summary>
        public virtual uint LogRetentionDays { get; set; } = 30;

        [JsonIgnore]
        public string? FilePath { get; set; }

        public ConfigFile(string? filepath = null)
        {
            FilePath = filepath;
            Serialize = indented => JsonConvert.SerializeObject(this, indented ? Formatting.Indented : Formatting.None);
        }

        /// <summary>
        /// param: indented
        /// </summary>
        [JsonIgnore]
        public Func<bool, string> Serialize { get; set; }

        [JsonIgnore]
        public static Func<string, TImpl?> Deserialize { get; set; } = data => JsonConvert.DeserializeObject<TImpl>(data);

        public string SaveToMemory(bool indented = true) => Serialize(indented);

        public static TImpl? LoadFromMemory(string data) => Deserialize(data);

        public virtual void Save(string? filepath = null, bool indented = true)
        => File.WriteAllText(filepath ?? FilePath ?? throw new ArgumentException(), SaveToMemory(indented), Encode);

        public static TImpl Load(string filepath, Encoding? encode = default)
        {
            var conf = File.Exists(filepath) ? LoadFromMemory(File.ReadAllText(filepath, encode ?? DefaultEncode)) ?? new() : new();
            conf.FilePath = filepath;
            return conf;
        }

        public bool EqualsEx(ConfigFile<TImpl> other)
        => other != null && FilePath == other.FilePath && SaveToMemory(false) == other.SaveToMemory(false);
    }
}

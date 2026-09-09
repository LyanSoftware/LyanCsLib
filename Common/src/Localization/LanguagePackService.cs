using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Immutable;
using System.Globalization;

namespace Lytec.Common.Localization;

public interface ILanguagePackService
{
    event EventHandler? CurrentLanguageChanged;

    CultureInfo StartupCulture { get; }

    string CurrentLanguageId { get; }

    string LanguageDirectory { get; }

    IReadOnlyList<LanguagePackInfo> AvailableLanguages { get; }

    Task<IReadOnlyList<LanguagePackInfo>> InitializeAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LanguagePackInfo>> DiscoverAsync(
        CancellationToken cancellationToken = default);

    Task<bool> ChangeLanguageAsync(
        string languageId,
        CancellationToken cancellationToken = default);
}

public sealed class JsonLanguagePackService(
    JsonLocalizer localizer,
    ILanguagePreferenceStore? preferenceStore = null,
    ILocalizationLogSink? log = null,
    string? languageDirectory = null,
    CultureInfo? startupCulture = null,
    ILanguagePackSource? languagePackSource = null) : ILanguagePackService
{
    private const string JsonSuffix = ".json";
    private const string OverrideSuffix = ".override.json";
    private const string LocalizeScope = "Lytec.Common.Localization.JsonLanguagePackService";
    private static LocalizeString Localize(string Key, object? Arguments = null, string? DefaultMessage = null)
    => new(LocalizeScope, Key, Arguments, DefaultMessage);

    private readonly JsonLocalizer localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    private readonly ILanguagePreferenceStore preferenceStore = preferenceStore ?? new NullLanguagePreferenceStore();
    private readonly ILocalizationLogSink log = log ?? new DebugLocalizationLogSink(localizer);
    private readonly SynchronizationContext? notificationContext = SynchronizationContext.Current;
    private readonly SemaphoreSlim saveGate = new(1, 1);

    private ImmutableDictionary<string, LanguagePack> packs =
        ImmutableDictionary.Create<string, LanguagePack>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<LanguagePackInfo> availableLanguages = [];
    private long changeVersion;

    public event EventHandler? CurrentLanguageChanged;

    public CultureInfo StartupCulture { get; } = startupCulture ?? CultureInfo.CurrentUICulture;

    public string CurrentLanguageId { get; private set; } = LanguageId.Auto;

    public string LanguageDirectory { get; } = Path.GetFullPath(languageDirectory ?? Path.Combine(AppContext.BaseDirectory, "lang"));

    /// <summary>
    /// Gets the source used to discover and open language packs.
    /// </summary>
    public ILanguagePackSource LanguagePackSource { get; } = languagePackSource
        ?? new DirectoryLanguagePackSource(languageDirectory);

    public IReadOnlyList<LanguagePackInfo> AvailableLanguages => availableLanguages;

    public async Task<IReadOnlyList<LanguagePackInfo>> InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        var discovered = await DiscoverAsync(cancellationToken);

        string? requestedLanguage;
        try
        {
            requestedLanguage = await preferenceStore.LoadAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            log.Write(
                LocalizationLogLevel.Warning,
                Localize("PreferenceLoadFailed", DefaultMessage: "读取语言偏好设置失败，将使用自动语言。"),
                ex);
            requestedLanguage = LanguageId.Auto;
        }

        var normalized = NormalizeSelectableLanguageId(requestedLanguage);
        await ChangeLanguageCoreAsync(normalized, persist: false, cancellationToken);
        return discovered;
    }

    public async Task<IReadOnlyList<LanguagePackInfo>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        DiscoveryResult result;
        try
        {
            result = await Task.Run(
                () => DiscoverCore(cancellationToken),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            log.Write(
                LocalizationLogLevel.Error,
                Localize(
                    "DirectoryScanFailed",
                    new { Directory = LanguagePackSource.Description },
                    "读取语言包来源“{Directory}”失败，将继续使用程序内嵌文本。"),
                ex);
            result = new DiscoveryResult(
                ImmutableDictionary.Create<string, LanguagePack>(StringComparer.OrdinalIgnoreCase),
                []);
        }

        packs = result.Packs;
        availableLanguages = result.Languages;
        return availableLanguages;
    }

    public Task<bool> ChangeLanguageAsync(
        string languageId,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeSelectableLanguageId(languageId);
        if (!string.Equals(normalized, languageId, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(languageId, LanguageId.Auto, StringComparison.OrdinalIgnoreCase))
        {
            log.Write(
                LocalizationLogLevel.Warning,
                Localize(
                    "LanguageNotAvailable",
                    new { LanguageId = languageId },
                    "语言“{LanguageId}”不可用，将保持当前语言。"));
            return Task.FromResult(false);
        }

        return ChangeLanguageCoreAsync(normalized, persist: true, cancellationToken);
    }

    private async Task<bool> ChangeLanguageCoreAsync(
        string languageId,
        bool persist,
        CancellationToken cancellationToken)
    {
        var requestVersion = Interlocked.Increment(ref changeVersion);
        var targetCulture = string.Equals(languageId, LanguageId.Auto, StringComparison.OrdinalIgnoreCase)
            ? StartupCulture
            : CultureInfo.GetCultureInfo(languageId);

        // Keep this operation asynchronous even when every pack is already cached. It
        // gives a later UI selection a chance to supersede this request before commit.
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        var layers = BuildLayers(targetCulture);
        if (requestVersion != Interlocked.Read(ref changeVersion))
            return false;

        localizer.Apply(targetCulture, layers);
        CurrentLanguageId = languageId;
        NotifyCurrentLanguageChanged();

        if (persist)
            await SavePreferenceIfCurrentAsync(languageId, requestVersion, cancellationToken);

        return requestVersion == Interlocked.Read(ref changeVersion);
    }

    private ImmutableArray<ImmutableDictionary<string, string>> BuildLayers(CultureInfo targetCulture)
    {
        var layers = ImmutableArray.CreateBuilder<ImmutableDictionary<string, string>>();
        foreach (var culture in BuildFallbackCultures(targetCulture))
        {
            if (packs.TryGetValue(culture.Name, out var pack))
            {
                layers.Add(pack.Data);
                continue;
            }

            log.Write(
                LocalizationLogLevel.Warning,
                Localize(
                    "FallbackPackMissing",
                    new { Culture = culture.Name },
                    "回退链中的语言包“{Culture}”不存在，将继续使用下一层。"));
        }

        return layers.ToImmutable();
    }

    private static IEnumerable<CultureInfo> BuildFallbackCultures(CultureInfo targetCulture)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var culture = targetCulture;
             culture != CultureInfo.InvariantCulture
             && !string.IsNullOrWhiteSpace(culture.Name)
             && seen.Add(culture.Name);
             culture = culture.Parent)
        {
            yield return culture;
        }

        var english = CultureInfo.GetCultureInfo("en");
        if (seen.Add(english.Name))
            yield return english;
    }

    private async Task SavePreferenceIfCurrentAsync(
        string languageId,
        long requestVersion,
        CancellationToken cancellationToken)
    {
        await saveGate.WaitAsync(cancellationToken);
        try
        {
            if (requestVersion != Interlocked.Read(ref changeVersion))
                return;

            try
            {
                await preferenceStore.SaveAsync(languageId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                log.Write(
                    LocalizationLogLevel.Warning,
                    Localize(
                        "PreferenceSaveFailed",
                        new { LanguageId = languageId },
                        "保存语言偏好“{LanguageId}”失败。"),
                    ex);
            }
        }
        finally
        {
            saveGate.Release();
        }
    }

    private string NormalizeSelectableLanguageId(string? languageId)
    {
        if (string.IsNullOrWhiteSpace(languageId)
            || string.Equals(languageId, LanguageId.Auto, StringComparison.OrdinalIgnoreCase))
            return LanguageId.Auto;

        try
        {
            var culture = CultureInfo.GetCultureInfo(languageId);
            return packs.ContainsKey(culture.Name) ? culture.Name : LanguageId.Auto;
        }
        catch (CultureNotFoundException)
        {
            return LanguageId.Auto;
        }
    }

    private DiscoveryResult DiscoverCore(CancellationToken cancellationToken)
    {
        var resources = LanguagePackSource.Enumerate(cancellationToken)
            .Where(static resource => resource.FileName.EndsWith(JsonSuffix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(static resource => resource.FileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var result = new Dictionary<string, LanguagePack>(StringComparer.OrdinalIgnoreCase);

        void proc(IEnumerable<LanguagePackResource> res, bool isOverride)
        {
            foreach (var r in res)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CultureInfo culture;
                try
                {
                    var cultureName = isOverride
                        ? r.FileName[..^OverrideSuffix.Length]
                        : r.FileName[..^JsonSuffix.Length];

                    culture = CultureInfo.GetCultureInfo(cultureName);
                    if (culture == CultureInfo.InvariantCulture || string.IsNullOrWhiteSpace(culture.Name))
                        throw new CultureNotFoundException(nameof(r.FileName), cultureName, "Invariant culture is not a language pack ID.");
                }
                catch (CultureNotFoundException ex)
                {
                    log.Write(
                        LocalizationLogLevel.Warning,
                        Localize(
                            "InvalidFileName",
                            new { FileName = r.FileName },
                            "语言包文件名“{FileName}”不是有效的 CultureInfo 名称，已跳过。"),
                        ex);
                    continue;
                }

                try
                {
                    using var stream = r.OpenRead();
                    ImmutableDictionary<string, string> data;
                    if (isOverride)
                    {
                        var overrides = ReadOverridePack(stream);
                        data = result.TryGetValue(culture.Name, out var pack)
                            ? pack.Data.SetItems(overrides)
                            : overrides;
                    }
                    else data = ReadBasePack(stream);
                    result[culture.Name] = new LanguagePack(culture, data);
                }
                catch (Exception ex) when (ex is IOException
                                           or UnauthorizedAccessException
                                           or JsonException
                                           or InvalidDataException)
                {
                    log.Write(
                        LocalizationLogLevel.Warning,
                        Localize(
                            "InvalidContent",
                            new { FileName = r.FileName },
                            "语言包“{FileName}”无法读取或内容无效，已跳过。"),
                        ex);
                }
            }
        }
        static bool isOverride(LanguagePackResource r) => r.FileName.EndsWith(OverrideSuffix, StringComparison.OrdinalIgnoreCase);
        proc(resources.Where(x => !isOverride(x)), false);
        proc(resources.Where(isOverride), true);

        var packs = result.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
        var languages = packs.Values
            .Select(static pack => new LanguagePackInfo(pack.Culture))
            .OrderBy(static info => info.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        return new DiscoveryResult(packs, languages);
    }

    private static ImmutableDictionary<string, string> ReadBasePack(Stream stream)
    {
        var root = ReadRoot(stream);
        var data = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach (var scope in root.Properties())
        {
            if (string.IsNullOrWhiteSpace(scope.Name) || scope.Value is not JObject entries)
                throw new InvalidDataException("Every scope must have a non-empty name and an object value.");

            foreach (var entry in entries.Properties())
            {
                if (string.IsNullOrWhiteSpace(entry.Name))
                    throw new InvalidDataException("Every localization key must have a non-empty name.");

                if (entry.Value.Type is JTokenType.Null or JTokenType.Undefined)
                    continue;

                if (entry.Value.Type != JTokenType.String)
                    throw new InvalidDataException("Every localization value must be a string or null.");

                data[Localizer.CombineScopeAndKey(scope.Name, entry.Name)] = entry.Value.Value<string>()!;
            }
        }

        return data.ToImmutable();
    }

    private static ImmutableDictionary<string, string> ReadOverridePack(Stream stream)
    {
        var root = ReadRoot(stream);
        var data = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach (var scope in root.Properties())
        {
            if (string.IsNullOrWhiteSpace(scope.Name) || scope.Value is not JObject entries)
                continue;

            foreach (var entry in entries.Properties())
            {
                if (string.IsNullOrWhiteSpace(entry.Name)
                    || !TryConvertOverrideValue(entry.Value, out var value))
                    continue;

                data[Localizer.CombineScopeAndKey(scope.Name, entry.Name)] = value;
            }
        }

        return data.ToImmutable();
    }

    private static bool TryConvertOverrideValue(JToken token, out string value)
    {
        if (token is JValue scalar && scalar.Value is not null)
        {
            try
            {
                value = Convert.ToString(scalar.Value, CultureInfo.InvariantCulture) ?? string.Empty;
                return true;
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException)
            {
            }
        }

        value = string.Empty;
        return false;
    }

    private static JObject ReadRoot(Stream stream)
    {
        using var textReader = new StreamReader(stream);
        using var jsonReader = new JsonTextReader(textReader)
        {
            DateParseHandling = DateParseHandling.None,
        };
        var root = JObject.Load(jsonReader, new JsonLoadSettings
        {
            CommentHandling = CommentHandling.Ignore,
            DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Replace,
        });

        while (jsonReader.Read())
        {
            if (jsonReader.TokenType != JsonToken.Comment)
                throw new InvalidDataException("A language pack must contain exactly one root object.");
        }

        return root;
    }

    private void NotifyCurrentLanguageChanged()
    {
        var handler = CurrentLanguageChanged;
        if (handler is null)
            return;

        if (notificationContext is null || ReferenceEquals(notificationContext, SynchronizationContext.Current))
            handler(this, EventArgs.Empty);
        else
            notificationContext.Send(static state =>
            {
                var args = ((JsonLanguagePackService Service, EventHandler Handler))state!;
                args.Handler(args.Service, EventArgs.Empty);
            }, (this, handler));
    }

    private sealed class LanguagePack(
        CultureInfo culture,
        ImmutableDictionary<string, string> data)
    {
        public CultureInfo Culture { get; } = culture;
        public ImmutableDictionary<string, string> Data { get; } = data;
    }

    private sealed class DiscoveryResult(
        ImmutableDictionary<string, LanguagePack> packs,
        IReadOnlyList<LanguagePackInfo> languages)
    {
        public ImmutableDictionary<string, LanguagePack> Packs { get; } = packs;
        public IReadOnlyList<LanguagePackInfo> Languages { get; } = languages;
    }
}

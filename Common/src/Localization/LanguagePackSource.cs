using System.Reflection;

namespace Lytec.Common.Localization;

/// <summary>
/// Enumerates named JSON language-pack resources and opens them as streams.
/// </summary>
public interface ILanguagePackSource
{
    /// <summary>
    /// Gets a human-readable description used for diagnostics.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Enumerates the available language-pack resources.
    /// </summary>
    IEnumerable<LanguagePackResource> Enumerate(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Describes one named language-pack resource.
/// </summary>
public record LanguagePackResource
{
    /// <summary>
    /// Gets the source file name used to identify its culture and role.
    /// </summary>
    public string FileName { get; }

    private Func<Stream> OpenReadFunc { get; }

    /// <summary>
    /// Initializes a language-pack resource.
    /// </summary>
    public LanguagePackResource(string fileName, Func<Stream> openRead)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("A language-pack file name is required.", nameof(fileName));

        FileName = fileName;
        OpenReadFunc = openRead ?? throw new ArgumentNullException(nameof(openRead));
    }

    /// <summary>
    /// Opens a readable stream for the resource.
    /// </summary>
    public Stream OpenRead()
        => OpenReadFunc() ?? throw new InvalidOperationException(
            $"The language-pack resource '{FileName}' returned no stream.");
}

/// <summary>
/// Reads language packs from a file-system directory.
/// </summary>
public sealed class DirectoryLanguagePackSource : ILanguagePackSource
{
    /// <summary>
    /// Initializes a directory source. A null directory uses
    /// <c>AppContext.BaseDirectory/lang</c>.
    /// </summary>
    public DirectoryLanguagePackSource(string? directory = null)
    {
        Directory = Path.GetFullPath(directory ?? Path.Combine(AppContext.BaseDirectory, "lang"));
    }

    /// <summary>
    /// Gets the absolute language-pack directory.
    /// </summary>
    public string Directory { get; }

    /// <inheritdoc />
    public string Description => Directory;

    /// <inheritdoc />
    public IEnumerable<LanguagePackResource> Enumerate(
        CancellationToken cancellationToken = default)
    {
        if (!System.IO.Directory.Exists(Directory))
            yield break;

        var files = System.IO.Directory.EnumerateFiles(Directory, "*", SearchOption.TopDirectoryOnly)
            .Where(static path => string.Equals(
                Path.GetExtension(path),
                ".json",
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase);

        foreach (var path in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new LanguagePackResource(
                Path.GetFileName(path),
                () => File.OpenRead(path));
        }
    }
}

/// <summary>
/// Reads language packs from manifest resources in an assembly.
/// </summary>
public sealed class EmbeddedResourceLanguagePackSource : ILanguagePackSource
{
    private readonly Assembly assembly;
    private readonly string resourcePrefix;

    /// <summary>
    /// Initializes an embedded source using the specified manifest-resource prefix.
    /// </summary>
    public EmbeddedResourceLanguagePackSource(Assembly assembly, string resourcePrefix)
    {
        this.assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        if (string.IsNullOrWhiteSpace(resourcePrefix))
            throw new ArgumentException("An embedded-resource prefix is required.", nameof(resourcePrefix));

        this.resourcePrefix = resourcePrefix;
    }

    /// <inheritdoc />
    public string Description => $"{assembly.GetName().Name}:{resourcePrefix}";

    /// <inheritdoc />
    public IEnumerable<LanguagePackResource> Enumerate(
        CancellationToken cancellationToken = default)
    {
        var names = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(resourcePrefix, StringComparison.Ordinal))
            .Select(name => new
            {
                ResourceName = name,
                FileName = name.Substring(resourcePrefix.Length),
            })
            .Where(static item => string.Equals(
                Path.GetExtension(item.FileName),
                ".json",
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(static item => item.FileName, StringComparer.OrdinalIgnoreCase);

        foreach (var item in names)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new LanguagePackResource(
                item.FileName,
                () => assembly.GetManifestResourceStream(item.ResourceName)
                    ?? throw new FileNotFoundException(
                        $"Embedded language-pack resource '{item.ResourceName}' was not found.",
                        item.ResourceName));
        }
    }
}

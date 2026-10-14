namespace Lytec.Image;

public sealed record ModifiedCharImage(int Char, byte[] ImageData);

public interface IModifiedCharReader
{
    Task<IReadOnlyList<ModifiedCharImage>> ReadAsync(
        FontInfo font,
        IReadOnlyCollection<int> characters,
        CancellationToken cancellationToken = default);
}

public sealed class InMemoryModifiedCharReader : IModifiedCharReader
{
    public static InMemoryModifiedCharReader Instance { get; } = new();

    private InMemoryModifiedCharReader()
    {
    }

    public Task<IReadOnlyList<ModifiedCharImage>> ReadAsync(
        FontInfo font,
        IReadOnlyCollection<int> characters,
        CancellationToken cancellationToken = default)
    {
        if (font == null)
            throw new ArgumentNullException(nameof(font));
        if (characters == null)
            throw new ArgumentNullException(nameof(characters));

        cancellationToken.ThrowIfCancellationRequested();
        if (characters.Count == 0)
            return Task.FromResult<IReadOnlyList<ModifiedCharImage>>([]);

        var characterSet = characters as ISet<int> ?? new HashSet<int>(characters);
        var result = new List<ModifiedCharImage>();
        foreach (var image in font.ModifiedChars)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (characterSet.Contains(image.Char))
                result.Add(new ModifiedCharImage(image.Char, image.ImageData));
        }

        return Task.FromResult<IReadOnlyList<ModifiedCharImage>>(result);
    }
}

using System.Text;
using Lytec.Image;
using Lytec.Protocol.Images.SCL;
using SkiaSharp;
using Xunit;

namespace Test.Protocol.Images;

public sealed class FontLibUtilsTest
{
    [Fact]
    public async Task InMemoryReader_ReturnsOnlyRequestedCharacters()
    {
        var font = new FontInfo { Id = 7 };
        font.ModifiedChars.Add(new CharImg(font, 1, [1]) { FontId = font.Id });
        font.ModifiedChars.Add(new CharImg(font, 2, [2]) { FontId = font.Id });

        var images = await InMemoryModifiedCharReader.Instance.ReadAsync(
            font,
            new[] { 2, 3 },
            TestContext.Current.CancellationToken);

        var image = Assert.Single(images);
        Assert.Equal(2, image.Char);
        Assert.Equal(new byte[] { 2 }, image.ImageData);
    }

    [Fact]
    public async Task ExportSBCSAsync_QueriesAndRendersInConfiguredBatches()
    {
        var reader = new RecordingReader(CreateWhiteBitmapData());
        var font = CreateFont();

        var result = await font.ExportSBCSAsync(
            reader,
            Encoding.Latin1,
            codeEnd: 3,
            batchSize: 2,
            maxThreads: 2,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, reader.Batches.Count);
        Assert.Equal(new[] { 0, 1 }, reader.Batches[0]);
        Assert.Equal(new[] { 2, 3 }, reader.Batches[1]);
        Assert.Equal(4, result.CharCount);
        Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, result.Data);
    }

    [Fact]
    public async Task ExportDBCSAsync_LimitsEachQueryAndKeepsBlockMetadataAccurate()
    {
        var reader = new RecordingReader(CreateWhiteBitmapData());
        var font = CreateFont();
        var results = new List<FontLib>();

        await foreach (var result in font.ExportDBCSAsync(
            reader,
            new FontLibUtils.ExportDBCSArgs(Encoding.Latin1, 1, 2, 0xFE),
            batchSize: 3,
            maxThreads: 2,
            cancellationToken: TestContext.Current.CancellationToken))
        {
            results.Add(result);
        }

        var fontLib = Assert.Single(results);
        Assert.Equal(2, reader.Batches.Count);
        Assert.Equal(3, reader.Batches[0].Length);
        Assert.Single(reader.Batches[1]);
        Assert.Equal(4, fontLib.CharCount);
        Assert.Equal((byte)1, fontLib.ExportOptions.Byte1Start);
        Assert.Equal((byte)2, fontLib.ExportOptions.Byte1End);
        Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, fontLib.Data);
    }

    private static FontInfo CreateFont()
    => new()
    {
        Id = 7,
        Width = 1,
        Height = 8,
    };

    private static byte[] CreateWhiteBitmapData()
    {
        using var bitmap = new SKBitmap(1, 8);
        bitmap.Erase(SKColors.White);
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private sealed class RecordingReader(byte[] imageData) : IModifiedCharReader
    {
        public List<int[]> Batches { get; } = [];

        public Task<IReadOnlyList<ModifiedCharImage>> ReadAsync(
            FontInfo font,
            IReadOnlyCollection<int> characters,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = characters.ToArray();
            Batches.Add(batch);
            IReadOnlyList<ModifiedCharImage> result = batch
                .Select(character => new ModifiedCharImage(character, imageData))
                .ToArray();
            return Task.FromResult<IReadOnlyList<ModifiedCharImage>>(result);
        }
    }
}

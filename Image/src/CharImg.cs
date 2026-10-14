namespace Lytec.Image;

public class CharImg(FontInfo Font, int Char, byte[] ImageData)
{
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public long FontId { get; set; } = Font.Id;

    public FontInfo Font { get; set; } = Font;

    public int Char { get; set; } = Char;

    public byte[] ImageData { get; set; } = ImageData;
}

using Newtonsoft.Json;

namespace Lytec.Protocol.LiaoNingHighSpeedLedGB;

[Serializable]
[JsonObject]
public class Playlist
{
    public string fileName { get; set; } = "";
    public ProgramItem[] programs { get; set; } = Array.Empty<ProgramItem>();

    public IJsonData Serialize() => new JsonObj(this);

    public static bool TryDeserialize(IJsonData jsonData, out Playlist Playlist)
    {
        throw new NotImplementedException();
    }
}

[Serializable]
[JsonObject]
public class ProgramItem
{
    public string id { get; set; } = "";
    public string name { get; set; } = "";
    public Element[] elements { get; set; } = Array.Empty<Element>();

    public static bool TryDeserialize(IJsonData jsonData, out ProgramItem Item)
    {
        throw new NotImplementedException();
    }
}

[Serializable]
[JsonObject]
public class Element
{
    public string type { get; set; } = "";
    public string? alias { get; set; }
    public ElementData[] data { get; set; } = Array.Empty<ElementData>();
}

[Serializable]
[JsonObject]
public abstract class ElementData
{
    public int x { get; set; }
    public int y { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public int? stayTime { get; set; }

    public ElementData(int x, int y, int width, int height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }
}

[Serializable]
[JsonObject]
public class TextElement : ElementData
{
    public int font { get; set; }
    public int fontSize { get; set; }
    public int? fontStyle { get; set; }
    public string color { get; set; }
    public string backgroundColor { get; set; }
    public string content { get; set; }
    public int? space { get; set; }
    public int? lineSpace { get; set; }
    public int? arrangement { get; set; }
    public int? align { get; set; }
    public int? valign { get; set; }
    public int? effects { get; set; }
    public int? effectsSpeed { get; set; }

    public TextElement(
        int x, int y, int width, int height,
        int font,
        int fontSize,
        string color,
        string backgroundColor,
        string content
        )
        : base(x, y, width, height)
    {
        this.font = font;
        this.fontSize = fontSize;
        this.color = color;
        this.backgroundColor = backgroundColor;
        this.content = content;
    }
}

[Serializable]
[JsonObject]
public class ImageElement : ElementData
{
    public string fileName { get; set; }
    public string? imageData { get; set; }

    public ImageElement(int x, int y, int width, int height, string fileName)
        : base(x, y, width, height)
    {
        this.fileName = fileName;
    }

    public ImageElement(int x, int y, int width, int height, byte[] imageData)
        : base(x, y, width, height)
    {
        this.fileName = "";
        this.imageData = App.Base64Encode(imageData);
    }
}

[Serializable]
[JsonObject]
public class VideoElement : ElementData
{
    public string fileName { get; set; }

    public VideoElement(int x, int y, int width, int height, string fileName)
        : base(x, y, width, height)
    {
        this.fileName = fileName;
    }
}

[Serializable]
[JsonObject]
public class GifElement : ElementData
{
    public string fileName { get; set; }
    public string? gifData { get; set; }
    public string backgroundColor { get; set; }

    public GifElement(int x, int y, int width, int height, string fileName, string backgroundColor)
        : base(x, y, width, height)
    {
        this.fileName = fileName;
        this.backgroundColor = backgroundColor;
    }
    public GifElement(int x, int y, int width, int height, byte[] gifData, string backgroundColor)
        : base(x, y, width, height)
    {
        this.fileName = "";
        this.gifData = App.Base64Encode(gifData);
        this.backgroundColor = backgroundColor;
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Lytec.Common;
using Newtonsoft.Json;

namespace Lytec.Image;

public partial class ColorJsonConverter : JsonConverter<Color>
{
    public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        static Color conv(int v, int len)
        {
            switch (len)
            {
                case 3: // RGB -> FFRRGGBB
                    {
                        var r = (v >> 8) & 0xF;
                        var g = (v >> 4) & 0xF;
                        var b = v & 0xF;
                        return new Color((r << 4) | r, (g << 4) | g, (b << 4) | b, 0xFF);
                    }
                case 6: // RRGGBB -> FFRRGGBB
                    return new Color((v >> 16) & 0xFF, (v >> 8) & 0xFF, v & 0xFF);
                case 4: // ARGB -> AARRGGBB
                    {
                        var a = (v >> 12) & 0xF;
                        var r = (v >> 8) & 0xF;
                        var g = (v >> 4) & 0xF;
                        var b = v & 0xF;
                        return new Color((r << 4) | r, (g << 4) | g, (b << 4) | b, (a << 4) | a);
                    }
                case 8: // AARRGGBB
                    return new Color((v >> 16) & 0xFF, (v >> 8) & 0xFF, v & 0xFF, (v >> 24) & 0xFF);
            }
            throw new JsonSerializationException();
        }
        if (reader.Value is string str)
        {
            if (str.Length > 0 && str[0] == '#')
                str = str[1..];
            switch (str.Length)
            {
                case 3:
                case 4:
                case 6:
                case 8:
                    if (uint.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
                        return conv((int)v, str.Length);
                    break;
            }
        }
        throw new JsonSerializationException($"Invalid color value '{reader.Value}'. Expected #RGB, #ARGB, #RRGGBB or #AARRGGBB.");
    }

    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
    {
        writer.WriteValue($"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}");
    }
}

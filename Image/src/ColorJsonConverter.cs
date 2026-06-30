using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Lytec.Image;

public class ColorJsonConverter : JsonConverter<Color>
{
    public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        Color c(long v) => new Color((byte)(v >> 16), (byte)(v >> 8), (byte)v, (byte)(v >> 24));
        switch (reader.Value)
        {
            case string str:
                {
                    if (str.StartsWith("#"))
                        str = str[1..];
                    if (uint.TryParse(str, out var v))
                        return c(v);
                    break;
                }
            case long v:
                return c(v);
        }
        return Color.Magenta;
    }

    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
    {
        writer.WriteValue($"#{value:X8}");
    }
}

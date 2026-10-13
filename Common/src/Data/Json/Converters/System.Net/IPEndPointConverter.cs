using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lytec.Common.Data.Json.Converters
{
    public class IPEndPointConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IPEndPoint);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is IPEndPoint ep)
            {
                new JObject()
                {
                    { "Address", ep.Address.ToString() },
                    { "Port", ep.Port }
                }.WriteTo(writer);
            }
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (jo["Address"]?.Value<string>() is string addrstr
                && IPAddress.TryParse(addrstr, out var addr)
                && jo["Port"]?.Value<int>() is int port)
                return new IPEndPoint(addr, port);
            return null;
        }
    }
}

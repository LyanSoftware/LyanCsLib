using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace Lytec.Common.Data.Json.Converters
{
    public static class Extensions
    {
        public static JsonSerializerSettings AddDefaultJsonConverters(this JsonSerializerSettings settings)
        {
            settings.Converters.Add(new IPAddressConverter());
            settings.Converters.Add(new IPEndPointConverter());
            return settings;
        }
    }
}

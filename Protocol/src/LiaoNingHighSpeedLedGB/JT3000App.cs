using System;
using System.Collections.Generic;
using System.Text;
using Lytec.Common;
using Lytec.Common.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lytec.Protocol.LiaoNingHighSpeedLedGB
{
    public class JT3000App : App
    {
        public Task<Result<bool>> executeBadPointScan()
        {
            LogAction();
            return Request("/api/led/executeBadPointScan", true, _ => true);
        }

        [JsonObject]
        public class AutoRunBadPixCheckConfig
        {
            [JsonProperty("interval")]
            public int IntervalHours { get; set; }

            [JsonProperty("open")]
            public bool IsEnabled { get; set; }

            public AutoRunBadPixCheckConfig() { }
            public AutoRunBadPixCheckConfig(int intervalHours, bool isEnabled)
            {
                IntervalHours = intervalHours;
                IsEnabled = isEnabled;
            }
        }

        [JsonObject]
        public class TemperatureConfig
        {
            [JsonProperty("max")]
            public float Max { get; set; }

            [JsonProperty("min")]
            public float Min { get; set; }

            [JsonProperty("open")]
            public bool IsEnabled { get; set; }

            public TemperatureConfig() { }
            public TemperatureConfig(float max, float min, bool isEnabled)
            {
                Max = max;
                Min = min;
                IsEnabled = isEnabled;
            }
        }

        [JsonObject]
        public class ReceiveCardConfig
        {
            [JsonProperty("count")]
            public int Count { get; set; }

            [JsonProperty("open")]
            public bool IsEnabled { get; set; }
        }

        [JsonObject]
        public class CheckPortConfig
        {
            public bool j1 { get; set; }
            public bool j2 { get; set; }
            public bool j3 { get; set; }
            public bool j4 { get; set; }
            public bool j5 { get; set; }
            public bool j6 { get; set; }
            /// <summary>
            /// SHT40 温湿度传感器
            /// </summary>
            public bool u6 { get; set; }

            public CheckPortConfig() { }
            public CheckPortConfig(bool j1, bool j2, bool j3, bool j4, bool j5, bool j6, bool u6)
            {
                this.j1 = j1;
                this.j2 = j2;
                this.j3 = j3;
                this.j4 = j4;
                this.j5 = j5;
                this.j6 = j6;
                this.u6 = u6;
            }
        }

        [JsonObject]
        public class CheckConfig
        {
            [JsonProperty("badPointConfig")]
            public AutoRunBadPixCheckConfig AutoRunBadPixCheck { get; set; } = new();

            [JsonProperty("temperatureConfig")]
            public TemperatureConfig Temperature { get; set; } = new();

            [JsonProperty("receiveCardConfig")]
            public ReceiveCardConfig ReceiveCard { get; set; } = new();

            [JsonProperty("boardConfig")]
            public CheckPortConfig[][] AllCheckPortConfigs { get; set; } = Array.Empty<CheckPortConfig[]>();

            [JsonIgnore]
            public CheckPortConfig[] CheckPortConfigs
            {
                get => AllCheckPortConfigs != null && AllCheckPortConfigs.Length > 0 ? AllCheckPortConfigs[0] : Array.Empty<CheckPortConfig>();
                set
                {
                    if (AllCheckPortConfigs == null || AllCheckPortConfigs.Length == 0)
                        AllCheckPortConfigs = new CheckPortConfig[1][];
                    AllCheckPortConfigs[0] = value;
                }
            }

            public CheckConfig() { }
            public CheckConfig(AutoRunBadPixCheckConfig autoRunBadPixCheck, TemperatureConfig temperature, CheckPortConfig[] checkPortConfigs)
                : this(autoRunBadPixCheck, temperature, new CheckPortConfig[][] { checkPortConfigs }) { }
            public CheckConfig(AutoRunBadPixCheckConfig autoRunBadPixCheck, TemperatureConfig temperature, CheckPortConfig[][] checkPortConfigs)
            {
                AutoRunBadPixCheck = autoRunBadPixCheck;
                Temperature = temperature;
                AllCheckPortConfigs = checkPortConfigs;
            }
        }

        public Task<Result<CheckConfig>> getCheckConfig()
        {
            LogAction();
            return Request("/api/led/config", false, null, r =>
            {
                if (r.Query("data", JsonValueType.Object) is JObject obj
                    && JsonConvert.DeserializeObject<CheckConfig>(obj.ToString()) is CheckConfig cfg
                    )
                    return cfg;
                throw new InvalidDataException();
            });
        }

        public Task<Result<bool>> setCheckConfig(CheckConfig config)
        {
            LogAction();
            return Request("/api/led/config", true, new JsonObj(JObject.FromObject(config)), _ => true);
        }

        [JsonObject]
        public class TemperatureStatus
        {
            [JsonIgnore]
            public bool HasError { get; set; }
            [JsonProperty("status")]
            protected int IsOKValue { get => HasError ? 0 : 1; set => HasError = value != 0; }

            [JsonProperty("value")]
            public float Value { get; set; }
        }

        [JsonObject]
        public class VccPortStatus
        {
            public bool vcc0 { get; set; }
            public bool vcc1 { get; set; }
            public bool vcc2 { get; set; }
            public bool vcc3 { get; set; }
            public bool vcc4 { get; set; }
            public bool vcc5 { get; set; }
        }

        [JsonObject]
        public class RxCardStatus
        {
            [JsonIgnore]
            public float Humidity { get; set; }
            [JsonProperty("humidity")]
            public int HumidityValue { get => (int)(Humidity * 100); set => Humidity = value / 100f; }

            [JsonProperty("temperature")]
            public TemperatureStatus Temperature { get; set; } = new();

            [JsonIgnore]
            public bool IsTemperatureHasError => Temperature.HasError;
            [JsonIgnore]
            public float TemperatureValue => Temperature.Value;

            [JsonProperty("vcc")]
            public VccPortStatus[] _Vcc = Array.Empty<VccPortStatus>();
            public VccPortStatus? Vcc => _Vcc.Length > 0 ? _Vcc[0] : null;

            [JsonIgnore]
            public bool? IsJ1VccHigh => Vcc?.vcc0;
            [JsonIgnore]
            public bool? IsJ2VccHigh => Vcc?.vcc1;
            [JsonIgnore]
            public bool? IsJ3VccHigh => Vcc?.vcc2;
            [JsonIgnore]
            public bool? IsJ4VccHigh => Vcc?.vcc3;
            [JsonIgnore]
            public bool? IsJ5VccHigh => Vcc?.vcc4;

            [JsonIgnore]
            public bool IsJ6Error { get; set; }
            [JsonProperty("arrester")]
            protected int IsJ6ErrorValue { get => IsJ6Error ? 0 : 1; set => IsJ6Error = value != 0; }
        }

        [JsonObject]
        public class AllRxCardStatus
        {
            public int count { get; set; }

            public RxCardStatus[][] list { get; set; } = new RxCardStatus[0][];
        }

        public Task<Result<AllRxCardStatus>> getAllRxCardStatus()
        {
            LogAction();
            return Request("/api/led/allRxCard", false, r =>
            {
                if (r.Query("data", JsonValueType.Object) is JObject obj)
                {
                    var d = JsonConvert.DeserializeObject<AllRxCardStatus>(obj.ToString());
                    if (d != null)
                        return d;
                }
                throw new InvalidDataException();
            });
        }

        public async Task<Result<int>> getRxCardCount()
        {
            var r = await getAllRxCardStatus();
            if (r.Ok && r.Value != null)
                return Ok(r, r.Value.count);
            return Error(r, -1);
        }

        public async Task<Result<AutoRunBadPixCheckConfig>> getAutoRunBadPixCheckConfig()
        {
            var r = await getCheckConfig();
            if (r.Ok && r.Value != null)
                return Ok(r, r.Value.AutoRunBadPixCheck);
            return Error<AutoRunBadPixCheckConfig>(r);
        }

        public async Task<Result<bool>> setAutoRunBadPixCheckConfig(AutoRunBadPixCheckConfig cfg)
        => await setAutoRunBadPixCheckConfig(cfg.IsEnabled, cfg.IntervalHours);
        public async Task<Result<bool>> setAutoRunBadPixCheckConfig(bool enable, int intervalHours)
        {
            var r = await getCheckConfig();
            if (r.Ok && r.Value != null)
            {
                r.Value.AutoRunBadPixCheck.IsEnabled = enable;
                r.Value.AutoRunBadPixCheck.IntervalHours = intervalHours;
                return await setCheckConfig(r.Value);
            }
            return Error(r, false);
        }

        public async Task<Result<TemperatureConfig>> getCheckTemperatureConfig()
        {
            var r = await getCheckConfig();
            if (r.Ok && r.Value != null)
                return Ok(r, r.Value.Temperature);
            return Error<TemperatureConfig>(r);
        }

        public async Task<Result<bool>> setCheckTemperatureConfig(TemperatureConfig cfg)
        => await setCheckTemperatureConfig(cfg.IsEnabled, cfg.Min, cfg.Max);
        public async Task<Result<bool>> setCheckTemperatureConfig(bool enable, float min, float max)
        {
            var r = await getCheckConfig();
            if (r.Ok && r.Value != null)
            {
                r.Value.Temperature.IsEnabled = enable;
                r.Value.Temperature.Min = Math.Min(min, max);
                r.Value.Temperature.Max = Math.Max(min, max);
                return await setCheckConfig(r.Value);
            }
            return Error(r, false);
        }

        record BadPixelList(IReadOnlyList<(int X, int Y, int Color)> List, int Count, string Time);

        public async Task<Result<PixelErrorData>> getBadPixels()
        {
            var sz = await queryResolution();
            if (!sz.Ok || sz.Value == null)
                return Error(sz, (PixelErrorData?)null);
            LogAction();
            var r = await Request("/api/led/badPointRecord", false, r =>
            {
                if (r.Query("data.points", JsonValueType.Array) is JArray arr)
                {
                    var lst = new List<BadPixel>();
                    foreach (var el in arr)
                    {
                        if (el.SelectToken(JsonObj.FormatJsonPath("$.x")) is JValue xv && xv.Type == JTokenType.Integer && xv.Value is long x
                            && el.SelectToken(JsonObj.FormatJsonPath("$.y")) is JValue yv && yv.Type == JTokenType.Integer && yv.Value is long y
                            && el.SelectToken(JsonObj.FormatJsonPath("$.color")) is JValue cv && cv.Type == JTokenType.Integer && cv.Value is long c
                            )
                        {
                            var color = BadColor.None;
                            if (BitHelper.GetFlag(c, 0))
                                color |= BadColor.Red;
                            if (BitHelper.GetFlag(c, 1))
                                color |= BadColor.Green;
                            if (BitHelper.GetFlag(c, 2))
                                color |= BadColor.Blue;
                            lst.Add(new((int)x, (int)y, color));
                        }
                    }
                    var count = lst.Count;
                    if (r.Query("data.count", JsonValueType.Int) is int count1)
                        count = count1;
                    var time = "";
                    if (r.Query("data.stamp", JsonValueType.String) is string time1)
                        time = time1;
                    return new PixelErrorData(count, sz.Value, BadColor.Red | BadColor.Green | BadColor.Blue, lst, time);
                }
                throw new InvalidDataException();
            });
            if (r.Ok)
                return Ok(r, r.Value);
            return Error(r, r.Value);
        }

        public Task getRuntimeLogArchivedZip(Func<Stream, Task> onGetFile)
        {
            LogAction();
            return RequestStream("/file/runtimeLog", false, null, onGetFile);
        }
    }
}

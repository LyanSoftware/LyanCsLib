using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Lytec.Common;
using Lytec.Common.Crypto;
using Lytec.Common.Data;
using Lytec.Common.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1;

namespace Lytec.Protocol.LiaoNingHighSpeedLedGB;

public class Result<T> : JsonResult
{
#if NET
    [MemberNotNullWhen(true, nameof(Value))]
#endif
    public bool Ok { get; set; } = false;
    public T? Value { get; set; }
    public Result(JsonResult result) : base(result) { }
    public Result(JsonResult result, T value) : this(result)
    {
        Ok = true;
        Value = value;
    }
}

public class App
{
    public ILogger? Logger { get; set; }
    public IStringLocalizer? Localizer { get; set; }
    public string i18n(string key) => Localizer.Query(key);
    public void LogAction([CallerMemberName] string action = "") => Logger?.LogInformation($"[{action}]");

    public virtual byte[] SM3Encode(params byte[] data) => SM3.Compute(data);

    public virtual string SM3Encode(string data) => SM3Encode(Encoding.UTF8.GetBytes(data)).ToHex("").ToLower();

    public static string Base64Encode(params byte[] data) => Convert.ToBase64String(data);
    public static string Base64Encode(string data) => Base64Encode(Encoding.UTF8.GetBytes(data));
    public static bool Base64Decode(string str, out byte[] data)
    {
        try
        {
            data = Convert.FromBase64String(str);
            return true;
        }
        catch (Exception)
        {
            data = Array.Empty<byte>();
            return false;
        }
    }
    public static bool Base64DecodeToString(string str, out string data)
    {
        if (Base64Decode(str, out var d))
        {
            try
            {
                data = Encoding.UTF8.GetString(d);
                return true;
            }
            catch (Exception) { }
        }
        data = "";
        return false;
    }

    public JsonResult OkResult => new JsonResult(200, i18n("OK"), new JsonObj());
    public JsonResult ArgumentError => new JsonResult(10004, i18n("参数不正确"), new JsonObj());
    public JsonResult NotConnectedError => new JsonResult(10004, i18n("未连接到设备"), new JsonObj());
    public JsonResult UnauthorizedError => new JsonResult(10004, i18n("未登录"), new JsonObj());
    public JsonResult RequestError => new JsonResult(10004, i18n("请求出错"), new JsonObj());

    public HttpClient? Client { get; protected set; }
    public bool IsConnected => Client != null;
    public async Task<bool> Connect(string addr, int port, bool keepAlive = true)
    {
        if (IsConnected)
            return true;
        ServicePointManager.SetTcpKeepAlive(keepAlive, 30000, 30000);
        Client = new HttpClient(new WinHttpHandler())
        {
            BaseAddress = new UriBuilder("http", addr, port).Uri,
            Timeout = TimeSpan.FromSeconds(10),
        };
        LogAction();
        var r = await queryNonce();
        if (!r.Ok)
        {
            Logger?.LogError($"{i18n("获取随机数失败")}\r\n{i18n("响应数据：{Data}").Replace("{Data}", r.Data.ToString())}");
            await Disconnect();
        }
        else Logger?.LogDebug($"{i18n("连接成功")}");
        return r.Ok;
    }
    public async Task<JsonResult> Request(string url, bool usePost, IJsonData? parameters = null)
    {
        try
        {
            if (url.IsNullOrEmpty())
            {
                Logger?.LogError(i18n("Url不能为空"));
                return ArgumentError;
            }
            if (!IsConnected || Client == null)
            {
                Logger?.LogError(i18n("未连接到设备"));
                return NotConnectedError;
            }
            return await RequestStream(url, usePost, parameters, async s =>
            {
                using var ms = new MemoryStream();
                await s.CopyToAsync(ms);
                var jsonStr = Encoding.UTF8.GetString(ms.ToArray());
                var json = new JsonObj(JObject.Parse(jsonStr));
                if (json.Query("code", JsonValueType.Int) is int code)
                {
                    var msg = json.Query("msg", JsonValueType.String) is string msg1 ? msg1 : null;
                    Logger?.LogDebug($"{i18n("响应数据：{Data}").Replace("{Data}", jsonStr)}\r\n{i18n("解析结果：\r\n{Data}").Replace("{Data}", json.ToString(Formatting.Indented))}");
                    return new JsonResult(code, msg, json);
                }
                Logger?.LogError($"{i18n("响应数据格式错误")}\r\n{i18n("响应数据：{Data}").Replace("{Data}", jsonStr)}");
                return RequestError;
            });
        }
        catch (Exception err)
        {
            if (Logger != null)
            {
                if (err is TaskCanceledException)
                    err = new TimeoutException();
                var msg = $"{i18n("发送请求出错：{Message}").Replace("{Message}", err.Message)}";
                if (err.InnerException != null && !err.InnerException.Message.IsNullOrEmpty())
                    msg += $"\r\n{err.InnerException.Message}";
                {
                    var ie = err.GetInnerMessage();
                    if (!ie.IsNullOrEmpty() && ie != err.Message && ie != err.InnerException?.Message)
                        msg += $"\r\n{ie}";
                }
                msg += $"\r\n{err}";
                Logger.LogError(err, msg);

            }
            return RequestError;
        }
    }
    public Task RequestStream(string url, bool usePost, IJsonData? parameters, Func<Stream, Task> onGetStream)
    => RequestStream(url, usePost, parameters, async s =>
    {
        await onGetStream(s);
        return true;
    });
    public async Task<T> RequestStream<T>(string url, bool usePost, IJsonData? parameters, Func<Stream, Task<T>> onGetStream)
    {
        if (url.IsNullOrEmpty())
        {
            Logger?.LogError(i18n("Url不能为空"));
            throw new ArgumentException();
        }
        if (!IsConnected || Client == null)
        {
            Logger?.LogError(i18n("未连接到设备"));
            throw new InvalidOperationException();
        }
        var pa = parameters?.ToString() ?? "";
        using var content = new StringContent(pa, Encoding.UTF8, "application/json");
        if (!AuthToken.IsNullOrEmpty())
            content.Headers.Add("X-Token", AuthToken);
        var req = new HttpRequestMessage
        {
            Method = usePost ? HttpMethod.Post : HttpMethod.Get,
            RequestUri = new Uri(Client.BaseAddress, url),
            Content = content,
        };
        if (Logger != null)
        {
            var headers = content.Headers.SelectMany(kv => kv.Value.Select(v => $"{kv.Key}: {v}")).JoinToString("\r\n");
            var log1 = $"{i18n("发送{Method}请求至：\"{Url}\"").Replace("{Method}", req.Method.ToString()).Replace("{Url}", url)}\r\n[Header]\r\n{headers}";
            if (!pa.IsNullOrEmpty())
                log1 += $"\r\n[Body]\r\n{pa}";
            Logger.LogDebug(log1);
        }
        using var rep = await Client.SendAsync(req);
        var codeStr = i18n("响应代码：HTTP {Code} ({Message})")
            .Replace("{Code}", ((long)rep.StatusCode).ToString())
            .Replace("{Message}", rep.StatusCode.ToString());
        if (!rep.IsSuccessStatusCode)
        {
            Logger?.LogError($"{i18n("请求失败")}，{codeStr}");
            throw new HttpRequestException();
        }
        else Logger?.LogDebug($"{i18n("请求成功")}，{codeStr}");
        var stream = await rep.Content.ReadAsStreamAsync();
        return await onGetStream(stream);
    }

    public async Task Disconnect()
    {
        await Task.CompletedTask;
        if (!IsConnected)
            return;
        LogAction();
        var client = Client;
        Client = null;
        client?.Dispose();
    }

    public Result<T> Ok<T>(JsonResult result, T value) => new(result, value);
    public Result<T> Ok<T>(T value) => new(OkResult, value);
    public Result<T> Error<T>(JsonResult result, T value) => new(result, value) { Ok = false };
    public Result<T> Error<T>(JsonResult result) => new(result);

    public async Task<Result<T>> Request<T>(string url, bool usePost, IJsonData? parameters, Func<JsonResult, T> procAnswer)
    {
        if (!IsConnected)
            return Error<T>(NotConnectedError);
        if (!IsLogged)
            return Error<T>(UnauthorizedError);
        var result = await Request(url, usePost, parameters);
        if (result.Code == 200)
        {
            try
            {
                return Ok(result, procAnswer(result));
            }
            catch (Exception) { }
        }
        return Error<T>(result);
    }
    public Task<Result<T>> Request<T>(string url, bool usePost, Func<JsonResult, T> procAnswer) => Request(url, usePost, null, procAnswer);

    public string AppId { get; set; } = "";
    public string Secret { get; set; } = "";
    public string AuthToken { get; set; } = "";
    public bool IsLogged => IsConnected && !AuthToken.IsNullOrEmpty();

    public async Task<Result<string>> queryNonce()
    {
        if (!IsConnected)
            return Error<string>(NotConnectedError);
        LogAction();
        var result = await Request("/api/led/queryNonce", false);
        if (result.Code == 200 && result.Query("data.nonce", JsonValueType.String) is string nonce)
            return Ok(result, nonce);
        return Error<string>(result);
    }

    public async Task<Result<bool>> login(string appid, string nonce, string secret)
    {
        if (!IsConnected)
            return Error(NotConnectedError, false);
        if (IsLogged)
            return Ok(true);
        LogAction();
        var result = await Request("/api/led/login", true, new JsonObj(
            ("appId", appid),
            ("nonce", nonce),
            ("sign", SM3Encode($"{nonce}:{SM3Encode($"{appid}:{SM3Encode(secret)}")}"))
            ));
        if (result.Code != 200 || result.Query("data.token", JsonValueType.String) is not string token)
            return Error(result, false);
        AppId = appid;
        Secret = secret;
        AuthToken = token;
        return Ok(result, true);
    }

    public async Task<Result<bool>> login(string appid, string secret)
    {
        if (!IsConnected)
            return Error(NotConnectedError, false);
        if (IsLogged)
            return Ok(true);
        var nonce = await queryNonce();
        return nonce.Ok ? await login(appid, nonce.Value!, secret) : Error(nonce, false);
    }

    public async Task logout()
    {
        await Task.CompletedTask;
        LogAction();
        AuthToken = "";
    }

    public async Task<Result<bool>> relogin()
    {
        if (AppId.IsNullOrEmpty() || Secret.IsNullOrEmpty())
            throw new ArgumentException();
        await logout();
        return await login(AppId, Secret);
    }

    public record Capabilities(SupportedFunction[] Functions, MediaType[] MediaTypes, string Version);

    public static readonly IReadOnlyDictionary<int, SupportedFunction> Functions
    = Enum.GetValues(typeof(SupportedFunction))
        .Cast<SupportedFunction>()
        .ToDictionary(v => (int)v);

    public static readonly IReadOnlyDictionary<string, MediaType> MediaTypes
    = Enum.GetValues(typeof(MediaType))
        .Cast<MediaType>()
        .ToDictionary(v => v.ToString().ToLower());

    public Task<Result<Capabilities>> queryCapabilities()
    {
        LogAction();
        return Request("/api/led/queryCapabilities", false, result =>
        {
            if (result.Query("data.functions", JsonValueType.String) is string funcsStr
                && result.Query("data.medias", JsonValueType.String) is string mediasStr
                && result.Query("data.version", JsonValueType.String) is string ver)
            {
                return new Capabilities(
                    funcsStr.Split(',')
                        .Select(f => int.TryParse(f, out var fi) ? fi : 0)
                        .Where(f => f > 0)
                        .OrderBy(f => f)
                        .Select(f => Functions.TryGetValue(f, out var fv) ? fv : 0)
                        .Where(f => f != 0)
                        .ToArray(),
                    mediasStr.Split(',')
                        .Select(m => MediaTypes.TryGetValue(m, out var mv) ? mv : 0)
                        .Where(m => m != 0)
                        .OrderBy(m => (int)m)
                        .ToArray(),
                    ver
                    );
            }
            throw new InvalidDataException();
        });
    }

    public Task<Result<bool>> controlDeviceReboot()
    {
        LogAction();
        return Request("/api/led/controlDeviceReboot", true, result => true);
    }

    public static readonly string[] TimeFormats = new[]
    {
        "yyyy-MM-dd'T'HH:mm:ss.fff'Z'zzz",
        "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
        "yyyy-MM-dd'T'HH:mm:ss'Z'zzz",
        "yyyy-MM-dd'T'HH:mm:ss'Z'",
    };
    public Task<Result<DateTime>> querySystemTime()
    {
        LogAction();
        return Request("/api/led/querySystemTime", false, result =>
        {
            if (result.Query("data.time", JsonValueType.String) is string timeStr)
            {
                foreach (var format in TimeFormats)
                    if (DateTime.TryParseExact(timeStr, format, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out var t))
                        return t;
            }
            throw new InvalidDataException();
        });
    }

    public Task<Result<bool>> controlDateSet()
    {
        LogAction();
        return Request("/api/led/controlDateSet", true, new JsonObj(("time", DateTime.Now.ToString(TimeFormats[0]))), _ => true);
    }

    public Task<Result<Size>> queryResolution()
    {
        LogAction();
        return Request("/api/led/queryResolution", false, result =>
        {
            if (result.Query("data.width", JsonValueType.Int) is int w
                && result.Query("data.height", JsonValueType.Int) is int h)
                return new Size(w, h);
            throw new InvalidDataException();
        });
    }

    public record DeviceStatus(
        int Status,
        int Power,
        int Screen,
        int AutoBrightness,
        int Brightness,
        float Temperature,
        int EnvBrightness
        )
    {
        public bool IsScreenOk => Status == 1;
        public bool IsPowerOn => Power == 1;
        public bool IsScreenOn => Screen == 1;
        public bool IsAutoBrightness => AutoBrightness == 0;
    }

    public Task<Result<DeviceStatus>> queryStatus()
    {
        LogAction();
        return Request("/api/led/queryStatus", false, result =>
        {
            if (result.Query("data.status", JsonValueType.Int) is int status
                && result.Query("data.power", JsonValueType.Int) is int power
                && result.Query("data.screen", JsonValueType.Int) is int screen
                && result.Query("data.autoBrightness", JsonValueType.Int) is int autoBright
                && result.Query("data.brightness", JsonValueType.Int) is int bright
                && result.Query("data.temperature", JsonValueType.Float) is double temperature
                && result.Query("data.envBrightness", JsonValueType.Int) is int envBright
                )
                return new DeviceStatus(
                    status,
                    power,
                    screen,
                    autoBright,
                    bright,
                    (float)temperature,
                    envBright
                    );
            throw new InvalidDataException();
        });
    }

    public Task<Result<bool>> controlLightControlType(bool auto, int bright)
    {
        LogAction();
        var json = new JsonObj(("auto", auto ? 0 : 1));
        if (!auto)
            json.Add("brightness", bright);
        return Request("/api/led/controlLightControlType", true, json, _ => true);
    }

    public Task<Result<bool>> controlLightControlType(int bright) => controlLightControlType(false, bright);

    public Task<Result<bool>> controlSwitch(bool on)
    {
        LogAction();
        return Request("/api/led/controlSwitch", true, new JsonObj(("action", on ? 1 : 0)), _ => true);
    }

    public Task<Result<bool>> uploadDeviceFile(string filename, FileType type, string base64content)
    {
        LogAction();
        return Request("/api/led/uploadDeviceFile", true, new JsonObj(
            ("fileName", filename),
            ("fileType", (int)type),
            ("fileContext", base64content)
            ), _ => true);
    }

    public Task<Result<bool>> uploadDeviceFile(string filename, FileType type, byte[] content) => uploadDeviceFile(filename, type, Base64Encode(content));

    public Task<Result<byte[]>> downDeviceFile(string filename, FileType type)
    {
        LogAction();
        return Request("/api/led/downDeviceFile", true, new JsonObj()
        {
            { "fileName", filename },
            { "fileType", (int)type },
        }, result =>
        {
            if (result.Query("data.fileContext", JsonValueType.String) is string data
                && Base64Decode(data, out var buf))
                return buf;
            throw new InvalidDataException();
        });
    }

    public Task<Result<bool>> deleteDeviceFile(string filename, FileType type)
    {
        LogAction();
        return Request("/api/led/deleteDeviceFile", true, new JsonObj(("fileName", filename), ("fileType", (int)type)), _ => true);
    }

    public Task<Result<string[]>> queryDeviceFileDir(FileType type)
    {
        LogAction();
        return Request("/api/led/queryDeviceFileDir", true, new JsonObj(("fileType", (int)type)), result =>
        {
            if (result.Query("data.fileList", JsonValueType.Array) is JArray arr)
            {
                var lst = new List<string>();
                for (var l = arr.Count; l > 0; l--)
                {
                    if (arr.ElementAt(l - 1) is JValue v
                        && v.Type == JTokenType.String
                        && v.Value is string str)
                        lst.Add(str);
                    else throw new InvalidDataException();
                }
                return lst.ToArray();
            }
            throw new InvalidDataException();
        });
    }

    public Task<Result<bool>> uploadPlaylist(Playlist playlist)
    {
        LogAction();
        return Request("/api/led/uploadPlaylist", true, new JsonObj(("data", playlist.Serialize())), _ => true);
    }
    
    public Task<Result<bool>> uploadPlaylist(IJsonData playlist)
    {
        LogAction();
        return Request("/api/led/uploadPlaylist", true, new JsonObj(("data", playlist)), _ => true);
    }

    public Task<Result<Playlist>> queryPlaylist()
    {
        LogAction();
        return Request("/api/led/queryPlaylist", false, result =>
        {
            if (result.Query("data", JsonValueType.Object) is JObject obj
                && Playlist.TryDeserialize(new JsonObj(obj), out var list))
                return list;
            throw new InvalidDataException();
        });
    }

    public Task<Result<string>> queryCurrentPlaylistFileName()
    {
        LogAction();
        return Request("/api/led/queryPlaylist", false, result =>
        {
            if (result.Query("data.fileName", JsonValueType.String) is string path)
                return path;
            throw new InvalidDataException();
        });
    }

    public Task<Result<bool>> controlPlaySpecifyPlaylist(string filename)
    {
        LogAction();
        return Request("/api/led/controlPlaySpecifyPlaylist", true, new JsonObj(("fileName", filename)), _ => true);
    }

    public Task<Result<byte[]>> queryScreenShot()
    {
        LogAction();
        return Request("/api/led/queryScreenShot", false, result =>
        {
            if (result.Query("data.file", JsonValueType.String) is string str
                && !str.IsNullOrEmpty()
                && Base64Decode(str, out var data))
                return data;
            throw new InvalidDataException();
        });
    }

    public Task<Result<ProgramItem>> queryCurrentPlaylist()
    {
        LogAction();
        return Request("/api/led/queryCurrentPlaylist", false, result =>
        {
            if (result.Query("data", JsonValueType.Object) is JObject obj
                && ProgramItem.TryDeserialize(new JsonObj(obj), out var item))
                return item;
            throw new InvalidDataException();
        });
    }

    public Task<Result<FaultInfo>> queryFaultInfo()
    {
        LogAction();
        return Request("/api/led/queryFaultInfo", true, result =>
        {
            if (result.Query("data.temperature", JsonValueType.Int) is int tempFault
                && result.Query("data.internalFault", JsonValueType.Int) is int internalFault
                && result.Query("data.moduleFault", JsonValueType.Int) is int moduleFault
                && result.Query("data.powerFault", JsonValueType.Int) is int powerFault
                && result.Query("data.pixelFault", JsonValueType.Int) is int pixelFault
                && result.Query("data.checkSystemFault", JsonValueType.Int) is int checkSystemFault
                && result.Query("data.acPowerFault", JsonValueType.Int) is int acPowerFault
                && result.Query("data.spdFault", JsonValueType.Int) is int spdFault
                && result.Query("data.photoSensitive", JsonValueType.Int) is int photoSensitiveFault
                && result.Query("data.doorOpenFault", JsonValueType.Int) is int doorOpenFault
                )
                return new FaultInfo(
                    tempFault,
                    internalFault,
                    moduleFault,
                    powerFault,
                    pixelFault,
                    checkSystemFault,
                    acPowerFault,
                    spdFault,
                    photoSensitiveFault,
                    doorOpenFault
                    );
            throw new InvalidDataException();
        });
    }

    public static readonly Regex ParseResolutionRegex = new Regex(@"^(?<Width>\d+)\*(?<Height>\d+)$", RegexOptions.Compiled);
    public static readonly Regex HexStringRegex = new Regex(@"^[a-fA-F0-9]*$", RegexOptions.Compiled);

    public Task<Result<PixelErrorData>> queryPixelError()
    {
        LogAction();
        return Request("/api/led/queryPixelError", false, result =>
        {
            if (result.Query("data.badCount", JsonValueType.Int) is int badCount
                && result.Query("data.resolution", JsonValueType.String) is string resolutionStr)
            {
                var m = ParseResolutionRegex.Match(resolutionStr);
                if (m.Success)
                {
                    var w = int.Parse(m.Groups["Width"].Value);
                    var h = int.Parse(m.Groups["Height"].Value);
                    var pxs = new Dictionary<long, BadColor>();
                    var colors = BadColor.None;
                    foreach (var color in Enum.GetValues(typeof(BadColor)).Cast<BadColor>().Where(c => c != 0))
                    {
                        if (result.Query($"data.{color.ToString().ToLower()}", JsonValueType.String) is string dataStr
                            && !dataStr.IsNullOrEmpty()
                            && HexStringRegex.IsMatch(dataStr))
                        {
                            colors |= color;
                            foreach (var px in BadPixelData.Decode(w, h, dataStr))
                            {
                                var key = ((long)px.X << 32) | (uint)px.Y;
                                pxs[key] = pxs.TryGetValue(key, out var bpx) ? (bpx | color) : color;
                            }
                        }
                    }
                    var time = "";
                    if (result.Query("data.stamp", JsonValueType.String) is string timestr)
                        time = timestr;
                    return new PixelErrorData(
                        badCount,
                        new(w, h),
                        colors,
                        pxs.Select(kv => new BadPixel(
                            (int)(kv.Key >> 32),
                            (int)(kv.Key & BitHelper.MakeMask(32)),
                            kv.Value
                            )).ToList(),
                        time);
                }
            }
            throw new InvalidDataException();
        });
    }

    public async Task<Result<bool>> controlResetAppIdAndSecret()
    {
        LogAction();
        var r = await Request("/api/led/controlResetAppidAndSecret", true, new JsonObj(), result =>
        {
            if (result.Query("data.appId", JsonValueType.String) is string appid
                && !appid.IsNullOrEmpty()
                && result.Query("data.secret", JsonValueType.String) is string secret
                && !secret.IsNullOrEmpty())
                return (appid, secret);
            throw new InvalidDataException();
        });
        if (r.Ok)
        {
            AppId = r.Value.appid;
            Secret = r.Value.secret;
            return Ok(r, true);
        }
        return Error(r, false);
    }

    protected Task<Result<bool>> controlNetRestartTime(string defaultPlaylistFileName, string heartbeatIP, int heartbeatPort, int heartbeatTimeoutSec, bool enable)
    {
        LogAction();
        var json = new JsonObj(("isEffect", enable ? 0 : 1));
        if (enable)
        {
            json.Add("fileName", defaultPlaylistFileName);
            json.Add("ip", heartbeatIP);
            json.Add("port", heartbeatPort);
            json.Add("outLineTime", heartbeatTimeoutSec);
        }
        return Request("/api/led/controlNetRestartTime", true, json, _ => true);
    }

    public Task<Result<bool>> controlNetRestartTime(string defaultPlaylistFileName, string heartbeatIP, int heartbeatPort, int heartbeatTimeoutSec)
    => controlNetRestartTime(defaultPlaylistFileName, heartbeatIP, heartbeatPort, heartbeatTimeoutSec, true);

    public Task<Result<bool>> controlNetRestartTimeDisable() => controlNetRestartTime("", "", 0, 0, false);
}

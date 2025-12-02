using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Security.Cryptography;
using static Lytec.Protocol.SCL.Constants;

namespace Lytec.Protocol;

public static partial class SCL
{
    public static Encoding DefaultEncode { get; set; }
    public static Func<ILogger>? GetLogger { get; set; }

    static ILogger? Logger => GetLogger?.Invoke();

    static CheckSum<ushort> CreateCRC16() => new CheckSum.CRC16.CCITT_XMODEM();

    [JsonConverter(typeof(StringEnumConverter))]
    public enum StructAddress
    {
        LoadFromSpStructs = 0,
        FPGAProgram = 0x080000,
        FPGAProgram_STM = 0x08000,
        PlayProgram = 0x0B8000,
        PlayProgram_STM = 0x40000,
        LEDConfig = 0x0E0000,
        FPGARAM = 0x0E1000,
        MacNetConfig = 0x0E2000,
        BIOS = 0x0F8000
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum SpStructIndex
    {
        RuntimeInfo = 0,
        MacConfig = 1,
        NetConfig = 2,
        LEDConfig = 3,
        FPGARAM = 4,
        FPGARAM_1stPart = 5,
        FPGARAM_2ndPart = 6,
        FullVersionCode = 7,
        GPSLocation = 8,
        MacNetConfig = 9,
        LoadedFontInfo = 10,
    }

    static SCL()
    {
        var encs =
#if NET6_0_OR_GREATER || !(NET || NETCOREAPP)
            Encoding.GetEncodings()
            .Select(i => (i.CodePage, Encoding: Encoding.GetEncoding(i.CodePage)))
            .OrderBy(i => i.CodePage)
#else
                Enumerable.Range(0, ushort.MaxValue)
                .Select(cp => (CodePage: cp, Encoding: CodePagesEncodingProvider.Instance.GetEncoding(cp)))
                .Where(v => v.Encoding != null)
                .Select(i => (i.CodePage, i.Encoding))
#endif
            .Where(i => i.Encoding.IsSingleByte || i.Encoding.GetType().Name.ToUpper().Contains("DBCS"))
            .ToDictionary(i => i.CodePage, i => i.Encoding);
        var enc = Encoding.Default;
        if (enc == null || !encs.Values.Contains(enc))
        {
            enc = new string[] { "gbk", "gb2312", "windows-1251", "iso-8859-1" }
                .Select(Encoding.GetEncoding)
                .Where(enc1 => enc != null)
                .Append(Encoding.ASCII)
                .First();
        }
        DefaultEncode = enc;
    }

    public static byte[] InitFlashDataBlock(int size, byte fillByte = 0xFF) => GetFlashDataBlock(size, fillByte).ToArray();
    public static IEnumerable<byte> GetFlashDataBlock(int size, byte fillByte = 0xFF) => Enumerable.Repeat(fillByte, size);

    public static byte[] GetFixedLengthStringWithFlash(string str, int size, bool addEnd = true)
    {
        if (str == null)
            str = "";
        var r = str.SelectMany(c => DefaultEncode.GetBytes(new char[] { c }));
        if (addEnd && str.Length > 0)
            r = r.Take(size - 1).Append<byte>(0);
        else r = r.Take(size);
        r = r.Concat(GetFlashDataBlock(size)).Take(size);
        return r.ToArray();
    }

    public static string GetStringFromFixedLength(byte[] str)
    {
        if (str == null || str.Length == 0 || str[0] == '\0')
            return "";
        return DefaultEncode.GetString(str.TakeWhile(c => c != '\0').ToArray());
    }

    public static bool IsFullColor(this in LEDConfig cfg, in RuntimeInfo rs) => rs.FPGAMaker == FPGAMaker.GaoYun ? cfg.IsFullColor : cfg.Range == ControlRange.Range1024x256FullColor || cfg.Range == ControlRange.Range1024x256FullColorCompact;
    public static bool IsFullColor(this in RuntimeInfo rs, in LEDConfig cfg) => cfg.IsFullColor(rs);

    public static int GetSendDataPackCount(int total) => (total + MaxDataLength - 1) / MaxDataLength;
}

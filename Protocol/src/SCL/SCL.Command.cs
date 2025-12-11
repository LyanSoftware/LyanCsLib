using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Lytec.Common.Data;
using static Lytec.Protocol.SCL.Constants;
using Lytec.Common.Communication;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Lytec.Common.Data.IntelHex;
using Lytec.Common;

namespace Lytec.Protocol;

#nullable enable

public static partial class SCL
{
    [Endian(DefaultEndian)]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CommandCode : uint
    {
        SendData = 0x00000000,
        GetData = 0x00000001,
        SaveToFile = 0x00000002,
        LoadFileToBuff = 0x00000003,
        DeleteFile = 0x00000004,
        GetDiskFreeSpace = 0x00000005,
        ReadDir = 0x00000006,
        FormatDisk = 0x00000007,
        _Reserved0 = 0x00000008,
        SetClock = 0x00000009,
        TurnVGA = 0x0000000a,
        SetPlayStatus = TurnVGA,
        SetBright = 0x0000000b,
        SetSwitch = 0x0000000c,
        SetLEDPower = SetSwitch,
        MakeDir = 0x0000000d,
        DeleteDir = 0x0000000e,
        ShowString = 0x0000000f,
        GetLastResult = 0x00000010,
        _Reserved1 = 0x00000011,
        GetPlayInfo = 0x00000012,
        DirectDraw = 0x00000013,
        PowerDotCheck = 0x00000014,
        SendSmallFile = 0x00000015,
        SendToUart = 0x00000016,
        RemoteControl = 0x00000017,
        DHCPConfig = 0x00000018,
        Rename = 0x00000019,
        Reset = 0x000055aa,

        #region 附加协议指令

        FoglightConfig = 0x40, // 读写雾灯配置

        #endregion

        #region 不开放给用户的部分

        Seek = 0x00000080,
        SetFPGAParam = 0x00000081,
        SetHardwave = 0x00000082,
        SaveTo = 0x00000083,
        ReadAny = 0x00000084,

        LoadFrom = ReadAny,
        #endregion

        GetRuntimeInfo = ReadAny,
    }

    public record CommandPack(CommandCode Command, int Arg1, int Arg2, byte[] Arg3)
    {
        public CommandPack(CommandCode cmd, int arg1, int arg2) : this(cmd, arg1, arg2, Array.Empty<byte>()) { }
    }

    public static bool Exec(ISendAndGetAnswerConfig conf, bool isSerialPort, [NotNullWhen(true)] out Pack? Answer, CommandPack command, string? password = null, Func<Pack, bool>? CheckIsSuccess = default, int extTimeout = 0)
    {
        var cmd = new Pack()
        {
            Identifier = isSerialPort ? Pack.IdentifierType.UartSCLSend : Pack.IdentifierType.NetSCLSend,
            AddrCode = (byte)(conf.AddrCode ?? 0),
            Command = command.Command,
            Arg1 = command.Arg1,
            Arg2 = command.Arg2,
            Arg3 = command.Arg3,
        };
        if (!password.IsNullOrEmpty())
            cmd.SetPassword(password);
        cmd.UpdatePackIndex();
        var sbuf = cmd.Serialize();
        var deserializer = Pack.CreateDeserializer();
        Answer = null;
        for (var tryCount = -1; tryCount < conf.Retries; tryCount++)
        {
            deserializer.Reset();
            byte[] recv;
            try
            {
                if (conf.SendAndGetAnswer(sbuf, out var r, extTimeout))
                    recv = r;
                else continue;
            }
            catch (TimeoutException)
            {
                continue;
            }
            var answer = deserializer.Deserialize(recv);
            if (answer == null || !cmd.IsMyAnswer(answer))
                continue;
            if (!answer.IsPasswordAccepted)
                return false;
            Answer = answer;
            if (CheckIsSuccess == null)
                CheckIsSuccess = p => p.Arg2 != FalseValue;
            return Answer != null && CheckIsSuccess(Answer);
        }
        return false;
    }

    public static bool LoadFrom(ISendAndGetAnswerConfig config, bool isSerialPort, int addr, int length, out byte[]? Data, Func<Pack, bool>? CheckIsValidData = null, string? password = null)
    {
        Data = null;
        if (!Exec(config, isSerialPort, out var ans, new(CommandCode.LoadFrom, addr, length), password, r => r.Arg2 != FalseValue && (CheckIsValidData == null || CheckIsValidData(r))))
            return false;
        Data = ans.Arg3;
        return true;
    }

    public static bool LoadFrom(ISendAndGetAnswerConfig config, bool isSerialPort, int addr, int length, [NotNullWhen(true)] out byte[]? Data, int minDataLen, string? password = null)
    => LoadFrom(config, isSerialPort, addr, length, out Data, r => r.Arg2 >= minDataLen && r.Arg3.Length >= minDataLen, password);

    public static bool GetSpStructData(ISendAndGetAnswerConfig config, bool isSerialPort, SpStructIndex index, [NotNullWhen(true)] out byte[]? Data, int minDataLen, string? password = null)
    => LoadFrom(config, isSerialPort, (int)StructAddress.LoadFromSpStructs, (int)index, out Data, minDataLen, password);

    public static bool GetSpStruct<T>(ISendAndGetAnswerConfig config, bool isSerialPort, SpStructIndex index, [NotNullWhen(true)] out T? Data, string? password = null)
    {
        Data = default;
        var t = typeof(T).GenericTypeArguments[0];
        if (!GetSpStructData(config, isSerialPort, index, out var data, Marshal.SizeOf(t), password))
            return false;
        try
        {
            Data = (T)data.ToStruct(t);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }


    /// <summary>
    /// 重新播放节目表
    /// </summary>
    /// <param name="config"></param>
    /// <param name="driver">节目表所在磁盘</param>
    /// <param name="index">节目表索引</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool Replay(ISendAndGetAnswerConfig config, bool isSerialPort, DiskDriver driver, int index, string? password = null)
    => Exec(config, isSerialPort, out _, new(CommandCode.Reset, 0, (index & 0xff) | ((int)driver << 8)), password);

    /// <summary>
    /// 重启设备
    /// </summary>
    /// <param name="config"></param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool Reboot(ISendAndGetAnswerConfig config, bool isSerialPort, string? password = null)
    => Exec(config, isSerialPort, out _, new(CommandCode.Reset, 1, 0), password);

    /// <summary>
    /// 设置SW1继电器状态
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="sw1">继电器开关状态</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool SetSwitch1(ISendAndGetAnswerConfig config, bool isSerialPort, bool sw1, string? password = null)
    => Exec(config, isSerialPort, out _, new(CommandCode.SetSwitch, 0x00010000 | (sw1 ? 1 : 0), 0), password);

    public enum SerialPorts
    {
        COM1 = 1,
        COM2 = 2,
    }

    /// <summary>
    /// 转发数据至串口
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="com">目标串口号</param>
    /// <param name="data">要转发的数据</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool SendToUart(ISendAndGetAnswerConfig config, bool isSerialPort, SerialPorts com, byte[] data, string? password = null)
    => data?.Length > 0 && Exec(config, isSerialPort, out _, new(CommandCode.SendToUart, (int)com, data.Length, data), password, r => r.Arg2 == 1);

    /// <summary>
    /// 获取磁盘可用空间
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="drv">磁盘</param>
    /// <param name="space">可用空间</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool GetDiskFreeSpace(ISendAndGetAnswerConfig config, bool isSerialPort, DiskDriver drv, out int space, string? password = null)
    {
        var ret = Exec(config, isSerialPort, out var answer, new(CommandCode.GetDiskFreeSpace, (int)drv, 0), password, r => r.Arg2 != FalseValue);
        space = (int)(answer?.Arg2 ?? default);
        return ret;
    }

    public record SeekData(IPv4Address IP, string Name, byte[] NameBytes);

    /// <summary>
    /// 探测设备
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="data">设备信息</param>
    /// <returns></returns>
    public static bool Seek(ISendAndGetAnswerConfig config, bool isSerialPort, [NotNullWhen(true)] out SeekData? data)
    {
        data = null;
        if (!Exec(config, isSerialPort, out var answer, new(CommandCode.Seek, 0, 0), null, r => r.Arg3.Length == NameSize))
            return false;
        try
        {
            data = new(answer.Arg1.ToBytes().ToStruct<IPv4Address>(), GetStringFromFixedLength(answer.Arg3), answer.Arg3);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// 设置控制卡名称（备注）
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="comment">名称（备注）</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool SetComment(ISendAndGetAnswerConfig config, bool isSerialPort, string comment, string? password = null)
    {
        const int SizeConst = 16;
        var buf = DefaultEncode
            .GetBytes(comment)
            .Concat(Enumerable.Repeat<byte>(0, SizeConst))
            .Take(SizeConst)
            .ToArray();
        return Exec(config, isSerialPort, out _, new(CommandCode.Rename, 0, buf.Length, buf), password) || SetCommentLegacy(config, isSerialPort, comment, password);
    }

    /// <summary>
    /// 设置控制卡名称（备注）
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="comment">名称（备注）</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool SetCommentLegacy(ISendAndGetAnswerConfig config, bool isSerialPort, string comment, string? password = null)
    {
        var cfg = new MacNetConfig();
        if (!GetSpStruct<NetConfig>(config, isSerialPort, SpStructIndex.NetConfig, out var netcfg, password))
            return false;
        try
        {
            if (netcfg.Name == comment)
                return true;
            netcfg.Name = comment;
            cfg.NetConfig = netcfg;
        }
        catch (Exception) { return false; }
        if (!GetSpStruct<MacConfig>(config, isSerialPort, SpStructIndex.MacConfig, out var maccfg, password))
            return false;
        try
        {
            cfg.MacConfig = maccfg;
        }
        catch (Exception) { return false; }
        if (!Exec(config, isSerialPort, out var answer, new(CommandCode.SendData, 0, MacNetConfig.SizeConst, cfg.Serialize()), password, r => r.Arg2 == MacNetConfig.SizeConst))
            return false;
        var ret = Exec(config, isSerialPort, out _, new(CommandCode.SaveTo, (int)SpStructIndex.MacConfig, MacNetConfig.SizeConst), password, r => r.Arg2 == MacNetConfig.SizeConst);
        if (ret)
            Reboot(config, isSerialPort, password);
        return ret;
    }

    /// <summary>
    /// 设置亮度。0-30为手动亮度（数值越大越亮），31为自动亮度。
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="bright">亮度值</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool SetBright(ISendAndGetAnswerConfig config, bool isSerialPort, int bright, string? password = null)
    {
        if (bright < 0 || bright > 31)
            bright = 31;
        return Exec(config, isSerialPort, out _, new(CommandCode.SetBright, 1, 5, new byte[] { (byte)bright, 0, 0, 0, 0 }), password);
    }

    /// <summary>
    /// 发送数据到通讯缓冲区
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="addr">缓冲区地址</param>
    /// <param name="data">要发送的数据</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool SendData(ISendAndGetAnswerConfig config, bool isSerialPort, int addr, byte[] data, string? password = null)
    {
        if (addr + data.Length > GBufferSize)
            throw new ArgumentException("Buffer out of bounds");
        for (int offset = 0, psz; offset < data.Length; offset += psz)
        {
            psz = Math.Min(data.Length - offset, MaxDataLength);
            var offaddr = addr + offset;
            if (!Exec(config, isSerialPort, out _, new(CommandCode.SendData, offaddr, data.Length, data.Skip(offset).Take(psz).ToArray()), password, r => r.Arg1 == offaddr && r.Arg2 == psz))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 从通讯缓冲区取回数据
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="addr">缓冲区地址</param>
    /// <param name="length">要取回的数据长度</param>
    /// <param name="Data">数据</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool GetData(ISendAndGetAnswerConfig config, bool isSerialPort, int addr, int length, [NotNullWhen(true)] out byte[]? Data, string? password = null)
    {
        if (addr + length > GBufferSize)
            throw new ArgumentException("Buffer out of bounds");
        Data = null;
        var buf = new List<byte>();
        for (int offset = 0, psz; offset < length; offset += psz)
        {
            psz = Math.Min(length - offset, MaxDataLength);
            var offaddr = addr + offset;
            if (!Exec(config, isSerialPort, out var answer, new(CommandCode.GetData, offaddr, psz), password, r => r.Arg1 == offaddr && r.Arg2 == psz))
                return false;
            buf.AddRange(answer.Arg3);
        }
        Data = buf.ToArray();
        return true;
    }

    /// <summary>
    /// 将通讯缓冲区的数据保存为文件
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="drv">目标磁盘</param>
    /// <param name="length">写入文件的数据长度</param>
    /// <param name="filetime">文件时间</param>
    /// <param name="filename">可以带子目录路径的文件名</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool SaveToFile(ISendAndGetAnswerConfig config, bool isSerialPort, DiskDriver drv, int length, DateTime filetime, string filename, string? password = null)
    {
        if (length > GBufferSize)
            throw new ArgumentException("DataBytes out of bounds (cannot exceed 2M bytes)");
        var exdata = new List<byte>();
        exdata.AddRange(new Fat16Time(filetime).ToBytes(DefaultEndian));
        exdata.AddRange(new Fat16Date(filetime).ToBytes(DefaultEndian));
        var filenameBytes = DefaultEncode.GetBytes(filename);
        var buf = new byte[32];
        Array.Copy(filenameBytes, 0, buf, 0, Math.Min(filenameBytes.Length, MaxFilePathLength));
        exdata.AddRange(buf);
        return Exec(config, isSerialPort, out _, new(CommandCode.SaveToFile, (int)drv, length, exdata.ToArray()), password, r => r.Arg2 == length, length / FlashWriteBytesPerSecond * 1000 + 3000);
    }

    /// <summary>
    /// 装载磁盘文件到通讯缓冲区
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="drv">目标磁盘</param>
    /// <param name="filename">可以带子目录路径的文件名</param>
    /// <param name="filesize">装入的文件大小</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool LoadFile(ISendAndGetAnswerConfig config, bool isSerialPort, DiskDriver drv, string filename, out int filesize, string? password = null)
    {
        var nameBytes = DefaultEncode.GetBytes(filename);
        var buf = new byte[36];
        Array.Copy(nameBytes, 0, buf, 4, Math.Min(nameBytes.Length, MaxFilePathLength));
        filesize = -1;
        if (!Exec(config, isSerialPort, out var answer, new(CommandCode.LoadFileToBuff, (int)drv, 0, buf), password, r => r.Arg2 != FalseValue, FlashReadMaxFileSeconds * 1000 + 3000))
            return false;
        filesize = (int)answer.Arg2;
        return filesize > 0;
    }

    /// <summary>
    /// 回读磁盘文件
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="drv">目标磁盘</param>
    /// <param name="filename">可以带子目录路径的文件名</param>
    /// <param name="data">文件内容</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool GetFile(ISendAndGetAnswerConfig config, bool isSerialPort, DiskDriver drv, string filename, [NotNullWhen(true)] out byte[]? Data, string? password = null)
    {
        Data = null;
        return LoadFile(config, isSerialPort, drv, filename, out var filesize, password) && GetData(config, isSerialPort, 0, filesize, out Data, password);
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="drv">目标磁盘</param>
    /// <param name="filename">可以带子目录路径的文件名</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool DeleteFile(ISendAndGetAnswerConfig config, bool isSerialPort, DiskDriver drv, string filename, string? password = null)
    {
        var filenameBytes = DefaultEncode.GetBytes(filename);
        var buf = new byte[36];
        Array.Copy(filenameBytes, 0, buf, 4, Math.Min(filenameBytes.Length, MaxFilePathLength));
        return Exec(config, isSerialPort, out _, new(CommandCode.DeleteFile, (int)drv, 0, buf), password);
    }

    /// <summary>
    /// 创建子文件夹
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="drv">目标磁盘</param>
    /// <param name="name">文件夹名</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool CreateDirectory(ISendAndGetAnswerConfig config, bool isSerialPort, DiskDriver drv, string name, string? password = null)
    {
        var nameBytes = DefaultEncode.GetBytes(name);
        var buf = new byte[36];
        Array.Copy(nameBytes, 0, buf, 4, Math.Min(nameBytes.Length, MaxDirNameLength));
        return Exec(config, isSerialPort, out _, new(CommandCode.MakeDir, (int)drv, 0, buf), password);
    }

    /// <summary>
    /// 删除子文件夹
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="drv">目标磁盘</param>
    /// <param name="name">文件夹名</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool DeleteDirectory(ISendAndGetAnswerConfig config, bool isSerialPort, DiskDriver drv, string name, string? password = null)
    {
        var nameBytes = DefaultEncode.GetBytes(name);
        var buf = new byte[36];
        Array.Copy(nameBytes, 0, buf, 4, Math.Min(nameBytes.Length, MaxDirNameLength));
        return Exec(config, isSerialPort, out _, new(CommandCode.DeleteDir, (int)drv, 0), password);
    }

    /// <summary>
    /// 格式化磁盘，仅支持内置存储（A盘）和RAM内存盘（C盘）
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="drv">目标磁盘</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool FormatDisk(ISendAndGetAnswerConfig config, bool isSerialPort, DiskDriver drv, string? password = null)
    => Exec(config, isSerialPort, out _, new(CommandCode.FormatDisk, (int)drv, 0), password);

    /// <summary>
    /// 在区域中实时显示文字，需要已配置字库，且有有效的节目表PLAYLIST.LY正在播放中
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="left">区域左边缘</param>
    /// <param name="top">区域上边缘</param>
    /// <param name="width">区域宽度</param>
    /// <param name="height">区域高度</param>
    /// <param name="str">要显示的文字</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool ShowString(ISendAndGetAnswerConfig config, bool isSerialPort, int left, int top, int width, int height, string str, string? password = null)
    {
        var bytes = DefaultEncode.GetBytes(str);
        const int maxlen = MaxDataLength - 12 - 1; // 12: CMD+PA1+PA2, 1: 结尾的0字节
        if (bytes.Length > maxlen)
            throw new ArgumentException($"String too long (current: {bytes.Length} bytes, max: {maxlen} bytes)");
        bytes = bytes.Concat(new byte[] { 0 }).ToArray();
        return Exec(config, isSerialPort, out _, new(CommandCode.ShowString, left | (top << 16), width | (height << 16), bytes), password);
    }

    public static bool SaveTo(ISendAndGetAnswerConfig config, bool isSerialPort, int addr, int length, string? password = null, int extTimeout = 0)
    => Exec(config, isSerialPort, out _, new(CommandCode.SaveTo, addr, length), password, r => r.Arg2 == length, extTimeout);

    public static bool GetRuntimeInfo(ISendAndGetAnswerConfig config, bool isSerialPort, [NotNullWhen(true)] out RuntimeInfo? Info, string? password = null)
    {
        Info = null;
        if (!Exec(config, isSerialPort, out var answer, new(CommandCode.GetRuntimeInfo, 0, 0), password, r => r.Arg2 == RuntimeInfo.SizeConst))
            return false;
        Info = answer.Arg3.ToStruct<RuntimeInfo>();
        return true;
    }

    /// <summary>
    /// 节目表位置信息
    /// </summary>
    public struct PlaylistLocationInfo
    {
        public DiskDriver Driver { get; set; }
        public int Index { get; set; }
    }

    /// <summary>
    /// 发送小文件，文件大小不得超过988字节
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="drv">目标磁盘</param>
    /// <param name="filename">可以带子目录路径的文件名</param>
    /// <param name="filedata">文件数据</param>
    /// <param name="replay">发送后重新播放指定节目表。为null则不重新播放</param>
    /// <param name="filetime">文件时间，默认为当前系统时间</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool SendSmallFile(ISendAndGetAnswerConfig config, bool isSerialPort, DiskDriver drv, string filename, byte[] filedata, PlaylistLocationInfo? replay = null, DateTime filetime = default, string? password = null)
    {
        if (filedata.Length > 988)
            throw new ArgumentException("File is too large (cannot exceed 988 bytes)");
        var exdata = new List<byte>();
        exdata.AddRange(new Fat16Time(filetime).ToBytes(DefaultEndian));
        exdata.AddRange(new Fat16Date(filetime).ToBytes(DefaultEndian));
        var filenameBytes = DefaultEncode.GetBytes(filename);
        var buf = new byte[28];
        Array.Copy(filenameBytes, 0, buf, 0, Math.Min(filenameBytes.Length, MaxFilePathLength));
        exdata.AddRange(buf);
        int i = 0;
        if (replay != null)
            i = 1 | ((int)replay.Value.Driver << 24) | (replay.Value.Index << 16);
        exdata.AddRange(i.ToBytes(DefaultEndian));
        exdata.AddRange(filedata);
        return Exec(config, isSerialPort, out _, new(CommandCode.SendSmallFile, (int)drv, filedata.Length, exdata.ToArray()), password, r => r.Arg2 == filedata.Length);
    }

    [Serializable]
    [Endian(DefaultEndian)]
    public readonly struct VersionCode
    {
        public const long InvalidValue = -1;
        public static VersionCode Invalid { get; } = new VersionCode(InvalidValue);

        public const int SizeConst = 8;
        static VersionCode() => Debug.Assert(Marshal.SizeOf<VersionCode>() == SizeConst);

        public long Value { get; }

        public int Major => (int)(Value >> 48);
        public int Minor => (int)((Value >> 32) & 0xffff);
        public int Build => (int)Value;

        public VersionCode(long ver) => Value = ver;
        public VersionCode(ushort major, ushort minor, uint build) => Value = ((long)major << 48) | ((long)minor << 32) | build;

        public override string ToString() => $"{Major}.{Minor}.{Build}";

        public static implicit operator long(VersionCode ver) => ver.Value;
        public static implicit operator VersionCode(long ver) => new VersionCode(ver);
    }

    public static bool GetVersionCode(ISendAndGetAnswerConfig config, bool isSerialPort, [NotNullWhen(true)] out VersionCode? Version, string? password = null)
    => GetSpStruct(config, isSerialPort, SpStructIndex.FullVersionCode, out Version, password);

    public static bool GetLEDConfig(ISendAndGetAnswerConfig config, bool isSerialPort, [NotNullWhen(true)] out LEDConfig? Cfg, string? password = null)
    => GetSpStruct(config, isSerialPort, SpStructIndex.LEDConfig, out Cfg, password) && Cfg.Value.IsValid;

    public static bool GetMacConfig(ISendAndGetAnswerConfig config, bool isSerialPort, [NotNullWhen(true)] out MacConfig? Cfg, string? password = null)
    => GetSpStruct(config, isSerialPort, SpStructIndex.MacConfig, out Cfg, password);

    public static bool GetNetConfig(ISendAndGetAnswerConfig config, bool isSerialPort, [NotNullWhen(true)] out NetConfig? Cfg, string? password = null)
    => GetSpStruct(config, isSerialPort, SpStructIndex.NetConfig, out Cfg, password) && Cfg.Value.IsValid;

    public static bool GetMacNetConfig(ISendAndGetAnswerConfig config, bool isSerialPort, [NotNullWhen(true)] out MacNetConfig? Cfg, string? password = null)
    {
        if (GetSpStruct(config, isSerialPort, SpStructIndex.MacNetConfig, out Cfg, password))
            return true;
        Cfg = null;
        if (!GetMacConfig(config, isSerialPort, out var mac, password) || !GetNetConfig(config, isSerialPort, out var net, password))
            return false;
        Cfg = new MacNetConfig(mac.Value, net.Value);
        return true;
    }

    public static bool SetLEDConfig(ISendAndGetAnswerConfig config, bool isSerialPort, LEDConfig conf, string? password = null)
    => SendData(config, isSerialPort, 0, conf.ToBytes(), password)
        && SaveTo(config, isSerialPort, (int)StructAddress.LEDConfig, LEDConfig.SizeConst, password);

    public static bool SetMacNetConfig(ISendAndGetAnswerConfig config, bool isSerialPort, MacNetConfig cfg, string? password = null)
    => SendData(config, isSerialPort, 0, cfg.Serialize(), password)
        && SaveTo(config, isSerialPort, (int)StructAddress.MacNetConfig, MacNetConfig.SizeConst, password);

    public static bool SetMacNetConfig(ISendAndGetAnswerConfig config, bool isSerialPort, MacConfig mac, NetConfig net, string? password = null)
    => SetMacNetConfig(config, isSerialPort, new MacNetConfig(mac, net), password);

    public static bool GetRouteData1stPart(ISendAndGetAnswerConfig config, bool isSerialPort, [NotNullWhen(true)] out byte[]? Data, string? password = null)
    => GetSpStructData(config, isSerialPort, SpStructIndex.FPGARAM_1stPart, out Data, 1024, password);

    public static bool GetRouteData2ndPart(ISendAndGetAnswerConfig config, bool isSerialPort, [NotNullWhen(true)] out byte[]? Data, string? password = null)
    => GetSpStructData(config, isSerialPort, SpStructIndex.FPGARAM_2ndPart, out Data, 1024, password);

    public static bool GetRouteData(ISendAndGetAnswerConfig config, bool isSerialPort, [NotNullWhen(true)] out RouteData? Data, string? password = null)
    {
        Data = null;
        if (!GetRouteData1stPart(config, isSerialPort, out var p1, password)
            || !GetRouteData2ndPart(config, isSerialPort, out var p2, password))
            return false;
        Data = new RouteData(p1.Concat(p2).ToArray().ToStruct<ushort[]>());
        return true;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum DirectDrawType
    {
        FillRect = 0,
        DrawLine = 1,
        DrawPoints = 2
    }

    public static bool DirectDraw(ISendAndGetAnswerConfig config, bool isSerialPort, DirectDrawType type, Color color, Point[] points, int regionIndex = 0, string? password = null)
    {
        switch (type)
        {
            case DirectDrawType.FillRect:
            case DirectDrawType.DrawLine:
                if (points.Length != 2)
                    return false;
                break;
            case DirectDrawType.DrawPoints:
            default:
                if (points.Length < 1)
                    return false;
                break;
        }
        return Exec(config, isSerialPort, out _, new(CommandCode.DirectDraw, color.To1BitRGBColor() | (regionIndex << 16), points.Length | ((int)type << 16), points.ToBytes()), password);
    }

    public static bool DirectDraw_FillRect(ISendAndGetAnswerConfig config, bool isSerialPort, Color color, Point p1, Point p2, int regionIndex = 0, string? password = null)
    => DirectDraw(config, isSerialPort, DirectDrawType.FillRect, color, new Point[] { p1, p2 }, regionIndex, password);

    public static bool DirectDraw_DrawLine(ISendAndGetAnswerConfig config, bool isSerialPort, Color color, Point p1, Point p2, int regionIndex = 0, string? password = null)
    => DirectDraw(config, isSerialPort, DirectDrawType.DrawLine, color, new Point[] { p1, p2 }, regionIndex, password);

    public static bool DirectDraw_DrawPoints(ISendAndGetAnswerConfig config, bool isSerialPort, Color color, IEnumerable<Point> points, int regionIndex = 0, string? password = null)
    => DirectDraw(config, isSerialPort, DirectDrawType.DrawPoints, color, points.ToArray(), regionIndex, password);

    public class HeartbeatArgs
    {
        public string Address { get; set; } = "";
        public ushort Port { get; set; }
        public byte IntervalMinutes { get; set; }
    }

    public static bool GetHeartbeatArgs(ISendAndGetAnswerConfig config, bool isSerialPort, [NotNullWhen(true)] out HeartbeatArgs? Args, string? password = null)
    {
        Args = null;
        if (!GetNetConfig(config, isSerialPort, out var cfg, password))
            return false;
        Args = new HeartbeatArgs()
        {
            Address = cfg.Value.ServerIP.IsValid ? cfg.Value.ServerIP.ToString() : cfg.Value.ServerAddr,
            Port = cfg.Value.HeartbeatPort,
            IntervalMinutes = cfg.Value.HeartbeatPeriod,
        };
        return true;
    }

    private static readonly Regex HeartbeatServerUrlRegex = new Regex(@"^[a-zA-Z]([a-zA-Z0-9.\-]*[a-zA-Z0-9])?\.[a-zA-Z]([a-zA-Z0-9.\-]*[a-zA-Z0-9])?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public static bool ValidHeartbeatServerUrl(string url)
    => HeartbeatServerUrlRegex.IsMatch(url) || (IPAddress.TryParse(url, out var ip) && ip.AddressFamily == AddressFamily.InterNetwork);
    public static bool SetHeartbeatArgs(ISendAndGetAnswerConfig config, bool isSerialPort, HeartbeatArgs args, string? password = null)
    {
        var ip = IPAddress.TryParse(args.Address, out var ip1) ? ip1 : null;
        if (ip == null && !ValidHeartbeatServerUrl(args.Address))
            return false;
        if (!GetMacNetConfig(config, isSerialPort, out var cfg1, password))
            return false;
        var cfg = cfg1.Value;
        var net = cfg.NetConfig;
        net.ServerIP = ip != null ? new IPv4AddressWithValid(ip) : IPv4AddressWithValid.Invalid;
        net.ServerAddr = ip == null ? args.Address : "";
        net.HeartbeatPort = args.Port;
        net.HeartbeatPeriod = args.IntervalMinutes;
        cfg.NetConfig = net;
        return SetMacNetConfig(config, isSerialPort, cfg, password);
    }

    /// <summary>
    /// 获取能见度监测仪信息及雾灯配置
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="Info">能见度监测仪信息</param>
    /// <param name="Args">雾灯配置</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool GetFoglight(ISendAndGetAnswerConfig config, bool isSerialPort, [NotNullWhen(true)] out VisMonitorInfo? Info, [NotNullWhen(true)] out FoglightArgs? Args, string? password = null)
    {
        Info = new VisMonitorInfo() { Status = unchecked((ushort)-1) };
        Args = default;
        if (!Exec(config, isSerialPort, out var answer, new(CommandCode.FoglightConfig, 0, 0), password, r => r.Arg2 >= FoglightArgs.MinSize))
            return false;
        try
        {
            Info = answer!.Arg1.ToBytes().ToStruct<VisMonitorInfo>();
            Args = FoglightArgs.Deserialize(answer.Arg3);
            if (Args == null)
                return false;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// 修改雾灯配置
    /// </summary>
    /// <param name="config">通信配置</param>
    /// <param name="args">雾灯配置</param>
    /// <param name="password">网络通信密码</param>
    /// <returns></returns>
    public static bool SetFoglight(ISendAndGetAnswerConfig config, bool isSerialPort, FoglightArgs args, string? password = null)
    => Exec(config, isSerialPort, out _, new(CommandCode.FoglightConfig, 0, FoglightArgs.SizeConst, args.Serialize()), password, r => r.Arg2 == FoglightArgs.SizeConst);

    public static bool GetPowerDotCheckInfo(ISendAndGetAnswerConfig config, bool isSerialPort, [NotNullWhen(true)] out PowerDotCheckInfo? Info, PowerDotCheckInfoParts parts = PowerDotCheckInfoParts.All, string? password = null)
    {
        Info = default;
        if (!Exec(config, isSerialPort, out _, new(CommandCode.PowerDotCheck, 1, 0), password, recv => recv.Arg2 != FalseValue))
            return false;
        while (true)
        {
            if (!Exec(config, isSerialPort, out var checkState, new(CommandCode.PowerDotCheck, 2, 0), password, recv => recv.Arg2 != FalseValue))
                return false;
            if (checkState.Arg2 == 0)
                break;
            Thread.Sleep(500);
        }
        if (!Exec(config, isSerialPort, out _, new(CommandCode.PowerDotCheck, 0, 0), password, recv => recv.Arg2 != FalseValue))
            return false;
        var info = new CheckInfo();
        if (parts.HasFlag(PowerDotCheckInfoParts.CheckInfo))
        {
            if (!GetData(config, isSerialPort, 0, CheckInfo.SizeConst, out var infoBytes, password))
                return false;
            info = CheckInfo.Deserialize(infoBytes);
        }
        var ledcfg = new LEDConfig();
        if (parts.HasFlag(PowerDotCheckInfoParts.LEDConfig))
        {
            if (!GetLEDConfig(config, isSerialPort, out var ledcfg1, password))
                return false;
            ledcfg = ledcfg1.Value;
        }
        var netcfg = new NetConfig();
        if (parts.HasFlag(PowerDotCheckInfoParts.NetConfig))
        {
            if (!GetNetConfig(config, isSerialPort, out var netcfg1, password))
                return false;
            netcfg = netcfg1.Value;
        }
        var route = new byte[RouteData.SizeConst];
        if (parts.HasFlag(PowerDotCheckInfoParts.Route1stPart))
        {
            if (GetRouteData1stPart(config, isSerialPort, out var routeBytes, password))
                return false;
            Array.Copy(routeBytes, route, routeBytes!.Length);
        }
        if (parts.HasFlag(PowerDotCheckInfoParts.Route2ndPart))
        {
            if (GetRouteData2ndPart(config, isSerialPort, out var routeBytes, password))
                return false;
            Array.Copy(routeBytes, 0, route, 1024, routeBytes!.Length);
        }
        var rinfo = new RouteInfo(ledcfg, RouteData.Deserialize(route));
        byte[] faultDotsBytes = Array.Empty<byte>();
        var faultDots = new List<FaultDotInfo>();
        if (parts.HasFlag(PowerDotCheckInfoParts.FaultDots))
        {
            if (!GetData(config, isSerialPort, 0x1000, info.FaultDotsCount * FaultDotData.SizeConst, out var faultDotsBytes1, password))
                return false;
            faultDotsBytes = faultDotsBytes1;
            for (var i = 0; i < info.FaultDotsCount; i++)
                faultDots.Add(new FaultDotInfo(FaultDotData.Deserialize(faultDotsBytes, i * FaultDotData.SizeConst), rinfo, ledcfg));
        }
        Info = new PowerDotCheckInfo(info, ledcfg, netcfg, rinfo, faultDots.ToArray());
        return true;
    }

    public enum ProgramFileType
    {
        Invalid = 0,
        Play,
        FPGA,
        Boot
    }

    public static bool Update(ISendAndGetAnswerConfig config, bool isSerialPort, ProgramFileType type, byte[] programFileData, int extTimeout = 0)
    {
        if (programFileData == null || programFileData.Length < 4)
            return false;
        if (!GetRuntimeInfo(config, isSerialPort, out var rs, SuPw))
            return false;
        int paddr;
        int maxsize;
        var isHex = true;
        var reboot = false;
        switch (type)
        {
            case ProgramFileType.Play:
                paddr = 0xB8000; maxsize = 0x28000;
                isHex = rs.Value.CPUMaker == CPUMaker.RDC;
                reboot = rs.Value.CPUMaker == CPUMaker.RDC;
                break;
            case ProgramFileType.FPGA:
            //paddr = 0xF8000; maxsize = 0x8000;
            case ProgramFileType.Boot:
            //paddr = 0x80000; maxsize = 0x37000;
            default: return false; // not supported
        }
        byte[] pdata;
        const int headerSize = 64;
        var crcBlockSize = 16;
        var crcTakeBlockBytes = 2;
        var programAlign = 0;
        var sendAlign = 0;
        if (isHex)
        {
            try
            {
                var recs = Records.Decode(Encoding.UTF8.GetString(programFileData));
                if (recs == null)
                    return false;
                pdata = recs.Data;
            }
            catch (Exception)
            {
                return false;
            }
        }
        else
        {
            pdata = programFileData;
            programAlign = 0x100;
            sendAlign = 0x400;
        }
        if (programFileData == null || programFileData.Length < 4)
            return false;
        if (programAlign > 0)
        {
            var sz = (pdata.Length + programAlign - 1) / programAlign;
            if (sz > pdata.Length)
                pdata = pdata.Concat(InitFlashDataBlock(sz)).ToArray();
        }
        uint programSize = (uint)pdata.Length;
        if (sendAlign > 0)
        {
            var sz = (pdata.Length + sendAlign - 1) / sendAlign;
            if (sz > pdata.Length)
                pdata = pdata.Concat(InitFlashDataBlock(sz)).ToArray();
        }
        if (pdata.Length + headerSize > maxsize)
            return false;
        ushort crc = 0;
        if (crcBlockSize > 0)
        {
            var cc = CreateCRC16();
            for (var offset = 0; offset < pdata.Length; offset += crcBlockSize)
                for (var i = 0; i < crcTakeBlockBytes; i++)
                    crc = cc.Compute(pdata[offset + i]);
        }
        var data = programSize.ToBytes(Endian.Little).Concat(crc.ToBytes(Endian.Little)).ToArray();
        data = data.Concat(InitFlashDataBlock(headerSize - data.Length)).Concat(pdata).ToArray();
        if (!SendData(config, isSerialPort, 0, data, SuPw))
            return false;
        if (!SaveTo(config, isSerialPort, paddr, data.Length, SuPw, data.Length / FlashWriteBytesPerSecond * 1000 + 3000 + extTimeout))
            return false;
        if (reboot && !Reboot(config, isSerialPort, SuPw))
            return false;
        Thread.Sleep(8000);
        return true;
    }

}

#nullable restore

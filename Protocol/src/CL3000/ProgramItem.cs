using System.Diagnostics;
using System.Runtime.InteropServices;
using Lytec.Common.Data;

namespace Lytec.Protocol.CL3000;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
[Endian(Endian.Little)]
public struct ClockType
{
    public byte Second;
    public byte Minute;
    public byte Hour;
    public byte Day;
    public byte Month;
    public byte Week;
    public byte Year;
    public byte NC;    //Reserved, keep0
}

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
[Endian(Endian.Little)]
public class PicFileHdr
{
    public byte Type;          //Picture file type 0-single color 1-double color
    public byte PicCount;      //Picture count
    public short PicHeight;    //Picture height
    public short PicWidth;     //Picture width
    public short PicOffset;    //Offset of the first picture
    public short LastPicH;     //Last picture height
    public short LastPicW;     //Last picture width

    public ColorType ColorType { get => (ColorType)Type; set => Type = (byte)value; }

    public const int SizeConst = 12;
    static PicFileHdr() => Debug.Assert(Marshal.SizeOf(typeof(PicFileHdr)) == SizeConst);

    public byte[] ToBytes()
    {
        var p = Marshal.AllocHGlobal(SizeConst);
        try
        {
            Marshal.StructureToPtr(this, p, false);
            var buf = new byte[SizeConst];
            Marshal.Copy(p, buf, 0, buf.Length);
            return buf;
        }
        finally
        {
            Marshal.FreeHGlobal(p);
        }
    }
}

/// <summary>
/// 节目类型
/// </summary>
public enum ProgramType
{
    [System.ComponentModel.Description("图片")]
    Image = 0,
    [System.ComponentModel.Description("实时版面")]
    RealTime = 1,
    [System.ComponentModel.Description("RAM文本")]
    RamText = 0b11
}

[Flags]
public enum Weekday
{
    Mon = 1 << 0,
    Tue = 1 << 1,
    Wed = 1 << 2,
    Thu = 1 << 3,
    Fri = 1 << 4,
    Sat = 1 << 5,
    Sun = 1 << 6,
}

/// <summary>
/// 节目项定时
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
[Endian(Endian.Little)]
public struct ProgramSchedule
{
    /// <summary>
    /// 不使用定时
    /// </summary>
    public static readonly ProgramSchedule NoSchedule = new ProgramSchedule() { Schedule = -1 };

    int Schedule;

    /// <summary>
    /// 定时开始时间的分钟
    /// </summary>
    public int StartMinute
    {
        get => BitHelper.GetValue(Schedule, 0, 6);
        set => Schedule = BitHelper.SetValue(Schedule, value, 0, 6);
    }

    /// <summary>
    /// 定时结束时间的分钟
    /// </summary>
    public int EndMinute
    {
        get => BitHelper.GetValue(Schedule, 6, 6);
        set => Schedule = BitHelper.SetValue(Schedule, value, 6, 6);
    }

    /// <summary>
    /// 定时开始时间的小时
    /// </summary>
    public int StartHour
    {
        get => BitHelper.GetValue(Schedule, 12, 5);
        set => Schedule = BitHelper.SetValue(Schedule, value, 12, 5);
    }

    /// <summary>
    /// 定时结束时间的小时
    /// </summary>
    public int EndHour
    {
        get => BitHelper.GetValue(Schedule, 17, 5);
        set => Schedule = BitHelper.SetValue(Schedule, value, 17, 5);
    }

    /* 中间3bit保留 */

    /// <summary>
    /// 周定时
    /// </summary>
    public Weekday Weekdays
    {
        get => (Weekday)BitHelper.GetValue(Schedule, 25, 7);
        set => Schedule = BitHelper.SetValue(Schedule, (int)value, 25, 7);
    }

    public ProgramSchedule(int startMinute, int endMinute, int startHour, int endHour, Weekday weekdays)
    {
        Schedule = NoSchedule.Schedule;
        StartMinute = startMinute;
        EndMinute = endMinute;
        StartHour = startHour;
        EndHour = endHour;
        Weekdays = weekdays;
    }
}

/// <summary>
/// 节目项
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
[Endian(Endian.Little)]
public struct ProgramItem
{
    /// <summary>
    /// 节目停留时间无限
    /// </summary>
    public const int InfiniteStay = 0xfffff;

    ushort Flag;
    ushort PicFIndex;
    int _Effect;
    int SpeedStay;

    /// <summary>
    /// 节目定时
    /// </summary>
    public ProgramSchedule Schedule { get; set; }

    /// <summary>
    /// 节目项所在分区
    /// </summary>
    public int AreaNumber
    {
        get => BitHelper.GetValue(Flag, 0, 2);
        set => Flag = (ushort)BitHelper.SetValue(Flag, value, 0, 2);
    }

    /// <summary>
    /// 节目类型
    /// </summary>
    public ProgramType Type
    {
        get => (ProgramType)BitHelper.GetValue(Flag, 2, 4);
        set => Flag = (ushort)BitHelper.SetValue(Flag, (int)value, 2, 4);
    }

    /// <summary>
    /// 所属节目表(节目组)编号
    /// </summary>
    public int GroupNumber
    {
        get => BitHelper.GetValue(Flag, 6, 10);
        set => Flag = (ushort)BitHelper.SetValue(Flag, value, 6, 10);
    }

    /// <summary>
    /// 节目内容索引：图片编号 或 实时版面编号 或 RAM文本索引
    /// </summary>
    public int ContentIndex
    {
        get => PicFIndex;
        set
        {
            var min = 0;
            var max = 2047;
            if (Type == ProgramType.RealTime)
                min = 1;
            else if (Type == ProgramType.RamText)
                max = 3;
            if (value < min || value > max)
                throw new ArgumentOutOfRangeException();
            PicFIndex = (ushort)value;
        }
    }

    /// <summary>
    /// 节目效果
    /// </summary>
    public int Effect
    {
        get => _Effect & (int)BitHelper.MakeMask(6);
        set
        {
            // CL3000不支持与进入效果不同的退出效果，因此退出参数复制进入参数
            if (value < 0 || value > 25)
                throw new ArgumentOutOfRangeException();
            _Effect = value | (value << 6);
        }
    }

    /// <summary>
    /// 节目效果速度
    /// </summary>
    public int Speed
    {
        get => SpeedStay & (int)BitHelper.MakeMask(4);
        set
        {
            // CL3000不支持与进入效果不同的退出效果，因此退出参数复制进入参数
            if (value < 0)
                throw new ArgumentOutOfRangeException();
            if (value > 12)
                value = 12;
            SpeedStay = (SpeedStay & ~0xff) | value | (value << 4);
        }
    }

    /// <summary>
    /// 节目停留时间
    /// </summary>
    public int Stay
    {
        get => BitHelper.GetValue(SpeedStay, 8, 20);
        set
        {
            if (value < 0 || value > InfiniteStay)
                throw new ArgumentOutOfRangeException();
            SpeedStay = BitHelper.SetValue(SpeedStay, value, 8, 20);
        }
    }

    public ProgramItem(int areaNumber, ProgramType type, int groupNumber, int contentIndex, int effect, int speed, int stay, ProgramSchedule schedule) : this()
    {
        AreaNumber = areaNumber;
        Type = type;
        GroupNumber = groupNumber;
        ContentIndex = contentIndex;
        Effect = effect;
        Speed = speed;
        Stay = stay;
        Schedule = schedule;
    }
}

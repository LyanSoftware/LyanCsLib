using System.Runtime.InteropServices;
using System.Diagnostics;
using Lytec.Common.Data;
using static Lytec.Protocol.SCL.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Lytec.Common.Number;

namespace Lytec.Protocol;

public static partial class SCL
{
    [Serializable]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CPUMaker : ushort
    {
        RDC = 'R',
        STM = 'S'
    }

    [Serializable]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FPGAMaker : ushort
    {
        Altera = 'A',
        GaoYun = 'G'
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Endian(DefaultEndian)]
    public struct RuntimeInfo
    {
        public const int SizeConst = 512;

        static RuntimeInfo() => Debug.Assert(Marshal.SizeOf<RuntimeInfo>() == SizeConst);

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        readonly byte[] Reserved1;                              // 无关数据       / Reserved.

        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        byte[] SoftwareVersion { get; set; }                    // 软件版本       / Software Version.
        public string SoftwareVersionStr
        {
            get
            {
                if ((SoftwareVersion[0] == '3' || SoftwareVersion[0] == '4')
                    && SoftwareVersion[1] == '.')
                    return DefaultEncode.GetString(SoftwareVersion);
                else return $"{SoftwareVersion[0]}.{SoftwareVersion[1]}.{SoftwareVersion[2] | (SoftwareVersion[3] << 8)}";
            }
        }

        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        byte[] FPGAVersion { get; set; }                        // FPGA版本       / FPGA Version.
        public string FPGAVersionStr => DefaultEncode.GetString(FPGAVersion);

        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        byte[] PCBVersion { get; set; }                         // PCB版本        / PCB Version.
        public string PCBVersionStr => DefaultEncode.GetString(PCBVersion);

        ushort FPGA_OK { get; set; }                            // FPGA配置成功   / FPGA Configure Success.

        public ushort TotalProgCount { get; private set; }      // 总节目数       / Total program count.

        public ushort CurrentProg { get; private set; }         // 当前播放的节目 / Which program is playing in current programs.

        ushort ProgValid { get; set; }                          // 节目表是否有效 / Is the program valid

        public ushort ProgDriver { get; private set; }          // 节目所在驱动器 / Disk number of the program from

        public ushort SD_OK { get; private set; }               // SD卡就绪标志   / Ready Flag of the SD card.

        ushort BrightCount { get; set; }                        // 亮度计数       / Brightness Count.

        public ushort Humid { get; private set; }               // 湿度           / Humidity from the sensor

        public short Temprature { get; private set; }           // 温度           / Tempeature from DS18B20

        public ushort PowerSwitch { get; private set; }         // 屏体电源       / State of the power supply of LED.

        int Reserved2 { get; set; }                             // 未使用         / Not Used.

        public byte ProgramIndex { get; private set; }          // 节目索引       / Which setDutyFreqVisible of program now playing.

        public byte ProgramDriver { get; private set; }         // 节目驱动器     / Such as ProgDrv

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        readonly byte[] Reserved3;                              // 无关数据       / Reserved.

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct Calendar
        {
            public ushort Second { get; private set; }          // 日历芯片之秒   / Second of the RTC in the controller.
            public ushort Minute { get; private set; }          // 日历芯片之分   / Minute of the RTC in the controller.
            public ushort Hour { get; private set; }            // 日历芯片之时   / Hour   of the RTC in the controller.
            public ushort Day { get; private set; }             // 日历芯片之日   / Day of month of the RTC in the controller.
            public ushort Month { get; private set; }           // 日历芯片之月   / Month  of the RTC in the controller.
            public ushort Week { get; private set; }            // 日历芯片之星期，0-6，0表示星期日 / Day of week of the RTC in the controller, 0-6, 0 represents Sunday.
            public ushort Year { get; private set; }            // 日历芯片之年，二位数字，加2000为实际的年份 / Year of the RTC in the controller.

            public Calendar(DateTime clock)
            {
                Year = (ushort)(clock.Year >= 2000 ? clock.Year - 2000 : 0);
                Month = (ushort)clock.Month;
                Day = (ushort)clock.Day;
                Hour = (ushort)clock.Hour;
                Minute = (ushort)clock.Minute;
                Second = (ushort)clock.Second;
                Week = (ushort)clock.DayOfWeek;
            }

            public static implicit operator DateTime(Calendar clock) => new DateTime(clock.Year + 2000, clock.Month, clock.Day, clock.Hour, clock.Minute, clock.Second);
            public static implicit operator Calendar(DateTime clock) => new Calendar(clock);
            public override string ToString() => ((DateTime)this).ToString();
        }
        public Calendar Clock { get; private set; }             // 时钟

        public ushort Brightness { get; private set; }          // 亮度           / Brightness setDutyFreqVisible to LED.

        ushort CheckPort { get; set; }                          // D0-D7 8个检测端口的结果 / D0-D7 Results for 8 test ports.


        // 8字符的单字节字符数组
        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public readonly struct CharArray_8char
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            readonly byte[] Data;
            public byte this[int i] => Data[i];
        }

        public CharArray_8char[] Com1Data => _Com1Data;         // COM1接收的数据 / 8 groups of data received from serial-port 1
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        readonly CharArray_8char[] _Com1Data;

        public CharArray_8char[] Com2Data => _Com2Data;         // COM2接收的数据 / 8 groups of data received from serial-port 2
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        readonly CharArray_8char[] _Com2Data;

        public CharArray_8char[] Com3Data => _Com3Data;         // COM3接收的数据 / 8 groups of data received from serial-port 3
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        readonly CharArray_8char[] _Com3Data;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        readonly int[] ComNewData;                              // 无关数据       / Reserved.

        ushort TimerOK { get; set; }                            // 定时器配置成功 / Timers Configure Success.

        public ushort PowerMode { get; private set; }           // 电源模式       / Mode of Power supply of LED.

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        readonly byte[] ProgPath;                               // 节目表路径     / Path of program list file.

        int PowerSwitch_Hardware { get; set; }                  // 屏体电源的硬件开关 / Hardware switch for power of LED.

        ushort DebugPort { get; set; }                          // Debug串口号    / Port of Debug.

        public WORDBool SW0 { get; private set; }                 // SW0的状态      / State of the SW0 port.

        public WORDBool SW1 { get; private set; }                 // SW1的状态      / State of the SW1 port.

        public ushort FontLoaded { get; private set; }          // 已装载字库数量 / Amount of loaded fonts.

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 112)]
        readonly byte[] Reserved5;                              // 无关数据       / Reserved.

        public FPGAMaker FPGAMaker => (FPGAMaker)PCBVersion[3];

        public CPUMaker CPUMaker => (CPUMaker)((char)PCBVersion[0] >= '4' ? 'S' : 'R');
    }
}

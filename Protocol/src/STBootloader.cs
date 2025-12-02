using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lytec.Common;
using Lytec.Common.Communication;
using Lytec.Common.Data;

namespace Lytec.Protocol;

public class STBootloader
{
    public enum Series
    {
        Unknown = 0,
        STM32C0,
        STM32F0,
        STM32F1,
        STM32F2,
        STM32F3,
        STM32F4,
        STM32F7,
        STM32G0,
        STM32G4,
        STM32H7,
        STM32L0,
        STM32L1,
        STM32L4,
        STM32L5,
        STM32WB,
        STM32WL,
        STM32U5,
    }

    public class DeviceInfo
    {
        [Flags]
        public enum Flag
        {
            F_NO_ME = 1 << 0,   /* Mass-Erase not supported */
            F_OBLL = 1 << 1,    /* OBL_LAUNCH required */
            F_PEMPTY = 1 << 2,  /* clear PEMPTY bit required */
        }

        public ushort DeviceId { get; set; }
        public Series Series { get; set; }
        public string Name { get; set; }
        public int RamStartAddress { get; set; }
        public int RamEndAddress { get; set; }
        public int FlashStartAddress { get; set; }
        public int FlashEndAddress { get; set; }
        public ushort FlashPagePerSector { get; set; }
        public int[] FlashPageSizes { get; set; }
        public int OptionBytesStartAddress { get; set; }
        public int OptionBytesEndAddress { get; set; }
        public int SystemMemoryStartAddress { get; set; }
        public int SystemMemoryEndAddress { get; set; }
        public Flag Flags { get; set; }
        public int[]? UniqueIdAddresses { get; set; }
        public int FlashSizeAddress { get; set; }

        public DeviceInfo(
            ushort deviceId,
            string name,
            int ramStartAddress,
            int ramEndAddress,
            int flashStartAddress,
            int flashEndAddress,
            ushort flashPagePerSector,
            int[] flashPageSizes,
            int optionBytesStartAddress,
            int optionBytesEndAddress,
            int systemMemoryStartAddress,
            int systemMemoryEndAddress,
            Flag flags,
            int[]? uniqueIdAddresses = null,
            int flashSizeAddress = 0,
            Series series = Series.Unknown
            )
        {
            DeviceId = deviceId;
            Name = name;
            RamStartAddress = ramStartAddress;
            RamEndAddress = ramEndAddress;
            FlashStartAddress = flashStartAddress;
            FlashEndAddress = flashEndAddress;
            FlashPagePerSector = flashPagePerSector;
            FlashPageSizes = flashPageSizes;
            OptionBytesStartAddress = optionBytesStartAddress;
            OptionBytesEndAddress = optionBytesEndAddress;
            SystemMemoryStartAddress = systemMemoryStartAddress;
            SystemMemoryEndAddress = systemMemoryEndAddress;
            Flags = flags;
            UniqueIdAddresses = uniqueIdAddresses;
            FlashSizeAddress = flashSizeAddress;
            Series = series;
        }



        #region Built-In List

        const int SZ_128 = 0x00000080;
        const int SZ_256 = 0x00000100;
        const int SZ_1K = 0x00000400;
        const int SZ_2K = 0x00000800;
        const int SZ_4K = 0x00001000;
        const int SZ_8K = 0x00002000;
        const int SZ_16K = 0x00004000;
        const int SZ_32K = 0x00008000;
        const int SZ_64K = 0x00010000;
        const int SZ_128K = 0x00020000;
        const int SZ_256K = 0x00040000;

        /* fixed size pages */
        static readonly int[] p_128 = new int[] { SZ_128 };
        static readonly int[] p_256 = new int[] { SZ_256 };
        static readonly int[] p_1k = new int[] { SZ_1K };
        static readonly int[] p_2k = new int[] { SZ_2K };
        static readonly int[] p_4k = new int[] { SZ_4K };
        static readonly int[] p_8k = new int[] { SZ_8K };
        static readonly int[] p_128k = new int[] { SZ_128K };
        /* F2 and F4 page size */
        static readonly int[] f2f4 = new int[] { SZ_16K, SZ_16K, SZ_16K, SZ_16K, SZ_64K, SZ_128K };
        /* F4 dual bank page size */
        static readonly int[] f4db = new int[]{
            SZ_16K, SZ_16K, SZ_16K, SZ_16K, SZ_64K, SZ_128K, SZ_128K, SZ_128K,
            SZ_16K, SZ_16K, SZ_16K, SZ_16K, SZ_64K, SZ_128K
        };
        /* F7 page size */
        static readonly int[] f7 = new int[] { SZ_32K, SZ_32K, SZ_32K, SZ_32K, SZ_128K, SZ_256K };

        public static readonly DeviceInfo[] BuiltInList;

        static DeviceInfo()
        {
            var list = new DeviceInfo[]
            {
                /* source: https://sourceforge.net/p/stm32flash/code/ci/master/tree/dev_table.c */
                /* source(backup, outdated): https://github.com/ARMinARM/stm32flash/blob/master/dev_table.c */
	                /* ID   "name"                                            SRAM-address-range      FLASH-address-range    PPS  PSize   Option-byte-address-range  System-mem-address-range   Flags */
	                /* C0 */
	                new DeviceInfo(0x443, "STM32C011xx"                     , 0x20001000, 0x20003000, 0x08000000, 0x08008000,  4, p_2k  , 0x1FFF7800, 0x1FFF787F, 0x1FFF0000, 0x1FFF1800, 0),
                /*	new DeviceInfo(0x453, "STM32C031xx"                     , 0x20001000, 0x20001800, 0x08000000, x         ,  x, x     , x         , x         , 0x1FFF0000, 0x1FFF1800, 0), */
	                /* F0 */
	                new DeviceInfo(0x440, "STM32F030x8/F05xxx"              , 0x20000800, 0x20002000, 0x08000000, 0x08010000,  4, p_1k  , 0x1FFFF800, 0x1FFFF80F, 0x1FFFEC00, 0x1FFFF800, 0),
                new DeviceInfo(0x444, "STM32F03xx4/6"                   , 0x20000800, 0x20001000, 0x08000000, 0x08008000,  4, p_1k  , 0x1FFFF800, 0x1FFFF80F, 0x1FFFEC00, 0x1FFFF800, 0),
                new DeviceInfo(0x442, "STM32F030xC/F09xxx"              , 0x20001800, 0x20008000, 0x08000000, 0x08040000,  2, p_2k  , 0x1FFFF800, 0x1FFFF80F, 0x1FFFD800, 0x1FFFF800, Flag.F_OBLL),
                new DeviceInfo(0x445, "STM32F04xxx/F070x6"              , 0x20001800, 0x20001800, 0x08000000, 0x08008000,  4, p_1k  , 0x1FFFF800, 0x1FFFF80F, 0x1FFFC400, 0x1FFFF800, 0),
                new DeviceInfo(0x448, "STM32F070xB/F071xx/F72xx"        , 0x20001800, 0x20004000, 0x08000000, 0x08020000,  2, p_2k  , 0x1FFFF800, 0x1FFFF80F, 0x1FFFC800, 0x1FFFF800, 0),
	                /* F1 */
	                new DeviceInfo(0x412, "STM32F10xxx Low-density"         , 0x20000200, 0x20002800, 0x08000000, 0x08008000,  4, p_1k  , 0x1FFFF800, 0x1FFFF80F, 0x1FFFF000, 0x1FFFF800, 0),
                new DeviceInfo(0x410, "STM32F10xxx Medium-density"      , 0x20000200, 0x20005000, 0x08000000, 0x08020000,  4, p_1k  , 0x1FFFF800, 0x1FFFF80F, 0x1FFFF000, 0x1FFFF800, 0),
                new DeviceInfo(0x414, "STM32F10xxx High-density"        , 0x20000200, 0x20010000, 0x08000000, 0x08080000,  2, p_2k  , 0x1FFFF800, 0x1FFFF80F, 0x1FFFF000, 0x1FFFF800, 0),
                new DeviceInfo(0x420, "STM32F10xxx Medium-density VL"   , 0x20000200, 0x20002000, 0x08000000, 0x08020000,  4, p_1k  , 0x1FFFF800, 0x1FFFF80F, 0x1FFFF000, 0x1FFFF800, 0),
                new DeviceInfo(0x428, "STM32F10xxx High-density VL"     , 0x20000200, 0x20008000, 0x08000000, 0x08080000,  2, p_2k  , 0x1FFFF800, 0x1FFFF80F, 0x1FFFF000, 0x1FFFF800, 0),
                new DeviceInfo(0x418, "STM32F105xx/F107xx"              , 0x20001000, 0x20010000, 0x08000000, 0x08040000,  2, p_2k  , 0x1FFFF800, 0x1FFFF80F, 0x1FFFB000, 0x1FFFF800, 0),
                new DeviceInfo(0x430, "STM32F10xxx XL-density"          , 0x20000800, 0x20018000, 0x08000000, 0x08100000,  2, p_2k  , 0x1FFFF800, 0x1FFFF80F, 0x1FFFE000, 0x1FFFF800, 0),
	                /* F2 */
	                new DeviceInfo(0x411, "STM32F2xxxx"                     , 0x20002000, 0x20020000, 0x08000000, 0x08100000,  1, f2f4  , 0x1FFFC000, 0x1FFFC00F, 0x1FFF0000, 0x1FFF7800, 0),
	                /* F3 */
	                new DeviceInfo(0x432, "STM32F373xx/F378xx"              , 0x20001400, 0x20008000, 0x08000000, 0x08040000,  2, p_2k  , 0x1FFFF800, 0x1FFFF80F, 0x1FFFD800, 0x1FFFF800, 0),
                new DeviceInfo(0x422, "STM32F302xB(C)/F303xB(C)/F358xx" , 0x20001400, 0x2000A000, 0x08000000, 0x08040000,  2, p_2k  , 0x1FFFF800, 0x1FFFF80F, 0x1FFFD800, 0x1FFFF800, 0),
                new DeviceInfo(0x439, "STM32F301xx/F302x4(6/8)/F318xx"  , 0x20001800, 0x20004000, 0x08000000, 0x08010000,  2, p_2k  , 0x1FFFF800, 0x1FFFF80F, 0x1FFFD800, 0x1FFFF800, 0),
                new DeviceInfo(0x438, "STM32F303x4(6/8)/F334xx/F328xx"  , 0x20001800, 0x20003000, 0x08000000, 0x08010000,  2, p_2k  , 0x1FFFF800, 0x1FFFF80F, 0x1FFFD800, 0x1FFFF800, 0),
                new DeviceInfo(0x446, "STM32F302xD(E)/F303xD(E)/F398xx" , 0x20001800, 0x20010000, 0x08000000, 0x08080000,  2, p_2k  , 0x1FFFF800, 0x1FFFF80F, 0x1FFFD800, 0x1FFFF800, 0),
	                /* F4 */
	                new DeviceInfo(0x413, "STM32F40xxx/41xxx"               , 0x20003000, 0x20020000, 0x08000000, 0x08100000,  1, f2f4  , 0x1FFFC000, 0x1FFFC00F, 0x1FFF0000, 0x1FFF7800, 0),
                new DeviceInfo(0x419, "STM32F42xxx/43xxx"               , 0x20003000, 0x20030000, 0x08000000, 0x08200000,  1, f4db  , 0x1FFEC000, 0x1FFFC00F, 0x1FFF0000, 0x1FFF7800, 0),
                new DeviceInfo(0x423, "STM32F401xB(C)"                  , 0x20003000, 0x20010000, 0x08000000, 0x08040000,  1, f2f4  , 0x1FFFC000, 0x1FFFC00F, 0x1FFF0000, 0x1FFF7800, 0),
                new DeviceInfo(0x433, "STM32F401xD(E)"                  , 0x20003000, 0x20018000, 0x08000000, 0x08080000,  1, f2f4  , 0x1FFFC000, 0x1FFFC00F, 0x1FFF0000, 0x1FFF7800, 0),
                new DeviceInfo(0x458, "STM32F410xx"                     , 0x20003000, 0x20008000, 0x08000000, 0x08020000,  1, f2f4  , 0x1FFFC000, 0x1FFFC00F, 0x1FFF0000, 0x1FFF7800, 0),
                new DeviceInfo(0x431, "STM32F411xx"                     , 0x20003000, 0x20020000, 0x08000000, 0x08080000,  1, f2f4  , 0x1FFFC000, 0x1FFFC00F, 0x1FFF0000, 0x1FFF7800, 0),
                new DeviceInfo(0x441, "STM32F412xx"                     , 0x20003000, 0x20040000, 0x08000000, 0x08100000,  1, f2f4  , 0x1FFFC000, 0x1FFFC00F, 0x1FFF0000, 0x1FFF7800, 0),
                new DeviceInfo(0x421, "STM32F446xx"                     , 0x20003000, 0x20020000, 0x08000000, 0x08080000,  1, f2f4  , 0x1FFFC000, 0x1FFFC00F, 0x1FFF0000, 0x1FFF7800, 0),
                new DeviceInfo(0x434, "STM32F469xx/479xx"               , 0x20003000, 0x20060000, 0x08000000, 0x08200000,  1, f4db  , 0x1FFEC000, 0x1FFFC00F, 0x1FFF0000, 0x1FFF7800, 0),
                new DeviceInfo(0x463, "STM32F413xx/423xx"               , 0x20003000, 0x20050000, 0x08000000, 0x08180000,  1, f2f4  , 0x1FFFC000, 0x1FFFC00F, 0x1FFF0000, 0x1FFF7800, 0),
	                /* F7 */
	                new DeviceInfo(0x452, "STM32F72xxx/73xxx"               , 0x20004000, 0x20040000, 0x08000000, 0x08080000,  1, f2f4  , 0x1FFF0000, 0x1FFF001F, 0x1FF00000, 0x1FF0EDC0, 0),
                new DeviceInfo(0x449, "STM32F74xxx/75xxx"               , 0x20004000, 0x20050000, 0x08000000, 0x08100000,  1, f7    , 0x1FFF0000, 0x1FFF001F, 0x1FF00000, 0x1FF0EDC0, 0),
                new DeviceInfo(0x451, "STM32F76xxx/77xxx"               , 0x20004000, 0x20080000, 0x08000000, 0x08200000,  1, f7    , 0x1FFF0000, 0x1FFF001F, 0x1FF00000, 0x1FF0EDC0, 0),
	                /* G0 */
	                new DeviceInfo(0x466, "STM32G03xxx/04xxx"               , 0x20001000, 0x20002000, 0x08000000, 0x08010000,  1, p_2k  , 0x1FFF7800, 0x1FFF787F, 0x1FFF0000, 0x1FFF2000, 0),
                new DeviceInfo(0x460, "STM32G07xxx/08xxx"               , 0x20002700, 0x20009000, 0x08000000, 0x08020000,  1, p_2k  , 0x1FFF7800, 0x1FFF787F, 0x1FFF0000, 0x1FFF7000, 0),
                new DeviceInfo(0x467, "STM32G0B0/B1/C1xx"               , 0x20004000, 0x20020000, 0x08000000, 0x08080000,  1, p_2k  , 0x1FFF7800, 0x1FFF787F, 0x1FFF0000, 0x1FFF7000, 0),
                new DeviceInfo(0x456, "STM32G05xxx/061xx"               , 0x20001000, 0x20004800, 0x08000000, 0x08010000,  1, p_2k  , 0x1FFF7800, 0x1FFF787F, 0x1FFF0000, 0x1FFF2000, 0),
	                /* G4 */
	                new DeviceInfo(0x468, "STM32G431xx/441xx"               , 0x20004000, 0x20005800, 0x08000000, 0x08020000,  1, p_2k  , 0x1FFF7800, 0x1FFF782F, 0x1FFF0000, 0x1FFF7000, 0),
                new DeviceInfo(0x469, "STM32G47xxx/48xxx"               , 0x20004000, 0x20018000, 0x08000000, 0x08080000,  1, p_2k  , 0x1FFF7800, 0x1FFF782F, 0x1FFF0000, 0x1FFF7000, 0),
                new DeviceInfo(0x479, "STM32G491xx/A1xx"                , 0x20004000, 0x2001C000, 0x08000000, 0x08080000,  1, p_2k  , 0x1FFF7800, 0x1FFF782F, 0x1FFF0000, 0x1FFF7000, 0),
	                /* H7 */
	                new DeviceInfo(0x483, "STM32H72xxx/73xxx"               , 0x20004100, 0x20020000, 0x08000000, 0x08100000,  1, p_128k, 0         , 0         , 0x1FF00000, 0x1FF1E800, 0),
                new DeviceInfo(0x450, "STM32H74xxx/75xxx"               , 0x20004100, 0x20020000, 0x08000000, 0x08200000,  1, p_128k, 0         , 0         , 0x1FF00000, 0x1FF1E800, 0),
                new DeviceInfo(0x480, "STM32H7A3xx/B3xx"                , 0x20004100, 0x20020000, 0x08000000, 0x08100000,  1, p_8k  , 0         , 0         , 0x1FF00000, 0x1FF14000, 0),
	                /* L0 */
	                new DeviceInfo(0x457, "STM32L01xxx/02xxx"               , 0x20000800, 0x20000800, 0x08000000, 0x08004000, 32, p_128 , 0x1FF80000, 0x1FF8001F, 0x1FF00000, 0x1FF01000, Flag.F_NO_ME),
                new DeviceInfo(0x425, "STM32L031xx/041xx"               , 0x20001000, 0x20002000, 0x08000000, 0x08008000, 32, p_128 , 0x1FF80000, 0x1FF8001F, 0x1FF00000, 0x1FF01000, Flag.F_NO_ME),
                new DeviceInfo(0x417, "STM32L05xxx/06xxx"               , 0x20001000, 0x20002000, 0x08000000, 0x08010000, 32, p_128 , 0x1FF80000, 0x1FF8001F, 0x1FF00000, 0x1FF01000, Flag.F_NO_ME),
                new DeviceInfo(0x447, "STM32L07xxx/08xxx"               , 0x20002000, 0x20005000, 0x08000000, 0x08030000, 32, p_128 , 0x1FF80000, 0x1FF8001F, 0x1FF00000, 0x1FF02000, Flag.F_NO_ME),
	                /* L1 */
	                new DeviceInfo(0x416, "STM32L1xxx6(8/B)"                , 0x20000800, 0x20004000, 0x08000000, 0x08020000, 16, p_256 , 0x1FF80000, 0x1FF8001F, 0x1FF00000, 0x1FF01000, Flag.F_NO_ME),
                new DeviceInfo(0x429, "STM32L1xxx6(8/B)A"               , 0x20001000, 0x20008000, 0x08000000, 0x08020000, 16, p_256 , 0x1FF80000, 0x1FF8001F, 0x1FF00000, 0x1FF01000, Flag.F_NO_ME),
                new DeviceInfo(0x427, "STM32L1xxxC"                     , 0x20001000, 0x20008000, 0x08000000, 0x08040000, 16, p_256 , 0x1FF80000, 0x1FF8001F, 0x1FF00000, 0x1FF02000, Flag.F_NO_ME),
                new DeviceInfo(0x436, "STM32L1xxxD"                     , 0x20001000, 0x2000C000, 0x08000000, 0x08060000, 16, p_256 , 0x1FF80000, 0x1FF8001F, 0x1FF00000, 0x1FF02000, Flag.F_NO_ME),
                new DeviceInfo(0x437, "STM32L1xxxE"                     , 0x20001000, 0x20014000, 0x08000000, 0x08080000, 16, p_256 , 0x1FF80000, 0x1FF8001F, 0x1FF00000, 0x1FF02000, Flag.F_NO_ME),
	                /* L4 */
	                new DeviceInfo(0x464, "STM32L412xx/422xx"               , 0x20003100, 0x20008000, 0x08000000, 0x08020000,  1, p_2k  , 0x1FFF7800, 0x1FFF780F, 0x1FFF0000, 0x1FFF7000, 0),
                new DeviceInfo(0x435, "STM32L43xxx/44xxx"               , 0x20003100, 0x2000C000, 0x08000000, 0x08040000,  1, p_2k  , 0x1FFF7800, 0x1FFF780F, 0x1FFF0000, 0x1FFF7000, 0),
                new DeviceInfo(0x462, "STM32L45xxx/46xxx"               , 0x20003100, 0x20020000, 0x08000000, 0x08080000,  1, p_2k  , 0x1FFF7800, 0x1FFF780F, 0x1FFF0000, 0x1FFF7000, Flag.F_PEMPTY),
                new DeviceInfo(0x415, "STM32L47xxx/48xxx"               , 0x20003100, 0x20018000, 0x08000000, 0x08100000,  1, p_2k  , 0x1FFF7800, 0x1FFFF80F, 0x1FFF0000, 0x1FFF7000, 0),
                new DeviceInfo(0x461, "STM32L496xx/4A6xx"               , 0x20003100, 0x20040000, 0x08000000, 0x08100000,  1, p_2k  , 0x1FFF7800, 0x1FFFF80F, 0x1FFF0000, 0x1FFF7000, 0),
                new DeviceInfo(0x470, "STM32L4Rxx/4Sxx"                 , 0x20003200, 0x200A0000, 0x08000000, 0x08100000,  1, p_2k  , 0x1FFF7800, 0x1FFFF80F, 0x1FFF0000, 0x1FFF7000, 0),
                new DeviceInfo(0x471, "STM32L4P5xx/Q5xx"                , 0x20004000, 0x20050000, 0x08000000, 0x08100000,  1, p_4k  , 0x1FF00000, 0x1FF0000F, 0x1FFF0000, 0x1FFF7000, 0), /* dual-bank */
	                /* L5 */
	                new DeviceInfo(0x472, "STM32L552xx/562xx"               , 0x20004000, 0x20040000, 0x08000000, 0x08080000,  1, p_2k  , 0         , 0         , 0x0BF90000, 0x0BF98000, 0), /* dual-bank */
	                /* WB */
	                new DeviceInfo(0x494, "STM32WB10xx/15xx"                , 0x20005000, 0x20040000, 0x08000000, 0x08050000,  1, p_2k  , 0x1FFF7800, 0x1FFF787F, 0x1FFF0000, 0x1FFF7000, 0),
                new DeviceInfo(0x495, "STM32WB30(5)xx/50(5)xx"          , 0x20004000, 0x2000C000, 0x08000000, 0x08100000,  1, p_4k  , 0x1FFF8000, 0x1FFF807F, 0x1FFF0000, 0x1FFF7000, 0),
	                /* WL */
	                new DeviceInfo(0x497, "STM32WLE5xx/WL55xx"              , 0x20002000, 0x20010000, 0x08000000, 0x08040000,  1, p_2k  , 0x1FFF7800, 0x1FFF7FFF, 0x1FFF0000, 0x1FFF4000, 0),
	                /* U5 */
	                new DeviceInfo(0x482, "STM32U575xx/585xx"               , 0x20004000, 0x200C0000, 0x08000000, 0x08200000,  1, p_8k  , 0         , 0         , 0x0BF90000, 0x0BFA0000, 0),
	                /* These are not (yet) in AN2606: */
	                new DeviceInfo(0x641, "Medium_Density PL"               , 0x20000200, 0x20005000, 0x08000000, 0x08020000,  4, p_1k  , 0x1FFFF800, 0x1FFFF80F, 0x1FFFF000, 0x1FFFF800, 0),
                new DeviceInfo(0x9a8, "STM32W-128K"                     , 0x20000200, 0x20002000, 0x08000000, 0x08020000,  4, p_1k  , 0x08040800, 0x0804080F, 0x08040000, 0x08040800, 0),
                new DeviceInfo(0x9b0, "STM32W-256K"                     , 0x20000200, 0x20004000, 0x08000000, 0x08040000,  4, p_2k  , 0x08040800, 0x0804080F, 0x08040000, 0x08040800, 0),
	                /* sentinel */
            };
            var slst = Enum.GetValues(typeof(Series))
                .Cast<Series>()
                .ToDictionary(v => v.ToString().ToUpper());
            foreach (var info in list)
            {
                if (info.Name.Length < 7)
                    continue;
                var seriesStr = info.Name[..7].ToUpper();
                if (!slst.TryGetValue(seriesStr, out var series))
                    continue;
                info.Series = series;
                switch (info.Series)
                {
                    case Series.STM32F0:
                    case Series.STM32F3:
                        info.UniqueIdAddresses = new int[] { 0x1FFFF7AC };
                        info.FlashSizeAddress = 0x1FFFF7CC;
                        break;
                    case Series.STM32F1:
                        info.UniqueIdAddresses = new int[] { 0x1FFFF7E8 };
                        info.FlashSizeAddress = 0x1FFFF7E0;
                        break;
                    case Series.STM32F2:
                    case Series.STM32F4:
                        info.UniqueIdAddresses = new int[] { 0x1FFF7A10 };
                        info.FlashSizeAddress = 0x1FFF7A22;
                        break;
                    case Series.STM32F7:
                    case Series.STM32H7:
                        info.UniqueIdAddresses = new int[] { 0x1FF0F420 };
                        info.FlashSizeAddress = 0x1FF0F442;
                        break;
                    case Series.STM32L0:
                        info.UniqueIdAddresses = new int[] { 0x1FF80050 };
                        info.FlashSizeAddress = 0x1FF8007C;
                        break;
                    //case Series.STM32L1:
                    //    info.UniqueIdAddresses = new int[] { 0x1FF80050, 0x1FF80050 + 0x14 };
                    //    info.FlashSizeAddress = 0x1FF8004C; // 0x1FF800CC
                    //    break;
                    case Series.STM32L4:
                        info.UniqueIdAddresses = new int[] { 0x1FFF7590 };
                        info.FlashSizeAddress = 0x1FFF75E0;
                        break;
                }
            }
            BuiltInList = list;
        }

        #endregion
    }


    public static IDictionary<ushort, DeviceInfo> DeviceHardwareInfos { get; set; } = DeviceInfo.BuiltInList.ToDictionary(v => v.DeviceId);

    public class DeviceInfos
    {
        public BootloaderInfo Bootloader { get; }
        public DeviceInfo Device { get; }
        public int FlashSize { get; }
        public DeviceInfos(BootloaderInfo btInfo, DeviceInfo devInfo, int flashSize)
        => (Bootloader, Device, FlashSize) = (btInfo, devInfo, flashSize);
    }

    public const Parity Parity = System.IO.Ports.Parity.Even;
    public const int MinBaudrate = 9600;
    public const int RecommendedBaudrate = 38400;
    public const int MaxBaudrate = 115200;
    public static readonly int[] SupportedBaudrates = new int[]
    {
        9600,
        19200,
        38400,
        115200,
    };

    public enum Command : byte
    {
        Get = 0x00,
        GetVersionAndReadProtectionStatus = 0x01,
        GetChipProductId = 0x02,
        ReadMemory = 0x11,
        Go = 0x21,
        WriteMemory = 0x31,
        Erase = 0x43,
        ExtendedErase = 0x44,
        WriteProtect = 0x63,
        WriteUnprotect = 0x73,
        ReadoutProtect = 0x82,
        ReadoutUnprotect = 0x92,
    }

    public enum SpBytes : byte
    {
        Call = 0x7f,
        ACK = 0x79,
        NACK = 0x1f,
    }

    public enum EraseSupportType
    {
        None = 0,
        Erase,
        ExtendedErase,
        ReadoutUnprotect,
    }

    public static readonly IReadOnlyDictionary<byte, Command> SupportedCommands
    = Enum.GetValues(typeof(Command))
        .Cast<Command>()
        .ToDictionary(v => (byte)v);

    public static Version ParseVersionCode(byte verCode) => new Version(verCode >> 4, verCode & 0xF);

    public class BootloaderInfo
    {
        public byte VersionCode { get; }

        public Version Version => ParseVersionCode(VersionCode);

        public byte[] SupportedRawCommands { get; }

        public Command[] SupportedCommands { get; }

        public bool SupportedExtendedErase { get; }

        public IReadOnlyList<EraseSupportType> EraseSupportTypes { get; }

        public BootloaderInfo(byte versionCode, byte[] supportedRawCommands)
        {
            VersionCode = versionCode;
            SupportedRawCommands = supportedRawCommands;
            SupportedCommands = supportedRawCommands
                .ToDictionary(d => d)
                .Select(k => k.Key)
                .Where(STBootloader.SupportedCommands.ContainsKey)
                .Select(v => STBootloader.SupportedCommands[v])
                .ToArray();
            SupportedExtendedErase = SupportedCommands.Contains(Command.ExtendedErase);
            var eraseTypes = new List<EraseSupportType>();
            if (SupportedCommands.Contains(Command.Erase))
                eraseTypes.Add(EraseSupportType.Erase);
            if (SupportedCommands.Contains(Command.ExtendedErase))
                eraseTypes.Add(EraseSupportType.ExtendedErase);
            if (SupportedCommands.Contains(Command.ReadoutProtect)
                    && SupportedCommands.Contains(Command.ReadoutUnprotect))
                eraseTypes.Add(EraseSupportType.ReadoutUnprotect);
            EraseSupportTypes = eraseTypes;
        }
    }

    public class ReadProtectionStatus
    {
        public byte OptionByte0 { get; }
        public byte OptionByte1 { get; }
        public int ReadoutUnprotectCount => OptionByte0;
        public int ReadoutProtectCount => OptionByte1;
        public bool ReadoutProtectionEnabled => ReadoutProtectCount > ReadoutUnprotectCount;

        public ReadProtectionStatus(byte optionByte1, byte optionByte2)
        {
            OptionByte0 = optionByte1;
            OptionByte1 = optionByte2;
        }
    }

    public enum CheckSumType
    {
        None = 0,
        Xor_Init0x00,
        Xor_Init0xFF,
    }

    public static byte GetCheckSum(IEnumerable<byte> data, int initValue = 0)
    => (byte)data.Aggregate(initValue, (a, b) => a ^ b);

    public readonly struct OnProcessEventArgs
    {
        public int CurrentStep { get; }
        public int TotalStep { get; }

        public OnProcessEventArgs(int curr, int total) => (CurrentStep, TotalStep) = (curr, total);

        public static implicit operator OnProcessEventArgs((int curr, int total) value) => new OnProcessEventArgs(value.curr, value.total);
        public void Deconstruct(out int Current, out int Total) => (Current, Total) = (CurrentStep, TotalStep);
    }

    static readonly byte[] CallBytes = new[] { (byte)SpBytes.Call };
    public static bool Call(ISendAndGetAnswerConfig conf, int timeout = 200)
    {
        var rTimeout = DateTime.Now.AddMilliseconds(timeout);
        conf.Send(CallBytes);
        while (DateTime.Now < rTimeout)
        {
            if (!conf.TryGetAnswerWithFixedTimeout(out var buf, timeout))
                continue;
            if (buf == null || buf.Length < 1)
                continue;
            switch (buf[^1])
            {
                case (byte)SpBytes.ACK:
                case (byte)SpBytes.NACK:
                    return true;
                default:
                    break;
            }
        }
        return false;
    }

    public static bool Send(ISendAndGetAnswerConfig conf, byte[] data, CheckSumType checkSum = CheckSumType.Xor_Init0x00)
    {
        if (checkSum != CheckSumType.None)
            data = data.Append(GetCheckSum(data, checkSum switch
            {
                CheckSumType.Xor_Init0x00 => 0x00,
                CheckSumType.Xor_Init0xFF => 0xFF,
                _ => throw new ArgumentException(),
            })).ToArray();
        conf.ClearReceiveBuffer();
        return conf.Send(data);
    }

    enum WaitAnswerStep
    {
        WaitAck,
        WaitData,
        WaitAfterAck
    }

    // answerLen:
    //      0   无数据
    //      -1  变长数据, 数据中的第1字节为数据长度
    //     其他 定长数据
    public static bool WaitAnswer(
        ISendAndGetAnswerConfig conf,
        out byte[] Answer,
        int answerLen,
        bool waitAck = true,
        int extTimeout = 0,
        bool ackAfterData = true
        )
    {
        Answer = Array.Empty<byte>();
        var rbuf = new Queue<byte>();
        var step = WaitAnswerStep.WaitAck;
        if (!waitAck)
            step = WaitAnswerStep.WaitData;
        var rTimeout = DateTime.Now.AddMilliseconds(200 + extTimeout);
        while (DateTime.Now < rTimeout)
        {
            if (!conf.TryGetAnswer(out var answer, 200 + extTimeout))
                continue;
            if (answer == null || answer.Length < 1)
                continue;
            foreach (var b in answer)
                rbuf.Enqueue(b);
            switch (step)
            {
                default:
                case WaitAnswerStep.WaitAck:
                    while (rbuf.Count > 0)
                    {
                        var ch = rbuf.Dequeue();
                        if (ch == (byte)SpBytes.ACK)
                        {
                            if (answerLen == 0)
                                goto case WaitAnswerStep.WaitAfterAck;
                            goto case WaitAnswerStep.WaitData;
                        }
                        else if (ch == (byte)SpBytes.NACK)
                            return false;
                    }
                    break;
                case WaitAnswerStep.WaitData:
                    step = WaitAnswerStep.WaitData;
                    if (answerLen < 0)
                        answerLen = rbuf.Dequeue() + 1;
                    if (answerLen <= rbuf.Count)
                    {
                        var lst = new List<byte>();
                        for (; answerLen > 0; answerLen--)
                            lst.Add(rbuf.Dequeue());
                        Answer = lst.ToArray();
                        goto case WaitAnswerStep.WaitAfterAck;
                    }
                    break;
                case WaitAnswerStep.WaitAfterAck:
                    step = WaitAnswerStep.WaitAfterAck;
                    if (!ackAfterData)
                        return true;
                    while (rbuf.Count > 0)
                    {
                        var ch = rbuf.Dequeue();
                        if (ch == (byte)SpBytes.ACK)
                            return true;
                        else if (ch == (byte)SpBytes.NACK)
                            return false;
                    }
                    break;
            }
        }
        return false;
    }

    // answerLen:
    //      0   无数据
    //      -1  变长数据, 数据中的第1字节为数据长度
    //     其他 定长数据
    public static bool Exec(
        ISendAndGetAnswerConfig conf,
        byte[] data,
        out byte[] Answer,
        int answerLen,
        int extTimeout = 0,
        bool waitAck = true,
        CheckSumType checkSum = CheckSumType.Xor_Init0x00,
        bool ackAfterData = false
        )
    {
        Answer = Array.Empty<byte>();
        for (var retryCount = -1; retryCount < conf.Retries; retryCount++)
        {
            if (data.Length > 0)
            {
                conf.ClearReceiveBuffer();
                if (!Send(conf, data, checkSum))
                    continue;
            }
            if (WaitAnswer(conf, out Answer, answerLen, waitAck, extTimeout, ackAfterData))
                return true;
        }
        return false;
    }

    // answerLen:
    //      0   无数据
    //      -1  变长数据, 数据中的第1字节为数据长度
    //     其他 定长数据
    public static bool Exec(ISendAndGetAnswerConfig conf, Command cmd, out byte[] Answer, int answerLen, int extTimeout = 0)
    => Exec(conf, new byte[] { (byte)cmd }, out Answer, answerLen, extTimeout, true, CheckSumType.Xor_Init0xFF);

    public static bool GetBootloaderInfo(ISendAndGetAnswerConfig conf, [NotNullWhen(true)] out BootloaderInfo? Info, int extTimeout = 0)
    {
        Info = null;
        if (!Exec(conf, Command.Get, out var data, -1, extTimeout))
            return false;
        Info = new BootloaderInfo(data[0], data.Skip(1).ToArray());
        return true;
    }

    public static bool GetVersionAndReadProtectionStatus(ISendAndGetAnswerConfig conf, [NotNullWhen(true)] out Version? Version, [NotNullWhen(true)] out ReadProtectionStatus? Status, int extTimeout = 0)
    {
        Version = null;
        Status = null;
        if (!Exec(conf, Command.GetVersionAndReadProtectionStatus, out var data, 3, extTimeout))
            return false;
        Version = ParseVersionCode(data[0]);
        Status = new ReadProtectionStatus(data[1], data[2]);
        return true;
    }

    public static bool GetChipProductId(ISendAndGetAnswerConfig conf, out ushort ProductId, out byte[] ProductIdBytes, int extTimeout = 0)
    {
        ProductId = 0;
        if (!Exec(conf, Command.GetChipProductId, out ProductIdBytes, -1, extTimeout))
            return false;
        switch (ProductIdBytes.Length)
        {
            case 1:
                ProductId = ProductIdBytes[0];
                break;
            case 2:
                ProductId = ProductIdBytes.ToStruct<ushort>(Endian.Big);
                break;
            default:
                ProductId = 0xffff;
                break;
        }
        return true;
    }

    public const int MaxReadStepSize = 256;

    public static bool Read(ISendAndGetAnswerConfig conf, int address, int length, out byte[] Data, Action<OnProcessEventArgs>? onProcess = null, int extTimeout = 0)
    {
        Data = Array.Empty<byte>();
        if (address < 1) // >= 0x80000000
            return false;
        // 地址可能需要对齐到128字节才能正常读取
        const int addrAlign = 128;
        // 文档说长度要对齐到4字节, 实际不一定需要对齐
        const int lenAlign = 4;
        var addr = address / addrAlign * addrAlign;
        var rlen = length;
        if (addr != address)
            rlen += address - addr;
        rlen = (rlen + lenAlign - 1) / lenAlign * lenAlign;
        var buf = new List<byte>();
        if (rlen > MaxReadStepSize)
            onProcess?.Invoke((0, rlen));
        for (int offset = 0, plen; offset < rlen; offset += plen)
        {
            plen = Math.Min(rlen - offset, MaxReadStepSize);
            if (!Exec(conf, Command.ReadMemory, out _, 0, extTimeout))
                return false;
            if (!Exec(conf, addr.ToBytes(Endian.Big), out _, 0, extTimeout))
                return false;
            if (!Exec(conf, new byte[] { (byte)(plen - 1) }, out var d, plen, extTimeout, true, CheckSumType.Xor_Init0xFF))
                return false;
            buf.AddRange(d);
            if (rlen > MaxReadStepSize)
                onProcess?.Invoke((offset + plen, rlen));
        }
        Data = buf.Skip(address - addr).Take(length).ToArray();
        return true;
    }

    public static bool ReadInt(ISendAndGetAnswerConfig conf, int address, out int Value, int extTimeout = 0)
    {
        Value = -1;
        if (!Read(conf, address, 4, out var bytes, null, extTimeout))
            return false;
        Value = bytes.ToStruct<int>(Endian.Little);
        return true;
    }

    public static bool ReadShortInt(ISendAndGetAnswerConfig conf, int address, out ushort Value, int extTimeout = 0)
    {
        Value = 0xffff;
        if (!Read(conf, address, 2, out var bytes, null, extTimeout))
            return false;
        Value = bytes.ToStruct<ushort>(Endian.Little);
        return true;
    }

    public static bool GetFlashSize(ISendAndGetAnswerConfig conf, DeviceInfo info, out int FlashSize, int extTimeout = 0)
    => GetFlashSize(conf, info.FlashSizeAddress, out FlashSize, extTimeout);

    public static bool GetFlashSize(ISendAndGetAnswerConfig conf, int address, out int FlashSize, int extTimeout = 0)
    {
        FlashSize = -1;
        if (!ReadShortInt(conf, address, out var sz, extTimeout))
            return false;
        FlashSize = sz << 10;
        return true;
    }

    public static bool Go(ISendAndGetAnswerConfig conf, int address, int extTimeout = 0)
    {
        if (!Exec(conf, Command.Go, out _, 0, extTimeout))
            return false;
        return Exec(conf, address.ToBytes(Endian.Big), out var buf, 1, extTimeout + 1000) && buf.Length == 1 && buf[0] == (int)SpBytes.ACK;
    }

    public const int DefaultMainFlashStartAddress = 0x08000000;
    public static bool Reset(ISendAndGetAnswerConfig conf, bool useWriteUnprotect = true)
    => useWriteUnprotect ? WriteUnprotect(conf) : Go(conf, DefaultMainFlashStartAddress);
    public static bool Reset(ISendAndGetAnswerConfig conf, DeviceInfo info)
    => Go(conf, info.FlashStartAddress);

    public const byte FillByte = 0xff;
    public const int MaxWriteStepSize = 256;

    public const int WriteAddressAlign = 128;
    public const int WriteSizeAlign = 4;

    public static bool Write(ISendAndGetAnswerConfig conf, int address, IEnumerable<byte> data, int extTimeout = 0, Action<OnProcessEventArgs>? onProcess = null)
    => WriteAligned(conf, address, data.ToArray(), extTimeout, onProcess);
    public static bool WriteAligned(ISendAndGetAnswerConfig conf, int address, byte[] data, int extTimeout = 0, Action<OnProcessEventArgs>? onProcess = null, int addrAlign = WriteAddressAlign, int sizeAlign = WriteSizeAlign)
    {
        var alignedAddr = address / addrAlign * addrAlign;
        var alignedSize = (address - alignedAddr + data.Length).SizeAlignTo(sizeAlign);
        if (alignedAddr != address || alignedSize != data.Length)
        {
            var wbuf = Enumerable.Repeat(FillByte, alignedSize).ToArray();
            Array.Copy(data, 0, wbuf, address - alignedAddr, data.Length);
            data = wbuf;
        }
        if (data.Length > MaxWriteStepSize)
            onProcess?.Invoke((0, data.Length));
        for (int offset = 0, plen; offset < data.Length; offset += plen)
        {
            plen = Math.Min(data.Length - offset, MaxWriteStepSize);
            var buf = data.Subarray(offset, plen);
            var alen = buf.Length.SizeAlignTo(4);
            {
                var buf2 = Enumerable.Repeat(FillByte, alen + 1).ToArray();
                Array.Copy(buf, 0, buf2, 1, buf.Length);
                buf2[0] = (byte)(alen - 1);
                buf = buf2;
            }
            if (!Exec(conf, Command.WriteMemory, out _, 0, extTimeout))
                return false;
            if (!Exec(conf, alignedAddr.ToBytes(Endian.Big), out _, 0, extTimeout))
                return false;
            if (!Exec(conf, buf, out _, 0, extTimeout + 1000))
                return false;
            alignedAddr += alen;
            if (data.Length > MaxWriteStepSize)
                onProcess?.Invoke((offset + plen, data.Length));
        }
        return true;
    }

    public static bool WriteValid(ISendAndGetAnswerConfig conf, int address, IEnumerable<byte> data, int extTimeout = 0, Action<OnProcessEventArgs>? onProcess = null)
    => WriteAlignedValid(conf, address, data.ToArray(), extTimeout, onProcess);
    public static bool WriteAlignedValid(ISendAndGetAnswerConfig conf, int address, byte[] data, int extTimeout = 0, Action<OnProcessEventArgs>? onProcess = null, int addrAlign = WriteAddressAlign, int sizeAlign = WriteSizeAlign)
    {
        if (!WriteAligned(conf, address, data, extTimeout, onProcess, addrAlign, sizeAlign))
            return false;
        if (!Read(conf, address, data.Length, out var vbuf, null, extTimeout))
            return false;
        return data.SequenceEqual(vbuf);
    }

    private static bool Erase(ISendAndGetAnswerConfig conf, IEnumerable<int> pageIndexes, int extTimeout = 0)
    {
        if (!Exec(conf, Command.Erase, out _, 0, extTimeout))
            return false;
        var pages = pageIndexes.Select(v => (byte)v).Distinct().ToList();
        return Exec(conf, pages.Prepend((byte)(pages.Count - 1)).ToArray(), out _, 0, extTimeout);
    }

    private static bool ExtendedErase(ISendAndGetAnswerConfig conf, IEnumerable<int> pageIndexes, int extTimeout = 0)
    {
        if (!Exec(conf, Command.ExtendedErase, out _, 0, extTimeout))
            return false;
        var pages = pageIndexes.Select(v => (ushort)v).Distinct().ToList();
        return Exec(conf, pages.Prepend((ushort)(pages.Count - 1)).SelectMany(v => v.ToBytes(Endian.Big)).ToArray(), out _, 0, extTimeout);
    }

    public enum ExtendedEraseBank
    {
        FullChip = 0xFFFF,
        Bank1 = 0xFFFE,
        Bank2 = 0xFFFD,
    }

    public static bool ExtendedErase(ISendAndGetAnswerConfig conf, ExtendedEraseBank bank, int extTimeout = 0)
    {
        if (!Exec(conf, Command.ExtendedErase, out _, 0, extTimeout))
            return false;
        return Exec(conf, bank.ToBytes(Endian.Big), out _, 0, extTimeout + 500);
    }

    public static bool FullChipErase(ISendAndGetAnswerConfig conf, bool useExtendedErase, int extTimeout = 0)
    {
        if (useExtendedErase)
            return ExtendedErase(conf, ExtendedEraseBank.FullChip, extTimeout);
        else
        {
            if (!Exec(conf, Command.Erase, out _, 0, extTimeout))
                return false;
            return Exec(conf, new byte[] { 0xFF }, out _, 0, extTimeout + 500, true, CheckSumType.Xor_Init0xFF);
        }
    }

    public static bool Erase(ISendAndGetAnswerConfig conf, DeviceInfo info, bool useExtendedErase, int address, int length, Action<OnProcessEventArgs>? onProcess = null, int extTimeout = 0)
    => InnerErase(conf, info, useExtendedErase, address, length, false, onProcess, extTimeout);
    public static bool Erase(ISendAndGetAnswerConfig conf, DeviceInfos info, int address, int length, Action<OnProcessEventArgs>? onProcess = null, int extTimeout = 0)
    => InnerErase(conf, info.Device, info.Bootloader.SupportedExtendedErase, address, length, false, onProcess, extTimeout);

    public static bool SafeErase(ISendAndGetAnswerConfig conf, DeviceInfo info, bool useExtendedErase, int address, int length, Action<OnProcessEventArgs>? onProcess = null, int extTimeout = 0)
    => InnerErase(conf, info, useExtendedErase, address, length, true, onProcess, extTimeout);
    public static bool SafeErase(ISendAndGetAnswerConfig conf, DeviceInfos info, int address, int length, Action<OnProcessEventArgs>? onProcess = null, int extTimeout = 0)
    => InnerErase(conf, info.Device, info.Bootloader.SupportedExtendedErase, address, length, true, onProcess, extTimeout);

    private static bool InnerErase(ISendAndGetAnswerConfig conf, DeviceInfo info, bool useExtendedErase, int address, int length, bool keepAlignData, Action<OnProcessEventArgs>? onProcess = null, int extTimeout = 0)
    {
        var flashStartAddr = info.FlashStartAddress;
        var flashEndAddr = info.FlashEndAddress;
        if (GetFlashSize(conf, info, out var flashSize, extTimeout))
            flashEndAddr = flashStartAddr + flashSize;
        var startAddr = address;
        var endAddr = startAddr + length;
        if (startAddr < flashStartAddr || endAddr > flashEndAddr)
            return false;
        var pageSizes = info.FlashPageSizes.Reverse().SkipWhile(v => v == 0).Reverse().ToArray();
        if (pageSizes.Length < 1)
            return false;
        var beforeBuf = Array.Empty<byte>();
        var afterBuf = Array.Empty<byte>();
        var alignedStartAddr = flashStartAddr;
        var alignedEndAddr = alignedStartAddr;
        List<int> pageIndexes;
        if (pageSizes.Length == 1)
        {
            // 统一的页大小
            var pageSize = pageSizes[0];
            // 对齐开始地址
            alignedStartAddr = (startAddr - flashStartAddr) / pageSize * pageSize + flashStartAddr;
            // 对齐结束地址
            alignedEndAddr = (endAddr - alignedStartAddr + pageSize - 1) / pageSize * pageSize + alignedStartAddr;
            var pageCount = (alignedEndAddr - alignedStartAddr) / pageSize;
            var startPageIndex = (alignedStartAddr - flashStartAddr) / pageSize;
            pageIndexes = Enumerable.Range(startPageIndex, pageCount).ToList();
        }
        else
        {
            // 非统一的页大小
            var startPage = 0;
            int GetFlashPageSize(int pageIndex) => pageSizes[Math.Min(pageSizes.Length-1, pageIndex)];
            while (alignedStartAddr < startAddr)
            {
                var pageSize = GetFlashPageSize(startPage);
                alignedEndAddr = alignedStartAddr + pageSize;
                if (alignedEndAddr > startAddr)
                    break;
                alignedStartAddr += pageSize;
                startPage++;
            }
            var endPage = startPage;
            while (alignedEndAddr < endAddr)
            {
                var pageSize = GetFlashPageSize(endPage);
                alignedEndAddr += pageSize;
                if (alignedEndAddr >= endAddr)
                    break;
                endPage++;
            }
            pageIndexes = Enumerable.Range(startPage, endPage - startPage + 1).ToList();
        }
        var beforeSize = keepAlignData ? (startAddr - alignedStartAddr) : 0;
        var afterSize = keepAlignData ? (alignedEndAddr - endAddr) : 0;
        var stepSizes = new int[]
        {
            beforeSize,
            afterSize,
            pageIndexes.Count,
            beforeSize,
            afterSize
        };
        var stepOffsets = Enumerable.Range(0, stepSizes.Length)
            .Select(i => stepSizes.Take(i).Sum())
            .ToArray();
        var totalSteps = stepSizes.Sum();
        OnProcessEventArgs GetStep(int outerStep, OnProcessEventArgs inner)
        => ((int)((float)inner.CurrentStep / inner.TotalStep * stepSizes[outerStep]) + stepOffsets[outerStep], totalSteps);
        if (keepAlignData && alignedStartAddr != startAddr)
        {
            // 尝试先将对齐后, 前面会被额外擦除的数据读回来
            if (Read(conf, alignedStartAddr, beforeSize, out var cacheBuf, p => onProcess?.Invoke(GetStep(0, p)), extTimeout))
                beforeBuf = cacheBuf;
        }
        if (keepAlignData && alignedEndAddr != endAddr)
        {
            // 尝试先将对齐后, 后面会被额外擦除的数据读回来
            if (Read(conf, endAddr, afterSize, out var cacheBuf, p => onProcess?.Invoke(GetStep(1, p)), extTimeout))
                afterBuf = cacheBuf;
        }
        if (keepAlignData)
            onProcess?.Invoke(GetStep(2, (0, 1)));
        if (useExtendedErase)
        {
            if (!ExtendedErase(conf, pageIndexes, extTimeout))
                return false;
        }
        else if (!Erase(conf, pageIndexes, extTimeout))
            return false;
        if (keepAlignData)
            onProcess?.Invoke(GetStep(2, (1, 1)));
        // 把数据写回去
        if (beforeBuf.Length > 0)
            WriteAligned(conf, alignedStartAddr, beforeBuf, extTimeout, p => onProcess?.Invoke(GetStep(3, p)));
        if (afterBuf.Length > 0)
            WriteAligned(conf, endAddr, afterBuf, extTimeout, p => onProcess?.Invoke(GetStep(4, p)));
        onProcess?.Invoke((totalSteps, totalSteps));
        return true;
    }

    public static bool WriteProtect(ISendAndGetAnswerConfig conf, IEnumerable<int> pageIndexes, int extTimeout = 0)
    {
        if (!Exec(conf, Command.WriteProtect, out _, 0, extTimeout))
            return false;
        var pages = pageIndexes.Select(v => (byte)v).Distinct().ToList();
        return Exec(conf, pages.Prepend((byte)(pages.Count - 1)).ToArray(), out _, 0, extTimeout);
    }

    public static bool WriteUnprotect(ISendAndGetAnswerConfig conf, int extTimeout = 0)
    => Exec(conf, Command.WriteUnprotect, out var buf, 1, extTimeout) && buf.Length == 1 && buf[0] == (int)SpBytes.ACK;

    public static bool ReadoutProtect(ISendAndGetAnswerConfig conf, int extTimeout = 0)
    {
        if (!Exec(conf, Command.ReadoutProtect, out _, 0, extTimeout))
            return false;
        return WaitAnswer(conf, out _, 0);
    }

    public static bool ReadoutUnprotect(ISendAndGetAnswerConfig conf, int extTimeout = 0)
    {
        if (!Exec(conf, Command.ReadoutUnprotect, out _, 0, extTimeout))
            return false;
        return WaitAnswer(conf, out _, 0);
    }

}

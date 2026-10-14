using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Ports;
using System.Linq;
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

    public record DeviceInfo(
        ushort DeviceId,
        string Name,
        int RamStartAddress,
        int RamEndAddress,
        int FlashStartAddress,
        int FlashEndAddress,
        ushort FlashPagePerSector,
        int[] FlashPageSizes,
        int OptionBytesStartAddress,
        int OptionBytesEndAddress,
        int SystemMemoryStartAddress,
        int SystemMemoryEndAddress,
        DeviceInfo.Flag Flags,
        int[]? UniqueIdAddresses = null,
        int FlashSizeAddress = 0,
        Series Series = Series.Unknown
            )
    {
        [Flags]
        public enum Flag
        {
            F_NO_ME = 1 << 0,   /* Mass-Erase not supported */
            F_OBLL = 1 << 1,    /* OBL_LAUNCH required */
            F_PEMPTY = 1 << 2,  /* clear PEMPTY bit required */
        }

        public ushort DeviceId { get; set; } = DeviceId;
        public Series Series { get; set; } = Series;
        public string Name { get; set; } = Name;
        public int RamStartAddress { get; set; } = RamStartAddress;
        public int RamEndAddress { get; set; } = RamEndAddress;
        public int FlashStartAddress { get; set; } = FlashStartAddress;
        public int FlashEndAddress { get; set; } = FlashEndAddress;
        public ushort FlashPagePerSector { get; set; } = FlashPagePerSector;
        public int[] FlashPageSizes { get; set; } = FlashPageSizes;
        public int OptionBytesStartAddress { get; set; } = OptionBytesStartAddress;
        public int OptionBytesEndAddress { get; set; } = OptionBytesEndAddress;
        public int SystemMemoryStartAddress { get; set; } = SystemMemoryStartAddress;
        public int SystemMemoryEndAddress { get; set; } = SystemMemoryEndAddress;
        public Flag Flags { get; set; } = Flags;
        public int[]? UniqueIdAddresses { get; set; } = UniqueIdAddresses;
        public int FlashSizeAddress { get; set; } = FlashSizeAddress;



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
        static readonly int[] p_128 = [SZ_128];
        static readonly int[] p_256 = [SZ_256];
        static readonly int[] p_1k = [SZ_1K];
        static readonly int[] p_2k = [SZ_2K];
        static readonly int[] p_4k = [SZ_4K];
        static readonly int[] p_8k = [SZ_8K];
        static readonly int[] p_128k = [SZ_128K];
        /* F2 and F4 page size */
        static readonly int[] f2f4 = [SZ_16K, SZ_16K, SZ_16K, SZ_16K, SZ_64K, SZ_128K];
        /* F4 dual bank page size */
        static readonly int[] f4db = [
            SZ_16K, SZ_16K, SZ_16K, SZ_16K, SZ_64K, SZ_128K, SZ_128K, SZ_128K,
            SZ_16K, SZ_16K, SZ_16K, SZ_16K, SZ_64K, SZ_128K
        ];
        /* F7 page size */
        static readonly int[] f7 = [SZ_32K, SZ_32K, SZ_32K, SZ_32K, SZ_128K, SZ_256K];

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
            var slst = Enum.GetValues<Series>()
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
                        info.UniqueIdAddresses = [0x1FFFF7AC];
                        info.FlashSizeAddress = 0x1FFFF7CC;
                        break;
                    case Series.STM32F1:
                        info.UniqueIdAddresses = [0x1FFFF7E8];
                        info.FlashSizeAddress = 0x1FFFF7E0;
                        break;
                    case Series.STM32F2:
                    case Series.STM32F4:
                        info.UniqueIdAddresses = [0x1FFF7A10];
                        info.FlashSizeAddress = 0x1FFF7A22;
                        break;
                    case Series.STM32F7:
                    case Series.STM32H7:
                        info.UniqueIdAddresses = [0x1FF0F420];
                        info.FlashSizeAddress = 0x1FF0F442;
                        break;
                    case Series.STM32L0:
                        info.UniqueIdAddresses = [0x1FF80050];
                        info.FlashSizeAddress = 0x1FF8007C;
                        break;
                    //case Series.STM32L1:
                    //    info.UniqueIdAddresses = new int[] { 0x1FF80050, 0x1FF80050 + 0x14 };
                    //    info.FlashSizeAddress = 0x1FF8004C; // 0x1FF800CC
                    //    break;
                    case Series.STM32L4:
                        info.UniqueIdAddresses = [0x1FFF7590];
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
    public const int DataBits = 8;
    public const StopBits StopBits = System.IO.Ports.StopBits.One;
    public const int MinBaudrate = 9600;
    public const int RecommendedBaudrate = 38400;
    public const int MaxBaudrate = 115200;
    public static readonly int[] SupportedBaudrates =
    [
        9600,
        19200,
        38400,
        115200,
    ];

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
    = Enum.GetValues<Command>()
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
            SupportedCommands = [.. supportedRawCommands
                .ToDictionary(d => d)
                .Select(k => k.Key)
                .Where(STBootloader.SupportedCommands.ContainsKey)
                .Select(v => STBootloader.SupportedCommands[v])];
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

    public record ReadProtectionStatus(byte OptionByte0, byte OptionByte1)
    {
        public int ReadoutUnprotectCount => OptionByte0;
        public int ReadoutProtectCount => OptionByte1;
        public bool ReadoutProtectionEnabled => ReadoutProtectCount > ReadoutUnprotectCount;

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

    static readonly byte[] CallBytes = [(byte)SpBytes.Call];
    const int OptionalSecondNackDrainTimeout = 20;

    enum WaitAnswerStep
    {
        WaitAck,
        WaitData,
        WaitAfterAck,
    }

    static void ValidateConfig(ISendAndGetAnswerConfig conf)
    {
        if (conf == null)
            throw new ArgumentNullException(nameof(conf));
    }

    static void ValidateTimeout(int timeout, string paramName)
    {
        if (timeout < 0)
            throw new ArgumentOutOfRangeException(paramName);
    }

    static void ValidateAddressAndLength(int address, int length)
    {
        if (address < 0)
            throw new ArgumentOutOfRangeException(nameof(address));
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));
        if ((long)address + length > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(length), "Address range exceeds Int32.MaxValue.");
    }

    static void DrainOptionalSecondNack(ISendAndGetAnswerConfig conf)
    {
        // Some STM32 products can send a second NACK while RDP is active.
        // Drain it before another transaction can mistake it for its response.
        conf.TryGetAnswerWithFixedTimeout(out _, OptionalSecondNackDrainTimeout);
        conf.ClearReceiveBuffer();
    }

    public static bool Call(ISendAndGetAnswerConfig conf, int timeout = 200)
    {
        ValidateConfig(conf);
        ValidateTimeout(timeout, nameof(timeout));
        conf.ClearReceiveBuffer();
        if (!conf.Send(CallBytes))
            return false;

        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);
        do
        {
            if (!conf.TryGetAnswerWithFixedTimeout(out var buffer, timeout))
                return false;
            foreach (var value in buffer)
            {
                if (value == (byte)SpBytes.ACK)
                    return true;
                if (value == (byte)SpBytes.NACK)
                {
                    DrainOptionalSecondNack(conf);
                    return false;
                }
            }
        }
        while (DateTime.UtcNow <= deadline);
        return false;
    }

    static bool Send(ISendAndGetAnswerConfig conf, byte[] data, CheckSumType checkSum)
    {
        if (checkSum != CheckSumType.None)
        {
            var initialValue = checkSum switch
            {
                CheckSumType.Xor_Init0x00 => 0x00,
                CheckSumType.Xor_Init0xFF => 0xFF,
                _ => throw new ArgumentOutOfRangeException(nameof(checkSum)),
            };
            data = [.. data.Append(GetCheckSum(data, initialValue))];
        }
        return conf.Send(data);
    }

    // answerLen:
    //      0   无数据
    //      -1  变长数据, 第1字节N表示后续数据长度减1
    //     其他 定长数据
    static bool WaitAnswer(
        ISendAndGetAnswerConfig conf,
        out byte[] answer,
        int answerLen,
        int extTimeout,
        bool ackAfterData
        )
    {
        answer = [];
        var receiveBuffer = new Queue<byte>();
        var step = WaitAnswerStep.WaitAck;
        var dataLength = answerLen;
        var variableLengthRead = answerLen < 0;
        var deadline = DateTime.UtcNow.AddMilliseconds((long)conf.Timeout + extTimeout);

        do
        {
            if (!conf.TryGetAnswer(out var received, extTimeout))
                return false;
            if (received != null)
            {
                foreach (var value in received)
                    receiveBuffer.Enqueue(value);
            }

            while (receiveBuffer.Count > 0)
            {
                switch (step)
                {
                    case WaitAnswerStep.WaitAck:
                    {
                        var value = receiveBuffer.Dequeue();
                        if (value == (byte)SpBytes.NACK)
                        {
                            DrainOptionalSecondNack(conf);
                            return false;
                        }
                        if (value != (byte)SpBytes.ACK)
                            continue;
                        if (answerLen == 0)
                        {
                            if (!ackAfterData)
                                return true;
                            step = WaitAnswerStep.WaitAfterAck;
                        }
                        else
                            step = WaitAnswerStep.WaitData;
                        break;
                    }
                    case WaitAnswerStep.WaitData:
                    {
                        if (variableLengthRead)
                        {
                            dataLength = receiveBuffer.Dequeue() + 1;
                            variableLengthRead = false;
                        }
                        if (receiveBuffer.Count < dataLength)
                            goto NeedMoreData;
                        var data = new byte[dataLength];
                        for (var index = 0; index < data.Length; index++)
                            data[index] = receiveBuffer.Dequeue();
                        answer = data;
                        if (!ackAfterData)
                            return true;
                        step = WaitAnswerStep.WaitAfterAck;
                        break;
                    }
                    case WaitAnswerStep.WaitAfterAck:
                    {
                        var value = receiveBuffer.Dequeue();
                        if (value == (byte)SpBytes.ACK)
                            return true;
                        if (value == (byte)SpBytes.NACK)
                        {
                            DrainOptionalSecondNack(conf);
                            return false;
                        }
                        break;
                    }
                    default:
                        throw new InvalidOperationException();
                }
            }

        NeedMoreData:
            continue;
        }
        while (DateTime.UtcNow <= deadline);
        return false;
    }

    static bool Exec(
        ISendAndGetAnswerConfig conf,
        byte[] data,
        out byte[] answer,
        int answerLen,
        int extTimeout = 0,
        CheckSumType checkSum = CheckSumType.Xor_Init0x00,
        bool ackAfterData = false
        )
    {
        answer = [];
        if (!Send(conf, data, checkSum))
            return false;
        return WaitAnswer(conf, out answer, answerLen, extTimeout, ackAfterData);
    }

    static bool Exec(
        ISendAndGetAnswerConfig conf,
        Command command,
        out byte[] answer,
        int answerLen,
        int extTimeout = 0,
        bool ackAfterData = false
        )
    => Exec(conf, [(byte)command], out answer, answerLen, extTimeout, CheckSumType.Xor_Init0xFF, ackAfterData);

    static bool ExecuteTransaction(ISendAndGetAnswerConfig conf, bool allowRetry, Func<bool> transaction)
    {
        var attemptCount = allowRetry ? (long)Math.Max(0, conf.Retries) + 1 : 1;
        for (long attempt = 0; attempt < attemptCount; attempt++)
        {
            conf.ClearReceiveBuffer();
            if (transaction())
                return true;
        }
        return false;
    }

    public static bool GetBootloaderInfo(ISendAndGetAnswerConfig conf, [NotNullWhen(true)] out BootloaderInfo? Info, int extTimeout = 0)
    {
        ValidateConfig(conf);
        ValidateTimeout(extTimeout, nameof(extTimeout));
        Info = null;
        var data = Array.Empty<byte>();
        if (!ExecuteTransaction(conf, true, () => Exec(conf, Command.Get, out data, -1, extTimeout, true)))
            return false;
        if (data.Length < 1)
            return false;
        Info = new BootloaderInfo(data[0], [.. data.Skip(1)]);
        return true;
    }

    public static bool GetVersionAndReadProtectionStatus(ISendAndGetAnswerConfig conf, [NotNullWhen(true)] out Version? Version, [NotNullWhen(true)] out ReadProtectionStatus? Status, int extTimeout = 0)
    {
        ValidateConfig(conf);
        ValidateTimeout(extTimeout, nameof(extTimeout));
        Version = null;
        Status = null;
        var data = Array.Empty<byte>();
        if (!ExecuteTransaction(conf, true, () => Exec(conf, Command.GetVersionAndReadProtectionStatus, out data, 3, extTimeout, true)))
            return false;
        Version = ParseVersionCode(data[0]);
        Status = new ReadProtectionStatus(data[1], data[2]);
        return true;
    }

    public static bool GetChipProductId(ISendAndGetAnswerConfig conf, out ushort ProductId, out byte[] ProductIdBytes, int extTimeout = 0)
    {
        ValidateConfig(conf);
        ValidateTimeout(extTimeout, nameof(extTimeout));
        ProductId = 0;
        ProductIdBytes = [];
        var productIdBytes = Array.Empty<byte>();
        if (!ExecuteTransaction(conf, true, () => Exec(conf, Command.GetChipProductId, out productIdBytes, -1, extTimeout, true)))
            return false;
        ProductIdBytes = productIdBytes;
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
        ValidateConfig(conf);
        ValidateTimeout(extTimeout, nameof(extTimeout));
        ValidateAddressAndLength(address, length);
        Data = [];
        if (length == 0)
            return true;
        var buf = new List<byte>();
        if (length > MaxReadStepSize)
            onProcess?.Invoke((0, length));
        for (int offset = 0, partLength; offset < length; offset += partLength)
        {
            partLength = Math.Min(length - offset, MaxReadStepSize);
            var partAddress = address + offset;
            var part = Array.Empty<byte>();
            if (!ExecuteTransaction(conf, true, () =>
                Exec(conf, Command.ReadMemory, out _, 0, extTimeout)
                && Exec(conf, partAddress.ToBytes(Endian.Big), out _, 0, extTimeout)
                && Exec(conf, [(byte)(partLength - 1)], out part, partLength, extTimeout, CheckSumType.Xor_Init0xFF)))
                return false;
            buf.AddRange(part);
            if (length > MaxReadStepSize)
                onProcess?.Invoke((offset + partLength, length));
        }
        Data = [.. buf];
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
    {
        if (info == null)
            throw new ArgumentNullException(nameof(info));
        return GetFlashSize(conf, info.FlashSizeAddress, out FlashSize, extTimeout);
    }

    public static bool GetFlashSize(ISendAndGetAnswerConfig conf, int address, out int FlashSize, int extTimeout = 0)
    {
        ValidateAddressAndLength(address, 2);
        FlashSize = -1;
        if (!ReadShortInt(conf, address, out var sz, extTimeout))
            return false;
        FlashSize = sz << 10;
        return true;
    }

    public static bool Go(ISendAndGetAnswerConfig conf, int address, int extTimeout = 0)
    {
        ValidateConfig(conf);
        ValidateTimeout(extTimeout, nameof(extTimeout));
        ValidateAddressAndLength(address, 0);
        return ExecuteTransaction(conf, false, () =>
            Exec(conf, Command.Go, out _, 0, extTimeout)
            && Exec(conf, address.ToBytes(Endian.Big), out _, 0, extTimeout + 1000));
    }

    public const byte FillByte = 0xff;
    public const int MaxWriteStepSize = 256;

    public static bool Write(ISendAndGetAnswerConfig conf, int address, IEnumerable<byte> data, int extTimeout = 0, Action<OnProcessEventArgs>? onProcess = null)
    {
        ValidateConfig(conf);
        ValidateTimeout(extTimeout, nameof(extTimeout));
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        var writeData = data as byte[] ?? [.. data];
        ValidateAddressAndLength(address, writeData.Length);
        if (writeData.Length == 0)
            return true;
        if (writeData.Length % 4 != 0)
            throw new ArgumentException("AN3155 requires the write length to be a multiple of four bytes.", nameof(data));

        if (writeData.Length > MaxWriteStepSize)
            onProcess?.Invoke((0, writeData.Length));
        for (int offset = 0, partLength; offset < writeData.Length; offset += partLength)
        {
            partLength = Math.Min(writeData.Length - offset, MaxWriteStepSize);
            var partAddress = address + offset;
            var packet = new byte[partLength + 1];
            packet[0] = (byte)(partLength - 1);
            Array.Copy(writeData, offset, packet, 1, partLength);
            if (!ExecuteTransaction(conf, true, () =>
                Exec(conf, Command.WriteMemory, out _, 0, extTimeout)
                && Exec(conf, partAddress.ToBytes(Endian.Big), out _, 0, extTimeout)
                && Exec(conf, packet, out _, 0, extTimeout + 1000)))
                return false;
            if (writeData.Length > MaxWriteStepSize)
                onProcess?.Invoke((offset + partLength, writeData.Length));
        }
        return true;
    }

    public static bool WriteValid(ISendAndGetAnswerConfig conf, int address, IEnumerable<byte> data, int extTimeout = 0, Action<OnProcessEventArgs>? onProcess = null)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        var writeData = data as byte[] ?? [.. data];
        if (!Write(conf, address, writeData, extTimeout, onProcess))
            return false;
        if (!Read(conf, address, writeData.Length, out var readData, null, extTimeout))
            return false;
        return writeData.SequenceEqual(readData);
    }

    private static bool Erase(ISendAndGetAnswerConfig conf, IEnumerable<int> pageIndexes, int extTimeout = 0)
    {
        var pages = pageIndexes.Distinct().ToArray();
        if (pages.Length == 0)
            throw new ArgumentException("At least one page is required.", nameof(pageIndexes));
        if (pages.Length > 256 || pages.Any(value => value < 0 || value > byte.MaxValue))
            throw new ArgumentOutOfRangeException(nameof(pageIndexes));
        var packet = pages.Select(value => (byte)value).Prepend((byte)(pages.Length - 1)).ToArray();
        return ExecuteTransaction(conf, true, () =>
            Exec(conf, Command.Erase, out _, 0, extTimeout)
            && Exec(conf, packet, out _, 0, extTimeout));
    }

    private static bool ExtendedErase(ISendAndGetAnswerConfig conf, IEnumerable<int> pageIndexes, int extTimeout = 0)
    {
        var pages = pageIndexes.Distinct().ToArray();
        if (pages.Length == 0)
            throw new ArgumentException("At least one page is required.", nameof(pageIndexes));
        if (pages.Length > ushort.MaxValue + 1L || pages.Any(value => value < 0 || value > ushort.MaxValue))
            throw new ArgumentOutOfRangeException(nameof(pageIndexes));
        var packet = pages
            .Select(value => (ushort)value)
            .Prepend((ushort)(pages.Length - 1))
            .SelectMany(value => value.ToBytes(Endian.Big))
            .ToArray();
        return ExecuteTransaction(conf, true, () =>
            Exec(conf, Command.ExtendedErase, out _, 0, extTimeout)
            && Exec(conf, packet, out _, 0, extTimeout));
    }

    public enum ExtendedEraseBank : ushort
    {
        FullChip = 0xFFFF,
        Bank1 = 0xFFFE,
        Bank2 = 0xFFFD,
    }

    public static bool ExtendedErase(ISendAndGetAnswerConfig conf, ExtendedEraseBank bank, int extTimeout = 0)
    {
        ValidateConfig(conf);
        ValidateTimeout(extTimeout, nameof(extTimeout));
        if (!Enum.IsDefined(typeof(ExtendedEraseBank), bank))
            throw new ArgumentOutOfRangeException(nameof(bank));
        return ExecuteTransaction(conf, true, () =>
            Exec(conf, Command.ExtendedErase, out _, 0, extTimeout)
            && Exec(conf, bank.ToBytes(Endian.Big), out _, 0, extTimeout + 500));
    }

    public static bool FullChipErase(ISendAndGetAnswerConfig conf, bool useExtendedErase, int extTimeout = 0)
    {
        ValidateConfig(conf);
        ValidateTimeout(extTimeout, nameof(extTimeout));
        extTimeout += 2000;
        if (useExtendedErase)
            return ExtendedErase(conf, ExtendedEraseBank.FullChip, extTimeout);
        return ExecuteTransaction(conf, true, () =>
            Exec(conf, Command.Erase, out _, 0, extTimeout)
            && Exec(conf, [0xFF], out _, 0, extTimeout, CheckSumType.Xor_Init0xFF));
    }

    public static bool Erase(ISendAndGetAnswerConfig conf, DeviceInfo info, bool useExtendedErase, int address, int length, Action<OnProcessEventArgs>? onProcess = null, int extTimeout = 0)
    => InnerErase(conf, info, 0, useExtendedErase, address, length, false, onProcess, extTimeout);
    public static bool Erase(ISendAndGetAnswerConfig conf, DeviceInfos info, int address, int length, Action<OnProcessEventArgs>? onProcess = null, int extTimeout = 0)
    {
        if (info == null)
            throw new ArgumentNullException(nameof(info));
        return InnerErase(conf, info.Device, info.FlashSize, info.Bootloader.SupportedExtendedErase, address, length, false, onProcess, extTimeout);
    }

    public static bool SafeErase(ISendAndGetAnswerConfig conf, DeviceInfo info, bool useExtendedErase, int address, int length, Action<OnProcessEventArgs>? onProcess = null, int extTimeout = 0)
    => InnerErase(conf, info, 0, useExtendedErase, address, length, true, onProcess, extTimeout);
    public static bool SafeErase(ISendAndGetAnswerConfig conf, DeviceInfos info, int address, int length, Action<OnProcessEventArgs>? onProcess = null, int extTimeout = 0)
    {
        if (info == null)
            throw new ArgumentNullException(nameof(info));
        return InnerErase(conf, info.Device, info.FlashSize, info.Bootloader.SupportedExtendedErase, address, length, true, onProcess, extTimeout);
    }

    private static bool InnerErase(ISendAndGetAnswerConfig conf, DeviceInfo info, int knownFlashSize, bool useExtendedErase, int address, int length, bool keepAlignData, Action<OnProcessEventArgs>? onProcess, int extTimeout)
    {
        ValidateConfig(conf);
        ValidateTimeout(extTimeout, nameof(extTimeout));
        if (info == null)
            throw new ArgumentNullException(nameof(info));
        ValidateAddressAndLength(address, length);
        if (length == 0)
            return true;

        var flashStartAddr = info.FlashStartAddress;
        var flashEndAddr = info.FlashEndAddress;
        var startAddr = address;
        var endAddr = startAddr + length;
        if (flashStartAddr < 0 || flashEndAddr <= flashStartAddr)
            throw new ArgumentException("Flash address range is invalid.", nameof(info));
        if (startAddr < flashStartAddr || endAddr > flashEndAddr)
            throw new ArgumentOutOfRangeException(nameof(address), "Erase range is outside the device flash.");
        if (knownFlashSize < 0)
            throw new ArgumentOutOfRangeException(nameof(knownFlashSize));
        if (knownFlashSize > 0)
        {
            if ((long)flashStartAddr + knownFlashSize > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(knownFlashSize));
            flashEndAddr = flashStartAddr + knownFlashSize;
        }
        else if (info.FlashSizeAddress > 0 && GetFlashSize(conf, info, out var flashSize, extTimeout))
        {
            if ((long)flashStartAddr + flashSize > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(info));
            flashEndAddr = flashStartAddr + flashSize;
        }
        if (startAddr < flashStartAddr || endAddr > flashEndAddr)
            throw new ArgumentOutOfRangeException(nameof(address), "Erase range is outside the device flash.");
        if (info.FlashPageSizes == null)
            throw new ArgumentException("Flash page sizes are required.", nameof(info));
        var pageSizes = info.FlashPageSizes.AsEnumerable().Reverse().SkipWhile(v => v == 0).Reverse().ToArray();
        if (pageSizes.Length < 1 || pageSizes.Any(value => value <= 0))
            throw new ArgumentException("Flash page sizes must contain positive values.", nameof(info));

        var beforeBuf = Array.Empty<byte>();
        var afterBuf = Array.Empty<byte>();
        var alignedStartAddr = 0;
        var alignedEndAddr = 0;
        var pageIndexes = new List<int>();
        var pageStart = flashStartAddr;
        for (var pageIndex = 0; pageStart < flashEndAddr; pageIndex++)
        {
            var pageSize = pageSizes[Math.Min(pageIndex, pageSizes.Length - 1)];
            var nextPage = Math.Min((long)pageStart + pageSize, flashEndAddr);
            if (startAddr < nextPage && endAddr > pageStart)
            {
                if (pageIndexes.Count == 0)
                    alignedStartAddr = pageStart;
                pageIndexes.Add(pageIndex);
                alignedEndAddr = (int)nextPage;
            }
            pageStart = (int)nextPage;
        }
        if (pageIndexes.Count == 0)
            throw new ArgumentOutOfRangeException(nameof(address));

        var beforeSize = keepAlignData ? (startAddr - alignedStartAddr) : 0;
        var afterSize = keepAlignData ? (alignedEndAddr - endAddr) : 0;
        var totalSteps = beforeSize + afterSize + pageIndexes.Count + beforeSize + afterSize;
        var currentStep = 0;
        onProcess?.Invoke((currentStep, totalSteps));
        if (beforeSize > 0)
        {
            if (!Read(conf, alignedStartAddr, beforeSize, out beforeBuf, null, extTimeout))
                return false;
            currentStep += beforeSize;
            onProcess?.Invoke((currentStep, totalSteps));
        }
        if (afterSize > 0)
        {
            if (!Read(conf, endAddr, afterSize, out afterBuf, null, extTimeout))
                return false;
            currentStep += afterSize;
            onProcess?.Invoke((currentStep, totalSteps));
        }

        if (useExtendedErase)
        {
            if (!ExtendedErase(conf, pageIndexes, extTimeout))
                return false;
        }
        else if (!Erase(conf, pageIndexes, extTimeout))
            return false;
        currentStep += pageIndexes.Count;
        onProcess?.Invoke((currentStep, totalSteps));

        if (keepAlignData && !RestoreEraseBoundaries(
            conf,
            alignedStartAddr,
            startAddr,
            endAddr,
            alignedEndAddr,
            beforeBuf,
            afterBuf,
            extTimeout))
            return false;
        currentStep += beforeSize + afterSize;
        onProcess?.Invoke((currentStep, totalSteps));
        return true;
    }

    static bool RestoreEraseBoundaries(
        ISendAndGetAnswerConfig conf,
        int alignedStartAddress,
        int startAddress,
        int endAddress,
        int alignedEndAddress,
        byte[] beforeData,
        byte[] afterData,
        int extTimeout)
    {
        var ranges = new List<(int Start, int End)>();
        if (beforeData.Length > 0)
            ranges.Add((alignedStartAddress, (int)(((long)startAddress + 3) / 4 * 4)));
        if (afterData.Length > 0)
            ranges.Add((endAddress / 4 * 4, alignedEndAddress));
        if (ranges.Count == 2 && ranges[0].End > ranges[1].Start)
            ranges = [(ranges[0].Start, ranges[1].End)];

        foreach (var range in ranges)
        {
            var restoreData = new byte[range.End - range.Start];
            for (var offset = 0; offset < restoreData.Length; offset++)
            {
                var currentAddress = range.Start + offset;
                if (currentAddress < startAddress)
                    restoreData[offset] = beforeData[currentAddress - alignedStartAddress];
                else if (currentAddress >= endAddress)
                    restoreData[offset] = afterData[currentAddress - endAddress];
                else
                    restoreData[offset] = FillByte;
            }
            if (!WriteValid(conf, range.Start, restoreData, extTimeout))
                return false;
        }
        return true;
    }

    public static bool WriteProtectAndReset(ISendAndGetAnswerConfig conf, IEnumerable<int> pageIndexes, int extTimeout = 0)
    {
        ValidateConfig(conf);
        ValidateTimeout(extTimeout, nameof(extTimeout));
        if (pageIndexes == null)
            throw new ArgumentNullException(nameof(pageIndexes));
        var pages = pageIndexes.Distinct().ToArray();
        if (pages.Length == 0)
            throw new ArgumentException("At least one page is required.", nameof(pageIndexes));
        if (pages.Length > 256 || pages.Any(value => value < 0 || value > byte.MaxValue))
            throw new ArgumentOutOfRangeException(nameof(pageIndexes));
        var packet = pages.Select(value => (byte)value).Prepend((byte)(pages.Length - 1)).ToArray();
        return ExecuteTransaction(conf, false, () =>
            Exec(conf, Command.WriteProtect, out _, 0, extTimeout)
            && Exec(conf, packet, out _, 0, extTimeout));
    }

    public static bool WriteUnprotectAndReset(ISendAndGetAnswerConfig conf, int extTimeout = 0)
    {
        ValidateConfig(conf);
        ValidateTimeout(extTimeout, nameof(extTimeout));
        return ExecuteTransaction(conf, false, () =>
            Exec(conf, Command.WriteUnprotect, out _, 0, extTimeout + 1000, true));
    }

    public static bool ReadoutProtectAndReset(ISendAndGetAnswerConfig conf, int extTimeout = 0)
    {
        ValidateConfig(conf);
        ValidateTimeout(extTimeout, nameof(extTimeout));
        return ExecuteTransaction(conf, false, () =>
            Exec(conf, Command.ReadoutProtect, out _, 0, extTimeout + 1000, true));
    }

    public static bool ReadoutUnprotectAndReset(ISendAndGetAnswerConfig conf, int extTimeout = 0)
    {
        ValidateConfig(conf);
        ValidateTimeout(extTimeout, nameof(extTimeout));
        return ExecuteTransaction(conf, false, () =>
            Exec(conf, Command.ReadoutUnprotect, out _, 0, extTimeout + 2000, true));
    }
}

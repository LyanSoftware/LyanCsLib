using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.Protocol.LyLCDTvBrt;

public class ProgramInfo
{
    public const string Identifier = "LytecCOM";
    public const ushort VersionID = ('B' << 8) | 'R';

    public const int FlashStartAddress = 0x08000000;
    public const int FlashPageSize = 0x800;
    public const int VersionInfoAddress = FlashStartAddress + 0x400;
    public const int ConfigAddress = FlashStartAddress + 0x1000;
    public const int ConfigSize = 0x4000;
    public const int BrightFixTableAddress = FlashStartAddress + 0x10000;
}

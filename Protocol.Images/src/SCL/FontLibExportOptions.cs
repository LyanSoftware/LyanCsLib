using System;
using System.Collections.Generic;
using System.Text;
using Lytec.Common.Text.Encoding;

namespace Lytec.Protocol.Images.SCL
{
    public record FontLibExportOptions(Encoding Encoding, byte Byte1Start, byte Byte1End, byte Byte2Start, byte Byte2End);
}

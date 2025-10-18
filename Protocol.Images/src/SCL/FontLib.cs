using System;
using System.Collections.Generic;
using System.Text;
using Lytec.Image;

namespace Lytec.Protocol.Images.SCL
{
    public record FontLib(FontInfo Info, Encoding Encoding, bool IsSBCS, int Width, int Height, int CharCount, FontLibExportOptions ExportOptions, byte[] Data)
    {
        public bool IsDBCS => !IsSBCS;
        public string GetConfigStr(string fileName)
        {
            fileName = Path.GetFileName(fileName);
            return IsSBCS ? $"{fileName},A,{CharCount}" : $"{fileName},C,{CharCount},{ExportOptions.Byte2Start},{ExportOptions.Byte1Start}";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Lytec.Common.Data;

namespace Lytec.Protocol
{
    partial class ADSCL
    {
        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct RGB565Color
        {
            public ushort Data { get; set; }

            public byte R
            {
                get => (byte)(BitHelper.GetValue(Data, 0, 5) << 3);
                set => Data = (ushort)BitHelper.SetValue(Data, value >> 3, 0, 5);
            }

            public byte G
            {
                get => (byte)(BitHelper.GetValue(Data, 5, 6) << 2);
                set => Data = (ushort)BitHelper.SetValue(Data, value >> 2, 5, 6);
            }

            public byte B
            {
                get => (byte)(BitHelper.GetValue(Data, 11, 5) << 3);
                set => Data = (ushort)BitHelper.SetValue(Data, value >> 3, 11, 5);
            }

            public RGB565Color(ushort data) => Data = data;
            public RGB565Color(byte r, byte g, byte b) => (R, G, B) = (r, g, b);
            public RGB565Color(Rgba8888Color color) : this(color.R, color.G, color.B) { }
            public Rgba8888Color ToRgba8888() => new(R, G, B);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lytec.Common;
using Lytec.Common.Communication;
using Lytec.Common.Data;

namespace Lytec.Protocol.Huidu
{
    public static class LCDSimple
    {
        public const int DefaultNetPort = 10623;
        public const int DefaultSerialPortBaudrate = 9600;

        public static Endian DefaultEndian = Endian.Little;

        public static readonly ISet<ConnectType> SupportedConnectTypes = new HashSet<ConnectType>()
        {
            ConnectType.UDP,
            ConnectType.TCP,
            ConnectType.UART,
        };

        public enum CommandCode : ushort
        {
            ScreenOn = 0x01,
            ScreenOff = 0x02,
            Play = 0x03,
            Pause = 0x04,
            SwitchPlay = 0x05,
            PlayPrev = 0x06,
            PlayNext = 0x07,
            SetAudioVolumn = 0x08,
            AudioMute = 0x09,
            IncAudioVolumn = 0x0A,
            DecAudioVolumn = 0x0B,
            SetBrightness = 0x0C,
            IncBrightness = 0x0D,
            DecBrightness = 0x0E,
            SetPlayMode = 0x0F,
            IsScreenOn = 0x20,
        }

        public enum ErrorCode : ushort
        {
            Timeout = 0xFFFF,
            Ok = 0,
            InvalidDataLength = 1,
            DataValidateFailed = 2,
            UnsupportedCommand = 3,
            InvalidData = 4,
            CommandFailed = 5,
        }

        public static ushort Exec(ISendAndGetAnswerConfig cfg, CommandCode cmd, params byte[] data) => Exec(cfg, cmd, data.AsEnumerable());
        public static ushort Exec(ISendAndGetAnswerConfig cfg, CommandCode cmd, IEnumerable<byte>? data = null)
        {
            var databuf = data?.ToArray() ?? Array.Empty<byte>();
            var buf = new List<byte>(databuf.Length + 4);
            buf.AddRange(((ushort)(databuf.Length + 4)).ToBytes(DefaultEndian));
            buf.AddRange(((ushort)cmd).ToBytes(DefaultEndian));
            buf.AddRange(databuf);
            databuf = buf.ToArray();

            for (var retry = cfg.Retries + 1; retry > 0; retry--)
            {
                if (cfg.SendAndGetAnswer(databuf, out var rcvBuf)
                    && rcvBuf.Length >= 6)
                {
                    var rcvLen = rcvBuf.ToStruct<ushort>(DefaultEndian);
                    if (rcvBuf.Length >= rcvLen)
                    {
                        var rcvCmd = rcvBuf.ToStruct<CommandCode>(2, DefaultEndian);
                        if (rcvCmd == cmd)
                            return rcvBuf.ToStruct<ushort>(4, DefaultEndian);
                    }
                }
            }
            return (ushort)ErrorCode.Timeout;
        }

        public static ushort SetScreenOn(ISendAndGetAnswerConfig cfg, bool on) => Exec(cfg, on ? CommandCode.ScreenOn : CommandCode.ScreenOff);
        public static ushort Play(ISendAndGetAnswerConfig cfg) => Exec(cfg, CommandCode.Play);
        public static ushort Pause(ISendAndGetAnswerConfig cfg) => Exec(cfg, CommandCode.Pause);
        public static ushort SwitchPlay(ISendAndGetAnswerConfig cfg, byte index) => Exec(cfg, CommandCode.SwitchPlay, index);
        public static ushort PlayPrev(ISendAndGetAnswerConfig cfg) => Exec(cfg, CommandCode.PlayPrev);
        public static ushort PlayNext(ISendAndGetAnswerConfig cfg) => Exec(cfg, CommandCode.PlayNext);
        public static ushort SetAudioVolumn(ISendAndGetAnswerConfig cfg, int volumnPercent) => Exec(cfg, CommandCode.SetAudioVolumn, (byte)volumnPercent.LimitToRange(0, 100));
        public static ushort SetAudioVolumn(ISendAndGetAnswerConfig cfg, float volumn) => SetAudioVolumn(cfg, (int)(volumn * 100));
        public static ushort AudioMute(ISendAndGetAnswerConfig cfg) => Exec(cfg, CommandCode.AudioMute);
        public static ushort IncAudioVolumn(ISendAndGetAnswerConfig cfg) => Exec(cfg, CommandCode.IncAudioVolumn);
        public static ushort DecAudioVolumn(ISendAndGetAnswerConfig cfg) => Exec(cfg, CommandCode.DecAudioVolumn);
        public static ushort SetBrightness(ISendAndGetAnswerConfig cfg, int brightnessPercent) => Exec(cfg, CommandCode.SetBrightness, (byte)brightnessPercent.LimitToRange(0, 100));
        public static ushort SetBrightness(ISendAndGetAnswerConfig cfg, float brightness) => SetBrightness(cfg, (int)(brightness * 100));
        public static ushort IncBrightness(ISendAndGetAnswerConfig cfg) => Exec(cfg, CommandCode.IncBrightness);
        public static ushort DecBrightness(ISendAndGetAnswerConfig cfg) => Exec(cfg, CommandCode.DecBrightness);
        public enum PlayMode : byte
        {
            ListLoop = 0,
            ItemLoop = 1,
        }
        public static ushort SetPlayMode(ISendAndGetAnswerConfig cfg, PlayMode mode) => Exec(cfg, CommandCode.SetPlayMode, (byte)mode);
        public static ushort IsScreenOn(ISendAndGetAnswerConfig cfg, out bool? on)
        {
            var code = Exec(cfg, CommandCode.IsScreenOn);
            switch (code)
            {
                case 0x10:
                case 0x11:
                    on = code == 0x11;
                    return (ushort)ErrorCode.Ok;
                default:
                    on = null;
                    return code;
            }
        }
    }
}

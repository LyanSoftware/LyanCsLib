using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Lytec.Common.Serialization;

namespace Lytec.Common.Communication
{
    public interface ISendAndGetAnswerConfig
    {
        int? AddrCode { get; set; }
        int Retries { get; }
        bool IsStream { get; }
        bool Send(byte[] data);
        bool TryGetAnswer(out byte[] data, int extTimeout = 0);
        bool TryGetAnswerWithFixedTimeout(out byte[] data, int timeout = 0);
        void ClearReceiveBuffer();
    }

    public class SendAndGetAnswerConfig : ISendAndGetAnswerConfig
    {
        public int? AddrCode { get; set; }
        public int Retries { get; set; }
        public bool IsStream { get; set; }
        public Func<byte[], bool> Send { get; set; }
        public delegate bool TryGetAnswerFunc(out byte[] data, int extTimeout = 0);
        public delegate bool TryGetAnswerWithFixedTimeoutFunc(out byte[] data, int timeout = 0);
        public TryGetAnswerFunc TryGetAnswer { get; set; }
        public TryGetAnswerWithFixedTimeoutFunc TryGetAnswerWithFixedTimeout { get; set; }
        public Action? ClearReceiveBuffer { get; set; }
        public SendAndGetAnswerConfig()
        {
            Send = _ => false;
            static bool ga(out byte[] data, int extTimeout)
            {
                data = Array.Empty<byte>();
                return false;
            }
            TryGetAnswer = ga;
            TryGetAnswerWithFixedTimeout = ga;
        }
        public SendAndGetAnswerConfig(
            int retries,
            bool isStream,
            Func<byte[], bool> send,
            TryGetAnswerFunc tryGetAnswer,
            TryGetAnswerWithFixedTimeoutFunc tryGetAnswerWithFixedTimeout,
            Action? clearReceiveBuffer = null
            )
        {
            Retries = retries;
            IsStream = isStream;
            Send = send;
            TryGetAnswer = tryGetAnswer;
            TryGetAnswerWithFixedTimeout = tryGetAnswerWithFixedTimeout;
            ClearReceiveBuffer = clearReceiveBuffer;
        }

        bool ISendAndGetAnswerConfig.Send(byte[] data) => Send(data);

        bool ISendAndGetAnswerConfig.TryGetAnswer(out byte[] data, int extTimeout) => TryGetAnswer(out data, extTimeout);

        bool ISendAndGetAnswerConfig.TryGetAnswerWithFixedTimeout(out byte[] data, int timeout) => TryGetAnswerWithFixedTimeout(out data, timeout);

        void ISendAndGetAnswerConfig.ClearReceiveBuffer() => ClearReceiveBuffer?.Invoke();
    }

    public static class SendAndGetAnswerConfigUtils
    {
        public static bool SendAndGetAnswer(this ISendAndGetAnswerConfig conf, byte[] send, out byte[] data, int extTimeout = 0)
        {
            data = Array.Empty<byte>();
            if (!conf.Send(send))
                return false;
            return conf.TryGetAnswer(out data, extTimeout);
        }
    }
}

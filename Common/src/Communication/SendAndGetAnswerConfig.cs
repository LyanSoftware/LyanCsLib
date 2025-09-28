using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Lytec.Common.Serialization;

namespace Lytec.Common.Communication
{
    public interface ISendAndGetAnswerConfig : IDisposable
    {
        int? AddrCode { get; set; }
        int Retries { get; }
        int Timeout { get; }
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
        public int Timeout { get; set; }
        public bool IsStream { get; set; }
        public Func<byte[], bool> Send { get; set; }
        public delegate bool TryGetAnswerFunc(out byte[] data, int timeout = 0);
        public TryGetAnswerFunc TryGetAnswer { get; set; }
        public Action? ClearReceiveBuffer { get; set; }
        public Action? DisposeFunc { get; set; }
        public SendAndGetAnswerConfig()
        {
            Send = _ => false;
            static bool ga(out byte[] data, int timeout)
            {
                data = Array.Empty<byte>();
                return false;
            }
            TryGetAnswer = ga;
        }
        public SendAndGetAnswerConfig(
            int retries,
            int timeout,
            bool isStream,
            Func<byte[], bool> send,
            TryGetAnswerFunc tryGetAnswer,
            Action? clearReceiveBuffer = null,
            Action? dispose = null
            )
        {
            Retries = retries;
            Timeout = timeout;
            IsStream = isStream;
            Send = send;
            TryGetAnswer = tryGetAnswer;
            ClearReceiveBuffer = clearReceiveBuffer;
            DisposeFunc = dispose;
        }
        
        public SendAndGetAnswerConfig(
            int retries,
            int timeout,
            bool isStream,
            Action<byte[]> send,
            TryGetAnswerFunc tryGetAnswer,
            Action? clearReceiveBuffer = null,
            Action? dispose = null
            )
            : this(retries, timeout, isStream, data =>
            {
                send(data);
                return true;
            }, tryGetAnswer, clearReceiveBuffer, dispose)
        { }

        bool ISendAndGetAnswerConfig.Send(byte[] data) => Send(data);

        bool ISendAndGetAnswerConfig.TryGetAnswer(out byte[] data, int extTimeout) => TryGetAnswer(out data, Timeout + extTimeout);

        bool ISendAndGetAnswerConfig.TryGetAnswerWithFixedTimeout(out byte[] data, int timeout) => TryGetAnswer(out data, timeout);

        void ISendAndGetAnswerConfig.ClearReceiveBuffer() => ClearReceiveBuffer?.Invoke();

        public void Dispose() => DisposeFunc?.Invoke();
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

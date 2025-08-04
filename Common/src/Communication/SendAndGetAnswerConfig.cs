using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Lytec.Common.Serialization;

namespace Lytec.Common.Communication
{
    public interface ISendAndGetAnswerConfig
    {
        int Retries { get; }
        bool IsStream { get; }
        bool Send(byte[] data);
        bool TryGetAnswer(out byte[] data, int extTimeout = 0);
    }

    public class SendAndGetAnswerConfig
    {
        public int? AddrCode { get; set; }
        public int Retries { get; set; }
        public bool IsStream { get; set; }
        public Func<byte[], bool> Send { get; set; }
        public delegate bool TryGetAnswerFunc([NotNullWhen(true)] out byte[]? data, int extTimeout = 0);
        public TryGetAnswerFunc TryGetAnswer { get; set; }
        public SendAndGetAnswerConfig()
        {
            Send = _ => false;
            static bool ga([NotNullWhen(true)] out byte[]? data, int extTimeout)
            {
                data = Array.Empty<byte>();
                return false;
            }
            TryGetAnswer = ga;
        }
        public SendAndGetAnswerConfig(int retries, bool isStream, Func<byte[], bool> send, TryGetAnswerFunc tryGetAnswer)
        {
            Retries = retries;
            IsStream = isStream;
            Send = send;
            TryGetAnswer = tryGetAnswer;
        }

        public bool SendAndGetAnswer(byte[] send, [NotNullWhen(true)] out byte[]? data, int extTimeout)
        {
            data = null;
            if (!Send(send))
                return false;
            return TryGetAnswer(out data, extTimeout);
        }
    }
}

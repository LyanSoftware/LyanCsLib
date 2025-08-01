using System;
using System.Collections.Generic;
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
        public int Retries { get; set; }
        public bool IsStream { get; set; }
        public delegate bool SendFunc(byte[] data);
        public SendFunc Send { get; set; }
        public delegate bool TryGetAnswerFunc(out byte[] data, int extTimeout = 0);
        public TryGetAnswerFunc TryGetAnswer { get; set; }
        public SendAndGetAnswerConfig()
        {
            static bool s(byte[] data) => false;
            Send = s;
            static bool ga(out byte[] data, int extTimeout)
            {
                data = Array.Empty<byte>();
                return false;
            }
            TryGetAnswer = ga;
        }
        public SendAndGetAnswerConfig(int retries, bool isStream, SendFunc send, TryGetAnswerFunc tryGetAnswer)
        {
            Retries = retries;
            IsStream = isStream;
            Send = send;
            TryGetAnswer = tryGetAnswer;
        }
    }
}

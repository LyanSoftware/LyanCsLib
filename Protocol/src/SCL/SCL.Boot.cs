using System.Text;

namespace Lytec.Protocol;

public static partial class SCL
{
    public class Boot
    {
        public static Encoding CallEncode => Encoding.ASCII;

        public static readonly byte[] UartCallCmd = CallEncode.GetBytes("\x1blYtEcC?");
        public static readonly byte[] NetCallCmd = CallEncode.GetBytes("\x1blYtEcN?");

        public const char CallAnswerIdChar = '%';
        public const byte CallAnswerIdByte = (byte)CallAnswerIdChar;

        public static readonly ISet<string> AnswersDic = new string[]
        {
            CallAnswerIdChar + "Update!",
            CallAnswerIdChar + "UpdatL!",
            CallAnswerIdChar + "UpdatX!",
        }.ToHashSet();
        public static readonly int CallAnswerMinLength = AnswersDic.Min(s => CallEncode.GetByteCount(s));
        public static readonly int CallAnswerMaxLength = AnswersDic.Max(s => CallEncode.GetByteCount(s));
        public static bool CheckCallAnswer(byte[] data) => data.Length >= CallAnswerMinLength && AnswersDic.Contains(CallEncode.GetString(data));
    }
}

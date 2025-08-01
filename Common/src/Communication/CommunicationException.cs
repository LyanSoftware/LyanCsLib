namespace Lytec.Common.Communication
{
    public class CommunicationException : Exception
    {
        public CommunicationException(string msg) : base(msg) { }
        public CommunicationException(string msg, Exception innerException) : base(msg, innerException) { }
    }
}

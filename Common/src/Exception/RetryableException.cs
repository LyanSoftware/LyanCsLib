namespace Lytec.Common;

public class RetryableException : Exception
{
    public RetryableException() { }
    public RetryableException(string msg) : base(msg) { }
}

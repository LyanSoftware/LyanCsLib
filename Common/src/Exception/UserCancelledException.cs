namespace Lytec.Common;

public class UserCancelledException : Exception
{
    public UserCancelledException() { }
    public UserCancelledException(string msg) : base(msg) { }
}

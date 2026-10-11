namespace OkeyServer.Exceptions;

public class ConnectAccountAPICallException : SystemException
{
    public ConnectAccountAPICallException(string message)
        : base(message) { }
}

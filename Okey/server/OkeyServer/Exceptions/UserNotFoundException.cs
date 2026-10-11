namespace OkeyServer.Exceptions;

public class UserNotFoundException : SystemException
{
    public UserNotFoundException(string message)
        : base(message) { }
}

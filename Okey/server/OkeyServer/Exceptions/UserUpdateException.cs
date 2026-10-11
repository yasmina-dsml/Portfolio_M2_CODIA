namespace OkeyServer.Exceptions;

public class UserUpdateException : SystemException
{
    public UserUpdateException(string message)
        : base(message) { }
}

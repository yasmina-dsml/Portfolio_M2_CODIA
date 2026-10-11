namespace OkeyServer.Exceptions;

public class UserAchievementsNotFoundException : SystemException
{
    public UserAchievementsNotFoundException(string message)
        : base(message) { }
}

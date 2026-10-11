namespace OkeyServer.Lobby.Exception;

using Exception = System.Exception;

/// <summary>
/// Exception levée lorsqu'une salle de jeu n'est pas trouvée.
/// </summary>
public class RoomNotFoundException : Exception
{
    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="RoomNotFoundException"/> avec un message d'erreur spécifié.
    /// </summary>
    /// <param name="message">Le message décrivant l'erreur.</param>
    public RoomNotFoundException(string message)
        : base(message) { }
}

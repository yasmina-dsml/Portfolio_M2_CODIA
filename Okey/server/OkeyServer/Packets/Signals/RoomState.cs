namespace OkeyServer.Packets;

/// <summary>
/// Représente l'état d'une salle de jeu.
/// </summary>
public class RoomState
{
    /// <summary>
    /// Obtient ou définit les données des joueurs dans la salle.
    /// </summary>
    public List<string?>? playerDatas { get; set; }

    public string? roomName { get; set; }
}

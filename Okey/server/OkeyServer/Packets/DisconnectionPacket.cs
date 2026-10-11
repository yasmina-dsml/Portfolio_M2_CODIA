namespace OkeyServer.Packets;

/// <summary>
/// Représente un paquet de données pour la déconnexion d'un joueur.
/// </summary>
public class DisconnectionPacket
{
    /// <summary>
    /// Obtient ou définit le message de déconnexion.
    /// </summary>
    public string? message { get; set; }
}

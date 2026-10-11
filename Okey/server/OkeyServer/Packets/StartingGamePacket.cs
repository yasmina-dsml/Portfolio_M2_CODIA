namespace OkeyServer.Packets;

/// <summary>
/// Représente un paquet de données pour le démarrage du jeu.
/// </summary>
public class StartingGamePacket
{
    /// <summary>
    /// Obtient ou définit la liste des ID des joueurs.
    /// </summary>
    public List<string>? playersList { get; set; }

    /// <summary>
    /// Obtient ou définit la liste des noms d'utilisateur des joueurs.
    /// </summary>
    public List<string>? playersUsernames { get; set; }

    /// <summary>
    /// Obtient ou définit l'ID du joueur.
    /// </summary>
    public string? playerId { get; set; }
}

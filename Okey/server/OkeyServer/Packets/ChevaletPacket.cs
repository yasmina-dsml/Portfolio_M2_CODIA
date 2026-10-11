namespace OkeyServer.Packets;

/// <summary>
/// Représente un paquet de données pour le chevalet d'un joueur.
/// </summary>
public class ChevaletPacket
{
    /// <summary>
    /// Obtient ou définit la première rangée de tuiles sur le chevalet.
    /// </summary>
    public List<string>? PremiereRangee { get; set; }

    /// <summary>
    /// Obtient ou définit la seconde rangée de tuiles sur le chevalet.
    /// </summary>
    public List<string>? SecondeRangee { get; set; }
}

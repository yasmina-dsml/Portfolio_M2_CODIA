namespace OkeyServer.Packets;

/// <summary>
/// Représente un paquet de données pour une tuile jetée.
/// </summary>
public class TuileJeteePacket
{
    /// <summary>
    /// Obtient ou définit la couleur de la tuile.
    /// </summary>
    public string? Couleur { get; set; }

    /// <summary>
    /// Obtient ou définit le numéro de la tuile.
    /// </summary>
    public string? numero { get; set; }

    /// <summary>
    /// Obtient ou définit si la tuile est dans la défausse.
    /// </summary>
    public string? isDefausse { get; set; }

    /// <summary>
    /// Obtient ou définit la position de la tuile.
    /// </summary>
    public int? position { get; set; }
}

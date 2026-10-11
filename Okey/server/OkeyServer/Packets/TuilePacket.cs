namespace OkeyServer.Packets;

/// <summary>
/// Représente un paquet de données pour une tuile.
/// </summary>
public class TuilePacket
{
    /// <summary>
    /// Obtient ou définit la coordonnée X de la tuile.
    /// </summary>
    public string? X { get; set; }

    /// <summary>
    /// Obtient ou définit la coordonnée Y de la tuile.
    /// </summary>
    public string? Y { get; set; }

    /// <summary>
    /// Obtient ou définit une valeur indiquant si la tuile permet de gagner.
    /// </summary>
    public bool? gagner { get; set; }
}

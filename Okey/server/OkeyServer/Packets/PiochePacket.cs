namespace OkeyServer.Packets;

/// <summary>
/// Représente un paquet de données pour les actions de pioche.
/// </summary>
public class PiochePacket
{
    /// <summary>
    /// Obtient ou définit une valeur indiquant si la pioche provient du centre.
    /// </summary>
    public bool? Centre { get; set; }

    /// <summary>
    /// Obtient ou définit une valeur indiquant si la pioche provient de la défausse.
    /// </summary>
    public bool? Defausse { get; set; }
}

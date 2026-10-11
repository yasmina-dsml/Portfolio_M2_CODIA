namespace OkeyServer.Packets;

/// <summary>
/// Représente un signal de paquet de données.
/// </summary>
public class PacketSignal
{
    /// <summary>
    /// Obtient ou définit le message contenu dans le paquet.
    /// </summary>
    public string? message { get; set; }
}

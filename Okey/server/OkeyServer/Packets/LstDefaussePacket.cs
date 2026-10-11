namespace OkeyServer.Packets;

/// <summary>
/// Représente un paquet de données pour la liste de défausse.
/// </summary>
public class LstDefaussePacket
{
    /// <summary>
    /// Obtient ou définit la liste des tuiles dans la défausse.
    /// </summary>
    public List<string>? Defausse { get; set; }
}

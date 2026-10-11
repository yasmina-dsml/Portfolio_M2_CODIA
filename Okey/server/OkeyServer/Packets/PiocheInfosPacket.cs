namespace OkeyServer.Packets;

/// <summary>
/// Représente un paquet de données contenant les informations sur la pioche.
/// </summary>
public class PiocheInfosPacket
{
    /// <summary>
    /// Obtient ou définit la tuile en tête de la pioche.
    /// </summary>
    public TuileJeteePacket? PiocheTete { get; set; }

    /// <summary>
    /// Obtient ou définit la taille de la pioche.
    /// </summary>
    public int PiocheTaille { get; set; }
}

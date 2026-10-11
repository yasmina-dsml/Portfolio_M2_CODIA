namespace OkeyServer.Packets;

using Dtos;

/// <summary>
/// Représente un paquet de données contenant la liste des salles.
/// </summary>
public class RoomsPacket
{
    /// <summary>
    /// Obtient ou définit la liste des salles de jeu.
    /// </summary>
    public List<RoomDto>? ListRooms { get; set; }
}

namespace OkeyServer.Packets.Dtos;

/// <summary>
/// Représente un Data Transfer Object (DTO) pour une salle de jeu.
/// </summary>
public class RoomDto
{
    /// <summary>
    /// Obtient ou définit le nom de la salle.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Obtient ou définit la capacité maximale de la salle.
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Obtient ou définit la liste des ID des joueurs dans la salle.
    /// </summary>
    public List<string>? Players { get; set; }
}

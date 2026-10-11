namespace OkeyServer.Misc;

using System.Collections.Concurrent;
using Lobby;

/// <summary>
/// Interface pour la gestion des salles de jeu.
/// </summary>
public interface IRoomManager
{
    /// <summary>
    /// Événement déclenché lorsqu'un jeu commence.
    /// </summary>
    event Action<string> GameStarted;

    /// <summary>
    /// Tente de rejoindre une salle avec un nom de salle et un ID de joueur spécifiés.
    /// </summary>
    /// <param name="roomName">Le nom de la salle.</param>
    /// <param name="playerId">L'ID du joueur.</param>
    /// <returns>Retourne <c>true</c> si le joueur a réussi à rejoindre la salle ; sinon, <c>false</c>.</returns>
    bool TryJoinRoom(string roomName, string playerId);

    /// <summary>
    /// Quitte la salle spécifiée par le nom de la salle et l'ID du joueur.
    /// </summary>
    /// <param name="roomName">Le nom de la salle.</param>
    /// <param name="playerId">L'ID du joueur.</param>
    void LeaveRoom(string roomName, string playerId);

    /// <summary>
    /// Obtient l'ID de la salle associée à l'ID de connexion spécifié.
    /// </summary>
    /// <param name="connectionId">L'ID de connexion.</param>
    /// <returns>L'ID de la salle associée.</returns>
    string GetRoomIdByConnectionId(string connectionId);

    /// <summary>
    /// Gère la déconnexion d'un joueur en utilisant son ID de connexion.
    /// </summary>
    /// <param name="connectionId">L'ID de connexion du joueur.</param>
    void PlayerDisconnected(string connectionId);

    /// <summary>
    /// Obtient les informations sur les salles sous forme de chaîne de caractères.
    /// </summary>
    /// <returns>Les informations sur les salles.</returns>
    string GetRoomsInfo();

    /// <summary>
    /// Vérifie si une salle est pleine.
    /// </summary>
    /// <param name="roomName">Le nom de la salle.</param>
    /// <returns>Retourne <c>true</c> si la salle est pleine ; sinon, <c>false</c>.</returns>
    public bool IsRoomFull(string roomName);

    /// <summary>
    /// Démarre le jeu pour une salle spécifiée.
    /// </summary>
    /// <param name="roomName">Le nom de la salle.</param>
    public void StartGameForRoom(string roomName);

    /// <summary>
    /// Obtient toutes les salles sous forme de <see cref="ConcurrentBag{Room}"/>.
    /// </summary>
    /// <returns>Un <see cref="ConcurrentBag{Room}"/> contenant toutes les salles.</returns>
    public ConcurrentBag<Room> GetRooms();

    /// <summary>
    /// Obtient une salle par son ID.
    /// </summary>
    /// <param name="id">L'ID de la salle.</param>
    /// <returns>La salle correspondante.</returns>
    public Room GetRoomById(string id);

    /// <summary>
    /// Réinitialise la salle spécifiée par le nom de la salle.
    /// </summary>
    /// <param name="roomName">Le nom de la salle.</param>
    public void ResetRoom(string roomName);

    /// <summary>
    /// Vérifie si une salle est occupée.
    /// </summary>
    /// <param name="roomName">Le nom de la salle.</param>
    /// <returns>Retourne <c>true</c> si la salle est occupée ; sinon, <c>false</c>.</returns>
    public bool IsRoomBusy(string roomName);

    /// <summary>
    /// Obtient la première salle disponible.
    /// </summary>
    /// <returns>Le nom de la première salle disponible.</returns>
    public string GetFirstRoomAvailable();

    public string? CreatePrivateRoom();

    public bool TryJoinPrivateRoom(string roomId, string playerId);
}

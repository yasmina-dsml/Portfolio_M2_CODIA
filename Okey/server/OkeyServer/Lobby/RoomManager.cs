namespace OkeyServer.Misc;

using System.Collections.Concurrent;
using Lobby;

/// <summary>
/// Gère les opérations relatives aux salles de jeu.
/// </summary>
public class RoomManager : IRoomManager
{
    private readonly Dictionary<string, Room> _rooms;
    private ConcurrentBag<Room> roomsAvailable;
    private ConcurrentBag<Room> RoomsBusy;

    private Dictionary<string, Room> _privateRooms;

    private static Random random = new Random();

    /// <summary>
    /// Événement déclenché lorsque le jeu commence dans une salle.
    /// </summary>
    public event Action<string>? GameStarted;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="RoomManager"/>.
    /// </summary>
    public RoomManager()
    {
        this._rooms = new Dictionary<string, Room>();
        this.roomsAvailable = new ConcurrentBag<Room>();
        this.RoomsBusy = new ConcurrentBag<Room>();
        this._privateRooms = new Dictionary<string, Room>();

        this._rooms.Add("room1", new Room("room1"));
        //this._rooms.Add("room2", new Room("room2"));
        //this._rooms.Add("room3", new Room("room3"));

        foreach (var room in this._rooms)
        {
            this.roomsAvailable.Add(room.Value);
        }
    }

    private static string GenerateRandomPossibleRoomName()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(
            Enumerable.Repeat(chars, 5).Select(s => s[random.Next(s.Length)]).ToArray()
        );
    }

    public string? CreatePrivateRoom()
    {
        var roomName = GenerateRandomPossibleRoomName();

        if (
            !this._privateRooms.TryGetValue(roomName, out _)
            && !this._rooms.TryGetValue(roomName, out _)
        )
        {
            this._rooms.Add(roomName, new Room(roomName));
            this._privateRooms.Add(roomName, new Room(roomName));
            return roomName;
        }
        return null;
    }

    public bool TryJoinPrivateRoom(string roomId, string playerId)
    {
        Console.WriteLine("On veut ajouter un joueur");
        var room = this._rooms[roomId];
        if (!room.IsFull())
        {
            room.AddPlayer(playerId);

            if (room.IsFull())
            {
                //this._privateRooms.Remove(roomId);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Obtient toutes les salles disponibles.
    /// </summary>
    /// <returns>Un <see cref="ConcurrentBag{Room}"/> contenant toutes les salles disponibles.</returns>
    public ConcurrentBag<Room> GetRooms() => this.roomsAvailable;

    /// <summary>
    /// Obtient une salle par son ID.
    /// </summary>
    /// <param name="id">L'ID de la salle.</param>
    /// <returns>La salle correspondante.</returns>
    public Room GetRoomById(string id) => this._rooms[id];

    /// <summary>
    /// Vérifie si une salle est pleine.
    /// </summary>
    /// <param name="roomName">Le nom de la salle.</param>
    /// <returns><c>true</c> si la salle est pleine ; sinon, <c>false</c>.</returns>
    public bool IsRoomFull(string roomName) =>
        this._rooms.TryGetValue(roomName, out var room) && room.IsFull();

    /// <summary>
    /// Réinitialise la salle spécifiée par le nom de la salle.
    /// </summary>
    /// <param name="roomName">Le nom de la salle.</param>
    public void ResetRoom(string roomName)
    {
        // On vérifie si la room est priveée --> on la supprime et on annule tout
        if (!roomName.Contains("room", StringComparison.Ordinal))
        {
            this._rooms.Remove(roomName);
        }
        else
        {
            var room = this.GetRoomById(roomName);
            room.Players = new List<string>();
            this.RoomsBusy = new ConcurrentBag<Room>(this.RoomsBusy.Except(new[] { room }));
            this.roomsAvailable.Add(room);
        }
    }

    /// <summary>
    /// Démarre le jeu pour une salle spécifiée.
    /// </summary>
    /// <param name="roomName">Le nom de la salle.</param>
    public void StartGameForRoom(string roomName)
    {
        if (this._rooms.TryGetValue(roomName, out var room) && room.IsFull())
        {
            this.GameStarted?.Invoke(roomName);
        }
    }

    /// <summary>
    /// Vérifie si une salle est occupée.
    /// </summary>
    /// <param name="roomName">Le nom de la salle.</param>
    /// <returns><c>true</c> si la salle est occupée ; sinon, <c>false</c>.</returns>
    public bool IsRoomBusy(string roomName)
    {
        foreach (var roomB in this.RoomsBusy)
        {
            if (roomB.Name.Equals(roomName, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Tente de rejoindre une salle avec un nom de salle et un ID de joueur spécifiés.
    /// </summary>
    /// <param name="roomName">Le nom de la salle.</param>
    /// <param name="playerId">L'ID du joueur.</param>
    /// <returns><c>true</c> si le joueur a réussi à rejoindre la salle ; sinon, <c>false</c>.</returns>
    public bool TryJoinRoom(string roomName, string playerId)
    {
        if (this._rooms.TryGetValue(roomName, out var room) && !room.IsFull())
        {
            room.AddPlayer(playerId);

            if (room.IsFull())
            {
                this.RoomsBusy.Add(room);
                this.roomsAvailable = new ConcurrentBag<Room>(
                    this.roomsAvailable.Except(new[] { room })
                );
            }

            return true;
        }
        return false;
    }

    /// <summary>
    /// Déclenche l'événement de démarrage du jeu pour la salle spécifiée.
    /// </summary>
    /// <param name="roomName">Le nom de la salle.</param>
    private void OnGameStarted(string roomName) => this.GameStarted?.Invoke(roomName);

    /// <summary>
    /// Quitte la salle spécifiée par le nom de la salle et l'ID du joueur.
    /// </summary>
    /// <param name="roomName">Le nom de la salle.</param>
    /// <param name="playerId">L'ID du joueur.</param>
    public void LeaveRoom(string roomName, string playerId)
    {
        if (this._rooms.TryGetValue(roomName, out var room))
        {
            room.RemovePlayer(playerId);
        }
    }

    /// <summary>
    /// Obtient l'ID de la salle associée à l'ID de connexion spécifié.
    /// </summary>
    /// <param name="connectionId">L'ID de connexion.</param>
    /// <returns>L'ID de la salle associée.</returns>
    public string GetRoomIdByConnectionId(string connectionId) =>
        this._rooms.FirstOrDefault(r => r.Value.HasPlayer(connectionId)).Key;

    /// <summary>
    /// Gère la déconnexion d'un joueur en utilisant son ID de connexion.
    /// </summary>
    /// <param name="connectionId">L'ID de connexion du joueur.</param>
    public void PlayerDisconnected(string connectionId)
    {
        foreach (var room in this._rooms.Values)
        {
            room.RemovePlayer(connectionId);
        }
    }

    /// <summary>
    /// Obtient la première salle disponible.
    /// </summary>
    /// <returns>Le nom de la première salle disponible.</returns>
    public string GetFirstRoomAvailable()
    {
        Room? room;
        if (this.roomsAvailable.TryPeek(out room))
        {
            return room.Name;
        }
        return "";
    }

    /// <summary>
    /// Obtient les informations sur les salles sous forme de chaîne de caractères.
    /// </summary>
    /// <returns>Les informations sur les salles.</returns>
    public string GetRoomsInfo() =>
        string.Join(
            "\n",
            this._rooms.Values.Select(r => $"{r.Name}: {r.Players.Count}/{r.Capacity} players")
        );
}

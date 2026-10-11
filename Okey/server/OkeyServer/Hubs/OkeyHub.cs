namespace OkeyServer.Hubs;

using System.Collections.Concurrent;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using Data;
using Microsoft.AspNetCore.SignalR;
using Misc;
using Okey;
using Okey.Game;
using Okey.Joueurs;
using Okey.Tuiles;
using Packets;
using Packets.Dtos;
using Player;
using Security;

/// <summary>
/// Hub de communication entre les clients et le serveur
/// </summary>
public sealed class OkeyHub : Hub
{
    private static readonly int time = 59000;

    private static ConcurrentDictionary<string, PlayerDatas> _connectedUsers =
        new ConcurrentDictionary<string, PlayerDatas>();

    internal static ConcurrentDictionary<string, string> UsersInRooms =
        new ConcurrentDictionary<string, string>();

    private readonly IRoomManager _roomManager;
    private readonly IHubContext<OkeyHub> _hubContext;
    private static readonly char[] Separator = new char[] { ';' };

    //private readonly IServiceScopeFactory _scopeFactory;
    private readonly ServerDbContext _dbContext;

    private ConcurrentDictionary<string, bool> _isPlayerTurn;

    private CancellationTokenSource? _cts;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="OkeyHub"/>.
    /// </summary>
    /// <param name="hubContext">Contexte du hub pour la communication SignalR.</param>
    /// <param name="roomManager">Gestionnaire de salle pour la gestion des salles de jeu.</param>
    /// <param name="dbContext">Contexte de base de données pour les opérations de données du serveur.</param>
    public OkeyHub(
        IHubContext<OkeyHub> hubContext,
        IRoomManager roomManager,
        //IServiceScopeFactory scopeFactory,
        ServerDbContext dbContext
    )
    {
        this._roomManager = roomManager;
        this._hubContext = hubContext;
        this._dbContext = dbContext;
        //this._scopeFactory = scopeFactory;
        this._isPlayerTurn = new ConcurrentDictionary<string, bool>();
    }

    /// <summary>
    /// On ajoute automatiquement le client au groupe Hub,
    /// ce groupe contient tous les clients qui ne sont pas dans une partie
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        await this._hubContext.Groups.AddToGroupAsync(this.Context.ConnectionId, "Hub");
        if (!UsersInRooms.Keys.Any(x => x == this.Context.ConnectionId))
        {
            UsersInRooms.TryAdd(this.Context.ConnectionId, "Hub");
        }
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Récupère le nom d'utilisateur du client.
    /// </summary>
    /// <param name="connectionId">ID de connexion du client.</param>
    private async Task GetUserNameFromClient(string connectionId)
    {
        try
        {
            var username = await this
                ._hubContext.Clients.Client(connectionId)
                .InvokeAsync<string>("UsernameRequest", cancellationToken: CancellationToken.None);

            if (username != null)
            {
                if (username == "")
                {
                    username = "Guest";
                }
                _connectedUsers[this.Context.ConnectionId] = new PlayerDatas(
                    this._dbContext,
                    username
                );
            }
            else
            {
                _connectedUsers[this.Context.ConnectionId] = new PlayerDatas(
                    this._dbContext,
                    "Guest"
                );
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erreur lors de recuperation du username client: {e.Message}");
        }
    }

    /// <summary>
    /// On supprime automatiquement le client au groupe Hub.
    /// </summary>
    /// <param name="exception">Exception causant la déconnexion, le cas échéant.</param>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (!UsersInRooms[this.Context.ConnectionId].Equals("Hub", StringComparison.Ordinal))
        {
            var roomId = this._roomManager.GetRoomIdByConnectionId(this.Context.ConnectionId);

            if (this._roomManager.IsRoomBusy(roomId))
            {
                this._roomManager.ResetRoom(roomId);
                Console.WriteLine($"Room {roomId} reset");
                UsersInRooms.TryRemove(this.Context.ConnectionId, out _);
                await this.LobbyDisconnection(roomId);
                this._roomManager.PlayerDisconnected(this.Context.ConnectionId);
                await this
                    ._hubContext.Clients.Group(roomId)
                    .SendAsync(
                        "GameCancelled",
                        new GameCancelled { playerSource = this.Context.ConnectionId }
                    );
                foreach (var player in this._roomManager.GetRoomById(roomId).GetPlayerIds())
                {
                    if (
                        !player.Equals(this.Context.ConnectionId, StringComparison.Ordinal)
                        && UsersInRooms.TryGetValue(player, out _)
                    )
                    {
                        await this.SendDisconnectionOrder(
                            player,
                            $"{this.Context.ConnectionId} a quitté, fin de partie."
                        );
                    }
                }
                await base.OnDisconnectedAsync(exception);
            }
            else
            {
                if (UsersInRooms.TryRemove(this.Context.ConnectionId, out _))
                {
                    await this.LobbyDisconnection(roomId);
                    this._roomManager.PlayerDisconnected(this.Context.ConnectionId);
                    if (!string.IsNullOrEmpty(roomId))
                    {
                        await this
                            ._hubContext.Clients.Group(roomId)
                            .SendAsync(
                                "ReceiveMessage",
                                new PacketSignal
                                {
                                    message =
                                        $"Player {this.Context.ConnectionId} has left the {roomId} lobby."
                                }
                            );
                    }
                    /*
                    if (!_connectedUsers.TryRemove(this.Context.ConnectionId, out _))
                    {
                        throw new ConnectedUSerDictionnaryRemoveException(
                            "Couldn't remove the user : "
                            + _connectedUsers[this.Context.ConnectionId]
                            + " from the connected users"
                        );
                    }*/
                }
            }
        }
        else
        {
            if (UsersInRooms[this.Context.ConnectionId].Equals("Hub", StringComparison.Ordinal))
            {
                if (UsersInRooms.TryRemove(this.Context.ConnectionId, out var ci))
                {
                    await this
                        ._hubContext.Clients.Group("Hub")
                        .SendAsync(
                            "ReceiveMessage",
                            new PacketSignal
                            {
                                message = $"Player {this.Context.ConnectionId} has left the Hub."
                            }
                        );
                    await this.SendRoomListUpdate();
                    /*
                    if (!_connectedUsers.TryRemove(this.Context.ConnectionId, out _))
                    {
                        throw new ConnectedUSerDictionnaryRemoveException(
                            "Couldn't remove the user : "
                            + _connectedUsers[this.Context.ConnectionId]
                            + " from the connected users"
                        );
                    }*/
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Quand le joueur fourni un JWT Token lie la session de connexion au nom d'utilisateur
    /// Une transmission AccountLinkResult est envoyé au client indiquant comment s'est passée la tentative d'association
    /// </summary>
    /// <param name="token">JWT token</param>
    public async Task ConnectAccount(string token)
    {
        // Premièrement on vérifie la validité du token
        if (JWTCheck.CheckToken(token))
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
            if (securityToken != null)
            {
                // on recupère le champ "given_name" correspondant au username
                var claim = securityToken.Claims.FirstOrDefault(c => c.Type == "given_name");

                if (claim != null)
                {
                    // on récupère le string
                    var username = claim.Value;
                    try
                    {
                        // récuperation et stockage des données utilisateurs dans le dictionnaire concurrent
                        _connectedUsers[this.Context.ConnectionId] = new PlayerDatas(
                            this._dbContext,
                            username
                        );
                        //Console.WriteLine("user : "+ claimValue + " associé à l'id : " + this.Context.ConnectionId );
                        // 0 = bon déroulement
                        await this.Clients.Caller.SendAsync("AccountLinkResult", 0);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        // 4 = exception lors de l'ajout ou de l'envoi du bon déroulement
                        await this.Clients.Caller.SendAsync("AccountLinkResult", 4);
                        throw;
                    }
                }
                else
                {
                    // 3 = l'attribut given_name n'est pas présent dans le token
                    await this.Clients.Caller.SendAsync("AccountLinkResult", 3);
                    // Console.WriteLine("No given_name in the token");
                }
            }
            else
            {
                // 2 = le token a été mal lu ou un token au format invalide à été transmis
                await this.Clients.Caller.SendAsync("AccountLinkResult", 2);
                // Console.WriteLine("Invalid JWT token format.");
            }
        }
        else
        {
            // 1 = la vérification du token à échouée
            await this.Clients.Caller.SendAsync("AccountLinkResult", 1);
        }
    }

    /// <summary>
    /// appel permettant de modifier la photo de profil du client à sa demande
    /// </summary>
    /// <param name="Photo"></param>
    public async Task UpdateAvatar(int Photo)
    {
        await _connectedUsers[this.Context.ConnectionId].UpdateAvatar(this._dbContext, Photo);
    }

    /// <summary>
    /// Envoi un message à tous les membres d'un groupe.
    /// </summary>
    /// <param name="lobbyName">Nom du groupe qui reçoit le message.</param>
    /// <param name="message">Message envoyé à tous les membres du groupe.</param>
    public async Task SendToLobby(string lobbyName, string message)
    {
        try
        {
            await this
                .Clients.Group(lobbyName)
                .SendAsync("ReceiveMessage", new PacketSignal { message = message });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task CreatePrivateRoom()
    {
        await this.GetUserNameFromClient(this.Context.ConnectionId);

        var roomId = this._roomManager.CreatePrivateRoom();

        if (roomId == null)
        {
            await this
                ._hubContext.Clients.Client(this.Context.ConnectionId)
                .SendAsync(
                    "JoinRoomFail",
                    new PacketSignal { message = "Impossible de créer une room priveée l'instant." }
                );
        }
        else
        {
            //Thread.Sleep(1000);
            Console.WriteLine(
                $"Nouvelle room: {roomId} {this._roomManager.GetRoomById(roomId).Name}"
            );

            var success = this._roomManager.TryJoinPrivateRoom(roomId, this.Context.ConnectionId);
            if (success)
            {
                await this._hubContext.Groups.RemoveFromGroupAsync(
                    this.Context.ConnectionId,
                    "Hub"
                );

                await this._hubContext.Groups.AddToGroupAsync(this.Context.ConnectionId, roomId);
                UsersInRooms.TryUpdate(this.Context.ConnectionId, roomId, "Hub");

                var packet = new RoomState();
                packet.playerDatas = new List<string?>();
                packet.roomName = roomId;

                foreach (var player in this._roomManager.GetRoomById(roomId).GetPlayerIds())
                {
                    Console.WriteLine(player);
                    packet.playerDatas.Add(
                        $"{_connectedUsers[player].GetUsername()};{_connectedUsers[player].GetPhoto()};{_connectedUsers[player].GetElo()};{_connectedUsers[player].GetExperience()}"
                    );
                }

                await this._hubContext.Clients.Group(roomId).SendAsync("SendRoomState", packet);
                await this.SendRoomListUpdate();
                if (this._roomManager.IsRoomFull(roomId))
                {
                    this.OnGameStarted(roomId);
                }
            }
            else
            {
                await this
                    ._hubContext.Clients.Client(this.Context.ConnectionId)
                    .SendAsync(
                        "JoinRoomFail",
                        new PacketSignal
                        {
                            message = "Impossible de rejoindre une room, réessayer plus tard"
                        }
                    );
            }
        }
    }

    /// <summary>
    /// Effectue la connexion au lobby si ce lobby est joignable.
    /// </summary>
    public async Task LobbyConnection()
    {
        await this.GetUserNameFromClient(this.Context.ConnectionId);

        var roomId = this._roomManager.GetFirstRoomAvailable();

        if (roomId.Equals("", StringComparison.Ordinal))
        {
            await this
                ._hubContext.Clients.Client(this.Context.ConnectionId)
                .SendAsync(
                    "JoinRoomFail",
                    new PacketSignal { message = "Pas de room disponible pour l'instant." }
                );
        }
        else
        {
            var success = this._roomManager.TryJoinRoom(roomId, this.Context.ConnectionId);
            if (success)
            {
                await this._hubContext.Groups.RemoveFromGroupAsync(
                    this.Context.ConnectionId,
                    "Hub"
                );

                await this._hubContext.Groups.AddToGroupAsync(this.Context.ConnectionId, roomId);
                await this.SendRoomListUpdate();
                UsersInRooms.TryUpdate(this.Context.ConnectionId, roomId, "Hub");

                var packet = new RoomState();
                packet.playerDatas = new List<string?>();
                packet.roomName = roomId;

                foreach (var player in this._roomManager.GetRoomById(roomId).GetPlayerIds())
                {
                    packet.playerDatas.Add(
                        $"{_connectedUsers[player].GetUsername()};{_connectedUsers[player].GetPhoto()};{_connectedUsers[player].GetElo()};{_connectedUsers[player].GetExperience()}"
                    );
                }

                await this._hubContext.Clients.Group(roomId).SendAsync("SendRoomState", packet);

                if (this._roomManager.IsRoomFull(roomId))
                {
                    this.OnGameStarted(roomId);
                }
            }
            else
            {
                await this
                    ._hubContext.Clients.Client(this.Context.ConnectionId)
                    .SendAsync(
                        "JoinRoomFail",
                        new PacketSignal
                        {
                            message = "Impossible de rejoindre une room, réessayer plus tard"
                        }
                    );
            }
        }
    }

    /// <summary>
    /// Déconnecte le client du lobby en question.
    /// </summary>
    /// <param name="lobbyName">Nom du lobby à quitter.</param>
    public async Task LobbyDisconnection(string lobbyName)
    {
        this._roomManager.LeaveRoom(lobbyName, this.Context.ConnectionId);
        await this
            ._hubContext.Clients.Group(lobbyName)
            .SendAsync(
                "ReceiveMessage",
                new PacketSignal { message = $"Player {this.Context.ConnectionId} left the lobby." }
            );
        await this.SendRoomListUpdate();
    }

    public async Task SendResetTimer(string roomId)
    {
        await this._hubContext.Clients.Group(roomId).SendAsync("ResetTimer");
    }

    /// <summary>
    /// Lit les coordonnées à partir d'une chaîne.
    /// </summary>
    /// <param name="str">Chaîne de coordonnées.</param>
    /// <returns>Objet <see cref="Coord"/> représentant les coordonnées.</returns>
    public Coord ReadCoords(string str)
    {
        Console.WriteLine(str);
        var parts = str.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2)
        {
            throw new ArgumentException(
                "La chaîne de coordonnées doit contenir exactement deux parties."
            );
        }

        return new Coord(
            int.Parse(parts[0], CultureInfo.InvariantCulture),
            int.Parse(parts[1], CultureInfo.InvariantCulture)
        );
    }

    /// <summary>
    /// Permet de lancer la partie.
    /// </summary>
    /// <param name="roomName">Nom de la salle.</param>
    private async void OnGameStarted(string roomName) => await this.StartGameForRoom(roomName);

    /// <summary>
    /// Envoie un message à tous les utilisateurs d'un groupe.
    /// </summary>
    /// <param name="roomName">Nom de la salle.</param>
    /// <param name="message">Message à envoyer.</param>
    private async Task BroadCastInRoom(string roomName, PacketSignal message) =>
        await this._hubContext.Clients.Group(roomName).SendAsync("ReceiveMessage", message);

    /// <summary>
    /// Envoie un message privé à un utilisateur.
    /// </summary>
    /// <param name="userId">Nom de l'utilisateur.</param>
    /// <param name="message">Message à envoyer.</param>
    private async Task SendMpToPlayer(string userId, string message) =>
        await this
            ._hubContext.Clients.Client(userId)
            .SendAsync("ReceiveMessage", new PacketSignal { message = message });

    /// <summary>
    /// Envoie la tuile jetée dans le cas où c'est fait automatiquement (délai dépassé par exemple).
    /// </summary>
    /// <param name="userId">Nom d'utilisateur.</param>
    /// <param name="tuile">Tuile jetée automatiquement.</param>
    private async Task SendTuileJeteeToPlayer(string userId, TuilePacket tuile) =>
        await this._hubContext.Clients.Client(userId).SendAsync("TuileJeteeAuto", tuile);

    /// <summary>
    /// Requête de coordonnées.
    /// </summary>
    /// <param name="connectionId">ID de l'utilisateur.</param>
    /// <returns>Coordonnées sous forme de chaîne.</returns>
    private async Task<string> CoordsRequest(string connectionId)
    {
        try
        {
            await this
                ._hubContext.Clients.Client(connectionId)
                .InvokeAsync<string>(
                    "CoordsActionRequest",
                    cancellationToken: CancellationToken.None
                );
        }
        catch (Exception)
        {
            return "FIN";
        }

        return "";
    }

    /// <summary>
    /// Envoie une requête de mise à jour des salles.
    /// </summary>
    private async Task SendRoomsRequest()
    {
        var listToSend = new List<RoomDto>();

        foreach (var room in this._roomManager.GetRooms())
        {
            var r = new RoomDto
            {
                Name = room.Name,
                Capacity = room.Capacity,
                Players = new List<string>(room.Players)
            };
            listToSend.Add(r);
        }

        var roomPack = new RoomsPacket { ListRooms = listToSend };

        await this._hubContext.Clients.Group("Hub").SendAsync("UpdateRoomsRequest", roomPack);
    }

    /// <summary>
    /// Envoie une commande de déconnexion à un utilisateur.
    /// </summary>
    /// <param name="connectionId">ID de l'utilisateur.</param>
    /// <param name="message">Message de déconnexion.</param>
    private async Task SendDisconnectionOrder(string connectionId, string message)
    {
        await this
            ._hubContext.Clients.Client(connectionId)
            .SendAsync("Disconnection", new DisconnectionPacket { message = message });
    }

    /// <summary>
    /// Requête pour obtenir les coordonnées de gain.
    /// </summary>
    /// <param name="connectionId">ID de l'utilisateur.</param>
    /// <returns>Coordonnées ou indication de gain.</returns>
    private async Task<string> CoordsGainRequest(string connectionId)
    {
        Console.WriteLine("Le pb arrive ici");
        var tuile = await this
            ._hubContext.Clients.Client(connectionId)
            .InvokeAsync<TuilePacket>(
                "CoordsGainRequest",
                cancellationToken: CancellationToken.None
            );
        if (tuile.gagner == true)
        {
            return "gagner";
        }

        return tuile.Y + ";" + tuile.X;
    }

    /// <summary>
    /// Requête pour piocher une tuile.
    /// </summary>
    /// <param name="connectionId">ID de l'utilisateur.</param>
    /// <param name="token">Jeton d'annulation.</param>
    /// <returns>Centre ou défausse.</returns>
    public async Task<string> PiochePacketRequest(
        string connectionId,
        CancellationTokenSource token,
        Joueur currentPlayer
    )
    {
        try
        {
            if (this._cts != null)
            {
                token.Token.ThrowIfCancellationRequested();
                var invokeTask = this
                    ._hubContext.Clients.Client(connectionId)
                    .InvokeAsync<PiochePacket>(
                        "PiochePacketRequest",
                        cancellationToken: this._cts.Token
                    );
                var completedTask = await Task.WhenAny(
                    invokeTask,
                    Task.Delay(time, cancellationToken: this._cts.Token)
                );
                token.Token.ThrowIfCancellationRequested();

                if (completedTask != invokeTask)
                {
                    //notifier l'action
                    await this.SendMpToPlayer(
                        connectionId,
                        "Temps écoulé, tuile piochée du centre."
                    );
                    if (this._cts.Token.IsCancellationRequested)
                    {
                        return "FIN";
                    }
                    return "Centrer";
                }

                if (!this._cts.Token.IsCancellationRequested)
                {
                    if (invokeTask.Result.Centre == true && invokeTask.Result.Defausse == false)
                    {
                        if (this._cts.Token.IsCancellationRequested)
                        {
                            return "FIN";
                        }
                        return "Centre";
                    }
                    else if (
                        invokeTask.Result.Centre == false
                        && invokeTask.Result.Defausse == true
                    )
                    {
                        if (this._cts.Token.IsCancellationRequested)
                        {
                            return "FIN";
                        }
                        return "Defausse";
                    }
                    else
                    {
                        throw new ArgumentException("Packet non conforme");
                    }
                }
                //return "FIN";
            }
            return "FIN";
        }
        catch (Exception e)
        {
            Console.WriteLine($"Ok piocherequest annulé {e.Message}");
            await base.OnDisconnectedAsync(new OperationCanceledException());
            return "FIN";
        }
    }

    /// <summary>
    /// Requête pour jeter une tuile.
    /// </summary>
    /// <param name="pl">Joueur qui jette la tuile.</param>
    /// <param name="roomName">Nom de la salle.</param>
    /// <param name="jeu">Instance de jeu.</param>
    /// <param name="token">Jeton d'annulation.</param>
    /// <returns>Coordonnées de la tuile jetée.</returns>
    private async Task<string> JeterRequest(Joueur pl, string roomName, Jeu jeu)
    {
        var invokeTask = this
            ._hubContext.Clients.Client(pl.getName())
            .InvokeAsync<TuilePacket>("JeterRequest", cancellationToken: CancellationToken.None);
        // attend soit la saisie du client, soit les 20sec
        // si le client saisie alors    completedTask <- invokeTask
        // sinon                        completedTask <- Task.Delay()

        var completedTask = await Task.WhenAny(invokeTask, Task.Delay(time));

        if (completedTask == invokeTask) // TODO: if non optimal du tout, discussion nécessaire
        {
            var coordinates = await invokeTask;

            if (coordinates.gagner == true)
            {
                Console.WriteLine($"Vous essayez de gagner {pl?.getName()}");
                if (pl?.VerifSerieChevalet() == true)
                {
                    Console.WriteLine($"Vous essayez de gagner {pl?.getName()}");
                    // Le joueur gagne


                    if (pl != null)
                    {
                        jeu.JeuTermine(pl);
                        await this
                            .Clients.Group(roomName)
                            .SendAsync("PlayerWon", _connectedUsers[pl.getName()].GetUsername());
                        Thread.Sleep(2000);
                        return "";
                    }
                }
                else
                {
                    if (pl != null)
                    {
                        await this.SendMpToPlayer(
                            pl.getName(),
                            "Vous n'avez pas de serie dans votre chevalet !"
                        );
                        var randTuileCoord = pl.GetRandomTuileCoords();
                        var coord = randTuileCoord.getY() + ";" + randTuileCoord.getX();

                        await this.SendTuileJeteeToPlayer(
                            pl.getName(),
                            new TuilePacket
                            {
                                X = randTuileCoord.getY().ToString(CultureInfo.InvariantCulture),
                                Y = randTuileCoord.getX().ToString(CultureInfo.InvariantCulture),
                                gagner = null
                            }
                        );

                        pl?.JeterTuile(
                            this.ReadCoords(randTuileCoord.getY() + ";" + randTuileCoord.getX()),
                            jeu
                        );
                        return "";
                    }
                }
            }
            else
            {
                pl?.JeterTuile(this.ReadCoords(coordinates.Y + ";" + coordinates.X), jeu);
                return "";
            }
        }

        if (completedTask != invokeTask) // c a d le client n'a pas donné les coordonnées pendant les 20sec
        {
            if (pl != null)
            {
                var RandTuileCoord = pl.GetRandomTuileCoords();
                var coord = RandTuileCoord.getY() + ";" + RandTuileCoord.getX();

                await this.SendTuileJeteeToPlayer(
                    pl.getName(),
                    new TuilePacket
                    {
                        X = RandTuileCoord.getY().ToString(CultureInfo.InvariantCulture),
                        Y = RandTuileCoord.getX().ToString(CultureInfo.InvariantCulture),
                        gagner = null
                    }
                );
                pl?.JeterTuile(
                    this.ReadCoords(RandTuileCoord.getY() + ";" + RandTuileCoord.getX()),
                    jeu
                );
                if (pl != null)
                {
                    await this.SendChevalet(pl.getName(), pl);
                }
                return "";
            }
        }
        return "FIN";
    }

    /// <summary>
    /// Requête pour le premier joueur de jeter une tuile (ne pas remplacer).
    /// </summary>
    /// <param name="pl">Premier joueur.</param>
    /// <returns>Coordonnées sous forme y;x.</returns>
    private async Task<string> FirstJeterRequest(Joueur pl)
    {
        try
        {
            var invokeTask = this
                ._hubContext.Clients.Client(pl.getName())
                .InvokeAsync<TuilePacket>(
                    "FirstJeterActionRequest",
                    cancellationToken: CancellationToken.None
                );

            var completedTask = await Task.WhenAny(invokeTask, Task.Delay(time));

            if (completedTask != invokeTask) // c a d le client n'a pas donné les coordonnées pendant les 20sec
            {
                var RandTuileCoord = pl.GetRandomTuileCoords();
                var coord = RandTuileCoord.getY() + ";" + RandTuileCoord.getX();

                //notifier l'action au joueur.
                await this.SendMpToPlayer(
                    pl.getName(),
                    $"Temps écoulé. La tuile ({coord}) a été jetée aléatoirement."
                );

                var tuile = pl.GetChevalet()[RandTuileCoord.getY()][RandTuileCoord.getX()];

                var tuileStringPacket = new TuileJeteePacket
                {
                    Couleur = tuile?.GetCouleur().ToString(),
                    numero = tuile?.GetNum().ToString(),
                    isDefausse = "true",
                    position = 3
                };
                await this
                    ._hubContext.Clients.Client(this.Context.ConnectionId)
                    .SendAsync("ReceiveTuileJete", tuileStringPacket);

                return RandTuileCoord.getY() + ";" + RandTuileCoord.getX(); // return random coords
            }
            else
            {
                var TuileObtenueSend = await invokeTask;
                return TuileObtenueSend.Y + ";" + TuileObtenueSend.X;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"On arrête la partie {e.Message}");
            return "FIN";
        }
    }

    /// <summary>
    /// Envoie le signal du tour à tous les joueurs de la salle.
    /// </summary>
    /// <param name="roomName">Nom de la salle.</param>
    /// <param name="connectionId">ID de l'utilisateur.</param>
    private async Task TourSignalRequest(string roomName, string? connectionId)
    {
        foreach (var player in this._roomManager.GetRoomById(roomName).GetPlayerIds())
        {
            if (!player.Equals(connectionId, StringComparison.Ordinal))
            {
                await this._hubContext.Clients.Client(player).SendAsync("TurnSignal", connectionId);
                //like we sent "TurnSignal" , we send "TimerStartSignal"
                await this._hubContext.Clients.Client(player).SendAsync("TimerStartSignal", player);
            }
            else
            {
                await this._hubContext.Clients.Client(player).SendAsync("YourTurnSignal");
            }
        }
    }

    /// <summary>
    /// Diffuse les informations de la pioche à tous les utilisateurs.
    /// </summary>
    /// <param name="connectionIds">Liste des ID de connexion des utilisateurs.</param>
    /// <param name="jeu">Instance de jeu.</param>
    private async Task SendPiocheInfosToAll(List<string> connectionIds, Jeu jeu)
    {
        var PiocheTete = jeu.GetPiocheHead();
        var PiocheTeteString = new TuileJeteePacket
        {
            numero =
                (PiocheTete != null)
                    ? PiocheTete.GetNum().ToString(CultureInfo.InvariantCulture)
                    : "",
            Couleur = (PiocheTete != null) ? this.FromEnumToString(PiocheTete.GetCouleur()) : "",
            isDefausse = "false"
        };

        foreach (var connectionId in connectionIds)
        {
            try
            {
                await this
                    ._hubContext.Clients.Client(connectionId)
                    .SendAsync(
                        "ReceivePiocheUpdate",
                        new PiocheInfosPacket
                        {
                            PiocheTete = PiocheTeteString,
                            PiocheTaille = jeu.GetPiocheTaille()
                        }
                    );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Erreur lors de l'envoi de la pioche au client {connectionId}: {ex.Message}"
                );
            }
        }
    }

    /// <summary>
    /// Diffuse les informations de la défausse actuelle à tous les utilisateurs.
    /// </summary>
    /// <param name="connectionIds">Liste des ID de connexion des utilisateurs.</param>
    /// <param name="pl">Joueur actuel.</param>
    private async Task SendCurrentDefausseInfosToAll(List<string> connectionIds, Joueur pl)
    {
        var DefausseTete = pl.GetTeteDefausse();
        var DefausseTeteString = new TuileJeteePacket
        {
            numero =
                (DefausseTete != null)
                    ? DefausseTete.GetNum().ToString(CultureInfo.InvariantCulture)
                    : "",
            Couleur =
                (DefausseTete != null) ? this.FromEnumToString(DefausseTete.GetCouleur()) : "",
            isDefausse = "true"
        };

        foreach (var connectionId in connectionIds)
        {
            try
            {
                await this
                    ._hubContext.Clients.Client(connectionId)
                    .SendAsync(
                        "ReceiveDefausseActuelleUpdate",
                        new PiocheInfosPacket
                        {
                            PiocheTete = DefausseTeteString,
                            PiocheTaille = pl.CountDefausse()
                        }
                    );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Erreur lors de l'envoi des infos de la défausse au client {connectionId}: {ex.Message}"
                );
            }
        }
    }

    /// <summary>
    /// Diffuse les informations de la prochaine défausse à tous les utilisateurs.
    /// </summary>
    /// <param name="connectionIds">Liste des ID de connexion des utilisateurs.</param>
    /// <param name="j">Instance de jeu.</param>
    /// <param name="pl">Joueur actuel.</param>
    private async Task SendNextDefausseInfosToAll(List<string> connectionIds, Jeu j, Joueur pl)
    {
        var nextPlayer = j.getNextJoueur(pl);

        var DefausseTete = nextPlayer.GetTeteDefausse();
        var DefausseTeteString = new TuileJeteePacket
        {
            numero =
                (DefausseTete != null)
                    ? DefausseTete.GetNum().ToString(CultureInfo.InvariantCulture)
                    : "",
            Couleur =
                (DefausseTete != null) ? this.FromEnumToString(DefausseTete.GetCouleur()) : "",
            isDefausse = "true"
        };

        foreach (var connectionId in connectionIds)
        {
            try
            {
                await this
                    ._hubContext.Clients.Client(connectionId)
                    .SendAsync(
                        "ReceiveDefausseProchaineUpdate",
                        new PiocheInfosPacket
                        {
                            PiocheTete = DefausseTeteString,
                            PiocheTaille = nextPlayer.CountDefausse()
                        }
                    );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Erreur lors de l'envoi des infos de la prochaine défausse au client {connectionId}: {ex.Message}"
                );
            }
        }
    }

    /// <summary>
    /// Diffuse les informations des défausses des autres joueurs (non actuel et prochain).
    /// </summary>
    /// <param name="connectionIds">Liste des ID de connexion des utilisateurs.</param>
    /// <param name="j">Instance de jeu.</param>
    /// <param name="pl">Joueur actuel.</param>
    private async Task SendAutreDefausseInfosToAll(List<string> connectionIds, Jeu j, Joueur pl)
    {
        Joueur[] autreJoueur =
        {
            j.getNextJoueur(j.getNextJoueur(pl)),
            j.getNextJoueur(j.getNextJoueur(j.getNextJoueur(pl)))
        }; // [avant dernier joueur, dernier joueur]

        foreach (var joueur in autreJoueur)
        {
            var DefausseTete = joueur.GetTeteDefausse();
            var DefausseTeteString = new TuileJeteePacket
            {
                numero =
                    (DefausseTete != null)
                        ? DefausseTete.GetNum().ToString(CultureInfo.InvariantCulture)
                        : "",
                Couleur =
                    (DefausseTete != null) ? this.FromEnumToString(DefausseTete.GetCouleur()) : "",
                isDefausse = "true"
            };

            foreach (var connectionId in connectionIds)
            {
                try
                {
                    await this
                        ._hubContext.Clients.Client(connectionId)
                        .SendAsync(
                            "ReceiveDefausseAutreUpdate",
                            new PiocheInfosPacket
                            {
                                PiocheTete = DefausseTeteString,
                                PiocheTaille = joueur.CountDefausse()
                            }
                        );
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Erreur lors de l'envoi des infos de la défausse de {joueur.getName()} au client {connectionId}: {ex.Message}"
                    );
                }
            }
        }
    }

    /// <summary>
    /// Envoie les chevalets des joueurs à tous les utilisateurs.
    /// </summary>
    /// <param name="connectionIds">Liste des ID de connexion des utilisateurs.</param>
    /// <param name="joueurs">Liste des joueurs.</param>
    private async Task SendChevalets(List<string> connectionIds, List<Joueur> joueurs)
    {
        foreach (var connectionId in connectionIds)
        {
            await this.SendChevalet(
                connectionId,
                joueurs.Find(x => x.getName().Equals(connectionId, StringComparison.Ordinal))!
            );
        }
    }

    /// <summary>
    /// Envoie le chevalet d'un joueur spécifique.
    /// </summary>
    /// <param name="connectionId">ID de l'utilisateur.</param>
    /// <param name="joueur">Joueur dont le chevalet doit être envoyé.</param>
    private async Task SendChevalet(string connectionId, Joueur joueur)
    {
        var chevaletInit = joueur.GetChevalet();
        var premiereRangee = new List<string>();
        var secondeRangee = new List<string>();

        for (var i = 0; i < 2; i++)
        {
            if (i == 0)
            {
                for (var j = 0; j < 14; j++)
                {
                    var val = chevaletInit[i][j];
                    if (val == null)
                    {
                        premiereRangee.Add($"couleur=;num=;defausse=;dansPioche=;Nom=;");
                    }
                    else
                    {
                        premiereRangee.Add(
                            $"couleur={this.FromEnumToString(chevaletInit[i][j]!.GetCouleur())};num={chevaletInit[i][j]!.GetNum()};defausse={chevaletInit[i][j]!.isDefausse()};dansPioche={chevaletInit[i][j]!.GetPioche()};Nom={chevaletInit[i][j]!.GetName()};"
                        );
                    }
                }
            }
            else
            {
                for (var j = 0; j < 14; j++)
                {
                    var val = chevaletInit[i][j];
                    if (val == null)
                    {
                        secondeRangee.Add($"couleur=;num=;defausse=;dansPioche=;Nom=;");
                    }
                    else
                    {
                        secondeRangee.Add(
                            $"couleur={this.FromEnumToString(chevaletInit[i][j]!.GetCouleur())};num={chevaletInit[i][j]!.GetNum()};defausse={chevaletInit[i][j]!.isDefausse()};dansPioche={chevaletInit[i][j]!.GetPioche()};Nom={chevaletInit[i][j]!.GetName()};"
                        );
                    }
                }
            }
        }
        await this
            ._hubContext.Clients.Client(connectionId)
            .SendAsync(
                "ReceiveChevalet",
                new ChevaletPacket
                {
                    PremiereRangee = premiereRangee,
                    SecondeRangee = secondeRangee
                }
            );
    }

    /// <summary>
    /// Envoie la liste des tuiles défaussées à tous les utilisateurs.
    /// </summary>
    /// <param name="connectionIds">Liste des ID de connexion des utilisateurs.</param>
    /// <param name="jeu">Instance de jeu.</param>
    private async Task SendListeDefausseToAll(List<string> connectionIds, Jeu jeu)
    {
        foreach (var connectionId in connectionIds)
        {
            try
            {
                var ListeDefausseSend = new List<string>();

                foreach (var tuile in jeu.GetListeDefausse())
                {
                    // Construire la chaîne de caractères représentant la tuile( pas besoin d'envoyer defausse est DansPioche mais je fais qd meme)
                    var tuileString =
                        $"couleur={this.FromEnumToString(tuile.GetCouleur())};num={tuile.GetNum()};defausse=\"true\";dansPioche=\"false\";Nom={tuile.GetName()}";
                    //pour la sécurité récupérer les isDefausse et isPioche

                    // Ajouter la chaîne de caractères à ListeDefausseSend
                    ListeDefausseSend.Add(tuileString);
                }

                await this
                    ._hubContext.Clients.Client(connectionId)
                    .SendAsync(
                        "ReceiveListeDefausse",
                        new LstDefaussePacket { Defausse = ListeDefausseSend }
                    );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Erreur lors de l'envoi de la liste de défaites au client {connectionId}: {ex.Message}"
                );
            }
        }
    }

    public async Task TryJoinPrivateRoom(string roomId)
    {
        await this.GetUserNameFromClient(this.Context.ConnectionId);
        Console.WriteLine($"Nouvelle room: {roomId} {this._roomManager.GetRoomById(roomId).Name}");

        var success = this._roomManager.TryJoinPrivateRoom(roomId, this.Context.ConnectionId);
        if (success)
        {
            await this._hubContext.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, "Hub");

            await this._hubContext.Groups.AddToGroupAsync(this.Context.ConnectionId, roomId);
            UsersInRooms.TryUpdate(this.Context.ConnectionId, roomId, "Hub");

            var packet = new RoomState();
            packet.playerDatas = new List<string?>();
            packet.roomName = roomId;

            foreach (var player in this._roomManager.GetRoomById(roomId).GetPlayerIds())
            {
                Console.WriteLine(player);
                packet.playerDatas.Add(
                    $"{_connectedUsers[player].GetUsername()};{_connectedUsers[player].GetPhoto()};{_connectedUsers[player].GetElo()};{_connectedUsers[player].GetExperience()}"
                );
            }

            await this._hubContext.Clients.Group(roomId).SendAsync("SendRoomState", packet);
            await this.SendRoomListUpdate();
            if (this._roomManager.IsRoomFull(roomId))
            {
                this.OnGameStarted(roomId);
            }
        }
        else
        {
            await this
                ._hubContext.Clients.Client(this.Context.ConnectionId)
                .SendAsync(
                    "JoinRoomFail",
                    new PacketSignal
                    {
                        message = "Impossible de rejoindre une room, réessayer plus tard"
                    }
                );
        }
    }

    /// <summary>
    /// Envoie une émote à tous les utilisateurs.
    /// </summary>
    /// <param name="packetEmote">Packet emote recu par l'utilisateur</param>
    public async Task EnvoyerEmoteAll(EmotePacket packetEmote)
    {
        Console.WriteLine(
            $"{packetEmote.PlayerSource} veut envoyer l'emote {packetEmote.EmoteValue}"
        );
        if (packetEmote.PlayerSource != null && packetEmote.EmoteValue != null)
        {
            var roomId = UsersInRooms[packetEmote.PlayerSource];

            foreach (var player in this._roomManager.GetRoomById(roomId).GetPlayerIds())
            {
                await this
                    ._hubContext.Clients.Client(player)
                    .SendAsync("ReceiveEmote", packetEmote);
                Console.WriteLine($"On a envoye l'emote a {player}");
            }
        }
    }

    /// <summary>
    /// Envoie les informations de pioche à tous les utilisateurs.
    /// </summary>
    /// <param name="connectionIds">Liste des ID de connexion des utilisateurs.</param>
    /// <param name="playerSource">Joueur source.</param>
    /// <param name="tuile">Tuile piochée.</param>
    /// <param name="j">Instance de jeu.</param>
    private async Task SendPiocheToAll(
        List<string> connectionIds,
        Joueur playerSource,
        Tuile? tuile,
        Jeu j
    )
    {
        foreach (var connectionId in connectionIds)
        {
            if (!connectionId.Equals(playerSource.getName(), StringComparison.Ordinal))
            {
                try
                {
                    /* Position == 0 -> pioche de gauche
                     * Position == 1 -> Pioche en haut à droite
                     * Position == 2 -> Pioche en haut à gauche
                     * Position == 3 -> Pioche au centre (on s'en fiche un peu)
                     */

                    if (
                        j.getNextJoueur(playerSource)
                            .getName()
                            .Equals(connectionId, StringComparison.Ordinal)
                    )
                    {
                        // 2

                        if (tuile != null)
                        {
                            var tuileStringPacket = new TuilePiocheePacket
                            {
                                position = 2,
                                Couleur = this.FromEnumToString(tuile.GetCouleur()),
                                numero = tuile.GetNum().ToString(CultureInfo.InvariantCulture)
                            };
                            await this
                                ._hubContext.Clients.Client(connectionId)
                                .SendAsync("ReceiveTuilePiochee", tuileStringPacket);
                        }
                        else
                        {
                            var tuileStringPacket = new TuilePiocheePacket
                            {
                                position = 2,
                                Couleur = null,
                                numero = null
                            };
                            await this
                                ._hubContext.Clients.Client(connectionId)
                                .SendAsync("ReceiveTuilePiochee", tuileStringPacket);
                        }
                    }
                    else if (
                        j.getNextJoueur(j.getNextJoueur(playerSource))
                            .getName()
                            .Equals(connectionId, StringComparison.Ordinal)
                    )
                    {
                        // 1

                        if (tuile != null)
                        {
                            var tuileStringPacket = new TuilePiocheePacket
                            {
                                position = 1,
                                Couleur = this.FromEnumToString(tuile.GetCouleur()),
                                numero = tuile.GetNum().ToString(CultureInfo.InvariantCulture)
                            };
                            await this
                                ._hubContext.Clients.Client(connectionId)
                                .SendAsync("ReceiveTuilePiochee", tuileStringPacket);
                        }
                        else
                        {
                            var tuileStringPacket = new TuilePiocheePacket
                            {
                                position = 1,
                                Couleur = null,
                                numero = null
                            };
                            await this
                                ._hubContext.Clients.Client(connectionId)
                                .SendAsync("ReceiveTuilePiochee", tuileStringPacket);
                        }
                    }
                    else if (
                        j.getNextJoueur(j.getNextJoueur(j.getNextJoueur(playerSource)))
                            .getName()
                            .Equals(connectionId, StringComparison.Ordinal)
                    )
                    {
                        // 0

                        if (tuile != null)
                        {
                            var tuileStringPacket = new TuilePiocheePacket
                            {
                                position = 0,
                                Couleur = this.FromEnumToString(tuile.GetCouleur()),
                                numero = tuile.GetNum().ToString(CultureInfo.InvariantCulture)
                            };
                            await this
                                ._hubContext.Clients.Client(connectionId)
                                .SendAsync("ReceiveTuilePiochee", tuileStringPacket);
                        }
                        else
                        {
                            var tuileStringPacket = new TuilePiocheePacket
                            {
                                position = 0,
                                Couleur = null,
                                numero = null
                            };
                            await this
                                ._hubContext.Clients.Client(connectionId)
                                .SendAsync("ReceiveTuilePiochee", tuileStringPacket);
                        }
                    }
                    else
                    {
                        // 3
                        var tuileStringPacket = new TuilePiocheePacket
                        {
                            position = 3,
                            Couleur = null,
                            numero = null
                        };
                        await this
                            ._hubContext.Clients.Client(connectionId)
                            .SendAsync("ReceiveTuilePiochee", tuileStringPacket);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Erreur lors de l'envoi de update de pioche {e.Message}");
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Envoie les informations de la tuile jetée à tous les utilisateurs.
    /// </summary>
    /// <param name="connectionIds">Liste des ID de connexion des utilisateurs.</param>
    /// <param name="playerSource">Joueur source.</param>
    /// <param name="tuile">Tuile jetée.</param>
    /// <param name="j">Instance de jeu.</param>
    private async Task SendTuileJeteToAll(
        List<string> connectionIds,
        Joueur playerSource,
        Tuile? tuile,
        Jeu j
    )
    {
        foreach (var connectionId in connectionIds)
        {
            if (!connectionId.Equals(playerSource.getName(), StringComparison.Ordinal))
            {
                try
                {
                    if (tuile != null)
                    {
                        var couleurString = this.FromEnumToString(tuile.GetCouleur());
                        var numeroString = tuile.GetNum().ToString(CultureInfo.InvariantCulture);

                        /* Position == 0 -> pile de gauche
                         * Position == 1 -> Pile en haut à droite
                         * Position == 2 -> Pile en haut à gauche
                         * Position == 3 -> Pile de droite (utilisé pour des "checks")
                        */

                        TuileJeteePacket tuileStringPacket;

                        if (
                            j.getNextJoueur(playerSource)
                                .getName()
                                .Equals(connectionId, StringComparison.Ordinal)
                        )
                        {
                            tuileStringPacket = new TuileJeteePacket
                            {
                                Couleur = couleurString,
                                numero = numeroString,
                                isDefausse = "true",
                                position = 0
                            };
                            await this
                                ._hubContext.Clients.Client(connectionId)
                                .SendAsync("ReceiveTuileJete", tuileStringPacket);
                        }
                        else if (
                            j.getNextJoueur(j.getNextJoueur(playerSource))
                                .getName()
                                .Equals(connectionId, StringComparison.Ordinal)
                        )
                        {
                            tuileStringPacket = new TuileJeteePacket
                            {
                                Couleur = couleurString,
                                numero = numeroString,
                                isDefausse = "true",
                                position = 2
                            };
                            await this
                                ._hubContext.Clients.Client(connectionId)
                                .SendAsync("ReceiveTuileJete", tuileStringPacket);
                        }
                        else
                        {
                            tuileStringPacket = new TuileJeteePacket
                            {
                                Couleur = couleurString,
                                numero = numeroString,
                                isDefausse = "true",
                                position = 1
                            };
                            await this
                                ._hubContext.Clients.Client(connectionId)
                                .SendAsync("ReceiveTuileJete", tuileStringPacket);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Erreur lors de l'envoi de la tuile au client {connectionId}: {ex.Message}"
                    );
                }
            }
        }
    }

    /// <summary>
    /// Envoie la tuile centrale à tous les utilisateurs.
    /// </summary>
    /// <param name="connectionIds">Liste des ID de connexion des utilisateurs.</param>
    /// <param name="tuile">Tuile centrale.</param>
    private async Task SendTuileCentreToAll(List<string> connectionIds, Tuile tuile)
    {
        foreach (var connectionId in connectionIds)
        {
            try
            {
                var couleurString = this.FromEnumToString(tuile.GetCouleur());
                var numeroString = tuile.GetNum().ToString(CultureInfo.InvariantCulture);

                var tuileStringPacket = new TuileJeteePacket
                {
                    Couleur = couleurString,
                    numero = numeroString,
                    isDefausse = "false"
                };

                await this
                    ._hubContext.Clients.Client(connectionId)
                    .SendAsync("ReceiveTuileCentre", tuileStringPacket);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Erreur lors de l'envoi de la tuile au client {connectionId}: {ex.Message}"
                );
            }
        }
    }

    private async Task SendPlayerOrder(Jeu j, string roomName)
    {
        var packet = new OrderPacket();

        packet.playerConnectionIds = new List<string>();
        packet.playerUsernames = new List<string>();

        packet.playerConnectionIds.Add(j.getJoueurActuel()!.getName());
        packet.playerConnectionIds.Add(j.getNextJoueur(j.getJoueurActuel()!).getName());
        packet.playerConnectionIds.Add(
            j.getNextJoueur(j.getNextJoueur(j.getJoueurActuel()!)).getName()
        );
        packet.playerConnectionIds.Add(
            j.getNextJoueur(j.getNextJoueur(j.getNextJoueur(j.getJoueurActuel()!))).getName()
        );

        packet.playerUsernames.Add(_connectedUsers[packet.playerConnectionIds[0]].GetUsername());
        packet.playerUsernames.Add(_connectedUsers[packet.playerConnectionIds[1]].GetUsername());
        packet.playerUsernames.Add(_connectedUsers[packet.playerConnectionIds[2]].GetUsername());
        packet.playerUsernames.Add(_connectedUsers[packet.playerConnectionIds[3]].GetUsername());

        await this._hubContext.Clients.Group(roomName).SendAsync("PlayerOrdered", packet);
    }

    /// <summary>
    /// Convertit une couleur de tuile énumérée en chaîne.
    /// </summary>
    /// <param name="col">Couleur de la tuile.</param>
    /// <returns>Chaîne représentant la couleur.</returns>
    public string FromEnumToString(CouleurTuile col)
    {
        switch (col)
        {
            case CouleurTuile.J:
                return "J";
            case CouleurTuile.N:
                return "N";
            case CouleurTuile.R:
                return "R";
            case CouleurTuile.B:
                return "B";
            case CouleurTuile.M:
                return "M";
            default:
                return "";
        }
    }

    /// <summary>
    /// Définit l'état du tour d'un joueur.
    /// </summary>
    /// <param name="playerName">Nom du joueur.</param>
    /// <param name="isTurn">Indique si c'est le tour du joueur.</param>
    private void SetPlayerTurn(string playerName, bool isTurn)
    {
        this._isPlayerTurn[playerName] = isTurn;
    }

    /// <summary>
    /// Envoie le signal que les tuiles ont été distribuées.
    /// </summary>
    /// <param name="roomName">Nom de la salle.</param>
    private async Task TuilesDistribueesSignal(string roomName)
    {
        await this.Clients.Group(roomName).SendAsync("TuilesDistribueesSignal");
    }

    /// <summary>
    /// Envoie le signal de début de jeu à tous les joueurs.
    /// </summary>
    /// <param name="players">Liste des joueurs.</param>
    private async Task StartGameSignal(List<string> players)
    {
        var listUsernames = new List<string>();

        foreach (var player in players)
        {
            listUsernames.Add(_connectedUsers[player].GetUsername());
        }

        foreach (var player in players)
        {
            await this
                .Clients.Client(player)
                .SendAsync(
                    "StartGame",
                    new StartingGamePacket
                    {
                        playerId = player,
                        playersList = players,
                        playersUsernames = listUsernames
                    }
                );
        }
    }

    /// <summary>
    /// Gère la victoire d'un joueur.
    /// </summary>
    /// <param name="roomName">Nom de la salle.</param>
    /// <param name="winner">Nom du joueur gagnant.</param>
    private async Task PlayerWon(string roomName, string? winner)
    {
        await this.Clients.Group(roomName).SendAsync("PlayerWon", winner); //TODO: Faire la distinction entre le gagnant et les autres
    }

    /// <summary>
    /// Fonction déclenchée lorsque suffisamment de joueurs sont présents, lancement du jeu.
    /// </summary>
    /// <param name="roomName">Nom de la salle qui accueille le jeu.</param>
    public async Task StartGameForRoom(string roomName)
    {
        this._cts = new CancellationTokenSource();
        var playerIds = this._roomManager.GetRoomById(roomName).GetPlayerIds();
        Joueur[] joueurs =
        {
            new Humain(1, playerIds[0], _connectedUsers[playerIds[0]].GetElo()),
            new Humain(2, playerIds[1], _connectedUsers[playerIds[1]].GetElo()),
            new Humain(3, playerIds[2], _connectedUsers[playerIds[2]].GetElo()),
            new Humain(4, playerIds[3], _connectedUsers[playerIds[3]].GetElo())
        };

        foreach (var t in joueurs)
        {
            this.SetPlayerTurn(t.getName(), false);
        }

        await this.StartGameSignal(playerIds);

        var jeu = new Jeu(1, joueurs);
        jeu.DistibuerTuile();

        Thread.Sleep(5000);

        await this.SendPlayerOrder(jeu, roomName);

        await this.TuilesDistribueesSignal(roomName);

        var joueurStarter = jeu.getJoueurActuel();
        var playerName = string.Empty;
        await this.SendResetTimer(roomName);
        this.SetPlayerTurn(joueurStarter?.getName() ?? playerName, true);
        await this.TourSignalRequest(roomName, joueurStarter?.getName());
        if (joueurStarter != null)
        {
            await this.SendChevalets(playerIds, joueurs.ToList());

            //envoie de la tuile du centre
            await this.SendListeDefausseToAll(playerIds, jeu);
            Thread.Sleep(500);

            await this.SendTuileCentreToAll(playerIds, jeu.GetTuileCentre());
            await this.SendPiocheInfosToAll(playerIds, jeu); // mohammed : broadcast l"etat de la pioche centre avant le debut de la partie
            var coords = await this.FirstJeterRequest(joueurStarter);

            if (coords.Equals("Move", StringComparison.Ordinal))
            {
                // Faire le mouvement de tuiles// MoveInLoop(joueurStarter, j);
                // continue;
            }
            else if (coords.Equals("FIN", StringComparison.Ordinal))
            {
                // Envoyer un packet de fin de partie
                await this.BroadCastInRoom(
                    roomName,
                    new PacketSignal { message = $"{joueurStarter.getName()} a quitté la partie" }
                );
                return;
            }

            // si il veut envoyer un emote (:emote_name: [string])
            /*if (coords.StartsWith(":", StringComparison.OrdinalIgnoreCase) && coords.EndsWith(":", StringComparison.OrdinalIgnoreCase))
            {
                await this.EnvoyerEmote(playerIds, coords);
                continue;
            }*/

            joueurStarter.JeterTuile(this.ReadCoords(coords), jeu);
            //envoi de la tuile jetée du joueur qui commence
            await this.SendTuileJeteToAll(
                playerIds,
                joueurStarter,
                joueurStarter.GetTeteDefausse(),
                jeu
            );
            await this.SendNextDefausseInfosToAll(playerIds, jeu, joueurStarter); // mohammed : update l'etat de la defausse du prochain joueur apres le jet
            await this.SendChevalet(joueurStarter.getName(), joueurStarter);
            this.SetPlayerTurn(joueurStarter.getName(), false);
        }

        await this.SendResetTimer(roomName);
        this.SetPlayerTurn(
            jeu.getJoueurActuel()?.getName() ?? throw new InvalidOperationException(),
            true
        );

        while (!jeu.isTermine())
        {
            var currentPlayer = jeu.getJoueurActuel();
            await this.TourSignalRequest(roomName, currentPlayer?.getName());
            if (this._isPlayerTurn[currentPlayer?.getName() ?? playerName])
            {
                if (currentPlayer != null)
                {
                    await this.SendChevalet(currentPlayer.getName(), currentPlayer);

                    var pioche = await this.PiochePacketRequest(
                        currentPlayer.getName(),
                        this._cts,
                        currentPlayer
                    );
                    if (pioche.Equals("Move", StringComparison.Ordinal))
                    {
                        //await this.MoveInLoop(currentPlayer, jeu);
                        continue;
                    }
                    if (pioche.Equals("FIN", StringComparison.Ordinal))
                    {
                        await this.BroadCastInRoom(
                            roomName,
                            new PacketSignal
                            {
                                message = $"{joueurStarter?.getName()} a quitté la partie"
                            }
                        );
                        return;
                    }

                    if (pioche.Equals("Centre", StringComparison.OrdinalIgnoreCase))
                    {
                        var tuilePiochee = currentPlayer.PiocherTuile(pioche, jeu);
                        await this.SendPiocheInfosToAll(playerIds, jeu);
                    }
                    else if (pioche.Equals("Centrer", StringComparison.OrdinalIgnoreCase))
                    {
                        var tuilePiochee = currentPlayer.PiocherTuile("Centre", jeu);
                        await this.SendChevalet(currentPlayer.getName(), currentPlayer);
                        await this.SendPiocheInfosToAll(playerIds, jeu);
                    }
                    else if (string.Equals(pioche, "Defausse", StringComparison.OrdinalIgnoreCase))
                    {
                        var tuilePiochee = currentPlayer.PiocherTuile(pioche, jeu);
                        await this.SendCurrentDefausseInfosToAll(playerIds, currentPlayer);
                        await this.SendPiocheToAll(playerIds, currentPlayer, tuilePiochee, jeu);
                    }
                    // si sous forme :emote_name:
                    else if (
                        pioche.StartsWith(":", StringComparison.OrdinalIgnoreCase)
                        && pioche.EndsWith(":", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        //await this.EnvoyerEmoteAll(playerIds, pioche);
                        continue;
                    }
                    else
                    {
                        continue;
                    }

                    await this.SendChevalet(currentPlayer.getName(), currentPlayer);

                    var res = await this.JeterRequest(currentPlayer, roomName, jeu);
                    if (res.Equals("FIN", StringComparison.Ordinal))
                    {
                        await this.BroadCastInRoom(
                            roomName,
                            new PacketSignal
                            {
                                message = $"{joueurStarter?.getName()} a quitté la partie"
                            }
                        );
                        return;
                    }

                    if (currentPlayer != null)
                    {
                        await this.SendChevalet(currentPlayer.getName(), currentPlayer);

                        //envoi de la tuile jetée
                        await this.SendTuileJeteToAll(
                            playerIds,
                            currentPlayer,
                            currentPlayer.GetTeteDefausse(),
                            jeu
                        );

                        await this.SendNextDefausseInfosToAll(playerIds, jeu, currentPlayer); // mohammed : update l'etat de la defausse du prochain joueur apres le jet

                        await this.SendListeDefausseToAll(playerIds, jeu);
                        this.SetPlayerTurn(currentPlayer?.getName() ?? playerName, false);
                    }
                }

                await this.SendResetTimer(roomName);
                this.SetPlayerTurn(jeu.getJoueurActuel()?.getName() ?? playerName, true);
            }
        }
        /* TODO fix this
        using (var scope = this._scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            for (var i = 0; i < 4; i++)
            {
                var joueur = jeu.GetJoueurs()[i];

                if (jeu.GetJoueurs()[i].isGagnant())
                {
                    Console.WriteLine("est-ce qu'on arrive ici ?");
                    await _connectedUsers[joueur.getName()]
                        .UpdateStats(dbContext, ((Humain)joueur).GetElo(), 5, true, true);
                }
                else
                {
                    await _connectedUsers[joueur.getName()]
                        .UpdateStats(dbContext, ((Humain)joueur).GetElo(), 3, true, false);
                }
            }
        }
        */
    }

    /// <summary>
    /// Déplace une tuile sur le chevalet.
    /// </summary>
    /// <param name="Pl">Joueur.</param>
    /// <param name="J">Instance de jeu.</param>
    public async Task MoveInLoop(Joueur Pl, Jeu J)
    {
        Console.Write("Donner les coords de la tuile à deplacer (y x): ");
        var from = await this.CoordsRequest(Pl.getName());
        Console.Write("Donner les coords d'où la mettre (y x): ");
        var to = await this.CoordsRequest(Pl.getName());
        Pl.MoveTuileChevalet(this.ReadCoords(from), this.ReadCoords(to), J);
    }

    /// <summary>
    /// Envoie la mise à jour des salles aux clients dans le Hub.
    /// </summary>
    private async Task SendRoomListUpdate()
    {
        await this.SendRoomsRequest();
    }

    /*
        var roomsInfo = this._roomManager.GetRoomsInfo();
        await this
            ._hubContext.Clients.Group("Hub")
            .SendAsync("ReceiveMessage", new PacketSignal { message = roomsInfo });*/

    /* Retirer le commentaire pour tester le client de gestion utilisateur XPR567

    /// <summary>
    /// affiche dans la console du serveur l'ensemble des utilisateurs actuellement connectés à celui-ci
    /// </summary>
    /// <returns></returns>
    public async Task GetAllAssociations()
    {
        string allconnection = "";
        foreach (var elmt in _connectedUsers)
        {
            allconnection += $"Key: {elmt.Key}, Value: " + elmt.Value + "\n";
        }

        await this.Clients.Caller.SendAsync("ReceiveMessage", allconnection);
    }

    /// <summary>
    ///  met à jour les valeurs de l'utilisateur appelant cette fonction, ici à titre d'exemple et à des fins de test
    /// </summary>
    /// <param name="Elo"></param>
    /// <param name="Experience"></param>
    /// <param name="PartieJouee"></param>
    /// <param name="PartieGagnee"></param>
    public async Task UpdatePlayerDatas(
        int Elo,
        int Experience,
        bool PartieJouee,
        bool PartieGagnee
    )
    {
        var id = this.Context.ConnectionId;
        await this.Clients.Caller.SendAsync("ReceiveStats", _connectedUsers[id].PlayerInfos());
        await _connectedUsers[id]
            .UpdateStats(this._dbContext, Elo, Experience, PartieGagnee, PartieJouee);
        await this.Clients.Caller.SendAsync("ReceiveStats", _connectedUsers[id].PlayerInfos());
    }

    /// <summary>
    /// met à jour la valeur de l'achievement passé en argument, avec le booléen fourni en argument ici a titre d'exemple sur els possiblités offertes par la gestion utilisateur
    /// </summary>
    /// <param name="Achievement"></param>
    /// <param name="Value"></param>
    public async Task UpdateAchievement(string Achievement, bool Value)
    {
        var id = this.Context.ConnectionId;
        await this.Clients.Caller.SendAsync(
            "ReceiveMessage",
            _connectedUsers[id].AchievementsToString()
        );
        await _connectedUsers[id].UpdateAchievement(this._dbContext, Achievement, Value);
        await this.Clients.Caller.SendAsync(
            "ReceiveMessage",
            _connectedUsers[id].AchievementsToString()
        );
    }
    */
}

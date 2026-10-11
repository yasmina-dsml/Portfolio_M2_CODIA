using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Code.Scripts.SignalR.Packets;
using Code.Scripts.SignalR.Packets.Emojis;
using Code.Scripts.SignalR.Packets.Rooms;
using JetBrains.Annotations;
using LogiqueJeu.Joueur;
using Microsoft.AspNetCore.SignalR.Client;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SignalRConnector : MonoBehaviour
{
    public static HubConnection _hubConnection;

    public static SignalRConnector Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        DontDestroyOnLoad(this.gameObject);
    }

    public async void InitializeConnection()
    {
        _hubConnection = new HubConnectionBuilder().WithUrl(Constants.SIGNALR_HUB_URL).Build();

        this.ConfigureHubEvents();

        try
        {
            await _hubConnection.StartAsync();
            Debug.Log("SignalR connection established.");
            LobbyManager.Instance.SetConnectionStatus(true);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SignalR connection failed: {ex.Message}");
        }

        await this.JoinRoom();
    }

    public async void InitializeConnectionForPrivate()
    {
        _hubConnection = new HubConnectionBuilder().WithUrl(Constants.SIGNALR_HUB_URL).Build();

        this.ConfigureHubEvents();

        try
        {
            await _hubConnection.StartAsync();
            Debug.Log("SignalR connection established.");
            LobbyManager.Instance.SetConnectionStatus(true);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SignalR connection failed: {ex.Message}");
        }
    }

    public async void HubDisconnect()
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            _hubConnection.StopAsync();
        });
        SceneManager.LoadScene("Acceuil");
    }

    private void ConfigureHubEvents()
    {
        _hubConnection.On<PacketSignal>(
            "ReceiveMessage",
            Message =>
            {
                Debug.Log(Message.message);
            }
        );

        _hubConnection.On<OrderPacket>(
            "PlayerOrdered",
            (playerList) =>
            {
                Debug.Log("Affichage des joueurs dans l'ordre de tour");

                MainThreadDispatcher.Enqueue(async () =>
                {
                    int mainPlayerIndex = -1;
                    for (var i = 0; i < playerList.playerUsernames.Count; i++)
                    {
                        Debug.Log(
                            $"{playerList.playerUsernames[i]} -> {playerList.playerConnectionIds[i]}"
                        );

                        var playerID = playerList.playerConnectionIds[i].Trim().ToLower();
                        var playerUsername = playerList.playerUsernames[i];

                        if (
                            playerID.Equals(
                                _hubConnection.ConnectionId.Trim().ToLower(),
                                StringComparison.Ordinal
                            )
                        )
                        {
                            mainPlayerIndex = i;
                            LobbyManager.mainPlayer = playerID;
                            LobbyManager.Instance.mainPlayerUsername = playerUsername;
                        }
                    }

                    if (mainPlayerIndex == -1)
                    {
                        Debug.LogError("Main player not found in the player list.");
                        return;
                    }

                    //int otherPlayerIndex = 0;

                    LobbyManager.player2 = playerList
                        .playerConnectionIds[(mainPlayerIndex + 1) % 4]
                        .Trim()
                        .ToLower();
                    LobbyManager.Instance.player2Username = playerList.playerUsernames[
                        (mainPlayerIndex + 1) % 4
                    ];

                    LobbyManager.player3 = playerList
                        .playerConnectionIds[(mainPlayerIndex + 2) % 4]
                        .Trim()
                        .ToLower();
                    LobbyManager.Instance.player3Username = playerList.playerUsernames[
                        (mainPlayerIndex + 2) % 4
                    ];

                    LobbyManager.player4 = playerList
                        .playerConnectionIds[(mainPlayerIndex + 3) % 4]
                        .Trim()
                        .ToLower();
                    LobbyManager.Instance.player4Username = playerList.playerUsernames[
                        (mainPlayerIndex + 3) % 4
                    ];

                    /*
                    for (var i = 0; i < playerList.playerUsernames.Count; i++)
                    {
                        if (i == mainPlayerIndex)
                            continue;

                        var playerID = playerList.playerConnectionIds[i].Trim().ToLower();
                        var playerUsername = playerList.playerUsernames[i];

                        switch (otherPlayerIndex)
                        {
                            case 0:
                                LobbyManager.player2 = playerID;
                                LobbyManager.Instance.player2Username = playerUsername;
                                break;
                            case 1:
                                LobbyManager.player3 = playerID;
                                LobbyManager.Instance.player3Username = playerUsername;
                                break;
                            case 2:
                                LobbyManager.player4 = playerID;
                                LobbyManager.Instance.player4Username = playerUsername;
                                break;
                        }

                        otherPlayerIndex++;

                    }*/

                    await UpdatePlayerAvatars(
                        new List<string>
                        {
                            LobbyManager.Instance.mainPlayerUsername,
                            LobbyManager.Instance.player2Username,
                            LobbyManager.Instance.player3Username,
                            LobbyManager.Instance.player4Username
                        }
                    );
                });
            }
        );

        _hubConnection.On<StartingGamePacket>(
            "StartGame",
            (players) =>
            {
                Debug.LogWarning("La partie peut commencer");

                // foreach (var player in players.playersUsernames)
                // {
                //     Debug.LogWarning(player);
                // }

                MainThreadDispatcher.Enqueue(() =>
                {
                    Debug.LogWarning($"Le jeu a commence");

                    //if (players.playersList != null && players.playersList.Count > 0)
                    //{
                    //    int otherPlayerIndex = 0;
                    //    for (int i = 0; i < players.playersList.Count; i++)
                    //    {
                    //        // string player = players.playersList[i];
                    //        // store the player value as a string in lower case trim
                    //        string player = players.playersList[i].Trim().ToLower();
                    //        string username = players.playersUsernames[i];
                    //        // Debug.LogWarning($"Player {i + 1}: {player}");

                    //        Debug.Log(
                    //            "Main player: "
                    //                + _hubConnection.ConnectionId
                    //                + " Player iteration: "
                    //                + player
                    //        );
                    //        if (player == _hubConnection.ConnectionId.Trim().ToLower())
                    //        {
                    //            LobbyManager.mainPlayer = player;
                    //            LobbyManager.Instance.mainPlayerUsername = username;
                    //            Debug.Log("MainPlayer: " + player);
                    //        }
                    //        else
                    //        {
                    //            switch (otherPlayerIndex)
                    //            {
                    //                case 0:
                    //                    LobbyManager.player2 = player;
                    //                    LobbyManager.Instance.player2Username = username;
                    //                    // Debug.Log($"Player 2 set to: {player}");
                    //                    break;
                    //                case 1:
                    //                    LobbyManager.player3 = player;
                    //                    LobbyManager.Instance.player3Username = username;
                    //                    // Debug.Log($"Player 3 set to: {player}");
                    //                    break;
                    //                case 2:
                    //                    LobbyManager.player4 = player;
                    //                    LobbyManager.Instance.player4Username = username;
                    //                    // Debug.Log($"Player 4 set to: {player}");
                    //                    break;
                    //            }
                    //            otherPlayerIndex++;
                    //        }
                    //    }
                    //}
                    //else
                    //{
                    //    Debug.LogError("No players found in playersList.");
                    //}
                    SceneManager.LoadSceneAsync("PlateauInit");
                });
            }
        );

        _hubConnection.On<GameCancelled>(
            "GameCancelled",
            (packet) =>
            {
                // On fait quitter le joueur (simple a faire)

                MainThreadDispatcher.Enqueue(() =>
                {
                    Debug.Log($"Le joueur {packet.playerSource} nous a fait quitter");
                    _hubConnection.StopAsync();
                    Chevalet.neverReceivedChevalet = true;
                    Chevalet.PiocheIsVide = false;
                    SceneManager.LoadScene("Acceuil");
                });
            }
        );

        _hubConnection.On<TuilePiocheePacket>(
            "ReceiveTuilePiochee",
            (tuileInfos) =>
            {
                if (tuileInfos.position == 0)
                {
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        Destroy(
                            Chevalet
                                .PileDroitePlaceHolder.transform.GetChild(
                                    Chevalet.PileDroitePlaceHolder.transform.childCount - 1
                                )
                                .gameObject
                        );
                    });
                }
                else if (tuileInfos.position == 1)
                {
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        if (tuileInfos.numero != null)
                        {
                            var tuileData = new TuileData(
                                tuileInfos.Couleur,
                                int.Parse(tuileInfos.numero),
                                tuileInfos.Couleur != null
                                    && (
                                        tuileInfos.Couleur.Equals("M", StringComparison.Ordinal)
                                        || tuileInfos.Couleur.Equals("X", StringComparison.Ordinal)
                                    )
                            );
                            Debug.LogWarning(Chevalet.FromTuileToSpriteName(tuileData));
                            var spriteRen =
                                Chevalet.PileHautDroitePlaceHolder.GetComponent<SpriteRenderer>();
                            spriteRen.sprite = Chevalet.spritesDic[
                                Chevalet.FromTuileToSpriteName(tuileData)
                            ];
                        }
                        else
                        {
                            var spriteRen =
                                Chevalet.PileHautDroitePlaceHolder.GetComponent<SpriteRenderer>();
                            spriteRen.sprite = null;
                        }
                    });
                }
                else
                {
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        if (tuileInfos.numero != null)
                        {
                            var tuileData = new TuileData(
                                tuileInfos.Couleur,
                                int.Parse(tuileInfos.numero),
                                tuileInfos.Couleur != null
                                    && (
                                        tuileInfos.Couleur.Equals("M", StringComparison.Ordinal)
                                        || tuileInfos.Couleur.Equals("X", StringComparison.Ordinal)
                                    )
                            );
                            Debug.LogWarning(Chevalet.FromTuileToSpriteName(tuileData));
                            var spriteRen =
                                Chevalet.PileHautGauchePlaceHolder.GetComponent<SpriteRenderer>();
                            spriteRen.sprite = Chevalet.spritesDic[
                                Chevalet.FromTuileToSpriteName(tuileData)
                            ];
                        }
                        else
                        {
                            var spriteRen =
                                Chevalet.PileHautGauchePlaceHolder.GetComponent<SpriteRenderer>();
                            spriteRen.sprite = null;
                        }
                    });
                }
            }
        );

        _hubConnection.On(
            "ResetTimer",
            () =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    Debug.Log("Resetting timer.");
                    Timer.Instance.LaunchTimer();
                });
            }
        );

        _hubConnection.On<string>(
            "TurnSignal",
            (PlayerName) =>
            {
                Chevalet.Instance.IsJete = false;
                Chevalet.Instance.TuileJete = null;
                Debug.Log($"C'est le tour de {PlayerName}");
                MainThreadDispatcher.Enqueue(() =>
                {
                    // PlateauSignals.Instance.SetPlayerSignal(PlayerName);

                    PlateauSignals.Instance.TuileCentre.GetComponent<CanvasGroup>().alpha = 0;
                    PlateauSignals.Instance.TuileGauche.GetComponent<CanvasGroup>().alpha = 0;
                    PlateauSignals.Instance.TuileDroite.GetComponent<CanvasGroup>().alpha = 0;

                    PlateauSignals.Instance.MainSignal.GetComponent<CanvasGroup>().alpha = 0;
                    PlateauSignals.Instance.player2TurnSignal.GetComponent<CanvasGroup>().alpha = 0;
                    PlateauSignals.Instance.player3TurnSignal.GetComponent<CanvasGroup>().alpha = 0;
                    PlateauSignals.Instance.player4TurnSignal.GetComponent<CanvasGroup>().alpha = 0;

                    Debug.Log("All signals set to invisible.");

                    var player = PlayerName.Trim().ToLower();
                    if (LobbyManager.player2.Equals(player, StringComparison.Ordinal))
                    {
                        PlateauSignals
                            .Instance.player2TurnSignal.GetComponent<CanvasGroup>()
                            .alpha = 1;
                        Debug.Log("Player 2 turn signal set to visible.");
                    }
                    else if (LobbyManager.player3.Equals(player, StringComparison.Ordinal))
                    {
                        PlateauSignals
                            .Instance.player3TurnSignal.GetComponent<CanvasGroup>()
                            .alpha = 1;
                        Debug.Log("Player 3 turn signal set to visible.");
                    }
                    else if (LobbyManager.player4.Equals(player, StringComparison.Ordinal))
                    {
                        PlateauSignals
                            .Instance.player4TurnSignal.GetComponent<CanvasGroup>()
                            .alpha = 1;
                        Debug.Log("Player 4 turn signal set to visible.");
                    }
                    else
                    {
                        Debug.LogError($"Player name {player} does not match any known player.");
                    }
                    Timer.Instance.LaunchTimer();
                });
            }
        );

        _hubConnection.On(
            "YourTurnSignal",
            () =>
            {
                Debug.Log($"C'est votre tour");
                MainThreadDispatcher.Enqueue(() =>
                {
                    PlateauSignals.Instance.TuileCentre.GetComponent<CanvasGroup>().alpha = 0;
                    PlateauSignals.Instance.TuileGauche.GetComponent<CanvasGroup>().alpha = 0;
                    PlateauSignals.Instance.TuileDroite.GetComponent<CanvasGroup>().alpha = 0;

                    PlateauSignals.Instance.MainSignal.GetComponent<CanvasGroup>().alpha = 0;
                    PlateauSignals.Instance.player2TurnSignal.GetComponent<CanvasGroup>().alpha = 0;
                    PlateauSignals.Instance.player3TurnSignal.GetComponent<CanvasGroup>().alpha = 0;
                    PlateauSignals.Instance.player4TurnSignal.GetComponent<CanvasGroup>().alpha = 0;

                    PlateauSignals.Instance.SetMainPlayerTurnSignal();
                    PlateauSignals.Instance.TuileCentre.gameObject.SetActive(true);
                    PlateauSignals.Instance.TuileGauche.gameObject.SetActive(true);

                    if (!Chevalet.PiocheIsVide)
                    {
                        PlateauSignals.Instance.TuileCentre.GetComponent<CanvasGroup>().alpha = 1;
                    }
                    PlateauSignals.Instance.TuileGauche.GetComponent<CanvasGroup>().alpha = 1;
                    PlateauSignals.Instance.MainSignal.GetComponent<CanvasGroup>().alpha = 1;

                    // Timer.Instance.LaunchTimer();
                });
            }
        );

        _hubConnection.On<TuilePacket>(
            "TuileJeteeAuto",
            (Tuile) =>
            {
                Chevalet.Instance.IsTireeHasard = true;
                Chevalet.Instance.TuileTireeHasard = Tuile;
                Debug.Log($"La tuile jetee automatiquement a pour coord {Tuile.X} {Tuile.Y}");
                MainThreadDispatcher.Enqueue(() =>
                {
                    Chevalet.Instance.MoveFromChevaletToDefausse(Tuile);
                });
            }
        );

        _hubConnection.On<PiocheInfosPacket>(
            "ReceivePiocheUpdate",
            (Pioche) =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    if (Pioche.PiocheTete != null && Pioche.PiocheTaille > 0)
                    {
                        var piocheCentale = Chevalet.PilePiochePlaceHolder.GetComponent<Tuile>();

                        if (piocheCentale.transform.childCount > 0)
                        {
                            for (int i = 0; i < piocheCentale.transform.childCount; i++)
                            {
                                Destroy(
                                    Chevalet.PilePiochePlaceHolder.transform.GetChild(i).gameObject
                                );
                            }
                        }

                        var piocheTuileData = new TuileData(
                            Chevalet.FromStringToCouleurTuile(Pioche.PiocheTete.Couleur),
                            int.Parse(Pioche.PiocheTete.numero),
                            Pioche.PiocheTete.Couleur != null
                                && (
                                    Pioche.PiocheTete.Couleur.Equals("M", StringComparison.Ordinal)
                                    || Pioche.PiocheTete.Couleur.Equals(
                                        "X",
                                        StringComparison.Ordinal
                                    )
                                )
                        );

                        piocheCentale.SetCouleur(piocheTuileData.couleur);
                        piocheCentale.SetValeur(piocheTuileData.num);

                        var childObject = new GameObject("SpriteChild");
                        childObject.transform.SetParent(Chevalet.PilePiochePlaceHolder.transform);
                        childObject.AddComponent<Tuile>();
                        childObject.GetComponent<Tuile>().SetCouleur(piocheTuileData.couleur);
                        childObject.GetComponent<Tuile>().SetValeur(piocheTuileData.num);

                        var spriteRen = childObject.AddComponent<SpriteRenderer>();
                        var mat = new Material(Shader.Find("Sprites/Default"))
                        {
                            color = new Color(
                                0.9529411764705882f,
                                0.9411764705882353f,
                                0.8156862745098039f
                            )
                        };
                        spriteRen.material = mat;
                        spriteRen.sprite = Chevalet.spritesDic["Pioche"];
                        spriteRen.sortingOrder = 3;
                        var transform1 = spriteRen.transform;
                        transform1.localPosition = new Vector3(0, 0, 0);
                        transform1.localScale = new Vector3(1, 1, 1);
                        var boxCollider2D = childObject.AddComponent<BoxCollider2D>();
                        boxCollider2D.size = new Vector2((float)0.875, (float)1.25);

                        for (int i = 0; i < piocheCentale.transform.childCount; i++)
                        {
                            Chevalet.Instance._pilePioche.Push(
                                piocheCentale.transform.GetChild(i).GetComponent<Tuile>()
                            );
                        }

                        childObject.GetComponent<Tuile>().SetIsDeplacable(false);
                    }
                    else
                    {
                        Chevalet.PiocheIsVide = true;
                        Chevalet.PilePiochePlaceHolder.GetComponent<SpriteRenderer>().sprite = null;
                    }
                });
            }
        );

        _hubConnection.On<PacketSignal>(
            "JoinRoomFail",
            (signal) =>
            {
                Debug.LogError(signal.message);
            }
        );

        _hubConnection.On<TuileStringPacket>(
            "ReceiveTuileCentre",
            (tuile) =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    Chevalet.Instance.SetTuileCentre(tuile);
                    Chevalet
                        .JokerPlaceHolder.transform.GetChild(1)
                        .gameObject.GetComponent<Tuile>()
                        .SetIsDeplacable(false);
                });
            }
        );

        _hubConnection.On<TuileJeteePacket>(
            "ReceiveTuileJete",
            (tuile) =>
            {
                if (tuile.position == 0)
                {
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        var tuileData = new TuileData(
                            tuile.Couleur,
                            int.Parse(tuile.numero),
                            tuile.Couleur.Equals("M", StringComparison.Ordinal)
                                || tuile.Couleur.Equals("X", StringComparison.Ordinal)
                        );
                        var childObject = new GameObject("SpriteChild");
                        childObject.transform.SetParent(Chevalet.PileGauchePlaceHolder.transform);
                        var spriteRen = childObject.AddComponent<SpriteRenderer>();
                        var mat = new Material(Shader.Find("Sprites/Default"))
                        {
                            color = new Color(
                                0.9529411764705882f,
                                0.9411764705882353f,
                                0.8156862745098039f
                            )
                        };
                        spriteRen.material = mat;
                        spriteRen.sprite = Chevalet.spritesDic[
                            Chevalet.FromTuileToSpriteName(tuileData)
                        ];
                        spriteRen.sortingOrder =
                            3 + Chevalet.PileGauchePlaceHolder.transform.childCount;
                        var transform1 = spriteRen.transform;
                        transform1.localPosition = new Vector3(0, 0, 0);
                        transform1.localScale = new Vector3(1, 1, 1);
                        childObject.AddComponent<Tuile>();
                        var boxCollider2D = childObject.AddComponent<BoxCollider2D>();
                        boxCollider2D.size = new Vector2((float)0.875, (float)1.25);

                        Chevalet
                            .PileGauchePlaceHolder.GetComponent<Tuile>()
                            .SetCouleur(tuileData.couleur);
                        Chevalet
                            .PileGauchePlaceHolder.GetComponent<Tuile>()
                            .SetValeur(tuileData.num);

                        Chevalet.Instance._pileGauche.Push(
                            Chevalet.PileGauchePlaceHolder.GetComponent<Tuile>()
                        );
                        Chevalet
                            .PileGauchePlaceHolder.transform.GetChild(0)
                            .GetComponent<Tuile>()
                            .SetIsDeplacable(false);
                    });
                }
                else if (tuile.position == 1)
                {
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        var tuileData = new TuileData(
                            tuile.Couleur,
                            int.Parse(tuile.numero),
                            tuile.Couleur.Equals("M", StringComparison.Ordinal)
                                || tuile.Couleur.Equals("X", StringComparison.Ordinal)
                        );
                        var spriteRen =
                            Chevalet.PileHautDroitePlaceHolder.GetComponent<SpriteRenderer>();
                        spriteRen.sprite = Chevalet.spritesDic[
                            Chevalet.FromTuileToSpriteName(tuileData)
                        ];
                    });
                }
                else if (tuile.position == 3)
                {
                    for (var y = 0; y < 2; y++)
                    {
                        for (int x = 0; x < 14; x++)
                        {
                            if (
                                Chevalet.Instance.Tuiles2D[y, x].num == int.Parse(tuile.numero)
                                && Chevalet
                                    .Instance.Tuiles2D[y, x]
                                    .couleur.Equals(tuile.Couleur, StringComparison.Ordinal)
                            )
                            {
                                // On la deplace en pioche
                                // D'abord on efface pour tester
                                Chevalet.Instance.Tuiles2D[y, x] = null;
                            }
                        }
                    }
                }
                else
                {
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        var tuileData = new TuileData(
                            tuile.Couleur,
                            int.Parse(tuile.numero),
                            tuile.Couleur.Equals("M", StringComparison.Ordinal)
                                || tuile.Couleur.Equals("X", StringComparison.Ordinal)
                        );
                        var spriteRen =
                            Chevalet.PileHautGauchePlaceHolder.GetComponent<SpriteRenderer>();
                        spriteRen.sprite = Chevalet.spritesDic[
                            Chevalet.FromTuileToSpriteName(tuileData)
                        ];
                    });
                }
            }
        );

        _hubConnection.On<TuilePacket>(
            "FirstJeterActionRequest",
            () =>
            {
                var chevaletInstance = Chevalet.Instance;
                Debug.Log("Ok il faut jeter une tuile");
                MainThreadDispatcher.Enqueue(() =>
                {
                    PlateauSignals.Instance.TuileCentre.GetComponent<CanvasGroup>().alpha = 0;
                    PlateauSignals.Instance.TuileGauche.GetComponent<CanvasGroup>().alpha = 0;

                    PlateauSignals.Instance.TuileDroite.GetComponent<CanvasGroup>().alpha = 1;
                });

                var tuile = new TuilePacket
                {
                    X = "0",
                    Y = "0",
                    gagner = false
                };

                while (chevaletInstance.IsJete == false) { }

                MainThreadDispatcher.Enqueue(() =>
                {
                    PlateauSignals.Instance.TuileDroite.GetComponent<CanvasGroup>().alpha = 0;
                });
                Debug.Log("La tuile vient d'etre jetee");

                if (chevaletInstance.TuileJete != null)
                {
                    return chevaletInstance.TuileJete;
                }

                // add code here signal
                MainThreadDispatcher.Enqueue(() =>
                {
                    LobbyManager.Instance.SetMyTurn(false);
                });
                return tuile;
            }
        );

        _hubConnection.On<string>(
            "WinInfos",
            (playerId) =>
            {
                //Histoire que l'on remarque bien...
                Debug.LogError($"Le gagnant est {playerId}");
                MainThreadDispatcher.Enqueue(() =>
                {
                    GameObject.Find("PlateauPanel").SetActive(false);
                    Plateau2.Instance.Awake();
                    PlateauUIManager.Instance.FindPartiePanel.SetActive(true);
                    PlateauUIManager
                        .Instance.FindPartiePanel.transform.GetChild(2)
                        .GetComponent<TextMeshProUGUI>()
                        .text = $"{playerId} \n à gagné !";
                });
            }
        );

        _hubConnection.On<TuilePacket>(
            "JeterRequest",
            () =>
            {
                var chevaletInstance = Chevalet.Instance;
                Debug.Log("Ok il faut jeter une tuile");

                Debug.Log("signalllsssssssssssssssssss");
                MainThreadDispatcher.Enqueue(() =>
                {
                    PlateauSignals.Instance.TuileCentre.GetComponent<CanvasGroup>().alpha = 0;
                    PlateauSignals.Instance.TuileGauche.GetComponent<CanvasGroup>().alpha = 0;

                    PlateauSignals.Instance.TuileDroite.GetComponent<CanvasGroup>().alpha = 1;
                });

                var tuile = new TuilePacket
                {
                    X = "0",
                    Y = "0",
                    gagner = false
                };

                while (chevaletInstance.IsJete == false) { }
                Debug.Log("La tuile vient d'etre jetee");

                if (chevaletInstance.TuileJete != null)
                {
                    Debug.Log(
                        $"Ok on envoie un truc non null {chevaletInstance.TuileJete.Y} {chevaletInstance.TuileJete.X}, {chevaletInstance.TuileJete.gagner}"
                    );
                    return chevaletInstance.TuileJete;
                }

                // add code here signal
                MainThreadDispatcher.Enqueue(() =>
                {
                    LobbyManager.Instance.SetMyTurn(false);
                    //PlateauSignals.Instance.SetMainPlayerTurnSignal();
                    //Timer.Instance.ResetTimer();
                });
                return tuile;
            }
        );

        _hubConnection.On<EmotePacket>(
            "ReceiveEmote",
            (packet) =>
            {
                Debug.Log($"On a recu l'emote de {packet.PlayerSource}");
                if (packet.PlayerSource != null && packet.EmoteValue != null)
                {
                    var src = packet.PlayerSource.Trim().ToLower();
                    Debug.Log("On rentre bien ici");
                    if (src.Equals(LobbyManager.player2, StringComparison.Ordinal))
                    {
                        Debug.Log("joueur a droite");
                        MainThreadDispatcher.Enqueue(() =>
                        {
                            Plateau2.Instance.DisplayEmote(2, (int)packet.EmoteValue);
                        });
                    }
                    else if (src.Equals(LobbyManager.player3, StringComparison.Ordinal))
                    {
                        Debug.Log("joueur en face");
                        MainThreadDispatcher.Enqueue(() =>
                        {
                            Plateau2.Instance.DisplayEmote(3, (int)packet.EmoteValue);
                        });
                    }
                    else if (src.Equals(LobbyManager.player4, StringComparison.Ordinal))
                    {
                        Debug.Log("joueur a gauche");
                        MainThreadDispatcher.Enqueue(() =>
                        {
                            Plateau2.Instance.DisplayEmote(4, (int)packet.EmoteValue);
                        });
                    }
                    else if (src.Equals(LobbyManager.mainPlayer, StringComparison.Ordinal))
                    {
                        Debug.Log($"{packet.PlayerSource} <-> {LobbyManager.mainPlayer}");
                    }
                    else
                    {
                        Debug.LogError("Impossible de detecter la source");
                        Debug.Log(LobbyManager.player2);
                        Debug.Log(LobbyManager.player3);
                        Debug.Log(LobbyManager.player4);
                        Debug.Log(LobbyManager.mainPlayer);
                    }
                }
                return Task.CompletedTask;
            }
        );

        _hubConnection.On<DefaussePacket>(
            "ReceiveListeDefausse",
            (Defausse) =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    var tuilesList = new List<TuileData>();

                    if (Defausse.Defausse != null)
                    {
                        foreach (var tuileData in Defausse.Defausse)
                        {
                            if (
                                !tuileData.Equals(
                                    "couleur=;num=;defausse=;dansPioche=;Nom=;",
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                var keyValuePairs = tuileData.Split(';');
                                string couleur = null;
                                var num = 0;
                                string nom = null;

                                foreach (var pair in keyValuePairs)
                                {
                                    if (string.IsNullOrWhiteSpace(pair))
                                    {
                                        continue;
                                    }

                                    var parts = pair.Split('=');

                                    if (parts.Length != 2)
                                    {
                                        continue;
                                    }

                                    var key = parts[0].Trim();
                                    var value = parts[1].Trim();
                                    switch (key)
                                    {
                                        case "couleur":
                                            couleur = value;
                                            break;
                                        case "num":
                                            int.TryParse(value, out num);
                                            break;
                                        case "defausse":
                                            bool.TryParse(value, out _);
                                            break;
                                        case "dansPioche":
                                            bool.TryParse(value, out _);
                                            break;
                                        case "Nom":
                                            nom = value;
                                            break;
                                        default:
                                            // Handle unknown keys (if needed)
                                            break;
                                    }
                                }

                                CouleurTuile coul;
                                switch (couleur)
                                {
                                    case "J":
                                        coul = CouleurTuile.J;
                                        break;
                                    case "N":
                                        coul = CouleurTuile.N;
                                        break;
                                    case "R":
                                        coul = CouleurTuile.R;
                                        break;
                                    case "B":
                                        coul = CouleurTuile.B;
                                        break;
                                    case "M":
                                        coul = CouleurTuile.M;
                                        break;
                                    default:
                                        throw new Exception();
                                }
                                tuilesList.Add(
                                    new TuileData(
                                        coul,
                                        num,
                                        nom != null && nom.Equals("Jo", StringComparison.Ordinal)
                                    )
                                );
                            }
                            else
                            {
                                tuilesList.Add(null);
                            }
                        }

                        // Fonctionaliteé qui affiche les defausses, a implementer plus tard....
                    }
                });
            }
        );

        _hubConnection.On<string>(
            "UsernameRequest",
            () =>
            {
                var user = JoueurManager.Instance;

                if (user.IsConnected)
                {
                    return user.GetSelfJoueur().NomUtilisateur;
                }
                return "Guest";
            }
        );

        _hubConnection.On(
            "TuilesDistribueesSignal",
            () =>
            {
                Debug.Log($"Les tuiles ont ete distribuees");
            }
        );

        _hubConnection.On<PiochePacket>(
            "PiochePacketRequest",
            () =>
            {
                var chevaletInstance = Chevalet.Instance;
                Debug.Log("Ok il faut piocher une tuile");

                // Set la pioche a piochable

                MainThreadDispatcher.Enqueue(() =>
                {
                    // PlateauSignals.Instance.SetMainPlayerTurnSignal();
                    // PlateauSignals.Instance.TuileCentre.gameObject.SetActive(true);
                    // PlateauSignals.Instance.TuileGauche.gameObject.SetActive(true);

                    if (!Chevalet.PiocheIsVide)
                    {
                        PlateauSignals.Instance.TuileCentre.GetComponent<CanvasGroup>().alpha = 1;
                    }
                    PlateauSignals.Instance.TuileGauche.GetComponent<CanvasGroup>().alpha = 1;
                    PlateauSignals.Instance.MainSignal.GetComponent<CanvasGroup>().alpha = 1;

                    PlateauSignals.Instance.TuileDroite.GetComponent<CanvasGroup>().alpha = 0;
                    PlateauSignals.Instance.player2TurnSignal.GetComponent<CanvasGroup>().alpha = 0;
                    PlateauSignals.Instance.player3TurnSignal.GetComponent<CanvasGroup>().alpha = 0;
                    PlateauSignals.Instance.player4TurnSignal.GetComponent<CanvasGroup>().alpha = 0;

                    if (
                        Chevalet.PilePiochePlaceHolder.transform.childCount > 0
                        && Chevalet.PiocheIsVide == false
                    )
                    {
                        Chevalet
                            .PilePiochePlaceHolder.transform.GetChild(
                                Chevalet.PilePiochePlaceHolder.transform.childCount - 1
                            )
                            .GetComponent<Tuile>()
                            .SetIsDeplacable(true);
                    }

                    if (Chevalet.PileGauchePlaceHolder.transform.childCount > 0)
                    {
                        Chevalet
                            .PileGauchePlaceHolder.transform.GetChild(
                                Chevalet.PileGauchePlaceHolder.transform.childCount - 1
                            )
                            .GetComponent<Tuile>()
                            .SetIsDeplacable(true);
                    }
                });

                var tuile = new PiochePacket { Centre = true, Defausse = false };

                while (Chevalet.IsPiochee == false) { }
                Debug.Log("La tuile vient d'etre piochee");
                if (chevaletInstance.TuilePiochee != null)
                {
                    Chevalet.IsPiochee = false;
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        if (Chevalet.PilePiochePlaceHolder.transform.childCount > 0)
                        {
                            Chevalet
                                .PilePiochePlaceHolder.transform.GetChild(
                                    Chevalet.PilePiochePlaceHolder.transform.childCount - 1
                                )
                                .GetComponent<Tuile>()
                                .SetIsDeplacable(false);
                        }

                        if (Chevalet.PileGauchePlaceHolder.transform.childCount > 0)
                        {
                            Chevalet
                                .PileGauchePlaceHolder.transform.GetChild(
                                    Chevalet.PileGauchePlaceHolder.transform.childCount - 1
                                )
                                .GetComponent<Tuile>()
                                .SetIsDeplacable(false);
                        }
                    });
                    return chevaletInstance.TuilePiochee;
                }

                // add code here signal
                // MainThreadDispatcher.Enqueue(() =>
                // {
                //     LobbyManager.Instance.SetMyTurn(false);
                //     // Plateau
                // });
                return tuile;
            }
        );

        _hubConnection.On<string>(
            "TileThrown",
            (cords) =>
            {
                string[] parts = cords.Split(':');

                var x = parts[0];
                var y = parts[1];
                var tile = Chevalet.Instance.TuilesPack[int.Parse(x), int.Parse(y)];
                MainThreadDispatcher.Enqueue(() =>
                {
                    for (var i = 0; i < 2; i++)
                    {
                        for (var j = 0; j < 14; j++)
                        {
                            if (
                                Chevalet
                                    .Instance.Tuiles2D[i, j]
                                    .couleur.Equals(tile.couleur, StringComparison.Ordinal)
                                && Chevalet.Instance.Tuiles2D[i, j].num == tile.num
                            )
                            {
                                Chevalet.Instance.Tuiles2D[i, j] = null;
                                if (Chevalet.Placeholders[i * 14 + j].transform.childCount > 0)
                                {
                                    Destroy(Chevalet.Placeholders[i * 14 + j].gameObject);
                                }
                            }
                        }
                    }
                });
            }
        );

        _hubConnection.On<RoomState>(
            "SendRoomState",
            (roomState) =>
            {
                if (roomState.playerDatas != null)
                {
                    this.UpdateWaitingPlayerUI(roomState.playerDatas, roomState.roomName);
                    // this.LoadAvatarsInLobby(roomState.playerDatas);

                    foreach (var data in roomState.playerDatas)
                    {
                        var dSplit = data.Split(';');
                        Debug.LogWarning(
                            $"Username: {dSplit[0]}, Photo: {dSplit[1]}, Elo: {dSplit[2]}, Experience: {dSplit[3]}"
                        );
                    }
                }
            }
        );

        _hubConnection.On<ChevaletPacket>(
            "ReceiveChevalet",
            (chevalet) =>
            {
                Debug.Log("On a bien recu le chevalet");
                var chevaletInstance = Chevalet.Instance;
                MainThreadDispatcher.Enqueue(() =>
                {
                    while (chevaletInstance == null)
                    {
                        chevaletInstance = Chevalet.Instance;
                    }

                    var tuilesData = new TuileData[2, 14];
                    var i = 0;
                    foreach (var tuileStr in chevalet.PremiereRangee)
                    {
                        if (
                            !tuileStr.Equals(
                                "couleur=;num=;defausse=;dansPioche=;Nom=;",
                                StringComparison.Ordinal
                            )
                        )
                        {
                            var keyValuePairs = tuileStr.Split(';');
                            string couleur = null;
                            var num = 0;
                            string nom = null;

                            foreach (var pair in keyValuePairs)
                            {
                                if (string.IsNullOrWhiteSpace(pair))
                                {
                                    continue;
                                }

                                var parts = pair.Split('=');

                                if (parts.Length != 2)
                                {
                                    continue;
                                }

                                var key = parts[0].Trim();
                                var value = parts[1].Trim();
                                switch (key)
                                {
                                    case "couleur":
                                        couleur = value;
                                        break;
                                    case "num":
                                        int.TryParse(value, out num);
                                        break;
                                    case "defausse":
                                        bool.TryParse(value, out _);
                                        break;
                                    case "dansPioche":
                                        bool.TryParse(value, out _);
                                        break;
                                    case "Nom":
                                        nom = value;
                                        break;
                                    default:
                                        // Handle unknown keys (if needed)
                                        break;
                                }
                            }

                            CouleurTuile coul;
                            switch (couleur)
                            {
                                case "J":
                                    coul = CouleurTuile.J;
                                    break;
                                case "N":
                                    coul = CouleurTuile.N;
                                    break;
                                case "R":
                                    coul = CouleurTuile.R;
                                    break;
                                case "B":
                                    coul = CouleurTuile.B;
                                    break;
                                case "M":
                                    coul = CouleurTuile.M;
                                    break;
                                default:
                                    throw new Exception();
                            }

                            tuilesData[0, i] = new TuileData(
                                coul,
                                num,
                                nom != null && nom.Equals("Jo", StringComparison.Ordinal)
                            );
                            i++;
                        }
                    }

                    i = 0;
                    foreach (var tuileStr in chevalet.SecondeRangee)
                    {
                        if (
                            !tuileStr.Equals(
                                "couleur=;num=;defausse=;dansPioche=;Nom=;",
                                StringComparison.Ordinal
                            )
                        )
                        {
                            var keyValuePairs = tuileStr.Split(';');

                            string couleur = null;
                            var num = 0;
                            string nom = null;

                            foreach (var pair in keyValuePairs)
                            {
                                if (string.IsNullOrWhiteSpace(pair))
                                {
                                    continue;
                                }

                                var parts = pair.Split('=');

                                if (parts.Length != 2)
                                {
                                    continue;
                                }

                                var key = parts[0].Trim();
                                var value = parts[1].Trim();
                                switch (key)
                                {
                                    case "couleur":
                                        couleur = value;
                                        break;
                                    case "num":
                                        int.TryParse(value, out num);
                                        break;
                                    case "defausse":
                                        bool.TryParse(value, out _);
                                        break;
                                    case "dansPioche":
                                        bool.TryParse(value, out _);
                                        break;
                                    case "Nom":
                                        nom = value;
                                        break;
                                    default:
                                        break;
                                }
                            }
                            CouleurTuile coul;
                            switch (couleur)
                            {
                                case "J":
                                    coul = CouleurTuile.J;
                                    break;
                                case "N":
                                    coul = CouleurTuile.N;
                                    break;
                                case "R":
                                    coul = CouleurTuile.R;
                                    break;
                                case "B":
                                    coul = CouleurTuile.B;
                                    break;
                                case "M":
                                    coul = CouleurTuile.M;
                                    break;
                                default:
                                    throw new Exception();
                            }

                            tuilesData[1, i] = new TuileData(
                                coul,
                                num,
                                nom != null && nom.Equals("Jo", StringComparison.Ordinal)
                            );
                            i++;
                        }
                    }

                    if (Chevalet.neverReceivedChevalet)
                    {
                        chevaletInstance.SetTuiles(tuilesData);
                        chevaletInstance.TuilesPack = tuilesData;
                        chevaletInstance.Print2DMatrix();
                        chevaletInstance.Init();
                        Chevalet.neverReceivedChevalet = false;
                    }
                    else
                    {
                        chevaletInstance.TuilesPack = tuilesData;
                        chevaletInstance.InitializeBoardFromTuiles();
                    }
                });
            }
        );
    }

    //public static void UpdateRoomDisplay(RoomsPacket roomsPacket)
    //{
    //    //if (PlayerInLobbyProgressScreen.Instance != null)
    //    //{
    //    //    PlayerInLobbyProgressScreen.Instance.SetRooms(roomsPacket);
    //    //}
    //    //else
    //    //{
    //    //    Debug.LogError("PlayerInLobbyProgressScreen instance is not found.");
    //    //}

    //    if (DisplayRooms.Instance != null)
    //    {
    //        DisplayRooms.Instance.changeLabel(roomsPacket);
    //    }
    //    else
    //    {
    //        Debug.LogError("DisplayRooms instance is not found.");
    //    }

    //}

    private void UpdateWaitingPlayerUI(List<string> PlayerData, string roomName)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            var vue = UIManagerPFormulaire.Instance.lobbyPlayerWaiting;

            Debug.Log(PlayerData.Count);

            var bg = vue.transform.GetChild(0);
            var header = bg.transform.GetChild(0);
            var txtCounter = header.transform.GetChild(2);
            txtCounter.transform.GetComponent<TextMeshProUGUI>().text = $"{PlayerData.Count}/4";

            var P1 = bg.transform.GetChild(1);
            var P2 = bg.transform.GetChild(2);
            var P3 = bg.transform.GetChild(3);
            var P4 = bg.transform.GetChild(4);

            Debug.Log($"Nom de room: {roomName}");

            if (!roomName.Contains("room", StringComparison.Ordinal))
            {
                bg.transform.GetChild(5).GetComponent<TextMeshProUGUI>().text =
                    $"Code de la partie:\n{roomName}";
            }

            var tabP = new List<Transform>();
            tabP.Add(P1);
            tabP.Add(P2);
            tabP.Add(P3);
            tabP.Add(P4);
            for (var i = 0; i < PlayerData.Count; i++)
            {
                var dSplit = PlayerData[i].Split(';');
                var username = tabP[i].transform.GetChild(1);
                var avatar = tabP[i].transform.GetChild(0);
                var elo = tabP[i].transform.GetChild(2);
                var experience = tabP[i].transform.GetChild(3);

                username.transform.GetComponent<TextMeshProUGUI>().text = dSplit[0];
                var res = Resources.Load<Sprite>("Avatar/avatarn" + dSplit[1]);
                avatar.transform.GetComponent<SpriteRenderer>().transform.localScale = new Vector3(
                    50f, //0.342f,
                    50f, //0.342f,
                    1f
                );
                avatar.transform.GetComponent<SpriteRenderer>().sortingOrder = 5;
                avatar.transform.GetComponent<SpriteRenderer>().sprite = res;
                if (dSplit[0].Equals("Guest", StringComparison.Ordinal))
                {
                    elo.transform.GetComponent<TextMeshProUGUI>().text = "";
                    experience.transform.GetComponent<TextMeshProUGUI>().text = "";
                }
                else
                {
                    elo.transform.GetComponent<TextMeshProUGUI>().text = "Elo Score: " + dSplit[2];
                    experience.transform.GetComponent<TextMeshProUGUI>().text =
                        "Niveau: " + dSplit[3];
                }
            }
        });
    }

    // private void LoadAvatarsInLobby(List<string> PlayerData)
    // {
    //     MainThreadDispatcher.Enqueue(() =>
    //     {
    //         var vue = PlateauUIManager.Instance.playerAvatars;
    //         var P1 = vue.transform.GetChild(1);
    //         var P2 = vue.transform.GetChild(2);
    //         var P3 = vue.transform.GetChild(3);
    //         var P4 = vue.transform.GetChild(4);

    //         var tabP = new List<Transform>();
    //         tabP.Add(P1);
    //         tabP.Add(P2);
    //         tabP.Add(P3);
    //         tabP.Add(P4);
    //         for (var i = 0; i < PlayerData.Count; i++)
    //         {
    //             var dSplit = PlayerData[i].Split(';');
    //             var avatar = tabP[i].transform.GetChild(0);
    //             var res = Resources.Load<Sprite>("Avatar/avatarn" + dSplit[1]);
    //             avatar.transform.GetComponent<SpriteRenderer>().transform.localScale = new Vector3(
    //                 50f,
    //                 50f,
    //                 1f
    //             );
    //             avatar.transform.GetComponent<SpriteRenderer>().sortingOrder = 5;
    //             avatar.transform.GetComponent<SpriteRenderer>().sprite = res;
    //         }
    //     });
    // }

    public async Task CreateJoinPrivateRoom()
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.SendAsync("CreatePrivateRoom");
                MainThreadDispatcher.Enqueue(() =>
                {
                    UIManagerPFormulaire.Instance.lobbyPlayerWaiting.SetActive(true);

                    var vue = UIManagerPFormulaire.Instance.lobbyPlayerWaiting;
                });
                Debug.Log($"Request to join private room sent.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to join private room : {ex.Message}");
            }
        }
        else
        {
            Debug.LogError("Cannot join private room. Hub connection is not established.");
        }
    }

    public async Task JoinWithCodePrivate(string code)
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.SendAsync("TryJoinPrivateRoom", code);
                MainThreadDispatcher.Enqueue(() =>
                {
                    UIManagerPFormulaire.Instance.lobbyPlayerWaiting.SetActive(true);

                    var vue = UIManagerPFormulaire.Instance.lobbyPlayerWaiting;
                });
                Debug.Log($"Request to join room 1 sent.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to join room 1: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError("Cannot join room. Hub connection is not established.");
        }
    }

    public async Task JoinRoom() // takes parameter roomName in future
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.SendAsync("LobbyConnection");
                MainThreadDispatcher.Enqueue(() =>
                {
                    UIManagerPFormulaire.Instance.lobbyPlayerWaiting.SetActive(true);

                    var vue = UIManagerPFormulaire.Instance.lobbyPlayerWaiting;
                });
                Debug.Log($"Request to join room 1 sent.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to join room 1: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError("Cannot join room. Hub connection is not established.");
        }
    }

    public async void SendEmoji(int indexEmoji)
    {
        var emotePacket = new EmotePacket
        {
            EmoteValue = indexEmoji,
            PlayerSource = _hubConnection.ConnectionId
        };
        await _hubConnection.SendAsync("EnvoyerEmoteAll", emotePacket);
    }

    public async void SendBoardState(TuileData[,] BoardState)
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync("SendBoardState", BoardState);
                Debug.Log("Board state sent successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to send board state: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError("Hub connection is not established.");
        }
    }

    private async void OnDestroy()
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
        }
    }

    // private IEnumerator ActivateShowRooms()
    // {
    //     yield return new WaitForEndOfFrame();

    //     if (UIManagerPFormulaire.Instance == null)
    //     {
    //         Debug.LogError("UIManagerPFormulaire instance is null.");
    //         yield break;
    //     }
    //     Debug.Log("setActiveShowRooms");
    //     UIManagerPFormulaire.Instance.setActiveShowRooms();
    // }
    private async Task UpdatePlayerAvatars(List<string> playerUsernames)
    {
        List<Task<Sprite>> avatarTasks = new List<Task<Sprite>>();
        foreach (var username in playerUsernames)
        {
            avatarTasks.Add(FetchPlayerAvatarAsync(username));
        }

        Sprite[] avatars = await Task.WhenAll(avatarTasks);

        if (avatars.Length >= 4)
        {
            LobbyManager.Instance.mainPlayerAvatar = avatars[0];
            LobbyManager.Instance.player2Avatar = avatars[1];
            LobbyManager.Instance.player3Avatar = avatars[2];
            LobbyManager.Instance.player4Avatar = avatars[3];

            MainThreadDispatcher.Enqueue(() =>
            {
                PlayerAvatarsLobby.Instance.LoadAndDisplayAvatars(
                    new List<Sprite>
                    {
                        LobbyManager.Instance.mainPlayerAvatar,
                        LobbyManager.Instance.player2Avatar,
                        LobbyManager.Instance.player3Avatar,
                        LobbyManager.Instance.player4Avatar
                    }
                );
            });
        }
    }

    private async Task<Sprite> FetchPlayerAvatarAsync(string username)
    {
        try
        {
            var joueur = await API.FetchJoueurAsync(username);
            if (joueur != null)
            {
                string avatarPath = $"Avatar/avatarn{joueur.photo}";
                Sprite avatarSprite = Resources.Load<Sprite>(avatarPath);
                return avatarSprite;
            }
            else
            {
                Debug.LogWarning($"No avatar found for user: {username}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to fetch avatar for user {username}: {ex.Message}");
            return null;
        }
    }
}

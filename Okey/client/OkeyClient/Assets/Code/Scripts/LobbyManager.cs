using System.Collections.Generic; // Add this line
using System.Linq;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    public SignalRConnector signalRConnector;

    //public RoomsPacket rooms;

    public bool connectionStarted = false;
    public bool rommsListFilled = false;
    public int playerCount;

    public bool myTurn = false;

    public string currentPlayerTurn;

    public static string mainPlayer;
    public static string player2;
    public static string player3;
    public static string player4;

    public string mainPlayerUsername;
    public string player2Username;
    public string player3Username;
    public string player4Username;

    public Sprite mainPlayerAvatar;
    public Sprite player2Avatar;
    public Sprite player3Avatar;
    public Sprite player4Avatar;

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

    void Start()
    {
        Debug.Log("[LobbyManager] Awake called.");

        /*
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[LobbyManager] Instance set and marked as DontDestroyOnLoad.");
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[LobbyManager] Duplicate instance detected, destroying this one.");
            Destroy(gameObject);
        }*/
    }

    // SignalR connection.
    public void InitializeConnection()
    {
        Debug.Log("[LobbyManager] InitializeConnection called.");

        if (signalRConnector == null)
        {
            Debug.LogError("SignalRConnector is not set on LobbyManager");
            connectionStarted = false;
            return;
        }

        Debug.Log("Initializing Connection");
        signalRConnector.InitializeConnection();
    }

    public async void JoinRoom()
    {
        await signalRConnector.JoinRoom(); // Awaiting the async operation
    }

    public void SetConnectionStatus(bool value)
    {
        connectionStarted = value;
    }

    public void setRoomCount(int count)
    {
        playerCount = count;
        DisplayRooms.Instance.updateLabel();
    }

    public bool GetConnectedStatus()
    {
        return connectionStarted;
    }

    private void Update()
    {
        // Gameloop here
        // requests
        // all checks
    }

    public void SetMyTurn(bool value)
    {
        myTurn = value;
        // PlateauSignals.Instance.SetMainPlayerTurnSignal();
    }

    public void SetPlayers(List<string> players)
    {
        if (players.Count >= 3) { }
        else
        {
            Debug.LogError(
                "Insufficient players provided. Expected at least 3, received " + players.Count
            );
        }
    }

    public void SetCurrentPlayerTurn(string currentPlayerTurn)
    {
        this.currentPlayerTurn = currentPlayerTurn;
        // PlateauSignals.Instance.SetPlayerSignal(currentPlayerTurn);
    }
}

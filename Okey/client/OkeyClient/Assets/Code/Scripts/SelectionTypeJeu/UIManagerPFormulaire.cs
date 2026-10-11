using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManagerPFormulaire : MonoBehaviour
{
    public static UIManagerPFormulaire Instance { get; private set; }

    [SerializeField]
    private Button backBtn;

    [SerializeField]
    private Button backBtnPublic;

    [SerializeField]
    private Button backBtnPrivate;

    [SerializeField]
    private Button joinPrivateLobbyBtn;

    [SerializeField]
    private Button joinPublicLobbyBtn;

    [SerializeField]
    private TextMeshProUGUI partieSimpleTxt;
    public TextMeshProUGUI partiePriveeTxt;
    public TextMeshProUGUI createPrivateLobbyTitle;
    public TextMeshProUGUI joinPrivateLobbyTitle;
    public TextMeshProUGUI createPrivateLbbyBtnLabel;
    public TextMeshProUGUI joinLbbyPrivateLbl;

    public TextMeshProUGUI waitingForPlayersTitle;
    public TextMeshProUGUI waitingForPlayer1;
    public TextMeshProUGUI waitingForPlayer2;
    public TextMeshProUGUI waitingForPlayer3;
    public TextMeshProUGUI waitingForPlayer4;

    [SerializeField]
    private GameObject lobbyPrivateConfig;

    public GameObject lobbyPlayerWaiting;

    [SerializeField]
    public Button CreatePrivateParty;

    [SerializeField]
    public TMP_InputField PartyCode;

    [SerializeField]
    public Button JoinPrivateParty;

    [SerializeField]
    private int SceneId;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (UIManager.singleton.language)
        {
            partieSimpleTxt.text = "Public Lobby";
            partiePriveeTxt.text = "Private Lobby";
            createPrivateLobbyTitle.text = "Create a private lobby";
            joinPrivateLobbyTitle.text = "Join a private lobby";
            createPrivateLbbyBtnLabel.text = "Create";
            joinLbbyPrivateLbl.text = "Join";

            waitingForPlayersTitle.text = "Waiting for players";
            waitingForPlayer1.text = "Waiting for player";
            waitingForPlayer2.text = "Waiting for player";
            waitingForPlayer3.text = "Waiting for player";
            waitingForPlayer4.text = "Waiting for player";
        }
        else
        {
            partieSimpleTxt.text = "Lobby public";
            partiePriveeTxt.text = "Lobby Privée";
            createPrivateLobbyTitle.text = "Créer un lobby privé";
            joinPrivateLobbyTitle.text = "Rejoindre un lobby privé";
            createPrivateLbbyBtnLabel.text = "Créer une partie privée";
            joinLbbyPrivateLbl.text = "Rejoindre";

            waitingForPlayersTitle.text = "En attente de joueurs";
            waitingForPlayer1.text = "En attente de joueur";
            waitingForPlayer2.text = "En attente de joueur";
            waitingForPlayer3.text = "En attente de joueur";
            waitingForPlayer4.text = "En attente de joueur";
        }

        backBtnPrivate.onClick.AddListener(onBackBtnPrivateClicked);
        backBtnPublic.onClick.AddListener(onBackBtnPrivateClicked);
        backBtn.onClick.AddListener(onBackBtnClicked);
        //joinLobbyBtn.onClick.AddListener(onJoinLobbyButtonPressed);
        joinPublicLobbyBtn.onClick.AddListener(onPublicLobbyClicked);
        joinPrivateLobbyBtn.onClick.AddListener(onPrivateLobbyClicked);
        this.CreatePrivateParty.onClick.AddListener(CreateAndJoinPrivateParty);
        this.JoinPrivateParty.onClick.AddListener(JoinParty);

        lobbyPlayerWaiting.SetActive(false);
        lobbyPrivateConfig.SetActive(false);

        SignalRConnector.Instance.InitializeConnectionForPrivate();
    }

    void Update()
    {
        /*if (UIManager.singleton.language) // En
        {
            partieSimpleTxt.text = "Texte en anglais";
        }
        else // Fr
        {
            partieSimpleTxt.text = "Lobby Public";
        }

        if (LobbyManager.Instance.GetConnectedStatus())
        {
            showRooms.SetActive(true);
        }*/
    }

    private async void CreateAndJoinPrivateParty()
    {
        await SignalRConnector.Instance.CreateJoinPrivateRoom();
    }

    private async void JoinParty()
    {
        if (this.PartyCode.text.Equals("", StringComparison.Ordinal))
        {
            Debug.LogError("Vous devez remplir avec un code");
        }
        else
        {
            // Essayer de join
            SignalRConnector.Instance.JoinWithCodePrivate(this.PartyCode.text.ToUpper());
        }
    }

    void onBackBtnClicked()
    {
        SceneManager.LoadScene(SceneId);
    }

    void onBackBtnPrivateClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        //lobbyPrivateConfig.SetActive(false);
        SignalRConnector.Instance.HubDisconnect();
    }

    public void onPublicLobbyClicked()
    {
        // lobbyPlayerWaiting.SetActive(true);
        onJoinLobbyButtonPressed();
    }

    public void onPrivateLobbyClicked()
    {
        lobbyPrivateConfig.SetActive(true);
    }

    public void onLoadBtnClicked()
    {
        SceneManager.LoadScene(2); // Charge la scène `PlateauInit`
    }

    public void onJoinLobbyButtonPressed()
    {
        if (LobbyManager.Instance == null)
        {
            Debug.LogError("LobbyManager instance is null.");
            return;
        }
        LobbyManager.Instance.InitializeConnection();
    }

    //public void UpdateLobbyCountDisplay(RoomsPacket rooms)
    //{
    //    buttonLabel.text = LobbyManager.Instance.playerCount.ToString();
    //    //if (rooms != null && rooms.listRooms.Count > 0)
    //    //{
    //    //    RoomDto firstRoom = rooms.listRooms[0];
    //    //    if (firstRoom != null && firstRoom.Players != null)
    //    //    {
    //    //        numberOfUsersInFirstRoom = firstRoom.Players.Count;
    //    //        buttonLabel.text = "Room1: " + numberOfUsersInFirstRoom.ToString() + "/4";
    //    //        //buttonLabel.text = DisplayRooms.Instance.messageLogs.Count.ToString();
    //    //    }
    //    //    else
    //    //    {
    //    //        buttonLabel.text = "No Players"; // Default or error text if no players
    //    //    }
    //    //}
    //    //else
    //    //{
    //    //    Debug.Log("Room data unavailable");
    //    //}
    //    //Debug.Log("Finished updating lobby count");
    //}



    //public void setRooms(RoomsPacket roomsPacket)
    //{
    //    Debug.Log("here");
    //    rooms = roomsPacket;
    //    UpdateLobbyCountDisplay(rooms);
    //}


    //public void changeLabel(RoomsPacket rooms)
    //{
    //    buttonLabel.text = $"Rejoindre Room 1 {rooms.listRooms[0].Players.Count}/{rooms.listRooms[0].Capacity}";

    //}

    public void setActiveShowRooms()
    {
        Debug.Log("setActiveShowRooms");
    }
}

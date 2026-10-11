using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUsernames : MonoBehaviour
{
    public TextMeshProUGUI mainPlayerUsername;
    public TextMeshProUGUI Player2Username;
    public TextMeshProUGUI Player3Username;
    public TextMeshProUGUI Player4Username;

    // Start is called before the first frame update
    void Start()
    {
        mainPlayerUsername.text = LobbyManager.Instance.mainPlayerUsername;
        Player2Username.text = LobbyManager.Instance.player2Username;
        Player3Username.text = LobbyManager.Instance.player3Username;
        Player4Username.text = LobbyManager.Instance.player4Username;
    }

    // Update is called once per frame
    void Update() { }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlateauSignals : MonoBehaviour
{
    public static PlateauSignals Instance;

    public Image TuileCentre;
    public Image TuileGauche;
    public Image TuileDroite;
    public Image MainSignal;
    public Image player2TurnSignal;
    public Image player3TurnSignal;
    public Image player4TurnSignal;

    public Color ping1;
    public Color ping2;
    public float speedPing;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        float lerpFactor = Mathf.PingPong(Time.unscaledTime * speedPing, 1);
        MainSignal.color = Color.Lerp(ping1, ping2, lerpFactor);
        TuileGauche.color = Color.Lerp(ping1, ping2, lerpFactor);
        TuileDroite.color = Color.Lerp(ping1, ping2, lerpFactor);
        TuileCentre.color = Color.Lerp(ping1, ping2, lerpFactor);
        player2TurnSignal.color = Color.Lerp(ping1, ping2, lerpFactor);
        player3TurnSignal.color = Color.Lerp(ping1, ping2, lerpFactor);
        player4TurnSignal.color = Color.Lerp(ping1, ping2, lerpFactor);
    }

    public void SetMainPlayerTurnSignal()
    {
        Debug.Log("Setting main player turn signal.");
        SetAlpha(TuileCentre, 1);
        SetAlpha(TuileGauche, 1);

        Debug.Log("Main player turn signals set to visible.");
        SetAlpha(player2TurnSignal, 0);
        SetAlpha(player3TurnSignal, 0);
        SetAlpha(player4TurnSignal, 0);

        Debug.Log("Other player turn signals set to invisible.");
    }

    public void SetPlayerSignal(string playerName)
    {
        Debug.Log("SetPlayerSignal called.");
        playerName = playerName.Trim().ToLower();

        SetAlpha(TuileCentre, 0);
        SetAlpha(TuileGauche, 0);
        SetAlpha(player2TurnSignal, 0);
        SetAlpha(player3TurnSignal, 0);
        SetAlpha(player4TurnSignal, 0);

        if (LobbyManager.player2.Equals(playerName))
        {
            SetAlpha(player2TurnSignal, 1);
            Debug.Log("Player 2 turn signal set.");
        }
        else if (LobbyManager.player3.Equals(playerName))
        {
            SetAlpha(player3TurnSignal, 1);
            Debug.Log("Player 3 turn signal set.");
        }
        else if (LobbyManager.player4.Equals(playerName))
        {
            SetAlpha(player4TurnSignal, 1);
            Debug.Log("Player 4 turn signal set.");
        }
        else
        {
            Debug.LogError($"Player name {playerName} does not match any known player.");
        }
    }

    private void SetAlpha(Image image, float alpha)
    {
        var canvasGroup = image.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = image.gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = alpha;
    }
}



// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// public class PlateauSignals : MonoBehaviour
// {
//     public static PlateauSignals Instance;

//     public Image TuileCentre;
//     public Image TuileGauche;
//     public Image player2TurnSignal;
//     public Image player3TurnSignal;
//     public Image player4TurnSignal;

//     private void Awake()
//     {
//         if (Instance == null)
//         {
//             Instance = this;
//             // DontDestroyOnLoad(gameObject);
//         }
//         else if (Instance != this)
//         {
//             Destroy(gameObject);
//         }
//     }

//     public void SetMainPlayerTurnSignal()
//     {
//         Debug.Log("Setting main player turn signal.");

//         TuileCentre.gameObject.SetActive(true);
//         TuileGauche.gameObject.SetActive(true);
//         Debug.Log("Main player turn signal set to active.");
//         player2TurnSignal.gameObject.SetActive(false);
//         player3TurnSignal.gameObject.SetActive(false);
//         player4TurnSignal.gameObject.SetActive(false);

//         Debug.Log("Main player turn signal setup complete.");
//     }

//     public void SetPlayerSignal(string? playerName)
//     {
//         Debug.Log("SetPlayerSignal called.");
//         // MainThreadDispatcher.Enqueue(() =>
//         // {
//         // Debug.Log($"Setting turn signal for player: {playerName}");

//         TuileCentre.gameObject.SetActive(false);
//         TuileGauche.gameObject.SetActive(false);
//         player2TurnSignal.gameObject.SetActive(false);
//         player3TurnSignal.gameObject.SetActive(false);
//         player4TurnSignal.gameObject.SetActive(false);

//         //  Debug.Log("player 2:" + LobbyManager.Instance.player2);
//         //  Debug.Log("player 3:" + LobbyManager.Instance.player3);
//         //  Debug.Log("player 4:" + LobbyManager.Instance.player4);

//         playerName = playerName.Trim().ToLower();
//         if (LobbyManager.player2 == playerName)
//         {
//             player2TurnSignal.gameObject.SetActive(true);
//             Debug.Log("Player 2 turn signal set.");
//         }
//         else if (LobbyManager.player3 == playerName)
//         {
//             player3TurnSignal.gameObject.SetActive(true);
//             Debug.Log("Player 3 turn signal set.");
//         }
//         else if (LobbyManager.player4 == playerName)
//         {
//             player4TurnSignal.gameObject.SetActive(true);
//             Debug.Log("Player 4 turn signal set.");
//         }
//         else
//         {
//             Debug.LogError($"Player name {playerName} does not match any known player.");
//         }
//         // });
//     }
// }

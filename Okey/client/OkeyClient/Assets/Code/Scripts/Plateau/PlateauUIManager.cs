using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlateauUIManager : MonoBehaviour
{
    public static PlateauUIManager Instance { get; private set; }

    public GameObject playerAvatars;

    public GameObject FindPartiePanel;

    public TextMeshProUGUI boonneChanceText;
    public TextMeshProUGUI bienJoueText;
    public TextMeshProUGUI superCombatText;
    public TextMeshProUGUI merciText;
    public TextMeshProUGUI superText;
    public TextMeshProUGUI aieText;

    // awake
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

    // Start is called before the first frame update
    void Start()
    {
        if (UIManager.singleton.language)
        {
            boonneChanceText.text = "Good luck";
            bienJoueText.text = "Well done !";
            superCombatText.text = "Great Game";
            merciText.text = "Thank you !";
            superText.text = "Great !";
            aieText.text = "Ouch....";
        }
        else
        {
            boonneChanceText.text = "Bonne chance";
            bienJoueText.text = "Bien joué !";
            superCombatText.text = "Super Combat";
            merciText.text = "Merci !";
            superText.text = "Super !";
            aieText.text = "Aie....";
        }
    }

    public void QuitterPartie()
    {
        SignalRConnector._hubConnection.StopAsync();
        Chevalet.neverReceivedChevalet = true;
        Chevalet.PiocheIsVide = false;
        SceneManager.LoadScene("Acceuil");
    }
}

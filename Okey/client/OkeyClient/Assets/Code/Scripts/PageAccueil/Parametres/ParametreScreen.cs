using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ParametreScreen : MonoBehaviour
{
    public Image parametresImage; // Reference to the image object

    [SerializeField]
    private TextMeshProUGUI titleCard;

    [SerializeField]
    private Button BackBtn;

    [SerializeField]
    private TextMeshProUGUI soundEffects;

    [SerializeField]
    private TextMeshProUGUI music;

    [SerializeField]
    private TextMeshProUGUI language;

    public void Start()
    {
        BackBtn.onClick.AddListener(onBackBtnClicked);
    }

    private void onFrBtnClicked()
    {
        UIManager.singleton.language = false;
    }

    private void onEnBtnClicked()
    {
        UIManager.singleton.language = true;
    }

    private void onBackBtnClicked()
    {
        parametresImage.gameObject.SetActive(false);
    }

    public void Update()
    {
        if (UIManager.singleton.language == true) // EN
        {
            soundEffects.text = "Sound Effects";
            music.text = "Music";
            language.text = "Language";
        }
        else
        {
            soundEffects.text = "Effet Sonores";
            music.text = "Ambiance Musique";
            language.text = "Langue";
        }
    }
}

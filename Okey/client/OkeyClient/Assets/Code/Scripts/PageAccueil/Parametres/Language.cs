using TMPro;
using UnityEngine;
using UnityEngine.UI;

//using UnityEngine.UIElements;
//using static Unity.VisualScripting.Icons;

//using UnityEngine.UIElements;

public class Language : MonoBehaviour
{
    public Button enButton;
    public Button frButton;

    public Image enImage;
    public Image frImage;

    public TextMeshProUGUI enLabel;
    public TextMeshProUGUI frLabel;

    Color color;

    public void Start()
    {
        ColorUtility.TryParseHtmlString("#39A24A", out color);

        enButton.onClick.AddListener(onEnClicked);
        frButton.onClick.AddListener(onFnClicked);

        if (UIManager.singleton.language)
        {
            frLabel.color = Color.black;
            enLabel.color = Color.white;

            enImage.color = color;
            frImage.color = Color.white;
        }
        else
        {
            frLabel.color = Color.white;
            enLabel.color = Color.black;

            frImage.color = color;
            enImage.color = Color.white;
        }
    }

    public void onEnClicked()
    {
        frLabel.color = Color.black;
        enLabel.color = Color.white;

        enImage.color = color;
        frImage.color = Color.white;

        UIManager.singleton.language = true;
    }

    public void onFnClicked()
    {
        frLabel.color = Color.white;
        enLabel.color = Color.black;

        frImage.color = color;
        enImage.color = Color.white;

        UIManager.singleton.language = false;
    }
}

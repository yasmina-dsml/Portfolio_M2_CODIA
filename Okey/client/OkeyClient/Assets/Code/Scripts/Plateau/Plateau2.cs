using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Plateau2 : MonoBehaviour
{
    public GameObject PlateauPanel;
    public GameObject ConfirmationPanel;
    public GameObject ParamPanel;
    public GameObject EmojiPanel; //Panneau d'emoji

    public Sprite[] emojiSprites;
    public Image EmojiDisplay;
    public Button[] wordButtons; // Boutons pour les mots
    public TextMeshProUGUI gameDisplayText; // Texte affiché sur le plateau de jeu

    public static Plateau2 Instance { get; private set; }

    private string[] words =
    {
        "Bonne chance",
        "Bien joué !",
        "Super Combat",
        "Merci !",
        "Super !",
        "Aie...."
    };

    private string[] wordsEn =
    {
        "Good luck",
        "Well done !",
        "Great Game",
        "Thank you !",
        "Great !",
        "Ouch...."
    };

    // Fonction pour activer le Confirmation_Panel et désactiver le Plateau_Panel
    // Tableau des mots, assurez-vous que cela correspond à l'ordre des boutons


    public void Awake()
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

    public void OnWordButtonClicked(int buttonIndex)
    {
        Debug.Log($"Button clicked with index: {buttonIndex}");
        if (buttonIndex >= 0 && buttonIndex < this.words.Length)
        {
            Debug.Log($"Displaying word: {this.words[buttonIndex]}");
            gameDisplayText.text = UIManager.singleton.language
                ? this.wordsEn[buttonIndex]
                : this.words[buttonIndex];
            SignalRConnector.Instance.SendEmoji(4 + buttonIndex);
            Debug.LogWarning("On a bien envoyé le texte");
            StartCoroutine(ClearTextAfterDelay(gameDisplayText, 2f));
            EmojiPanel.SetActive(false);
        }
        else
        {
            Debug.LogError(
                $"Index out of range: {buttonIndex}, words array length: {words.Length}"
            );
        }
    }

    public void DisplayEmote(int playerNumber, int emoteNumber)
    {
        var playerSignals = this.PlateauPanel.transform.GetChild(7);

        if (playerNumber == 2)
        {
            Debug.Log("Joueur de droite");
            var player = playerSignals.transform.GetChild(1);
            if (emoteNumber < 4)
            {
                var image = player.transform.GetChild(1);
                Debug.Log($"{image.name}");
                var noneSprite = image.GetComponent<SpriteRenderer>().sprite;
                var sprite = image.GetComponent<SpriteRenderer>();
                sprite.sprite = this.emojiSprites[emoteNumber];
                this.StartCoroutine(this.WaitForEraseSprite(sprite, noneSprite));
            }
            else
            {
                var text = player.transform.GetChild(2);
                var realText = text.GetComponent<TextMeshProUGUI>();
                realText.text = UIManager.singleton.language
                    ? this.wordsEn[emoteNumber - 4]
                    : this.words[emoteNumber - 4];
                this.StartCoroutine(this.ClearTextAfterDelay(realText, 2f));
            }
        }
        else if (playerNumber == 3)
        {
            Debug.Log("Joueur en face");
            var player = playerSignals.transform.GetChild(2);
            if (emoteNumber < 4)
            {
                var image = player.transform.GetChild(1);
                Debug.Log($"{image.name}");
                var noneSprite = image.GetComponent<SpriteRenderer>().sprite;
                var sprite = image.GetComponent<SpriteRenderer>();
                sprite.sprite = this.emojiSprites[emoteNumber];
                this.StartCoroutine(this.WaitForEraseSprite(sprite, noneSprite));
            }
            else
            {
                var text = player.transform.GetChild(2);
                var realText = text.GetComponent<TextMeshProUGUI>();
                realText.text = UIManager.singleton.language
                    ? this.wordsEn[emoteNumber - 4]
                    : this.words[emoteNumber - 4];
                this.StartCoroutine(this.ClearTextAfterDelay(realText, 2f));
            }
        }
        else if (playerNumber == 4)
        {
            Debug.Log("Joueur de gauche");
            var player = playerSignals.transform.GetChild(3);
            if (emoteNumber < 4)
            {
                var image = player.transform.GetChild(1);
                Debug.Log($"{image.name}");
                var noneSprite = image.GetComponent<SpriteRenderer>().sprite;
                var sprite = image.GetComponent<SpriteRenderer>();
                sprite.sprite = this.emojiSprites[emoteNumber];
                this.StartCoroutine(this.WaitForEraseSprite(sprite, noneSprite));
            }
            else
            {
                var text = player.transform.GetChild(2);
                var realText = text.GetComponent<TextMeshProUGUI>();
                realText.text = UIManager.singleton.language
                    ? this.wordsEn[emoteNumber - 4]
                    : this.words[emoteNumber - 4];
                this.StartCoroutine(this.ClearTextAfterDelay(realText, 2f));
            }
        }
    }

    IEnumerator WaitForEraseSprite(SpriteRenderer sp, Sprite s)
    {
        yield return new WaitForSeconds(2);
        sp.sprite = s;
    }

    IEnumerator ClearTextAfterDelay(TextMeshProUGUI textComponent, float delay)
    {
        yield return new WaitForSeconds(delay);
        textComponent.text = ""; // Efface le texte après le délai
    }

    public void ShowConfirmationPanel()
    {
        ConfirmationPanel.SetActive(true);
        PlateauPanel.SetActive(false);
    }

    public void QuitterPartie()
    {
        SignalRConnector._hubConnection.StopAsync();
        Chevalet.neverReceivedChevalet = true;
        Chevalet.PiocheIsVide = false;
        SceneManager.LoadScene("Acceuil");
    }

    public void toggleEmojiPanel()
    {
        EmojiPanel.SetActive(!EmojiPanel.activeSelf);
    }

    // Est appelée quand un bouton émoji est cliqué
    public void ShowEmojiPanel()
    {
        EmojiPanel.SetActive(true);
    }

    public void OnEmojiSelected(int emojiIndex)
    {
        if (emojiIndex < 0 || emojiIndex >= emojiSprites.Length)
            return;

        // Change le sprite de l'objet Image pour correspondre à l'émoji sélectionné
        EmojiDisplay.sprite = emojiSprites[emojiIndex];
        SignalRConnector.Instance.SendEmoji(emojiIndex);
        Debug.LogWarning("On a bien envoyé l'emoji");

        // Active l'objet Image pour afficher l'émoji et le désactive après 2 secondes
        EmojiDisplay.gameObject.SetActive(true);
        StartCoroutine(DisableAfterDelay(EmojiDisplay.gameObject, 2f));

        // Cache le panneau d'emojis après sélection
        EmojiPanel.SetActive(false);
    }

    IEnumerator DisableAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
    }

    // Fonction pour activer le Plateau_Panel et désactiver le Confirmation_Panel
    public void ShowPlateauPanel()
    {
        PlateauPanel.SetActive(true);
        ConfirmationPanel.SetActive(false);
        ParamPanel.SetActive(false);
    }

    public void ShowParamPanel()
    {
        ParamPanel.SetActive(true);
        ConfirmationPanel.SetActive(false);
        PlateauPanel.SetActive(false);
    }

    //Fonction Pour retourner a la page D'accuile
    public void ChangeScene()
    {
        SceneManager.LoadScene(0);
    }
}

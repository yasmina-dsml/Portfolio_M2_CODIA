using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoIntroSequence : MonoBehaviour
{
    public GameObject blackBackground;
    public GameObject imageToShow;

    public float timeToShowBlackBackground = 2f;
    public float timeToShowImage = 2f;

    void Start()
    {
        // Vérifie si le splash screen a déjà été montré.
        if (PlayerPrefs.GetInt("HasShownSplashScreen", 0) == 0)
        {
            StartCoroutine(ShowIntroSequence());
            PlayerPrefs.SetInt("HasShownSplashScreen", 1); // Indique que le splash screen a été montré.
            PlayerPrefs.Save();
        }
    }

    IEnumerator ShowIntroSequence()
    {
        blackBackground.SetActive(true);
        imageToShow.SetActive(false);

        yield return new WaitForSeconds(timeToShowBlackBackground);

        blackBackground.SetActive(false);
        imageToShow.SetActive(true);
        yield return new WaitForSeconds(timeToShowImage);

        imageToShow.SetActive(false);
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.DeleteKey("HasShownSplashScreen"); // Reset lors de la fermeture de l'application.
    }
}

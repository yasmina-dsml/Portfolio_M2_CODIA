using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogiqueJeu.Joueur;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gère l'affichage et la pagination des classements des joueurs.
/// </summary>
public class ClassementManager : MonoBehaviour
{ // Références aux éléments UI dans l'éditeur Unity
    public ScrollRect scrollRect; // Composant ScrollRect pour gérer le scrolling dans l'UI
    public GameObject playerEntryPrefab; // Pour les entrées dynamiques dans le ScrollView
    public Transform contentPanel; // Content du ScrollView
    public GameObject ClassementPanel; // Panneau affichant le classement
    public GameObject AvatarPanel;
    public GameObject ClassementAvantConnexion;
    public TextMeshProUGUI[] rankTexts; //Textes pour les classements des top joueurs
    public TextMeshProUGUI[] userTexts; // Textes pour les noms des top joueurs
    public TextMeshProUGUI[] eloTexts; // Textes pour les scores ELO des top joueurs
    public const int MAX_REQUEST_RETRIES = 5; // Supérieur ou égal à 1
    public const int REQUEST_RETRY_DELAY = 1000; // En millisecondes
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private int currentPage = 1; // Page actuelle initialisée à 1.
    private bool isLoading = false;
    public Button refreshButton;

    /// <summary>
    /// Initialisation des requêtes de classement.
    /// </summary>
    void Start()
    {
        Debug.Log("Fetching classements...");
        FetchClassementsTopAsync(5);
        FetchClassementsPageAsync(currentPage);

        // Ajouter un écouteur d'événement pour le scroll
        scrollRect.onValueChanged.AddListener(HandleScroll);
        refreshButton.onClick.AddListener(ResetAndFetchFirstPage);
    }

    void HandleScroll(Vector2 position)
    {
        // Vérifier si l'utilisateur a défilé jusqu'à la fin
        if (position.y <= 0.01f && !isLoading)
        {
            isLoading = true;
            FetchClassementsPageAsync(++currentPage);
        }
    }

    /// <summary>
    /// Réinitialise la vue à la première page et récupère les données pour celle-ci.
    /// </summary>
    public void ResetAndFetchFirstPage()
    {
        currentPage = 1; // Reset à la premiére page
        FetchClassementsPageAsync(currentPage); // Fetch les données pour la premiére page
        scrollRect.verticalNormalizedPosition = 1; // Reset scroll
    }

    private async void FetchClassementsTopAsync(int limit)
    {
        List<Joueur> joueurs = null;
        try
        {
            // Utilisation directe de la classe API pour la requête.
            joueurs = await API.FetchClassementsTopAsync(limit, cancellationTokenSource.Token);
        }
        catch (Exception e)
        {
            Debug.Log($"An error occurred: {e.Message}");
            return;
        }

        DisplayTopPlayers(joueurs); // Séparation de la logique d'affichage.
    }

    private async void FetchClassementsPageAsync(int pageNumber)
    {
        List<Joueur> joueurs = null;
        try
        {
            joueurs = await API.FetchClassementsPageAsync(
                pageNumber,
                cancellationTokenSource.Token
            );
            DisplayPlayers(joueurs);
        }
        catch (Exception e)
        {
            Debug.Log($"An error occurred: {e.Message}");
        }
        finally
        {
            isLoading = false; // Réinitialiser l'état de chargement après le chargement des données ou en cas d'erreur.
        }
    }

    /// <summary>
    /// Affiche les top joueurs récupérés.
    /// </summary>
    /// <param name="joueurs">Liste des joueurs à afficher.</param>
    private void DisplayTopPlayers(List<Joueur> joueurs)
    {
        if (joueurs != null && joueurs.Count > 0)
        {
            Debug.Log("Top classement reçu:");
            int displayCount = Mathf.Min(joueurs.Count, rankTexts.Length);
            for (int i = 0; i < displayCount; i++)
            {
                rankTexts[i].text = (i + 1).ToString();
                userTexts[i].text = joueurs[i].NomUtilisateur;
                eloTexts[i].text = joueurs[i].Elo.ToString();
            }
        }
        else
        {
            Debug.Log("Erreur lors de la récupération des classements ou aucune donnée reçue");
            ClearDisplay(); // Appel de la fonction de nettoyage de l'affichage.
        }
    }

    /// <summary>
    /// Affiche les joueurs pour la pagination.
    /// </summary>
    /// <param name="joueurs">Liste des joueurs à afficher.</param>
    private void DisplayPlayers(List<Joueur> joueurs)
    { /*
        if (joueurs == null || joueurs.Count == 0)
        {
            Debug.Log("Aucun joueur trouvé.");
            return;
        }
        */
        foreach (var joueur in joueurs)
        {
            if (playerEntryPrefab == null || contentPanel == null)
            {
                Debug.LogError("Prefab ou Content Panel non assigné dans l'éditeur.");
                return;
            }

            GameObject newEntry = Instantiate(playerEntryPrefab, contentPanel);
            if (newEntry == null)
            {
                Debug.LogError("Instantiation du prefab a échoué.");
                continue;
            }

            var rankText = newEntry.transform.Find("Rank")?.GetComponent<TextMeshProUGUI>();
            var userText = newEntry.transform.Find("UserId")?.GetComponent<TextMeshProUGUI>();
            var eloText = newEntry.transform.Find("Score")?.GetComponent<TextMeshProUGUI>();

            if (rankText == null || userText == null || eloText == null)
            {
                Debug.LogError(
                    "Un ou plusieurs composants TextMeshProUGUI sont manquants sur le prefab."
                );
                continue;
            }

            rankText.text = joueur.Classement.ToString();
            userText.text = joueur.NomUtilisateur;
            eloText.text = joueur.Elo.ToString();
        }
    }

    // Fonction ajoutée pour nettoyer l'affichage si nécessaire.
    /// <summary>
    /// Nettoie l'affichage si une erreur de récupération des données se produit.
    /// </summary>
    private void ClearDisplay()
    {
        foreach (var textMesh in new[] { rankTexts, userTexts, eloTexts })
        {
            foreach (var text in textMesh)
            {
                text.text = "-";
            }
        }
    }

    public void BackAcceuil()
    {
        ClassementAvantConnexion.SetActive(false);
        AvatarPanel.SetActive(true);
        ClassementPanel.SetActive(false);
    }
}

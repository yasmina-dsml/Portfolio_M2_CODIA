using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LogiqueJeu.Joueur;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogInScreen : MonoBehaviour
{
    public GameObject Panel;

    //public GameObject PanelAvatar;
    //public GameObject creationPanel;

    [SerializeField]
    private TMP_InputField Username;

    [SerializeField]
    private TMP_InputField Password;

    [SerializeField]
    private Button connexionButton;

    [SerializeField]
    private Button createButton;

    [SerializeField]
    private Button backButton;

    private JoueurManager manager;

    [SerializeField]
    private UIManagerPAcceuil accueil;

    [SerializeField]
    private TextMeshProUGUI erreurTxt;

    public TextMeshProUGUI Title;
    public TextMeshProUGUI nomUtilisateurLabel;
    public TextMeshProUGUI motPasseLabel;
    public TextMeshProUGUI connectionBtnLabel;
    public TextMeshProUGUI creeCompteLabel;

    [SerializeField]
    private GameObject createAccountView;

    public const int MAX_REQUEST_RETRIES = 5; // Superieur ou égale à 1
    public const int REQUEST_RETRY_DELAY = 1000; // En milisecondes
    private readonly CancellationTokenSource Source = new();

    // Start is called before the first frame update
    void Start()
    {
        this.manager = JoueurManager.Instance;

        Password.contentType = TMP_InputField.ContentType.Password;
        // Ajoute un écouteur au bouton "Créer"
        connexionButton.onClick.AddListener(OnConnexionClicked);

        createButton.onClick.AddListener(OnCreateClicked);

        // Ajoute un écouteur au bouton "Retour"
        backButton.onClick.AddListener(onBackBtnClicked);

        Username.onValueChanged.AddListener(OnInputChanged);

        Password.onValueChanged.AddListener(OnInputChanged);

        //this.manager.ConnexionChangeEvent.AddListener(OnConnexionClicked);

        erreurTxt.gameObject.SetActive(false);
    }

    // Méthode appelée lors du clic sur le bouton "Créer"
    async void OnConnexionClicked()
    {
        string username = Username.text.Trim();
        string password = Password.text.Trim();

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            await this.UpdateWithConnection(username, password);
        }
        else
        {
            erreurTxt.text = "Veuillez remplir tous les champs !";
            erreurTxt.gameObject.SetActive(true);
        }
    }

    void OnCreateClicked()
    {
        Panel.SetActive(false);
        createAccountView.SetActive(true);
    }

    // Méthode pour charger la scène précédente
    void onBackBtnClicked()
    {
        Panel.SetActive(false);
    }

    void Update()
    {
        if (UIManager.singleton.language)
        {
            Title.text = "Log In";
            nomUtilisateurLabel.text = "Username";
            motPasseLabel.text = "Password";
            connectionBtnLabel.text = "Log In";
            creeCompteLabel.text = "Create Account";
        }
        else
        {
            Title.text = "Connexion";
            nomUtilisateurLabel.text = "Nom d'utilisateur";
            motPasseLabel.text = "Mot de passe";
            connectionBtnLabel.text = "Connexion";
            creeCompteLabel.text = "Créer un compte";
        }
    }

    public async Task UpdateWithConnection(string username, string password)
    {
        for (var i = 0; i < MAX_REQUEST_RETRIES; i++)
        {
            try
            {
                await this.manager.ConnexionSelfJoueurAsync(username, password, this.Source.Token);
                Panel.SetActive(false);
                return;
            }
            catch (HttpRequestException e)
            {
                var Code = e.GetStatusCode();
                if (Code != null)
                {
                    Debug.Log("Request error with response code " + Code);
                    erreurTxt.text = "Identifiant ou mot de passe invalide";
                    erreurTxt.gameObject.SetActive(true);
                    return;
                }
                else
                {
                    Debug.Log("Network error");
                    if (i != MAX_REQUEST_RETRIES - 1)
                    {
                        Debug.Log("Retrying...");
                        try
                        {
                            await Task.Delay(REQUEST_RETRY_DELAY, this.Source.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            Debug.Log("Request cancelled.");
                            return;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Request cancelled.");
                return;
            }
        }
        erreurTxt.text = "Erreur réseau, votre Internet marche bien ?";
        erreurTxt.gameObject.SetActive(true);
    }

    private void OnInputChanged(string value)
    {
        if (value != null)
        {
            erreurTxt.gameObject.SetActive(false);
        }
    }
}

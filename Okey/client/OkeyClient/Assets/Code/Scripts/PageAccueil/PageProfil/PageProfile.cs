using System.Collections;
using System.Collections.Generic;
using LogiqueJeu.Joueur;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PageProfile : MonoBehaviour
{
    [SerializeField]
    public GameObject Panel;

    private int currentAvatarId;

    [SerializeField]
    private GameObject avatar0;

    [SerializeField]
    private GameObject avatar1;

    [SerializeField]
    private GameObject avatar2;

    [SerializeField]
    private GameObject avatar3;

    private float scaleFactor = 1.3f;

    [SerializeField]
    private Button backButton;

    [SerializeField]
    private Button disconnectionButton;

    [SerializeField]
    private GameObject PanelAvatar;

    [SerializeField]
    private JoueurManager manager;

    [SerializeField]
    private TextMeshProUGUI NiveauTxt;

    [SerializeField]
    private TextMeshProUGUI ScoreTxt;

    [SerializeField]
    private TextMeshProUGUI StatistiqueTxt;

    [SerializeField]
    private TextMeshProUGUI TempsTxt;

    [SerializeField]
    private TextMeshProUGUI WinTxt;

    [SerializeField]
    private TextMeshProUGUI LostTxt;

    [SerializeField]
    private TextMeshProUGUI RankTxt;

    // Start is called before the first frame update
    void Start()
    {
        if (this.manager.IsConnected)
        {
            updateUserProfile();
        }
        manager.ConnexionChangeEvent.AddListener(updateUserProfile);
        disconnectionButton.onClick.AddListener(onDisconnectionClicked);
        currentAvatarId = (int)manager.GetSelfJoueur().IconeProfil;
        OnAvatarButtonClick();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null)
            {
                if (hit.collider.gameObject == avatar0.gameObject)
                {
                    ResetAvatarView();
                    currentAvatarId = 1;
                    OnAvatarButtonClick();
                }
                else if (hit.collider.gameObject == avatar1.gameObject)
                {
                    ResetAvatarView();
                    currentAvatarId = 2;
                    OnAvatarButtonClick();
                }
                else if (hit.collider.gameObject == avatar2.gameObject)
                {
                    ResetAvatarView();
                    currentAvatarId = 3;
                    OnAvatarButtonClick();
                }
                else if (hit.collider.gameObject == avatar3.gameObject)
                {
                    ResetAvatarView();
                    currentAvatarId = 4;
                    OnAvatarButtonClick();
                }
            }
        }
    }

    void OnAvatarButtonClick()
    {
        switch (currentAvatarId)
        {
            case 1:
                avatar0.transform.localScale *= scaleFactor;
                break;
            case 2:
                avatar1.transform.localScale *= scaleFactor;
                break;
            case 3:
                avatar2.transform.localScale *= scaleFactor;
                break;
            case 4:
                avatar3.transform.localScale *= scaleFactor;
                break;
        }
    }

    void ResetAvatarView()
    {
        switch (currentAvatarId)
        {
            case 1:
                avatar0.transform.localScale /= scaleFactor;
                break;
            case 2:
                avatar1.transform.localScale /= scaleFactor;
                break;
            case 3:
                avatar2.transform.localScale /= scaleFactor;
                break;
            case 4:
                avatar3.transform.localScale /= scaleFactor;
                break;
        }
    }

    // Méthode pour charger la scène précédente
    void onBackBtnClicked()
    {
        Panel.SetActive(false);
    }

    public void updateUserProfile()
    {
        PanelAvatar.GetComponentInChildren<TextMeshProUGUI>().text = manager
            .GetSelfJoueur()
            .NomUtilisateur;
        NiveauTxt.text = "Niveau : " + manager.GetSelfJoueur().Niveau;
        ScoreTxt.text = "Score : " + manager.GetSelfJoueur().Score;
        StatistiqueTxt.text = "Statistique : " + manager.GetSelfJoueur().Elo;
        TempsTxt.text = "Temps Total : 15h";
        WinTxt.text = "Parties Gagnées : " + manager.GetSelfJoueur().NombrePartiesGagnees;
        LostTxt.text =
            "Parties Perdues : "
            + (
                manager.GetSelfJoueur().NombreParties - manager.GetSelfJoueur().NombrePartiesGagnees
            );
        RankTxt.text = "Classement : " + manager.GetSelfJoueur().Classement;
        //Mettre à jour le reste du profil de l'utilisateur avec ses données
        Sprite newSprite = Resources.Load<Sprite>("Avatar/avatarn4");
        switch ((int)manager.GetSelfJoueur().IconeProfil)
        {
            case 1:
                newSprite = Resources.Load<Sprite>("Avatar/avatarn1");
                break;
            case 2:
                newSprite = Resources.Load<Sprite>("Avatar/avatarn2");
                break;
            case 3:
                newSprite = Resources.Load<Sprite>("Avatar/avatarn3");
                break;
        }
        PanelAvatar.GetComponentInChildren<Image>().sprite = newSprite;
    }

    void onDisconnectionClicked()
    {
        manager.DeconnexionSelfJoueur();
    }
}

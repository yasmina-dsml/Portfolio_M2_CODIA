using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogiqueJeu.Joueur;
using UnityEngine;
using UnityEngine.Events;

public class JoueurManager : MonoBehaviour
{
    public static JoueurManager Instance { get; private set; }

    [SerializeField]
    private bool CleanStart = false; // Manipulez dans l'éditeur Unity pour invalider les données de cache SelfJoueur (détails de dernier login)
    private readonly List<Joueur> Joueurs = new(3);
    private SelfJoueur SoiMeme;

    private bool _IsConnected = false;
    public bool IsConnected
    {
        get { return this._IsConnected; }
        private set
        {
            this._IsConnected = value;
            this.ConnexionChangeEvent?.Invoke();
        }
    }

    [HideInInspector]
    public UnityEvent SelfJoueurChangeEvent = new();

    [HideInInspector]
    public UnityEvent OtherJoueurChangeEvent = new();

    [HideInInspector]
    public UnityEvent AnyJoueurChangeEvent = new();

    [HideInInspector]
    public UnityEvent ConnexionChangeEvent = new();

    private async void Awake()
    {
#if !UNITY_EDITOR
        this.CleanStart = false;
#endif
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        DontDestroyOnLoad(this.gameObject);

        this.SoiMeme = new();
        if (this.CleanStart)
        {
            this.SoiMeme.DeleteXML();
        }
        this.SoiMeme.JoueurChangeEvent += this.OnSelfJoueurChange;
        await this.SoiMeme.LoadSelf();
        if (!string.IsNullOrEmpty(this.SoiMeme.TokenConnexion))
        {
            this.IsConnected = true;
        }
    }

    private void OnSelfJoueurChange(object O = null, EventArgs E = null)
    {
        this.SelfJoueurChangeEvent?.Invoke();
        this.AnyJoueurChangeEvent?.Invoke();
    }

    private void OnOtherJoueurChange(object O = null, EventArgs E = null)
    {
        this.OtherJoueurChangeEvent?.Invoke();
        this.AnyJoueurChangeEvent?.Invoke();
    }

    public async Task AddGenericJoueur(
        int ID,
        string NomUtilisateur = null,
        Position? Pos = null,
        bool Replace = false,
        CancellationToken Token = default
    )
    {
        if (Pos == Position.SoiMeme)
        {
            throw new ArgumentException("Cannot add a player to the same position as oneself");
        }
        if (this.Joueurs.Count == 3)
        {
            throw new ArgumentException(
                "Cannot add any more players, maximum number of players reached"
            );
        }
        if (!Replace && this.Joueurs.Exists(Joueur => Joueur.InGame.ID == ID))
        {
            throw new ArgumentException("Cannot add a player with the same ID as another player");
        }

        var Joueur = new GenericJoueur();
        Joueur.InGame.ID = ID;
        Joueur.InGame.Pos = Pos;
        if (NomUtilisateur != null)
        {
            Joueur.NomUtilisateur = NomUtilisateur;
            await Joueur.LoadSelf(Token);
        }

        if (Replace)
        {
            var J = this.Joueurs.FindAll(Joueur => Joueur.InGame.ID == ID);
            if (J.Count > 1)
            {
                throw new DataMisalignedException(
                    "There are multiple players with the same ID, internal error, this should've never happenned"
                );
            }
            foreach (var JF in J)
            {
                JF.JoueurChangeEvent -= this.OnOtherJoueurChange;
                this.Joueurs.Remove(JF);
            }
        }

        this.Joueurs.Add(Joueur);
        Joueur.JoueurChangeEvent += this.OnOtherJoueurChange;
        this.OnOtherJoueurChange();
    }

    public void AssignPositionJoueur(int ID, Position Pos)
    {
        if (Pos == Position.SoiMeme)
        {
            throw new ArgumentException("Cannot add a player to the same position as oneself");
        }
        var Joueur =
            this.Joueurs.Find(Joueur => Joueur.InGame.ID == ID)
            ?? throw new ArgumentException("Player not found");
        if (Joueur is SelfJoueur)
        {
            throw new ArgumentException("Cannot change the position of oneself");
        }
        Joueur.InGame.Pos = Pos;
        this.OnOtherJoueurChange();
    }

    public int RemoveJoueur(int ID)
    {
        var J = this.Joueurs.FindAll(Joueur => Joueur.InGame.ID == ID && Joueur is not SelfJoueur);
        if (J.Count > 1)
        {
            throw new DataMisalignedException(
                "There are multiple players with the same ID, internal error, this should've never happenned"
            );
        }
        foreach (var JF in J)
        {
            JF.JoueurChangeEvent -= this.OnOtherJoueurChange;
            this.Joueurs.Remove(JF);
        }
        return J.Count;
    }

    public int RemoveJoueur(Joueur Joueur)
    {
        return this.RemoveJoueur(Joueur.InGame.ID);
    }

    public async Task UpdateSelfJoueur(CancellationToken Token = default)
    {
        await this.SoiMeme.LoadSelf(Token);
    }

    public async Task UpdateAllJoueurs(CancellationToken Token = default)
    {
        await this.UpdateSelfJoueur(Token);
        foreach (var Joueur in this.Joueurs)
        {
            await Joueur.LoadSelf(Token);
        }
    }

    public void SaveSelfJoueur()
    {
        this.SoiMeme.SaveXML();
    }

    public SelfJoueur GetSelfJoueur()
    {
        return (SelfJoueur)this.SoiMeme.Clone();
    }

    public List<Joueur> GetOtherJoueurs()
    {
        return this.Joueurs.ConvertAll(Joueur => (Joueur)Joueur.Clone());
    }

    public List<Joueur> GetAllJoueurs()
    {
        var res = this.Joueurs.ConvertAll(Joueur => (Joueur)Joueur.Clone());
        res.Insert(0, (Joueur)this.SoiMeme.Clone());
        return res;
    }

    public Joueur GetLeftJoueur()
    {
        return (Joueur)this.Joueurs.Find(Joueur => Joueur.InGame.Pos == Position.Gauche).Clone();
    }

    public Joueur GetRightJoueur()
    {
        return (Joueur)this.Joueurs.Find(Joueur => Joueur.InGame.Pos == Position.Droite).Clone();
    }

    public Joueur GetTopJoueur()
    {
        return (Joueur)this.Joueurs.Find(Joueur => Joueur.InGame.Pos == Position.Haut).Clone();
    }

    public async Task ConnexionSelfJoueurAsync(
        string NomUtilisateur,
        string MotDePasse,
        CancellationToken Token = default
    )
    {
        await this.SoiMeme.ConnexionCompteAsync(NomUtilisateur, MotDePasse, Token);
        this.IsConnected = true;
    }

    public async Task CreationCompteSelfJoueur(
        string NomUtilisateur,
        string MotDePasse,
        IconeProfil IconeProfil,
        CancellationToken Token = default
    )
    {
        await this.SoiMeme.CreationCompteAsync(NomUtilisateur, MotDePasse, IconeProfil, Token);
        this.IsConnected = true;
    }

    public void DeconnexionSelfJoueur()
    {
        this.SoiMeme.DeconnexionCompte();
        this.IsConnected = false;
    }

    // Une requête est à faire ici pour mettre à jour l'icone dans l'API
    public void SetIconeSelfJoueur(IconeProfil Icone)
    {
        throw new NotImplementedException();
    }
}

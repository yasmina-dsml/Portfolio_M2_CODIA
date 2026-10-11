namespace LogiqueJeu.Joueur
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    public abstract class Joueur : ICloneable
    {
        public string NomUtilisateur { get; set; }

        public int Elo { get; set; }

        public IconeProfil IconeProfil { get; set; }

#pragma warning disable IDE0052, IDE0044
        // A placeholder for the time being until it gets potentially implemented
        // private List<Achievement> Achievements;
        public List<bool> Achievements;
#pragma warning restore IDE0052, IDE0044
        public int Score { get; set; }

        public int Niveau { get; set; }

        public int Classement { get; set; }

        public DateTime DateInscription { get; set; }

        public int NombreParties { get; set; }

        public int NombrePartiesGagnees { get; set; }

        // Not sure if this is necessary
        public bool IsInGame { get; set; }

        // public bool IsInLobby { get; set; }

        [XmlIgnore]
        public InGameDetails InGame;

        public struct InGameDetails
        {
            public int ID;
            public Position? Pos;
            public EtatTour _etatTour;
            public Chevalet Chevalet { get; set; }
            public Emote CurrentEmote { get; set; }

            public readonly EtatTour.Etats EtatTour
            {
                get { return this._etatTour.Etat; }
            }
        }

        public event EventHandler JoueurChangeEvent;

        public Joueur()
        {
            this.NomUtilisateur = Constants.ANONYMOUS_PLAYER_NAME;
            this.Elo = 0;
            this.IconeProfil = IconeProfil.Icone1;
            this.Score = 0;
            this.Niveau = 0;
            this.Classement = 0;
            this.DateInscription = DateTime.UnixEpoch;
            this.NombreParties = 0;
            this.NombrePartiesGagnees = 0;
            this.IsInGame = false;
            this.InGame = new InGameDetails
            {
                ID = -1,
                Pos = null,
                _etatTour = new EtatTour(),
                Chevalet = null,
                CurrentEmote = null
            };
        }

        public override string ToString()
        {
            var Representation =
                $@"
                                NomUtilisateur: {this.NomUtilisateur},
                                Elo: {this.Elo},
                                IconeProfil: {this.IconeProfil},
                                Achievements: {((this.Achievements != null) ? string.Join(", ", this.Achievements) : "Liste vide")},
                                Score: {this.Score},
                                Niveau: {this.Niveau},
                                Classement: {this.Classement}
                                ";
            if (this.IsInGame)
            {
                Representation +=
                    $@"

                                ID (SignalR): {this.InGame.ID},
                                Positon: {this.InGame.Pos},
                                EtatTour: {this.InGame.EtatTour},
                                Chevalet: {this.InGame.Chevalet},
                                CurrentEmote: {this.InGame.CurrentEmote}
                                ";
            }
            return Representation;
        }

        protected virtual void CopyFrom(Joueur Joueur)
        {
            this.NomUtilisateur = Joueur.NomUtilisateur;
            this.Elo = Joueur.Elo;
            this.IconeProfil = Joueur.IconeProfil;
            this.Score = Joueur.Score;
            this.Niveau = Joueur.Niveau;
            this.IsInGame = false;
        }

        public abstract Task LoadSelf(CancellationToken Token = default);

        protected abstract Task UpdateDetailsAsync(CancellationToken Token = default);

        protected virtual void OnShapeChanged(EventArgs E)
        {
            JoueurChangeEvent?.Invoke(this, E);
        }

        public virtual object Clone()
        {
            var copy = (Joueur)this.MemberwiseClone();
            copy.Achievements = (this.Achievements != null) ? new(this.Achievements) : null;
            copy.InGame._etatTour = (EtatTour)this.InGame._etatTour.Clone();
            copy.JoueurChangeEvent = null;
            return copy;
        }
    }
}

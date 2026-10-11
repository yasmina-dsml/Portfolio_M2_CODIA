namespace LogiqueJeu.Joueur
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Serialization;
    using UnityEngine;

    public sealed class SelfJoueur : Joueur
    {
        public string TokenConnexion { get; set; }

        public SelfJoueur()
            : base()
        {
            this.TokenConnexion = null;
            this.InGame.Pos = Position.SoiMeme;
        }

        public override async Task LoadSelf(CancellationToken Token = default)
        {
#if !LOCAL
            if (File.Exists(Application.persistentDataPath + Constants.SELF_PLAYER_SAVE_FILE))
            {
                TextReader reader = null;
                try
                {
                    var serializer = new XmlSerializer(typeof(SelfJoueur));
                    reader = new StreamReader(
                        Application.persistentDataPath + Constants.SELF_PLAYER_SAVE_FILE
                    );
                    this.CopyFrom((SelfJoueur)serializer.Deserialize(reader));
                }
                finally
                {
                    reader?.Close();
                }
            }
#endif
            if (this.TokenConnexion != null && this.NomUtilisateur != null)
            {
                await this.UpdateDetailsAsync(Token);
            }
        }

        public void SaveXML()
        {
#if !LOCAL
            if (this.TokenConnexion == null)
            {
                return;
            }
            TextWriter writer = null;
            try
            {
                var serializer = new XmlSerializer(this.GetType());
                writer = new StreamWriter(
                    Application.persistentDataPath + Constants.SELF_PLAYER_SAVE_FILE
                );
                serializer.Serialize(writer, this);
            }
            finally
            {
                writer?.Close();
            }
#endif
        }

        public void DeleteXML()
        {
            try
            {
                File.Delete(Application.persistentDataPath + Constants.SELF_PLAYER_SAVE_FILE);
            }
            catch (Exception e)
            {
                if (e is DirectoryNotFoundException or NotSupportedException)
                {
                    Debug.Log("Could not delete SelfJoueur save file");
                    return;
                }
                throw;
            }
        }

        protected override async Task UpdateDetailsAsync(CancellationToken Token = default)
        {
            var Response = await API.FetchSelfJoueurAsync(
                this.NomUtilisateur,
                this.TokenConnexion,
                Token
            );
            this.NomUtilisateur = Response.username;
            this.IconeProfil = (IconeProfil)Response.photo;
            this.Score = Response.experience;
            this.Elo = Response.elo;
            this.DateInscription = Response.dateInscription;
            this.NombreParties = Response.nombreParties;
            this.NombrePartiesGagnees = Response.nombrePartiesGagnees;
            this.Achievements = new(Response.achievements);
            this.SaveXML();
            this.OnShapeChanged(EventArgs.Empty);
        }

        protected override void OnShapeChanged(EventArgs E)
        {
            base.OnShapeChanged(E);
        }

        private void CopyFrom(SelfJoueur SelfJoueur)
        {
            base.CopyFrom(SelfJoueur);
            this.TokenConnexion = SelfJoueur.TokenConnexion;
        }

        public async Task ConnexionCompteAsync(
            string NomUtilisateur,
            string MotDePasse,
            CancellationToken Token = default
        )
        {
            var Response = await API.PostSelfJoueurConnexionAsync(
                new SelfJoueurAPIConnexionDTO(NomUtilisateur, MotDePasse),
                Token
            );
            this.NomUtilisateur = Response.username;
            this.TokenConnexion = Response.token;
            this.SaveXML();
            await this.LoadSelf(Token);
        }

        public async Task CreationCompteAsync(
            string NomUtilisateur,
            string MotDePasse,
            IconeProfil IconeProfil,
            CancellationToken Token = default
        )
        {
            var Response = await API.PostSelfJoueurCreationAsync(
                new SelfJoueurAPICreationDTO(NomUtilisateur, MotDePasse, (int)IconeProfil),
                Token
            );
            this.NomUtilisateur = Response.username;
            this.TokenConnexion = Response.token;
            this.SaveXML();
            await this.LoadSelf(Token);
        }

        public void DeconnexionCompte()
        {
            this.DeleteXML();
            this.CopyFrom(new SelfJoueur());
        }

        public override string ToString()
        {
            return base.ToString()
                + $@"

                                TokenConnexion: {this.TokenConnexion}
                                ";
        }

        public override object Clone()
        {
            var copy = (SelfJoueur)base.Clone();
            copy.TokenConnexion = this.TokenConnexion;
            return copy;
        }
    }
}

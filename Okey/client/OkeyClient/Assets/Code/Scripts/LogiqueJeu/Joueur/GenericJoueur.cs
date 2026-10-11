namespace LogiqueJeu.Joueur
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class GenericJoueur : Joueur
    {
        protected override async Task UpdateDetailsAsync(CancellationToken Token = default)
        {
            var Response = await API.FetchJoueurAsync(this.NomUtilisateur, Token);
            this.NomUtilisateur = Response.username;
            this.IconeProfil = (IconeProfil)Response.photo;
            this.Score = Response.experience;
            this.Elo = Response.elo;
            this.DateInscription = Response.dateInscription;
            this.NombreParties = Response.nombreParties;
            this.NombrePartiesGagnees = Response.nombrePartiesGagnees;
            this.OnShapeChanged(EventArgs.Empty);
        }

        public override async Task LoadSelf(CancellationToken Token = default)
        {
            await this.UpdateDetailsAsync(Token);
        }
    }
}

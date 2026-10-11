namespace LogiqueJeu.Joueur
{
    using System;

    public class SelfJoueurAPICompteDTO
    {
        public string username { get; set; }
        public int photo { get; set; }
        public int experience { get; set; }
        public int elo { get; set; }
        public DateTime dateInscription { get; set; }
        public bool[] achievements { get; set; }
        public int nombreParties { get; set; }
        public int nombrePartiesGagnees { get; set; }

        public override string ToString()
        {
            return $"NomUtilisateur: {this.username}, Photo: {this.photo}, Experience: {this.experience}, Elo: {this.elo}, DateInscription: {this.dateInscription}, Achievements: {string.Join(", ", this.achievements)}, NombreParties: {this.nombreParties}, NombrePartiesGagnees: {this.nombrePartiesGagnees}";
        }
    }
}

namespace LogiqueJeu.Joueur
{
    public class JoueurAPIClassementDTO
    {
        public string username { get; set; }
        public int classement { get; set; }
        public int elo { get; set; }

        public override string ToString()
        {
            return $"NomUtilisateur: {this.username}, Classement: {this.classement}, Elo: {this.elo}";
        }
    }
}

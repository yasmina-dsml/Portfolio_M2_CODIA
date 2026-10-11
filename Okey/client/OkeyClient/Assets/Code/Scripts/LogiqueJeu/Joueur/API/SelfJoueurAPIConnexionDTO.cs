namespace LogiqueJeu.Joueur
{
    public class SelfJoueurAPIConnexionDTO
    {
        public string username { get; set; }
        public string password { get; set; }

        public SelfJoueurAPIConnexionDTO(string username, string password)
        {
            this.username = username;
            this.password = password;
        }
    }
}

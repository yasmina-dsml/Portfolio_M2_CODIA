namespace LogiqueJeu.Joueur
{
    public class SelfJoueurAPICreationDTO
    {
        public string username { get; set; }
        public string password { get; set; }
        public int photo { get; set; }

        public SelfJoueurAPICreationDTO(string username, string password, int photo)
        {
            this.username = username;
            this.password = password;
            this.photo = photo;
        }
    }
}

namespace LogiqueJeu.Joueur
{
    public class SelfJoueurAPIConnexionResponseDTO
    {
        public string username { get; set; }
        public string token { get; set; }

        public SelfJoueurAPIConnexionResponseDTO(string username, string token)
        {
            this.username = username;
            this.token = token;
        }

        public override string ToString()
        {
            return $"username: {this.username}, token: {this.token}";
        }
    }
}

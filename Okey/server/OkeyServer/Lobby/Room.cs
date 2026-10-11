namespace OkeyServer.Lobby
{
    /// <summary>
    /// Représente une salle de jeu.
    /// </summary>
    public class Room
    {
        /// <summary>
        /// Obtient le nom de la salle.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Obtient la capacité maximale de la salle.
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        /// Obtient ou définit la liste des ID des joueurs dans la salle.
        /// </summary>
        public List<string> Players { get; set; }

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="Room"/> avec un nom spécifié et une capacité optionnelle.
        /// </summary>
        /// <param name="name">Le nom de la salle.</param>
        /// <param name="capacity">La capacité maximale de la salle. La valeur par défaut est 4.</param>
        public Room(string name, int capacity = 4)
        {
            this.Name = name;
            this.Capacity = capacity;
            this.Players = new List<string>();
        }

        /// <summary>
        /// Détermine si la salle est pleine.
        /// </summary>
        /// <returns><c>true</c> si la salle est pleine ; sinon, <c>false</c>.</returns>
        public bool IsFull() => this.Players.Count >= this.Capacity;

        /// <summary>
        /// Détermine si la salle est vide.
        /// </summary>
        /// <returns><c>true</c> si la salle est vide ; sinon, <c>false</c>.</returns>
        public bool IsEmpty() => this.Players.Count == 0;

        /// <summary>
        /// Ajoute un joueur à la salle.
        /// </summary>
        /// <param name="playerId">L'ID du joueur à ajouter.</param>
        public void AddPlayer(string playerId)
        {
            if (!this.Players.Contains(playerId))
            {
                this.Players.Add(playerId);
            }
        }

        /// <summary>
        /// Supprime un joueur de la salle.
        /// </summary>
        /// <param name="playerId">L'ID du joueur à supprimer.</param>
        public void RemovePlayer(string playerId) => this.Players.Remove(playerId);

        /// <summary>
        /// Vérifie si un joueur est dans la salle.
        /// </summary>
        /// <param name="playerId">L'ID du joueur à vérifier.</param>
        /// <returns><c>true</c> si le joueur est dans la salle ; sinon, <c>false</c>.</returns>
        public bool HasPlayer(string playerId) => this.Players.Contains(playerId);

        /// <summary>
        /// Obtient la liste des ID des joueurs dans la salle.
        /// </summary>
        /// <returns>Une liste des ID des joueurs dans la salle.</returns>
        public List<string> GetPlayerIds() => this.Players;
    }
}

namespace Okey.Joueurs
{
    using Okey.Game;

    /// <summary>
    /// Cette classe représente un bot dans le jeu.
    /// </summary>
    public class Bot : Joueur
    {
        /// <summary>
        /// La difficulté du bot.
        /// </summary>
        private int difficulte;

        /// <summary>
        /// Initialise une nouvelle instance de la classe Bot avec une difficulté spécifiée.
        /// </summary>
        /// <param name="diff">Niveau de difficulté du bot.</param>
        public Bot(int diff)
            : base(0, "BOT")
        {
            this.difficulte = diff;
        }

        /// <summary>
        /// Met à jour l'Elo du bot. Ne fait rien car le bot n'a pas de Elo.
        /// </summary>
        /// <param name="j">Instance du jeu.</param>
        public override void UpdateElo(Jeu j)
        {
            // n'a pas de ELo -> ne fait rien (à discuter)
        }

        /// <summary>
        /// Indique que le bot a gagné le jeu.
        /// </summary>
        /// <param name="j">Instance du jeu.</param>
        public override void Gagne(Jeu j)
        {
            Console.Write($"le gagnant est {this}.");
        }

        /// <summary>
        /// Obtient le niveau de difficulté du bot.
        /// </summary>
        /// <returns>Le niveau de difficulté.</returns>
        public int GetDifficulte()
        {
            return this.difficulte;
        }

        /// <summary>
        /// Retourne une chaîne qui représente le bot.
        /// </summary>
        /// <returns>Chaîne représentant le bot.</returns>
        public override string ToString()
        {
            return $"Bot de difficulté : {this.difficulte}";
        }
    }
}

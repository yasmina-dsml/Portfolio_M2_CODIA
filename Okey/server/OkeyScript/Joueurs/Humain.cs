namespace Okey.Joueurs
{
    using Okey.Game;

    /// <summary>
    /// Cette classe représente un humain dans le jeu.
    /// </summary>
    public class Humain : Joueur
    {
        /// <summary>
        /// Le nombre de points Elo de l'humain.
        /// </summary>
        private int nbElo;

        /// <summary>
        /// Le classement de l'humain.
        /// </summary>
        private int Rank;

        /// <summary>
        /// Initialise une nouvelle instance de la classe Humain avec un identifiant, un nom et un Elo spécifiés.
        /// </summary>
        /// <param name="id">Identifiant de l'humain.</param>
        /// <param name="Name">Nom de l'humain.</param>
        /// <param name="elo">Nombre de points Elo de l'humain.</param>
        public Humain(int id, string Name, int elo)
            : base(id, Name)
        {
            this.nbElo = elo;
            this.Rank = -1; // au début le joueur n'est pas classé
        }

        /// <summary>
        /// Met à jour le nombre de points Elo de l'humain.
        /// </summary>
        /// <param name="j">Instance du jeu.</param>
        public override void UpdateElo(Jeu j)
        {
            this.nbElo += Elo.CalculElo(j, this);
        }

        /// <summary>
        /// Indique que l'humain a gagné le jeu.
        /// </summary>
        /// <param name="j">Instance du jeu.</param>
        public override void Gagne(Jeu j)
        {
            if (this.isGagnant())
                Console.WriteLine($"Gagnant est : {Name}");

            Console.Write($"{Name}, ancien Elo = {nbElo}, ");
            this.UpdateElo(j);
            Console.WriteLine($"nouveau Elo = {nbElo}.");
        }

        /// <summary>
        /// Obtient le classement de l'humain.
        /// </summary>
        /// <returns>Le classement.</returns>
        public int GetRank()
        {
            return this.Rank;
        }

        /// <summary>
        /// Obtient le nombre de points Elo de l'humain.
        /// </summary>
        /// <returns>Le nombre de points Elo.</returns>
        public int GetElo()
        {
            return this.nbElo;
        }

        /// <summary>
        /// Retourne une chaîne qui représente l'humain.
        /// </summary>
        /// <returns>Chaîne représentant l'humain.</returns>
        public override string ToString()
        {
            return $"{this.Name}";
        }
    }
}

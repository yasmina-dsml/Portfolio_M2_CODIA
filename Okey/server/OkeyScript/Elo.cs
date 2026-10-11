namespace Okey
{
    using Okey.Game;
    using Okey.Joueurs;

    /// <summary>
    /// Classe pour le calcul du Elo dans le jeu.
    /// </summary>
    public class Elo
    {
        /*// Nom d'utilisateur : Ancien Elo
        private Dictionary<string, int> UsersData;
        private string winnerName;*/

        /// <summary>
        /// Valeur maximale de K.
        /// </summary>
        private static readonly int K_max = 32;

        /// <summary>
        /// Valeur minimale de K.
        /// </summary>
        private static readonly int K_min = 4;

        /*public Elo(Dictionary<string, int> UsersData, string winnerName)
        {
            this.winnerName = winnerName;
            this.UsersData = UsersData;
        }*/

        /// <summary>
        /// Calcule la valeur de K pour un joueur.
        /// </summary>
        /// <param name="j">Instance du joueur.</param>
        /// <returns>Valeur de K calculée.</returns>
        public static int ComputeK(Joueur j)
        {
            // Pour calculer le K, on va travailler avec la fonction de décroissance (comme en décroissance radioactive)
            // La différence de K est proportionnelle à la différence du nombre des matches multiplié par l'opposé de K et une constante qui détermine le taux de changement de K
            // c-à-d : delta(K) = - alpha * K * delta(nombrePartiesGagne), alors pour des variations instantanées et fines, la formule nous donne une équation différentielle dont la solution est :
            // K(n) = K0 * exp(-alpha * nombrePartie).
            // mais cette fonction décroissante converge vers 0 pour les joueurs expérimentés, donc faut ajouter une valeur de K minimum, pour gérer la convergence
            // alors la fonction finale est : K(n) = Kmin + (K0 - Kmin) * exp(-alpha * nombrePartiesGagne) avec K0 l'abscisse à l'origine et c'est le K pour un joueur nouveau qui a 0 de PartieJoue.

            var nbrPartieGagne = j.GetPartieGagne();
            var winRate = j.GetWinRate();
            var alpha = 0.02; // taux de décroissance de la fonction

            if (winRate < 0.1)
                return K_max;
            if (winRate < 0.3)
                return 29;
            if (winRate < 0.4)
                return 26;

            // Si son winrate est positif alors utiliser la formule
            return (int)Math.Round(K_min + ((K_max - K_min) * Math.Exp(-alpha * nbrPartieGagne))); // Si il gagne trop on diminue le K
        }

        /*
        private int Compute_ExpectationPlayer(string PlayerName) // Je pense c'est mieux de travailler avec joueur en argument
        {
            var moyenneElos = 0;
            foreach (var user in this.UsersData)
            {
                if (!user.Key.Equals(PlayerName, StringComparison.Ordinal))
                {
                    moyenneElos += user.Value;
                }
            }
            moyenneElos /= 3;
            var playerElo = this.UsersData[PlayerName];
            return (int)(1 / (1 + double.Pow(10, (playerElo - moyenneElos) / 400.0))); // 400 est en float pour éviter que la portion de fraction soit perdue.
        }
        */

        /// <summary>
        /// Calcule l'espérance de victoire d'un joueur.
        /// </summary>
        /// <param name="j">Instance du jeu.</param>
        /// <param name="pl">Instance du joueur.</param>
        /// <returns>Espérance de victoire calculée.</returns>
        private static double Compute_ExpectationPlayer(Jeu j, Joueur pl)
        {
            var moyenneElos = 0;
            foreach (var joueur in j.GetJoueurs())
            {
                if (
                    !string.Equals(
                        joueur.getName(),
                        pl.getName(),
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    if (joueur is Humain)
                    {
                        moyenneElos += ((Humain)joueur).GetElo();
                    }
                    else if (joueur is Bot)
                    {
                        moyenneElos += ((Bot)joueur).GetDifficulte();
                    }
                }
            }

            moyenneElos /= 3;
            int playerElo = 0; // Assigne 0 pour contourner l'erreur, soit le if soit le elseif qui marchera
            if (pl is Humain)
                playerElo = ((Humain)pl).GetElo();
            else if (pl is Bot)
                playerElo = ((Bot)pl).GetDifficulte();

            return (double)(1 / (1 + double.Pow(10, (playerElo - moyenneElos) / 400.0))); // 400 est en float pour éviter que la portion de fraction soit perdue.
        }

        /*
        public Dictionary<string, int> CalculElo()
        {
            var newElos = new Dictionary<string, int>();
            foreach (var user in this.UsersData)
            {
                if (user.Key.Equals(this.winnerName, StringComparison.Ordinal))
                {
                    newElos.TryAdd(
                        this.winnerName,
                        Compute_K(null) * (1 - this.Compute_ExpectationPlayer(user.Key))
                    );
                }
                else
                {
                    newElos.TryAdd(
                        this.winnerName,
                        Compute_K(null) * (0 - this.Compute_ExpectationPlayer(user.Key))
                    );
                }
            }
            return newElos;
        }
        */

        /// <summary>
        /// Calcule le nouveau Elo d'un joueur après une partie.
        /// </summary>
        /// <param name="j">Instance du jeu.</param>
        /// <param name="pl">Instance du joueur.</param>
        /// <returns>Nouvelle valeur de Elo calculée.</returns>
        // À la fin du jeu, on itérera sur tous les joueurs en appelant CalculElo afin de faire l'update de leurs Elo
        public static int CalculElo(Jeu j, Joueur pl)
        {
            // Le Si = 1 pour le winner, sinon 0
            // Si on implémente le système de in-game score, faut normaliser le Si pour qu'il soit dans l'intervalle [0,1], et pour cela Si = in-game_score_i / max_in_score pour le joueur
            var Si = pl.isGagnant() ? 1 : 0;

            // Console.WriteLine($"{pl} : compute = {Compute_ExpectationPlayer(j, pl)}");
            return (int)Math.Round(pl.GetK() * (Si - Compute_ExpectationPlayer(j, pl))); // getK() est correct puisque à l'instanciation de chaque joueur, on lui calcule un K avec la méthode Compute_K (voir constructeur de Joueur)
        }

        /// <summary>
        /// Renvoie un dictionnaire contenant les nouveaux Elo et les noms des joueurs.
        /// </summary>
        /// <param name="j">Instance du jeu.</param>
        /// <returns>Dictionnaire avec les nouveaux Elo.</returns>
        // Retourne un dict contenant les nouveaux Elo et les noms des joueurs {player_name1 : elo1, player_name_2 : elo2 ....}
        public static Dictionary<string, int> GetEloDataDict(Jeu j)
        {
            Dictionary<string, int> EloData = [];

            foreach (var player in j.GetJoueurs())
            {
                _ = EloData.TryAdd(
                    player.getName(),
                    (player is Humain) ? ((Humain)player).GetElo() : ((Bot)player).GetDifficulte()
                );
            }

            return EloData;
        }
    }
}

using Okey.Game;
using Okey.Joueurs;

namespace Okey
{
    /// <summary>
    /// Classe principale pour démarrer le jeu.
    /// </summary>
    public class Start
    {
        /// <summary>
        /// Point d'entrée principal pour démarrer le jeu.
        /// </summary>
        /// <param name="args">Arguments de ligne de commande.</param>
        /// <returns>Tâche asynchrone.</returns>
        public static async Task Main(string[] args)
        {
            Joueur[] Joueurs =
            {
                new Humain(1, "mohammed", 800),
                new Humain(2, "Emin", 1100),
                new Humain(3, "toto", 1760),
                //new Bot(50)
                new Humain(4, "test", 1240)
            };
            Jeu j = new Jeu(1, Joueurs);

            j.DistibuerTuile(); // on commence
            Console.WriteLine("Tuiles distribuées.\n");

            Joueur? joueurStarter = j.getJoueurActuel();
            j.AfficheChevaletActuel();

            Console.Write("choisis la tuile à jeter (donner ces coords y x) ou 'Move' : ");
            string? action = Console.ReadLine();

            while (string.Equals(action, "move", StringComparison.OrdinalIgnoreCase)) //action == "move"
            {
                MoveInLoop(joueurStarter, j);
                joueurStarter?.AfficheChevalet();
                Console.Write("choisis la tuile à jeter (donner ces coords y x) ou 'Move' : ");
                action = Console.ReadLine();
            }

            Coord coords = readCoord(action);
            joueurStarter?.JeterTuile(coords, j);

            bool doitJete = false;

            while (!j.isTermine())
            {
                Joueur? joueurActuel = j.getJoueurActuel();

                Console.WriteLine($"\nc'est le tour de {joueurActuel?.getName()}:");
                j.AfficheChevaletActuel();

                //le joueur pioche
                if (!doitJete)
                {
                    Console.Write("choisis de où piocher ('Centre' ou 'Defausse') ou 'Move': ");
                    //string? ouPiocher = "centre"; // pour tester
                    string? ouPiocher = await GetUserInputAsync();
                    ;

                    if (ouPiocher == null)
                    {
                        Console.WriteLine("\nTime elapsed, Tuile piochée du Centre!");
                        joueurActuel?.PiocherTuile("Centre", j);
                        Console.WriteLine("\nmaintenant vous devez jeter une tuile.");
                    }
                    else if (string.Equals(ouPiocher, "move", StringComparison.OrdinalIgnoreCase))
                    {
                        MoveInLoop(joueurActuel, j);
                        continue;
                    }
                    else if (
                        string.Equals(ouPiocher, "Centre", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(ouPiocher, "Defausse", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        joueurActuel?.PiocherTuile(ouPiocher, j);
                        if (j.isTermine())
                            break;
                        Console.WriteLine("\nmaintenant vous devez jeter une tuile.");
                    }
                    else
                    { // s'il a pas taper centre ou defausse
                        continue;
                    }
                }

                j.AfficheChevaletActuel();
                //le joueur jete

                Console.Write(
                    "choisis la tuile à jeter (donner ces coords y x) ou taper 'Move' ou 'Gagner': "
                );
                //string? coordStr = "0 0";
                string? coordStr = await GetUserInputAsync();

                if (coordStr == null)
                {
                    var JeterTuileAlea = joueurActuel?.GetRandomTuileCoords();
                    joueurActuel?.JeterTuile(JeterTuileAlea, j);
                    Console.Write("\nTuile aleatoire jetee");
                    Console.WriteLine($"Tuile jetee de coordonnees : {JeterTuileAlea} !");
                }
                else if (string.Equals(coordStr, "move", StringComparison.OrdinalIgnoreCase))
                {
                    MoveInLoop(joueurActuel, j);
                    doitJete = true;
                    continue;
                }
                else if (string.Equals(coordStr, "gagner", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Write("donner les coords de la tuile que vous voulez finir avec : ");
                    string? coordsToFinish = Console.ReadLine();
                    Coord c = readCoord(coordsToFinish);
                    joueurActuel?.JeteTuilePourTerminer(c, j);
                    doitJete = true;
                    continue;
                }
                else
                {
                    Coord coordos = readCoord(coordStr);
                    joueurActuel?.JeterTuile(coordos, j);
                }
                doitJete = false;
            }

            //return Task.CompletedTask;
        }

        /// <summary>
        /// Séparateurs pour la lecture des coordonnées.
        /// </summary>
        internal static readonly char[] separator = new char[] { ' ' };

        /// <summary>
        /// Lecture des coordonnées à partir d'une chaîne.
        /// </summary>
        /// <param name="str">Chaîne contenant les coordonnées.</param>
        /// <returns>Instance de Coord avec les coordonnées lues.</returns>
        public static Coord readCoord(String? str)
        {
            if (str == null)
                return new Coord(-1, -1); // renvoie une erreur (on doit garantir qu'on ne passera jamais ici)

            string[] parts = str.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            return new Coord(
                int.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture)
            );
        }

        /// <summary>
        /// Déplace une tuile dans le chevalet d'un joueur.
        /// </summary>
        /// <param name="pl">Instance du joueur.</param>
        /// <param name="j">Instance du jeu.</param>
        public static void MoveInLoop(Joueur? pl, Jeu j)
        {
            Console.Write("Donner les coords de la tuile à deplacer (y x): ");
            Coord from = readCoord(Console.ReadLine());
            Console.Write("Donner les coords d'où la mettre (y x): ");
            Coord to = readCoord(Console.ReadLine());
            pl?.MoveTuileChevalet(from, to, j);
        }

        /// <summary>
        /// Attend une entrée utilisateur de manière asynchrone.
        /// </summary>
        /// <returns>Entrée utilisateur ou null si le délai est dépassé.</returns>
        static async Task<string?> GetUserInputAsync()
        {
            var userInputTask = Task.Run(() => Console.ReadLine()); // Exécute Console.ReadLine de manière asynchrone

            // Attend soit l'entrée utilisateur soit un délai de 20 secondes
            await Task.WhenAny(userInputTask, Task.Delay(20000));

            // Si userInputTask est terminé avant le délai, retourne son résultat
            if (userInputTask.IsCompleted)
                return userInputTask.Result;
            else
                return null; // Délai dépassé
        }
    }
}

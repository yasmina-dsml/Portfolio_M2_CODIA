using System;
using System.Runtime.CompilerServices;
using Okey.Game;
using Okey.Tuiles;

namespace Okey.Joueurs
{
    /// <summary>
    /// Classe abstraite représentant un joueur dans le jeu.
    /// </summary>
    public abstract class Joueur
    {
        /// <summary>
        /// Identifiant du joueur.
        /// </summary>
        private int id;

        /// <summary>
        /// Nom du joueur.
        /// </summary>
        private String name;

        /// <summary>
        /// Chevalet du joueur, contenant les tuiles.
        /// </summary>
        private List<List<Tuile?>> chevalet = new List<List<Tuile?>>();

        /// <summary>
        /// Indique si c'est le tour du joueur.
        /// </summary>
        private bool tour;

        /// <summary>
        /// Indique si le joueur peut piocher.
        /// </summary>
        private bool peutPiocher;

        /// <summary>
        /// Indique si le joueur est gagnant.
        /// </summary>
        private bool gagnant;

        /// <summary>
        /// Pile des tuiles défaussées.
        /// </summary>
        private Stack<Tuile> defausse = new Stack<Tuile>();

        /// <summary>
        /// Constante pour le calcul d'Elo.
        /// </summary>
        private int K;

        /// <summary>
        /// Nombre de parties jouées.
        /// </summary>
        private int partieJoue = 28; // à extraire

        /// <summary>
        /// Nombre de parties gagnées.
        /// </summary>
        private int partieGagne = 28; // à extraire

        /// <summary>
        /// Nombre d'étages dans le chevalet.
        /// </summary>
        static readonly int etage = 2;

        /// <summary>
        /// Nombre de tuiles par étage.
        /// </summary>
        static readonly int tuilesDansEtage = 14;

        /// <summary>
        /// Générateur de nombres aléatoires.
        /// </summary>
        private Random random = new Random();

        /// <summary>
        /// Score initial du joueur.
        /// </summary>
        private int score = 7;

        /// <summary>
        /// Initialise une nouvelle instance de la classe Joueur avec un identifiant et un nom spécifiés.
        /// </summary>
        /// <param name="id">Identifiant du joueur.</param>
        /// <param name="Name">Nom du joueur.</param>
        public Joueur(int id, String Name)
        {
            this.id = id;
            this.name = Name;
            this.tour = false;
            this.peutPiocher = false;
            this.gagnant = false;

            this.K = Elo.ComputeK(this);

            // Initialisation du chevalet (prêt à recevoir des tuiles)
            for (int i = 0; i < etage; i++)
            {
                this.chevalet.Add(new List<Tuile?>());

                for (int j = 0; j < tuilesDansEtage; j++)
                {
                    this.chevalet[i].Add(null);
                }
            }
        }

        /// <summary>
        /// Obtient ou définit l'identifiant du joueur.
        /// </summary>
        protected int Id
        {
            get => id;
            set => id = value;
        }

        /// <summary>
        /// Obtient ou définit le nom du joueur.
        /// </summary>
        protected string Name
        {
            get => name;
            set => name = value;
        }

        /// <summary>
        /// Obtient ou définit le chevalet du joueur.
        /// </summary>
        protected List<List<Tuile?>> Chevalet
        {
            get => chevalet;
            set => chevalet = value;
        }

        /// <summary>
        /// Obtient ou définit une valeur indiquant si c'est le tour du joueur.
        /// </summary>
        protected bool Tour
        {
            get => tour;
            set => tour = value;
        }

        /// <summary>
        /// Obtient ou définit une valeur indiquant si le joueur est gagnant.
        /// </summary>
        protected bool Gagnant
        {
            get => gagnant;
            set => gagnant = value;
        }

        /// <summary>
        /// Obtient ou définit la pile des tuiles défaussées.
        /// </summary>
        protected Stack<Tuile> Defausse
        {
            get => defausse;
            set => defausse = value;
        }

        /// <summary>
        /// Méthode abstraite pour indiquer que le joueur a gagné.
        /// </summary>
        /// <param name="j">Instance du jeu.</param>
        public abstract void Gagne(Jeu j);

        /// <summary>
        /// Méthode abstraite pour mettre à jour le Elo du joueur.
        /// </summary>
        /// <param name="j">Instance du jeu.</param>
        public abstract void UpdateElo(Jeu j);

        /// <summary>
        /// Indique que ce n'est plus le tour du joueur.
        /// </summary>
        public void EstPlusTour()
        {
            this.Tour = false;
        }

        /// <summary>
        /// Indique que c'est le tour du joueur.
        /// </summary>
        public void Ajouer()
        {
            this.Tour = true;
        }

        /// <summary>
        /// Indique que le joueur peut piocher.
        /// </summary>
        public void Apiocher()
        {
            this.peutPiocher = true;
        }

        /// <summary>
        /// Indique que le joueur ne peut pas piocher.
        /// </summary>
        public void PeutPasPiocher()
        {
            this.peutPiocher = false;
        }

        /// <summary>
        /// Ajoute une tuile au chevalet du joueur.
        /// </summary>
        /// <param name="t">Tuile à ajouter.</param>
        public void AjoutTuileChevalet(Tuile? t)
        {
            for (int j = 0; j < etage; j++)
            {
                for (int i = 0; i < tuilesDansEtage; i++)
                {
                    if (this.chevalet[j][i] == null)
                    {
                        this.chevalet[j][i] = t;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Jette une tuile du chevalet à la défausse.
        /// </summary>
        /// <param name="c">Coordonnées de la tuile.</param>
        /// <param name="j">Instance du jeu.</param>
        public void JeterTuile(Coord? c, Jeu j)
        {
            if (this.tour)
            {
                if (this == null || c == null)
                    return;

                int x = c.getX();
                int y = c.getY();

                if (x >= 0 && x < tuilesDansEtage && (y >= 0 || y < etage))
                {
                    Tuile? t = this.chevalet[y][x];
                    if (t == null)
                    {
                        Console.WriteLine("erreur dans JeterTuile. (pas de tuile)");
                        return;
                    }
                    this.chevalet[y][x] = null; // enlever la tuile du chevalet
                    t?.SetDefause(); // devient défausse
                    this.JeteTuileDefausse(t); // la poser dans la défausse
                    this.EstPlusTour(); // plus son tour
                    j.setJoueurActuel(j.getNextJoueur(this)); // passer le tour au prochain joueur

                    Console.WriteLine($"{this.Name} a jeté la tuile {t}\n");

                    if (t != null)
                        j.AddToListeDefausse(t);
                }
                else
                {
                    Console.WriteLine("erreur dans JeterTuile. (coords invalides)");
                }
            }
            else
            {
                Console.WriteLine("Pas ton tour.");
            }
        }

        /// <summary>
        /// Pioche une tuile d'une source spécifiée.
        /// </summary>
        /// <param name="OuPiocher">Source de la pioche ("Centre" ou "Defausse").</param>
        /// <param name="j">Instance du jeu.</param>
        /// <returns>La tuile piochée ou null si aucune tuile disponible.</returns>
        public Tuile? PiocherTuile(string? OuPiocher, Jeu j)
        {
            if (this.peutPiocher && this.tour)
            {
                if (OuPiocher == null)
                {
                    Console.WriteLine("OuPiocher est null");
                    return null;
                }

                if (string.Equals(OuPiocher, "Centre", StringComparison.OrdinalIgnoreCase))
                {
                    var tuilePiochee = j.PopPiocheCentre();

                    this.AjoutTuileChevalet(tuilePiochee);

                    if (j.isPiocheCentreEmpty())
                    {
                        Console.WriteLine("\nLa pile au centre est vide, jeu terminé.");
                        j.JeuTermine();
                    }
                }
                else if (string.Equals(OuPiocher, "Defausse", StringComparison.OrdinalIgnoreCase))
                {
                    Joueur PreviousPlayer = j.getPreviousPlayer(this);

                    if (PreviousPlayer.isDefausseEmpty())
                    {
                        Console.WriteLine("La défausse du joueur est vide, impossible à piocher");
                    }
                    else
                    {
                        Tuile tuilePiochee = PreviousPlayer.PopDefausseJoueur();
                        this.AjoutTuileChevalet(tuilePiochee);

                        if (PreviousPlayer.defausse.Count > 1)
                        {
                            return PreviousPlayer.defausse.Peek();
                        }
                    }
                }

                this.PeutPasPiocher();
            }
            return null;
        }

        /// <summary>
        /// Jette la 15ème tuile pour déclarer la victoire.
        /// </summary>
        /// <param name="c">Coordonnées de la tuile.</param>
        /// <param name="j">Instance du jeu.</param>
        /// <returns>Vrai si la tuile a été jetée pour terminer, sinon faux.</returns>
        public bool JeteTuilePourTerminer(Coord c, Jeu j)
        {
            if (this.tour)
            {
                if (this.CountTuileDansChevalet() == 14)
                {
                    Console.WriteLine("Pioche une tuile!!");
                    return false;
                }

                int x = c.getX();
                int y = c.getY();
                Tuile? toThrow = this.chevalet[y][x];

                // debugging
                Console.WriteLine("tuile jetée fin de partie : x : " + x + " y : " + y);
                for (int i2 = 0; i2 < etage; i2++)
                {
                    for (int j2 = 0; j2 < tuilesDansEtage; j2++)
                    {
                        Console.Write(this.chevalet[i2][j2]?.ToString() + " : ");
                    }
                    Console.WriteLine();
                }
                // fin debugging


                this.chevalet[y][x] = null;
                if (this.VerifSerieChevalet()) // ici le joueur gagne
                {
                    j.PushPiocheCentre(toThrow); // on met la tuile que le joueur désire finir avec sur la pioche
                    j.JeuTermine(this); // ici on met à jour le Elo et K
                    return true;
                }
                else
                {
                    Console.WriteLine("Vous n'avez pas de série dans votre chevalet !");
                    this.chevalet[y][x] = toThrow; // annuler les changements
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Déplace une tuile dans le chevalet.
        /// </summary>
        /// <param name="from">Coordonnées de départ.</param>
        /// <param name="to">Coordonnées d'arrivée.</param>
        /// <param name="j">Instance du jeu.</param>
        public void MoveTuileChevalet(Coord from, Coord to, Jeu j)
        {
            (int yFrom, int xFrom) = (from.getY(), from.getX());
            (int yTo, int xTo) = (to.getY(), to.getX());

            Tuile? tmpTuileTo = this.chevalet[to.getY()][to.getX()];

            this.chevalet[yTo][xTo] = this.chevalet[yFrom][xFrom];
            this.chevalet[yFrom][xFrom] = tmpTuileTo;
        }

        /// <summary>
        /// Vérifie si une liste de tuiles est une série de même chiffre.
        /// </summary>
        /// <param name="tuiles">Liste de tuiles.</param>
        /// <returns>Vrai si c'est une série de même chiffre, sinon faux.</returns>
        private static bool EstSerieDeMemeChiffre(List<Tuile> tuiles)
        {
            if (tuiles.Count <= 2)
                return false;
            List<CouleurTuile> CouleurVues = new List<CouleurTuile>();
            int premierNum = tuiles[0].GetNum();

            foreach (Tuile tuile in tuiles)
            {
                if (tuile is Okay)
                    continue;
                if (premierNum != tuile.GetNum())
                    return false; // deux tuiles pas même numéro -> faux

                if (CouleurVues.Contains(tuile.GetCouleur()))
                    return false; // deux tuiles de même couleur -> faux
                // renvoie faux automatiquement si y a > 4 Tuiles
                // car il n'y a que 4 couleurs
                CouleurVues.Add(tuile.GetCouleur()); // on mémorise la couleur vue
            }

            return true;
        }

        /// <summary>
        /// Vérifie si une liste de tuiles est une série de même couleur.
        /// </summary>
        /// <param name="t">Liste de tuiles.</param>
        /// <returns>Vrai si c'est une série de même couleur, sinon faux.</returns>
        private static bool EstSerieDeCouleur(List<Tuile> t)
        {
            if (t.Count < 3)
                return false;

            var sortedTuiles = t.OrderBy(t => t.GetNum()).ToList(); // to comment (ajouté lors de detection de serie sans ordre)

            for (int i = 0; i < t.Count - 1; i++)
            {
                int j = i + 1;

                if (
                    !sortedTuiles[i].MemeCouleur(sortedTuiles[j])
                    || !sortedTuiles[i].estSuivant(sortedTuiles[j])
                )
                    return false;
                /*if (!t[i].MemeCouleur(t[j]) || !t[i].estSuivant(t[j]))
                    return false;*/
            }
            return true;
        }

        /// <summary>
        /// Vérifie si une liste de tuiles est une série.
        /// </summary>
        /// <param name="tuiles">Liste de tuiles.</param>
        /// <returns>Vrai si c'est une série, sinon faux.</returns>
        private static bool EstSerie(List<Tuile> tuiles)
        {
            return EstSerieDeMemeChiffre(tuiles) || EstSerieDeCouleur(tuiles);
        }

        /// <summary>
        /// Calcule le nombre d'occurence de la tuile t dans le chevalet.
        /// </summary>
        /// <param name="t">La tuiles.</param>
        /// <returns>Le nombre d'occurence de t dans le chevalet.</returns>
        public int OccurenceTuileInChevalet(Tuile? t)
        {
            int res = 0;
            if (t != null)
            {
                foreach (var tuile in this.chevalet[0])
                {
                    if (tuile != null && t.TuileEquals(tuile))
                    {
                        res++;
                    }
                }
                foreach (var tuile in this.chevalet[1])
                {
                    if (tuile != null && t.TuileEquals(tuile))
                    {
                        res++;
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Partitionne une liste de tuiles en sous-listes séparées par des null.
        /// </summary>
        /// <param name="etage">Liste de tuiles.</param>
        /// <returns>Liste de partitions de tuiles.</returns>
        private static List<List<Tuile>> PartitionListOnNulls(List<Tuile?> etage)
        {
            List<List<Tuile>> partitions = new List<List<Tuile>>();
            List<Tuile> partition = new List<Tuile>();

            foreach (Tuile? t in etage)
            {
                if (t == null)
                {
                    if (partition.Count != 0)
                        partitions.Add(partition);
                    partition = new List<Tuile>();
                }
                else
                {
                    partition.Add(t);
                }
            }
            if (partition.Count != 0)
                partitions.Add(partition);
            return partitions;
        }

        /// <summary>
        /// Vérifie si le chevalet contient uniquement des couples.
        /// </summary>
        /// <returns>Vrai si le chevalet contient uniquement des couples, sinon faux.</returns>
        /*private bool ChevaletHasCouples()
        {
            Tuile t0,
                t1;
            List<List<Tuile>> ParitionEtage1 = PartitionListOnNulls(this.chevalet[0]);
            List<List<Tuile>> ParitionEtage2 = PartitionListOnNulls(this.chevalet[1]);

            // si == 7  -> on a alors que des couples
            if (ParitionEtage1.Count + ParitionEtage2.Count != 7)
                return false;

            foreach (var coupleTuile in ParitionEtage1)
            {
                t0 = coupleTuile[0];
                t1 = coupleTuile[1];
                if (!t0.TuileEquals(t1))
                    return false;
            }

            foreach (var coupleTuile in ParitionEtage1)
            {
                t0 = coupleTuile[0];
                t1 = coupleTuile[1];
                if (!t0.TuileEquals(t1))
                    return false;
            }

            return true;
        }*/
        public bool ChevaletHasCouples()
        {
            foreach (var tuile in this.chevalet[0])
            {
                if (tuile != null)
                {
                    int occ = this.OccurenceTuileInChevalet(tuile);
                    if (occ % 2 != 0)
                        return false;
                }
            }
            foreach (var tuile in this.chevalet[1])
            {
                if (tuile != null)
                {
                    int occ = this.OccurenceTuileInChevalet(tuile);
                    if (occ % 2 != 0)
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Calcule tous les series possible dans le chevalet.
        /// </summary>
        /// <returns>Renvoie une list des serie possible.</returns>
        private static List<List<Tuile>> GetAllPossibleSeries(List<Tuile> tuiles) // ajouter lors serie sans ordre (to comment)
        {
            var possibleSeries = new List<List<Tuile>>();

            // Check for series of the same number with different colors
            var numberGroups = tuiles.GroupBy(t => t.GetNum());
            foreach (var group in numberGroups)
            {
                var sortedGroup = group.OrderBy(t => t.GetCouleur()).ToList();
                if (sortedGroup.Count >= 3 && EstSerieDeMemeChiffre(sortedGroup))
                {
                    possibleSeries.Add(sortedGroup);
                }
            }

            // Check for series of the same color with consecutive numbers
            var colorGroups = tuiles.GroupBy(t => t.GetCouleur());
            foreach (var group in colorGroups)
            {
                var sortedGroup = group.OrderBy(t => t.GetNum()).ToList();
                for (int i = 0; i <= sortedGroup.Count - 3; i++)
                {
                    var series = sortedGroup.Skip(i).Take(3).ToList();
                    if (EstSerieDeCouleur(series))
                    {
                        possibleSeries.Add(series);
                    }
                }
            }

            return possibleSeries;
        }

        /// <summary>
        /// Vérifie si le chevalet contient des séries valides.
        /// </summary>
        /// <returns>Vrai si le chevalet contient des séries valides, sinon faux.</returns>
        /*private bool VerifSerieChevalet()
        {
            if (this.ChevaletHasCouples())
                return true;

            List<List<Tuile>> ParitionEtage1 = PartitionListOnNulls(this.chevalet[0]);
            List<List<Tuile>> ParitionEtage2 = PartitionListOnNulls(this.chevalet[1]);

            foreach (List<Tuile> part in ParitionEtage1)
            {
                if (!EstSerie(part))
                    return false;
            }

            foreach (List<Tuile> part in ParitionEtage2)
            {
                if (!EstSerie(part))
                    return false;
            }

            return true;
        }*/

        public bool VerifSerieChevalet()
        {
            var nonNullTuiles = this
                .chevalet.SelectMany(row => row)
                .Where(t => t != null)
                .Cast<Tuile>()
                .ToList();

            if (nonNullTuiles.Count != 14)
                return false;

            if (this.ChevaletHasCouples())
                return true;

            var possibleSeries = GetAllPossibleSeries(nonNullTuiles);

            foreach (var series in possibleSeries)
            {
                nonNullTuiles = nonNullTuiles.Except(series).ToList();
            }

            return nonNullTuiles.Count == 0;
        }

        /// <summary>
        /// Compte le nombre de tuiles dans le chevalet.
        /// </summary>
        /// <returns>Nombre de tuiles dans le chevalet.</returns>
        public int CountTuileDansChevalet()
        {
            int res = 0;
            for (int j = 0; j < etage; j++)
            {
                for (int i = 0; i < tuilesDansEtage; i++)
                {
                    if (this.chevalet[j][i] != null)
                    {
                        res++;
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Compte le nombre de tuiles dans la défausse.
        /// </summary>
        /// <returns>Nombre de tuiles dans la défausse.</returns>
        public int CountDefausse()
        {
            return this.defausse.Count;
        }

        /// <summary>
        /// Obtient les coordonnées d'une tuile aléatoire dans le chevalet.
        /// </summary>
        /// <returns>Coordonnées d'une tuile aléatoire.</returns>
        public Coord GetRandomTuileCoords()
        {
            int etageRand = -1,
                tuileDansEtageRand = -1;

            do
            {
                etageRand = random.Next(0, etage - 1);
                tuileDansEtageRand = random.Next(0, tuilesDansEtage - 1);
            } while (this.chevalet[etageRand][tuileDansEtageRand] == null);

            return new Coord(etageRand, tuileDansEtageRand);
        }

        /// <summary>
        /// Ajoute une tuile à la défausse.
        /// </summary>
        /// <param name="t">Tuile à ajouter.</param>
        public void JeteTuileDefausse(Tuile? t)
        {
            if (t == null)
                return;
            this.defausse.Push(t);
        }

        /// <summary>
        /// Retire la tuile du sommet de la défausse.
        /// </summary>
        /// <returns>Tuile retirée.</returns>
        public Tuile PopDefausseJoueur()
        {
            return this.defausse.Pop();
        }

        /// <summary>
        /// Indique si la défausse est vide.
        /// </summary>
        /// <returns>Vrai si la défausse est vide, sinon faux.</returns>
        public bool isDefausseEmpty()
        {
            return this.defausse.Count == 0;
        }

        /// <summary>
        /// Indique si le joueur est gagnant.
        /// </summary>
        /// <returns>Vrai si le joueur est gagnant, sinon faux.</returns>
        public bool isGagnant()
        {
            return this.Gagnant;
        }

        /// <summary>
        /// Définit le joueur comme gagnant.
        /// </summary>
        public void SetGagnant()
        {
            this.Gagnant = true;
        }

        /// <summary>
        /// Obtient la pile des tuiles défaussées.
        /// </summary>
        /// <returns>La pile des tuiles défaussées.</returns>
        public Stack<Tuile> GetDefausse()
        {
            return this.defausse;
        }

        /// <summary>
        /// Obtient le score du joueur.
        /// </summary>
        /// <returns>Le score du joueur.</returns>
        public int GetScore()
        {
            return this.score;
        }

        /// <summary>
        /// Obtient le nombre de parties gagnées.
        /// </summary>
        /// <returns>Nombre de parties gagnées.</returns>
        public int GetPartieGagne()
        {
            return this.partieGagne;
        }

        /// <summary>
        /// Définit le nombre de parties gagnées.
        /// </summary>
        /// <param name="g">Nombre de parties gagnées.</param>
        public void SetPartieGagne(int g)
        {
            this.partieGagne = g;
        }

        /// <summary>
        /// Incrémente le nombre de parties gagnées.
        /// </summary>
        public void SetPartieGagneIncrement()
        {
            this.partieGagne++;
        }

        /// <summary>
        /// Obtient le nombre de parties jouées.
        /// </summary>
        /// <returns>Nombre de parties jouées.</returns>
        public int GetPartieJoue()
        {
            return this.partieJoue;
        }

        /// <summary>
        /// Incrémente le nombre de parties jouées.
        /// </summary>
        public void SetPartieJoueIncrement()
        {
            this.partieJoue++;
        }

        /// <summary>
        /// Obtient le taux de victoire du joueur.
        /// </summary>
        /// <returns>Taux de victoire.</returns>
        public double GetWinRate()
        {
            return (this.partieJoue != 0) ? (double)this.partieGagne / this.partieJoue : 0;
        }

        /// <summary>
        /// Obtient la constante K pour le calcul du Elo.
        /// </summary>
        /// <returns>La constante K.</returns>
        public int GetK()
        {
            return this.K;
        }

        /// <summary>
        /// Définit la constante K pour le calcul du Elo.
        /// </summary>
        /// <param name="k">La constante K.</param>
        public void SetK(int k)
        {
            this.K = k;
        }

        /// <summary>
        /// Met à jour la constante K pour le calcul du Elo.
        /// </summary>
        public void UpdateK()
        {
            this.SetK(Elo.ComputeK(this));
        }

        /// <summary>
        /// Affiche les tuiles dans la défausse.
        /// </summary>
        public void AfficheDefausse()
        {
            if (this.isDefausseEmpty())
            {
                Console.WriteLine("La défausse est vide.");
                return;
            }

            Console.WriteLine($"La défausse du {this} contient : ");
            foreach (var elem in this.defausse)
            {
                Console.WriteLine(elem);
            }
        }

        /// <summary>
        /// Affiche les tuiles dans le chevalet.
        /// </summary>
        public void AfficheChevalet()
        {
            Console.WriteLine($"\nLe chevalet de {this} : ");
            for (int i = 0; i < etage; i++)
            {
                for (int j = 0; j < tuilesDansEtage; j++)
                {
                    Tuile? t = this.chevalet[i][j];
                    if (t != null)
                        Console.Write("|" + t);
                    else
                        Console.Write("|(         )");
                }
                Console.Write("|\n");
            }
        }

        /// <summary>
        /// Retourne une chaîne représentant le contenu du chevalet.
        /// </summary>
        /// <returns>Chaîne représentant le contenu du chevalet.</returns>
        public string StringChevalet()
        {
            var buff = "";
            for (int i = 0; i < etage; i++)
            {
                for (int j = 0; j < tuilesDansEtage; j++)
                {
                    Tuile? t = this.chevalet[i][j];
                    if (t != null)
                        buff += "|" + t;
                    else
                        buff += "|(         )";
                }
                buff += "|\n";
            }
            return buff;
        }

        /// <summary>
        /// Obtient le nom du joueur.
        /// </summary>
        /// <returns>Le nom du joueur.</returns>
        public String getName()
        {
            return this.Name;
        }

        /// <summary>
        /// Obtient le chevalet du joueur.
        /// </summary>
        /// <returns>Le chevalet du joueur.</returns>
        public List<List<Tuile?>> GetChevalet()
        {
            return this.chevalet;
        }

        /// <summary>
        /// Obtient la tuile en tête de la défausse.
        /// </summary>
        /// <returns>La tuile en tête de la défausse, ou null si la défausse est vide.</returns>
        public Tuile? GetTeteDefausse()
        {
            if (defausse.Count > 0)
            {
                return defausse.Peek();
            }
            else
            {
                return null;
            }
        }
    }
}

using Okey.Joueurs;
using Okey.Tuiles;

namespace Okey.Game
{
    /// <summary>
    /// La classe Jeu contenant les Joueurs et les Tuiles.
    /// </summary>
    public class Jeu
    {
        private int id;
        private Joueur[] Joueurs = new Joueur[4];
        private Stack<Tuile> pioche = new Stack<Tuile>();
        private bool etat; // false : en jeu
        private Tuile[] Jokers = new Joker[2];
        private Tuile[] Okays = new Okay[2];
        private List<Tuile> PacketTuile = new List<Tuile>();
        private readonly Tuile TuileCentre;
        private Joueur? JoueurActuel;
        private List<Tuile> ListeDefausse;

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="Jeu"/>.
        /// </summary>
        /// <param name="id">L'identifiant unique du jeu.</param>
        /// <param name="joueurs">Tableau des joueurs participant au jeu.</param>
        public Jeu(int id, Joueur[] joueurs)
        {
            this.id = id;
            this.Joueurs = joueurs;
            this.etat = false;
            (this.PacketTuile, this.TuileCentre) = GenererPacketTuiles();
            this.ListeDefausse = new List<Tuile>();
        }

        /// <summary>
        /// Génère le paquet de tuiles et sélectionne la tuile centrale.
        /// </summary>
        /// <returns>Un tuple contenant la liste des tuiles et la tuile centrale.</returns>
        private (List<Tuile>, Tuile) GenererPacketTuiles()
        {
            List<Tuile> tableauTuiles = new List<Tuile>();

            for (int numero = 1; numero <= 13; numero++)
            {
                foreach (CouleurTuile couleur in Enum.GetValues(typeof(CouleurTuile)))
                {
                    if (couleur != CouleurTuile.M)
                    {
                        Tuile tuile = new TuileNormale(couleur, numero, true);
                        tableauTuiles.Add(tuile);

                        Tuile tuile2 = new TuileNormale(couleur, numero, true);
                        tableauTuiles.Add(tuile2);
                    }
                }
            }

            Random random = new Random();
            int randIndex = random.Next(0, 103);
            Tuile tuileCentre = tableauTuiles[randIndex];

            int numOkey = (tuileCentre.GetNum() == 13) ? 1 : tuileCentre.GetNum() + 1;
            CouleurTuile couleurOkey = tuileCentre.GetCouleur();

            int ok = 0;
            for (int i = 0; i < 103; i++)
            {
                if (
                    tableauTuiles[i].GetNum() == numOkey
                    && tableauTuiles[i].GetCouleur() == couleurOkey
                )
                {
                    Okay okay = new Okay(true);
                    tableauTuiles[i] = okay;
                    this.Okays[ok] = okay;
                    ok++;
                }
            }

            Tuile joker1 = new Joker(couleurOkey, numOkey, true);
            tableauTuiles[randIndex] = joker1;
            this.Jokers[0] = joker1;

            Tuile joker2 = new Joker(couleurOkey, numOkey, true);
            tableauTuiles.Add(joker2);
            this.Jokers[1] = joker2;

            return (tableauTuiles, tuileCentre);
        }

        /// <summary>
        /// Distribue les tuiles aux joueurs.
        /// </summary>
        public void DistibuerTuile()
        {
            for (int i = 0; i < 14; i++)
            {
                foreach (Joueur pl in this.Joueurs)
                {
                    Random random = new Random();
                    int randIndex = random.Next(0, this.PacketTuile.Count - 1);

                    Tuile toGive = this.PacketTuile[randIndex];

                    this.PacketTuile.RemoveAt(randIndex);
                    pl.AjoutTuileChevalet(toGive);
                }
            }

            Random randomm = new Random();
            int randT = randomm.Next(0, this.PacketTuile.Count - 1);
            Tuile LastTuileTogive = this.PacketTuile[randT];
            this.PacketTuile.RemoveAt(randT);

            this.Joueurs[randT % 4].AjoutTuileChevalet(LastTuileTogive);

            foreach (Tuile tuile in this.PacketTuile)
            {
                this.pioche.Push(tuile);
            }

            ShuffleStack(this.pioche);

            this.setJoueurActuel(this.Joueurs[randT % 4]);
        }

        /// <summary>
        /// Affiche les tuiles sur les chevalets des joueurs.
        /// </summary>
        public void AfficheChevaletJoueurs()
        {
            foreach (Joueur pl in this.Joueurs)
            {
                pl.AfficheChevalet();
                Console.WriteLine("");
            }
        }

        /// <summary>
        /// Affiche les tuiles sur le chevalet du joueur actuel.
        /// </summary>
        public void AfficheChevaletActuel()
        {
            if (JoueurActuel != null)
                this.JoueurActuel.AfficheChevalet();
        }

        /// <summary>
        /// Obtient la représentation en chaîne du chevalet du joueur actuel.
        /// </summary>
        /// <returns>Une chaîne représentant le chevalet du joueur actuel.</returns>
        public string StringChevaletActuel()
        {
            if (this.JoueurActuel == null)
                return "";
            return this.JoueurActuel.StringChevalet();
        }

        /// <summary>
        /// Affiche les tuiles dans la pioche centrale.
        /// </summary>
        public void AffichePiocheCentre()
        {
            if (this.isPiocheCentreEmpty())
            {
                Console.WriteLine("La pioche du centre est vide.");
                return;
            }

            Console.WriteLine("La pioche du centre contient : ");
            foreach (var elem in this.pioche)
            {
                Console.WriteLine(elem);
            }
        }

        /// <summary>
        /// Obtient la liste des tuiles.
        /// </summary>
        /// <returns>La liste des tuiles.</returns>
        public List<Tuile> GetPacketTuile()
        {
            return this.PacketTuile;
        }

        /// <summary>
        /// Obtient la tuile centrale.
        /// </summary>
        /// <returns>La tuile centrale.</returns>
        public Tuile GetTuileCentre()
        {
            return this.TuileCentre;
        }

        /// <summary>
        /// Obtient les jokers.
        /// </summary>
        /// <returns>Un tableau de tuiles joker.</returns>
        public Tuile[] GetJokers()
        {
            return this.Jokers;
        }

        /// <summary>
        /// Obtient les Okays.
        /// </summary>
        /// <returns>Un tableau de tuiles Okay.</returns>
        public Tuile[] GetOkays()
        {
            return this.Okays;
        }

        /// <summary>
        /// Obtient les joueurs.
        /// </summary>
        /// <returns>Un tableau des joueurs.</returns>
        public Joueur[] GetJoueurs()
        {
            return Joueurs;
        }

        /// <summary>
        /// Obtient le joueur actuel.
        /// </summary>
        /// <returns>Le joueur actuel, ou null s'il n'y en a pas.</returns>
        public Joueur? getJoueurActuel()
        {
            return this.JoueurActuel;
        }

        /// <summary>
        /// Vérifie si le jeu est terminé.
        /// </summary>
        /// <returns>True si le jeu est terminé, sinon false.</returns>
        public bool isTermine()
        {
            return this.etat;
        }

        /// <summary>
        /// Obtient le joueur suivant.
        /// </summary>
        /// <param name="j">Le joueur actuel.</param>
        /// <returns>Le joueur suivant.</returns>
        public Joueur getNextJoueur(Joueur j)
        {
            int indexOfNextPlayer = (Array.IndexOf(this.Joueurs, j) + 1) % 4;
            return this.Joueurs[indexOfNextPlayer];
        }

        /// <summary>
        /// Obtient le joueur précédent.
        /// </summary>
        /// <param name="j">Le joueur actuel.</param>
        /// <returns>Le joueur précédent.</returns>
        public Joueur getPreviousPlayer(Joueur j)
        {
            int indexOfj = Array.IndexOf(this.Joueurs, j);
            int indexOfPreviousPlayer = indexOfj == 0 ? 3 : indexOfj - 1;
            return this.Joueurs[indexOfPreviousPlayer];
        }

        /// <summary>
        /// Vérifie si la pioche centrale est vide.
        /// </summary>
        /// <returns>True si la pioche centrale est vide, sinon false.</returns>
        public bool isPiocheCentreEmpty()
        {
            return this.pioche.Count == 0;
        }

        /// <summary>
        /// Obtient la taille de la pioche.
        /// </summary>
        /// <returns>La taille de la pioche.</returns>
        public int GetPiocheTaille()
        {
            return this.pioche.Count;
        }

        /// <summary>
        /// Retire et renvoie la tuile en haut de la pioche centrale.
        /// </summary>
        /// <returns>La tuile en haut de la pioche centrale, ou null si elle est vide.</returns>
        public Tuile? PopPiocheCentre()
        {
            if (this.isPiocheCentreEmpty())
            {
                return null;
            }
            return this.pioche.Pop();
        }

        /// <summary>
        /// Obtient la tuile en haut de la pioche centrale sans la retirer.
        /// </summary>
        /// <returns>La tuile en haut de la pioche centrale, ou null si elle est vide.</returns>
        public Tuile? GetPiocheHead()
        {
            if (this.isPiocheCentreEmpty())
            {
                return null;
            }
            return this.pioche.Peek();
        }

        /// <summary>
        /// Obtient la liste des tuiles défaussées.
        /// </summary>
        /// <returns>La liste des tuiles défaussées.</returns>
        public List<Tuile> GetListeDefausse()
        {
            return this.ListeDefausse;
        }

        /// <summary>
        /// Ajoute une tuile à la liste des tuiles défaussées.
        /// </summary>
        /// <param name="t">La tuile à ajouter à la défausse.</param>
        public void AddToListeDefausse(Tuile t)
        {
            this.ListeDefausse.Add(t);
        }

        /// <summary>
        /// Ajoute une tuile à la pioche centrale.
        /// </summary>
        /// <param name="t">La tuile à ajouter.</param>
        public void PushPiocheCentre(Tuile? t)
        {
            if (t == null)
                return;
            this.pioche.Push(t);
        }

        /// <summary>
        /// Définit le joueur qui a son tour de joueur.
        /// </summary>
        /// <param name="j">Le joueur à jouer.</param>
        public void setJoueurActuel(Joueur j)
        {
            this.JoueurActuel = j;
            j.Ajouer();
            j.Apiocher();
        }

        /// <summary>
        /// Marque le jeu comme terminé sans gagnant.
        /// </summary>
        public void JeuTermine()
        {
            this.etat = true;
        }

        /// <summary>
        /// Marque le jeu comme terminé avec un gagnant.
        /// </summary>
        /// <param name="pl">Le joueur gagnant.</param>
        public void JeuTermine(Joueur pl)
        {
            this.etat = true;
            pl.SetGagnant();
            this.updateJoueursElo();
            pl.SetPartieGagneIncrement();
            this.PlayersPartieJouerIncrement();
            this.UpdateAllPlayersK();
        }

        /// <summary>
        /// Met à jour l'Elo des joueurs.
        /// </summary>
        public void updateJoueursElo()
        {
            foreach (var pl in this.Joueurs)
            {
                pl.Gagne(this);
            }
        }

        /// <summary>
        /// Met à jour le coefficient K de tous les joueurs.
        /// </summary>
        public void UpdateAllPlayersK()
        {
            foreach (var pl in this.Joueurs)
            {
                pl.UpdateK();
            }
        }

        /// <summary>
        /// Incrémente le nombre de parties jouées par les joueurs.
        /// </summary>
        public void PlayersPartieJouerIncrement()
        {
            foreach (var pl in this.Joueurs)
            {
                pl.SetPartieJoueIncrement();
            }
        }

        /// <summary>
        /// Mélange les éléments de la pile.
        /// </summary>
        /// <param name="stack">La pile à mélanger.</param>
        static void ShuffleStack(Stack<Tuile> stack)
        {
            List<Tuile> list = stack.ToList();
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                Tuile value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            stack.Clear();
            foreach (Tuile item in list)
            {
                stack.Push(item);
            }
        }
    }
}

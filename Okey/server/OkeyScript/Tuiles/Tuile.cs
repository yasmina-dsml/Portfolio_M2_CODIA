using System.Text.Json;
using Okey.Joueurs;

namespace Okey.Tuiles
{
    /// <summary>
    /// Enumération des couleurs de tuiles.
    /// </summary>
    public enum CouleurTuile
    {
        /// <summary>
        /// Jaune.
        /// </summary>
        J,

        /// <summary>
        /// Noir.
        /// </summary>
        N,

        /// <summary>
        /// Rouge.
        /// </summary>
        R,

        /// <summary>
        /// Bleu.
        /// </summary>
        B,

        /// <summary>
        /// Multicolore.
        /// </summary>
        M
    }

    /// <summary>
    /// Classe abstraite représentant une tuile dans le jeu.
    /// </summary>
    public abstract class Tuile
    {
        /// <summary>
        /// La couleur de la tuile.
        /// </summary>
        private CouleurTuile couleur;

        /// <summary>
        /// Le numéro de la tuile.
        /// </summary>
        private int num;

        /// <summary>
        /// Indique si la tuile est dans la défausse.
        /// </summary>
        private bool defausse;

        /// <summary>
        /// Indique si la tuile est dans la pioche.
        /// </summary>
        private bool dansPioche;

        /// <summary>
        /// Initialise une nouvelle instance de la classe Tuile.
        /// </summary>
        /// <param name="couleur">Couleur de la tuile.</param>
        /// <param name="num">Numéro de la tuile.</param>
        /// <param name="dansPioche">Indique si la tuile est dans la pioche.</param>
        public Tuile(CouleurTuile couleur, int num, bool dansPioche)
        {
            this.couleur = couleur;
            this.num = num;
            this.defausse = false;
            this.dansPioche = dansPioche;
        }

        /// <summary>
        /// Obtient ou définit la couleur de la tuile.
        /// </summary>
        protected CouleurTuile Couleur
        {
            get => couleur;
            set => couleur = value;
        }

        /// <summary>
        /// Obtient ou définit le numéro de la tuile.
        /// </summary>
        protected int Num
        {
            get => num;
            set => num = value;
        }

        /// <summary>
        /// Obtient ou définit si la tuile est dans la défausse.
        /// </summary>
        protected bool Defausse
        {
            get => defausse;
            set => defausse = value;
        }

        /// <summary>
        /// Obtient ou définit si la tuile est dans la pioche.
        /// </summary>
        protected bool DansPioche
        {
            get => dansPioche;
            set => dansPioche = value;
        }

        /// <summary>
        /// Définit la tuile comme étant dans la défausse.
        /// </summary>
        public void SetDefause()
        {
            if (this == null)
                return;
            this.defausse = true;
        }

        /// <summary>
        /// Obtient si la tuile est dans la pioche.
        /// </summary>
        /// <returns>Vrai si la tuile est dans la pioche.</returns>
        public bool GetPioche() => this.dansPioche;

        /// <summary>
        /// Indique si la tuile est dans la défausse.
        /// </summary>
        /// <returns>Vrai si la tuile est dans la défausse.</returns>
        public Boolean isDefausse()
        {
            return this.defausse;
        }

        /// <summary>
        /// Indique si la tuile est dans la pioche.
        /// </summary>
        /// <returns>Vrai si la tuile est dans la pioche.</returns>
        public Boolean inPioche()
        {
            return this.dansPioche;
        }

        /// <summary>
        /// Obtient la couleur de la tuile.
        /// </summary>
        /// <returns>Couleur de la tuile.</returns>
        public CouleurTuile GetCouleur()
        {
            return this.couleur;
        }

        /// <summary>
        /// Obtient le numéro de la tuile.
        /// </summary>
        /// <returns>Numéro de la tuile.</returns>
        public int GetNum()
        {
            return this.num;
        }

        /// <summary>
        /// Vérifie si une tuile est égale à une autre tuile.
        /// </summary>
        /// <param name="t">Tuile à comparer.</param>
        /// <returns>Vrai si les tuiles sont égales.</returns>
        public abstract bool TuileEquals(Tuile t);

        /// <summary>
        /// Vérifie si une tuile a la même couleur qu'une autre tuile.
        /// </summary>
        /// <param name="t">Tuile à comparer.</param>
        /// <returns>Vrai si les tuiles ont la même couleur.</returns>
        public abstract bool MemeCouleur(Tuile t);

        /// <summary>
        /// Vérifie si une tuile est la suivante d'une autre tuile.
        /// </summary>
        /// <param name="t">Tuile à comparer.</param>
        /// <returns>Vrai si les tuiles se suivent.</returns>
        public abstract bool estSuivant(Tuile t);

        /// <summary>
        /// Retourne une chaîne qui représente la tuile.
        /// </summary>
        /// <returns>Chaîne représentant la tuile.</returns>
        public abstract override string ToString();

        /// <summary>
        /// Retourne le nom de la tuile.
        /// </summary>
        /// <returns>Le nom de la tuile.</returns>
        public abstract string GetName();
    }
}

namespace Okey.Tuiles
{
    using System.Text.Json;

    /// <summary>
    /// Classe représentant une tuile normale dans le jeu.
    /// </summary>
    public class TuileNormale : Tuile
    {
        /// <summary>
        /// Initialise une nouvelle instance de la classe TuileNormale.
        /// </summary>
        /// <param name="couleur">Couleur de la tuile.</param>
        /// <param name="num">Numéro de la tuile.</param>
        /// <param name="dansPioche">Indique si la tuile est dans la pioche.</param>
        public TuileNormale(CouleurTuile couleur, int num, bool dansPioche)
            : base(couleur, num, dansPioche) { }

        /// <summary>
        /// Vérifie si la tuile a la même couleur qu'une autre tuile.
        /// </summary>
        /// <param name="t">Tuile à comparer.</param>
        /// <returns>Vrai si les tuiles ont la même couleur.</returns>
        public override bool MemeCouleur(Tuile t)
        {
            return this.Couleur == t.GetCouleur();
        }

        /// <summary>
        /// Vérifie si la tuile est la suivante d'une autre tuile.
        /// </summary>
        /// <param name="t">Tuile à comparer.</param>
        /// <returns>Vrai si la tuile est la suivante.</returns>
        public override bool estSuivant(Tuile t)
        {
            return this.Num + 1 == t.GetNum() || (this.Num == 13 && t.GetNum() == 1);
        }

        /// <summary>
        /// Retourne une chaîne qui représente la tuile normale.
        /// </summary>
        /// <returns>Chaîne représentant la tuile normale.</returns>
        public override string ToString()
        {
            return String.Format(null, "({0:00}, {1}, {2})", this.Num, this.Couleur, "No");
        }

        /// <summary>
        /// Vérifie si une tuile est égale à la tuile normale.
        /// </summary>
        /// <param name="t">Tuile à comparer.</param>
        /// <returns>Vrai si les tuiles sont égales.</returns>
        public override bool TuileEquals(Tuile t)
        {
            if (t is Okay)
                return true;
            return this.Num == t.GetNum() && this.Couleur == t.GetCouleur();
        }

        /// <summary>
        /// Retourne le nom de la tuile.
        /// </summary>
        /// <returns>Le nom "No".</returns>
        public override string GetName() => "No";
    }
}

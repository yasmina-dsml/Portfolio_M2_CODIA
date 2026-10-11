namespace Okey.Tuiles
{
    /// <summary>
    /// Classe représentant une tuile Okay dans le jeu.
    /// </summary>
    public class Okay : Tuile
    {
        /// <summary>
        /// Initialise une nouvelle instance de la classe Okay.
        /// </summary>
        /// <param name="dansPioche">Indique si la tuile est dans la pioche.</param>
        public Okay(bool dansPioche)
            : base(CouleurTuile.M, 0, dansPioche)
        {
            // Calculer la valeur
        }

        /// <summary>
        /// Vérifie si la tuile a la même couleur qu'une autre tuile.
        /// </summary>
        /// <param name="t">Tuile à comparer.</param>
        /// <returns>Retourne toujours vrai pour Okay.</returns>
        public override bool MemeCouleur(Tuile t)
        {
            return true;
        }

        /// <summary>
        /// Vérifie si la tuile est la suivante d'une autre tuile.
        /// </summary>
        /// <param name="t">Tuile à comparer.</param>
        /// <returns>Retourne toujours vrai pour Okay.</returns>
        public override bool estSuivant(Tuile t)
        {
            return true;
        }

        /// <summary>
        /// Retourne une chaîne qui représente la tuile Okay.
        /// </summary>
        /// <returns>Chaîne représentant la tuile Okay.</returns>
        public override String ToString()
        {
            return String.Format(null, "({0:00}, {1}, {2})", this.Num, this.Couleur, "Ok");
        }

        /// <summary>
        /// Vérifie si une tuile est égale à la tuile Okay.
        /// </summary>
        /// <param name="t">Tuile à comparer.</param>
        /// <returns>Retourne toujours vrai pour Okay.</returns>
        public override bool TuileEquals(Tuile t)
        {
            return true;
        }

        /// <summary>
        /// Retourne le nom de la tuile.
        /// </summary>
        /// <returns>Le nom "Ok".</returns>
        public override string GetName() => "Ok";
    }
}

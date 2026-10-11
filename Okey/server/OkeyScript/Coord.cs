namespace Okey
{
    /// <summary>
    /// Classe représentant des coordonnées dans le jeu (pour les tuiles).
    /// </summary>
    public class Coord
    {
        /// <summary>
        /// Coordonnée x.
        /// </summary>
        private int x;

        /// <summary>
        /// Coordonnée y.
        /// </summary>
        private int y;

        /// <summary>
        /// Initialise une nouvelle instance de la classe Coord.
        /// </summary>
        /// <param name="y">Coordonnée y.</param>
        /// <param name="x">Coordonnée x.</param>
        public Coord(int y, int x)
        {
            this.y = y;
            this.x = x;
        }

        /// <summary>
        /// Obtient la coordonnée x.
        /// </summary>
        /// <returns>La coordonnée x.</returns>
        public int getX()
        {
            return x;
        }

        /// <summary>
        /// Obtient la coordonnée y.
        /// </summary>
        /// <returns>La coordonnée y.</returns>
        public int getY()
        {
            return y;
        }

        /// <summary>
        /// Retourne une chaîne représentant les coordonnées.
        /// </summary>
        /// <returns>Chaîne représentant les coordonnées.</returns>
        public override string ToString()
        {
            return $"({this.y}, {this.x})";
        }
    }
}

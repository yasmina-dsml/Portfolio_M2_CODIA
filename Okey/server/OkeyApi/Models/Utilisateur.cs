using Microsoft.AspNetCore.Identity;

namespace OkeyApi.Models
{
    /// <summary>
    /// Classe modèle utilisateur
    /// </summary>
    public class Utilisateur : IdentityUser
    {
        /// <summary>
        /// Photo de profil
        /// </summary>
        public int Photo { get; set; } = 1;

        /// <summary>
        /// Points d'experience utilisateur
        /// </summary>
        public int Experience { get; set; } = 1;

        /// <summary>
        /// Date d'inscription du joueur
        /// </summary>
        public DateTime DateInscription { get; set; } = DateTime.Now;

        /// <summary>
        /// Permet de représenter l'Elo Score du joueur
        /// </summary>
        public int Elo { get; set; } = 400;

        /// <summary>
        /// Nombres de parties effectuees par le joueur
        /// </summary>
        public int NombreParties { get; set; }

        /// <summary>
        /// Nombres de parties gagné par le joueur
        /// </summary>
        public int NombrePartiesGagnees { get; set; }
    }
}

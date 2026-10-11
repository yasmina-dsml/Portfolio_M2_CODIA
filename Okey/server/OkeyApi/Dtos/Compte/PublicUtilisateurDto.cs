namespace OkeyApi.Dtos.Compte;

/// <summary>
/// Dto pour un utilisateur non connecté/public
/// </summary>
public class PublicUtilisateurDto
{
    /// <summary>
    /// Nom d'utilisateur
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Photo de profil
    /// </summary>
    public int? Photo { get; set; }

    /// <summary>
    /// Points d'experience
    /// </summary>
    public int? Experience { get; set; }

    /// <summary>
    /// Elo Score
    /// </summary>
    public int Elo { get; set; }

    /// <summary>
    /// Date d'inscription
    /// </summary>
    public DateTime DateInscription { get; set; }

    /// <summary>
    /// Nombres de parties effectuees par le joueur
    /// </summary>
    public int NombreParties { get; set; } // = 0; to rm warnings (integers are 0 by default)

    /// <summary>
    /// Nombres de parties gagné par le joueur
    /// </summary>
    public int NombrePartiesGagnees { get; set; }
}

namespace OkeyApi.Dtos.Compte;

/// <summary>
/// Dto pour un utilisateur connecté pouvant accéder a ses données
/// </summary>
public class PrivateUtilisateurDto
{
    /// <summary>
    /// Nom d'utilisateur
    /// </summary>
    public string? Username { get; set; } = string.Empty;

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
    /// Liste des achievements de l'utilisateur
    /// </summary>
    public List<bool> Achievements { get; set; } = new List<bool>();

    /// <summary>
    /// Nombres de parties effectuees par le joueur
    /// </summary>
    public int NombreParties { get; set; }

    /// <summary>
    /// Nombres de parties gagné par le joueur
    /// </summary>
    public int NombrePartiesGagnees { get; set; }
}

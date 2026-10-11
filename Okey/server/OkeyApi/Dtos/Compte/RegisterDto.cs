namespace OkeyApi.Dtos.Compte;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Dto pour l'inscription utilisateur
/// </summary>
public class RegisterDto
{
    /// <summary>
    /// Nom d'utilisateur
    /// </summary>
    [Required]
    public string? Username { get; set; }

    /// <summary>
    /// Photo de profil
    /// </summary>
    [Required]
    public int Photo { get; set; }

    /// <summary>
    /// Mot de passe
    /// </summary>
    [Required]
    public string? Password { get; set; }
}

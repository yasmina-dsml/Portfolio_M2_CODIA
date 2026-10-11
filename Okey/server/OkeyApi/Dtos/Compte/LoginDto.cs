namespace OkeyApi.Dtos.Compte;

/// <summary>
/// Dto pour le Login utilisateur
/// </summary>
public class LoginDto
{
    /// <summary>
    /// Attribut pour le nom d'utilisateur
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Attribut pour le mot de passe
    /// </summary>
    public string? Password { get; set; }
}

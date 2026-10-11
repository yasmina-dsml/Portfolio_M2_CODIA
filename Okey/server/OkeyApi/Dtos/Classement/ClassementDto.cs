namespace OkeyApi.Dtos.Classement;

/// <summary>
/// Dto du modèle Classement
/// </summary>
public class ClassementDto
{
    /// <summary>
    /// Attribut repreésentant le Nom d'utilisateur
    /// </summary>
    public string? Username { get; set; } = string.Empty;

    /// <summary>
    /// Attribut représentant le classement du joueur
    /// </summary>
    public int Classement { set; get; }

    /// <summary>
    /// Attribut représentant l'Elo Score du joueur
    /// </summary>
    public int Elo { get; set; }
}

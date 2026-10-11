namespace OkeyApi.Dtos.Achievements;

using Models;

/// <summary>
/// Dto du modèle Achievement
/// </summary>
public class AchievementDto
{
    /// <summary>
    /// Id de l'Achievement
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Attribut qui repésente l'achievement "Jouer 5 parties"
    /// </summary>
    public bool Jouer5Parties { get; set; }

    /// <summary>
    /// Attribut qui repreésente l'achievement "Gagner une fois"
    /// </summary>
    public bool GagnerUneFois { get; set; }

    /// <summary>
    /// Attribut permettant de stocker le userId en foreign key dans la base de donnée
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Attribut repreésentant le joueur associé (propre au framework)
    /// </summary>
    public Utilisateur? Utilisateur { get; set; }
}

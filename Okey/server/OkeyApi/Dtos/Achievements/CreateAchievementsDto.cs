namespace OkeyApi.Dtos.Achievements;

using Models;

/// <summary>
/// Dto propre à la l'insertion d'un achievement en Base de donneée
/// </summary>
public class CreateAchievementsDto
{
    /// <summary>
    /// Id de l'achievement
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// UserId associé aux achievements
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Attribut propre au framework
    /// </summary>
    public Utilisateur? Utilisateur { get; set; }
}

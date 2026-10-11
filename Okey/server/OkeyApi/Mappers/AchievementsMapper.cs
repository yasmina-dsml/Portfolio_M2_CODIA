namespace OkeyApi.Mappers;

using Dtos.Achievements;
using Models;

/// <summary>
/// Classe Mapper pour faire passer un Dto de Achievement vers un Modèle Achievement
/// </summary>
public static class AchievementsMapper
{
    /// <summary>
    /// Faire passer un modèle Achievement à un Dto Achievement
    /// </summary>
    /// <param name="achievementsModel">Modèle Achievement, remplis ou non.</param>
    /// <returns>Le Dto attendu (AchievementDto)</returns>
    public static AchievementDto ToAchievementDto(this Achievements achievementsModel)
    {
        return new AchievementDto
        {
            Id = achievementsModel.Id,
            Jouer5Parties = achievementsModel.Jouer5Parties,
            GagnerUneFois = achievementsModel.GagnerUneFois,
            UserId = achievementsModel.UserId,
            Utilisateur = achievementsModel.Utilisateur
        };
    }

    /// <summary>
    /// Faire passer un CreateAchievementDto pour une insertion en Base de Donnée
    /// </summary>
    /// <param name="createAchievementsDto">Dto de création d'achievement</param>
    /// <param name="utilisateur">Utilisateur associé</param>
    /// <returns>Modele Achievement complet prêt pour l'insertion.</returns>
    public static Achievements ToAchievementFromCreate(
        this CreateAchievementsDto createAchievementsDto,
        Utilisateur utilisateur
    )
    {
        return new Achievements
        {
            Id = createAchievementsDto.Id,
            UserId = createAchievementsDto.UserId,
            Utilisateur = utilisateur
        };
    }
}

namespace OkeyApi.Repository;

using Data;
using Interfaces;
using Models;

/// <summary>
/// Classe Repository des Achievements, permet l'implémentation d'écriture en Base de Donnée
/// </summary>
public class AchievementsRepository : IAchievementsRepository
{
    /// <summary>
    /// Contexte de la base de donnée
    /// </summary>
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Constructeur de la classe
    /// </summary>
    /// <param name="context">Contexte Base de Donnée</param>
    public AchievementsRepository(ApplicationDbContext context)
    {
        this._context = context;
    }

    /// <summary>
    /// Création asynchrone d'une ligne dans la table Achievement en Base de donnée
    /// </summary>
    /// <param name="achievements">Modèle de l'Achievement à insérer</param>
    /// <returns>Contrat permettant la validation des actions à faire, propre au système</returns>
    public async Task<Achievements?> CreateAsync(Achievements achievements)
    {
        var achievementsEnumerable = this._context.Achievements;
        if (achievementsEnumerable != null)
        {
            await achievementsEnumerable.AddAsync(achievements);
        }

        await this._context.SaveChangesAsync();
        return achievements;
    }
}

namespace OkeyApi.Interfaces;

using Models;

/// <summary>
/// Interface permettant la conception de méthodes propres aux achievements en Base de Donneées
/// </summary>
public interface IAchievementsRepository
{
    /// <summary>
    /// Méthode permettant la création asynchrone d'un achievement en Base de Donnée
    /// </summary>
    /// <param name="achievements">Achievement complet permettant la complétion en Base de Donnée</param>
    /// <returns>Contrat permettant de notifier le systeme de l'execution de la fonction</returns>
    Task<Achievements?> CreateAsync(Achievements achievements);
}

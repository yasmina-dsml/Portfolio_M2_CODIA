namespace OkeyApi.Interfaces;

using Models;

/// <summary>
/// Interface permettant la conception de méthodes propres aux Utilisateurs en Base de Donneées
/// </summary>
public interface IUtilisateurRepository
{
    /// <summary>
    /// Methode asynchrone qui permet la récupération de tout les utilisateurs
    /// </summary>
    /// <returns>Contrat permettant de notifier le systeme de l'execution de la fonction</returns>
    Task<List<Utilisateur>> GetAllAsync();

    /// <summary>
    /// Methode asynchrone qui permet la récupération d'un utilisateur
    /// </summary>
    /// <param name="username">Nom d'utilisateur de l'utilisateur intéressé</param>
    /// <returns>Contrat permettant de notifier le systeme de l'execution de la fonction</returns>
    Task<Utilisateur?> GetByUsername(string username);

    Task UpdatePhotoAsync(string username, int photo);
    Task UpdateUsernameAsync(string username, string? new_username);
    Task UpdatePasswordAsync(string username, string old_password, string new_password);
}

namespace OkeyApi.Interfaces;

using OkeyApi.Models;

/// <summary>
/// Interface permettant la conception de méthodes propres aux token
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Méthode permettant la création asynchrone d'un token
    /// </summary>
    /// <param name="utilisateur">Modèle qui va etre inscrit en Base de Donnée</param>
    /// <returns>Token sous forme de chaîne de caractères</returns>
    public string CreateToken(Utilisateur utilisateur);
}

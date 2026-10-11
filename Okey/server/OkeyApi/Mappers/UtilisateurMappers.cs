namespace OkeyApi.Mappers;

using Dtos.Compte;
using Models;

/// <summary>
/// Classe Mapper pour faire passer un Dto de Utilisateur vers un Modèle Utilisateur
/// </summary>
public static class UtilisateurMappers
{
    /// <summary>
    /// Methode qui rend un permet d'obtenir les données à afficher publiquement
    /// </summary>
    /// <param name="userModel">Modèle utilisateur utilisé pour l'implémentation des méthodes</param>
    /// <returns>Dto d'un profil public</returns>
    public static PublicUtilisateurDto ToPublicUtilisateurDto(this Utilisateur userModel) =>
        new()
        {
            Username = userModel.UserName,
            Photo = userModel.Photo,
            Experience = userModel.Experience,
            Elo = userModel.Elo,
            DateInscription = userModel.DateInscription,
            NombreParties = userModel.NombreParties,
            NombrePartiesGagnees = userModel.NombrePartiesGagnees
        };

    /// <summary>
    /// Methode qui rend un permet d'obtenir les données privée à afficher
    /// </summary>
    /// <param name="userModel">Modèle utilisateur utilisé pour l'implémentation des méthodes</param>
    /// <returns>Dto d'un profil privé</returns>
    public static PrivateUtilisateurDto ToPrivateUtilisateurDto(this Utilisateur userModel) =>
        new()
        {
            Username = userModel.UserName,
            Photo = userModel.Photo,
            Experience = userModel.Experience,
            Elo = userModel.Elo,
            DateInscription = userModel.DateInscription,
            Achievements = new List<bool>(),
            NombreParties = userModel.NombreParties,
            NombrePartiesGagnees = userModel.NombrePartiesGagnees
        };
}

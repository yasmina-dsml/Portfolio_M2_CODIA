namespace OkeyApi.Repository;

using Data;
using Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models;

/// <summary>
/// Classe Repository des Utilisateurs, permet l'implémentation d'écriture en Base de Donnée
/// </summary>
public class UtilisateurRepository : IUtilisateurRepository
{
    /// <summary>
    /// Manager de l'utilisateur, utilisé pour la gestion de mot de passe
    /// </summary>
    private readonly UserManager<Utilisateur> _userManager;

    /// <summary>
    /// Contexte de la base de donnée
    /// </summary>
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Constructeur de la classe
    /// </summary>
    /// <param name="context">Contexte Base de Donnée</param>
    public UtilisateurRepository(ApplicationDbContext context, UserManager<Utilisateur> userManager)
    {
        this._context = context;
        this._userManager = userManager;
    }

    /// <summary>
    /// Récupération asynchrone de tout les utilisateurs en Base de Donnée
    /// </summary>
    /// <returns>Contrat permettant la validation des actions à faire, propre au système</returns>
    public async Task<List<Utilisateur>> GetAllAsync() => await this._context.Users.ToListAsync();

    /// <summary>
    /// Récupération asynchrone d'utilisateur en Base de Donnée
    /// </summary>
    /// <param name="username">Identitfiant de l'utilisateur à chercher</param>
    /// <returns>Contrat permettant la validation des actions à faire, propre au système</returns>
    public async Task<Utilisateur?> GetByUsername(string username) =>
        await this._context.Users.FirstOrDefaultAsync(s =>
            s.UserName != null && s.UserName.Equals(username, StringComparison.Ordinal)
        );

    public async Task UpdatePhotoAsync(string username, int photo)
    {
        var user = await this._context.Users.FirstOrDefaultAsync(s =>
            s.UserName != null && s.UserName.Equals(username, StringComparison.Ordinal)
        );

        if (user != null)
        {
            try
            {
                user.Photo = photo;

                await this._context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Echec de changement de la photo de '{username}': {ex.Message}");
                throw;
            }
        }
    }

    public async Task UpdateUsernameAsync(string username, string? new_username)
    {
        if (string.IsNullOrEmpty(new_username))
        {
            throw new ArgumentException(
                "Le nom d'utilisateur ne peut pas etre null ou vide.",
                nameof(new_username)
            );
        }

        var user = await this._context.Users.FirstOrDefaultAsync(s =>
            s.UserName != null && s.UserName.Equals(username, StringComparison.Ordinal)
        );

        if (user != null)
        {
            try
            {
                await this._userManager.SetUserNameAsync(user, new_username);

                await this._context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine(
                    $"Echec de changement du nom d'utilisateur de '{username}': {ex.Message}"
                );
                throw;
            }
        }
    }

    public async Task UpdatePasswordAsync(string username, string old_password, string new_password)
    {
        var user = await this._context.Users.FirstOrDefaultAsync(s =>
            s.UserName != null && s.UserName.Equals(username, StringComparison.Ordinal)
        );
        if (user != null)
        {
            await this._userManager.ChangePasswordAsync(user, old_password, new_password);
            await this._context.SaveChangesAsync();
        }
    }
}

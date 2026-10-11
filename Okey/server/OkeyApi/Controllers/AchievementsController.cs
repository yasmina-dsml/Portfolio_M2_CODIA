namespace OkeyApi.Controllers;

using Data;
using Interfaces;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Classe Controller de la table Achievements
/// Aucune route mise en place, set-up en cas d'ajout de end-points
/// </summary>
[Route("okeyapi/achievements")]
[ApiController]
public class AchievementsController : ControllerBase
{
    private readonly IAchievementsRepository _achievementsRepo;
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Constructeur de la classe AchievementsController
    /// </summary>
    /// <param name="context">Issu de notre API permet l'écriture/lecture en Base de Donnée</param>
    /// <param name="achievementsRepo">Issu de notre API fournissant des fonctions de recherche des achievements efficaces.</param>
    public AchievementsController(
        ApplicationDbContext context,
        IAchievementsRepository achievementsRepo
    )
    {
        this._context = context;
        this._achievementsRepo = achievementsRepo;
    }
}

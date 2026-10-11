namespace OkeyApi.Controllers;

using Dtos.Classement;
using Interfaces;
using Microsoft.AspNetCore.Mvc;

///<summary>
/// Classe Controller de la gestion du classement utilisateur
/// Contient toute la logique des end-points de la forme: /classement/*
/// </summary>

[Route("okeyapi/classement")]
[ApiController]
public class ClassementController : ControllerBase
{
    private readonly IUtilisateurRepository _utilisateurRepo;

    /// <summary>
    /// Constructeur de la classe ClassementController
    /// </summary>
    /// <param name="utilisateurRepository">Issu de notre API fournissant des fonctions de recherche d'utilisateurs efficaces.</param>
    public ClassementController(IUtilisateurRepository utilisateurRepository)
    {
        this._utilisateurRepo = utilisateurRepository;
    }

    /// <summary>
    /// Route GET de l'API permettant d'obtenir le classement général
    /// </summary>
    /// <returns>Retourne le contrat représentant l'action de la méthode, géré par le framework.</returns>
    /// <remarks>Le JSON obtenu contient: le nom d'utilisateur, son elo ainsi que son classement. Le classement est deéjà trié.</remarks>>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var utilisateurs = await this._utilisateurRepo.GetAllAsync(); // Obtenez tous les utilisateurs
            var classement = new List<ClassementDto>();
            foreach (var utilisateur in utilisateurs)
            {
                classement.Add(
                    new ClassementDto { Username = utilisateur.UserName, Elo = utilisateur.Elo }
                );
            }

            classement = classement.OrderByDescending(s => s.Elo).ToList();

            for (var i = 0; i < classement.Count; i++)
            {
                classement[i].Classement = i + 1;
            }

            return this.Ok(classement);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return this.StatusCode(
                500,
                "Une erreur s'est produite lors de la récupération du classement."
            );
        }
    }

    /// <summary>
    /// Route GET de l'API permettant d'obtenir le classement d'un utilisateur identifié par son nom d'utilisateur
    /// </summary>
    /// <param name="username">Nom d'utilisateur du joueur recherché</param>
    /// <returns>Retourne le contrat représentant l'action de la méthode, géré par le framework.</returns>
    [HttpGet("{username}")]
    public async Task<IActionResult> GetById([FromRoute] string username)
    {
        try
        {
            var utilisateurs = await this._utilisateurRepo.GetAllAsync(); // Obtenez tous les utilisateurs
            var classement = new List<ClassementDto>();
            foreach (var utilisateur in utilisateurs)
            {
                classement.Add(
                    new ClassementDto { Username = utilisateur.UserName, Elo = utilisateur.Elo }
                );
            }

            classement = classement.OrderByDescending(s => s.Elo).ToList();

            for (var i = 0; i < classement.Count; i++)
            {
                classement[i].Classement = i + 1;
            }

            var user = classement.FirstOrDefault(u =>
                u.Username != null && u.Username.Equals(username, StringComparison.Ordinal)
            );
            if (user == null)
            {
                return this.StatusCode(404, "Utilisateur non trouvé");
            }
            return this.Ok(user);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return this.StatusCode(
                500,
                "Une erreur s'est produite lors de la récupération du classement."
            );
        }
    }

    [HttpGet("{pagination:int}")]
    public async Task<IActionResult> GetPaginate([FromRoute] int pagination)
    {
        try
        {
            if (pagination <= 0)
            {
                return this.StatusCode(400, "La valeur de pagination doit être supérieure à zéro.");
            }

            var usersPerPage = 30;

            var utilisateurs = await this._utilisateurRepo.GetAllAsync();

            var classement = utilisateurs
                .Select(u => new ClassementDto { Username = u.UserName, Elo = u.Elo })
                .OrderByDescending(s => s.Elo)
                .ToList();

            var startIndex = (pagination - 1) * usersPerPage;
            var count = Math.Min(usersPerPage, classement.Count - startIndex);

            var paginatedClassement = classement.Skip(startIndex).Take(count).ToList();

            for (var i = 0; i < paginatedClassement.Count; i++)
            {
                paginatedClassement[i].Classement = startIndex + i + 1;
            }

            return this.StatusCode(200, paginatedClassement);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return this.StatusCode(
                500,
                "Une erreur s'est produite lors de la récupération du classement."
            );
        }
    }

    /// <summary>
    /// Route GET de l'API permettant d'obtenir le top n (n un entier naturel supérieur à 0) général.
    /// </summary>
    /// <param name="pagination">Entier supérieur à 0</param>
    /// <returns>Retourne le contrat représentant l'action de la méthode, géré par le framework.</returns>
    [HttpGet("top/{topn:int}")]
    public async Task<IActionResult> GetTopN([FromRoute] int topn)
    {
        if (topn > 0)
        {
            try
            {
                var utilisateurs = await this._utilisateurRepo.GetAllAsync(); // Obtenez tous les utilisateurs
                var classement = new List<ClassementDto>();
                foreach (var utilisateur in utilisateurs)
                {
                    classement.Add(
                        new ClassementDto { Username = utilisateur.UserName, Elo = utilisateur.Elo }
                    );
                }

                classement = classement.OrderByDescending(s => s.Elo).ToList();

                for (var i = 0; i < classement.Count; i++)
                {
                    classement[i].Classement = i + 1;
                }

                return this.Ok(classement.Take(topn));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return this.StatusCode(
                    500,
                    "Une erreur s'est produite lors de la récupération du classement."
                );
            }
        }
        return this.StatusCode(500, "Le top doit être supérieur à 0.");
    }
}

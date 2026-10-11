namespace OkeyApi.Controllers;

using Data;
using Dtos.Compte;
using Dtos.PutsDtos;
using Interfaces;
using Mappers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;

///<summary>
/// Classe Controller de la gestion des comptes utilisateurs
/// Contient toute la logique des end-points de la forme: /compte/*
/// </summary>

[Route("okeyapi/compte")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly UserManager<Utilisateur> _utilisateurManager;
    private readonly ITokenService _tokenService;
    private readonly SignInManager<Utilisateur> _signInManager;
    private readonly IUtilisateurRepository _utilisateurRepository;
    private readonly ApplicationDbContext _dbContext;

    /// <summary>
    /// Constructeur de la classe Account Controller
    /// </summary>
    /// <param name="utilisateurManager">Issu de l'API ASP.NET permettant de gérer la gestion des "Users".</param>
    /// <param name="tokenService">Service de notre API permettant de gérer les JWT Tokens.</param>
    /// <param name="signInManager">Issu de l'API ASP.NET permettant de gérer les l'inscription des "Users".</param>
    /// <param name="utilisateurRepository">Issu de notre API fournissant des fonctions de recherche d'utilisateurs efficaces.</param>
    /// <param name="dbContext">Issu de notre API permet l'écriture/lecture en Base de Donnée</param>
    /// <remarks>Le constructeur est directement pris en charge par l'API, aucun appel n'est nécessaire.</remarks>
    public AccountController(
        UserManager<Utilisateur> utilisateurManager,
        ITokenService tokenService,
        SignInManager<Utilisateur> signInManager,
        IUtilisateurRepository utilisateurRepository,
        ApplicationDbContext dbContext
    )
    {
        this._utilisateurManager = utilisateurManager;
        this._tokenService = tokenService;
        this._signInManager = signInManager;
        this._utilisateurRepository = utilisateurRepository;
        this._dbContext = dbContext;
    }

    /// <summary>
    /// Route POST de l'API supportant la connexion utilisateur
    /// </summary>
    /// <param name="loginDto">Dto de Login contenant deux string: Username et Password. Nom d'utilisateur et Mot de Passe</param>
    /// <returns>Retourne le contrat représentant l'action de la méthode, géré par le framework.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (!this.ModelState.IsValid)
        {
            return this.BadRequest(this.ModelState);
        }

        var user = await this._utilisateurManager.Users.FirstOrDefaultAsync(x =>
            x.UserName != null
            && x.UserName.Equals(loginDto.UserName, StringComparison.OrdinalIgnoreCase)
        );
        if (user == null)
        {
            return this.Unauthorized("Nom d'utilisateur invalide");
        }

        if (loginDto.Password != null)
        {
            var result = await this._signInManager.CheckPasswordSignInAsync(
                user,
                loginDto.Password,
                false
            );
            if (!result.Succeeded)
            {
                return this.Unauthorized(
                    "Utilisateur non trouvé! (nom d'utilisateur ou mot de passe incorrect)"
                );
            }
        }

        return this.Ok(
            new NewUtilisateurDto
            {
                Username = user.UserName,
                Photo = user.Photo,
                Token = this._tokenService.CreateToken(user)
            }
        );
    }

    /// <summary>
    /// Route POST de l'API supportant l'inscription d'utilisateur
    /// </summary>
    /// <param name="registerDto">Dto de Register contenant deux string: Username et Password. Nom d'utilisateur et Mot de Passe</param>
    /// <returns>Retourne le contrat représentant l'action de la méthode, géré par le framework.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            if (registerDto.Photo >= 1 && registerDto.Photo <= 4)
            {
                var utilisateur = new Utilisateur
                {
                    UserName = registerDto.Username,
                    Photo = registerDto.Photo
                };
                if (registerDto.Password != null)
                {
                    var createUtilisateur = await this._utilisateurManager.CreateAsync(
                        utilisateur,
                        registerDto.Password
                    );
                    if (createUtilisateur.Succeeded)
                    {
                        var roleResult = await this._utilisateurManager.AddToRoleAsync(
                            utilisateur,
                            "USER"
                        );
                        if (roleResult.Succeeded)
                        {
                            try
                            {
                                var achievement = new Achievements
                                {
                                    UserId = utilisateur.Id,
                                    Utilisateur = utilisateur
                                };
                                var dbContextAchievements = this._dbContext.Achievements;
                                if (dbContextAchievements != null)
                                {
                                    dbContextAchievements.Add(achievement);
                                }

                                await this._dbContext.SaveChangesAsync();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                throw;
                            }
                            return this.Ok(
                                new NewUtilisateurDto
                                {
                                    Username = utilisateur.UserName,
                                    Photo = utilisateur.Photo,
                                    Token = this._tokenService.CreateToken(utilisateur)
                                }
                            );
                        }
                        return this.StatusCode(500, roleResult.Errors);
                    }
                    return this.StatusCode(500, createUtilisateur.Errors);
                }
            }
            else
            {
                return this.StatusCode(500, "La photo n'est pas disponnible.");
            }
        }
        catch (Exception e)
        {
            return this.StatusCode(500, e);
        }

        return null!;
    }

    /// <summary>
    /// Route GET de l'API permettant de recevoir la liste des utilisateurs inscrits avec Username et Elo Score
    /// </summary>
    /// <returns>Retourne le contrat représentant l'action de la méthode, géré par le framework.</returns>

    [HttpGet("watch")]
    public async Task<IActionResult> GetAll()
    {
        if (!this.ModelState.IsValid)
        {
            return this.BadRequest();
        }
        var users = await this._utilisateurRepository.GetAllAsync();
        var usersDto = users.Select(s => s.ToPublicUtilisateurDto());
        return this.Ok(usersDto);
    }

    /// <summary>
    /// Route GET de l'API permettant de voir le profil d'un utilisateur
    /// </summary>
    /// <param name="username">Nom d'utilisateur</param>
    /// <returns>Retourne le contrat représentant l'action de la méthode, géré par le framework.</returns>
    /// <remarks>Si l'utilisateur met son token JWT (donc si il est connecté et sa session est valide), il peut observer ses propres achievements en regardant sont profil.</remarks>>

    [HttpGet("watch/{username}")]
    public async Task<IActionResult> GetByUsername([FromRoute] string username)
    {
        if (!this.ModelState.IsValid)
        {
            return this.BadRequest();
        }

        var user = await this._utilisateurRepository.GetByUsername(username);
        if (user == null)
        {
            return this.NotFound();
        }

        var userIdentity = this.User.Identity;
        if (userIdentity != null && userIdentity.IsAuthenticated)
        {
            var utilisateur = this.GetCurrentUser();
            if (utilisateur.Result.Username == username)
            {
                var dbContextAchievements = this._dbContext.Achievements;
                if (dbContextAchievements != null)
                {
                    var achievements = await dbContextAchievements.FirstOrDefaultAsync(e =>
                        e.Utilisateur != null
                        && e.Utilisateur.UserName != null
                        && e.Utilisateur.UserName.Equals(
                            utilisateur.Result.Username,
                            StringComparison.Ordinal
                        )
                    );
                    var list = new List<bool>();
                    list.Add(achievements != null && achievements.Jouer5Parties);
                    list.Add(achievements != null && achievements.GagnerUneFois);
                    return this.Ok(
                        new PrivateUtilisateurDto
                        {
                            Username = utilisateur.Result.Username,
                            Photo = utilisateur.Result.Photo,
                            Experience = utilisateur.Result.Experience,
                            Elo = utilisateur.Result.Elo,
                            DateInscription = utilisateur.Result.DateInscription,
                            Achievements = list,
                            NombreParties = utilisateur.Result.NombreParties,
                            NombrePartiesGagnees = utilisateur.Result.NombrePartiesGagnees
                        }
                    );
                }
            }
        }
        return this.Ok(user.ToPublicUtilisateurDto());
    }

    [HttpPut("photo")]
    public async Task<IActionResult> PostChangePhoto([FromBody] PhotoDto PhotoDto)
    {
        if (!this.ModelState.IsValid)
        {
            return this.BadRequest(this.ModelState);
        }
        var user = this.GetCurrentUser();
        var userIdentity = this.User.Identity;
        if (userIdentity is { IsAuthenticated: true })
        {
            if (PhotoDto.photo is >= 1 and <= 4)
            {
                if (user.Result.Username != null)
                {
                    await this._utilisateurRepository.UpdatePhotoAsync(
                        user.Result.Username,
                        PhotoDto.photo
                    );
                }
                return this.Ok("La photo a bien été modifié");
            }
            return this.StatusCode(400, "La photo doit etre comprise entre 1 et 4.");
        }

        return this.StatusCode(
            500,
            "Veuillez vous connecter au compte pour pouvoir changer ces attributs."
        );
    }

    [HttpPut("username")]
    public async Task<IActionResult> PostChangeUsername([FromBody] UsernameDto UsernameDto)
    {
        if (!this.ModelState.IsValid)
        {
            return this.BadRequest(this.ModelState);
        }

        var userIdentity = this.User.Identity;
        if (userIdentity is { IsAuthenticated: true })
        {
            var user = this.GetCurrentUser();
            await this._utilisateurRepository.UpdateUsernameAsync(
                user.Result.Username!,
                UsernameDto.new_username
            );

            var newUser = this._utilisateurManager.Users.FirstOrDefaultAsync(s =>
                (s.UserName != null)
                && s.UserName.Equals(UsernameDto.new_username, StringComparison.Ordinal)
            );

            if (newUser.Result != null)
            {
                return this.Ok(
                    new NewUtilisateurDto
                    {
                        Username = newUser.Result.UserName,
                        Photo = newUser.Result.Photo,
                        Token = this._tokenService.CreateToken(newUser.Result)
                    }
                );
            }
        }

        return this.StatusCode(
            500,
            "Veuillez vous connecter au compte pour pouvoir changer ces attributs."
        );
    }

    [HttpPut("password")]
    public async Task<IActionResult> PostChangePassword([FromBody] PasswordDto PasswordDto)
    {
        if (!this.ModelState.IsValid)
        {
            return this.BadRequest(this.ModelState);
        }

        var user = this.GetCurrentUser();
        var userIdentity = this.User.Identity;
        if (userIdentity is { IsAuthenticated: true })
        {
            try
            {
                if (user.Result.Username != null)
                {
                    await this._utilisateurRepository.UpdatePasswordAsync(
                        user.Result.Username,
                        PasswordDto.old_password,
                        PasswordDto.new_password
                    );
                }
                return this.Ok("Mot de passe modifié");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return this.StatusCode(400, "Le mot de passe est mauvais");
            }
        }

        return this.StatusCode(
            500,
            "Veuillez vous connecter au compte pour pouvoir changer ces attributs."
        );
    }

    /// <summary>
    /// Fonction qui reécupère les donnée de l'utilisateur en fonction du JWT dans le header de requête http.
    /// </summary>
    /// <returns>Retourne le contrat représentant l'action de la méthode, géré par le framework.</returns>
    private async Task<PrivateUtilisateurDto> GetCurrentUser()
    {
        var claims = this.HttpContext?.User?.Claims;
        if (claims != null)
        {
            var username = claims.First().Value;
            var user = await this._utilisateurRepository.GetByUsername(username);
            if (user == null)
            {
                return null!;
            }
            return user.ToPrivateUtilisateurDto();
        }
        return null!;
    }
}

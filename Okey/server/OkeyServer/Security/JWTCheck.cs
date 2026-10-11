namespace OkeyServer.Security;

using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Classe pour vérifier les jetons JWT.
/// </summary>
public class JWTCheck
{
    /// <summary>
    /// Attribut contenant la configuration de l'API.
    /// </summary>
    private readonly IConfiguration _config;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="JWTCheck"/>.
    /// </summary>
    /// <param name="config">Configuration de l'API.</param>
    public JWTCheck(IConfiguration config)
    {
        this._config = config;
    }

    /// <summary>
    /// Vérifie la validité d'un jeton JWT.
    /// </summary>
    /// <param name="token">Le jeton JWT à vérifier.</param>
    /// <returns>Retourne vrai si le jeton est valide, sinon faux.</returns>
    public static bool CheckToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidIssuer = "http://localhost:5246", // Définir l'émetteur ici
            ValidateAudience = true,
            ValidAudience = "http://localhost:5246", // Définir l'audience ici
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(
                    "sdgfijjh3466iu345g87g08c24g7204gr803g30587ghh35807fg39074fvg80493745gf082b507807g807fgf"
                )
            ),
        };

        try
        {
            // Valider le jeton et extraire les revendications
            var claimsPrincipal = tokenHandler.ValidateToken(
                token,
                tokenValidationParameters,
                out var validatedToken
            );
        }
        catch (SecurityTokenValidationException)
        {
            return false;
        }
        return true;
    }
}

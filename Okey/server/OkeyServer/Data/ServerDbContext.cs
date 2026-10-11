namespace OkeyServer.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Models;

/// <summary>
/// Représente le contexte de la base de données du serveur, incluant les configurations de l'identité.
/// </summary>
public class ServerDbContext(DbContextOptions dbContextOptions)
    : IdentityDbContext<Utilisateur>(dbContextOptions)
{
    /// <summary>
    /// Obtient ou définit les entités de réalisations dans la base de données.
    /// </summary>
    public DbSet<Achievements>? Achievements { get; set; }

    /// <summary>
    /// Configurations du modèle lors de la création du modèle.
    /// </summary>
    /// <param name="builder">Le générateur de modèle à utiliser pour configurer le modèle.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var roles = new List<IdentityRole>
        {
            new IdentityRole { Name = "Admin", NormalizedName = "ADMIN" },
            new IdentityRole { Name = "Utilisateur", NormalizedName = "USER" }
        };
        builder.Entity<IdentityRole>().HasData(roles);
    }
}

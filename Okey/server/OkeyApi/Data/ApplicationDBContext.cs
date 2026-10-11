namespace OkeyApi.Data;

using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OkeyApi.Models;

/// <summary>
/// Classe permettant l'insertion des Modèles en Base de Donnée
/// </summary>
/// <param name="dbContextOptions">Paramètre propre au framework.</param>
public class ApplicationDbContext(DbContextOptions dbContextOptions)
    : IdentityDbContext<Utilisateur>(dbContextOptions)
{
    public DbSet<Achievements>? Achievements { get; set; }

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

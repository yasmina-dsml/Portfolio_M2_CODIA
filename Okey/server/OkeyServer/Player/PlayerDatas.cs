namespace OkeyServer.Player;

using System.Collections.Concurrent;
using Data;
using Exceptions;

/// <summary>
/// Classe contenant toutes les données d'un utilisateur connecté ou d'un invité actuellement sur le serveur.
/// </summary>
public class PlayerDatas
{
    /// <summary>
    /// Obtient ou définit le nom d'utilisateur.
    /// </summary>
    private string Username { get; set; }

    /// <summary>
    /// Obtient ou définit le score Elo du joueur.
    /// </summary>
    private int Elo { get; set; }

    /// <summary>
    /// Obtient ou définit l'ID de la photo de l'utilisateur.
    /// </summary>
    private int Photo { get; set; }

    /// <summary>
    /// Obtient ou définit la date d'inscription de l'utilisateur.
    /// </summary>
    private DateTime DateInscription { get; set; }

    /// <summary>
    /// Obtient ou définit l'expérience de l'utilisateur.
    /// </summary>
    private int Experience { get; set; }

    /// <summary>
    /// Obtient ou définit le nombre de parties jouées par l'utilisateur.
    /// </summary>
    private int NombreParties { get; set; }

    /// <summary>
    /// Obtient ou définit le nombre de parties gagnées par l'utilisateur.
    /// </summary>
    private int NombrePartiesGagnees { get; set; }

    /// <summary>
    /// Dictionnaire concurrent contenant les succès de l'utilisateur.
    /// </summary>
    private ConcurrentDictionary<string, bool>? _achievements;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="PlayerDatas"/>.
    /// </summary>
    /// <param name="DbContext">Contexte de la base de données du serveur.</param>
    /// <param name="Username">Nom d'utilisateur.</param>
    public PlayerDatas(ServerDbContext DbContext, string Username)
    {
        this.Username = Username;
        if (Username == "Guest")
        {
            this.Elo = 400;
            var random = new Random();
            var randomNumber = random.Next(1, 5);
            this.Photo = randomNumber;
            this.Experience = 0;
            this.NombreParties = 0;
            this.NombrePartiesGagnees = 0;
            this.DateInscription = DateTime.Now;
        }
        else
        {
            // on cherche dans la base de donnée la ligne de la table correspondante au username fourni
            var player = DbContext.Users.FirstOrDefault(User => User.UserName == Username);

            if (player != null)
            {
                this.Elo = player.Elo;
                this.Photo = player.Photo;
                this.Experience = player.Experience;
                this.NombreParties = player.NombreParties;
                this.NombrePartiesGagnees = player.NombrePartiesGagnees;
                this.DateInscription = player.DateInscription;

                // partie Achievements :
                this._achievements = new ConcurrentDictionary<string, bool>();
                var achievDbContext = DbContext.Achievements;
                if (achievDbContext != null)
                {
                    var achievements = achievDbContext.FirstOrDefault(Ach =>
                        Ach.Utilisateur != null
                        && Ach.Utilisateur.UserName != null
                        && Ach.Utilisateur.UserName.Equals(Username, StringComparison.Ordinal)
                    );
                    if (achievements != null)
                    {
                        // ajouter tous les achievements au dictionnaire, à la main ;'(
                        var added = this._achievements.TryAdd(
                            "Jouer5Parties",
                            achievements.Jouer5Parties
                        );
                        added =
                            added
                            && this._achievements.TryAdd(
                                "GagnerUneFois",
                                achievements.GagnerUneFois
                            );
                        if (added == false)
                        {
                            // exception un ajout au dictionnaire a échoué
                            throw new DictionnaryAddException(
                                "Couldn't add one or more achievements to the user data structure"
                            );
                        }
                    }
                    else
                    {
                        // exception aucune ligne achievements correspondat à l'utilisateur n'a été trouvée
                        throw new UserAchievementsNotFoundException(
                            "Couldn't find " + this.Username + " achievements"
                        );
                    }
                }
                else
                {
                    // exception aucune entrée de la table achievements n'a été trouvée
                    throw new UserAchievementsNotFoundException("Couldn't find users achievements");
                }
            }
            else
            {
                // le joueur avec cet username n'a pas été trouvé dans la base de donnée
                throw new UserAchievementsNotFoundException(
                    "Couldn't find user : " + this.Username
                );
            }
        }
    }

    /// <summary>
    /// Vérifie si l'utilisateur a débloqué un succès spécifique.
    /// </summary>
    /// <param name="Achievement">Nom du succès.</param>
    /// <returns>Booléen vrai si l'achievement est débloqué.</returns>
    public bool CheckAchievement(string Achievement)
    {
        if (this.Username == "Guest")
        {
            return false;
        }
        if (this._achievements != null)
        {
            return this._achievements[Achievement];
        }
        return false;
    }

    /// <summary>
    /// Met à jour les statistiques d'un utilisateur en les incrémentant des valeurs fournies en paramètres.
    /// </summary>
    /// <param name="dbContext">Le contexte permettant d'accéder à la base de données.</param>
    /// <param name="elo">Le Elo à rajouter au Elo du joueur.</param>
    /// <param name="experience">L'expérience à rajouter à l'expérience du joueur.</param>
    /// <param name="partieJouee">Booléen indiquant si le nombre de parties doit être incrémenté, vrai par défaut.</param>
    /// <param name="PartieGagnee">Booléen indiquant si le nombre de parties gagnées doit être incrémenté, faux par défaut.</param>
    /// <exception cref="UserUpdateException">Exception indiquant l'échec de la mise à jour de l'utilisateur.</exception>
    public async Task UpdateStats(
        ServerDbContext dbContext,
        int elo,
        int experience,
        bool partieJouee = true,
        bool PartieGagnee = false
    )
    {
        if (this.Username != "Guest")
        {
            var userUpdate = dbContext.Users.SingleOrDefault(u => u.UserName == this.Username);
            if (userUpdate != null)
            {
                this.Elo = elo;
                this.Experience += experience;

                if (this.NombreParties == 4 && partieJouee)
                {
                    await this.UpdateAchievement(dbContext, "Jouer5Parties");
                }

                if (partieJouee)
                {
                    this.NombreParties++;
                }

                if (this.NombrePartiesGagnees == 0 && PartieGagnee)
                {
                    await this.UpdateAchievement(dbContext, "GagnerUneFois");
                }

                if (PartieGagnee)
                {
                    this.NombrePartiesGagnees++;
                }

                userUpdate.Elo = this.Elo;
                userUpdate.Experience = this.Experience;
                userUpdate.NombreParties = this.NombreParties;
                userUpdate.NombrePartiesGagnees = this.NombrePartiesGagnees;
                await dbContext.SaveChangesAsync();
            }
            else
            {
                // exception erreur lors de la récupération du profil
                throw new UserUpdateException(
                    "Couldn't fetch user :" + this.Username + " from the database"
                );
            }
        }
    }

    /// <summary>
    /// Met à jour un succès, par défaut le déverrouille et si value est renseigné le met à la valeur de value.
    /// </summary>
    /// <param name="dbContext">Contexte de la base de données du serveur.</param>
    /// <param name="achievement">Titre du succès.</param>
    /// <param name="value">Nouvelle valeur du succès.</param>
    /// <exception cref="UserAchievementsNotFoundException">Exception indiquant l'échec de récupérations des succès dans la base de données.</exception>
    public async Task UpdateAchievement(
        ServerDbContext dbContext,
        string achievement,
        bool value = true
    )
    {
        if (this.Username != "Guest")
        {
            if (this._achievements != null)
            {
                this._achievements[achievement] = value;
            }
            else
            {
                throw new UserAchievementsNotFoundException("achievements attribute is null");
            }

            var achievDbContext = dbContext.Achievements;
            if (achievDbContext != null)
            {
                var achievements = achievDbContext.FirstOrDefault(ach =>
                    ach.Utilisateur != null
                    && ach.Utilisateur.UserName != null
                    && ach.Utilisateur.UserName.Equals(this.Username, StringComparison.Ordinal)
                );
                if (achievements != null)
                {
                    if (achievement.Equals("Jouer5Parties", StringComparison.Ordinal))
                    {
                        achievements.Jouer5Parties = value;
                    }
                    else if (achievement.Equals("GagnerUneFois", StringComparison.Ordinal))
                    {
                        achievements.GagnerUneFois = value;
                    }
                    else
                    {
                        throw new UserAchievementsNotFoundException(
                            "Couldn't find the achievement : "
                                + achievement
                                + " in the user : "
                                + this.Username
                                + " achievements collection"
                        );
                    }

                    await dbContext.SaveChangesAsync();
                }
                else
                {
                    // exception aucune ligne achievements correspondant à l'utilisateur n'a été trouvée
                    throw new UserAchievementsNotFoundException(
                        "Couldn't find " + this.Username + " achievements"
                    );
                }
            }
            else
            {
                // exception aucune entrée de la table achievements n'a été trouvée
                throw new UserAchievementsNotFoundException("Couldn't find users achievements");
            }
        }
    }

    /// <summary>
    /// Retourne une chaîne représentant les informations de l'utilisateur.
    /// </summary>
    /// <returns>Chaîne contenant les informations de l'utilisateur.</returns>
    public override string ToString()
    {
        return "username : "
            + this.Username
            + " elo : "
            + this.Elo
            + " date d'inscription : "
            + this.DateInscription
            + " photo : "
            + this.Photo
            + " exp : "
            + this.Experience
            + " nb parties jouées : "
            + this.NombreParties
            + " nb parties gagnées : "
            + this.NombrePartiesGagnees;
    }

    /// <summary>
    /// Retourne les informations publiques du joueur.
    /// </summary>
    /// <returns>Instance de <see cref="PlayerPublicData"/> contenant les informations publiques du joueur.</returns>
    public PlayerPublicData PlayerInfos()
    {
        return new PlayerPublicData(
            this.Username,
            this.Elo,
            this.Photo,
            this.DateInscription,
            this.Experience,
            this.NombreParties,
            this.NombrePartiesGagnees
        );
    }

    /// <summary>
    /// Met à jour l'avatar de l'utilisateur.
    /// </summary>
    /// <param name="dbContext">Contexte de la base de données du serveur.</param>
    /// <param name="photo">Nouvelle photo de l'utilisateur.</param>
    /// <exception cref="UserUpdateException">Exception indiquant l'échec de la mise à jour de l'avatar de l'utilisateur.</exception>
    public async Task UpdateAvatar(ServerDbContext dbContext, int photo)
    {
        if (this.Username == "Guest")
        {
            this.Photo = photo;
        }
        else
        {
            var userUpdate = dbContext.Users.SingleOrDefault(u => u.UserName == this.Username);
            if (userUpdate != null)
            {
                userUpdate.Photo = photo;
                await dbContext.SaveChangesAsync();
            }
            else
            {
                throw new UserUpdateException("couldn't update user's avatar");
            }
        }
    }

    /// <summary>
    /// Obtient le nom d'utilisateur.
    /// </summary>
    /// <returns>Nom d'utilisateur.</returns>
    public string GetUsername()
    {
        return this.Username;
    }

    /// <summary>
    /// Obtient la photo de l'utilisateur.
    /// </summary>
    /// <returns>Identifiant de la photo de l'utilisateur.</returns>
    public int GetPhoto()
    {
        return this.Photo;
    }

    /// <summary>
    /// Obtient l'expérience de l'utilisateur.
    /// </summary>
    /// <returns>Valeur de l'expérience de l'utilisateur.</returns>
    public int GetExperience()
    {
        return this.Experience;
    }

    /// <summary>
    /// Obtient le Elo de l'utilisateur.
    /// </summary>
    /// <returns>Valeur du Elo de l'utilisateur.</returns>
    public int GetElo()
    {
        return this.Elo;
    }

    /// <summary>
    /// Retourne une chaîne représentant les succès de l'utilisateur.
    /// </summary>
    /// <returns>Chaîne contenant les succès de l'utilisateur.</returns>
    /// <exception cref="UserAchievementsNotFoundException">Exception indiquant l'absence du dictionnaire des succès.</exception>
    public string AchievementsToString()
    {
        var achievements = "";
        if (this._achievements == null)
        {
            throw new UserAchievementsNotFoundException("_achievements attribute is null");
        }
        if (this.Username != "Guest")
        {
            foreach (var elmt in this._achievements)
            {
                achievements += elmt.Key + " : " + elmt.Value + "\n";
            }
        }
        else
        {
            achievements += "this user is a guest, no achievements available";
        }

        return achievements;
    }
}

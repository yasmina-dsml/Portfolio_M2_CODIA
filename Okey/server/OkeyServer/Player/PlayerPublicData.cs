namespace OkeyServer.Player;

/// <summary>
/// Classe représentant les données publiques d'un joueur.
/// </summary>
public class PlayerPublicData
{
    /// <summary>
    /// Obtient ou définit le nom d'utilisateur.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Obtient ou définit le score Elo du joueur.
    /// </summary>
    public int Elo { get; set; }

    /// <summary>
    /// Obtient ou définit l'identifiant de la photo de l'utilisateur.
    /// </summary>
    public int Photo { get; set; }

    /// <summary>
    /// Obtient ou définit la date d'inscription de l'utilisateur.
    /// </summary>
    public DateTime DateInscription { get; set; }

    /// <summary>
    /// Obtient ou définit l'expérience de l'utilisateur.
    /// </summary>
    public int Experience { get; set; }

    /// <summary>
    /// Obtient ou définit le nombre de parties jouées par l'utilisateur.
    /// </summary>
    public int NombreParties { get; set; }

    /// <summary>
    /// Obtient ou définit le nombre de parties gagnées par l'utilisateur.
    /// </summary>
    public int NombrePartiesGagnees { get; set; }

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="PlayerPublicData"/>.
    /// </summary>
    /// <param name="Username">Nom d'utilisateur.</param>
    /// <param name="Elo">Score Elo du joueur.</param>
    /// <param name="Photo">Identifiant de la photo de l'utilisateur.</param>
    /// <param name="DateInscription">Date d'inscription de l'utilisateur.</param>
    /// <param name="Experience">Expérience de l'utilisateur.</param>
    /// <param name="NombreParties">Nombre de parties jouées par l'utilisateur.</param>
    /// <param name="NombrePartiesGagnees">Nombre de parties gagnées par l'utilisateur.</param>
    public PlayerPublicData(
        string Username,
        int Elo,
        int Photo,
        DateTime DateInscription,
        int Experience,
        int NombreParties,
        int NombrePartiesGagnees
    )
    {
        this.Username = Username;
        this.Elo = Elo;
        this.Photo = Photo;
        this.DateInscription = DateInscription;
        this.Experience = Experience;
        this.NombreParties = NombreParties;
        this.NombrePartiesGagnees = NombrePartiesGagnees;
    }

    /// <summary>
    /// Retourne une chaîne représentant les informations publiques du joueur.
    /// </summary>
    /// <returns>Chaîne contenant les informations publiques du joueur.</returns>
    public override string ToString()
    {
        return "username : "
            + this.Username
            + " elo : "
            + this.Elo
            + " photo : "
            + this.Photo
            + " date d'inscription : "
            + this.DateInscription
            + " exp : "
            + this.Experience
            + " nb parties jouées : "
            + this.NombreParties
            + " nb parties gagnées : "
            + this.NombrePartiesGagnees;
    }
}

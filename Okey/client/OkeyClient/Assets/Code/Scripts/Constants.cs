using System.Net;

public static class Constants
{
    public const int MAX_JOUEURS = 4;
    public const int MAX_TUILES = 106;
    public const int MAX_TUILES_PENDANT_JEU = 105;
    public const int MAX_TUILES_PAR_JOUEUR = 14;
    public const int MAX_TUILES_PAR_PILE = 12;
    public const int MAX_TUILES_PAR_COULEUR = 26;
    public const int MAX_TUILES_JOKER = 2;
    public const string API_URL =
#if LOCAL
        "http://localhost/okeyapi/";
#elif (DEBUG || UNITY_EDITOR)
        "https://mai-projet-integrateur.u-strasbg.fr/vmProjetIntegrateurgrp0-0/okeyapi/";
#else
        "https://mai-projet-integrateur.u-strasbg.fr/vmProjetIntegrateurgrp0-1/okeyapi/";
#endif
    public const string SELF_PLAYER_SAVE_FILE = "/Player.dat";
    public const string ANONYMOUS_PLAYER_NAME = "Anonyme";
    public const string SIGNALR_HUB_URL =
#if LOCAL
        "http://localhost:10000/OkeyHub";
#elif (DEBUG || UNITY_EDITOR)
        "http://mai-projet-integrateur.u-strasbg.fr:10000/OkeyHub";
#else
        "http://mai-projet-integrateur.u-strasbg.fr:10008/OkeyHub";
#endif
}

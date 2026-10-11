# Les applications Backend

L'organisation des fichiers dans ce projet Unity suit la structure proposée sur [cette page](https://unity.com/how-to/organizing-your-project) et surtout l'exemple 1.

La convention de nommage des fichiers (et autres) suit [PascalCase](https://en.wikipedia.org/wiki/Camel_case) (la première lettre aussi en majuscule).

## Structure des projets

Il existe 3 projets C# et un projet [Express](https://expressjs.com/) pour le site web dans ce répertoire dédié au backend :

* **API** : l'API REST qui communique avec la base de données (sous `OkeyApi/`)
* **Server** : le serveur qui gère les connexions des clients aux lobby en temps réel avec [SignalR](https://dotnet.microsoft.com/en-us/apps/aspnet/signalr) (sous `OkeyServer/`)
* **Logique** : la logique métier du jeu, les règles du jeu et son déroulement (sous `OkeyScript/`)
* **Site web** : le site web qui a comme but de présenter le jeu et de permettre aux joueurs de télécharger le jeu (sous `okeywebsite/new_app/`)

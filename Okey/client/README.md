# Client Okey en Unity

L'organisation des fichiers dans ce projet Unity suit la structure proposée sur [cette page](https://unity.com/how-to/organizing-your-project) et surtout l'exemple 1.

La convention de nommage des fichiers (et autres) suit [PascalCase](https://en.wikipedia.org/wiki/Camel_case) (la première lettre aussi en majuscule).

La version de Unity utilisée est la **2022.3.19f1 LTS**.

## Structure du projet

Le projet Unity se trouve sous `OkeyClient/`. Au sein de ce répertoire, il y a surtout des répertoires et des fichiers auto générés par Unity. Les fichiers qui nous intéressent sont dans `OkeyClient/Assets/`. Ce sont les fichiers que nous avons créés. Il y a des images, des fonds, des icônes, des logos, des scripts, des scènes, etc.

Le cœur du projet est dans le répertoire `OkeyClient/Assets/Code/Scripts/`. C'est là que se trouvent les scripts qui font fonctionner le jeu. Les scripts sont écrits en C#. Les scripts sont organisés en sous-répertoires en fonction de leur rôle. Par exemple, les scripts qui gèrent les multiples aspects de la page d'accueil sont dans `OkeyClient/Assets/Code/Scripts/PageAccueil/`.

Les plugins tiers non disponibles depuis Unity Asset Store sont dans `OkeyClient/Assets/Plugins/`. Il y a des paquets NuGet comme le [client SignalR](https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Client/8.0.4#readme-body-tab) et [`System.Text.Json`](https://www.nuget.org/packages/System.Text.Json/8.0.3#readme-body-tab). Ce sont des `.dll` compilés pour .NET Standard 2.0 et/ou 2.1.

Sous `OkeyClient/Assets/Editor/` se trouve des scripts qui ne sont pas exécutés dans le jeu mais qui sont exécutés dans l'éditeur Unity. Par exemple, c'est ici que se trouve le script pour compiler le client avec les bons paramètres.

## Astuces pour Unity

### Utiliser le script de compilation

Pour compiler le client en local, il faut exécuter le script `OkeyClient/Assets/Editor/Builders/Builder.cs`. Il est possible de le faire depuis l'éditeur Unity. Il suffit de cliquer sur le menu `MyTools` puis choisir le type de build que vous voulez.

C'est ce script qui est utilisé dans le CI/CD pour compiler le client. Donc même s'il reste possible de compiler manuellement vous même le client en allant sous `File > Build Settings`, il est recommandé d'utiliser le script de compilation pour avoir une compilation officiel avec les bons paramètres. Les différences entre les variante de build sont les symboles de compilation et les paramètres de compilation. Cela est expliqué plus en détails dans le [README général](../README.md#Déploiements-frontend) du projet.

### Utiliser les symboles de compilation

Les build lancés en utilisant le script de compilation officiel utilisent des symboles de compilation et la compilation conditionnelle pour différencier les variantes de build.

Par exemple, le build de développement (`*-test`) utilise le symbole `DEBUG` et le build de développement local (`*-local-test`) utilise en plus le symbole `LOCAL`. Cela permet de faire des actions conditionnelles dans le code en fonction de la variante de build. Par exemple, pour activer ou désactiver des logs, des fonctionnalités, etc.

Notamment définir le symbole `LOCAL` fait que le client contacte `http://localhost/` dans ses requêtes réseau vers le backend. Cela est utile pour les tests en local. Il faut savoir que ce symbole désactive la mémorisation des identifiants de connexion. Cela signifie que le client ne se souvient pas des identifiants de connexion entre les sessions. Cela est fait pour permettre de se connecter à plusieurs comptes différents simultanément sur plusieurs instances du jeu lancés et donc pour pouvoir tester le multi joueur à 4 personnes en local tout seul.

Le symbole `DEBUG` fait que le client contacte le serveur "Staging" via HTTPS dans ses requêtes réseau vers le backend.

Si aucun de ces symboles sont définis, le client contacte le serveur "Production" via HTTPS dans ses requêtes réseau vers le backend. La mémorisation des identifiants de connexion est activée. Il faut pas essayer de se connecter à plusieurs comptes différents simultanément sur plusieurs instances du jeu lancés. De toute façon, cela n'a pas de sens en production et ne devrait pas arriver vu que le script de compilation officiel désactive le lancement de plus d'un seul instance sur ce build `*-release`.

Regardez cette page de [documentation Unity](https://docs.unity3d.com/Manual/CustomScriptingSymbols.html) pour plus d'informations sur les symboles de compilation et comment les définir au sein de l'éditeur Unity.

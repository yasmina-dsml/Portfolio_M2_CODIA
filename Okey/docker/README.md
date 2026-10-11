# Les images Docker

Vous pouvez utiliser le `Makefile` fourni pour construire les images Docker en local. Pensez à supprimer les images et les conteneurs manuellement une fois vous avez fini vu que le `Makefile` ne le fait pas. Les commandes `make` sont à exécuter depuis ce répertoire-ci si jamais vous les utilisez. Les commandes `make` sont les suivantes :

```sh
make api
make server
make cicd
make db-init
make clean
```

`make clean` nettoie les fichiers temporaires créés par le `Makefile` ainsi que les données persistantes des conteneurs créés par Docker Compose (plus de détails sur ça [en bas](#faire-tourner-un-back-end-complet-en-local)). Cela ne supprime pas les images Docker que vous avez construites.

Pensez à bien nommer les images Docker quand vous faites un build local comme le fait le `Makefile` si vous voulez les push sur le registre GitLab Unistra par après. Normalement vous n'êtes pas censé faire des push des images locales sur le registre GitLab Unistra. C'est le job des pipelines CI/CD.

À savoir que le [build context](https://docs.docker.com/build/building/context/) Docker est le répertoire racine du projet. Dans les Dockerfiles que vous allez écrire, utilisez donc des chemins relatifs à partir de la racine du projet.

##  Construction automatique des images Docker sur GitLab CI/CD

Les pipelines CI/CD GitLab sont configurés pour construire les images Docker et les pousser sur le registre GitLab Unistra lorsqu'il y a un changement dans les Dockerfiles ou bien dans leurs dépendances sur certaines branches git.

Les images des applications back-end sont construits automatiquement sur la branche `back-end` quand un changement dans les fichiers `Dockerfile` ou dans leurs autres dépendances est détecté. Ces images sont taggés systématiquement `dev-<short git hash>` et le méta tag `dev` pointe vers la dernière de ces versions test/développement.

La version stable de ces images est construite quand un tag de release est détecté sur la branche `main`. Ces images stables sont taggés à leur tour par la version de la release et le méta tag `latest` pointe vers la dernière version stable.

## Récupération des images Docker

Vous avez la possibilité de récupérer les images Docker depuis le registre GitLab Unistra en utilisant les commandes suivantes :

```sh
docker pull registry.app.unistra.fr/okaybytes/okey/okeyapi:[tag]

docker pull registry.app.unistra.fr/okaybytes/okey/okeyserver:[tag]

docker pull registry.app.unistra.fr/okaybytes/okey/projet-integrateur-cicd:[tag]

docker pull registry.app.unistra.fr/okaybytes/okey/db-init:[tag]
```

avec `[tag]` qui peut être soit `dev` pour la version de développement ou bien une version de release. Regardez le [registre GitLab Unistra](https://git.unistra.fr/okaybytes/okey/container_registry) pour les tags disponibles.

Vous n'êtes donc pas obligé de construire les images Docker localement si vous voulez juste les tester. Mais si vous avez des modifications en local, vous pouvez les construire localement en utilisant le `Makefile` fourni pour tester avec vos derniers changements intégrés.

## Faire tourner un back-end complet en local

Il faut que vous ayez [Docker Compose](https://docs.docker.com/compose/) installé sur votre machine pour pouvoir faire tourner un back-end complet en local. C'est le fichier `compose.yml` qui décrit comment lancer les conteneurs Docker des applications back-end et de la base de données en local. Les commandes `docker compose` sont à exécuter depuis ce répertoire-ci si jamais vous les utilisez.

Pour vous simplifier la tâche le `Makefile` contient les commandes `docker compose` nécessaires pour tout lancer en local. Les commandes `make` sont les suivantes :

```sh
make start-core
make stop-core
make build-core

make start-extra
make stop-extra
make build-extra

make clean
```

La famille de commandes `core` sont destinées pour lancer un jeu minimal de conteneurs pour aller à l'essentiel et lancer tout simplement le minimum requis pour un back-end fonctionnel en local.

La famille de commandes `extra` inclut des outils en plus pour l'administration. Notamment [phpMyAdmin](https://www.phpmyadmin.net/) pour une visualisation facile de la base de données MySQL et [Portainer](https://www.portainer.io/) pour une visualisation facile des conteneurs Docker. C'est cette famille de conteneurs qui est déployé sur le serveur de production.

Les commandes `start` lancent les conteneurs et tous les ressources nécessaires pour un environnement back-end complet. Les commandes `stop` arrêtent et suppriment les conteneurs. Les commandes `build` construisent les images Docker en local si jamais vous avez des modifications en local comme le fait les autres commandes `make` expliquées en haut mais cette fois en exploitant `docker compose`. Avant d'utiliser les commandes `build`, assurez vous d'avoir supprimé toutes les images de l'application que vous voulez reconstruire en local. Par exemple si vous avez fait des changements à `OkeyApi` en local et que vous ne les avez pas encore push sur la branche `back-end`, ni `main` (donc CI/CD n'a pas pu encore construire de nouvelles images et les mettre sur le registre GitLab Unistra), vous pouvez supprimer toutes les images reliées à `OkeyApi`, puis lancer `make build-[profil]`. La prochaine fois vous lancez `make start-[profil]`, les conteneurs vont utiliser les images que vous avez construites en local. Cela vous permet de tester vos changements en local avant de les push sur le dépôt git.

Tous les fichiers persistants des conteneurs lancés avec `docker compose` sont stockés dans le répertoire `docker_data/`. La première fois que vous lancez les conteneurs, les données persistantes sont stocké dans ce répertoire comme par exemple les fichiers de la base de données et encore des autres outils du profil extra. Cela vous permet de supprimer les conteneurs et les redémarrer sans perdre les données. Si vous voulez supprimer les données persistantes, utilisez la commande `make clean` pour nettoyer tout. Vous aurez donc un back-end tout propre.

Les fichiers de configuration et d'initialisation se trouvent aussi sous `docker_data/`. Comme par exemple la configuration Nginx, l'initialisation MySQL ou encore la configuration phpMyAdmin.

Vous pouvez facultativement spécifier la variable d'environnement `TARGET` avec une valeur correspondant à un tag d'image Docker comme `dev`, `latest` ou encore `dev-14619891` pour indiquer à `docker compose` de pull une image spécifique depuis le registre. Il faut bien sûr supprimer les images avant quand vous voulez changer entre les images que vous construisez localement et celles du registre et vice versa. Par défaut `docker compose` utilise les images du registre GitLab Unistra avec le tag `dev` donc le plus récent depuis la branche `back-end` du dépôt git. Un exemple d'utilisation de la variable d'environnement `TARGET` est :

```sh
export TARGET=latest
```

Si vous lancez le profil `extra` et que vous voulez accéder à phpMyAdmin ou Portainer, vous pouvez les trouver sur `http://localhost/phpmyadmin/` et `http://localhost/portainer/`. Pour phpMyAdmin en local, les identifiants disponibles sont `readonly` et `admin` sans mot de passe par défaut. Pour Portainer, créez un compte administrateur et connectez vous. Si vous faites un `make clean` il faut recréer un compte administrateur pour Portainer.

Le compte phpMyAdmin `readonly` a seulement des droits de lecture sur toutes les bases de données. Le compte `admin` a en plus de ces droits, les droits d'écriture dans la base de données `okeydatabase` utilisé par l'API. Vous pouvez donc tester des requêtes SQL en local avec phpMyAdmin si vous voulez ou modifier, insérer ou effacer des entrées comme vous le désirez lors de vos tests.

L'API reste disponible sur `http://localhost/okeyapi/` et le serveur sur `http://localhost/OkeyHub/`. Pour les endpoints exactes et leurs utilisation référez vous à la documentation de l'API ou du serveur.

Tout est rendu accessible dans un réseau Docker interne. Vous pouvez donc accéder à tous les services depuis votre machine locale sans problème mais pas de l'extérieur. Tous les services sont exposés sur le port `80` grâce à Nginx donc pas besoin de spécifier un numéro de port dans vos URL ou requêtes.

Quand vous lancez pour la première fois les conteneurs ou après un `make clean`, il faut attendre un peu pour que les conteneurs soient prêts. Les conteneurs de l'API et du serveur ont besoin de quelques secondes pour démarrer et se connecter à la base de données. La base de données nécessite un peu de temps elle même pour créer les tables nécessaires et initialiser. Si vous avez des problèmes de connexion à la base de données, c'est probablement parce que la base de données n'est pas encore prête. Attendez un peu et réessayez.

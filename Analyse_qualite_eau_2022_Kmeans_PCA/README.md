# Analyse de la qualité de l’eau — données 2022

Projet universitaire en M1 d’analyse de données environnementales avec Python : nettoyage, exploration statistique, regroupement de stations de mesure et cartographie des résultats.

## Objectif

L’objectif est d’identifier des stations de cours d’eau présentant des caractéristiques similaires à partir de leurs mesures de qualité de l’eau en 2022.

L’analyse porte sur 16 paramètres physico-chimiques, notamment la température, le pH, les nitrates, l’oxygène dissous et la turbidité. Elle explore ensuite la répartition géographique des groupes et l’occupation du sol au niveau des stations.

## Travail réalisé

### 1. Préparation des données

Les données proviennent de trois tables : les stations, les prélèvements et les résultats d’analyse.

Les traitements réalisés comprennent :

- La lecture du fichier d’analyses par blocs de 300 000 lignes pour limiter la mémoire utilisée au chargement.
- La sélection des prélèvements d’eau et des paramètres étudiés.
- Le filtrage des analyses de qualité `Correcte` et la sélection de la fraction analysée selon le paramètre (eau brute ou phase aqueuse filtrée).
- La conversion des dates et des valeurs numériques.
- Le traitement des doublons et des résultats manquants.
- Les jointures entre les trois tables.
- La conservation des stations ayant au moins 10 paramètres renseignés sur 16.

Les sorties enregistrées du notebook indiquent **331 122 mesures pour 3 321 stations** après cette préparation.

### 2. Exploration et analyse statistique

L’exploration permet de comprendre les données avant d’appliquer un modèle :

- Calcul de statistiques descriptives.
- Visualisation des distributions avec des histogrammes et des boxplots.
- Vérification des paramètres disponibles pour chaque station.
- Analyse des corrélations de Spearman entre les valeurs médianes des paramètres par station.

### 3. Construction des profils de stations

Les mesures sont résumées pour obtenir un tableau contenant une ligne par station et une colonne par caractéristique.

Deux représentations sont comparées :

- **Moyennes annuelles** : résumer les conditions moyennes observées.
- **Percentiles orientés** : prendre en compte des conditions potentiellement plus défavorables.

Dans les deux représentations, le pH est remplacé par son écart absolu à une référence fixée à 7,5 avant agrégation. Pour la représentation par percentiles, le 10e percentile est utilisé pour l’oxygène dissous et sa saturation, et le 90e percentile pour les autres caractéristiques, dont cet écart de pH.

Ces choix sont des simplifications méthodologiques du projet, et non des seuils réglementaires.

Avant le regroupement, les valeurs manquantes sont remplacées par la médiane de leur variable. Les variables sont ensuite standardisées pour rendre leurs échelles comparables.

### 4. Regroupement avec K-means

L’algorithme **K-means** est utilisé pour regrouper les stations selon leurs ressemblances, sans disposer de catégories connues à l’avance.

Le notebook compare :

- Deux représentations des stations : moyennes et percentiles.
- Trois nombres de groupes : 4, 5 et 6.
- Les scores de silhouette, qui évaluent la cohésion et la séparation des groupes.

Chaque ajustement utilise `n_init=50` (50 initialisations) et `random_state=42`. Le notebook retient quatre groupes et conserve une comparaison à cinq groupes.

Une **analyse en composantes principales (ACP) à deux composantes** permet ensuite de visualiser les stations sur un graphique. Elle sert à la représentation des résultats ; le clustering utilise les 16 caractéristiques standardisées.

### 5. Analyse géographique

Les groupes sont placés sur une carte à partir des coordonnées des stations.

Cette étape comprend :

- La création de points géographiques avec GeoPandas.
- La cartographie des groupes et l’analyse de leur répartition par région.
- L’ajout de données CORINE Land Cover sur l’occupation du sol.
- Une jointure spatiale entre chaque station et le polygone CORINE correspondant.
- Des tableaux croisés pour comparer les groupes et les catégories d’occupation du sol.

## Compétences mises en pratique

| Compétence | Application dans le projet |
|---|---|
| **Nettoyage de données avec pandas** | Conversion des types, filtrage, traitement des doublons et des valeurs manquantes. |
| **Jointures entre plusieurs tables** | Association des analyses, prélèvements et stations à partir d’identifiants et de dates. |
| **Lecture de fichiers volumineux** | Chargement par blocs et sélection des données utiles. |
| **Analyse exploratoire** | Statistiques descriptives, distributions et corrélations. |
| **Construction de variables** | Transformation des mesures en profils annuels par station. |
| **Préparation des données pour le ML** | Imputation par la médiane et standardisation. |
| **Apprentissage non supervisé** | Application de K-means pour identifier des groupes de stations similaires. |
| **Évaluation des regroupements** | Comparaison des scores de silhouette pour plusieurs configurations. |
| **Visualisation de données** | Histogrammes, matrices de corrélation, profils de groupes et projection ACP. |
| **Analyse géographique** | Cartographie et croisement spatial avec l’occupation du sol. |
| **Organisation d’une analyse** | Structuration du notebook en étapes et sauvegarde des tables intermédiaires en Parquet. |

Ces compétences correspondent aux méthodes utilisées dans le notebook. Elles décrivent la pratique réalisée dans ce projet, sans revendiquer une expertise générale sur chaque outil.

## Outils utilisés

- **Python** : programmation des traitements dans le notebook.
- **pandas et NumPy** : manipulation et calculs sur les données.
- **scikit-learn** : préparation des variables, K-means, silhouette et ACP.
- **Matplotlib et Seaborn** : graphiques.
- **GeoPandas et Shapely** : traitement géographique.
- **PyArrow et Zstandard** : fichiers Parquet et données compressées.
- **Jupyter / Google Colab** : exécution du notebook.

## Principaux résultats

Les chiffres ci-dessous proviennent des sorties enregistrées dans `projet_qualite_eau_2022.ipynb`. Les résultats de préparation des données et de clustering (nombre de mesures et de stations retenues, scores de silhouette, tailles des groupes) ont été indépendamment reproduits à l’identique dans un environnement neuf, à partir des seuls fichiers versionnés dans ce dépôt (voir `requirements.txt`) — la partie géographique liée à CORINE, dont les couches ne sont pas versionnées ici (voir plus bas), n’a pas été revérifiée de cette façon. Les scores de silhouette sont arrondis à trois décimales.

| Nombre de groupes | Silhouette — moyennes | Silhouette — percentiles |
|---|---:|---:|
| **4** | **0,205** | **0,256** |
| 5 | 0,143 | 0,135 |
| 6 | 0,144 | 0,140 |

La configuration à **4 groupes** obtient le meilleur score parmi toutes les configurations testées, pour les deux représentations. Les scores restent modestes : ils indiquent une séparation limitée des groupes et ne mesurent pas une exactitude de classification.

Les groupes restent toutefois déséquilibrés. Avec les percentiles, ils contiennent respectivement **823, 2 479, 14 et 5 stations**. Les petits groupes nécessitent un examen complémentaire pour comprendre ce qui distingue ces stations.

La jointure spatiale associe une catégorie CORINE à 3 320 stations ; une station reste sans correspondance. Le groupe P2 reste dominant dans les catégories d’occupation du sol bien représentées. Ce constat descriptif, influencé par son effectif élevé, ne suffit pas à établir un lien causal entre occupation du sol et qualité de l’eau.

## Exécuter le projet

### Fichier principal

`projet_qualite_eau_2022.ipynb`

### Données nécessaires

Le dossier `data/`, situé à côté du notebook, contient les trois tables obligatoires :

- `stations.csv.zst` : caractéristiques et coordonnées des stations ;
- `operations_2022.csv.zst` : prélèvements ;
- `analyses_2022.csv.zst` : résultats des analyses.

Le notebook lit ces fichiers compressés directement, avec un séparateur `;`. Il accepte aussi les fichiers `.csv` de même nom, prioritaires s’ils sont présents. Les PDF `data/DescriptionDonneesPC.pdf` et `data/grilles-seq-eau-v2.pdf` sont des documents complémentaires, non nécessaires à l’exécution.

### Archive complète

Une archive `analyse_qualite_eau_2022.zip`, réunissant le notebook, une copie du README et un dossier `data/` complet (les trois tables, les deux PDF et les archives géographiques `CLC12_FR_RGF_SHP.zip` et `CoursEau_FXX-shp.zip`), a été utilisée pour l’exécution sur Google Colab. Elle n’est pas incluse dans ce dépôt Git : à plus de 800 Mo, elle dépasse largement la limite de 100 Mo par fichier imposée par GitHub, et duplique du contenu déjà présent dans `data/`.

Cette archive reste utile pour l’exécution sur Colab (voir ci-dessous) ; en local, le notebook et `data/` tels que fournis dans ce dépôt suffisent pour l’analyse principale (sections 1 à 10.3).

### Sur Google Colab

1. Importer `projet_qualite_eau_2022.ipynb` dans Colab.
2. Placer `analyse_qualite_eau_2022.zip` dans `/content`, `MyDrive` ou `MyDrive/Envir`. Cette archive n’est pas versionnée dans ce dépôt (voir la section « Archive complète » ci-dessus) ; à défaut, on peut recréer une archive équivalente en zippant soi-même le dossier `data/` de ce dépôt.
3. Exécuter la cellule d’installation des dépendances puis, si nécessaire, celle de montage de Google Drive.
4. Exécuter la cellule d’extraction : elle extrait le projet dans `/content/projet_eau_extrait`, détecte le dossier contenant les trois tables dans `data/` et en fait le répertoire de travail.
5. Continuer les cellules dans l’ordre. Avant la section 10.4 du notebook, préparer les couches géographiques selon les instructions ci-dessous pour poursuivre l’analyse CORINE.

La détection attend un seul dossier `data/` contenant les trois tables. Utiliser un dossier d’extraction vide lors d’un changement d’archive pour éviter de mélanger plusieurs versions.

### En local

1. Disposer de Python et ouvrir un terminal dans le dossier contenant directement `projet_qualite_eau_2022.ipynb` et `data/`. Les sorties conservées proviennent d’un environnement Python 3.12 sur Colab. Les sections 1 à 4 (préparation des données et clustering K-means/ACP) ont été revérifiées en local avec `requirements.txt` dans un environnement séparé ; les sections géographiques (10.4 et suivantes, qui nécessitent CORINE) n’ont pas été revérifiées de cette façon.
2. Créer et activer un environnement dédié. Sous Linux ou macOS :

   ```bash
   python3 -m venv .venv
   source .venv/bin/activate
   python -m pip install -r requirements.txt
   ```

   Ce fichier liste les paquets nécessaires sans version figée (le projet a été exécuté sur Google Colab, sans environnement local à figer par `pip freeze`).

3. Depuis ce même dossier, lancer JupyterLab :

   ```bash
   python -m jupyterlab
   ```

4. Ouvrir le notebook et exécuter les cellules dans l’ordre, en ignorant celles de montage de Google Drive et d’extraction vers `/content`. L’installation dans le notebook peut être ignorée si les dépendances sont déjà installées.
5. Avant la section 10.4, préparer les données géographiques ci-dessous si l’analyse CORINE est souhaitée.

Les graphiques s’affichent dans le notebook. Le programme crée `cache/` dans le répertoire de travail et y enregistre les tables nettoyées, les agrégations et les affectations aux groupes, notamment `analysis_tp1_filtered.parquet`, `tp2_mean_matrix.parquet`, `tp2_pct_matrix.parquet`, `tp2_mean_clusters.parquet` et `tp2_pct_clusters.parquet`. En cas d’échec de l’écriture Parquet, la fonction de sauvegarde utilise le format `.pkl`.

### Données géographiques complémentaires

Les cartes de points et les tableaux par région fonctionnent sans fond externe. CORINE est nécessaire pour les sections 10.5 à 10.8 ; le réseau hydrographique BD Carthage reste optionnel.

Les archives `CLC12_FR_RGF_SHP.zip` (643 Mo) et `CoursEau_FXX-shp.zip` (56 Mo) ne sont pas versionnées dans ce dépôt Git (voir `.gitignore`) : la première dépasse la limite de 100 Mo par fichier de GitHub, et les deux alourdiraient inutilement le dépôt pour une couche optionnelle. Pour reproduire cette partie de l’analyse, se procurer ces couches (CORINE Land Cover 2012 et BD Carthage, données géographiques françaises en accès libre) et les placer dans `data/` sous ces mêmes noms avant l’étape suivante.

1. Depuis le répertoire du projet, extraire les archives géographiques. Ces commandes peuvent aussi être exécutées dans une cellule du notebook, après la configuration des chemins :

   ```python
   from zipfile import ZipFile

   with ZipFile("data/CLC12_FR_RGF_SHP.zip") as archive:
       archive.extractall("data")

   # Facultatif : fond représentant le réseau hydrographique.
   with ZipFile("data/CoursEau_FXX-shp.zip") as archive:
       archive.extractall("data/CoursEau_FXX-shp")
   ```

2. Dans la cellule de la section 10.4, remplacer les affectations initiales à `None` par les chemins suivants. Conserver `TP3_BD_CARTHAGE_PATH = None` si ce fond n’a pas été extrait :

   ```python
   TP3_CLC_PATH = "data/CLC12_FR_RGF_SHP/CLC12_FR_RGF.shp"
   TP3_BD_CARTHAGE_PATH = "data/CoursEau_FXX-shp/CoursEau_FXX.shp"
   ```

3. Exécuter cette cellule puis les suivantes. Conserver les fichiers associés aux shapefiles (`.dbf`, `.shx`, `.prj`) dans leurs dossiers respectifs. Sans couche CORINE extraite, s’arrêter avant la section 10.5, qui exige cette ressource.

## Limites et améliorations possibles

- **Période limitée** : une seule année de données, avec une fréquence de mesure variable selon les stations.
- **Groupes déséquilibrés** : certaines petites classes peuvent correspondre à des stations atypiques ou à des valeurs extrêmes à vérifier.
- **Interprétation des clusters** : les groupes obtenus ne sont pas des classes officielles de qualité de l’eau et ne permettent pas de conclure sur sa qualité.
- **Données manquantes et choix de préparation** : l’imputation par la médiane et le seuil de 10 paramètres peuvent influencer les regroupements ; leur sensibilité n’est pas évaluée.
- **Contexte spatial local** : la jointure CORINE décrit le polygone contenant la station, pas l’occupation du sol de son bassin versant.
- **Décalage temporel** : les données CORINE utilisées correspondent à 2012, contre 2022 pour les analyses d’eau.

Des prolongements possibles seraient de comparer plusieurs années, d’évaluer la stabilité des groupes selon les choix de préparation et de croiser les profils avec l’occupation du sol à l’échelle des bassins versants.

## Contributeurs

Projet réalisé en binôme dans le cadre du M1, avec **Mohamed-Amine Ben-Kahla** :

- **Mohamed-Amine Ben-Kahla** et **Yasmina Farid** (moi) : les deux membres du binôme ont travaillé ensemble sur l’ensemble des étapes (préparation des données, exploration, clustering, analyse géographique), sans répartition formelle des tâches entre nous.

# Analyse de données génomiques de cancer du sein — jeu METABRIC

Projet universitaire en M1 (Data Science) : réduction de dimension, apprentissage non supervisé et modèle linéaire en R, sur un jeu de données d'expression génique.

## Objectif

Ce TP noté consistait à analyser des données de cancer du sein à partir de l'expression de gènes, puis à regrouper les patientes et prédire un indicateur médical de gravité.

Le jeu de données **METABRIC** (Molecular Taxonomy of Breast Cancer International Consortium) contient, pour chaque patiente :

- des variables médicales (statut hormonal, sous-type moléculaire, stade, etc.) ;
- des données d'expression de centaines de gènes ;
- des données de mutations (non utilisées dans ce projet).

## Travail réalisé

### 1. Réduction du nombre de gènes

Les données de mutation sont supprimées. Parmi les gènes restants, les **100 gènes les plus corrélés** (corrélation au carré) à l'**indice de Nottingham** — un score de gravité du cancer utilisé pour orienter la décision d'opérer — sont sélectionnés. Les lignes contenant des valeurs manquantes sont ensuite retirées (`na.omit`).

### 2. Analyse en composantes principales (ACP)

Une ACP normée (centrée-réduite) est réalisée sur les 100 gènes retenus, avec `FactoMineR::PCA`. Les 30 variables médicales sont ajoutées comme variables supplémentaires (quantitatives pour 6 d'entre elles, qualitatives pour les 24 autres), pour aider à l'interprétation sans influencer le calcul des axes.

Le nombre de composantes à garder est discuté à partir de l'éboulis des valeurs propres (`fviz_eig`).

### 3. Regroupement des patientes avec K-means

Le clustering K-means est appliqué sur les patientes projetées sur les premières composantes de l'ACP — un apprentissage non supervisé, sans catégorie connue à l'avance. Le code fait varier le nombre de composantes et de clusters, et trace la somme des carrés intra-classes (WSS) pour appliquer un critère du coude.

### 4. Interprétation des clusters

Les groupes obtenus sont comparés aux variables médicales qualitatives à l'aide de **tests du χ² (chi-deux)** d'indépendance, pour identifier les variables les plus associées à la classification. Une table de contingence est produite pour la variable la plus significative.

### 5. Prédiction de l'indice de Nottingham

Un modèle de **régression linéaire** (`lm`) prédit l'indice de Nottingham à partir des composantes de l'ACP retenues.

## Compétences mises en pratique

| Compétence | Application dans le projet |
|---|---|
| **Sélection de variables** | Réduction à 100 gènes par corrélation avec la variable cible. |
| **Réduction de dimension** | ACP normée avec variables supplémentaires (FactoMineR). |
| **Apprentissage non supervisé** | Clustering K-means sur les composantes principales. |
| **Choix de paramètres** | Critère du coude pour le nombre de composantes et de clusters. |
| **Tests statistiques** | Tests du χ² d'indépendance entre clusters et variables qualitatives. |
| **Visualisation** | Éboulis des valeurs propres, projection des clusters sur le plan principal. |
| **Modélisation prédictive** | Régression linéaire et lecture d'un résumé de modèle (R², significativité). |

Ces compétences correspondent aux méthodes utilisées dans le fichier R Markdown. Elles décrivent la pratique réalisée dans ce projet, sans revendiquer une expertise générale sur chaque outil.

## Outils utilisés

- **R** : langage du projet.
- **tidyverse** : lecture et mise en forme des données (`read.csv2`, `type_convert`).
- **FactoMineR** : ACP avec variables supplémentaires.
- **factoextra** : visualisation de l'éboulis et des clusters.
- **R Markdown / knitr** : exécution reproductible et génération du rendu HTML/PDF.

## Principaux résultats

Les résultats ci-dessous ont été **reproduits indépendamment** à partir du fichier `METABRIC_RNA_Mutation.csv`, dans un environnement R neuf (R 4.3, packages `tidyverse`, `FactoMineR`, `factoextra` installés depuis zéro), en exécutant le code du `.Rmd` tel quel.

- Après sélection des gènes et suppression des valeurs manquantes : **1092 patientes, 130 colonnes** (30 variables médicales + 100 gènes) — identique à la sortie d'origine.
- Inertie cumulée de l'ACP aux 10 premières composantes (celles retenues pour le clustering) : **50,5 %**. Le texte du code mentionne la convention générale des ~70–80 % d'inertie cumulée, mais retient `ncp = 10` sur la base du critère du coude (rendements décroissants par composante, visibles sur l'éboulis) — un critère différent, légitime pour ce type de données à forte dimension (100 gènes faiblement corrélés entre eux).
- Nombre de clusters K-means retenu : **k = 3**, présenté dans le code comme un choix d'exemple (« supposons que l'on trouve un coude autour de k = 3 ou k = 4 ») plutôt que comme le résultat d'un critère du coude appliqué formellement. Tailles des 3 clusters obtenus : **168, 537 et 387 patientes**.
- Variable médicale la plus liée aux clusters (test du χ²) : le **sous-type par classificateur à 3 gènes** (`3-gene_classifier_subtype`), très significatif (p ≈ 2×10⁻²⁰²) — identique à la variable trouvée dans le rendu d'origine. Table de contingence obtenue (les numéros de cluster sont arbitraires et peuvent différer d'une exécution à l'autre, K-means n'étant pas initialisé avec une graine fixe dans le code d'origine) :

  | | Cluster A | Cluster B | Cluster C |
  |---|---:|---:|---:|
  | ER−/HER2− | 142 | 9 | 34 |
  | ER+/HER2− High Prolif | 4 | 168 | 214 |
  | ER+/HER2− Low Prolif | 0 | 347 | 53 |
  | HER2+ | 22 | 13 | 86 |

- Régression linéaire de l'indice de Nottingham sur les 10 composantes : **R² = 0,2163** (R² ajusté = 0,209), F = 29,83 sur 10 et 1081 degrés de liberté, p < 2,2e-16 — coefficients identiques à la décimale près à ceux du rendu d'origine, ce qui est attendu car l'ACP (contrairement au clustering) ne dépend pas d'un tirage aléatoire. Le modèle est statistiquement significatif dans son ensemble, mais explique moins d'un quart de la variance de l'indice. 4 des 10 composantes (Dim1, Dim2, Dim5, Dim7, Dim8) sont individuellement significatives à 5 %.

Un R² de 0,2163 est modeste : les composantes retenues ne suffisent pas à bien prédire la gravité du cancer à elles seules.

## Exécuter le projet

### Fichier principal

`TPN.Rmd`

### Correction apportée

Le fichier tel que rendu par R Markdown à l'origine commençait par un bloc de code ```` ```r ... ``` ```` englobant l'en-tête YAML (titre, auteur, date, format de sortie). Ce bloc empêche R Markdown de reconnaître l'en-tête et bloque le knit. Il a été retiré dans cette copie ; le reste du contenu est inchangé.

### Données nécessaires

Le fichier `METABRIC_RNA_Mutation.csv` (environ 8 Mo) provient du jeu Kaggle *Breast Cancer Gene Expression Profiles (METABRIC)* :
<https://www.kaggle.com/datasets/raghadalharbi/breast-cancer-gene-expression-profiles-metabric>

Il n'est **pas inclus dans ce dépôt Git** (voir `.gitignore`) : le télécharger depuis Kaggle et le placer dans `data/METABRIC_RNA_Mutation.csv`, à côté du `.Rmd`.

### En local (RStudio ou R en ligne de commande)

1. Installer R (version 4.x) et les packages nécessaires :

   ```r
   install.packages(c("tidyverse", "FactoMineR", "factoextra", "rmarkdown"))
   ```

   Sous Ubuntu/Debian, une alternative plus rapide passe par les paquets système :

   ```bash
   sudo apt install r-cran-tidyverse r-cran-factominer r-cran-factoextra r-cran-rmarkdown
   ```

2. Placer `METABRIC_RNA_Mutation.csv` dans `data/` (voir ci-dessus).
3. Depuis le dossier du projet, produire le rendu :

   ```r
   rmarkdown::render("TPN.Rmd")
   ```

   ou ouvrir `TPN.Rmd` dans RStudio et cliquer sur *Knit*.

Les graphiques (éboulis, projection des clusters) s'affichent dans le document généré (HTML ou PDF selon le format choisi).

## Limites et améliorations possibles

- **Choix de `ncp` fondé sur un exemple, pas sur un calcul explicite** : le code retient `ncp = 10` « d'après l'éboulis, ou autre », sans indiquer le point exact du coude ni de seuil numérique (l'inertie cumulée à 10 composantes est de 50,5 %, ce qui reste raisonnable pour 100 variables génétiques faiblement corrélées, mais mériterait d'être justifié plus précisément).
- **`k` choisi sans critère numérique explicite** : le code affiche la courbe du coude (WSS en fonction de k) mais fixe `k_final = 3` par une phrase (« supposons que... »), sans seuil ou méthode (silhouette, gap statistic) pour trancher entre 3 et 4.
- **Absence de graine aléatoire (`set.seed`)** : les numéros de cluster, et dans une moindre mesure leur composition exacte, peuvent varier d'une exécution à l'autre. Les résultats restés stables d'une exécution à l'autre dans nos tests (tailles de clusters proches, même variable la plus discriminante) : la conclusion qualitative tient, mais la reproductibilité stricte demanderait `set.seed()` avant les appels à `kmeans`.
- **Pouvoir prédictif limité** : R² = 0,2163 pour la régression linéaire. D'autres modèles (régression pénalisée, forêts aléatoires) ou d'autres variables explicatives pourraient être testés.
- **Interprétation des clusters restreinte à une variable** : seule la variable la plus significative au test du χ² est détaillée dans le rendu ; les suivantes ne sont pas explorées plus loin.
- **Données de mutation écartées** : elles pourraient apporter de l'information complémentaire, non exploitée ici.

## Contributeurs

Projet réalisé individuellement par **Yasmina Farid**, dans le cadre du M1.

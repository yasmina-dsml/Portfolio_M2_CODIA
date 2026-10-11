# Classification de cultures agricoles par régression logistique et ingénierie de caractéristiques

**Projet de Science des Données — Master 1 SDSC, Université de Strasbourg**

Ce dépôt correspond au jeu de données d'un projet de groupe (3 étudiants) portant sur la
classification du type de culture agricole d'une parcelle à partir de séries temporelles
d'images satellites Sentinel-2. Le groupe a exploré plusieurs approches (CNN, SVM, forêt
aléatoire) dans un dépôt séparé ; **ce dépôt-ci ne contient que ma contribution
individuelle** : l'exploration des données, le prétraitement, l'ingénierie de
caractéristiques et un modèle de régression logistique multiclasse, développés et
documentés indépendamment du reste du groupe.

## Objectif

Classifier le type de culture agricole d'une parcelle (20 classes : blé tendre d'hiver,
orge de printemps, colza d'hiver, maïs, luzerne, tournesol, etc.) à partir de sa série
temporelle d'images Sentinel-2, en abordant le problème par des **méthodes statistiques
classiques** plutôt que par apprentissage profond : l'enjeu est de voir jusqu'où une
ingénierie de caractéristiques soignée permet à un modèle linéaire de rester compétitif
face à un déséquilibre de classes marqué.

## Travail réalisé

1. **Exploration des données** (`01_exploration_donnees.ipynb`) — chargement des images
   et des étiquettes, statistiques descriptives du jeu de données, visualisation de la
   distribution des classes en train et en test, aperçu visuel d'une séquence
   d'images mensuelles.
2. **Prétraitement et rééchantillonnage** (`02_pretraitement_reechantillonnage.ipynb`) —
   exploration de trois stratégies pour compenser le déséquilibre des classes :
   sur-échantillonnage aléatoire, sous-échantillonnage aléatoire, et une méthode
   combinée (SMOTEENN). Ces stratégies sont documentées à titre exploratoire ; le
   modèle final gère le déséquilibre autrement (voir ci-dessous).
3. **Ingénierie de caractéristiques et modélisation** (`03_regression_logistique.ipynb`) —
   plutôt que de donner les pixels bruts au modèle, chaque parcelle est résumée par des
   statistiques calculées par canal et par mois (moyenne, écart-type, min, max),
   enrichies d'un indice de végétation de type **NDVI** — `(B08 − B04) / (B08 + B04)` —
   qui capture l'activité chlorophyllienne et donc le cycle de croissance de la culture.
   Le modèle est un pipeline scikit-learn : standardisation → sélection de variables
   (`SelectKBest`, ANOVA F) → régression logistique avec `class_weight='balanced'` pour
   gérer le déséquilibre directement au niveau du classifieur (plutôt que par
   rééchantillonnage des données). Les hyperparamètres (`C`, nombre de variables
   sélectionnées) sont optimisés par `GridSearchCV` avec validation croisée
   **stratifiée** à 5 plis, sur le score F1 macro.

## Compétences mises en pratique

| Compétence | Application dans ce projet |
|---|---|
| Ingénierie de caractéristiques sur séries temporelles | Extraction de statistiques par canal/mois et calcul d'un indice NDVI à partir d'images satellites multi-spectrales |
| Gestion du déséquilibre de classes | Comparaison de stratégies de rééchantillonnage (over/undersampling, SMOTEENN) et choix argumenté d'une pondération de classe dans le classifieur |
| Construction de pipelines scikit-learn | Chaînage standardisation → sélection de variables → modèle, avec recherche d'hyperparamètres par validation croisée stratifiée |
| Évaluation de modèles multiclasses déséquilibrés | Balanced accuracy, F1 macro, matrice de confusion, analyse de l'écart entre performance en validation croisée et sur le jeu de test |

## Outils utilisés

Python · scikit-learn · imbalanced-learn · NumPy · pandas · matplotlib · tqdm

## Principaux résultats

Résultats obtenus lors de la dernière exécution documentée par les sorties sauvegardées
du notebook (recherche d'hyperparamètres sur `C ∈ {0.01, 0.1, 1, 10, 100}` et
`k ∈ {30, 50, 70, toutes}` variables) :

- Meilleurs hyperparamètres : `C = 10`, `k = 70` variables sélectionnées.
- Validation croisée (5 plis stratifiés, sur le jeu d'entraînement) : balanced accuracy
  **0,689**, F1 macro **0,608**.
- Jeu de test : balanced accuracy **0,523**, F1 macro **0,478**.

**Non revérifié de façon indépendante** — ces chiffres proviennent des sorties déjà
enregistrées dans le notebook d'origine. Je n'ai pas pu réexécuter l'entraînement pour
les revérifier : les images sources (`train_images.npy`, `test_public_images.npy`) ne
sont pas incluses dans ce dépôt (voir `data/NOTE_DONNEES.md`) et n'étaient plus
disponibles localement au moment de la préparation de ce dépôt. Voir la section
Limites ci-dessous concernant l'écart entre performance en validation croisée et sur le
jeu de test.

## Exécuter le projet

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

Fichier principal : `03_regression_logistique.ipynb` (dépend de
`01_exploration_donnees.ipynb` et `02_pretraitement_reechantillonnage.ipynb` pour le
contexte, mais chaque notebook se charge indépendamment).

**Données** — les fichiers d'images (`train_images.npy`, `test_public_images.npy`)
dépassent la limite de 100 Mo de GitHub et ne sont pas inclus dans ce dépôt ; seules les
étiquettes (`train_labels.npy`, `test_public_labels.npy`) sont fournies. Sans les
images, `01_exploration_donnees.ipynb` reste exécutable (statistiques et distribution
des classes), mais `02_pretraitement_reechantillonnage.ipynb` et
`03_regression_logistique.ipynb` s'arrêteront au chargement des images. Voir
`data/NOTE_DONNEES.md` pour plus de détails.

Exécuté en local, sans GPU dédié (le modèle est un pipeline scikit-learn classique,
pas un réseau de neurones).

## Limites et améliorations possibles

- **Écart important entre validation croisée et test** (balanced accuracy 0,689 en CV
  contre 0,523 sur le test) : les hyperparamètres ont potentiellement été surajustés
  aux plis de validation croisée, ou la distribution du jeu de test diffère de celle du
  jeu d'entraînement sur certaines classes minoritaires. Cet écart n'a pas été
  investigué plus en détail.
- Les stratégies de rééchantillonnage explorées dans
  `02_pretraitement_reechantillonnage.ipynb` n'ont pas été comparées quantitativement
  à la pondération de classe utilisée dans le modèle final (pas de mesure de
  performance avec données rééchantillonnées) — leur intérêt réel pour ce jeu de
  données reste donc à vérifier.
- L'extraction de caractéristiques suppose que le canal 0 correspond à la bande
  proche infrarouge (NIR) et le canal 1 à la bande rouge (RED) pour le calcul du NDVI ;
  cette hypothèse n'a pas été revérifiée indépendamment de la documentation d'origine
  du jeu de données.
- Pas de comparaison directe avec les approches du groupe (CNN, SVM, forêt aléatoire)
  dans ce dépôt : ces modèles vivent dans un dépôt séparé, propriété du groupe.

## Contributeurs

Projet de groupe (3 étudiants) pour le jeu de données et le sujet ; ce dépôt contient
uniquement le travail individuel de Yasmina Farid (exploration des données,
prétraitement, ingénierie de caractéristiques, modèle de régression logistique). Les
contributions des autres membres du groupe (modèles CNN, SVM, forêt aléatoire) ne sont
pas incluses ici.

## Provenance

Dépôt d'origine : `git.unistra.fr/yfarid/projet-ds_l`.

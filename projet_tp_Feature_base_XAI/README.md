# Explications feature-based avec LIME et SHAP — BankChurners

**TP (labwork) — Master 1, cours d'explicabilité (XAI)**

TP individuel : génération d'explications *feature-based* (LIME et SHAP) pour un
modèle de forêt aléatoire entraîné sur des données tabulaires (jeu de données
[Credit Card Customers](https://www.kaggle.com/datasets/sakshigoyal7/credit-card-customers/data),
prédiction du départ de clients bancaires). L'énoncé fourni par les enseignants est un
notebook à compléter (cellules « à faire ») ; ce dépôt contient ma version complétée,
avec le code, les réponses aux questions posées, et les résultats obtenus.

## Objectif

- Générer des explications locales avec LIME et SHAP pour un modèle de classification
  binaire (client qui reste vs. client qui part).
- Comparer empiriquement la stabilité de LIME à celle des valeurs de Shapley.
- Générer une explication globale et s'interroger sur sa portée par rapport aux
  explications locales.
- Discuter la qualité des données et son impact sur le modèle et les explications
  produites.

## Travail réalisé

1. **Exploration des données** — distribution de la variable cible (déséquilibre
   ~84 %/16 %), des variables numériques et catégorielles.
2. **Préparation des données** — séparation attributs/cible, encodage ordinal des 5
   attributs catégoriels (`cat.codes`, pas de one-hot — justifié dans le notebook) avec
   construction des méta-données attendues par LIME (`categorical_features`,
   `categorical_names`), split train/test stratifié (graine fixée).
3. **Modélisation** — forêt aléatoire (`n_estimators=10`, `max_depth=6`, graine fixée),
   évaluée par exactitude, rapport de classification et matrice de confusion.
4. **Explications LIME** — génération d'explications locales pour une instance du jeu
   de test avec différentes configurations (`num_features`, `num_samples`), et
   vérification empirique de l'instabilité de LIME en répétant la même configuration
   plusieurs fois sans fixer la graine du générateur aléatoire.
5. **Explications SHAP** — `TreeExplainer`, histogramme et diagramme en cascade pour
   la même instance, puis explication globale agrégée sur tout le jeu de test.
6. **Réponses rédigées** à l'ensemble des questions du TP (comparaison LIME/SHAP,
   portée locale/globale, sous-détermination, limites méthodologiques et éthiques).

Un bug a été corrigé dans une cellule fournie par l'énoncé (voir Limites).

## Compétences mises en pratique

| Compétence | Application dans ce projet |
|---|---|
| Explicabilité locale (LIME) | Génération d'explications pour une instance, avec analyse de la variabilité selon `num_features`/`num_samples` |
| Explicabilité par valeurs de Shapley (SHAP) | `TreeExplainer`, histogramme, diagramme en cascade, agrégation en explication globale |
| Préparation de données tabulaires mixtes | Encodage ordinal justifié (plutôt que one-hot) pour un modèle à base d'arbres, construction de méta-données pour LIME |
| Évaluation de modèle déséquilibré | Lecture critique de l'exactitude globale (0,92) au regard du rappel très inégal entre classes (0,99 vs 0,59) |
| Esprit critique sur l'explicabilité | Distinction entre corrélation et cause (les variables les plus « explicatives » sont en grande partie des symptômes du churn, pas ses causes) |

## Outils utilisés

Python · scikit-learn · pandas · NumPy · matplotlib · seaborn · **LIME** · **SHAP**

## Principaux résultats

- Forêt aléatoire (10 arbres, profondeur 6) : **exactitude 0,9235** sur le jeu de test.
  Rappel très inégal entre classes : **0,99** sur *Existing Customer* (majoritaire) contre
  **0,59** sur *Attrited Customer* (minoritaire, ~16 % des données) — attendu compte tenu
  du déséquilibre.
- **LIME est instable** : sur 3 générations successives de la même explication (même
  instance, mêmes `num_features`/`num_samples`, graine aléatoire non fixée), le poids de
  l'attribut le plus important varie de +0,114 à +0,136, et des attributs de rang
  inférieur apparaissent/disparaissent d'une exécution à l'autre.
- **SHAP est déterministe** (`TreeExplainer` calcule les valeurs exactement à partir de
  la structure des arbres) : mêmes résultats à chaque exécution.
- Explication globale (SHAP, moyenne sur le jeu de test) : les 5 attributs les plus
  influents sont tous des mesures d'activité transactionnelle (`Total_Trans_Ct`,
  `Total_Revolving_Bal`, `Total_Trans_Amt`, `Total_Ct_Chng_Q4_Q1`,
  `Total_Relationship_Count`) ; les attributs démographiques (genre, âge, éducation,
  statut marital, revenu, type de carte) contribuent très peu.
- Ces résultats sont **directement reproductibles** : voir section Reproductibilité.

## Exécuter le projet

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
jupyter notebook Feature_based_XAI_BankChurners.ipynb
```

Fichier principal : `Feature_based_XAI_BankChurners.ipynb`. Données : `BankChurners.csv`
(incluses dans ce dépôt, ~1,1 Mo). Exécuté en local, sans GPU (forêt aléatoire de petite
taille, pas de réseau de neurones).

## Limites et améliorations possibles

- **Bug corrigé dans l'énoncé fourni** : la cellule d'exploration des variables
  catégorielles utilisait `df.select_dtypes(include=["object"])`, qui ne détecte plus
  aucune colonne avec les versions récentes de pandas (dtype `str` dédié plutôt que
  `object`) — remplacé par `select_dtypes(exclude=[np.number])`, sans changer le
  comportement recherché.
- Le rappel faible sur la classe minoritaire (0,59) n'a pas été amélioré volontairement
  (pas de pondération de classe ni de rééchantillonnage) : l'objectif du TP est
  l'explicabilité, pas l'optimisation du modèle — mais cela limite d'autant la
  pertinence des explications sur les cas mal classés de cette classe.
- Les explications identifient des corrélats de l'attrition (baisse d'activité
  transactionnelle) plutôt que des causes profondes : ce point est discuté dans le
  notebook (Partie 5) mais n'a pas pu être creusé faute de variables supplémentaires
  (satisfaction client, contexte de vie, etc.) dans le jeu de données.
- Un seul modèle (forêt aléatoire) et une seule instance de test ont été utilisés pour
  la comparaison LIME/SHAP ; les conclusions sur la stabilité relative des deux méthodes
  ne sont illustrées que sur ce cas, pas mesurées systématiquement sur tout le jeu de
  test.

## Contributeurs

Travail individuel (Yasmina Farid).

# Prédiction d'emoji à partir d'une phrase

Projet universitaire en M1 de traitement du langage naturel avec Python : comparaison d'une régression logistique, d'un RNN simple et d'un LSTM pour prédire l'emoji correspondant au sens d'une courte phrase en anglais.

Projet réalisé en groupe de trois pendant le M1. Ce dépôt met en avant la partie que j'ai menée : l'augmentation des données par back-traduction, les embeddings GloVe, l'implémentation et la comparaison des trois modèles, et les deux évaluations (validation croisée et test indépendant). Détail des contributions dans la section [Contributeurs](#contributeurs).

## En bref

| | |
|---|---|
| **Tâche** | Classification de phrases courtes en 5 classes (emojis) |
| **Données** | 274 phrases d'entraînement (dont 59 issues de back-traduction), 56 phrases de test |
| **Modèles** | Régression logistique (sac de mots), RNN simple et LSTM sur embeddings GloVe gelés |
| **Protocole** | Validation croisée stratifiée à 5 folds, puis test sur un fichier séparé ; graines fixées, opérations TensorFlow déterministes |
| **Résultat fiable** | Accuracy de 0,55 à 0,59 sur le test ; de 0,62 à 0,69 en validation croisée sans fuite (évaluation 4) |
| **Point fort** | Fuite d'information localisée et mesurée : le découpage des folds séparait des phrases de leurs paraphrases, ce qui gonflait la validation croisée de la régression logistique de 9 points |
| **Reproductibilité** | Les chiffres de ce README ont été reproduits à l'identique dans un environnement neuf ; `src/verification_fuite.py` rejoue l'évaluation 4 |

## Objectif

L'objectif est de comparer trois modèles de complexité croissante — une régression logistique, un RNN simple et un LSTM — pour choisir, parmi 5 emojis, celui qui correspond le mieux au sens d'une phrase.

Les 5 classes correspondent, d'après le contenu des phrases du jeu de données, à : **0 = ❤️ amour, 1 = ⚾ sport, 2 = 😄 joie, 3 = 😞 tristesse/colère, 4 = 🍴 nourriture**.

## Travail réalisé

### 1. Données et préparation

Le jeu de données est réparti sur plusieurs fichiers dans `data/` :

- `train_emoji.csv` : 132 phrases annotées (répartition par classe : 22, 19, 38, 36, 17).
- `train_emoji_enrichi.csv` : version enrichie manuellement à 215 phrases (44, 24, 57, 60, 30), sur-ensemble du fichier précédent.
- `test_emoji.csv` : 56 phrases (12, 5, 18, 15, 6), tenues à l'écart de l'entraînement et de l'augmentation, utilisées comme jeu de test indépendant.

Les phrases sont tokenisées avec le `Tokenizer` de Keras puis converties en séquences d'entiers, complétées (`pad_sequences`, padding en début de séquence) à la longueur maximale du corpus.

### 2. Augmentation des données par back-traduction

Le notebook `src/data_augmentation.ipynb` enrichit le jeu de données par back-traduction anglais → français → anglais, à l'aide des modèles Hugging Face `Helsinki-NLP/opus-mt-en-fr` et `opus-mt-fr-en`.

Le traitement comprend :

- L'application de la back-traduction uniquement aux classes minoritaires (0 amour, 1 sport, 4 nourriture) pour rééquilibrer partiellement les classes.
- Le filtrage manuel des phrases générées incohérentes ou dont le sens a changé (liste `invalid_augmented_sentences`).
- La déduplication par rapport aux phrases déjà présentes dans le jeu de données.

Le résultat est sauvegardé dans `train_emoji_enrichi_aug.csv` : **274 phrases** (72, 39, 57, 60, 46), utilisé ensuite comme jeu d'entraînement pour les trois modèles.

### 3. Représentation des mots avec des embeddings pré-entraînés

Le notebook `src/lstm.ipynb` charge des embeddings **GloVe 50 dimensions**, téléchargés automatiquement au premier lancement puis mis en cache localement (fichier `.npy`).

La couverture du vocabulaire du jeu de données par GloVe est de **96,26 % (360/374 mots)**. Ces embeddings sont gelés (non ré-entraînés) pour les modèles RNN et LSTM.

### 4. Modèles comparés

Trois modèles sont entraînés et comparés :

- **Régression logistique** (baseline non-DL) : représentation bag-of-words (`CountVectorizer`) + `LogisticRegression`.
- **RNN simple** : `Embedding` (GloVe, gelé) → `SimpleRNN(32)` → `Dense(5, softmax)`.
- **LSTM** : `Embedding` (GloVe, gelé) → `LSTM(32)` → `Dense(5, softmax)`.

Les modèles séquentiels sont entraînés sur 30 epochs, en batchs de 16, avec la perte `sparse_categorical_crossentropy`, l'optimiseur `adam`, sans mélange des données, et avec des graines aléatoires fixées (`seed=42`) pour la reproductibilité.

### 5. Évaluation

Chaque modèle est évalué de deux façons complémentaires dans `src/lstm.ipynb` :

- **Évaluation 1 — validation croisée stratifiée à 5 folds** (`StratifiedKFold`, dataset restant petit après augmentation) sur `train_emoji_enrichi_aug.csv`.
- **Évaluation 2 — jeu de test indépendant** : chaque modèle est ré-entraîné sur l'intégralité de `train_emoji_enrichi_aug.csv`, puis évalué une seule fois sur `test_emoji.csv`, un jeu de phrases jamais vu pendant l'entraînement ni l'augmentation.

L'accuracy est utilisée comme métrique dans les deux cas.

## Compétences mises en pratique

| Compétence | Application dans le projet |
| --- | --- |
| **Prétraitement de texte** | Tokenisation, construction du vocabulaire, padding des séquences. |
| **Augmentation de données par back-traduction** | Pipeline de traduction anglais-français-anglais avec des modèles Hugging Face, filtrage manuel des résultats incohérents. |
| **Utilisation d'embeddings pré-entraînés** | Chargement, mise en cache et alignement de GloVe avec le vocabulaire du tokenizer. |
| **Modèles de deep learning séquentiels** | Construction de RNN et LSTM avec Keras/TensorFlow. |
| **Comparaison à une baseline non-DL** | Régression logistique sur représentation bag-of-words. |
| **Validation croisée et jeu de test indépendant** | Évaluation stratifiée à 5 folds, complétée par une évaluation hold-out sur `test_emoji.csv`. |
| **Reproductibilité** | Fixation des graines aléatoires (NumPy, Python, TensorFlow) à chaque fold. |
| **Analyse comparative de modèles** | Interprétation des écarts de performance entre LR, RNN et LSTM. |

## Outils utilisés

- **Python** : programmation des traitements dans les notebooks.
- **pandas et NumPy** : manipulation des données.
- **scikit-learn** : `CountVectorizer`, `LogisticRegression`, `StratifiedKFold`.
- **TensorFlow / Keras** : `Tokenizer`, `Embedding`, `SimpleRNN`, `LSTM`, `Dense`.
- **Hugging Face Transformers** : modèles de traduction `Helsinki-NLP/opus-mt` pour la back-traduction.
- **GloVe** : embeddings de mots pré-entraînés (50 dimensions).
- **Jupyter** : exécution des notebooks.

## Principaux résultats

Les chiffres ci-dessous proviennent d'une exécution complète de `src/lstm.ipynb` faite pour cette rédaction (les deux évaluations ont été relancées dans un même run). Ils ont été reproduits à l'identique le 11/10/2026 dans un environnement neuf (CPU, TensorFlow 2.21, scikit-learn 1.8).

### Évaluation 1 — validation croisée (5 folds, sur les données d'entraînement)

| Modèle | Accuracy moyenne (5 folds) |
| --- | --- |
| **Régression logistique** | **0,7337** |
| LSTM | 0,6975 |
| RNN simple | 0,6464 |

La régression logistique obtient le meilleur score, malgré sa simplicité. Le LSTM fait mieux que le RNN simple grâce à ses mécanismes de portes, qui l'aident à mieux décider quelles informations garder ou oublier le long de la phrase, mais ne dépasse pas la baseline.

### Évaluation 2 — jeu de test indépendant (`test_emoji.csv`, 56 phrases)

| Modèle | Accuracy (test) |
| --- | --- |
| Régression logistique | 0,5893 |
| RNN simple | 0,5893 |
| LSTM | 0,5893 |

Les trois modèles obtiennent exactement la même accuracy sur ce jeu de test (33 phrases correctes sur 56), mais avec des prédictions différentes d'un modèle à l'autre : il ne s'agit pas d'un comportement identique, mais d'une coïncidence liée à la petite taille du jeu de test (56 exemples), où chaque phrase pèse environ 1,8 point d'accuracy.

Sur ce jeu de test indépendant, les trois modèles perdent entre 8 et 18 points par rapport à leur score en validation croisée. Cela suggère que l'estimation par validation croisée était optimiste, probablement parce que les phrases générées par back-traduction (évaluation 1) restent proches, en style et en vocabulaire, des phrases originales dont elles dérivent, alors que `test_emoji.csv` contient des formulations plus variées.

Ce résultat s'explique surtout par la taille encore limitée du jeu de données (274 phrases d'entraînement même après enrichissement et augmentation) : les modèles de deep learning ont plus de paramètres à apprendre et généralisent moins bien avec peu d'exemples, alors que la régression logistique reste relativement stable — même si, sur le test indépendant, elle rejoint finalement les deux autres modèles.

### Évaluation 3 — effet de la back-traduction

Pour mesurer ce qu'apporte l'augmentation, `src/lstm.ipynb` a été ré-exécuté à l'identique (mêmes modèles, mêmes graines, mêmes folds), en changeant une seule ligne : le fichier d'entraînement `train_emoji_enrichi_aug.csv` (274 phrases, avec back-traduction) est remplacé par `train_emoji_enrichi.csv` (215 phrases, sans back-traduction). La ré-exécution avec augmentation redonne exactement les chiffres des évaluations 1 et 2, ce qui confirme que la comparaison porte uniquement sur les données.

| Modèle | Test sans back-trad. | Test avec back-trad. | Gain (test) | CV sans | CV avec |
| --- | --- | --- | --- | --- | --- |
| Régression logistique | 0,5893 (33/56) | 0,5893 (33/56) | 0 | 0,6233 | 0,7337 |
| RNN simple | 0,5179 (29/56) | 0,5893 (33/56) | **+7,1 pts** | 0,5349 | 0,6464 |
| LSTM | 0,5536 (31/56) | 0,5893 (33/56) | **+3,6 pts** | 0,6326 | 0,6975 |

- **Sur le test indépendant**, la back-traduction aide les deux modèles séquentiels (+4 phrases correctes pour le RNN, +2 pour le LSTM) et ne change rien pour la régression logistique. Ces gains restent petits en valeur absolue (une phrase vaut environ 1,8 point) et proviennent d'une seule exécution : ils indiquent une tendance, pas un effet établi.
- **En validation croisée**, le gain apparent est bien plus grand (+6 à +11 points). L'écart entre validation croisée et test passe de 2 à 8 points sans augmentation à 6 à 14 points avec. Ce creusement vient de la fuite d'information localisée dans l'évaluation 4 : une partie du gain mesuré en validation croisée vient de paraphrases trop proches des phrases de validation, pas d'une meilleure généralisation.

### Évaluation 4 — où se situe la fuite d'information

L'écart entre validation croisée et test pouvait venir de quatre endroits : le prétraitement, l'augmentation, le découpage des folds ou le jeu de test lui-même. Chacun a été vérifié avec le code des modèles du notebook, inchangé (script `src/verification_fuite.py`).

**1. Découpage des folds : c'est là que se situe la fuite principale.** Le notebook applique la back-traduction avant la validation croisée, puis découpe les 274 phrases au hasard. Une phrase originale et sa paraphrase peuvent donc tomber l'une en entraînement et l'autre en validation. Le jeu contient aussi 5 doublons exacts. Au total, **106 phrases de validation sur 274 avaient un « jumeau » (source, paraphrase ou doublon) dans les données d'entraînement de leur fold.**

Pour corriger, chaque paraphrase a été rattachée à sa phrase source, par un alignement qui suit l'ordre de génération du notebook d'augmentation et la similarité des phrases, puis la validation croisée a été refaite en gardant chaque groupe dans un seul fold (`StratifiedGroupKFold`, 5 folds) :

| Modèle | CV du notebook | CV sans fuite | Écart |
| --- | --- | --- | --- |
| Régression logistique | 0,7337 | 0,6421 | **−9,2 pts** |
| RNN simple | 0,6464 | 0,6171 | −2,9 pts |
| LSTM | 0,6975 | **0,6861** | −1,1 pt |

La fuite profitait surtout à la régression logistique : un modèle par sac de mots reconnaît facilement une paraphrase qui reprend les mêmes mots. Sans fuite, **le classement s'inverse : le LSTM devient le meilleur modèle en validation croisée**, et la conclusion initiale (« le modèle simple est le meilleur ») ne tient plus.

**2. Jeu de test : contamination partielle.** 5 des 56 phrases de test figurent aussi, à l'identique, dans le jeu d'entraînement. Les trois modèles les classent toutes correctement. Sans ces 5 phrases, l'accuracy tombe à **0,549 (28/51) pour les trois modèles**, au lieu de 0,589. L'accuracy sur test présentée plus haut est donc légèrement optimiste.

**3. Prétraitement : effet négligeable.** Le `Tokenizer` Keras est ajusté sur les 274 phrases avant la validation croisée, ce qui donne au modèle les mots des phrases de validation. Comme les embeddings GloVe sont pré-entraînés et gelés, cela ne transmet aucune information sur les étiquettes. Pour la régression logistique, le `CountVectorizer` est dans le pipeline et n'est ajusté que sur les données d'entraînement de chaque fold.

**4. Ce qui reste.** Même sans fuite, la validation croisée reste au-dessus du test (de 0,62 à 0,69, contre 0,55 sur les phrases de test inédites). Le reste de l'écart vient probablement de la différence de style entre les phrases d'entraînement et celles du test, et de la petite taille du test (une phrase vaut environ 2 points).

## Exécuter le projet

### Fichiers principaux

- `src/data_augmentation.ipynb` : génère `data/train_emoji_enrichi_aug.csv` par back-traduction (optionnel à ré-exécuter, le fichier de sortie est déjà fourni dans `data/`).
- `src/lstm.ipynb` : charge les données augmentées, construit les embeddings GloVe et compare les trois modèles.
- `src/verification_fuite.py` : rejoue l'évaluation 4 (validation croisée sans fuite et test sur phrases inédites). À lancer depuis `src/` avec `python verification_fuite.py`, après une première exécution de `lstm.ipynb` ou avec accès à Internet pour télécharger GloVe.

### Données nécessaires

Le dossier `data/` contient les fichiers `train_emoji.csv`, `train_emoji_enrichi.csv`, `train_emoji_enrichi_aug.csv` (entraînement) et `test_emoji.csv` (test indépendant, évaluation 2).

Les fichiers GloVe (`glove.6B.zip`, `glove.6B.50d.txt`, `embedding_matrix_glove_50d.npy`) ne sont volontairement pas inclus dans ce dossier : ils sont volumineux (plus de 800 Mo pour l'archive complète) et entièrement régénérés automatiquement par `src/lstm.ipynb` au premier lancement (téléchargement, extraction, puis mise en cache). Aucune action manuelle n'est nécessaire au-delà d'exécuter le notebook.

### Installation

1. Créer et activer un environnement virtuel Python :

   ```bash
   python3 -m venv .venv
   source .venv/bin/activate
   ```

2. Installer les dépendances :

   ```bash
   pip install -r req.txt
   ```

   Ce fichier est une sortie `pip freeze` d'un environnement Linux avec GPU (CUDA 13, paquets `nvidia-*`). Sur une autre configuration (CPU seul, macOS, etc.), installer plutôt les paquets nécessaires directement : `pandas`, `numpy`, `scikit-learn`, `tensorflow`, `transformers`, `torch`, `jupyterlab`.

3. Lancer Jupyter et ouvrir `src/lstm.ipynb` (et, si besoin, `src/data_augmentation.ipynb` au préalable).

Au premier lancement de `lstm.ipynb`, l'archive GloVe 50d est téléchargée automatiquement (fichier volumineux) puis la matrice d'embedding est mise en cache en `.npy` pour les exécutions suivantes.

## Limites et améliorations possibles

- **Jeu de données très petit** : 274 phrases d'entraînement même après enrichissement et augmentation, et seulement 56 phrases de test, ce qui limite la capacité des modèles à généraliser et rend les scores sensibles au découpage des folds comme au contenu exact du jeu de test.
- **La validation croisée du notebook est gonflée par une fuite d'information** : le découpage des folds sépare des phrases de leurs paraphrases et de leurs doublons (106 phrases de validation sur 274 concernées). La fuite est localisée et mesurée dans l'évaluation 4 : sans elle, la régression logistique perd 9 points et le LSTM devient le meilleur modèle. Le notebook garde volontairement le protocole d'origine, pour que ses chiffres restent comparables à ceux du rendu ; le protocole corrigé est dans `src/verification_fuite.py`.
- **Le choix d'architecture semble donc moins déterminant que la quantité et la diversité des données** : sur test indépendant, aucun des trois modèles ne se distingue vraiment des deux autres, ce qui nuance la conclusion initiale ("un modèle simple généralise mieux avec peu de données") — elle est plutôt à lire comme "avec ce volume de données, aucune des trois architectures ne prend un avantage net face à des formulations nouvelles".
- **Le test lui-même reste petit et partiellement contaminé** (56 phrases, soit ~1,8 point d'accuracy par exemple, dont 5 déjà présentes dans l'entraînement) : l'égalité parfaite entre les trois scores est en partie due à cette granularité (les modèles font des erreurs différentes, mais en nombre égal) et ne doit pas être sur-interprétée comme une preuve d'équivalence stricte des modèles ; la baisse commune, elle, est un signal plus robuste car observée sur les trois architectures.
- **Filtrage manuel des back-traductions** : le tri des phrases générées incohérentes est subjectif et non automatisé.
- **Embeddings gelés** : GloVe n'est pas ré-entraîné (fine-tuné), ce qui peut limiter l'adaptation au vocabulaire spécifique du jeu de données.
- **Pas de recherche d'hyperparamètres** : une seule configuration testée (30 epochs, batch 16, 32 unités) pour le RNN et le LSTM.
- **Classes encore déséquilibrées** malgré l'enrichissement manuel et l'augmentation.

Des prolongements possibles seraient d'appliquer la back-traduction à l'intérieur de chaque fold d'entraînement seulement, de retirer du jeu d'entraînement les phrases déjà présentes dans le test, de collecter davantage de phrases de test indépendantes pour confirmer l'écart observé, d'essayer des embeddings plus grands ou fine-tunés, d'autres architectures (GRU, LSTM bidirectionnel, attention), et une recherche d'hyperparamètres.

## Contributeurs

Projet réalisé à trois dans le cadre du M1 :

- **Elio Ouedraogo** : mise en place initiale du dépôt et de sa structure, et développement en parallèle d'une approche par transformers destinée à être comparée aux autres modèles.
- **Emma Robillard** : nettoyage et organisation du jeu de données, et développement en parallèle d'une autre approche de modélisation.
- **Yasmina Farid** (moi) : augmentation des données par back-traduction, intégration des embeddings GloVe, implémentation et comparaison des trois modèles présentés ici (régression logistique, RNN, LSTM), conception des deux évaluations (validation croisée et test indépendant), et rédaction de ce README.

Elio et Emma n'ont pas poussé la version finale de leurs modèles respectifs sur le dépôt : ce README et ce notebook ne présentent donc que la comparaison LR / RNN / LSTM, pas une comparaison complète de toutes les approches explorées par le groupe.

Le dépôt original du projet est hébergé sur le GitLab de l'Unistra ; ce dépôt reprend la branche sur laquelle j'ai travaillé.

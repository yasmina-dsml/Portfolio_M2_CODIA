# Superposition et interférence des caractéristiques dans un CNN

TER (Travail d'Étude et de Recherche) réalisé dans le cadre du Master 1 Sciences de Données et Systèmes Complexes à l'Université de Strasbourg, sous la direction d'Anne Jeannin-Girardon (ICube). Le sujet porte sur l'étude empirique du phénomène de superposition des caractéristiques dans les réseaux de neurones convolutifs (CNN), et de son lien possible avec l'interférence entre caractéristiques apprises.

## En bref

| | |
|---|---|
| **Question** | Le design d'un bloc convolutif (classique, bottleneck, bottleneck inversé) change-t-il la façon dont un CNN superpose ses caractéristiques ? |
| **Protocole** | 3 CNN comparables, tous mesurés en sortie du même bloc (8 canaux) ; mêmes données (CIFAR-10, 4 classes, 20 000 images d'entraînement et 4 000 de test), même perte, même optimiseur, même nombre d'epochs |
| **Classification** | Top-1 de 67,4 % (bottleneck standard) et 69,3 % (bottleneck inversé) ; 69,6 % de validation en fin d'entraînement pour la baseline. Les trois modèles apprennent la tâche à un niveau comparable, condition nécessaire pour comparer leurs représentations |
| **Mesures** | Similarité cosinus entre canaux et rang effectif (SVD) des cartes de caractéristiques, extraites par hook PyTorch |
| **Résultat** | Rang effectif **4,1/8** pour le bottleneck standard contre **7,7/8** pour le bottleneck inversé et 7,0/8 pour la baseline |
| **Robustesse** | Le code ne fixe pas de graine : une ré-exécution complète dans un environnement neuf donne des valeurs proches et les mêmes tendances pour les trois architectures |
| **Limite assumée** | Ces mesures montrent un chevauchement des représentations, mais ne prouvent pas à elles seules une superposition |

## Objectif

Étudier si le design d'un bloc de convolution (standard vs. bottleneck vs. bottleneck inversé) influence l'organisation interne des caractéristiques apprises par un CNN, en particulier leur degré de redondance/chevauchement entre canaux. L'hypothèse sous-jacente, issue des travaux d'Elhage et coll. (2022) sur la superposition dans les réseaux de neurones, est qu'un réseau peut représenter plus de caractéristiques que sa dimensionnalité ne le permettrait normalement en les superposant dans des directions partagées de l'espace latent — ce qui peut être efficace, mais aussi source d'interférence entre caractéristiques.

## Travail réalisé

Le travail comprend une partie état de l'art (mémoire, chapitre 1) et une partie expérimentale (mémoire, chapitre 2 ; code dans `Code_Experiments/`) :

1. **État de l'art** — synthèse de la littérature sur la superposition et la polysémanticité des neurones (Elhage et coll., 2022), le rôle de la sparsité et de ReLU dans l'émergence de la superposition, les architectures de blocs convolutifs (convolution standard, depthwise, pointwise, depthwise separable) et les architectures bottleneck (standard et inversée), ainsi que sur la dépendance à l'interférence de différentes architectures bottleneck en vision (Hollard et coll., 2025).
2. **Conception du protocole expérimental** — définition de trois architectures CNN comparables (`architecture.py`) : un modèle baseline à deux blocs convolutifs classiques, un modèle à bottleneck standard (réduction puis expansion des canaux via convolutions pointwise, `8→4→8`), et un modèle à bottleneck inversé (expansion puis réduction, avec convolution depthwise, `8→16→8`). Les trois modèles sont conçus pour produire le même nombre de canaux (8) en sortie du deuxième bloc, afin de rendre leurs représentations comparables.
3. **Entraînement** — entraînement des trois modèles sur un sous-ensemble de CIFAR-10 à 4 classes (voiture, oiseau, chat, chien ; 20 000 images d'entraînement, 4 000 de test), avec la même fonction de perte (cross-entropy), le même optimiseur (Adam, lr=1e-3) et le même nombre d'époques (5), pour isoler l'effet du design du bloc plutôt que celui du protocole d'entraînement (`train.py`).
4. **Évaluation de la performance de classification** — calcul de l'accuracy top-1 et top-3, de l'écart top-3/top-1, et des précision/rappel/F1 par classe pour les modèles standard et inversé, à titre de diagnostic complémentaire (le mémoire précise que ces métriques ne sont pas la preuve directe d'une superposition, mais servent à vérifier que les modèles apprennent correctement la tâche).
5. **Extraction et analyse des représentations internes** — extraction des cartes de caractéristiques (feature maps) produites par le deuxième bloc de chaque modèle (`utils.py`, via un hook PyTorch), puis calcul de deux mesures de redondance représentationnelle sur ces cartes :
   - **similarité cosinus** entre canaux (matrice 8×8 par modèle), pour quantifier l'alignement directionnel entre paires de canaux ;
   - **rang effectif** (effective rank, Roy & Vetterli, 2007) de la matrice d'activations, pour estimer le nombre de directions réellement utilisées par la représentation.
6. **Interprétation** — comparaison des trois architectures sur ces deux mesures, mise en relation avec les résultats de classification, et discussion des limites de l'approche (mesures indirectes, chevauchement observé même sans bottleneck explicite pour le baseline, résultats d'une seule exécution).

Le mémoire complet (`Memoire_TER_Yasmina_Farid.pdf`) détaille l'ensemble de la démarche ; ce dépôt contient également le sujet du TER (`Sujet.pdf`) et le support de soutenance (`Soutenance_Yasmina_Farid.pdf`).

## Compétences mises en pratique

| Compétence | Application dans ce projet |
|---|---|
| Conception d'architectures CNN avec PyTorch | Implémentation de trois variantes de blocs convolutifs (`nn.Module`) partageant une structure comparable : baseline, bottleneck standard (reduce→regular→expand), bottleneck inversé (expand→depthwise→reduce) |
| Contrôle expérimental | Design d'un protocole où les trois modèles sont entraînés dans des conditions identiques (données, perte, optimiseur, époques) et mesurés à une couche où ils produisent le même nombre de canaux, pour isoler l'effet du design architectural |
| Extraction de représentations internes | Utilisation de hooks PyTorch (`register_forward_hook`) pour capturer les activations intermédiaires (feature maps) d'un bloc donné sans modifier le modèle |
| Mesures de géométrie de l'espace latent | Implémentation from scratch de la similarité cosinus inter-canaux et du rang effectif (via décomposition en valeurs singulières et entropie des valeurs singulières normalisées) |
| Évaluation de modèles de classification | Calcul de métriques top-k, matrice de confusion et précision/rappel/F1 par classe avec `torcheval` et `scikit-learn` |
| Synthèse bibliographique et recherche | Lecture et synthèse critique d'articles de recherche (Elhage et coll. 2022 sur la superposition, Hollard et coll. 2025 sur les modèles vision bas-paramètres, Younesi et coll. 2024 et Liu et coll. 2024 sur les architectures convolutives) pour construire un état de l'art et motiver le protocole expérimental |
| Rédaction scientifique | Rédaction d'un mémoire structuré (LaTeX) présentant contexte, état de l'art, protocole, résultats et discussion critique des limites |

## Outils utilisés

Bibliothèques Python effectivement importées dans le code (`architecture.py`, `train.py`, `utils.py`, `cnn.ipynb`) :
- **torch**, **torchvision** — définition des modèles, chargement et normalisation de CIFAR-10, entraînement
- **torcheval** — métriques de classification multiclasse (matrice de confusion, précision, rappel, F1)
- **scikit-learn** — affichage de la matrice de confusion (`ConfusionMatrixDisplay`)
- **matplotlib** — visualisation (courbes d'entraînement, heatmaps de similarité cosinus)

Le notebook s'exécute dans un environnement Jupyter (kernel Python), en local sur une machine avec GPU (le fichier `requirements.txt` d'origine contient des paquets CUDA/NVIDIA correspondant à cet environnement). Le mémoire est rédigé en LaTeX (`Memoire/memoire.tex`).

## Principaux résultats

Les chiffres ci-dessous proviennent des sorties sauvegardées dans `Code_Experiments/cnn.ipynb` (dernière exécution) et correspondent à ceux publiés dans les tableaux 2.2 et 2.3 du mémoire. Ils ont été vérifiés en ré-exécutant le code réel du notebook de bout en bout dans un environnement Python propre (voir [Exécuter le projet](#exécuter-le-projet)). **Important : le code ne fixe aucune graine aléatoire** (pas de `torch.manual_seed`), donc l'initialisation des poids et le mélange des données diffèrent à chaque exécution — les valeurs exactes ne sont donc pas reproductibles au chiffre près. La ré-exécution donne des résultats du même ordre de grandeur (écarts de quelques points de pourcentage sur l'accuracy, par exemple standard 67,33 % vs 67,40 % en top-1) et confirme entièrement les tendances qualitatives : le bottleneck standard reste systématiquement celui qui a le rang effectif le plus bas (~4/8) et la similarité cosinus maximale la plus élevée, le bottleneck inversé reste celui qui a le rang effectif le plus haut (~7,8-7,9/8) et aucune paire de canaux au-delà de 0,5 de similarité absolue, et le baseline reste intermédiaire avec une similarité cosinus moyenne élevée malgré l'absence de bottleneck.

**Performance de classification** (sous-ensemble CIFAR-10 à 4 classes, top-1 / top-3 accuracy) :

| Modèle | Top-1 accuracy | Top-3 accuracy |
|---|---|---|
| Baseline (val. accuracy en fin d'entraînement) | 69,6 % | — |
| Bottleneck standard | 67,40 % | 98,42 % |
| Bottleneck inversé | 69,33 % | 98,02 % |

Le modèle baseline n'a pas été évalué avec les mêmes métriques top-1/top-3/par-classe que les deux bottlenecks dans le notebook (seule sa validation accuracy en fin d'entraînement est disponible) — voir [Limites](#limites-et-améliorations-possibles).

**Redondance représentationnelle du deuxième bloc (feature maps, 8 canaux)** :

| Architecture | Similarité cosinus moyenne (abs.) | Similarité cosinus max (abs.) | Fraction de paires > 0,5 | Rang effectif | Rang effectif normalisé |
|---|---|---|---|---|---|
| Baseline | 0,4198 | 0,7063 | 25,0 % | 7,04 | 0,880 |
| Bottleneck standard | 0,3247 | 0,9199 | 25,0 % | 4,10 | 0,513 |
| Bottleneck inversé | 0,1450 | 0,4398 | 0,0 % | 7,73 | 0,966 |

- Le bottleneck standard concentre le plus sa représentation (rang effectif ≈ 4 sur 8 canaux possibles) et présente la similarité cosinus maximale la plus élevée entre canaux, ce qui suggère un degré de superposition plus marqué.
- Le bottleneck inversé garde une représentation plus distribuée (rang effectif proche de 8, aucune paire de canaux au-delà de 0,5 de similarité absolue).
- Le baseline présente lui aussi une similarité cosinus moyenne élevée, ce qui montre que le chevauchement de canaux n'est pas spécifique aux architectures bottleneck — c'est l'une des limites explicitement discutées dans le mémoire (chapitre 2.3).
- Ces observations géométriques sont cohérentes avec la meilleure performance de classification (top-1/top-3, F1 par classe) du bottleneck inversé par rapport au bottleneck standard, mais le mémoire souligne que la similarité cosinus et le rang effectif restent des mesures indirectes : elles ne permettent pas d'identifier individuellement les caractéristiques superposées.

## Exécuter le projet

Fichier principal : `Code_Experiments/cnn.ipynb`.

1. Se placer dans `Code_Experiments/`, créer un environnement virtuel et installer les dépendances :
   ```bash
   cd Code_Experiments
   python3 -m venv venv
   source venv/bin/activate
   pip install -r requirements.txt
   ```
2. Lancer Jupyter (`jupyter notebook` ou l'extension Jupyter d'un IDE) et ouvrir `cnn.ipynb`.
3. Le notebook télécharge automatiquement CIFAR-10 via `torchvision.datasets.CIFAR10` (paramètre `download=True`) au premier chargement des données ; aucune donnée n'a besoin d'être fournie séparément.
4. `requirements.txt` est un `pip freeze` figé de l'environnement local d'origine (GPU, CUDA 13) : il fixe des versions exactes, y compris des paquets `nvidia-*` spécifiques à cet environnement. Sur une machine sans GPU compatible CUDA, PyTorch retombera automatiquement sur le CPU (le code utilise `torch.accelerator.current_accelerator()` / vérifie `torch.cuda.is_available()`), mais l'installation stricte de `requirements.txt` peut échouer ou installer inutilement les paquets CUDA ; dans ce cas, il est préférable d'installer les dépendances sans version imposée (`torch`, `torchvision`, `torcheval`, `scikit-learn`, `matplotlib`, `ipykernel`).
5. Le mémoire (`Memoire/memoire.tex`) peut être recompilé avec une distribution LaTeX standard (ex. `pdflatex` ou `latexmk`) depuis le dossier `Memoire/`.

## Limites et améliorations possibles

- **Un seul run d'entraînement par architecture, sans graine aléatoire fixée** : les résultats (classification, similarité cosinus, rang effectif) proviennent d'une seule exécution par modèle, sans répétition ni intervalle de confiance — le mémoire le signale explicitement comme une limite (chapitre 2.3). Le code ne fixe pas non plus de graine aléatoire (`torch.manual_seed`), ce qui a été confirmé lors de la vérification de reproductibilité : une ré-exécution donne des chiffres légèrement différents (même ordre de grandeur, mêmes tendances qualitatives) à chaque fois.
- **Baseline non évalué avec les mêmes métriques que les bottlenecks** : le notebook calcule l'accuracy top-1/top-3 et les métriques par classe pour les modèles standard et inversé, mais pas pour le baseline (seule sa validation accuracy en fin d'entraînement est disponible) — la comparaison de classification à trois modèles n'est donc pas parfaitement symétrique.
- **Mesures indirectes de la superposition** : la similarité cosinus et le rang effectif sont des proxys géométriques (ils montrent une concentration ou une dispersion des directions d'activation), pas une preuve directe de superposition au sens d'Elhage et coll. — le mémoire propose les autoencodeurs parcimonieux (sparse autoencoders) comme piste pour une mesure plus directe, non explorée dans ce travail.
- **Comparaison non parfaitement contrôlée entre baseline et bottlenecks** : le baseline et les deux bottlenecks n'ont pas la même résolution spatiale en sortie du deuxième bloc (6×6 pour le baseline contre 14×14 pour les bottlenecks), ce qui limite la lecture de la comparaison à l'organisation par canal plutôt qu'à une comparaison strictement contrôlée.
- **Échelle réduite** : modèles volontairement petits (8 canaux en sortie du bloc mesuré) et sous-ensemble à 4 classes de CIFAR-10, pour rendre l'expérience interprétable et rapide à entraîner ; la généralisation à des architectures et jeux de données plus larges (ConvNeXt, goulets asymétriques/sandglass mentionnés dans le sujet du TER) n'a pas été testée.

## Contributeurs

Projet réalisé en solo par Yasmina FARID, sous la direction d'Anne Jeannin-Girardon (ICube, Université de Strasbourg).

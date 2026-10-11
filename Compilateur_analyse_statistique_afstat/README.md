# AFSTAT — compilateur pour l'analyse statistique de données

Projet réalisé dans le cadre du cours de Compilation en M1. AFSTAT est un mini-langage déclaratif (DSL) permettant de décrire, à partir d'un fichier texte `.afs`, le chargement de fichiers CSV, leur structuration en schémas typés, puis des opérations de jointure, de filtrage, de calcul de colonnes et d'analyse statistique (moyenne, médiane, écart-type, min/max, histogramme, résumé). Le projet implémente la chaîne de compilation complète : analyse lexicale (flex), analyse syntaxique et sémantique (bison), génération de code C, et une bibliothèque runtime en C qui exécute ce code généré sur des données réelles.

## Objectif

Concevoir et implémenter, de bout en bout, un compilateur pour un langage dédié à l'analyse de données tabulaires : reconnaître un programme AFSTAT, vérifier sa cohérence (types, colonnes, schémas), puis produire un programme C autonome qui charge les CSV décrits, exécute les transformations demandées (jointures, filtres, colonnes calculées) et affiche les résultats statistiques.

## Travail réalisé

1. **Analyse lexicale** (`lexer.l`, `tokens.h`) — scanner flex reconnaissant les mots-clés du langage (`source`, `schema`, `associate`, `join`, `filter`, `compute`, `analyze`, les opérations statistiques `mean`/`median`/`std_dev`/`min`/`max`/`histogram`/`summary`...), les opérateurs, les littéraux (entiers, flottants, chaînes) et les identifiants, avec détection des caractères invalides et gestion des commentaires `//`.

2. **Analyse syntaxique et sémantique** (`parser.y`) — grammaire bison (~168 règles) décrivant la structure d'un programme AFSTAT (sources → schémas/associations → opérations optionnelles de join/filter/compute → blocs d'analyse obligatoires). La table des symboles (`symtable.c/h`) enregistre sources, schémas et associations et permet de détecter redéclarations et références invalides. Les contrôles sémantiques vérifient l'existence des colonnes référencées, la compatibilité des types lors d'un join ou d'une expression, et la cohérence entre l'opération d'analyse demandée et le type de la colonne visée.

3. **Génération de code C** (`parser.y`, fonctions `code_expr_emit`/`codegen_*`) — à partir des structures de requêtes collectées pendant le parsing (`ComputeRequest`, `FilterRequest`, `JoinRequest`, `AnalyzeRequest`) et des expressions typées (`CodeExpr`), le compilateur écrit un fichier C complet qui déclare une `RtTable` par dataset, charge les CSV via `rt_load_csv`, exécute séquentiellement joins/filtres/calculs, puis les opérations d'analyse.

4. **Runtime d'exécution** (`runtime.c/h`) — bibliothèque C fournissant le chargement/validation de CSV (vérification de l'en-tête et du typage colonne par colonne), une table typée générique (`RtTable`, types int/float/string), les primitives de copie de lignes/colonnes utilisées par join et filter, et les fonctions statistiques (moyenne, médiane, écart-type, min, max, histogramme sur colonne texte).

5. **Tests** — 7 programmes d'exemple (`exemples/prog1.afs` à `prog7.afs`) couvrant chaque fonctionnalité (analyse simple, colonne calculée, jointure, jointure+calcul, filtre numérique, filtre texte), plus un programme combinant double jointure/filtre/calcul (`exemples/test8.afs`). Le dossier `test/` contient des cas positifs (`test/positive/`) et des cas négatifs attendant une erreur (`test/negative/`) : erreur lexicale, erreur de syntaxe, colonne/identifiant inconnu, jointure de types incompatibles, opération d'analyse incompatible avec le type de colonne, schéma incohérent avec le CSV associé. Les cibles du `Makefile` (`make test`, `make test_codegen`, `make test_positive_*`, `make test_lexical_invalid`, etc.) automatisent ces vérifications.

   En préparant ce dépôt, l'exécution de `exemples/test8.afs` (qui enchaîne une jointure puis deux filtres successifs, le second filtre référençant une colonne par le nom du schéma source plutôt que par le dataset filtré courant) a révélé un bug de génération de code : les colonnes étaient relues dans la table d'origine du schéma cité, en réutilisant l'index de ligne du dataset filtré, ce qui désynchronisait les lignes dès qu'un filtre précédent avait déjà réordonné/réduit le jeu de données. Le bug a été corrigé dans `parser.y` (résolution de l'offset de colonne dans la table du dataset réellement itéré, via `resolve_offset_in_schema`, en suivant la chaîne de join/filter qui a produit ce dataset) ; les 7 exemples et l'ensemble des tests positifs/négatifs ont été rejoués après correction et donnent des résultats identiques à avant, à l'exception de `test8.afs` qui produit désormais les valeurs correctes (voir Principaux résultats).

## Compétences mises en pratique

| Compétence | Application dans ce projet |
|---|---|
| Analyse lexicale avec flex | Écriture de `lexer.l` : reconnaissance de tous les tokens du langage AFSTAT, gestion des erreurs lexicales et des commentaires |
| Analyse syntaxique avec bison (LALR) | Grammaire complète de `parser.y`, gestion des priorités d'opérateurs, actions sémantiques attachées aux règles, `%destructor` pour la libération mémoire en cas d'erreur de parsing |
| Analyse sémantique et table des symboles | `symtable.c/h` : résolution de schémas/colonnes, détection de redéclarations, vérification de compatibilité de types (join, expressions, opérations d'analyse) |
| Génération de code C à partir d'une représentation intermédiaire | Traduction des requêtes collectées (join/filter/compute/analyze) et des expressions typées (`CodeExpr`) en code C compilable, avec gestion des offsets de colonnes à travers des chaînes de transformations (join puis filter) |
| Conception d'un runtime en C | `runtime.c/h` : structure de table générique typée, chargement/validation de CSV, statistiques numériques, gestion mémoire (allocation/libération des colonnes chaîne) |
| Débogage et vérification empirique de résultats | Identification d'un bug d'indexation dans un cas de test à plusieurs niveaux de jointure/filtrage, correction ciblée et re-validation de la suite de tests complète |
| Outillage de build et tests automatisés | `Makefile` avec cibles dédiées par cas de test (positif, négatif, génération+exécution), compilation avec `-Wall -Wextra -std=c11` |

## Outils utilisés

Vérifiés par lecture des `#include` du code source et du `Makefile` — le projet n'a aucune dépendance externe hors outils de build C standard :

- **flex** (2.6.4 sur la machine de développement) — génération du lexer
- **GNU Bison** (3.8.2) — génération du parser LALR
- **gcc**, standard **C11**, `-Wall -Wextra` — compilation (compile sans aucun warning)
- **GNU Make** — orchestration du build et des tests
- Bibliothèque standard C uniquement : `stdio.h`, `stdlib.h`, `string.h`, `math.h` (écart-type via `sqrt`), `ctype.h`, `errno.h`, `sys/types.h`, `unistd.h`

## Principaux résultats

Valeurs recalculées le 19/09/2026 en recompilant le projet (`make clean && make`) et en exécutant chaque exemple (`make test_codegen RUN=1`, `make test_positive`) — voir la section Reproductibilité de la préparation de ce dépôt pour le détail de cette vérification.

- `exemples/prog1.afs` (démographie patients, 29 lignes) : `mean age = 51.9`, histogramme genre : M=15, F=15
- `exemples/prog2.afs` (poids/taille/IMC, 29 lignes) : poids — min 65.4, max 95.0, mean 79.46, std_dev 7.92 ; taille — min 1.60, max 1.81, mean 1.70, std_dev 0.061 ; IMC — min 20.64, max 32.85, mean 27.67, std_dev 3.33
- `exemples/prog4.afs` (jointure patients × dossiers médicaux) : cholestérol — min 175.6, max 270.6, mean 223.01, std_dev 25.08
- `exemples/prog6.afs` (jointure + filtre âge > 50) : `mean cholesterol = 239.99`, histogramme genre : F=8, M=8
- `exemples/prog7.afs` (filtre texte département = "R&D") : salaire — min 65000, max 75000, mean 70066.67, std_dev 2914.66
- `exemples/test8.afs` (double jointure/filtre + colonne calculée, après correction du bug décrit ci-dessus) : `mean heart_stress = 102.0`, histogramme ville : Paris=2, résumé température — min 38.8, max 39.0, mean 38.9, std_dev 0.14 — ces valeurs correspondent exactement aux résultats attendus documentés en commentaire dans le fichier, ce qui a servi de vérification croisée pour la correction du bug

Les commentaires "résultats attendus" présents dans certains fichiers `exemples/prog*.afs` (notes de développement de l'auteur, non systématiquement revérifiées) diffèrent parfois légèrement des valeurs ci-dessus ; ce sont les valeurs ci-dessus, recalculées lors de la préparation de ce dépôt, qui font foi.

## Exécuter le projet

Dépendances : `gcc`, `flex`, `bison`, `make` (testé sous Linux avec gcc 13, flex 2.6.4, bison 3.8.2 ; pas de dépendance à un GPU ou à un environnement particulier).

```bash
# Compiler le compilateur AFSTAT
make clean && make

# Vérifier que le parser accepte tous les exemples fournis
make test

# Générer, compiler et exécuter tous les exemples (exemples/prog1..7.afs)
RUN=1 make test_codegen

# Lancer un exemple précis
make test_positive_join      # ou test_positive_compute, test_positive_filter, ...

# Lancer les tests d'erreurs attendues (lexicale, syntaxique, sémantique)
make test_negative

# Compiler et exécuter un programme .afs manuellement
./afstat_parser -o gen.c exemples/prog1.afs
gcc -Wall -Wextra -std=c11 -I. gen.c runtime.c -o gen -lm
cd exemples && ../gen   # les .afs référencent leurs CSV en chemin relatif "data/<fichier>.csv"
```

Options de la ligne de commande du compilateur (`./afstat_parser`) :
- `-version` : affiche les membres du projet
- `-tos` : affiche la table des symboles après analyse
- `-syntax-only` : arrête après l'analyse syntaxique, sans générer de code
- `-o <fichier>` : nom du fichier C généré (par défaut `afstat.c`)

## Limites et améliorations possibles

- Pas de représentation intermédiaire (IR) ni de passes d'optimisation : le compilateur génère directement du C à partir des requêtes collectées pendant le parsing, considéré comme code intermédiaire avant compilation par `gcc` (limite assumée et documentée dans le rapport de projet, faute de temps pour finaliser une passe d'IR à 3 adresses).
- Le runtime ne supporte que trois types (`int`, `float`, `string`) ; pas de valeurs manquantes/NULL, pas de dates typées (les dates sont traitées comme de simples chaînes, voir `visit_date` dans `prog2.afs`).
- Les jointures sont implémentées par double boucle naïve (O(n×m)), sans index : suffisant pour les volumes de test utilisés (~30 lignes par CSV) mais ne passerait pas à l'échelle sur des jeux de données plus grands.
- La validation du schéma déclaré face au CSV réel (nombre de colonnes, typage) n'a lieu qu'à l'exécution du programme généré (`rt_load_csv`), pas à la compilation : un `schema`/`associate` syntaxiquement valide mais incohérent avec le CSV n'est détecté qu'au runtime (voir `test/negative/bad_schema.afs`, `schema_type_mismatch.afs`).
- `ast.c`/`ast.h` ne portent qu'un nœud AST minimal (juste un type de nœud, sans payload) : il sert surtout à la libération mémoire ordonnée du parser en cas d'erreur (`%destructor` de bison) ; la génération de code s'appuie en réalité sur des structures de requêtes dédiées (`ComputeRequest`, `FilterRequest`, `JoinRequest`, `AnalyzeRequest`) et non sur cet AST.
- La table des symboles utilise des tableaux de taille fixe (`SYMTABLE_MAX_SYMBOLS = 100`, `MAX_COMPUTE_REQUESTS`/`MAX_ANALYZE_REQUESTS`/`MAX_FILTER_REQUESTS`/`MAX_JOIN_REQUESTS` = 128 à 256) plutôt que des structures dynamiques : suffisant pour des programmes de démonstration, mais limite artificiellement la taille d'un programme AFSTAT réel.
- Le bug de résolution de colonnes décrit dans "Travail réalisé" n'a été identifié que sur le cas `test8.afs`, qui n'était rattaché à aucune cible du `Makefile` avant la préparation de ce dépôt (seuls `exemples/prog*.afs` sont couverts par `make test`/`make test_codegen`) ; il n'est pas exclu que d'autres combinaisons de jointures/filtres imbriqués non testées présentent des cas limites non couverts.

## Contributeurs

Projet réalisé en groupe de 4 dans le cadre du cours de Compilation (M1) : Yasmina Farid, Yassine Fadl, Lenny Borry, Adrien Criseo.

Contribution de Yasmina Farid : analyse lexicale et syntaxique (`lexer.l`, `parser.y`), rédaction de la majorité des règles de grammaire, écriture des fonctions appelées depuis le parser (actions sémantiques et génération de code), et table des symboles (`symtable.c/h`). La répartition détaillée des tâches entre les autres membres du groupe n'est pas documentée ici.

## Licence

Ce projet est distribué sous licence MIT — voir le fichier [LICENSE](LICENSE).

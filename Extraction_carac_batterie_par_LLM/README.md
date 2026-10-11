# Extraction d’informations de fiches techniques de batteries avec un LLM

## Contexte du projet

J’ai réalisé ce projet en M1 à l’Université de Strasbourg, avec Olivia Amele Adamah et Fatima Ould Hammouda. Je présente ici le fonctionnement du projet, ma contribution et les résultats de notre travail en équipe.

- Type : projet universitaire réalisé en M1.
- Établissement : Université de Strasbourg.
- Date du rapport : mai 2026.
- Équipe : Yasmina Farid, Olivia Amele Adamah, Fatima Ould Hammouda.
- Domaine : extraction d’informations, LLM, données structurées et validation.
- Documents étudiés : quatre fiches techniques de batteries au format PDF.

## En bref

| | |
|---|---|
| **Tâche** | Extraire 20 champs techniques (avec unités) de fiches PDF de batteries et produire un JSON validé |
| **Modèle** | Mistral Small 3.2 24B, via l'API OpenWebUI du serveur de l'Université de Strasbourg |
| **Protocole** | Comparaison zero-shot et few-shot sur 4 fiches techniques, F1 calculé par rapport à des JSON de référence annotés à la main |
| **Résultat** | F1 moyen **0,85 en zero-shot, 0,87 en few-shot** ; corpus trop petit pour généraliser |
| **Contrôle qualité** | Règles de validation (valeurs hors plage physique, champs obligatoires manquants, valeurs suspectes) pour repérer les hallucinations |
| **Code** | Package Python en 6 modules (`src/`), configuration YAML, clé API lue dans une variable d'environnement |
| **Tests** | 24 tests `pytest` : ils passent en moins d'une seconde, sans réseau ni clé API ; couverture de 39 % sur l'ensemble du code, de 41 à 67 % sur les modules d'extraction et de validation |

## Objectif

Notre objectif était de transformer les informations contenues dans des fiches techniques PDF en données JSON exploitables.

Nous avons organisé le projet en trois parties :

1. Extraire les caractéristiques techniques avec un LLM.
2. Organiser les données dans un graphe de connaissances.
3. Appliquer des contrôles pour repérer des incohérences et des hallucinations potentielles.

## Travail réalisé

### 1. Ma contribution

J’ai principalement contribué aux aspects suivants :

- La chaîne d’extraction des informations.
- Les prompts envoyés au LLM.
- La comparaison zero-shot et few-shot.
- La structuration des réponses.
- L’évaluation des extractions.
- La validation des données.

Mes coéquipières ont principalement réalisé la partie consacrée au graphe de connaissances et à l’ontologie.

Cette liste résume ma contribution sans détailler la répartition de chaque tâche. Les résultats présentés ici sont ceux de notre travail collectif.

### 2. Données utilisées

Nous avons travaillé sur quatre fiches techniques :

| Batterie | Référence utilisée dans le rapport |
|---|---|
| Batterie bouton | LIR2450 |
| Batterie cylindrique | 16340 |
| Batterie polymère | PL-9059156 |
| Batterie CATL | FFH3DS |
 
Pour évaluer les réponses du modèle, nous avons constitué des fichiers JSON de référence en annotant manuellement les PDF.

### 3. Extraction du texte des PDF

Pour extraire le texte des PDF, nous avons utilisé :

- `pypdf`
- `pdfplumber`

Le texte extrait est transmis au modèle de langage.

La qualité de cette étape influence la suite du traitement, notamment lorsque les informations sont présentées dans des tableaux.

### 4. Extraction des informations avec le LLM

Nous avons utilisé le modèle suivant, décrit dans notre rapport :

- Nom : Mistral Small 3.2 24B.
- Accès : serveur de l’Université de Strasbourg.
- Interface : API OpenWebUI.
- Communication : requêtes HTTP avec la bibliothèque Python `requests`.

Le modèle reçoit :

- Des consignes générales.
- La liste des champs attendus.
- Des contraintes de format.
- Le texte de la fiche technique.
- Un exemple complet supplémentaire pour la stratégie few-shot.

Le modèle doit retourner un objet JSON comportant 20 champs, unités comprises.

Informations recherchées :

- Référence de la batterie.
- Fabricant.
- Chimie.
- Tension nominale.
- Capacité.
- Courant de charge.
- Courant de décharge.
- Résistance interne.
- Plage de température.
- Dimensions.
- Poids.
- Durée de vie en cycles.
- Tension de fin de charge.
- Tension de fin de décharge.
- Unités associées aux champs concernés.

Les prompts demandent au modèle de respecter les consignes suivantes :

- Utiliser uniquement les informations du document.
- Retourner `unknown` lorsqu’une information est absente ou ambiguë.
- Séparer les valeurs numériques et les unités.
- Convertir les courants exprimés en C-rate avec la capacité nominale.

Exemple de conversion :

    0,2C × 120 mAh = 24 mA

### 5. Comparaison des stratégies de prompting

| Stratégie | Fonctionnement |
|---|---|
| Zero-shot | Consignes sans exemple complet de fiche technique et de réponse JSON |
| Few-shot | Consignes accompagnées d’un exemple fictif complet et de la réponse JSON attendue |

Précisions :

- Notre implémentation few-shot utilise un seul exemple complet.
- Les deux stratégies utilisent le même modèle.
- Nous n’avons pas réalisé de fine-tuning.
- L’amélioration recherchée vient du prompt, pas d’un entraînement du modèle.

### 6. Structuration et sauvegarde

Étapes présentes dans le programme :

1. Recevoir la réponse du LLM.
2. Essayer de la lire directement comme du JSON.
3. Si nécessaire, rechercher le JSON entre les accolades.
4. Convertir les champs numériques.
5. Construire un objet Python `BatteryData`.
6. Appliquer la validation.
7. Sauvegarder les données en JSON.

Le programme comprend également :

- Un traitement par lot de plusieurs PDF.
- Une journalisation des opérations et des erreurs.
- Une configuration YAML.
- Le chargement de variables d’environnement.

### 7. Évaluation des extractions

Pour évaluer les extractions, nous comparons les valeurs produites aux JSON de référence.

Méthodes utilisées :

- Comparaison champ par champ.
- Identification des informations absentes.
- Normalisation de certaines unités.
- Tolérance relative de 5 % pour les valeurs numériques.
- Règles de comparaison spécifiques aux textes, températures et dimensions.

Exemples de normalisation :

- Ah vers mAh.
- mA vers A.
- kg vers g.
- Ω vers mΩ.

Métriques calculées :

| Métrique | Signification |
|---|---|
| Précision | Part des informations extraites considérées comme correctes |
| Rappel | Part des informations attendues correctement retrouvées |
| F1 | Mesure combinant précision et rappel |

Les règles de comparaison sont propres au projet. Elles influencent les scores obtenus.

### 8. Validation des données

Le module `DataValidator` vérifie notamment :

- La présence de la tension et de la capacité.
- Les plages de valeurs autorisées.
- La cohérence des températures minimale et maximale.
- Certaines relations entre chimie et tension.
- Certaines valeurs inhabituelles ou suspectes.

Le score de confiance commence à 1.

Lorsqu’une erreur est ajoutée, le score est multiplié par un facteur dépendant de sa gravité :

| Gravité | Facteur |
|---|---:|
| Critique | 0,3 |
| Majeure | 0,7 |
| Mineure | 0,9 |

Certaines valeurs sont seulement signalées comme suspectes. Tous les signalements ne provoquent pas individuellement une pénalité.

Ce score est un indicateur fondé sur des règles. Ce n’est pas une probabilité statistique que la réponse soit correcte.

Une valeur incohérente peut signaler une erreur d’extraction. Une valeur cohérente peut néanmoins être inventée.

### 9. Graphe de connaissances — partie collective

La partie graphe de notre projet comprend :

- Une ontologie OWL.
- La conversion des données JSON en triples RDF.
- Un export au format Turtle.
- Des requêtes SPARQL.
- Des relations entre batteries.
- Des contrôles utilisant l’ontologie et le graphe.

Le fichier `battery_extractor.py` contient des appels au module de graphe.

Je présente cette partie pour expliquer le fonctionnement global du projet. Sa réalisation a été principalement prise en charge par mes coéquipières.

## Compétences mises en pratique

Ma participation à ce projet m’a permis de mettre en pratique les compétences suivantes :

| Compétence | Élément concret du projet |
|---|---|
| Extraction d’informations | Transformation du contenu de fiches techniques en champs structurés |
| Utilisation d’une API de LLM | Requêtes HTTP, paramètres du modèle et traitement des réponses |
| Conception de prompts | Contraintes de sortie, gestion des informations absentes et exemple de référence |
| Comparaison de stratégies | Évaluation zero-shot et few-shot |
| Manipulation de JSON | Lecture, récupération et sauvegarde des réponses |
| Structuration de données en Python | Utilisation de `BatteryData` et séparation des valeurs et unités |
| Évaluation | Calcul de précision, rappel et F1 par comparaison aux références |
| Normalisation des unités | Conversion des valeurs avant comparaison |
| Validation de données | Contrôles de présence, de plages et de cohérence |
| Organisation du code | Modules Python, configuration YAML et journalisation |
| Intégration collective | Connexion de l’extraction à la partie graphe du projet |

## Outils utilisés

| Outil | Utilisation |
|---|---|
| Python | Traitement et orchestration |
| Mistral | Extraction par modèle de langage |
| OpenWebUI | Interface du serveur LLM |
| requests | Requêtes HTTP |
| JSON | Données structurées et références |
| dataclasses | Structure `BatteryData` |
| PyYAML | Configuration |
| python-dotenv | Variables d’environnement |
| pypdf et pdfplumber | Extraction du texte des PDF |
| RDF, OWL, SPARQL | Partie graphe du projet collectif |
| rdflib et Protégé | Manipulation et visualisation du graphe et de l’ontologie |

## Principaux résultats

Je reprends ici les résultats du tableau 3, page 4 de [notre rapport](Rapport.pdf).

| Batterie | F1 zero-shot | F1 few-shot |
|---|---:|---:|
| 16340 | 0,8333 | 0,8333 |
| PL-9059156 | 0,6486 | 0,7027 |
| FFH3DS — CATL | 0,9500 | 0,9500 |
| LIR2450 | 0,9524 | 1,0000 |
| Moyenne | 0,8461 | 0,8715 |

**Résultats rapportés — mai 2026**

D’après le tableau 3 du rapport, le F1 moyen sur quatre fiches techniques passe de **0,8461 en zero-shot à 0,8715 avec un exemple dans le prompt**, soit un gain de **2,54 points sur une échelle de 0 à 100**. Deux documents présentent une amélioration et deux conservent le même score. Ce petit corpus ne permet pas de généraliser ces résultats. Je reprends les scores du rapport ; je n’ai pas relancé l’évaluation pour cette mise à jour du README.

**Note de cohérence :** la conclusion du rapport mentionne un F1 moyen few-shot de 0,84, incompatible avec son tableau 3. Je retiens ici la moyenne du tableau détaillé : 0,8715 (soit 0,87 à deux décimales).

## Exécuter le projet

### Fichiers principaux

| Fichier | Rôle |
|---|---|
| `battery_extractor.py` | Chaîne de traitement et sauvegarde |
| `llm_interface.py` | Prompts, API et évaluation |
| `validator.py` | Contrôles de cohérence |
| `src/pdf_processor.py` | Extraction du texte des PDF |
| `src/knowledge_graph.py` | Gestion du graphe |
| `config/config.yml` | Paramètres du programme |

### Prérequis

Conserver l’arborescence du dépôt.

Disposer :

- Des modules du dossier `src/`.
- Du fichier de configuration.
- Des PDF à analyser.
- Des JSON de référence pour l’évaluation.
- D’un accès autorisé au serveur LLM.
- D’une clé API configurée localement.
- Des fichiers d’ontologie si le graphe est activé.

Sections de configuration attendues :

- `llm`
- `validation`
- `knowledge_graph`
- `logging`
- `paths`

Le module de graphe est importé même lorsque son utilisation est désactivée. Il doit rester disponible avec ses dépendances.

Ne pas publier les clés API ni le fichier `.env` contenant les identifiants.

### Installation

Installer les dépendances du dépôt en priorité si un fichier dédié est fourni.

Bibliothèques principales identifiées :

```bash
python -m pip install requests PyYAML python-dotenv pypdf pdfplumber rdflib
```

D’autres dépendances peuvent être nécessaires aux modules du projet complet.

### Extraction d’un document

Depuis la racine du projet :

```python
from src.battery_extractor import BatteryExtractor

extractor = BatteryExtractor("config/config.yml")

result = extractor.extract_from_pdf(
    "chemin/vers/fiche_batterie.pdf",
    method="few_shot",
)

print(result.to_dict())
```

Remplacer le chemin du PDF.

Pour utiliser la stratégie zero-shot :

```python
method="zero_shot"
```

### Comparaison des stratégies

```python
import json

with open("chemin/vers/reference.json", encoding="utf-8") as fichier:
    reference = json.load(fichier)

texte = extractor.pdf_processor.extract_text(
    "chemin/vers/fiche_batterie.pdf"
)

evaluation = extractor.llm.evaluate_extraction(
    texte,
    reference,
)

print(evaluation["comparison"])
```

### Sorties

- Résultats JSON : dossier défini par `paths.output_dir`.
- Journaux : dossier défini par `paths.logs_dir`.

Les exemples ci-dessus utilisent les méthodes du projet. Je n’ai pas revérifié l’exécution complète pour cette mise à jour du README.

## Tests

Les tests unitaires couvrent la structure des données (`BatteryData`), les règles de validation (tension invalide, champs manquants, valeurs suspectes), le nettoyage du texte des PDF, l'analyse des réponses du LLM (JSON valide ou non), la conversion d'unités et un pipeline complet simulé. Ils n'appellent jamais le serveur LLM.

```bash
pip install -r requirements.txt
python -m pytest -q --cov=src
```

Résultat vérifié le 11/10/2026 dans un environnement neuf : **24 tests réussis**, couverture de 39 %. La partie graphe de connaissances (`src/knowledge_graph.py`, réalisée par mes coéquipières) est la moins couverte (9 %).

## Limites et améliorations possibles

Pour poursuivre ce travail, voici les limites que je retiens et les améliorations que j’envisagerais :

| Limite | Conséquence ou amélioration possible |
|---|---|
| Quatre documents évalués | Élargir le corpus avant de généraliser les résultats |
| Extraction du texte PDF | Vérifier les tableaux et les informations mal extraites |
| Règles de comparaison personnalisées | Examiner les correspondances acceptées et les erreurs restantes |
| Validation par règles | Mesurer les anomalies correctement détectées et les fausses alertes |
| Gestion partielle des unités | Harmoniser les conversions entre extraction, évaluation et validation |
| Absence de preuve textuelle pour chaque valeur | Associer les champs extraits à leur passage source dans le PDF |
| Dépendance au serveur et au modèle | Documenter la configuration et répéter les évaluations |

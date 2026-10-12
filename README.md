# Yasmina Farid

**Stage de fin d'études en Data Science / Machine Learning — 6 mois, dès février 2027**
Master Informatique, parcours CODIA (Connaissances, science des données et intelligence artificielle) — Université de Strasbourg
Lieu : France entière

[yasmina.dsml@gmail.com](mailto:yasmina.dsml@gmail.com) · [LinkedIn](https://www.linkedin.com/in/yasmina-farid-93231a196) · [CV (PDF)](CV_Yasmina_Farid.pdf)

## Profil

Étudiante en M2 data science et IA. De la collecte et la préparation des données à l'évaluation critique des modèles : machine learning supervisé et non supervisé, deep learning, LLM (extraction, RAG).

## Formation

- **Master Informatique, parcours CODIA**, Université de Strasbourg (2025 – 2027)
  - M2 (en cours) : données à large échelle, entreposage de données massives distribuées et Business Intelligence, mise en œuvre de l'Edge AI.
  - M1 : science des données et apprentissage, statistiques pour la science des données, apprentissage profond, données complexes, programmation parallèle.
- **Licence Informatique**, Université de Strasbourg 

---

## Projets

### Projets principaux

| Projet | Problème → méthode | Résultat clé | Stack | Cadre · rôle |
|---|---|---|---|---|
| **PrivRAG** — assistant RAG sur documents privés, 100 % local | Interroger et résumer des documents privés sans les envoyer à un service externe → recherche BM25, vectorielle ou hybride, LLM local | Projet en cours (rendu en janvier 2027) : protocole de benchmark inspiré de MLPerf Inference (latence p50/p95, RAM, CPU/GPU) et jeu d'évaluation de questions annotées réalisés | Python · Qdrant · BM25 · Ollama · GitLab | Projet de M2, équipe de 5 · rôle : résumés et protocole d'évaluation de bout en bout |
| [Gravité des accidents de la route](Prediction-gravite-accidents-routiers/) | Prédire si un accident est grave → fusion de 6 sources ouvertes, 6 classifieurs comparés | F1 « grave » **0,665** sur 10 881 accidents de test (baseline : 0,53) ; stable en validation croisée (0,654 ± 0,005) | Python · pandas · scikit-learn | Projet de M1, équipe de 3 · rôle : construction et nettoyage du jeu de données |
| ↳ *Prolongements personnels, en cours* | **GraviRoute** : mise en production du modèle (API FastAPI, Docker, suivi MLflow). **VigiRoute** : tableau de bord Power BI sur un modèle en étoile PostgreSQL | En développement, pas encore publiés | FastAPI · Docker · MLflow · PostgreSQL · Power BI | Solo |
| [Extraction d'informations par LLM](Extraction_carac_batterie_par_LLM/) | Transformer des fiches techniques PDF en JSON de 20 champs → Mistral Small 24B, prompts zero-shot et few-shot, règles de validation contre les hallucinations | F1 moyen **0,87** en few-shot (0,85 en zero-shot) ; package Python de 6 modules, 24 tests pytest | Python · Mistral (API) · pytest | Projet de M1, équipe de 3 · rôle : extraction, prompts et évaluation |
| [Classification de cultures (Sentinel-2)](Classification_Cultures_Regression_Logistique/) | 20 types de cultures depuis des séries d'images satellites → features par canal et par mois, NDVI, pipeline optimisé par GridSearchCV | F1 macro **0,48** sur 20 classes déséquilibrées (hasard : 0,05) | Python · scikit-learn · imbalanced-learn | Projet de M1, équipe de 3 · contribution individuelle |
| [Qualité de l'eau en France (2022)](Analyse_qualite_eau_2022_Kmeans_PCA/) | Regrouper des stations de mesure → préparation de 331 122 mesures, K-means, ACP, croisement spatial et carte des résultats | 3 321 stations, 4 groupes (silhouette **0,26**) | Python · scikit-learn · GeoPandas | Projet de M1, binôme · rôle : préparation et clustering |
| [TER : superposition des caractéristiques dans les CNN](TER_FSI_CNN/) | Le design d'un bloc convolutif change-t-il la redondance des représentations ? → 3 architectures comparables, similarité cosinus, rang effectif | Rang effectif **4,1/8** (bottleneck) contre **7,7/8** (bottleneck inversé) ; félicitations et bonus du jury | Python · PyTorch | Projet de recherche encadré, laboratoire ICube · solo |
| [Classification de texte : RNN et LSTM](Emoji_prediction_LR_RNN_LSTM/) | Associer une phrase à un emoji (5 classes : amour, sport, joie, tristesse, nourriture) → back-traduction, GloVe, régression logistique, RNN et LSTM | Fuite d'information localisée et corrigée : validation croisée de la régression logistique ramenée de 0,73 à **0,64**, le LSTM devient le meilleur (**0,69**) | Python · Keras · Hugging Face | Projet de M1, équipe de 3 · rôle : augmentation des données et modèles |
| [Base de données d'une application de gestion des tâches](Base_donnees_gestion_taches/) | Concevoir entièrement la base d'une application → modèle conceptuel et logique, schéma Oracle à 9 tables, requêtes SQL | 12 requêtes : jointures, agrégations, sous-requêtes, requête récursive, fonctions analytiques | Oracle · SQL | Projet de L3 · conception complète de la base |

Pour le TER, voir aussi le [mémoire](TER_FSI_CNN/Memoire_TER_Yasmina_Farid.pdf) et les [slides de soutenance](TER_FSI_CNN/Soutenance_Yasmina_Farid.pdf).

### Autres projets

| Projet | Problème → méthode | Résultat clé | Stack | Cadre · rôle |
|---|---|---|---|---|
| [Génomique du cancer du sein (METABRIC)](Projet_tp_Genomique_METABRIC/) | Regrouper 1 092 patientes par expression génique → ACP, K-means, tests du khi-deux | Clusters fortement liés au sous-type moléculaire (p ≈ 10⁻²⁰²) | R · FactoMineR | Projet de M1, solo |
| [Explicabilité LIME et SHAP](projet_tp_Feature_base_XAI/) | Expliquer un modèle d'attrition bancaire → forêt aléatoire, LIME, SHAP | Accuracy **0,92** ; instabilité de LIME mesurée, SHAP déterministe | scikit-learn · SHAP · LIME | Projet de M1, solo |
| Régression pénalisée (Lasso) | Sélectionner les bonnes variables → Ridge, Lasso et Elastic-Net sur données simulées, puis Lasso logistique et stability selection sur des données publicitaires | Les deux vraies variables retenues 100 fois sur 100 ; 2 variables stables sur les données réelles | R · glmnet · caret | TP noté de M1 · dossier pas encore publié |
| [Ontologie du domaine musical](Projet_MC_Ontologie_Musical/) | Modéliser musiciens, groupes et récompenses dans le temps → OWL, FOAF, relations n-aires | 9 requêtes SPARQL et SQWRL réussies sur 10 | Protégé · OWL · SPARQL | Projet de M1, solo |
| [Jeu multijoueur Okey](Okey/) | Jeu de tuiles en ligne pour 2 à 4 joueurs → client Unity, API REST et serveur temps réel, Docker | Application complète développée à 10 (GitLab flow, revues de code) | C# · Unity · ASP.NET Core · MySQL | Projet de L3, équipe de 10 · rôle : client de jeu Unity |
| [Compilateur AFSTAT](Compilateur_analyse_statistique_afstat/) | Langage dédié à l'analyse de fichiers CSV → lexer, parser, génération de code C | Chaîne complète testée sur 8 programmes | C · flex · bison | Projet de M1, équipe de 4 · rôle : lexer, parser et table des symboles |
| [Audit de sécurité CVE-2017-11774](Audit_Securite_CVE-2017-11774/) | Évaluer l'impact d'une faille Outlook sur un système d'information → kill chain, MITRE ATT&CK | Scénario d'attaque et recommandations, priorité P1 | Analyse de risque | Projet de M1, équipe de 3 |

Chaque dossier contient un README qui commence par une fiche technique (tâche, données, protocole, résultat comparé à une baseline), puis détaille la méthode, les limites et la façon d'exécuter le projet. Les chiffres annoncés ont été reproduits dans un environnement neuf, sauf mention contraire.

---

## Compétences

- **Langages** : C, C++, Java, Python, R, SQL (PostgreSQL, Oracle, PL/SQL).
- **Données** : pandas, NumPy ; nettoyage, fusion de sources multiples, analyse exploratoire, feature engineering.
- **Machine learning** : apprentissage supervisé (classification, régression) et non supervisé (clustering, ACP) ; évaluation et validation des modèles (validation croisée, métriques, déséquilibre de classes) ; optimisation des hyperparamètres ; explicabilité (SHAP, LIME) ; scikit-learn, XGBoost.
- **Deep learning et LLM** : PyTorch, TensorFlow, Keras, Hugging Face ; CNN, RNN, LSTM ; prompting, extraction structurée, évaluation de RAG.
- **Statistiques (R)** : tidyverse, FactoMineR, glmnet ; ACP, khi-deux, régression linéaire, Ridge, Lasso.
- **Outils** : Git, Docker, FastAPI, MLflow, pytest, Jupyter, Matplotlib, Seaborn, LaTeX.
- **Notions** : C#, Spark, MongoDB, Power BI, Tableau, Edge AI (TFLite, ONNX), OWL, SPARQL, pipeline RAG.

## Contact

Disponible pour un stage de 6 mois à partir de février 2027, partout en France : [yasmina.dsml@gmail.com](mailto:yasmina.dsml@gmail.com) ou [LinkedIn](https://www.linkedin.com/in/yasmina-farid-93231a196).

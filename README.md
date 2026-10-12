# Yasmina Farid

**Étudiante en M2 Informatique : Connaissances, science de Données  et Intelligence Artificielle (CODIA) — Université de Strasbourg**
Je cherche un **stage de fin d'études en IA / data science /ML** de 5 à 6 mois, à partir de février 2027,dans toute la France.

[LinkedIn](https://www.linkedin.com/in/yasmina-farid-93231a196) · [CV (PDF)](CV_Yasmina_Farid.pdf) · [yasmina.dsml@gmail.com](mailto:yasmina.dsml@gmail.com)

**Ce que je sais faire :** deep learning (CNN, RNN/LSTM) · LLM et prompting (zero/few-shot, extraction structurée) · explicabilité (SHAP, LIME) · machine learning supervisé et non supervisé · préparation de données réelles multi-sources

**Stack :** Python · PyTorch · TensorFlow/Keras · scikit-learn · pandas · Hugging Face Transformers · R · SPARQL · C · C# · Git

---

## Projets

### Projet de recherche : superposition des caractéristiques dans un CNN

**[TER_FSI_CNN](TER_FSI_CNN/)** · Solo · encadré par Anne Jeannin-Girardon (ICube) · [mémoire](TER_FSI_CNN/Memoire_TER_Yasmina_Farid.pdf) · [slides de soutenance](TER_FSI_CNN/Soutenance_Yasmina_Farid.pdf)

- **Contexte.** Sur un appareil à ressources limitées, un CNN doit être précis et léger. Les choix d'architecture réduisent le coût de calcul, mais ils changent aussi la façon dont le réseau organise ses caractéristiques en interne.
- **Question.** Le design d'un bloc convolutif influence-t-il l'organisation des caractéristiques, et qu'est-ce que cela suggère sur la superposition (plusieurs caractéristiques stockées dans les mêmes directions) ?
- **Protocole.** 3 CNN comparables (bloc classique, bottleneck standard 8 → 4 → 8, bottleneck inversé 8 → 16 → 8), mêmes conditions d'entraînement sur CIFAR-10 à 4 classes. Les trois modèles atteignent 67 à 70 % d'accuracy, ce qui rend leurs représentations comparables. La sortie du bloc étudié est mesurée par similarité cosinus entre canaux et par rang effectif.
- **Résultat.** La compression concentre la représentation (rang effectif **4,1/8**, paires de canaux très alignées) ; l'expansion la distribue (**7,7/8**, aucune paire au-delà de 0,5). Le bloc classique montre déjà du chevauchement : il n'est pas propre aux bottlenecks.
- **Limite et suite.** Ces mesures sont indirectes : elles suggèrent un chevauchement mais ne prouvent pas la superposition. Piste proposée : des sparse autoencoders pour identifier les caractéristiques réellement partagées.

PyTorch · torchvision · torcheval · LaTeX

### IA : deep learning, LLM, NLP, explicabilité

| Projet | Problème → méthode | Résultat clé | Stack | Cadre · rôle |
|---|---|---|---|---|
| [Extraction de fiches techniques par LLM](Extraction_carac_batterie_par_LLM/) | Transformer des fiches PDF de batteries en JSON (20 champs) → Mistral Small 24B, zero-shot vs few-shot, contrôle des hallucinations | F1 moyen **0,85 → 0,87** avec un exemple dans le prompt (4 fiches) ; package testé (24 tests pytest) | Python · LLM API · pytest · pdfplumber | Groupe de 3 · rôle : extraction, prompts et évaluation |
| [Explicabilité LIME vs SHAP](projet_tp_Feature_base_XAI/) | Expliquer un modèle de churn bancaire → forêt aléatoire + LIME + SHAP | Accuracy **0,92** ; instabilité de LIME mesurée, SHAP déterministe | scikit-learn · SHAP · LIME | Solo |
| [Prédiction d'emoji (NLP)](Emoji_prediction_LR_RNN_LSTM/) | Associer une phrase à un emoji (5 classes) → back-traduction, GloVe, régression logistique vs RNN vs LSTM | Fuite d'information localisée et corrigée : validation croisée de la régression logistique ramenée de 0,73 à **0,64**, le LSTM devient le meilleur (**0,69**) | Keras · Hugging Face · scikit-learn | Groupe de 3 · rôle : augmentation des données et modèles |

### Machine learning et data science

| Projet | Problème → méthode | Résultat clé | Stack | Cadre · rôle |
|---|---|---|---|---|
| [Gravité des accidents de la route](Prediction-gravite-accidents-routiers/) | Prédire si un accident est grave → fusion de 6 sources publiques, 6 classifieurs comparés | F1 « grave » **0,665** sur 10 881 accidents de test (baseline : 0,53) ; stable en validation croisée (0,654 ± 0,005) | pandas · scikit-learn | Groupe de 3 · rôle : fusion et nettoyage des données |
| [Classification de cultures (Sentinel-2)](Classification_Cultures_Regression_Logistique/) | 20 types de cultures depuis des séries d'images satellites → features par mois + NDVI, régression logistique | F1 macro **0,48** en test contre 0,05 au hasard (20 classes déséquilibrées) ; 0,61 en validation croisée | scikit-learn · imbalanced-learn | Groupe de 3 · rôle : features et régression logistique |
| [Qualité de l'eau en France](Analyse_qualite_eau_2022_Kmeans_PCA/) | Regrouper des stations de mesure → K-means, ACP, jointure spatiale | 3 321 stations, 4 groupes (silhouette **0,26**) | scikit-learn · GeoPandas | Groupe de 2 · rôle : préparation et clustering |
| [Génomique du cancer du sein (METABRIC)](Projet_tp_Genomique_METABRIC/) | Regrouper 1 092 patientes par expression génique → ACP, K-means, tests du χ² | Clusters fortement liés au sous-type moléculaire (p ≈ 10⁻²⁰²) | R · FactoMineR | Solo |

### Autres compétences : génie logiciel, web sémantique, compilation, sécurité

| Projet | Problème → méthode | Résultat clé | Stack | Cadre · rôle |
|---|---|---|---|---|
| [Jeu multijoueur Okey](Okey/) | Jeu de tuiles en ligne pour 2 à 4 joueurs → client Unity, API REST et serveur temps réel, Docker, CI/CD | Application complète développée à 10 en GitLab flow (revues de code, intégration continue) | C# · Unity · ASP.NET Core · MySQL · Docker | Groupe de 10 · rôle : client de jeu Unity |
| [Ontologie du domaine musical](Projet_MC_Ontologie_Musical/) | Modéliser musiciens, groupes et récompenses dans le temps → OWL, FOAF, relations n-aires | 8/8 requêtes SPARQL validées | Protégé · OWL · SPARQL | Solo |
| [Compilateur AFSTAT](Compilateur_analyse_statistique_afstat/) | Langage dédié à l'analyse de CSV → lexer, parser, génération de code C | Chaîne complète testée sur 8 programmes | C · flex · bison | Groupe de 4 · rôle : lexer, parser et table des symboles |
| [Audit de sécurité CVE-2017-11774](Audit_Securite_CVE-2017-11774/) | Évaluer l'impact d'une faille Outlook sur un SI → kill chain, MITRE ATT&CK | Scénario d'attaque + recommandations, priorité P1 | Analyse de risque | Groupe de 3 · rôle : analyse de la faille et recommandations |

Chaque dossier contient un README qui commence par une fiche technique (tâche, données, protocole, résultat comparé à une baseline), puis détaille la méthode, les limites et la façon d'exécuter le projet. Les chiffres annoncés ont été reproduits dans un environnement neuf, sauf mention contraire.

---

## Contact

Disponible pour un stage à partir de février 2027. Le plus simple pour me joindre : [yasmina.dsml@gmail.com](mailto:yasmina.dsml@gmail.com) ou [LinkedIn](https://www.linkedin.com/in/yasmina-farid-93231a196).

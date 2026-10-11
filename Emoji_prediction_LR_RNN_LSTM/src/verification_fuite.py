"""Évaluation 4 du README : localisation de la fuite d'information.

Lancer depuis le dossier src/ :  python verification_fuite.py

Le script exécute les cellules de chargement de lstm.ipynb (données, tokenizer,
GloVe, jeu de test) pour utiliser exactement le même prétraitement, puis compare :
  1. la validation croisée du notebook (StratifiedKFold sur les 274 phrases) ;
  2. une validation croisée sans fuite (StratifiedGroupKFold), où chaque phrase,
     ses paraphrases back-traduites et ses doublons restent dans le même fold ;
  3. l'accuracy sur le test avec et sans les phrases déjà présentes à l'entraînement.
Les modèles (architecture, epochs, batch, graines) sont ceux du notebook.
"""
import difflib
import json
import os

import numpy as np
import pandas as pd


def rattacher_paraphrases(enrichi, augmente):
    """Associe chaque paraphrase à sa phrase source.

    data_augmentation.ipynb parcourt les phrases des classes 0, 1 et 4 dans l'ordre
    et ajoute les paraphrases retenues à la fin du fichier, dans le même ordre.
    On aligne donc les deux séquences de façon monotone (programmation dynamique),
    en maximisant la similarité entre paraphrase et source de même classe.
    """
    nouvelles = augmente.iloc[len(enrichi):].reset_index(drop=True)
    sources = [(i, r.sentence, r.emoji) for i, r in enrichi.iterrows() if r.emoji in (0, 1, 4)]
    n, m = len(nouvelles), len(sources)
    sim = np.full((n, m), -1e9)
    for a, (phrase, classe) in enumerate(zip(nouvelles.sentence, nouvelles.emoji)):
        for b, (_, source, classe_source) in enumerate(sources):
            if classe == classe_source:
                sim[a, b] = difflib.SequenceMatcher(None, phrase.lower(), source.lower()).ratio()
    score = np.full((n + 1, m + 1), -1e18)
    score[0, :] = 0
    for a in range(1, n + 1):
        for b in range(1, m + 1):
            score[a, b] = max(score[a, b - 1], score[a - 1, b - 1] + sim[a - 1, b - 1])
    a, b, appariement = n, m, [None] * n
    while a > 0:
        if score[a, b] == score[a, b - 1]:
            b -= 1
        else:
            appariement[a - 1] = b - 1
            a, b = a - 1, b - 1
    return list(range(len(enrichi))) + [sources[k][0] for k in appariement]


def main():
    nb = json.load(open("lstm.ipynb"))
    cellules = {i: "".join(c["source"]) for i, c in enumerate(nb["cells"]) if c["cell_type"] == "code"}
    g = {"__name__": "__main__"}
    for i in (1, 2, 3, 10):
        exec(cellules[i], g)

    from sklearn.feature_extraction.text import CountVectorizer
    from sklearn.linear_model import LogisticRegression
    from sklearn.model_selection import StratifiedGroupKFold, StratifiedKFold
    from sklearn.pipeline import make_pipeline
    import tensorflow as tf
    from tensorflow.keras.layers import LSTM, Dense, Embedding, SimpleRNN
    from tensorflow.keras.models import Sequential

    x, y, X = g["x"], g["y"], g["X"]
    enrichi = pd.read_csv(os.path.join(g["data_path"], "train_emoji_enrichi.csv"))
    groupes_source = rattacher_paraphrases(enrichi, g["df"])
    # les doublons exacts (insensibles à la casse) rejoignent le groupe de leur première occurrence
    cle = pd.Series(x).str.lower().str.strip()
    premier = {}
    groupes = np.array([premier.setdefault(k, gr) for k, gr in zip(cle, groupes_source)])

    def modele(couche):
        return Sequential([
            Embedding(g["vocab_size"], g["embedding_dim"], weights=[g["embedding_matrix"]], trainable=False, mask_zero=True),
            couche(32),
            Dense(5, activation="softmax"),
        ])

    def entrainer(couche, X_tr, y_tr):
        tf.keras.backend.clear_session()
        g["reset_random_seed"](42)
        m = modele(couche)
        m.compile(loss="sparse_categorical_crossentropy", optimizer="adam", metrics=["accuracy"])
        m.fit(X_tr, y_tr, epochs=30, batch_size=16, shuffle=False, verbose=0)
        return m

    def validation_croisee(folds, nom):
        jumeaux = sum(int(np.isin(groupes[va], groupes[tr]).sum()) for tr, va in folds)
        lr = [make_pipeline(CountVectorizer(), LogisticRegression(max_iter=1000)).fit(x[tr], y[tr]).score(x[va], y[va]) for tr, va in folds]
        rnn = [entrainer(SimpleRNN, X[tr], y[tr]).evaluate(X[va], y[va], verbose=0)[1] for tr, va in folds]
        lstm = [entrainer(LSTM, X[tr], y[tr]).evaluate(X[va], y[va], verbose=0)[1] for tr, va in folds]
        print(f"{nom} : phrases de validation avec un jumeau en entraînement = {jumeaux}/{len(x)}")
        print(f"  LR {np.mean(lr):.4f} | RNN {np.mean(rnn):.4f} | LSTM {np.mean(lstm):.4f}")

    validation_croisee(list(StratifiedKFold(n_splits=5, shuffle=True, random_state=42).split(X, y)), "CV du notebook")
    validation_croisee(list(StratifiedGroupKFold(n_splits=5, shuffle=True, random_state=42).split(X, y, groupes)), "CV sans fuite")

    x_test, y_test, X_test = g["x_test"], g["y_test"], g["X_test"]
    inedites = np.array([s.lower().strip() not in set(cle) for s in x_test])
    print(f"Test : {int((~inedites).sum())}/{len(x_test)} phrases déjà présentes dans l'entraînement")
    lr = make_pipeline(CountVectorizer(), LogisticRegression(max_iter=1000)).fit(x, y)
    print(f"  LR   complet {lr.score(x_test, y_test):.4f} | inédites {lr.score(x_test[inedites], y_test[inedites]):.4f}")
    for nom, couche in (("RNN", SimpleRNN), ("LSTM", LSTM)):
        m = entrainer(couche, X, y)
        print(f"  {nom:4} complet {m.evaluate(X_test, y_test, verbose=0)[1]:.4f} | inédites {m.evaluate(X_test[inedites], y_test[inedites], verbose=0)[1]:.4f}")


if __name__ == "__main__":
    main()

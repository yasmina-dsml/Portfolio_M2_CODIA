# Données non incluses

Les fichiers `train_images.npy` (~586 Mo) et `test_public_images.npy` (~147 Mo) sont
exclus de ce dépôt car ils dépassent la limite de 100 Mo par fichier imposée par
GitHub. Seules les étiquettes (`train_labels.npy`, `test_public_labels.npy`, quelques
dizaines de Ko) sont conservées, ce qui permet d'exécuter les analyses de distribution
des classes sans les images.

Les images proviennent du même jeu de données que le projet de groupe sur la
classification par CNN (sujet fourni par les enseignants du cours, Sentinel-2, cultures
agricoles). Sans elles, les notebooks de prétraitement et de modélisation
(`02_pretraitement_reechantillonnage.ipynb`, `03_regression_logistique.ipynb`)
s'arrêteront à l'étape de chargement des images.

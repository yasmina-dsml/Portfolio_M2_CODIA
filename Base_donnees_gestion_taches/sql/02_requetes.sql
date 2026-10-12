-- =====================================================================
-- Base de données d'une application de gestion des tâches (Oracle)
-- Script 2 : requêtes d'interrogation
--
-- À lancer après 01_creation_tables.sql. Chaque requête répond à une
-- question qu'un utilisateur ou un chef de projet se poserait dans l'application.
-- =====================================================================

SET LINESIZE 200
SET PAGESIZE 100

-- ---------------------------------------------------------------------
-- R1. Tableau de bord d'un projet : tâches en cours, par liste, avec les personnes assignées
--     (jointures multiples, agrégation de chaînes)
-- ---------------------------------------------------------------------
SELECT p.nom AS projet,
       l.titre AS liste,
       t.intitule,
       t.priorite,
       t.statut,
       TO_CHAR(t.deadline, 'DD/MM/YYYY') AS echeance,
       LISTAGG(u.prenom || ' ' || u.nom, ', ') WITHIN GROUP (ORDER BY u.nom) AS assignes
FROM projet p
JOIN liste_de_taches l ON l.id_projet = p.id_projet
JOIN tache_active t ON t.id_liste = l.id_liste
LEFT JOIN assignation a ON a.id_tache = t.id_tache
LEFT JOIN utilisateur u ON u.id_user = a.id_user
WHERE p.id_projet = 1
GROUP BY p.nom, l.titre, t.intitule, t.priorite, t.statut, t.deadline
ORDER BY l.titre, t.deadline NULLS LAST;

-- ---------------------------------------------------------------------
-- R2. Tâches en retard : échéance dépassée, avec le nombre de jours de retard
-- ---------------------------------------------------------------------
SELECT t.id_tache,
       t.intitule,
       t.priorite,
       TRUNC(SYSDATE) - TRUNC(t.deadline) AS jours_de_retard
FROM tache_active t
WHERE t.deadline < SYSDATE
ORDER BY jours_de_retard DESC;

-- ---------------------------------------------------------------------
-- R3. Charge de travail par utilisateur, y compris ceux qui n'ont aucune tâche
--     (LEFT JOIN + agrégation conditionnelle)
-- ---------------------------------------------------------------------
SELECT u.login,
       COUNT(t.id_tache) AS taches_actives,
       SUM(CASE WHEN t.priorite = 'haute' THEN 1 ELSE 0 END) AS dont_priorite_haute,
       SUM(CASE WHEN t.deadline < SYSDATE THEN 1 ELSE 0 END) AS dont_en_retard
FROM utilisateur u
LEFT JOIN assignation a ON a.id_user = u.id_user
LEFT JOIN tache_active t ON t.id_tache = a.id_tache
GROUP BY u.login
ORDER BY taches_actives DESC, u.login;

-- ---------------------------------------------------------------------
-- R4. Utilisateurs sans aucune tâche assignée (sous-requête NOT EXISTS)
-- ---------------------------------------------------------------------
SELECT u.prenom, u.nom, u.login
FROM utilisateur u
WHERE NOT EXISTS (SELECT 1 FROM assignation a WHERE a.id_user = u.id_user);

-- ---------------------------------------------------------------------
-- R5. Avancement de chaque projet : part des tâches terminées
--     (UNION ALL entre tâches actives et terminées, puis agrégation)
-- ---------------------------------------------------------------------
WITH toutes_taches AS (
  SELECT id_liste, 0 AS terminee FROM tache_active
  UNION ALL
  SELECT id_liste, 1 AS terminee FROM tache_terminee
)
SELECT p.nom AS projet,
       COUNT(*) AS total_taches,
       SUM(tt.terminee) AS terminees,
       ROUND(100 * SUM(tt.terminee) / COUNT(*), 1) AS pct_avancement
FROM projet p
JOIN liste_de_taches l ON l.id_projet = p.id_projet
JOIN toutes_taches tt ON tt.id_liste = l.id_liste
GROUP BY p.nom
ORDER BY pct_avancement DESC;

-- ---------------------------------------------------------------------
-- R6. Projets entièrement terminés : plus aucune tâche active dans leurs listes
-- ---------------------------------------------------------------------
SELECT p.nom
FROM projet p
WHERE NOT EXISTS (
  SELECT 1
  FROM liste_de_taches l
  JOIN tache_active t ON t.id_liste = l.id_liste
  WHERE l.id_projet = p.id_projet
);

-- ---------------------------------------------------------------------
-- R7. Chaîne de dépendances d'une tâche : tout ce qui doit être fini avant
--     la mise en production (requête récursive)
-- ---------------------------------------------------------------------
WITH chaine (id_tache, niveau) AS (
  SELECT id_parente, 1 FROM tache_parente WHERE id_dependante = 4
  UNION ALL
  SELECT tp.id_parente, c.niveau + 1
  FROM tache_parente tp
  JOIN chaine c ON tp.id_dependante = c.id_tache
)
SELECT c.niveau, t.id_tache, t.intitule, t.statut
FROM chaine c
JOIN tache_active t ON t.id_tache = c.id_tache
ORDER BY c.niveau;

-- ---------------------------------------------------------------------
-- R8. Tâches réellement bloquées : dépendantes dont au moins une tâche parente est encore active
-- ---------------------------------------------------------------------
SELECT d.intitule AS tache_bloquee,
       LISTAGG(p.intitule, ', ') WITHIN GROUP (ORDER BY p.intitule) AS attend
FROM tache_parente tp
JOIN tache_active d ON d.id_tache = tp.id_dependante
JOIN tache_active p ON p.id_tache = tp.id_parente
GROUP BY d.intitule
ORDER BY d.intitule;

-- ---------------------------------------------------------------------
-- R9. Classement des utilisateurs par score (fonction analytique RANK)
-- ---------------------------------------------------------------------
SELECT RANK() OVER (ORDER BY s.score DESC) AS rang,
       u.login,
       s.score,
       s.niveau
FROM score_niveau s
JOIN utilisateur u ON u.id_user = s.id_user
ORDER BY rang;

-- ---------------------------------------------------------------------
-- R10. Prochaine occurrence des tâches périodiques encore dans leur période
-- ---------------------------------------------------------------------
SELECT t.intitule,
       pe.intervalle AS tous_les_n_jours,
       TO_CHAR(pe.date_debut
               + pe.intervalle * CEIL((TRUNC(SYSDATE) - TRUNC(pe.date_debut)) / pe.intervalle),
               'DD/MM/YYYY') AS prochaine_occurrence
FROM periode pe
JOIN tache_active t ON t.id_tache = pe.id_tache
WHERE pe.date_fin IS NULL OR pe.date_fin >= SYSDATE;

-- ---------------------------------------------------------------------
-- R11. Respect des délais sur les tâches terminées, par utilisateur
-- ---------------------------------------------------------------------
SELECT u.login,
       COUNT(*) AS taches_terminees,
       SUM(CASE WHEN tt.date_fin <= tt.deadline THEN 1 ELSE 0 END) AS dans_les_temps,
       ROUND(100 * AVG(CASE WHEN tt.date_fin <= tt.deadline THEN 1 ELSE 0 END), 1) AS pct_dans_les_temps
FROM tache_terminee tt
JOIN utilisateur u ON u.id_user = tt.id_createur
GROUP BY u.login
ORDER BY pct_dans_les_temps DESC;

-- ---------------------------------------------------------------------
-- R12. Répartition des tâches actives par priorité et statut, avec sous-totaux (ROLLUP)
-- ---------------------------------------------------------------------
SELECT NVL(priorite, 'TOTAL') AS priorite,
       NVL(statut, 'tous statuts') AS statut,
       COUNT(*) AS nb_taches
FROM tache_active
GROUP BY ROLLUP (priorite, statut)
ORDER BY priorite, statut;

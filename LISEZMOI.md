# M.G-S.U.G.E — projet Unity (jalons 0 à 6)

Châssis du joueur : **GX-55 « Vesper »**, construit à partir de `Mecha_GX_55_00_Proto.blend`.
Code : namespace `MGSUGE`, données dans des ScriptableObjects (`Assets/_MGSUGE/Data`), textes du jeu en français.

## 1. Ouvrir et jouer

Les scènes sont **déjà construites et enregistrées** dans `Assets/_MGSUGE/Scenes/` :
- **World.unity** (n° 0 dans *File > Build Profiles*) : le monde complet, les 7 contrats, les 36 avant-postes, les patrouilles. Démarre sur l'**écran titre** (Entrée ou « Mode normal » / « Mode DIVIN »).
- **Test.unity** (n° 1) : terrain plat, cibles d'entraînement, un ennemi de chaque type, un contrat d'essai et un avant-poste d'essai. Démarre directement en jeu (`GameManager → Start On Title` décoché).

L'écran titre fait partie de la scène World (le cahier, §4.4, veut le monde affiché derrière, avec la caméra qui tourne autour du châssis) : il n'y a donc pas de scène Title séparée.

Double-cliquer la scène → **Play**. Cliquer dans la vue Game pour capturer la souris. Rien n'est généré au lancement : tout ce que vous voyez dans la Hierarchy est dans la scène.

Hierarchy (les deux scènes) :
```
Managers   GameManager · Contrats (ContractManager) · Patrouilles (PatrolSpawner) · FX (effets, tirs, missiles, obus) > Particules
Lights     Lumiere_Soleil · Lumiere_Remplissage · Volume_Global
Player     Point_Depart · MGSUGE_Player (prefab) · Camera
World      Ciel > Astres (planète, anneau, lune, étoiles) · Terrain > 64 tuiles · Mer · Lave > Lave_Lumiere
           Arcologie_Vesta (flèche, gyroscopes, halo, 51 tours MGSUGE_Tour, passerelles, plateforme Hélios)
           Zones (décors provisoires) : pylônes de Kessler, épave du Thalassa, ascenseur orbital, 5 stations flottantes, 3 croiseurs
Enemies    Patrouilles (reçoit les patrouilles errantes en jeu) · [Test] Cibles_Essai · Ennemis_Essai
Contracts  Contrat_1_Lyra … Contrat_7_Ceinture (leurs ennemis sont les enfants, désactivés tant que le contrat n'est pas en cours)
Outposts   AvantPoste_01_Patrouille … AvantPoste_36_Mixte (garnison = enfants désactivés)
Bosses     (jalon 7)
UI         EventSystem · HUD (Canvas : objectif, fil de messages, crédits, barre de boss, cible, vitesse, armure/énergie, armes, mini-carte, grand message, vignette)
           Surimpression (Canvas : réticule, horizon, verrouillage, marqueurs, coups reçus, missiles entrants, chiffres de dégâts, arc de lame)
           Ecrans (Canvas : Ecran_Titre, Menu_Pause > Onglet_Ameliorations / Onglet_Commandes, Chassis_Detruit)
```

Menus de l'éditeur (facultatifs) :
- **MGSUGE → 1. Construire tout** : régénère les prefabs et les deux scènes (vos modifications dans ces scènes et prefabs sont alors perdues ; les données de `Data/` sont conservées). À ne relancer qu'après une mise à jour du code ou de la heightmap.
- **MGSUGE → 6. Mettre à jour l'interface** : reconstruit uniquement le groupe UI des deux scènes (le reste des scènes n'est pas touché).
- **MGSUGE → 4 / 5. Test automatique** (scène Test / scène World) : entrées simulées, rapport et captures dans `Logs/AutoTest` du projet.
- **MGSUGE → Avancé** : reconstruire seulement les prefabs, seulement World ou seulement Test ; réimporter le châssis.

Créer de nouvelles données : clic droit dans le dossier Project → **Create → MGSUGE → Donnees → …** (Ennemis, Contrat, Campagne, Avant-postes, Patrouilles, Armes, Monde…).

## 2. Ce qui est livré, jalon par jalon

### Jalon 0 — analyse de la figurine (à valider)
Source : photo principale de la partie 2 (planche face / profil / dos / 3-4) et modèle Proto.
- **Proportions** (hauteur ≈ 12 m au sommet de la tête, 12,9 m avec les ailettes) : tête ≈ 1/9 de la hauteur ; bassin à 7 m du sol ; jambes longues et massives ; épaules très larges (≈ 6 m hors tout), carrées, avec grille avant et panneau latéral « GX-55 ».
- **Silhouette** : buste en V inversé avec plastron central en pointe, éventail de **6 drones-ailettes** dans le dos, grand **bouclier** à gauche portant une nacelle à missiles, **fusil long** à droite, **sabre** rangé dans le dos.
- **Articulations** : cou, épaules, coudes, poignets, doigts (2 phalanges), hanches, genoux, chevilles, orteils, jupes avant / latérales / arrière ; bras du support de drones (ailes).
- **Parties mobiles** : drones détachables (sondes laser), couvercle de nacelle, sabre (dos → main gauche), nez du vaisseau (rentré en châssis).
- **Hors photo** (proposé, à valider) : dessous du vaisseau = face avant du châssis ; flammes arrière émises par la grille existante de la jupe arrière (aucune tuyère tournée vers l'arrière n'existe sur le modèle).

### Jalon 2 — modèle Blender (complet)
Fichiers : `Blender/MGSUGE_Vesper.blend` (collections **Mesh / Rig / Refs**), `Art/Models/MGSUGE_Vesper_Mech.fbx`, `MGSUGE_Vesper_Poses.json`, `MGSUGE_Vesper_Palette.json`, et le script `build_vesper.py` qui regénère tout depuis le fichier d'art.
- **Échelle** : 1 unité = 1 m ; pivot au centre du bassin ; centre → sol = **7 m** (validé par l'auteur le 28/10 : châssis de 12 m ; le prototype utilisait 5,7 m). Facteur appliqué au modèle d'art : 0,6161 (uniforme).
- **Fidèle au .blend** : biseaux (2 segments), normales durcies, lissage et matériaux d'origine conservés ; pièces rigides fusionnées **par os** (69 maillages). Contrôle : rendu du Proto et du modèle de jeu dans la même scène, même caméra → seule différence, le fusil passé de la hanche à la main (planche `MGSUGE_Fidelite_Proto_vs_Jeu.png`).
- **98 428 triangles** : au-dessus de la cible du cahier (25 000 – 40 000) car les biseaux du modèle d'art sont conservés ; des niveaux de détail (LOD1 / LOD2 à 40 % / 15 %) sont à produire au jalon 10 pour les châssis vus de loin (ennemis, as).
- **Squelette** (75 os, noms du cahier) :

| Cahier | Os / objet |
|---|---|
| Root, Body (bassin), Torso, Head | `Root`, `Body`, `Spine` (abdomen), `Torso`, `Neck`, `Head` |
| Pack, Thruster_Rear_L/R, Thruster_Down_L/R | `Pack` ; `Thruster_Down_*` = grosses tuyères du sac ; `Thruster_Rear_*` = points d'émission sur la **grille de la jupe arrière** (pas de géométrie ajoutée) ; en plus `Thruster_Leg_*_1/2` (mollets) |
| Wing_L / Wing_R | `Wing_L/R` = bras porte-drones (gauche : drones 1-3, droite : 4-6) |
| Arm_R : Shoulder > Elbow > Hand | `Clavicle_R` > `Shoulder_R` > `Elbow_R` > `Hand_R` (+ doigts) |
| Rifle + Muzzle | `Rifle` (dans la main droite) > `Muzzle` |
| Arm_L : Shoulder > Elbow > Hand | `Clavicle_L` > `Shoulder_L` > `Elbow_L` > `Hand_L` |
| Blade (émetteur) | `Blade` = le sabre (rangé sur `Pack`), `BladeGrip` = prise dans la main gauche ; `Blade_Energy` = lame d'énergie **d'origine du .blend** (3,8 m à l'échelle de jeu ; le cahier indique 7 m × 0,18 m : à trancher) |
| MuzzleL | `MuzzleL` = pointe du **bouclier** |
| MissilePod + Lid + PodOut | sur le **bouclier** : `MissilePod` > `Lid` (couvercle ajouté, ouverture 72°) et `PodOut` |
| Leg : Hip > Knee > Ankle | `Hip_*` > `Knee_*` > `Ankle_*` > `Toe_*` |
| Nose + NoseTip | `Nose` (cône ajouté, caché en châssis) > `NoseTip` (pointe lumineuse) |
| (sondes) | `Probe_1` … `Probe_6` = les drones |

- **Poses** (actions Blender + JSON lu par Unity) : `Pose_Mech`, `Pose_Ship`, `Pose_Crouch`, `Pose_BladeStrike` (+ `LidOpen`). Ce sont des décalages en espace modèle ; Unity les applique par code (pas de clip figé), comme le prototype.
- **Matériaux** : rangés par rôle `MGSUGE_Vesper_<Rôle>[_nuance]` (Armor, Frame, Accent, Visor, Glow, Flame, Blade). `PaletteApplier` recolore les 7 rôles sans dupliquer le maillage ; palettes fournies : Joueur (couleurs d'origine), Corbeau, As A, As B, Titan, Archonte.

### Jalon 1 — monde (partiel)
Fait : terrain 15 km à facettes (carte 513 × 513, tuiles de 64 cellules, couleurs de biome, collisions), mer animée à −42 m, plancher −43,4 m, lave à −16 m (croûte mouvante, −380 AP/s), ciel crépusculaire, soleil / remplissage / ambiance hémisphérique / brouillard exponentiel carré (densité 0,00019), passage à l'espace (3 600 → 7 200 m), planète à anneaux, lune, 7 100 étoiles, plafond 12 000 m, limite ±7 440 m, noms des 13 secteurs, arcologie Vesta (flèche de 780 m, anneaux, balise, gyroscopes, halo, 51 tours, passerelles) et **plateforme Hélios** (départ).
**Terrain provisoire** : le HTML source n'étant pas joint, le relief vient d'un générateur qui respecte les repères du cahier ((0;0) = 10 m, Hélios = 14 m, cratère de Kessler, crêtes, glacier, 4 volcans, marais, erg, mur de 520 m) mais **pas le relief exact**. Pour le terrain exact : exporter `heightmap_513_min…_max….raw` depuis le HTML (script du §6.3), le déposer dans `Assets/StreamingAssets/MGSUGE/` et renseigner son nom dans `Data/MGSUGE_World` → *Heightmap File* (cases *Flip X / Flip Z* pour corriger l'orientation).
Reste : les zones et décors hors arcologie, les structures mobiles porteuses, les animations du monde.

### Jalon 3 — déplacements (complet, à tester)
Bassin à 7 m du sol (capsule de collision de −7 m à +3,2 m). Marche 34 m/s, boost sol 72 / air 70 (plané, gravité 16 %, chute ≤ 9 m/s), saut boost 84 m/s × 0,22 s (petit saut 30 m/s sans EN), montée 46, plongée et descente 64,4, quick boost 150 m/s × 0,24 s, assault boost 190 m/s vers le point visé (S ou R pour arrêter), énergie (1 000, recharge 420/s après 0,45 s, surchauffe 1,6 s puis recharge ×1,5), atterrissage (accroupi jusqu'à 1,1 m, secousse, poussière), cycle de marche et inclinaison, **transformation T en 0,75 s**, vaisseau 6 degrés de liberté (300 / 440 m/s, recul 70, glissement 90, tonneau d'esquive, remise à niveau après 0,9 s, flèches), caméra (21 m, épaule 3,2, élévation 5,2, FOV 72 + effets, vaisseau 26 m, jamais sous le sol, point visé jusqu'à 1 200 m), flammes de tuyères selon la poussée, lave, mort et redéploiement.
Toutes les valeurs sont dans `MGSUGE_FrameStats` et `MGSUGE_Camera`.

### Jalon 4 — armes (complet, à tester sur le terrain d'essai)
Fusil (115, 0,105 s, 680 m/s, anticipation × 0,9, alternance des bouches en vaisseau), missiles multi-cibles depuis la **nacelle du bouclier** (couvercle animé), salve C, fragmentation V (porteur + mini-missiles), lame F (charge 270 m/s, sabre pris dans le dos), verrouillage automatique et touche X, la tête suit la cible, jauge d'impact et surcharge (×1,6), chiffres de dégâts, **sondes laser = les drones du dos** (ils se détachent, encerclent la cible et tirent en jaune ; maximum 5, le 6e reste à poste), pools 700 tirs / 160 missiles, sons provisoires synthétisés.
Le **terrain d'essai** est maintenant la scène **Test.unity** (cibles d'entraînement fixes, ennemis de chaque type, contrat et avant-poste d'essai).
Touches de test (désactivables : `GameManager → Test Keys`) : **F1** +1 000 000 C, **F2** +1 sonde, **F3** toutes les améliorations +1 niveau, **F4** plein d'AP / EN / kits. Le menu **U** permet déjà d'acheter les 11 améliorations (valeurs du §11.2 vérifiées) et les sondes.

### Jalon 5 — ennemis, contrats, avant-postes, patrouilles, menace (complet, testé dans Unity)
- **6 types d'ennemis** (prefabs `Prefabs/Ennemis`, réglages `Data/Ennemis`) : Sentinelle volante (650 AP, 120 C), Relais de brouillage (5 200 AP, 400 C), Tourelle de défense (1 800 AP, 250 C), Chasseur « Faucon » (1 500 AP, 300 C), Araignée de siège (3 000 AP, 450 C, obus d'artillerie avec télégraphe au sol), Châssis « Corbeau » (4 200 AP, 900 C, même modèle que le joueur, palette hostile, quick boosts et missiles).
- **Engagement** : 750 m (950 m pour un ennemi de contrat, 1 100 m pour les chasseurs), désengagement × 1,6, discrétion au-delà de 3 200 m.
- **Contrats** (objets `Contracts/Contrat_N_…`, données `Data/Contrats`) : les 7 contrats du cahier (2 600 → 4 400 C), leurs ennemis sont les enfants du contrat dans la Hierarchy ; marqueur d'objectif dans le HUD ; contrat suivant 5 s après la fin ; après le 7e : menace +1 et retour au contrat 1.
- **Menace** (`Data/MGSUGE_Campagne`) : PV × (1 + 0,3 m), dégâts × (1 + 0,2 m), récompenses × (1 + 0,3 m).
- **36 avant-postes** (graine 90210, > 1 500 m d'Hélios, ≥ 850 m entre eux) : garnison activée à 1 700 m (1 300 m dans l'espace), désactivée à 2 700 m, prime +400 C, réarmement après 150 s. 5 modèles (Patrouille, Aérien, Blindé, Siège, Mixte) dans `Prefabs/AvantPostes`.
- **Patrouilles errantes** (`Managers/Patrouilles`, `Data/MGSUGE_Patrouilles`) : toutes les 16 s (première à 8 s), moins de 8 à la fois, à 800–1 200 m du joueur, jamais à moins de 500 m d'Hélios ; 40 % : 2 chasseurs (160–260 m), sinon 3 sentinelles (50–120 m) ; au-dessus de 3 000 m d'altitude : toujours des chasseurs ; supprimées au-delà de 3 000 m.
- **Redéploiement** : supprime les ennemis hors contrat à moins de 600 m de la plateforme (`GameManager → Redeploy Clear Radius`).
- **Tests automatiques** : scène Test 65 OK / 0 échec ; scène World 29 OK / 0 échec, 0 erreur console, ≈ 78 ips sur la machine de test.

### Jalon 6 — interface, menus, améliorations, modes Normal / DIVIN (complet, testé dans Unity)
- **Polices** (licence libre OFL, `Art/Fonts`) : Saira Extra Condensed 400 / 600 / 800 (titres), Saira (texte), IBM Plex Mono (chiffres). J'y ai ajouté les glyphes ◀ ▶, absents des fichiers d'origine.
- **HUD** (§16.1) en Canvas uGUI, un objet par bloc. Tous les réglages partagés (couleurs §4.5, polices, durées des messages, zooms de la mini-carte, option « réduire les animations ») sont dans `Data/MGSUGE_Interface`.
- **Mini-carte** : disque de 210 px (140 px sous 720 px de haut), orientée comme la caméra, fond = carte du monde précalculée (`Art/UI/MGSUGE_Carte_Monde.png`), zoom M 600 → 1 800 → 4 500 m. Elle affiche les ennemis, l'objectif, les avant-postes et les boss.
- **Surimpression** (§16.2) : réticule qui s'écarte au tir, horizon artificiel en vaisseau, cadre de verrouillage et losanges orange, marqueurs d'objectif et de boss ramenés au bord de l'écran, arcs des coups reçus, alerte « Missiles entrants », chiffres de dégâts.
- **Écrans** (§16.3) :
  - titre : 3 mises en page du nom au choix, champ `Layout` du composant *TitleScreen* ;
  - pause : Reprendre / Améliorations / Commandes, avec apparition échelonnée des lignes ;
  - « Châssis détruit ».
  Les menus se pilotent à la souris et au clavier.
- **Améliorations** (§11.2–11.4) : les 11 modules avec valeur actuelle → suivante, prix 900 / 1 600 / 2 600 / 3 800 / 5 200 C, « Maximum », Sonde 250 000 C, Rayon Arcane 1 000 000 C, Territoire 500 000 C, Territoire II 2 500 000 C (verrouillé tant que l'Archonte n'est pas vaincu), Réincarnation 10 000 000 C (×5).
- **Modes** (§4.3) :
  - Normal : 2 000 C ;
  - DIVIN : tout est débloqué, sélecteurs ◀ ▶ gratuits, réincarnation jusqu'à 12, « Mode DIVIN » en or.
- **Tests automatiques** : Test 104 OK / 0 échec, World 53 OK / 0 échec, 0 erreur console.

## 3. Critères d'acceptation à tester (§18.1)
- Jalon 1 : (0;0) = 10 m, mer à −42 m, altitude affichée = Y + 42.
- Jalon 2 : T transforme en 0,75 s ; toutes les pièces nommées bougent (bras, jambes, ailes-drones, nez, tête).
- Jalon 3 : vitesses affichées en km/h (marche 122, boost sol 259, boost air 252, assault 684, quick 540, vaisseau 1 080, postcombustion 1 584).
- Jalon 4 : valeurs du §9 ; tir continu sans chute de performance ; transformation pendant un saut boost, un assault boost et une salve.
- Jalon 6 : textes et prix des parties 11 et 16 (vérifiés par le test automatique) ; Entrée lance le mode normal ; M change le zoom.
- Jalon 5 : chaque type d'ennemi attaque ; contrats 1 → 7 puis menace +1 ; PV / dégâts / récompenses multipliés par la menace ; avant-postes activés à 1 700 m et récompensés ; patrouilles toutes les 16 s.

## 4. Hypothèses prises (le cahier ne les chiffre pas)
- Accélérations « 6,5 / 4 / 9 / 1,4 » interprétées comme des taux de rapprochement exponentiels (1/s).
- Accélération du vaisseau : 1,6/s (`shipAccel`). Tonneau d'esquive : coût et recharge du quick boost.
- Sondes : orbite de 28 m autour de la cible, 260 m/s. Lame en forme vaisseau : autorisée.
- Recharge du quick boost comptée depuis le début de l'impulsion.
- Jalon 5 (valeurs non chiffrées par le cahier) : sentinelle orbite 50 m/s, accélération 140 ; Corbeau quick boost 0,25 s, altitude de suivi 25 m ; tourelle 140°/s, tir sous 12° d'écart ; chasseur virage 1,6 rad/s, rafale toutes les 0,08 s, remontée 0,45, garde au sol 40 m ; araignée anticipation aléatoire 0,6–1,25, dispersion des obus 14 m ; avant-postes : passage « espace » à 3 600 m, pente maxi 8 m ; 3 tourelles sur la coque du Thalassa ; composition du contrat d'essai.
- Jalon 6 (non chiffré par le cahier) :
  - écran titre : décalage latéral de 9 m, hauteur de la caméra 4 m, rotation 6°/s ;
  - « Missiles entrants » : texte et chevrons autour du centre ;
  - réticule : écart de base de 14 px, +10 px au tir ;
  - DIVIN : les capacités uniques se règlent Oui / Non ;
  - G et B affichent « activation au jalon 8 ».
Toutes ces valeurs sont modifiables dans les ScriptableObjects.

## 5. Limites connues
- Les décors des zones (pylônes, épave, ascenseur, stations, croiseurs) sont provisoires.
- Les avant-postes et contrats sont posés sur le **terrain provisoire** : après dépôt de la vraie heightmap, relancer **MGSUGE → 1. Construire tout** pour les reposer au sol.
- Le cratère de Kessler est sous le niveau de la mer sur le terrain provisoire (il apparaît inondé).
- Les barres d'état des compétences spéciales (Rayon Arcane, Territoire) et la barre de boss sont prêtes, mais elles ne s'animeront qu'aux jalons 7 et 8.
- Pas encore de remappage des touches ni de sauvegarde (jalon 10).
- Les sources audio provisoires sont encore créées par `SfxSystem` au lancement (son final au jalon 10).
- Pas encore : boss (7), compétences spéciales (8), orbite (9), sauvegarde, audio et VFX finaux (10).

## 6. Regénérer le châssis après une retouche du modèle d'art
```
blender -b --python build_vesper.py -- Mecha_GX_55_00_Proto.blend dossier_sortie
```
Puis copier `MGSUGE_Vesper_Mech.fbx`, `MGSUGE_Vesper_Poses.json` et `MGSUGE_Vesper_Palette.json` dans `Assets/_MGSUGE/Art/Models/` et lancer **MGSUGE → Avancé → Réimporter le châssis**, puis **MGSUGE → 1. Construire tout**.
Les noms d'objets du fichier d'art servent au rangement des pièces (sabre, fusil, bouclier, drones, sac) : les conserver.

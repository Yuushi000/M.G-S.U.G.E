# M.G-S.U.G.E — projet Unity (jalons 0 à 4)

Châssis du joueur : **GX-55 « Vesper »**, construit à partir de `Mecha_GX_55_00_Proto.blend`.
Code : namespace `MGSUGE`, données dans des ScriptableObjects (`Assets/_MGSUGE/Data`), textes du jeu en français.

## 1. Installation (5 minutes)

1. Unity Hub → **Nouveau projet** → **Unity 6 LTS (6000.x)** → modèle **« Universal 3D »** (URP + Input System inclus) → nom `MGSUGE`.
2. Fermer Unity, copier le dossier `Assets/_MGSUGE` (et `Assets/StreamingAssets/MGSUGE`) de l'archive dans le dossier `Assets` du projet.
3. Rouvrir le projet. Si Unity demande d'activer le nouvel Input System, accepter (redémarrage).
4. Menu **MGSUGE → 1. Construire le projet (tout)**. Le constructeur crée les couches (Player, World, Enemy), règle le lecteur
   (nom « M.G-S.U.G.E », espace Linear), crée les matériaux, les données, importe le châssis, crée le prefab du joueur et la scène `Scenes/World`.
5. Ouvrir `Assets/_MGSUGE/Scenes/World.unity` → **Play**. Cliquer dans la vue Game pour capturer la souris.

Le constructeur peut être relancé à tout moment : les **données** déjà créées (ScriptableObjects) sont conservées avec vos réglages ; les matériaux, le prefab du joueur et la scène World sont regénérés.

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
Le **terrain d'essai** (au nord d'Hélios) contient 8 sentinelles volantes, 2 relais et 2 châssis « Corbeau » (même modèle, palette hostile) qui ne tirent pas et réapparaissent après 6 s.
Touches de test (désactivables : `GameManager → Test Keys`) : **F1** +1 000 000 C, **F2** +1 sonde, **F3** toutes les améliorations +1 niveau, **F4** plein d'AP / EN / kits. Le menu **U** permet déjà d'acheter les 11 améliorations (valeurs du §11.2 vérifiées) et les sondes.

## 3. Critères d'acceptation à tester (§18.1)
- Jalon 1 : (0;0) = 10 m, mer à −42 m, altitude affichée = Y + 42.
- Jalon 2 : T transforme en 0,75 s ; toutes les pièces nommées bougent (bras, jambes, ailes-drones, nez, tête).
- Jalon 3 : vitesses affichées en km/h (marche 122, boost sol 259, boost air 252, assault 684, quick 540, vaisseau 1 080, postcombustion 1 584).
- Jalon 4 : valeurs du §9 ; tir continu sans chute de performance ; transformation pendant un saut boost, un assault boost et une salve.

## 4. Hypothèses prises (le cahier ne les chiffre pas)
- Accélérations « 6,5 / 4 / 9 / 1,4 » interprétées comme des taux de rapprochement exponentiels (1/s).
- Accélération du vaisseau : 1,6/s (`shipAccel`). Tonneau d'esquive : coût et recharge du quick boost.
- Sondes : orbite de 28 m autour de la cible, 260 m/s. Lame en forme vaisseau : autorisée.
- Recharge du quick boost comptée depuis le début de l'impulsion.
Toutes ces valeurs sont modifiables dans les ScriptableObjects.

## 5. Limites connues
- Le code a été **compilé** (C# 9) contre les déclarations de l'API Unity 6 et ses appels d'API vérifiés contre les définitions officielles, mais **pas encore exécuté dans l'éditeur Unity** : premier lancement à faire de votre côté.
- Les matériaux additifs (flammes, tirs, lame) sont réglés par script ; s'ils apparaissent opaques, ouvrir le matériau et repasser *Surface Type* sur *Transparent / Additive*.
- HUD de travail en IMGUI (police par défaut) ; le HUD final (traits fins, Saira / IBM Plex Mono, mini-carte) est prévu au jalon 6.
- Pas encore : ennemis qui attaquent, avant-postes, contrats (jalon 5), boss (7), compétences spéciales (8), orbite (9), sauvegarde, audio et VFX finaux (10).

## 6. Regénérer le châssis après une retouche du modèle d'art
```
blender -b --python build_vesper.py -- Mecha_GX_55_00_Proto.blend dossier_sortie
```
Puis copier `MGSUGE_Vesper_Mech.fbx`, `MGSUGE_Vesper_Poses.json` et `MGSUGE_Vesper_Palette.json` dans `Assets/_MGSUGE/Art/Models/` et lancer **MGSUGE → 3. Réimporter le châssis et les poses**.
Les noms d'objets du fichier d'art servent au rangement des pièces (sabre, fusil, bouclier, drones, sac) : les conserver.

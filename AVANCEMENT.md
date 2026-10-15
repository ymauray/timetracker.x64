# Avancement

Où en est l'application, ce qui reste à faire, et pourquoi certains choix ont été faits. À lire après [`AGENTS.md`](AGENTS.md) pour reprendre le travail à froid. L'app iPhone [`timetracker.ios`](https://github.com/ymauray/timetracker.ios) a un fichier du même nom ; les deux applications suivent les mêmes règles métier, celles de la CLI [`timetracker`](https://github.com/ymauray/timetracker).

Chaque changement passe par une branche puis une *pull request*, mergée en squash : `main` est protégée (check `build-test` requis, y compris pour l'administrateur). Une version part par un tag : `git tag vX.Y.Z && git push origin vX.Y.Z`.

## Fait

**1. Noyau** — `src/TimeTracker.Core/` reprend le calcul de la CLI (`TimeCalculator`, `ReleveParser`, `ConfigParser`, formats). Les validations d'une journée sont factorisées dans `ValidationJournee`, partagée par le parseur et l'écran d'édition. Les six fixtures de la CLI passent.

**2. Pointage et stockage** — `JourStocke` (zéro à quatre heures ou une absence, lecture selon leur nombre, bouton, annulation, refus), `Bilan` (soldes au soir d'hier), `ReleveWriter` et `ReleveImport` (échange de `releve.md`), `DepotDonnees` (JSON, écriture atomique).

**3. Application WPF** — Pointer (bouton, pointages du jour, réalisé en cours, soldes, alertes), Historique (semaines ISO, édition, ajout, suppression), Réglages (quatre durées, export et import de `releve.md`).

**4. Zone de notification** — icône, menu, fermeture de la fenêtre qui la cache, raccourci global `Ctrl+Alt+P`, instance unique, options `--minimise` et `--smoke`.

**5. Livraison** — icône embarquée (`Assets/pointage.ico`), publication autonome en un seul fichier (`Properties/PublishProfiles/win-x64.pubxml`).

**6. Dépôt** — GitHub public, conformité repocheck 100/100 (README, gouvernance, CI, Dependabot, protection de `main`), release **v0.1.0** publiée le 15 octobre 2026 par `release.yml`.

**Où on en est** (15 octobre 2026) : la v0.1.0 est publiée et l'application a été essayée par Yannick, qui la juge fonctionnelle. Elle n'est pas encore utilisée au quotidien : les retours de cet usage réel sont la prochaine source de travail.

## À faire

Par ordre de priorité conseillé.

1. **Comparer les fixtures avec celles de la CLI** — un job de `ci.yml` qui récupère `ymauray/timetracker` (public) et compare `tests/TimeTracker.Core.Tests/Fixtures/` avec les siennes, comme le fait l'app iPhone. Sans lui, les copies peuvent diverger sans alerte.
2. **Export avec une journée incomplète** — décider du comportement (voir « Décisions »).
3. **Tests de l'interface** — seul `TimeTracker.Core` est testé. Les view-models (`PointerViewModel`, `EditionJourViewModel`, `ReglagesViewModel`) le sont assez facilement, mais `AppState.Maintenant()` lit l'horloge système : prévoir une horloge injectable.
4. **Lancement au démarrage de Windows** — `--minimise` existe déjà ; il manque l'option dans les Réglages (clé `Run` du registre ou raccourci de démarrage).
5. **Signature de l'exécutable** — il n'est pas signé ; SmartScreen peut avertir au premier lancement.
6. **Raccourci global configurable** — `Ctrl+Alt+P` est fixe ; l'app prévient dans une bulle s'il est déjà pris, sans proposer d'alternative.
7. **Plus tard** — jours fériés et congés saisis à la main comme dans la CLI ; une évolution possible est de proposer un calendrier de jours fériés.

## Décisions

- **Stockage** : un fichier JSON (`%APPDATA%\Pointage\donnees.json`) plutôt que SQLite. Les données sont petites, lues en entier, et les heures sont des minutes entières, jamais des `DateTime`. `releve.md` n'est pas la source de vérité : il sert à l'échange avec la CLI.
- **Code de la CLI copié, pas partagé** : `TimeTracker.Core` est une copie adaptée du code de la CLI, pas un paquet commun. Les fixtures sont ce qui garantit la parité (d'où le point 1 de « À faire »). Toute évolution d'une règle commence dans la CLI.
- **Aujourd'hui** : jamais compté dans les soldes, qui se lisent « au soir d'hier », même une fois la journée complète. Il s'affiche dans le réalisé du jour. Sans cela, deux pointages à midi se liraient comme un départ et le solde plongerait jusqu'à la reprise.
- **Période des soldes** : elle court jusqu'à hier, pas seulement jusqu'au dernier jour complet. Une veille restée incomplète compte donc déjà en déficit, alors que la CLI arrête le calcul au dernier jour renseigné (`TimeCalculator.Calculer(..., jusquA)`).
- **Journée passée restée incomplète** (une ou trois heures) : exclue du calcul, signalée sur l'écran Pointer et dans l'historique.
- **Export avec une journée incomplète** : `releve.md` ne sait pas l'écrire, donc l'export l'**ignore silencieusement**. L'app iPhone, elle, bloque l'export tant qu'elle existe. Ce n'est pas tranché ici (point 2 de « À faire »).
- **Import** : remplace tout l'historique et les réglages (ceux du front-matter), après confirmation. Les erreurs sont celles de la CLI, ligne par ligne.
- **Messages de validation** : une seule validation (`ValidationJournee`), deux formulations. `MessageReleve` est celui de la CLI au caractère près, sans accents, et les fixtures le vérifient ; `MessageEcran` est du français courant pour l'écran d'édition.
- **Édition d'une journée en cours** : elle ne s'enregistre qu'avec arrivée et départ, comme dans `releve.md` ; avant, c'est le bouton Pointer qui la mène.
- **Bouton Pointer** : le deuxième appui est un départ tant qu'un troisième ne le change pas en début de pause. Un cinquième appui, un appui sur un jour d'absence entière et deux appuis dans la même minute sont refusés avec un message. « Annuler le dernier pointage » rattrape un appui de trop.
- **Fenêtre et tâche de fond** : fermer la fenêtre la cache, « Quitter » est dans le menu de l'icône. Une seule instance tourne à la fois.
- **Thème** : Fluent de Windows 11 (`ThemeMode="System"`, encore expérimental dans WPF, d'où `WPF0001` ignoré dans le `.csproj`).
- **Publication** : le profil `win-x64` est autonome (runtime embarqué) et en un seul fichier ; les bibliothèques natives de WPF s'extraient au premier lancement, qui est donc un peu plus lent.

## Ce qui n'est pas vérifié

- **Le rendu visuel** n'a été vu que par Yannick. Les captures d'écran sont proscrites sur son poste (l'antivirus Elastic Security les bloque) : pour vérifier qu'un changement démarre, lancer `Pointage.exe --smoke`, qui ouvre les fenêtres puis quitte (code 1 et trace dans `%TEMP%\pointage-smoke.txt` en cas d'erreur).
- **L'interface n'a aucun test automatisé** (voir le point 3 de « À faire »).
- **Le raccourci global et l'icône de la zone de notification** n'ont été essayés qu'à la main.

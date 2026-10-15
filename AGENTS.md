# AGENTS.md

Instructions pour les assistants IA travaillant sur ce dépôt.

## Le projet

Pointage est une application Windows (WPF, .NET 10) de pointage des heures de travail. Un bouton enregistre l'heure courante et en déduit arrivée, début de pause, fin de pause ou départ ; l'application affiche les soldes de la semaine et du mois. Elle applique exactement les calculs de la CLI [TimeTracker](https://github.com/ymauray/timetracker) et échange avec elle par un fichier `releve.md`. L'app iPhone [timetracker.ios](https://github.com/ymauray/timetracker.ios) en est la sœur. Aucune donnée ne quitte l'ordinateur.

L'état d'avancement, les décisions prises et ce qui reste à faire sont dans [`AVANCEMENT.md`](AVANCEMENT.md) : le lire avant de reprendre le travail.

## Structure

```
src/TimeTracker.Core/           calcul, parseurs, pointage, export/import de releve.md, stockage (net10.0, sans WPF)
src/TimeTracker.Wpf/            application WPF : Services/, ViewModels/, Views/, Assets/ (assembly Pointage.exe)
tests/TimeTracker.Core.Tests/   tests xUnit et Fixtures/ (copie de celles de la CLI)
.github/workflows/              ci.yml (build + test) et release.yml (publication sur tag vX.Y.Z)
```

## Commandes

```
dotnet build
dotnet test
dotnet publish src/TimeTracker.Wpf -p:PublishProfile=win-x64   # publish/win-x64/Pointage.exe, autonome, un seul fichier
```

Pour vérifier le démarrage sans interface : `Pointage.exe --smoke` ouvre les fenêtres puis quitte (code 1 et trace dans `%TEMP%\pointage-smoke.txt` en cas d'erreur). `--minimise` démarre dans la zone de notification.

## Règles métier

La référence est la CLI (`TimeCalculator.cs`, `ConfigParser.cs`, `ReleveParser.cs`). `TimeTracker.Core` en reprend le code ; les fixtures garantissent que les deux calculent pareil.

- Un jour : quatre heures (arrivée, début pause, fin pause, départ) ou un code d'absence. Pas de seconde pause, pas de journée à cheval sur minuit.
- Réglages (`duree_journee` 8h12, `pause_minimum` 0h30, `seuil_pause` 5h, `tolerance` 10h) : valables pour tout l'historique, les modifier recalcule tous les soldes passés.
- Pause décomptée = `max(pause réelle, pause minimum)`, y compris sans pause saisie. Une présence strictement inférieure au seuil de pause ne décompte que la pause réelle.
- Codes d'absence `Conges` (alias `CP`), `Maladie`, `Ferie`, `RTT`, `Divers` : réalisé = théorique. `Demi` exige des heures : réalisé = travail + théorique ÷ 2 (minute inférieure). `Maladie` accepte des heures : réalisé complété jusqu'au théorique.
- Théorique = durée de la journée du lundi au vendredi, 0 le week-end. Un jour ouvré vide entre le premier et le dernier jour renseignés compte en déficit.
- Semaines ISO 8601. Le solde mensuel se cumule sans remise à zéro ; la tolérance s'évalue sur ce cumul.
- Les validations d'une journée sont dans `ValidationJournee` : le parseur de `releve.md` et l'écran d'édition passent par elle. Les messages de `MessageReleve` sont ceux de la CLI, au caractère près (sans accents) : les fixtures les vérifient.

## Bouton Pointer

`JourStocke` garde les pointages bruts du jour (heures en minutes depuis minuit, jamais de `DateTime`) et en déduit la lecture : 1 heure = arrivée, 2 = arrivée et départ, 3 = pause en cours, 4 = journée complète. Le deuxième appui est un départ tant qu'aucun troisième ne le change en début de pause. Un cinquième appui est refusé.

`Bilan` lit les soldes « au soir d'hier » : aujourd'hui n'y entre jamais, ni une journée passée restée incomplète (signalée à part).

## Données

`%APPDATA%\Pointage\donnees.json` (réglages et pointages bruts), écrit de façon atomique par `DepotDonnees`. Le format d'échange est `releve.md` (export et import dans les Réglages).

## Fixtures partagées

`tests/TimeTracker.Core.Tests/Fixtures/` contient des paires `releve.md` + `attendu.json`, copiées du dépôt de la CLI (`tests/TimeTracker.Tests/Fixtures/`) et vérifiées par `FixturesTests.cs`. Toute évolution d'une règle passe par une fixture, d'abord dans la CLI, puis copiée ici et dans l'app iPhone. Le format est décrit dans `Fixtures/LISEZMOI.md`.

## Style

`.editorconfig` fait foi. Noms de types, de méthodes et de variables en français, par cohérence avec le domaine et `releve.md`. Zéro avertissement à la compilation. Pas de commentaires inutiles : un commentaire explique un pourquoi.

## CI et releases

- `ci.yml` : build et tests sur push et PR vers `main`, sur `windows-latest` (job `build-test`, check requis sur `main`).
- `release.yml` : sur tag `vX.Y.Z`, teste puis publie `Pointage.exe` et crée la release GitHub. La version vient du tag (`-p:Version=`).
- Aucun outil externe : tout passe par GitHub Actions.
- `main` est protégée : passer par une PR, mergée en squash.

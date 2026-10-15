# Pointage

[![CI](https://github.com/ymauray/timetracker.x64/actions/workflows/ci.yml/badge.svg)](https://github.com/ymauray/timetracker.x64/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/ymauray/timetracker.x64)](https://github.com/ymauray/timetracker.x64/releases/latest)
[![Licence MIT](https://img.shields.io/github/license/ymauray/timetracker.x64)](LICENSE)
[![repocheck](https://img.shields.io/badge/repocheck%201.3.0-100%2F100-brightgreen)](https://github.com/ymauray/repocheck)

Application Windows de pointage des heures de travail, compagne de la CLI [TimeTracker](https://github.com/ymauray/timetracker) et de l'app iPhone [Pointage](https://github.com/ymauray/timetracker.ios). Un bouton enregistre l'heure courante et en déduit l'arrivée, la pause ou le départ ; l'application applique les calculs de la CLI et affiche les soldes de la semaine et du mois. Aucune donnée ne quitte l'ordinateur.

## Fonctionnalités

- **Pointer** : un appui enregistre l'heure. Le bouton annonce l'action suivante (arrivée, pause ou départ, fin de pause, départ) ; « Annuler le dernier pointage » rattrape un appui de trop.
- **Raccourci global** `Ctrl + Alt + P` et icône dans la zone de notification : pointer sans ouvrir la fenêtre. Fermer la fenêtre la réduit dans la zone de notification ; « Quitter » se trouve dans le menu de l'icône.
- **Historique** : les jours regroupés par semaine ISO, avec l'écart de chacun. Une journée se corrige ou se complète (quatre heures ou un code d'absence), et « + Ajouter une journée oubliée » saisit un pointage manquant.
- **Soldes** : écart de la semaine et solde cumulé de mois en mois, avec alerte quand le solde dépasse la tolérance. Ils se lisent « au soir d'hier » : la journée en cours n'y entre pas.
- **Réglages** : durée de la journée (8h12 par défaut), pause minimum (0h30), seuil de pause (5h) et tolérance mensuelle (10h). Ils valent pour tout l'historique : les modifier recalcule tous les soldes passés, comme la CLI.
- **Échange avec la CLI** : l'export produit un `releve.md` que la CLI lit tel quel ; l'import reprend un `releve.md` existant et remplace l'historique après confirmation.

### Règles de calcul

Les règles sont celles de la CLI, vérifiées par des fixtures communes (`tests/TimeTracker.Core.Tests/Fixtures/`) :

- une journée travaillée compte `départ − arrivée − max(pause réelle, pause minimum)`, y compris sans pause saisie ;
- une journée plus courte que le seuil de pause ne perd que sa pause réelle ;
- les codes d'absence (`Conges`, `Maladie`, `Ferie`, `RTT`, `Divers`) sont neutres sur le solde ; `Demi` ajoute la moitié de la journée aux heures travaillées ;
- un jour ouvré sans rien entre le premier et le dernier jour renseignés compte en déficit.

## Installation

Téléchargez `Pointage.exe` depuis la [dernière release](https://github.com/ymauray/timetracker.x64/releases/latest). C'est un fichier unique, autonome : le runtime .NET est embarqué, rien d'autre n'est à installer. Il suffit de le lancer (Windows 10 ou 11, x64).

Tant qu'aucune release n'est publiée, construisez l'application depuis les sources (voir plus bas).

L'exécutable n'est pas signé : Windows SmartScreen peut afficher un avertissement au premier lancement.

Les données sont enregistrées dans `%APPDATA%\Pointage\donnees.json`.

## Construire depuis les sources

Prérequis : Windows et le [SDK .NET 10](https://dotnet.microsoft.com/download).

```sh
dotnet build
dotnet test
dotnet publish src/TimeTracker.Wpf -p:PublishProfile=win-x64
```

La publication produit `publish/win-x64/Pointage.exe`, en un seul fichier, runtime inclus.

## Organisation du code

```
src/TimeTracker.Core/           calcul, parseurs, pointage, export et import de releve.md, stockage (net10.0)
src/TimeTracker.Wpf/            l'application WPF, qui produit Pointage.exe
tests/TimeTracker.Core.Tests/   tests xUnit et fixtures partagées avec la CLI
```

`TimeTracker.Core` reprend le calcul de la CLI et ne dépend pas de WPF.

## Intégration continue et releases

Tout passe par GitHub Actions ; aucun outil externe n'intervient.

- [`ci.yml`](.github/workflows/ci.yml) : sur chaque push et chaque pull request vers `main`, restaure, compile en Release et lance les tests sur `windows-latest` (l'application est en WPF). Le job `build-test` est requis pour merger dans `main`.
- [`release.yml`](.github/workflows/release.yml) : sur un tag `vX.Y.Z`, relance les tests, publie `Pointage.exe` (autonome, un seul fichier) et crée la release GitHub avec ses notes. La version de l'exécutable vient du tag.
- [`dependabot.yml`](.github/dependabot.yml) : mises à jour hebdomadaires des paquets NuGet et des actions GitHub.

Pour publier une version : `git tag v0.1.0 && git push origin v0.1.0`. Tout ce qui précède le tag (build, tests, revue) est fait par la CI du dépôt ; la publication de l'exécutable et de la release l'est aussi, il n'y a pas de frontière avec un service tiers.

## Contribuer

Voir [CONTRIBUTING.md](CONTRIBUTING.md). Une question ? Voir [SUPPORT.md](.github/SUPPORT.md).

## Licence

[MIT](LICENSE)

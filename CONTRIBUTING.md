# Contribuer à Pointage

Merci de vous intéresser à Pointage ! C'est un petit projet personnel, mais les contributions externes sont bienvenues.

## Avant de commencer

Pour une nouvelle fonctionnalité ou un changement important, ouvrez d'abord une [issue](https://github.com/ymauray/timetracker.x64/issues/new/choose) ou une [discussion](https://github.com/ymauray/timetracker.x64/discussions) pour en parler avec le mainteneur.

## Environnement de développement

Windows et le [SDK .NET 10](https://dotnet.microsoft.com/download) suffisent (l'application est en WPF).

```sh
dotnet build
dotnet test
```

Pour produire l'exécutable autonome : `dotnet publish src/TimeTracker.Wpf -p:PublishProfile=win-x64`.

## Proposer une modification

1. Forkez le dépôt et créez une branche dédiée depuis `main` (ex. `fix/...`, `feature/...`).
2. Faites des commits atomiques, avec des messages clairs.
3. Ajoutez ou mettez à jour les tests (`tests/TimeTracker.Core.Tests`) pour tout changement de comportement, et vérifiez que `dotnet build` et `dotnet test` passent en local.
4. Ouvrez une pull request vers `main` en remplissant le modèle fourni. Les PR sont mergées en squash.

## Règles métier

Le calcul reprend celui de la CLI [TimeTracker](https://github.com/ymauray/timetracker). Toute évolution d'une règle passe par une fixture (`tests/TimeTracker.Core.Tests/Fixtures/`, décrite dans `LISEZMOI.md`) et doit être reportée dans la CLI et l'app iPhone, qui partagent ces fixtures.

## Style

Le fichier `.editorconfig` à la racine fait foi. Le code, les noms de types et de méthodes sont en français, par cohérence avec le domaine et avec le fichier `releve.md`.

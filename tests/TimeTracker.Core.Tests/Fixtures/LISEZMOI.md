# Fixtures partagées

Chaque dossier contient un `releve.md` et le résultat attendu, `attendu.json`,
calculé à la main. `FixturesTests.cs` vérifie la CLI contre ces fichiers ; l'app
iOS (`timetracker.ios`) en garde une copie et doit produire les mêmes résultats.

Toutes les durées sont en minutes entières, signées pour les écarts et soldes.
Les dates sont au format `aaaa-mm-jj`.

- `config` : réglages retenus (`dureeJournee`, `pauseMinimum`, `seuilPause`, `tolerance`),
  valeurs par défaut comprises.
- `erreurs` : erreurs du front-matter puis du tableau, dans l'ordre, avec le
  numéro de ligne et le message exact. Quand la liste n'est pas vide, aucun
  calcul n'est fait et les sections suivantes sont vides.
- `jours` : chaque jour du premier au dernier jour renseigné, week-ends vides
  exclus, avec `pauseDecomptee`, `theorique` et `realise`.
- `joursNonRenseignes` : jours ouvrés absents du fichier, comptés en déficit.
- `semaines` : semaines ISO 8601, avec l'écart cumulé depuis la première.
- `mois` : solde cumulé depuis le premier mois, et dépassement de la tolérance.

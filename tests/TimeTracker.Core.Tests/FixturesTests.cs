using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TimeTracker.Tests;

public class FixturesTests
{
    private static readonly string Dossier = Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private static readonly JsonSerializerOptions Indente = new() { WriteIndented = true };

    public static TheoryData<string> Noms()
    {
        var noms = new TheoryData<string>();
        foreach (var chemin in Directory.GetDirectories(Dossier).Order())
            noms.Add(Path.GetFileName(chemin));
        return noms;
    }

    [Theory]
    [MemberData(nameof(Noms))]
    public void Fixture_ProduitLeResultatAttendu(string nom)
    {
        var lignes = File.ReadAllLines(Path.Combine(Dossier, nom, "releve.md"));
        var attendu = JsonNode.Parse(File.ReadAllText(Path.Combine(Dossier, nom, "attendu.json")))!;

        Assert.Equal(attendu.ToJsonString(Indente), Calculer(lignes).ToJsonString(Indente));
    }

    private static JsonObject Calculer(string[] lignes)
    {
        var config = ConfigParser.Parse(lignes);
        var releve = ReleveParser.Parse(lignes);
        var erreurs = config.Erreurs.Concat(releve.Erreurs).ToList();

        var resultat = new JsonObject
        {
            ["config"] = new JsonObject
            {
                ["dureeJournee"] = Minutes(config.Config.DureeJournee),
                ["pauseMinimum"] = Minutes(config.Config.PauseMinimum),
                ["seuilPause"] = Minutes(config.Config.SeuilPause),
                ["tolerance"] = Minutes(config.Config.Tolerance),
            },
            ["erreurs"] = new JsonArray([.. erreurs.Select(e => new JsonObject { ["ligne"] = e.Ligne, ["message"] = e.Message })]),
        };

        if (erreurs.Count > 0)
        {
            resultat["jours"] = new JsonArray();
            resultat["joursNonRenseignes"] = new JsonArray();
            resultat["semaines"] = new JsonArray();
            resultat["mois"] = new JsonArray();
            return resultat;
        }

        var rapport = TimeCalculator.Calculer(releve.Entrees, config.Config);
        resultat["jours"] = new JsonArray([.. rapport.Jours.Select(j => new JsonObject
        {
            ["date"] = Date(j.Entree.Date),
            ["pauseDecomptee"] = Minutes(j.PauseDecomptee),
            ["theorique"] = Minutes(j.TempsTheorique),
            ["realise"] = Minutes(j.TempsRealise),
        })]);
        resultat["joursNonRenseignes"] = new JsonArray([.. rapport.JoursNonRenseignes.Select(d => JsonValue.Create(Date(d)))]);
        resultat["semaines"] = new JsonArray([.. rapport.Semaines.Select(s => new JsonObject
        {
            ["anneeIso"] = s.AnneeIso,
            ["semaineIso"] = s.SemaineIso,
            ["lundi"] = Date(s.Lundi),
            ["theorique"] = Minutes(s.Theorique),
            ["realise"] = Minutes(s.Realise),
            ["ecartCumule"] = Minutes(s.EcartCumule),
        })]);
        resultat["mois"] = new JsonArray([.. rapport.Mois.Select(m => new JsonObject
        {
            ["annee"] = m.Annee,
            ["mois"] = m.Mois,
            ["theorique"] = Minutes(m.Theorique),
            ["realise"] = Minutes(m.Realise),
            ["soldeCumule"] = Minutes(m.SoldeCumule),
            ["horsTolerance"] = m.HorsTolerance,
        })]);
        return resultat;
    }

    private static int Minutes(TimeSpan duree) => (int)duree.TotalMinutes;

    private static string Date(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

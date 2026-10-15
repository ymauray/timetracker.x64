using System.Text.Json;
using System.Text.Json.Serialization;

namespace TimeTracker;

/// Ce que l'app garde sur disque : les reglages et les pointages bruts, dans un
/// fichier JSON. Les heures sont des minutes entieres, jamais des dates avec fuseau.
public sealed class Donnees
{
    public ReleveConfig Config { get; set; } = ReleveConfig.Defaut;
    public List<JourStocke> Jours { get; set; } = [];

    public JourStocke Jour(DateOnly date) => Jours.FirstOrDefault(j => j.Date == date) ?? new JourStocke(date);

    /// Remplace le jour (ou le retire s'il est vide).
    public void Enregistrer(JourStocke jour)
    {
        Jours.RemoveAll(j => j.Date == jour.Date);
        if (jour.Lecture != JourStocke.Etat.Vide) Jours.Add(jour);
        Jours.Sort((a, b) => a.Date.CompareTo(b.Date));
    }
}

public sealed class DepotDonnees(string chemin)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public string Chemin { get; } = chemin;

    public static string CheminParDefaut() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Pointage", "donnees.json");

    public Donnees Charger()
    {
        if (!File.Exists(Chemin)) return new Donnees();
        var dto = JsonSerializer.Deserialize<DonneesDto>(File.ReadAllText(Chemin), Options) ?? new DonneesDto();
        return dto.VersDonnees();
    }

    /// Ecrit dans un fichier voisin puis le substitue : un crash ne laisse pas un fichier tronque.
    public void Enregistrer(Donnees donnees)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Chemin)!);
        var temporaire = Chemin + ".tmp";
        File.WriteAllText(temporaire, JsonSerializer.Serialize(DonneesDto.Depuis(donnees), Options));
        File.Move(temporaire, Chemin, overwrite: true);
    }

    private sealed class ConfigDto
    {
        public int DureeJourneeMinutes { get; set; } = (int)ReleveConfig.Defaut.DureeJournee.TotalMinutes;
        public int PauseMinimumMinutes { get; set; } = (int)ReleveConfig.Defaut.PauseMinimum.TotalMinutes;
        public int SeuilPauseMinutes { get; set; } = (int)ReleveConfig.Defaut.SeuilPause.TotalMinutes;
        public int ToleranceMinutes { get; set; } = (int)ReleveConfig.Defaut.Tolerance.TotalMinutes;
    }

    private sealed class JourDto
    {
        public string Date { get; set; } = "";
        public List<int> Heures { get; set; } = [];
        public Absence Absence { get; set; }
    }

    private sealed class DonneesDto
    {
        public ConfigDto Config { get; set; } = new();
        public List<JourDto> Jours { get; set; } = [];

        public static DonneesDto Depuis(Donnees d) => new()
        {
            Config = new ConfigDto
            {
                DureeJourneeMinutes = (int)d.Config.DureeJournee.TotalMinutes,
                PauseMinimumMinutes = (int)d.Config.PauseMinimum.TotalMinutes,
                SeuilPauseMinutes = (int)d.Config.SeuilPause.TotalMinutes,
                ToleranceMinutes = (int)d.Config.Tolerance.TotalMinutes,
            },
            Jours = [.. d.Jours.Select(j => new JourDto { Date = j.Date.ToString("yyyy-MM-dd"), Heures = [.. j.Heures], Absence = j.Absence })],
        };

        public Donnees VersDonnees() => new()
        {
            Config = new ReleveConfig
            {
                DureeJournee = TimeSpan.FromMinutes(Config.DureeJourneeMinutes),
                PauseMinimum = TimeSpan.FromMinutes(Config.PauseMinimumMinutes),
                SeuilPause = TimeSpan.FromMinutes(Config.SeuilPauseMinutes),
                Tolerance = TimeSpan.FromMinutes(Config.ToleranceMinutes),
            },
            Jours = [.. Jours.Select(j => new JourStocke(DateOnly.ParseExact(j.Date, "yyyy-MM-dd"), j.Heures, j.Absence))],
        };
    }
}

/// L'import d'un `releve.md` existant, avec les messages d'erreur de la CLI.
public static class ReleveImport
{
    public sealed record Resultat(Donnees? Donnees, IReadOnlyList<ParseError> Erreurs);

    public static Resultat Lire(string[] lignes)
    {
        var config = ConfigParser.Parse(lignes);
        var releve = ReleveParser.Parse(lignes);
        var erreurs = config.Erreurs.Concat(releve.Erreurs).ToList();
        if (erreurs.Count > 0) return new Resultat(null, erreurs);
        var donnees = new Donnees
        {
            Config = config.Config,
            Jours = [.. releve.Entrees.Select(JourStocke.DepuisEntree).OrderBy(j => j.Date)],
        };
        return new Resultat(donnees, erreurs);
    }
}

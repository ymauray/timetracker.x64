namespace TimeTracker;

public sealed class ReleveConfig
{
    public required TimeSpan DureeJournee { get; init; }
    public required TimeSpan PauseMinimum { get; init; }
    public required TimeSpan SeuilPause { get; init; }
    public required TimeSpan Tolerance { get; init; }

    public static readonly ReleveConfig Defaut = new()
    {
        DureeJournee = new TimeSpan(8, 12, 0),
        PauseMinimum = TimeSpan.FromMinutes(30),
        SeuilPause = TimeSpan.FromHours(5),
        Tolerance = TimeSpan.FromHours(10),
    };
}

public sealed class ConfigParseResult
{
    public required ReleveConfig Config { get; init; }
    public required List<ParseError> Erreurs { get; init; }
}

public static class ConfigParser
{
    private const string Delimiteur = "---";

    public static ConfigParseResult Parse(string[] lignes)
    {
        var erreurs = new List<ParseError>();

        if (lignes.Length == 0 || lignes[0].Trim() != Delimiteur)
            return new ConfigParseResult { Config = ReleveConfig.Defaut, Erreurs = erreurs };

        var finIndex = -1;
        for (var i = 1; i < lignes.Length; i++)
        {
            if (lignes[i].Trim() == Delimiteur)
            {
                finIndex = i;
                break;
            }
        }

        if (finIndex == -1)
        {
            erreurs.Add(new ParseError { Ligne = 1, Message = "Front-matter non ferme : ligne '---' de fin introuvable." });
            return new ConfigParseResult { Config = ReleveConfig.Defaut, Erreurs = erreurs };
        }

        var valeurs = new Dictionary<string, string>();
        for (var i = 1; i < finIndex; i++)
        {
            var ligne = lignes[i].Trim();
            if (ligne.Length == 0) continue;

            var sep = ligne.IndexOf(':');
            if (sep < 0)
            {
                erreurs.Add(new ParseError { Ligne = i + 1, Message = $"Ligne de front-matter invalide (attendu 'cle: valeur') : '{ligne}'." });
                continue;
            }

            var cle = ligne[..sep].Trim().ToLowerInvariant();
            var valeur = ligne[(sep + 1)..].Trim();
            valeurs[cle] = valeur;
        }

        TimeSpan LireDuree(string cle, TimeSpan defaut)
        {
            if (!valeurs.TryGetValue(cle, out var texte)) return defaut;
            var parsed = DureeParsing.Parse(texte);
            if (parsed is null)
            {
                erreurs.Add(new ParseError { Ligne = 1, Message = $"Valeur invalide pour '{cle}' dans le front-matter : '{texte}' (format attendu ex: 8h12)." });
                return defaut;
            }
            return new TimeSpan(parsed.Value.Heures, parsed.Value.Minutes, 0);
        }

        var config = new ReleveConfig
        {
            DureeJournee = LireDuree("duree_journee", ReleveConfig.Defaut.DureeJournee),
            PauseMinimum = LireDuree("pause_minimum", ReleveConfig.Defaut.PauseMinimum),
            SeuilPause = LireDuree("seuil_pause", ReleveConfig.Defaut.SeuilPause),
            Tolerance = LireDuree("tolerance", ReleveConfig.Defaut.Tolerance),
        };

        return new ConfigParseResult { Config = config, Erreurs = erreurs };
    }
}

using System.Globalization;

namespace TimeTracker;

public sealed class ParseResult
{
    public required List<DayEntry> Entrees { get; init; }
    public required List<ParseError> Erreurs { get; init; }
}

public static partial class ReleveParser
{
    private static readonly string[] ColonnesAttendues =
        ["Date", "Arrivee", "Debut pause", "Fin pause", "Depart", "Absence"];

    // "d"/"M" acceptent 1 ou 2 chiffres a l'analyse : couvre 1.10.2026, 21.2.2027, 01.10.2026...
    private static readonly string[] FormatsDate = ["d.M.yyyy"];

    public static ParseResult Parse(string[] lignes)
    {
        var erreurs = new List<ParseError>();
        var entrees = new List<DayEntry>();
        var datesVues = new Dictionary<DateOnly, int>();

        var tableLignes = lignes
            .Select((texte, index) => (texte, numero: index + 1))
            .Where(l => l.texte.TrimStart().StartsWith('|'))
            .ToList();

        if (tableLignes.Count < 2)
        {
            erreurs.Add(new ParseError { Ligne = 1, Message = "Aucun tableau markdown (lignes commencant par '|') trouve dans le fichier." });
            return new ParseResult { Entrees = entrees, Erreurs = erreurs };
        }

        var enTete = SplitCells(tableLignes[0].texte).Select(NormaliserEnTete).ToArray();
        for (var i = 0; i < ColonnesAttendues.Length; i++)
        {
            if (i >= enTete.Length || enTete[i] != ColonnesAttendues[i])
            {
                erreurs.Add(new ParseError
                {
                    Ligne = tableLignes[0].numero,
                    Message = $"En-tete de tableau invalide : colonnes attendues = " +
                              $"|{string.Join('|', ["Date", "Arrivée", "Début pause", "Fin pause", "Départ", "Absence"])}|",
                });
                return new ParseResult { Entrees = entrees, Erreurs = erreurs };
            }
        }

        // ligne 1 = en-tete, ligne 2 = separateur "---", les suivantes = donnees
        for (var i = 2; i < tableLignes.Count; i++)
        {
            var (texte, numero) = tableLignes[i];
            var cellules = SplitCells(texte);
            if (cellules.Length < ColonnesAttendues.Length)
            {
                erreurs.Add(new ParseError { Ligne = numero, Message = $"Ligne incomplete : {cellules.Length} colonne(s) trouvee(s), 6 attendues." });
                continue;
            }

            var dateTexte = cellules[0].Trim();
            if (dateTexte.Length == 0) continue; // ligne sans date : separateur visuel, ignoree

            if (!DateOnly.TryParseExact(dateTexte, FormatsDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                erreurs.Add(new ParseError { Ligne = numero, Message = $"Date invalide '{dateTexte}' (format attendu jj.mm.aaaa, ex: 1.10.2026 ou 21.02.2027)." });
                continue;
            }

            if (datesVues.TryGetValue(date, out var premiereLigne))
            {
                erreurs.Add(new ParseError { Ligne = numero, Message = $"Date en double : {FormatHelpers.FormatDate(date)} (deja presente ligne {premiereLigne})." });
                continue;
            }

            var absenceTexte = cellules[5].Trim();
            Absence absence;
            if (absenceTexte.Length == 0)
            {
                absence = Absence.Aucune;
            }
            else if (!TryParseAbsence(absenceTexte, out absence))
            {
                erreurs.Add(new ParseError { Ligne = numero, Message = $"Code absence inconnu '{absenceTexte}' (valeurs valides : Conges ou CP, Maladie, Ferie, RTT, Divers, Demi)." });
                continue;
            }

            var erreurLigne = false;
            TimeOnly? LireHeure(string cell, string nomChamp)
            {
                var texteHeure = cell.Trim();
                if (texteHeure.Length == 0) return null;
                var parsed = DureeParsing.Parse(texteHeure);
                if (parsed is null)
                {
                    erreurs.Add(new ParseError { Ligne = numero, Message = $"Heure invalide pour '{nomChamp}' : '{texteHeure}' (format attendu ex: 8h50)." });
                    erreurLigne = true;
                    return null;
                }
                var (h, min) = parsed.Value;
                if (h > 23)
                {
                    erreurs.Add(new ParseError { Ligne = numero, Message = $"Heure hors limites pour '{nomChamp}' : '{texteHeure}'." });
                    erreurLigne = true;
                    return null;
                }
                return new TimeOnly(h, min);
            }

            var arrivee = LireHeure(cellules[1], "Arrivee");
            var debutPause = LireHeure(cellules[2], "Debut pause");
            var finPause = LireHeure(cellules[3], "Fin pause");
            var depart = LireHeure(cellules[4], "Depart");
            if (erreurLigne) continue;

            var validation = ValidationJournee.Valider(arrivee, debutPause, finPause, depart, absence);
            if (!validation.EstValide)
            {
                erreurs.Add(new ParseError { Ligne = numero, Message = validation.MessageReleve! });
                continue;
            }

            datesVues[date] = numero;
            entrees.Add(new DayEntry
            {
                Date = date,
                Absence = absence,
                Arrivee = arrivee,
                DebutPause = debutPause,
                FinPause = finPause,
                Depart = depart,
                Renseigne = true,
                LigneSource = numero,
            });
        }

        return new ParseResult { Entrees = entrees, Erreurs = erreurs };
    }

    private static bool TryParseAbsence(string texte, out Absence absence)
    {
        var normalise = RetirerAccents(texte).Trim().ToUpperInvariant();
        switch (normalise)
        {
            case "CP" or "CONGES": absence = Absence.Conges; return true;
            case "MALADIE": absence = Absence.Maladie; return true;
            case "FERIE": absence = Absence.Ferie; return true;
            case "RTT": absence = Absence.RTT; return true;
            case "DIVERS": absence = Absence.Divers; return true;
            case "DEMI": absence = Absence.Demi; return true;
            default: absence = Absence.Aucune; return false;
        }
    }

    private static string RetirerAccents(string s) =>
        s.Replace("é", "e").Replace("è", "e").Replace("ê", "e").Replace("É", "E").Replace("È", "E").Replace("Ê", "E");

    private static string NormaliserEnTete(string s) => RetirerAccents(s.Trim());

    private static string[] SplitCells(string ligne)
    {
        var t = ligne.Trim();
        if (t.StartsWith('|')) t = t[1..];
        if (t.EndsWith('|')) t = t[..^1];
        return t.Split('|');
    }
}

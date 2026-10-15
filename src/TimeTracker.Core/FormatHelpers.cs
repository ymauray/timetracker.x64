using System.Globalization;

namespace TimeTracker;

public static class FormatHelpers
{
    private static readonly string[] JoursFr =
        ["lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi", "dimanche"];

    private static readonly string[] MoisFr =
    [
        "janvier", "fevrier", "mars", "avril", "mai", "juin",
        "juillet", "aout", "septembre", "octobre", "novembre", "decembre",
    ];

    public static string NomJour(DateOnly date)
    {
        // DayOfWeek: Sunday = 0 ... Saturday = 6 -> reindex to lundi = 0
        var index = ((int)date.DayOfWeek + 6) % 7;
        return JoursFr[index];
    }

    public static string NomMois(int mois) => MoisFr[mois - 1];

    public static string NomAbsence(Absence absence) => absence switch
    {
        Absence.Aucune => "",
        Absence.Conges => "Congés",
        Absence.Maladie => "Maladie",
        Absence.Ferie => "Férié",
        Absence.RTT => "RTT",
        Absence.Divers => "Divers",
        Absence.Demi => "Demi-journée",
        _ => absence.ToString(),
    };

    public static string FormatDate(DateOnly date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    /// Duree non signee (toujours >= 0), ex: "8h12", "41h00".
    public static string FormatDuree(TimeSpan ts)
    {
        var totalMinutes = (long)Math.Round(ts.TotalMinutes);
        var heures = totalMinutes / 60;
        var minutes = Math.Abs(totalMinutes % 60);
        return $"{heures}h{minutes:00}";
    }

    /// Plage symetrique, ex: "-10h00/+10h00".
    public static string FormatTolerance(TimeSpan tolerance) => $"-{FormatDuree(tolerance)}/+{FormatDuree(tolerance)}";

    /// Ecart signe, ex: "+2h30", "-0h45", "0h00".
    public static string FormatEcart(TimeSpan ts)
    {
        if (ts == TimeSpan.Zero) return "0h00";
        var signe = ts < TimeSpan.Zero ? "-" : "+";
        var abs = ts.Duration();
        var totalMinutes = (long)Math.Round(abs.TotalMinutes);
        var heures = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        return $"{signe}{heures}h{minutes:00}";
    }

    public static string? FormatHeure(TimeOnly? t) => t is null ? null : t.Value.ToString("HH'h'mm", CultureInfo.InvariantCulture);
}

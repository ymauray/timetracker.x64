using System.Globalization;
using System.Text.RegularExpressions;

namespace TimeTracker.Wpf.Services;

/// Les formats d'affichage et de saisie des durees et des heures.
public static partial class Formats
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    /// « 8h12 » ; une duree negative garde son signe.
    public static string Duree(int minutes)
    {
        var signe = minutes < 0 ? "-" : "";
        var v = Math.Abs(minutes);
        return $"{signe}{v / 60}h{v % 60:00}";
    }

    public static string Duree(TimeSpan duree) => Duree((int)Math.Round(duree.TotalMinutes));

    /// « +0h18 », « -2h30 », « 0h00 ».
    public static string Ecart(TimeSpan ecart) => FormatHelpers.FormatEcart(ecart);

    /// « 08:12 » pour l'affichage d'un pointage.
    public static string Heure(int minutes) => $"{minutes / 60:00}:{minutes % 60:00}";

    public static string Heure(TimeOnly heure) => Heure(JourStocke.VersMinutes(heure));

    /// « jeudi 15 octobre 2026 ».
    public static string DateLongue(DateOnly date) => date.ToString("dddd d MMMM yyyy", Fr);

    /// « lun. 28.09 ».
    public static string DateCourte(DateOnly date) => date.ToString("ddd dd.MM", Fr);

    /// Lit « 8h50 », « 8:50 », « 8h » ou « 08:05 » ; vide donne null sans erreur.
    public static bool TryLireHeure(string? texte, out TimeOnly? heure)
    {
        heure = null;
        var t = (texte ?? "").Trim();
        if (t.Length == 0) return true;
        var m = SaisieHeure().Match(t);
        if (!m.Success) return false;
        var h = int.Parse(m.Groups[1].Value);
        var min = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 0;
        if (h > 23 || min > 59) return false;
        heure = new TimeOnly(h, min);
        return true;
    }

    /// Lit une duree « 8h12 » (comme le front-matter de releve.md) ; null si invalide.
    public static TimeSpan? LireDuree(string? texte)
    {
        var parsed = DureeParsing.Parse((texte ?? "").Trim().Replace(':', 'h'));
        return parsed is null ? null : new TimeSpan(parsed.Value.Heures, parsed.Value.Minutes, 0);
    }

    [GeneratedRegex(@"^(\d{1,2})[h:](\d{2})?$", RegexOptions.IgnoreCase)]
    private static partial Regex SaisieHeure();
}

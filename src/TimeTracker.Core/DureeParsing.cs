using System.Text.RegularExpressions;

namespace TimeTracker;

public static partial class DureeParsing
{
    [GeneratedRegex(@"^(\d{1,2})h(\d{2})?$", RegexOptions.IgnoreCase)]
    private static partial Regex Regex();

    /// Parse un texte du type "8h12", "8h" ou "0h30" -> (heures, minutes), ou null si invalide.
    public static (int Heures, int Minutes)? Parse(string texte)
    {
        var m = Regex().Match(texte.Trim());
        if (!m.Success) return null;
        var h = int.Parse(m.Groups[1].Value);
        var min = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 0;
        if (min > 59) return null;
        return (h, min);
    }
}

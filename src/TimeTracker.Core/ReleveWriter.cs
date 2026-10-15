using System.Text;

namespace TimeTracker;

/// L'ecriture de `releve.md`, dans le format que la CLI lit telle quelle.
public static class ReleveWriter
{
    public static string Ecrire(IEnumerable<JourStocke> jours, ReleveConfig config)
    {
        var sb = new StringBuilder();
        sb.Append("---\n");
        sb.Append($"duree_journee: {Duree(config.DureeJournee)}\n");
        sb.Append($"pause_minimum: {Duree(config.PauseMinimum)}\n");
        sb.Append($"seuil_pause: {Duree(config.SeuilPause)}\n");
        sb.Append($"tolerance: {Duree(config.Tolerance)}\n");
        sb.Append("---\n");
        sb.Append("|Date|Arrivée|Début pause|Fin pause|Départ|Absence|\n");
        sb.Append("|---|---|---|---|---|---|\n");
        foreach (var jour in jours.OrderBy(j => j.Date))
        {
            if (jour.VersEntree() is not { } e) continue; // une journee incomplete n'a pas de releve
            var code = e.Absence == Absence.Aucune ? "" : e.Absence.ToString();
            sb.Append($"|{e.Date.Day}.{e.Date.Month}.{e.Date.Year}|{Heure(e.Arrivee)}|{Heure(e.DebutPause)}|{Heure(e.FinPause)}|{Heure(e.Depart)}|{code}|\n");
        }
        return sb.ToString();
    }

    private static string Duree(TimeSpan d) => $"{(int)d.TotalHours}h{d.Minutes:00}";

    private static string Heure(TimeOnly? h) => h is null ? "" : $"{h.Value.Hour}h{h.Value.Minute:00}";
}

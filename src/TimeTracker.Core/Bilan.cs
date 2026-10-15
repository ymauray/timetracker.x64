using System.Globalization;

namespace TimeTracker;

/// Les soldes tels que l'app les affiche, calcules sur les journees stockees.
/// Ils se lisent « au soir d'hier » : aujourd'hui n'y entre jamais, meme complet
/// (deux pointages a midi se liraient sinon comme un depart). Une journee passee
/// restee incomplete n'y entre pas non plus, et elle est signalee. La periode
/// court jusqu'a hier : un jour ouvre passe sans journee complete compte en deficit.
public sealed class Bilan
{
    /// Null tant qu'aucune journee complete n'existe.
    public TimeCalculator.Rapport? Rapport { get; }
    public IReadOnlyList<DateOnly> JourneesIncompletes { get; }
    /// Ecart de la semaine ISO d'aujourd'hui, aujourd'hui exclu.
    public TimeSpan EcartSemaine { get; }
    /// Solde cumule depuis le debut de l'historique.
    public TimeSpan SoldeCumule { get; }
    public bool HorsTolerance { get; }

    public Bilan(IEnumerable<JourStocke> jours, DateOnly aujourdhui, ReleveConfig config)
    {
        var completes = new List<DayEntry>();
        var incompletes = new List<DateOnly>();
        foreach (var jour in jours)
        {
            if (jour.Date >= aujourdhui) continue;
            if (jour.Lecture == JourStocke.Etat.EnCours) incompletes.Add(jour.Date);
            else if (jour.VersEntree() is { } entree) completes.Add(entree);
        }
        JourneesIncompletes = [.. incompletes.Order()];

        if (completes.Count == 0) return;
        Rapport = TimeCalculator.Calculer(completes, config, aujourdhui.AddDays(-1));
        var date = aujourdhui.ToDateTime(TimeOnly.MinValue);
        var annee = ISOWeek.GetYear(date);
        var numero = ISOWeek.GetWeekOfYear(date);
        EcartSemaine = Rapport.Semaines.FirstOrDefault(s => s.AnneeIso == annee && s.SemaineIso == numero)?.Ecart ?? TimeSpan.Zero;
        var dernierMois = Rapport.Mois.LastOrDefault();
        SoldeCumule = dernierMois?.SoldeCumule ?? TimeSpan.Zero;
        HorsTolerance = dernierMois?.HorsTolerance ?? false;
    }
}

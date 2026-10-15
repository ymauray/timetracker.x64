using System.Globalization;

namespace TimeTracker;

public static class TimeCalculator
{
    public sealed class Rapport
    {
        public required List<DayResult> Jours { get; init; }
        public required List<WeekSummary> Semaines { get; init; }
        public required List<MonthSummary> Mois { get; init; }
        public required DateOnly DateDebut { get; init; }
        public required DateOnly DateFin { get; init; }
        public required List<DateOnly> JoursNonRenseignes { get; init; }
        public required ReleveConfig Config { get; init; }
    }

    /// La periode va du premier au dernier jour renseigne, ou jusqu'a `jusquA` s'il est plus tard.
    public static Rapport Calculer(List<DayEntry> entreesFichier, ReleveConfig config, DateOnly? jusquA = null)
    {
        if (entreesFichier.Count == 0)
            throw new InvalidOperationException("Aucune ligne exploitable dans le releve.");

        var dateDebut = entreesFichier.Min(e => e.Date);
        var dateFin = entreesFichier.Max(e => e.Date);
        if (jusquA is not null && jusquA > dateFin) dateFin = jusquA.Value;
        var parDate = entreesFichier.ToDictionary(e => e.Date);

        var joursComplets = new List<DayEntry>();
        var joursNonRenseignes = new List<DateOnly>();
        for (var d = dateDebut; d <= dateFin; d = d.AddDays(1))
        {
            if (parDate.TryGetValue(d, out var entree))
            {
                joursComplets.Add(entree);
                continue;
            }

            var estOuvre = d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
            if (!estOuvre) continue; // week-end non renseigne : sans impact, on ne le liste pas

            joursNonRenseignes.Add(d);
            joursComplets.Add(new DayEntry { Date = d, Renseigne = false });
        }

        var resultats = joursComplets.Select(e => CalculerJour(e, config)).OrderBy(r => r.Entree.Date).ToList();

        var semaines = resultats
            .GroupBy(r => (annee: ISOWeek.GetYear(r.Entree.Date.ToDateTime(TimeOnly.MinValue)),
                            semaine: ISOWeek.GetWeekOfYear(r.Entree.Date.ToDateTime(TimeOnly.MinValue))))
            .OrderBy(g => g.Key.annee).ThenBy(g => g.Key.semaine)
            .Select(g =>
            {
                var lundi = DateOnly.FromDateTime(ISOWeek.ToDateTime(g.Key.annee, g.Key.semaine, DayOfWeek.Monday));
                return new WeekSummary
                {
                    AnneeIso = g.Key.annee,
                    SemaineIso = g.Key.semaine,
                    Lundi = lundi,
                    Dimanche = lundi.AddDays(6),
                    Theorique = Somme(g.Select(r => r.TempsTheorique)),
                    Realise = Somme(g.Select(r => r.TempsRealise)),
                };
            })
            .ToList();

        var cumulSemaine = TimeSpan.Zero;
        foreach (var s in semaines)
        {
            cumulSemaine += s.Ecart;
            s.EcartCumule = cumulSemaine;
        }

        var mois = resultats
            .GroupBy(r => (annee: r.Entree.Date.Year, mois: r.Entree.Date.Month))
            .OrderBy(g => g.Key.annee).ThenBy(g => g.Key.mois)
            .Select(g => new MonthSummary
            {
                Annee = g.Key.annee,
                Mois = g.Key.mois,
                Theorique = Somme(g.Select(r => r.TempsTheorique)),
                Realise = Somme(g.Select(r => r.TempsRealise)),
            })
            .ToList();

        var cumulMois = TimeSpan.Zero;
        foreach (var m in mois)
        {
            cumulMois += m.Ecart;
            m.SoldeCumule = cumulMois;
            m.HorsTolerance = cumulMois.Duration() > config.Tolerance;
        }

        return new Rapport
        {
            Jours = resultats,
            Semaines = semaines,
            Mois = mois,
            DateDebut = dateDebut,
            DateFin = dateFin,
            JoursNonRenseignes = joursNonRenseignes,
            Config = config,
        };
    }

    private static TimeSpan Somme(IEnumerable<TimeSpan> valeurs)
    {
        var total = TimeSpan.Zero;
        foreach (var v in valeurs) total += v;
        return total;
    }

    private static DayResult CalculerJour(DayEntry entree, ReleveConfig config)
    {
        var estOuvre = entree.Date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
        var theorique = estOuvre ? config.DureeJournee : TimeSpan.Zero;

        var pauseDecomptee = TimeSpan.Zero;
        TimeSpan realise;
        if (entree.Absence != Absence.Aucune && entree.Arrivee is null)
        {
            realise = theorique; // jour d'absence : neutre sur le solde
        }
        else if (entree.Arrivee is not null && entree.Depart is not null)
        {
            var pauseReelle = entree.DebutPause is not null && entree.FinPause is not null
                ? entree.FinPause.Value - entree.DebutPause.Value
                : TimeSpan.Zero;
            var presence = entree.Depart.Value - entree.Arrivee.Value;
            // reglement : une journee sans pause saisie perd quand meme la pause minimum,
            // sauf une journee courte (presence sous le seuil), qui ne perd que sa pause reelle
            pauseDecomptee = presence < config.SeuilPause || pauseReelle > config.PauseMinimum
                ? pauseReelle
                : config.PauseMinimum;
            realise = presence - pauseDecomptee;
            if (entree.Absence == Absence.Demi)
            {
                // demi-journee d'absence (conge, RTT...) : la moitie du theorique, en minutes entieres
                realise += TimeSpan.FromMinutes(Math.Floor(theorique.TotalMinutes / 2));
            }
            else if (entree.Absence == Absence.Maladie && realise < theorique)
            {
                realise = theorique; // maladie en cours de journee : complete jusqu'a la journee entiere
            }
        }
        else
        {
            realise = TimeSpan.Zero; // jour ouvre non renseigne : deficit
        }

        return new DayResult
        {
            Entree = entree,
            PauseDecomptee = pauseDecomptee,
            TempsTheorique = theorique,
            TempsRealise = realise,
            EstJourOuvre = estOuvre,
        };
    }
}

namespace TimeTracker;

/// Une journee telle que l'app la stocke : de zero a quatre heures pointees
/// (minutes depuis minuit, strictement croissantes) et un code d'absence facultatif.
/// Le bouton Pointer et la saisie manuelle ecrivent tous deux ici ; le nombre
/// d'heures donne la lecture (1 = arrivee, 2 = arrivee et depart, 3 = pause en
/// cours, 4 = journee complete).
public sealed record JourStocke(DateOnly Date, IReadOnlyList<int> Heures, Absence Absence = Absence.Aucune)
{
    public JourStocke(DateOnly date) : this(date, []) { }

    public enum Etat { Vide, EnCours, Complete }

    // Un record compare ses listes par reference : on veut une egalite par valeur.
    public bool Equals(JourStocke? autre) =>
        autre is not null && Date == autre.Date && Absence == autre.Absence && Heures.SequenceEqual(autre.Heures);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Date);
        hash.Add(Absence);
        foreach (var h in Heures) hash.Add(h);
        return hash.ToHashCode();
    }

    public static bool PorteDesHeures(Absence absence) => absence is Absence.Aucune or Absence.Demi or Absence.Maladie;

    public static TimeOnly VersHeure(int minutes) => new(minutes / 60, minutes % 60);

    public static int VersMinutes(TimeOnly heure) => heure.Hour * 60 + heure.Minute;

    public static bool EstOuvre(DateOnly date) => date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    public Etat Lecture => Heures.Count switch
    {
        0 => Absence == Absence.Aucune ? Etat.Vide : Absence == Absence.Demi ? Etat.EnCours : Etat.Complete,
        2 or 4 => Etat.Complete,
        _ => Etat.EnCours,
    };

    /// L'entree de calcul d'une journee complete ; null sinon.
    public DayEntry? VersEntree()
    {
        if (Lecture != Etat.Complete) return null;
        return Heures.Count switch
        {
            0 => new DayEntry { Date = Date, Absence = Absence },
            2 => new DayEntry { Date = Date, Absence = Absence, Arrivee = VersHeure(Heures[0]), Depart = VersHeure(Heures[1]) },
            _ => new DayEntry
            {
                Date = Date,
                Absence = Absence,
                Arrivee = VersHeure(Heures[0]),
                DebutPause = VersHeure(Heures[1]),
                FinPause = VersHeure(Heures[2]),
                Depart = VersHeure(Heures[3]),
            },
        };
    }

    public static JourStocke DepuisEntree(DayEntry e)
    {
        var heures = new List<int>();
        if (e.Arrivee is { } a) heures.Add(VersMinutes(a));
        if (e.DebutPause is { } d) heures.Add(VersMinutes(d));
        if (e.FinPause is { } f) heures.Add(VersMinutes(f));
        if (e.Depart is { } p) heures.Add(VersMinutes(p));
        return new JourStocke(e.Date, heures, e.Absence);
    }

    /// Nom de chaque heure selon leur nombre : la deuxieme est un depart tant
    /// qu'aucune troisieme ne la change en debut de pause.
    public IReadOnlyList<string> NomsDesHeures => Heures.Count switch
    {
        1 => ["Arrivée"],
        2 => ["Arrivée", "Départ"],
        3 => ["Arrivée", "Début pause", "Fin pause"],
        4 => ["Arrivée", "Début pause", "Fin pause", "Départ"],
        _ => [],
    };

    /// Ce que fera le prochain appui, null quand plus aucun n'est possible.
    public string? ActionSuivante
    {
        get
        {
            if (!PorteDesHeures(Absence)) return null;
            return Heures.Count switch
            {
                0 => "Arrivée",
                1 => "Pause ou départ",
                2 => "Fin de pause",
                3 => "Départ",
                _ => null,
            };
        }
    }

    /// Ajoute l'heure `minute`. Refuse apres quatre pointages, sur un jour d'absence
    /// entiere, et dans la meme minute que le precedent ou avant lui.
    public bool TryPointer(int minute, out JourStocke jour, out string? refus)
    {
        jour = this;
        if (!PorteDesHeures(Absence)) { refus = "Cette journée est marquée en absence."; return false; }
        if (Heures.Count >= 4) { refus = "La journée a déjà ses quatre pointages."; return false; }
        if (Heures.Count > 0 && minute <= Heures[^1]) { refus = "Un pointage existe déjà à cette minute ou plus tard."; return false; }
        jour = this with { Heures = [.. Heures, minute] };
        refus = null;
        return true;
    }

    public JourStocke SansDernierPointage() =>
        Heures.Count == 0 ? this : this with { Heures = [.. Heures.Take(Heures.Count - 1)] };

    /// Temps travaille jusqu'a `maintenant` pour une journee en cours (pauses reelles
    /// retirees ; la pause minimum ne s'applique qu'au depart ; une demi-journee
    /// compte deja sa moitie d'absence). Pour une journee complete, le realise du calcul.
    public int Realise(int maintenant, ReleveConfig config)
    {
        switch (Lecture)
        {
            case Etat.Vide:
                return 0;
            case Etat.EnCours:
                var demi = Absence == Absence.Demi && EstOuvre(Date) ? (int)config.DureeJournee.TotalMinutes / 2 : 0;
                if (Heures.Count == 0) return demi;
                var fin = Math.Max(maintenant, Heures[^1]);
                var travail = Heures.Count == 1 ? fin - Heures[0] : (Heures[1] - Heures[0]) + (fin - Heures[2]);
                return travail + demi;
            default:
                var rapport = TimeCalculator.Calculer([VersEntree()!], config);
                return (int)rapport.Jours[0].TempsRealise.TotalMinutes;
        }
    }
}

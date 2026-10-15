namespace TimeTracker;

public enum Absence
{
    Aucune,
    Conges,
    Maladie,
    Ferie,
    RTT,
    Divers,
    Demi,
}

public sealed class DayEntry
{
    public required DateOnly Date { get; init; }
    public Absence Absence { get; init; } = Absence.Aucune;
    public TimeOnly? Arrivee { get; init; }
    public TimeOnly? DebutPause { get; init; }
    public TimeOnly? FinPause { get; init; }
    public TimeOnly? Depart { get; init; }
    public bool Renseigne { get; init; } = true;
    public int? LigneSource { get; init; }
}

public sealed class DayResult
{
    public required DayEntry Entree { get; init; }
    public TimeSpan PauseDecomptee { get; init; }
    public TimeSpan TempsTheorique { get; init; }
    public TimeSpan TempsRealise { get; init; }
    public TimeSpan Ecart => TempsRealise - TempsTheorique;
    public bool EstJourOuvre { get; init; }
}

public sealed class WeekSummary
{
    public required int AnneeIso { get; init; }
    public required int SemaineIso { get; init; }
    public required DateOnly Lundi { get; init; }
    public required DateOnly Dimanche { get; init; }
    public TimeSpan Theorique { get; init; }
    public TimeSpan Realise { get; init; }
    public TimeSpan Ecart => Realise - Theorique;
    public TimeSpan EcartCumule { get; set; }
}

public sealed class MonthSummary
{
    public required int Annee { get; init; }
    public required int Mois { get; init; }
    public TimeSpan Theorique { get; init; }
    public TimeSpan Realise { get; init; }
    public TimeSpan Ecart => Realise - Theorique;
    public TimeSpan SoldeCumule { get; set; }
    public bool HorsTolerance { get; set; }
}

public sealed class ParseError
{
    public required int Ligne { get; init; }
    public required string Message { get; init; }
}

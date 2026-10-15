using CommunityToolkit.Mvvm.ComponentModel;
using TimeTracker.Wpf.Services;

namespace TimeTracker.Wpf.ViewModels;

public sealed record ChoixAbsence(Absence Absence, string Libelle);

/// L'edition manuelle d'une journee : quatre heures ou un code d'absence, avec les
/// validations de la CLI.
public sealed partial class EditionJourViewModel : ObservableObject
{
    private readonly AppState _state;
    private readonly DateOnly? _dateInitiale;

    public static IReadOnlyList<ChoixAbsence> Choix { get; } =
    [
        new(Absence.Aucune, "Aucune (journée travaillée)"),
        new(Absence.Conges, "Congés"),
        new(Absence.Maladie, "Maladie"),
        new(Absence.Ferie, "Férié"),
        new(Absence.RTT, "RTT"),
        new(Absence.Divers, "Divers"),
        new(Absence.Demi, "Demi-journée (avec les heures travaillées)"),
    ];

    public bool EstAjout => _dateInitiale is null;
    public bool PeutSupprimer => !EstAjout;
    public string Titre => EstAjout ? "Ajouter une journée" : $"Modifier le {FormatHelpers.FormatDate(_dateInitiale!.Value)}";

    [ObservableProperty] private DateTime _date;
    [ObservableProperty] private ChoixAbsence _absenceChoisie = Choix[0];
    [ObservableProperty] private string _arrivee = "";
    [ObservableProperty] private string _debutPause = "";
    [ObservableProperty] private string _finPause = "";
    [ObservableProperty] private string _depart = "";
    [ObservableProperty] private string _erreur = "";

    public bool EstValide => Erreur.Length == 0;

    /// Edite `existant`, ou ajoute une journee oubliee quand il est null.
    public EditionJourViewModel(AppState state, JourStocke? existant)
    {
        _state = state;
        _dateInitiale = existant?.Date;
        if (existant is null)
        {
            _date = DateTime.Today.AddDays(-1);
        }
        else
        {
            _date = existant.Date.ToDateTime(TimeOnly.MinValue);
            _absenceChoisie = Choix.First(c => c.Absence == existant.Absence);
            // Les heures reprennent leur role selon leur nombre : deux = arrivee et depart.
            string Texte(int i) => i < existant.Heures.Count ? Formats.Heure(existant.Heures[i]) : "";
            switch (existant.Heures.Count)
            {
                case 2: _arrivee = Texte(0); _depart = Texte(1); break;
                case 4: _arrivee = Texte(0); _debutPause = Texte(1); _finPause = Texte(2); _depart = Texte(3); break;
                default: // une ou trois heures : journee sans depart, a completer
                    _arrivee = Texte(0); _debutPause = Texte(1); _finPause = Texte(2); break;
            }
        }
        Recalculer();
    }

    partial void OnDateChanged(DateTime value) => Recalculer();
    partial void OnAbsenceChoisieChanged(ChoixAbsence value) => Recalculer();
    partial void OnArriveeChanged(string value) => Recalculer();
    partial void OnDebutPauseChanged(string value) => Recalculer();
    partial void OnFinPauseChanged(string value) => Recalculer();
    partial void OnDepartChanged(string value) => Recalculer();

    private JourStocke? _resultat;

    public JourStocke? Resultat => _resultat;

    private void Recalculer()
    {
        _resultat = null;
        Erreur = Construire(out _resultat) ?? "";
        OnPropertyChanged(nameof(EstValide));
    }

    /// Renvoie le message de la premiere regle enfreinte, ou null et la journee construite.
    private string? Construire(out JourStocke? jour)
    {
        jour = null;
        var date = DateOnly.FromDateTime(Date);

        TimeOnly?[] heures = new TimeOnly?[4];
        string[] textes = [Arrivee, DebutPause, FinPause, Depart];
        string[] noms = ["l'arrivée", "le début de pause", "la fin de pause", "le départ"];
        for (var i = 0; i < 4; i++)
        {
            if (!Formats.TryLireHeure(textes[i], out heures[i]))
                return $"Heure invalide pour {noms[i]} : « {textes[i].Trim()} » (exemple : 8h50).";
        }

        var validation = ValidationJournee.Valider(heures[0], heures[1], heures[2], heures[3], AbsenceChoisie.Absence);
        if (!validation.EstValide) return validation.MessageEcran;

        if (EstAjout && _state.Donnees.Jour(date).Lecture != JourStocke.Etat.Vide)
            return "Cette journée existe déjà : modifiez-la depuis l'historique.";

        var saisies = heures.Where(h => h is not null).Select(h => JourStocke.VersMinutes(h!.Value)).ToList();
        jour = new JourStocke(date, saisies, AbsenceChoisie.Absence);
        return null;
    }

    public void Enregistrer()
    {
        if (_resultat is null) return;
        _state.Enregistrer(_resultat);
    }

    public void Supprimer()
    {
        if (_dateInitiale is { } date) _state.Supprimer(date);
    }
}

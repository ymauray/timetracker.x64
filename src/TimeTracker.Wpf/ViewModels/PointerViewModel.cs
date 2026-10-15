using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeTracker.Wpf.Services;

namespace TimeTracker.Wpf.ViewModels;

/// Le sens d'un ecart, pour le colorer.
public enum Ton { Neutre, Positif, Negatif }

/// L'ecran Pointer : le bouton, les pointages du jour, le realise en cours et les soldes.
public sealed partial class PointerViewModel : ObservableObject
{
    private readonly AppState _state;

    [ObservableProperty] private string _dateTexte = "";
    [ObservableProperty] private string _realiseTexte = "0h00";
    [ObservableProperty] private string _journeeTexte = "";
    [ObservableProperty] private string _libelleBouton = "Arrivée";
    [ObservableProperty] private string _libelleActionSuivante = "";
    [ObservableProperty] private bool _peutPointer = true;
    [ObservableProperty] private bool _peutAnnuler;
    [ObservableProperty] private string _ecartSemaineTexte = "0h00";
    [ObservableProperty] private Ton _ecartSemaineTon;
    [ObservableProperty] private string _soldeTexte = "0h00";
    [ObservableProperty] private Ton _soldeTon;
    [ObservableProperty] private string _toleranceTexte = "";
    [ObservableProperty] private string _alerteTolerance = "";
    [ObservableProperty] private string _alerteIncompletes = "";
    [ObservableProperty] private string _message = "";

    public ObservableCollection<string> Pointages { get; } = [];

    public bool AlerteToleranceVisible => AlerteTolerance.Length > 0;

    public PointerViewModel(AppState state)
    {
        _state = state;
        state.Changed += Rafraichir;
        Rafraichir();
    }

    public void Rafraichir()
    {
        var (date, minute) = _state.Maintenant();
        var config = _state.Config;
        var jour = _state.Donnees.Jour(date);
        var bilan = new Bilan(_state.Donnees.Jours, date, config);

        var texteDate = Formats.DateLongue(date);
        DateTexte = char.ToUpper(texteDate[0]) + texteDate[1..];
        RealiseTexte = Formats.Duree(jour.Realise(minute, config));
        JourneeTexte = $"Journée de référence : {Formats.Duree(config.DureeJournee)}";

        var action = jour.ActionSuivante;
        PeutPointer = action is not null;
        LibelleBouton = action ?? (JourStocke.PorteDesHeures(jour.Absence) ? "Journée complète" : "Jour d'absence");
        LibelleActionSuivante = action is null ? "" : "Prochain pointage";
        PeutAnnuler = jour.Heures.Count > 0;

        Pointages.Clear();
        var noms = jour.NomsDesHeures;
        for (var i = 0; i < jour.Heures.Count; i++)
            Pointages.Add($"{noms[i]}  {Formats.Heure(jour.Heures[i])}");
        if (jour.Absence != Absence.Aucune)
            Pointages.Add(FormatHelpers.NomAbsence(jour.Absence));

        EcartSemaineTexte = Formats.Ecart(bilan.EcartSemaine);
        EcartSemaineTon = TonDe(bilan.EcartSemaine);
        SoldeTexte = Formats.Ecart(bilan.SoldeCumule);
        SoldeTon = bilan.HorsTolerance ? Ton.Negatif : TonDe(bilan.SoldeCumule);
        ToleranceTexte = $"tolérance {FormatHelpers.FormatTolerance(config.Tolerance)}";
        AlerteTolerance = bilan.HorsTolerance
            ? $"Le solde dépasse la tolérance ({FormatHelpers.FormatTolerance(config.Tolerance)})."
            : "";
        AlerteIncompletes = bilan.JourneesIncompletes.Count switch
        {
            0 => "",
            1 => $"Le {FormatHelpers.FormatDate(bilan.JourneesIncompletes[0])} n'a pas de départ : corrigez-le dans l'historique.",
            var n => $"{n} journées passées n'ont pas de départ : corrigez-les dans l'historique.",
        };
    }

    partial void OnAlerteToleranceChanged(string value) => OnPropertyChanged(nameof(AlerteToleranceVisible));

    /// Execute le geste du bouton ; renvoie le texte d'une notification en cas de succes.
    public string? PointerMaintenant()
    {
        var action = _state.Aujourdhui.ActionSuivante;
        var refus = _state.Pointer();
        if (refus is not null)
        {
            Message = refus;
            return null;
        }
        Message = "";
        var (_, minute) = _state.Maintenant();
        return $"{action ?? "Pointage"} à {Formats.Heure(minute)}";
    }

    [RelayCommand]
    private void Pointer() => PointerMaintenant();

    [RelayCommand]
    private void Annuler()
    {
        _state.AnnulerDernierPointage();
        Message = "";
    }

    private static Ton TonDe(TimeSpan ecart) =>
        ecart > TimeSpan.Zero ? Ton.Positif : ecart < TimeSpan.Zero ? Ton.Negatif : Ton.Neutre;
}

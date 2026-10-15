using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using TimeTracker.Wpf.Services;

namespace TimeTracker.Wpf.ViewModels;

public enum Statut { Travail, Absence, EnCours, SansDepart, NonRenseigne }

/// Une ligne de l'historique.
public sealed record JourLigne(
    DateOnly Date, JourStocke Stocke, Statut Statut, string DateTexte, string Detail, string EcartTexte, Ton Ton)
{
    public bool EstAlerte => Statut is Statut.SansDepart or Statut.NonRenseigne;
}

/// Une semaine ISO et ses jours, le plus recent d'abord.
public sealed record SemaineLignes(string Titre, string EcartTexte, Ton Ton, IReadOnlyList<JourLigne> Jours);

/// L'ecran Historique : les jours par semaine, avec l'ecart de chacun.
public sealed partial class HistoriqueViewModel : ObservableObject
{
    private readonly AppState _state;

    public ObservableCollection<SemaineLignes> Semaines { get; } = [];

    [ObservableProperty] private bool _vide = true;

    public HistoriqueViewModel(AppState state)
    {
        _state = state;
        state.Changed += Rafraichir;
        Rafraichir();
    }

    public void Rafraichir()
    {
        var (aujourdhui, _) = _state.Maintenant();
        var config = _state.Config;
        var bilan = new Bilan(_state.Donnees.Jours, aujourdhui, config);
        var stockes = _state.Donnees.Jours.ToDictionary(j => j.Date);
        var calcules = bilan.Rapport?.Jours.ToDictionary(j => j.Entree.Date) ?? [];

        var dates = calcules.Keys
            .Union(stockes.Values.Where(j => j.Lecture != JourStocke.Etat.Vide).Select(j => j.Date))
            .OrderDescending();

        var lignes = dates.Select(date =>
        {
            var stocke = stockes.GetValueOrDefault(date) ?? new JourStocke(date);
            return Ligne(date, stocke, calcules.GetValueOrDefault(date), aujourdhui);
        }).ToList();

        Semaines.Clear();
        foreach (var groupe in lignes.GroupBy(l => Semaine(l.Date)).OrderByDescending(g => g.Key))
        {
            var (annee, numero) = groupe.Key;
            var lundi = DateOnly.FromDateTime(ISOWeek.ToDateTime(annee, numero, DayOfWeek.Monday));
            var calculee = bilan.Rapport?.Semaines.FirstOrDefault(s => s.AnneeIso == annee && s.SemaineIso == numero);
            Semaines.Add(new SemaineLignes(
                $"Semaine {numero} · {lundi:dd.MM} – {lundi.AddDays(6):dd.MM.yyyy}",
                calculee is null ? "" : Formats.Ecart(calculee.Ecart),
                calculee is null ? Ton.Neutre : TonDe(calculee.Ecart),
                [.. groupe]));
        }
        Vide = Semaines.Count == 0;
    }

    private static (int Annee, int Numero) Semaine(DateOnly date)
    {
        var d = date.ToDateTime(TimeOnly.MinValue);
        return (ISOWeek.GetYear(d), ISOWeek.GetWeekOfYear(d));
    }

    private static JourLigne Ligne(DateOnly date, JourStocke stocke, DayResult? calcule, DateOnly aujourdhui)
    {
        var statut = stocke.Lecture switch
        {
            JourStocke.Etat.Vide => Statut.NonRenseigne,
            JourStocke.Etat.EnCours => date < aujourdhui ? Statut.SansDepart : Statut.EnCours,
            _ => stocke.Heures.Count == 0 ? Statut.Absence : Statut.Travail,
        };

        var detail = statut switch
        {
            Statut.NonRenseigne => "Non renseigné",
            Statut.EnCours => "En cours" + Heures(stocke),
            Statut.SansDepart => "Sans départ" + Heures(stocke),
            Statut.Absence => FormatHelpers.NomAbsence(stocke.Absence),
            _ => stocke.Absence == Absence.Aucune ? Heures(stocke)[3..] : $"{FormatHelpers.NomAbsence(stocke.Absence)}{Heures(stocke)}",
        };

        // Aujourd'hui ne compte pas encore ; un jour sans depart compte en deficit.
        var ecart = calcule?.Ecart;
        return new JourLigne(date, stocke, statut, Formats.DateCourte(date), detail,
            ecart is null ? "" : Formats.Ecart(ecart.Value), ecart is null ? Ton.Neutre : TonDe(ecart.Value));
    }

    /// « · 08:00 – 12:00 · 12:30 – 17:00 » : le trait de liaison sert d'amorce, retire pour un jour travaille.
    private static string Heures(JourStocke j) => j.Heures.Count switch
    {
        0 => "",
        1 => $" · {Formats.Heure(j.Heures[0])}",
        2 => $" · {Formats.Heure(j.Heures[0])} – {Formats.Heure(j.Heures[1])}",
        3 => $" · {Formats.Heure(j.Heures[0])} · pause {Formats.Heure(j.Heures[1])} – {Formats.Heure(j.Heures[2])}",
        _ => $" · {Formats.Heure(j.Heures[0])} – {Formats.Heure(j.Heures[1])} · {Formats.Heure(j.Heures[2])} – {Formats.Heure(j.Heures[3])}",
    };

    private static Ton TonDe(TimeSpan ecart) =>
        ecart > TimeSpan.Zero ? Ton.Positif : ecart < TimeSpan.Zero ? Ton.Negatif : Ton.Neutre;
}

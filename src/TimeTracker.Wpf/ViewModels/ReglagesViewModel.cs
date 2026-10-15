using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TimeTracker.Wpf.Services;

namespace TimeTracker.Wpf.ViewModels;

/// Les reglages, valables pour tout l'historique, et l'echange de `releve.md` avec la CLI.
public sealed partial class ReglagesViewModel : ObservableObject
{
    private readonly AppState _state;

    [ObservableProperty] private string _dureeJournee = "";
    [ObservableProperty] private string _pauseMinimum = "";
    [ObservableProperty] private string _seuilPause = "";
    [ObservableProperty] private string _tolerance = "";
    [ObservableProperty] private string _erreur = "";
    [ObservableProperty] private string _confirmation = "";

    public string CheminDonnees => _state.CheminDonnees;

    public ReglagesViewModel(AppState state)
    {
        _state = state;
        state.Changed += Charger;
        Charger();
    }

    private void Charger()
    {
        var c = _state.Config;
        DureeJournee = Formats.Duree(c.DureeJournee);
        PauseMinimum = Formats.Duree(c.PauseMinimum);
        SeuilPause = Formats.Duree(c.SeuilPause);
        Tolerance = Formats.Duree(c.Tolerance);
    }

    [RelayCommand]
    private void Enregistrer()
    {
        Confirmation = "";
        var valeurs = new[]
        {
            ("durée de la journée", DureeJournee), ("pause minimum", PauseMinimum),
            ("seuil de pause", SeuilPause), ("tolérance", Tolerance),
        };
        var durees = new List<TimeSpan>();
        foreach (var (nom, texte) in valeurs)
        {
            if (Formats.LireDuree(texte) is not { } duree)
            {
                Erreur = $"Valeur invalide pour {nom} : « {texte.Trim()} » (exemple : 8h12).";
                return;
            }
            durees.Add(duree);
        }
        Erreur = "";
        _state.ChangerConfig(new ReleveConfig
        {
            DureeJournee = durees[0], PauseMinimum = durees[1], SeuilPause = durees[2], Tolerance = durees[3],
        });
        Confirmation = "Réglages enregistrés. Tous les soldes de l'historique ont été recalculés.";
    }

    [RelayCommand]
    private void Exporter()
    {
        var dialogue = new SaveFileDialog
        {
            Title = "Exporter le relevé",
            FileName = "releve.md",
            Filter = "Relevé markdown (*.md)|*.md|Tous les fichiers|*.*",
        };
        if (dialogue.ShowDialog() != true) return;
        // La CLI lit le fichier tel quel : UTF-8 sans BOM, fins de ligne LF.
        File.WriteAllText(dialogue.FileName, ReleveWriter.Ecrire(_state.Donnees.Jours, _state.Config), new UTF8Encoding(false));
        Confirmation = $"Relevé exporté : {dialogue.FileName}";
    }

    [RelayCommand]
    private void Importer()
    {
        var dialogue = new OpenFileDialog
        {
            Title = "Importer un relevé",
            Filter = "Relevé markdown (*.md)|*.md|Tous les fichiers|*.*",
        };
        if (dialogue.ShowDialog() != true) return;

        var lu = ReleveImport.Lire(File.ReadAllLines(dialogue.FileName));
        if (lu.Donnees is null)
        {
            Erreur = "Import impossible :\n" + string.Join('\n', lu.Erreurs.Take(10).Select(e => $"ligne {e.Ligne} : {e.Message}"))
                     + (lu.Erreurs.Count > 10 ? $"\n… et {lu.Erreurs.Count - 10} autre(s)." : "");
            Confirmation = "";
            return;
        }

        var confirmer = MessageBox.Show(
            $"L'import remplace tout l'historique actuel ({_state.Donnees.Jours.Count} jour(s)) par les {lu.Donnees.Jours.Count} jour(s) du fichier, " +
            "réglages compris. Continuer ?",
            "Importer un relevé", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (confirmer != MessageBoxResult.Yes) return;

        _state.Remplacer(lu.Donnees);
        Erreur = "";
        Confirmation = $"{lu.Donnees.Jours.Count} jour(s) importé(s).";
    }
}

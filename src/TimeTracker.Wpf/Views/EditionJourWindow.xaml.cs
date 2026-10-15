using System.Windows;
using TimeTracker.Wpf.ViewModels;

namespace TimeTracker.Wpf.Views;

public partial class EditionJourWindow : Window
{
    private readonly EditionJourViewModel _modele;

    public EditionJourWindow(EditionJourViewModel modele)
    {
        InitializeComponent();
        _modele = modele;
        DataContext = modele;
    }

    private void Enregistrer_Click(object sender, RoutedEventArgs e)
    {
        _modele.Enregistrer();
        DialogResult = true;
    }

    private void Supprimer_Click(object sender, RoutedEventArgs e)
    {
        var confirmer = MessageBox.Show(this, "Supprimer tous les pointages de cette journée ?", "Supprimer la journée",
            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (confirmer != MessageBoxResult.Yes) return;
        _modele.Supprimer();
        DialogResult = true;
    }
}

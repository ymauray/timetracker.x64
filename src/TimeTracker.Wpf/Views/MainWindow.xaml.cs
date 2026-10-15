using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using TimeTracker.Wpf.ViewModels;
using TimeTracker;

namespace TimeTracker.Wpf.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _modele;

    /// Vrai quand l'utilisateur quitte via le menu de l'icone : sinon, fermer la fenetre la cache.
    public bool Quitter { get; set; }

    public MainWindow(MainViewModel modele)
    {
        InitializeComponent();
        _modele = modele;
        DataContext = modele;
        // Au retour au premier plan, relire l'heure et les donnees.
        Activated += (_, _) => modele.State.Rafraichir();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!Quitter)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }

    public void Afficher()
    {
        Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
    }

    private void Modifier_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: JourLigne ligne })
            Editer(ligne.Stocke);
    }

    private void Ajouter_Click(object sender, RoutedEventArgs e) => Editer(null);

    private void Editer(JourStocke? jour)
    {
        var fenetre = new EditionJourWindow(new EditionJourViewModel(_modele.State, jour)) { Owner = this };
        fenetre.ShowDialog();
    }
}

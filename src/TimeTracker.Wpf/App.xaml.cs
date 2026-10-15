using System.IO;
using System.Windows;
using System.Windows.Threading;
using TimeTracker.Wpf.Services;
using TimeTracker.Wpf.ViewModels;
using TimeTracker.Wpf.Views;

namespace TimeTracker.Wpf;

public partial class App : Application
{
    private const uint ToucheP = 0x50;

    private Mutex? _instanceUnique;
    private AppState? _state;
    private MainWindow? _fenetre;
    private TrayIcon? _icone;
    private GlobalHotKey? _raccourci;
    private DispatcherTimer? _minuteur;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Une seule instance : la seconde se contente de se fermer.
        _instanceUnique = new Mutex(true, @"Local\TimeTracker.Pointage", out var premiere);
        if (!premiere)
        {
            Shutdown();
            return;
        }

        // --smoke : ouvre les fenetres puis quitte (code 1 et trace dans %TEMP% si le XAML ou le demarrage echoue).
        var smoke = e.Args.Contains("--smoke");

        DispatcherUnhandledException += (_, args) =>
        {
            args.Handled = true;
            if (smoke)
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "pointage-smoke.txt"), args.Exception.ToString());
                Shutdown(1);
                return;
            }
            MessageBox.Show(args.Exception.Message, "Pointage", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        try
        {
            _state = new AppState(new DepotDonnees(DepotDonnees.CheminParDefaut()));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Impossible de lire {DepotDonnees.CheminParDefaut()} :\n{ex.Message}", "Pointage",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        var modele = new MainViewModel(_state);
        _fenetre = new MainWindow(modele);
        _icone = new TrayIcon(ouvrir: _fenetre.Afficher, pointer: () => PointerDepuisRaccourci(modele), quitter: Quitter);

        _raccourci = new GlobalHotKey(() => PointerDepuisRaccourci(modele));
        if (!_raccourci.Enregistrer(ToucheP))
            _icone.Notifier("Pointage", "Ctrl+Alt+P est déjà pris par une autre application : le raccourci global est désactivé.");

        // Chaque minute : relire l'heure (le realise en cours avance, minuit change la journee).
        _minuteur = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
        _minuteur.Tick += (_, _) =>
        {
            _state.Rafraichir();
            _icone.Info = $"Pointage · réalisé {modele.Pointer.RealiseTexte}";
        };
        _minuteur.Start();

        // Lancee avec --minimise (ex. au demarrage de Windows) : reste dans la zone de notification.
        if (!e.Args.Contains("--minimise")) _fenetre.Afficher();

        if (smoke)
        {
            new EditionJourWindow(new EditionJourViewModel(_state, null)) { Owner = _fenetre }.Show();
            var fin = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            fin.Tick += (_, _) => Quitter();
            fin.Start();
        }
    }

    private void PointerDepuisRaccourci(MainViewModel modele)
    {
        var notification = modele.Pointer.PointerMaintenant();
        _icone?.Notifier("Pointage", notification ?? modele.Pointer.Message);
    }

    private void Quitter()
    {
        if (_fenetre is not null) _fenetre.Quitter = true;
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _minuteur?.Stop();
        _raccourci?.Dispose();
        _icone?.Dispose();
        _instanceUnique?.Dispose();
        base.OnExit(e);
    }
}

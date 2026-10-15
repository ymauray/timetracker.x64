namespace TimeTracker.Wpf.Services;

/// L'etat partage de l'application : les donnees sur disque et les gestes qui les
/// modifient. Les ecrans s'abonnent a `Changed` et se relisent.
public sealed class AppState(DepotDonnees depot)
{
    public Donnees Donnees { get; private set; } = depot.Charger();

    public string CheminDonnees => depot.Chemin;

    public ReleveConfig Config => Donnees.Config;

    public event Action? Changed;

    /// La date et l'heure (en minutes depuis minuit, secondes tronquees) du moment.
    public (DateOnly Date, int Minute) Maintenant()
    {
        var maintenant = DateTime.Now;
        return (DateOnly.FromDateTime(maintenant), maintenant.Hour * 60 + maintenant.Minute);
    }

    public JourStocke Aujourdhui => Donnees.Jour(Maintenant().Date);

    /// Relit l'horloge : a chaque minute et au retour au premier plan.
    public void Rafraichir() => Changed?.Invoke();

    /// Le geste du bouton Pointer. Renvoie le message du refus, ou null si le pointage est enregistre.
    public string? Pointer()
    {
        var (date, minute) = Maintenant();
        if (!Donnees.Jour(date).TryPointer(minute, out var jour, out var refus)) return refus;
        Enregistrer(jour);
        return null;
    }

    public void AnnulerDernierPointage()
    {
        var jour = Aujourdhui;
        Enregistrer(jour.SansDernierPointage());
    }

    public void Enregistrer(JourStocke jour)
    {
        Donnees.Enregistrer(jour);
        Sauvegarder();
    }

    public void Supprimer(DateOnly date) => Enregistrer(new JourStocke(date));

    public void ChangerConfig(ReleveConfig config)
    {
        Donnees.Config = config;
        Sauvegarder();
    }

    /// Remplace tout l'historique et les reglages (import d'un `releve.md`).
    public void Remplacer(Donnees donnees)
    {
        Donnees = donnees;
        Sauvegarder();
    }

    private void Sauvegarder()
    {
        depot.Enregistrer(Donnees);
        Changed?.Invoke();
    }
}

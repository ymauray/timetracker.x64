namespace TimeTracker.Tests;

public class JourStockeTests
{
    private static readonly DateOnly Lundi = new(2026, 9, 28);

    private static JourStocke Pointer(JourStocke jour, params int[] minutes)
    {
        foreach (var m in minutes)
        {
            Assert.True(jour.TryPointer(m, out jour, out var refus), refus);
        }
        return jour;
    }

    [Theory]
    [InlineData(0, "Arrivée")]
    [InlineData(1, "Pause ou départ")]
    [InlineData(2, "Fin de pause")]
    [InlineData(3, "Départ")]
    [InlineData(4, null)]
    public void ActionSuivante_AnnonceLeProchainAppui(int pointages, string? attendu)
    {
        var jour = Pointer(new JourStocke(Lundi), [.. Enumerable.Range(0, pointages).Select(i => 480 + i * 60)]);
        Assert.Equal(attendu, jour.ActionSuivante);
    }

    [Theory]
    [InlineData(0, JourStocke.Etat.Vide)]
    [InlineData(1, JourStocke.Etat.EnCours)]
    [InlineData(2, JourStocke.Etat.Complete)]
    [InlineData(3, JourStocke.Etat.EnCours)]
    [InlineData(4, JourStocke.Etat.Complete)]
    public void Lecture_SelonLeNombreDeHeures(int pointages, JourStocke.Etat attendu)
    {
        var jour = Pointer(new JourStocke(Lundi), [.. Enumerable.Range(0, pointages).Select(i => 480 + i * 60)]);
        Assert.Equal(attendu, jour.Lecture);
    }

    [Fact]
    public void CinquiemeAppui_EstRefuse()
    {
        var jour = Pointer(new JourStocke(Lundi), 480, 720, 750, 1020);
        Assert.False(jour.TryPointer(1100, out _, out var refus));
        Assert.Equal("La journée a déjà ses quatre pointages.", refus);
    }

    [Fact]
    public void MemeMinute_EstRefusee()
    {
        var jour = Pointer(new JourStocke(Lundi), 480);
        Assert.False(jour.TryPointer(480, out _, out _));
        Assert.False(jour.TryPointer(470, out _, out _));
    }

    [Fact]
    public void JourDAbsence_RefuseLePointage_SaufDemiEtMaladie()
    {
        Assert.False(new JourStocke(Lundi, [], Absence.Conges).TryPointer(480, out _, out _));
        Assert.True(new JourStocke(Lundi, [], Absence.Demi).TryPointer(480, out _, out _));
        Assert.True(new JourStocke(Lundi, [], Absence.Maladie).TryPointer(480, out _, out _));
    }

    [Fact]
    public void Annuler_RetireLeDernierPointage()
    {
        var jour = Pointer(new JourStocke(Lundi), 480, 720).SansDernierPointage();
        Assert.Equal([480], jour.Heures);
        Assert.Equal(new JourStocke(Lundi), new JourStocke(Lundi).SansDernierPointage());
    }

    [Fact]
    public void Realise_EnCours_RetirePauseReelleSeulement()
    {
        var config = ReleveConfig.Defaut;
        // arrive 8h00, pause 12h00-12h30, il est 14h00 : 4h + 1h30 = 5h30
        var jour = Pointer(new JourStocke(Lundi), 480, 720, 750);
        Assert.Equal(330, jour.Realise(840, config));
        // une seule heure : temps ecoule depuis l'arrivee
        Assert.Equal(120, Pointer(new JourStocke(Lundi), 480).Realise(600, config));
        // pause en cours (2 heures : arrivee + depart) → resultat du calcul : 8h00-17h00 = 9h - 30 min
        Assert.Equal(510, Pointer(new JourStocke(Lundi), 480, 1020).Realise(0, config));
    }

    [Fact]
    public void VersEntree_RoundTrip()
    {
        var jour = Pointer(new JourStocke(Lundi), 480, 720, 750, 1020);
        Assert.Equal(jour, JourStocke.DepuisEntree(jour.VersEntree()!));
    }
}

public class BilanTests
{
    [Fact]
    public void AujourdhuiEtJournesIncompletes_NeComptentPas()
    {
        var aujourdhui = new DateOnly(2026, 10, 1); // jeudi
        var jours = new[]
        {
            new JourStocke(new DateOnly(2026, 9, 28), [480, 720, 750, 1020]),   // 8h00-17h00, 30 min : 8h30 → +18
            new JourStocke(new DateOnly(2026, 9, 29), [480]),                  // sans depart : ecarte, signale
            new JourStocke(aujourdhui, [480, 1200]),                           // aujourd'hui : ecarte
        };

        var bilan = new Bilan(jours, aujourdhui, ReleveConfig.Defaut);

        Assert.Equal([new DateOnly(2026, 9, 29)], bilan.JourneesIncompletes);
        // 28/9 : +18 ; 29/9 et 30/9 ouvres sans journee complete : -492 chacun
        Assert.Equal(TimeSpan.FromMinutes(18 - 492 * 2), bilan.SoldeCumule);
        Assert.Equal(bilan.SoldeCumule, bilan.EcartSemaine);
    }

    [Fact]
    public void SansJourneeComplete_PasDeRapport()
    {
        var bilan = new Bilan([new JourStocke(new DateOnly(2026, 10, 1), [480])], new DateOnly(2026, 10, 1), ReleveConfig.Defaut);
        Assert.Null(bilan.Rapport);
        Assert.Equal(TimeSpan.Zero, bilan.SoldeCumule);
    }
}

public class ReleveEchangeTests
{
    [Fact]
    public void Export_PuisImport_RendLesMemesJours()
    {
        var jours = new[]
        {
            new JourStocke(new DateOnly(2026, 9, 28), [480, 720, 750, 1020]),
            new JourStocke(new DateOnly(2026, 9, 29), [480, 1020]),
            new JourStocke(new DateOnly(2026, 9, 30), [], Absence.Conges),
            new JourStocke(new DateOnly(2026, 10, 1), [540, 720], Absence.Demi),
            new JourStocke(new DateOnly(2026, 10, 2), [480]), // incomplete : non exportee
        };
        var config = new ReleveConfig
        {
            DureeJournee = new TimeSpan(8, 0, 0),
            PauseMinimum = TimeSpan.FromMinutes(45),
            SeuilPause = TimeSpan.FromHours(4),
            Tolerance = TimeSpan.FromHours(12),
        };

        var texte = ReleveWriter.Ecrire(jours, config);
        var relu = ReleveImport.Lire(texte.Split('\n'));

        Assert.Empty(relu.Erreurs);
        Assert.Equal(config.DureeJournee, relu.Donnees!.Config.DureeJournee);
        Assert.Equal(config.PauseMinimum, relu.Donnees.Config.PauseMinimum);
        Assert.Equal(config.SeuilPause, relu.Donnees.Config.SeuilPause);
        Assert.Equal(config.Tolerance, relu.Donnees.Config.Tolerance);
        Assert.Equal(jours.Take(4), relu.Donnees.Jours);
    }

    [Fact]
    public void Import_RemonteLesErreursDeLaCli()
    {
        var relu = ReleveImport.Lire(File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "erreurs", "releve.md")));
        Assert.Null(relu.Donnees);
        Assert.Equal(8, relu.Erreurs.Count);
    }
}

public class DepotDonneesTests
{
    [Fact]
    public void Enregistrer_PuisCharger_RendLesMemesDonnees()
    {
        var dossier = Path.Combine(Path.GetTempPath(), "pointage-tests-" + Guid.NewGuid());
        try
        {
            var depot = new DepotDonnees(Path.Combine(dossier, "donnees.json"));
            var donnees = new Donnees();
            donnees.Enregistrer(new JourStocke(new DateOnly(2026, 10, 2), [480, 700]));
            donnees.Enregistrer(new JourStocke(new DateOnly(2026, 10, 1), [], Absence.RTT));
            donnees.Config = new ReleveConfig
            {
                DureeJournee = new TimeSpan(7, 0, 0),
                PauseMinimum = TimeSpan.FromMinutes(20),
                SeuilPause = TimeSpan.FromHours(6),
                Tolerance = TimeSpan.FromHours(8),
            };

            depot.Enregistrer(donnees);
            var relu = depot.Charger();

            Assert.Equal(donnees.Jours, relu.Jours);
            Assert.Equal(donnees.Config.DureeJournee, relu.Config.DureeJournee);
            Assert.Equal(donnees.Config.Tolerance, relu.Config.Tolerance);
            Assert.Equal(new DateOnly(2026, 10, 1), relu.Jours[0].Date);
        }
        finally
        {
            if (Directory.Exists(dossier)) Directory.Delete(dossier, true);
        }
    }

    [Fact]
    public void Charger_FichierAbsent_DonneesParDefaut()
    {
        var depot = new DepotDonnees(Path.Combine(Path.GetTempPath(), "absent-" + Guid.NewGuid(), "x.json"));
        var donnees = depot.Charger();
        Assert.Empty(donnees.Jours);
        Assert.Equal(ReleveConfig.Defaut.DureeJournee, donnees.Config.DureeJournee);
    }

    [Fact]
    public void Enregistrer_UnJourVide_LeRetire()
    {
        var donnees = new Donnees();
        var date = new DateOnly(2026, 10, 1);
        donnees.Enregistrer(new JourStocke(date, [480]));
        donnees.Enregistrer(new JourStocke(date, []));
        Assert.Empty(donnees.Jours);
    }
}

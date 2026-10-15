namespace TimeTracker.Tests;

public class TimeCalculatorTests
{
    private static readonly ReleveConfig Config = ReleveConfig.Defaut; // 8h12 / pause min 0h30 / tolerance 10h

    private static DayEntry Jour(DateOnly date, string arrivee, string? debutPause, string? finPause, string depart) => new()
    {
        Date = date,
        Arrivee = TimeOnly.Parse(arrivee),
        DebutPause = debutPause is null ? null : TimeOnly.Parse(debutPause),
        FinPause = finPause is null ? null : TimeOnly.Parse(finPause),
        Depart = TimeOnly.Parse(depart),
    };

    [Fact]
    public void PauseCourte_EstDecompteeAuMinimum()
    {
        // pause reelle de 20 min, minimum configure a 30 min
        var jour = Jour(new DateOnly(2026, 9, 28), "08:00", "12:00", "12:20", "17:00");
        var rapport = TimeCalculator.Calculer([jour], Config);

        var resultat = Assert.Single(rapport.Jours);
        Assert.Equal(TimeSpan.FromMinutes(30), resultat.PauseDecomptee);
        Assert.Equal(new TimeSpan(8, 30, 0), resultat.TempsRealise); // 9h00 - 0h30
    }

    [Fact]
    public void PauseLongue_EstDecompteeReellement()
    {
        // pause reelle de 45 min, superieure au minimum de 30 min
        var jour = Jour(new DateOnly(2026, 9, 28), "08:00", "12:00", "12:45", "17:00");
        var rapport = TimeCalculator.Calculer([jour], Config);

        var resultat = Assert.Single(rapport.Jours);
        Assert.Equal(TimeSpan.FromMinutes(45), resultat.PauseDecomptee);
    }

    [Fact]
    public void JourSansPause_PerdLaPauseMinimum()
    {
        var jour = Jour(new DateOnly(2026, 9, 28), "08:00", null, null, "17:00");
        var rapport = TimeCalculator.Calculer([jour], Config);

        var resultat = Assert.Single(rapport.Jours);
        Assert.Equal(TimeSpan.FromMinutes(30), resultat.PauseDecomptee);
        Assert.Equal(new TimeSpan(8, 30, 0), resultat.TempsRealise); // 9h00 - 0h30
    }

    [Fact]
    public void JourAbsence_EstNeutreSurLeSolde()
    {
        var jour = new DayEntry { Date = new DateOnly(2026, 9, 28), Absence = Absence.Conges };
        var rapport = TimeCalculator.Calculer([jour], Config);

        var resultat = Assert.Single(rapport.Jours);
        Assert.Equal(resultat.TempsTheorique, resultat.TempsRealise);
        Assert.Equal(TimeSpan.Zero, resultat.Ecart);
    }

    [Fact]
    public void JourOuvreNonRenseigne_EstCompteEnDeficit()
    {
        // lundi et mercredi saisis, mardi manquant entre les deux
        var lundi = Jour(new DateOnly(2026, 9, 28), "08:00", "12:00", "12:30", "17:12");
        var mercredi = Jour(new DateOnly(2026, 9, 30), "08:00", "12:00", "12:30", "17:12");
        var rapport = TimeCalculator.Calculer([lundi, mercredi], Config);

        var manquant = Assert.Single(rapport.JoursNonRenseignes);
        Assert.Equal(new DateOnly(2026, 9, 29), manquant);

        var jourManquant = rapport.Jours.Single(j => j.Entree.Date == manquant);
        Assert.Equal(TimeSpan.Zero, jourManquant.TempsRealise);
        Assert.Equal(Config.DureeJournee, jourManquant.TempsTheorique);
    }

    [Fact]
    public void WeekEnd_NonRenseigne_NestPasCompteEnDeficit()
    {
        var vendredi = Jour(new DateOnly(2026, 10, 2), "08:00", "12:00", "12:30", "17:12");
        var lundiSuivant = Jour(new DateOnly(2026, 10, 5), "08:00", "12:00", "12:30", "17:12");
        var rapport = TimeCalculator.Calculer([vendredi, lundiSuivant], Config);

        Assert.Empty(rapport.JoursNonRenseignes);
    }

    [Fact]
    public void BilanHebdomadaire_CumuleLEcartEntreSemaines()
    {
        var jours = new List<DayEntry>
        {
            Jour(new DateOnly(2026, 9, 28), "08:00", "12:00", "12:30", "16:00"), // semaine courte
            Jour(new DateOnly(2026, 10, 5), "08:00", "12:00", "12:30", "18:00"), // semaine longue
        };
        var rapport = TimeCalculator.Calculer(jours, Config);

        Assert.Equal(2, rapport.Semaines.Count);
        var s1 = rapport.Semaines[0];
        var s2 = rapport.Semaines[1];
        Assert.Equal(s1.Ecart, s1.EcartCumule);
        Assert.Equal(s1.Ecart + s2.Ecart, s2.EcartCumule);
    }

    [Fact]
    public void BilanMensuel_SoldeSeReportSurLeMoisSuivant()
    {
        var jours = new List<DayEntry>
        {
            Jour(new DateOnly(2026, 9, 28), "08:00", "12:00", "12:30", "16:00"), // sept, deficit
            Jour(new DateOnly(2026, 10, 5), "08:00", "12:00", "12:30", "17:12"), // oct, pile
        };
        var rapport = TimeCalculator.Calculer(jours, Config);

        Assert.Equal(2, rapport.Mois.Count);
        var septembre = rapport.Mois[0];
        var octobre = rapport.Mois[1];
        Assert.Equal(septembre.Ecart, septembre.SoldeCumule);
        Assert.Equal(septembre.Ecart + octobre.Ecart, octobre.SoldeCumule);
    }

    [Fact]
    public void MoisHorsTolerance_QuandLeSoldeDepasseDixHeures()
    {
        // un seul jour avec un enorme depassement (23h sans pause) pour forcer le solde au-dela de 10h
        var jour = Jour(new DateOnly(2026, 9, 28), "00:00", null, null, "23:00");
        var rapport = TimeCalculator.Calculer([jour], Config);

        var mois = Assert.Single(rapport.Mois);
        Assert.True(mois.HorsTolerance);
    }

    [Fact]
    public void ToleranceConfiguree_RemplaceLesDixHeures()
    {
        // lundi seul, sans pause : 0h30 realise pour 8h12 theoriques, solde -7h42
        var jour = Jour(new DateOnly(2026, 9, 28), "08:00", null, null, "09:00");

        var large = TimeCalculator.Calculer([jour], ReleveConfig.Defaut);
        Assert.False(Assert.Single(large.Mois).HorsTolerance);

        var serree = new ReleveConfig { DureeJournee = ReleveConfig.Defaut.DureeJournee, PauseMinimum = ReleveConfig.Defaut.PauseMinimum, SeuilPause = ReleveConfig.Defaut.SeuilPause, Tolerance = TimeSpan.FromHours(5) };
        var rapport = TimeCalculator.Calculer([jour], serree);
        Assert.True(Assert.Single(rapport.Mois).HorsTolerance);
    }

    [Fact]
    public void JourneeCourte_SansPause_NePerdPasLaPauseMinimum()
    {
        // 4h59 de presence, sous le seuil de 5h
        var jour = Jour(new DateOnly(2026, 9, 28), "08:00", null, null, "12:59");
        var rapport = TimeCalculator.Calculer([jour], Config);

        var resultat = Assert.Single(rapport.Jours);
        Assert.Equal(TimeSpan.Zero, resultat.PauseDecomptee);
        Assert.Equal(new TimeSpan(4, 59, 0), resultat.TempsRealise);
    }

    [Fact]
    public void JourneeAuSeuil_PerdLaPauseMinimum()
    {
        // 5h00 de presence pile : le seuil est atteint
        var jour = Jour(new DateOnly(2026, 9, 28), "08:00", null, null, "13:00");
        var rapport = TimeCalculator.Calculer([jour], Config);

        var resultat = Assert.Single(rapport.Jours);
        Assert.Equal(TimeSpan.FromMinutes(30), resultat.PauseDecomptee);
        Assert.Equal(new TimeSpan(4, 30, 0), resultat.TempsRealise);
    }

    [Fact]
    public void JourneeCourte_AvecPause_DecompteLaPauseReelle()
    {
        var jour = Jour(new DateOnly(2026, 9, 28), "08:00", "10:00", "10:15", "12:00");
        var rapport = TimeCalculator.Calculer([jour], Config);

        var resultat = Assert.Single(rapport.Jours);
        Assert.Equal(TimeSpan.FromMinutes(15), resultat.PauseDecomptee);
        Assert.Equal(new TimeSpan(3, 45, 0), resultat.TempsRealise);
    }

    [Fact]
    public void DemiJournee_AjouteLaMoitieDuTheoriqueAuTravail()
    {
        // 4h travaillees + 4h06 de demi-journee : 6 min de deficit
        var jour = Jour(new DateOnly(2026, 9, 28), "08:00", null, null, "12:00");
        var rapport = TimeCalculator.Calculer([Absent(jour, Absence.Demi)], Config);

        var resultat = Assert.Single(rapport.Jours);
        Assert.Equal(TimeSpan.Zero, resultat.PauseDecomptee); // 4h de presence, sous le seuil
        Assert.Equal(new TimeSpan(8, 6, 0), resultat.TempsRealise);
    }

    [Fact]
    public void DemiJournee_ArrondiLaMoitieALaMinuteInferieure()
    {
        var config = new ReleveConfig { DureeJournee = new TimeSpan(7, 5, 0), PauseMinimum = Config.PauseMinimum, SeuilPause = Config.SeuilPause, Tolerance = Config.Tolerance };
        var jour = Jour(new DateOnly(2026, 9, 28), "08:00", null, null, "12:00");
        var rapport = TimeCalculator.Calculer([Absent(jour, Absence.Demi)], config);

        Assert.Equal(TimeSpan.FromMinutes(240 + 212), Assert.Single(rapport.Jours).TempsRealise); // 7h05 / 2 = 3h32,5 -> 3h32
    }

    [Fact]
    public void MaladieEnCoursDeJournee_CompleteJusquALaJourneeEntiere()
    {
        var jour = Jour(new DateOnly(2026, 9, 28), "07:50", null, null, "09:00");
        var rapport = TimeCalculator.Calculer([Absent(jour, Absence.Maladie)], Config);

        Assert.Equal(Config.DureeJournee, Assert.Single(rapport.Jours).TempsRealise);
    }

    [Fact]
    public void MaladieApresUneJourneePleine_GardeLeTempsTravaille()
    {
        var jour = Jour(new DateOnly(2026, 9, 28), "08:00", "12:00", "12:30", "17:30");
        var rapport = TimeCalculator.Calculer([Absent(jour, Absence.Maladie)], Config);

        Assert.Equal(new TimeSpan(9, 0, 0), Assert.Single(rapport.Jours).TempsRealise);
    }

    private static DayEntry Absent(DayEntry jour, Absence absence) => new()
    {
        Date = jour.Date,
        Absence = absence,
        Arrivee = jour.Arrivee,
        DebutPause = jour.DebutPause,
        FinPause = jour.FinPause,
        Depart = jour.Depart,
    };

    [Fact]
    public void ConfigPersonnalisee_ChangeLObjectifEtLaPauseMinimum()
    {
        var config = new ReleveConfig { DureeJournee = new TimeSpan(7, 0, 0), PauseMinimum = TimeSpan.FromMinutes(45), SeuilPause = ReleveConfig.Defaut.SeuilPause, Tolerance = TimeSpan.FromHours(10) };
        var jour = Jour(new DateOnly(2026, 9, 28), "08:00", "12:00", "12:20", "15:30");
        var rapport = TimeCalculator.Calculer([jour], config);

        var resultat = Assert.Single(rapport.Jours);
        Assert.Equal(new TimeSpan(7, 0, 0), resultat.TempsTheorique);
        Assert.Equal(TimeSpan.FromMinutes(45), resultat.PauseDecomptee); // pause reelle (20min) < minimum (45min)
    }
}

namespace TimeTracker;

/// Les regles de coherence d'une journee, reprises de ReleveParser : le parser et
/// l'ecran d'edition passent tous deux par ici.
public static class ValidationJournee
{
    public enum Erreur
    {
        HorairesSurAbsence,
        DemiSansHoraires,
        ArriveeEtDepartObligatoires,
        DepartAvantArrivee,
        PauseIncomplete,
        FinPauseAvantDebut,
        PauseHorsJournee,
    }

    public sealed record Resultat(Erreur? Erreur, string? MessageReleve, string? MessageEcran)
    {
        public bool EstValide => Erreur is null;
    }

    /// Premiere regle enfreinte, dans l'ordre de la CLI, ou un resultat valide.
    public static Resultat Valider(TimeOnly? arrivee, TimeOnly? debutPause, TimeOnly? finPause, TimeOnly? depart, Absence absence)
    {
        var horairesSaisis = arrivee is not null || debutPause is not null || finPause is not null || depart is not null;
        if (absence == Absence.Demi && !horairesSaisis)
            return Echec(Erreur.DemiSansHoraires,
                "Le code Demi demande les horaires de la demi-journee travaillee.",
                "Une demi-journée demande les heures de la partie travaillée.");
        if (absence == Absence.Aucune || horairesSaisis)
        {
            // Seules une demi-journee et une maladie survenue en cours de journee portent des horaires.
            if (absence is not (Absence.Aucune or Absence.Demi or Absence.Maladie))
                return Echec(Erreur.HorairesSurAbsence,
                    "Jour marque en absence mais des horaires sont renseignes : videz les colonnes horaires ou retirez le code absence.",
                    "Une journée d'absence ne peut pas avoir d'horaires.");
            if (arrivee is null || depart is null)
                return Echec(Erreur.ArriveeEtDepartObligatoires,
                    "Arrivee et Depart sont obligatoires pour un jour travaille (ou renseignez un code Absence).",
                    "Renseignez l'arrivée et le départ, ou choisissez une absence.");
            if (depart <= arrivee)
                return Echec(Erreur.DepartAvantArrivee,
                    $"Depart ({FormatHelpers.FormatHeure(depart)}) doit etre apres Arrivee ({FormatHelpers.FormatHeure(arrivee)}).",
                    "Le départ doit suivre l'arrivée.");
            if ((debutPause is null) != (finPause is null))
                return Echec(Erreur.PauseIncomplete,
                    "Debut pause et Fin pause doivent etre soit tous les deux renseignes, soit tous les deux vides.",
                    "Renseignez le début et la fin de la pause, ou aucun des deux.");
            if (debutPause is not null && finPause <= debutPause)
                return Echec(Erreur.FinPauseAvantDebut,
                    $"Fin pause ({FormatHelpers.FormatHeure(finPause)}) doit etre apres Debut pause ({FormatHelpers.FormatHeure(debutPause)}).",
                    "La fin de la pause doit suivre son début.");
            if (debutPause is not null && (debutPause < arrivee || finPause > depart))
                return Echec(Erreur.PauseHorsJournee,
                    "La pause doit etre comprise entre l'arrivee et le depart.",
                    "La pause doit se situer entre l'arrivée et le départ.");
        }
        return new Resultat(null, null, null);
    }

    private static Resultat Echec(Erreur erreur, string releve, string ecran) => new(erreur, releve, ecran);
}

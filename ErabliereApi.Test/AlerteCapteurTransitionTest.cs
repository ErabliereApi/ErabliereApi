using ErabliereApi.Donnees;
using ErabliereApi.Services.Notifications;
using System;
using Xunit;

namespace ErabliereApi.Test;

public class AlerteCapteurTransitionTest
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 13, 14, 0, 0, TimeSpan.FromHours(-4));

    [Theory]
    // Min seul
    [InlineData(0, null, -1, TypeTransitionAlerte.Debut)]
    [InlineData(0, null, 0, TypeTransitionAlerte.Debut)]
    [InlineData(0, null, 1, TypeTransitionAlerte.Aucune)]
    // Max seul
    [InlineData(null, 40, 41, TypeTransitionAlerte.Debut)]
    [InlineData(null, 40, 40, TypeTransitionAlerte.Debut)]
    [InlineData(null, 40, 39, TypeTransitionAlerte.Aucune)]
    // Min et max
    [InlineData(40, 70, 10, TypeTransitionAlerte.Debut)]
    [InlineData(40, 70, 90, TypeTransitionAlerte.Debut)]
    [InlineData(40, 70, 55, TypeTransitionAlerte.Aucune)]
    // Aucun seuil
    [InlineData(null, null, 55, TypeTransitionAlerte.Aucune)]
    public void AlerteNormale_CommenceSeulementHorsSeuil(int? min, int? max, int valeur, TypeTransitionAlerte attendu)
    {
        var alerte = new AlerteCapteur { MinValue = min, MaxValue = max };

        Assert.Equal(attendu, AlerteCapteurTransition.Evaluer(alerte, valeur, T0));
    }

    [Theory]
    [InlineData(0, null, 0, TypeTransitionAlerte.Aucune)]
    [InlineData(0, null, 1, TypeTransitionAlerte.Fin)]
    [InlineData(null, 40, 40, TypeTransitionAlerte.Aucune)]
    [InlineData(null, 40, 39, TypeTransitionAlerte.Fin)]
    [InlineData(40, 70, 55, TypeTransitionAlerte.Fin)]
    // Passer directement de sous le min à au-dessus du max garde l'alerte en cours
    [InlineData(40, 70, 90, TypeTransitionAlerte.Aucune)]
    public void AlerteEnCours_SeTermineSeulementAuRetourALaNormale(int? min, int? max, int valeur, TypeTransitionAlerte attendu)
    {
        var alerte = new AlerteCapteur { MinValue = min, MaxValue = max, EstEnAlerte = true, DateChangementEtat = T0 };

        Assert.Equal(attendu, AlerteCapteurTransition.Evaluer(alerte, valeur, T0.AddMinutes(1)));
    }

    [Theory]
    // Min = 10, marge = 2 : le retour exige une valeur au-dessus de 12
    [InlineData(10, null, 11, TypeTransitionAlerte.Aucune)]
    [InlineData(10, null, 12, TypeTransitionAlerte.Aucune)]
    [InlineData(10, null, 12.5, TypeTransitionAlerte.Fin)]
    // Max = 40, marge = 2 : le retour exige une valeur en dessous de 38
    [InlineData(null, 40, 39, TypeTransitionAlerte.Aucune)]
    [InlineData(null, 40, 38, TypeTransitionAlerte.Aucune)]
    [InlineData(null, 40, 37.5, TypeTransitionAlerte.Fin)]
    public void Marge_RetardeLaFinDeLAlerte(int? min, int? max, double valeur, TypeTransitionAlerte attendu)
    {
        var alerte = new AlerteCapteur { MinValue = min, MaxValue = max, Marge = 2, EstEnAlerte = true, DateChangementEtat = T0 };

        Assert.Equal(attendu, AlerteCapteurTransition.Evaluer(alerte, (decimal)valeur, T0.AddMinutes(1)));
    }

    [Fact]
    public void Marge_NeChangePasLeDebutDeLAlerte()
    {
        var alerte = new AlerteCapteur { MinValue = 10, Marge = 2 };

        Assert.Equal(TypeTransitionAlerte.Aucune, AlerteCapteurTransition.Evaluer(alerte, 11, T0));
        Assert.Equal(TypeTransitionAlerte.Debut, AlerteCapteurTransition.Evaluer(alerte, 10, T0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void DonneePlusAncienneQueLeDernierChangement_NeRenversePasLEtat(int minutesApresChangement)
    {
        var date = T0.AddMinutes(minutesApresChangement);

        var enCours = new AlerteCapteur { MinValue = 0, EstEnAlerte = true, DateChangementEtat = T0 };
        Assert.Equal(TypeTransitionAlerte.Aucune, AlerteCapteurTransition.Evaluer(enCours, 1, date));

        var normale = new AlerteCapteur { MinValue = 0, EstEnAlerte = false, DateChangementEtat = T0 };
        Assert.Equal(TypeTransitionAlerte.Aucune, AlerteCapteurTransition.Evaluer(normale, 0, date));
    }

    [Fact]
    public void DateDeLaDonnee_EstCompareeSansEgardAuFuseauHoraire()
    {
        var alerte = new AlerteCapteur { MinValue = 0, EstEnAlerte = true, DateChangementEtat = T0 };

        // 17 h 59 UTC précède 14 h 00 UTC-4 (18 h 00 UTC)
        var avant = new DateTimeOffset(2026, 10, 13, 17, 59, 0, TimeSpan.Zero);
        var apres = new DateTimeOffset(2026, 10, 13, 18, 1, 0, TimeSpan.Zero);

        Assert.Equal(TypeTransitionAlerte.Aucune, AlerteCapteurTransition.Evaluer(alerte, 1, avant));
        Assert.Equal(TypeTransitionAlerte.Fin, AlerteCapteurTransition.Evaluer(alerte, 1, apres));
    }
}

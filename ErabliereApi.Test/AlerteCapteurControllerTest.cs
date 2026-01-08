using ErabliereApi.Controllers;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Test.Autofixture;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Test;
public class AlerteCapteurControllerTest
{
    [Theory, AutoApiData]
    public async Task TestGetAlerteCapteur(
        AlerteCapteursController controller, ErabliereDbContext context) 
    {
        var erabliere = context.Erabliere.GetRandom();

        Assert.NotNull(erabliere.Id);
        var response = await controller.ListerAlerteCapteurErabliere(
            erabliere.Id.Value,
            include: "Capteur",
            System.Threading.CancellationToken.None
        );  

        Assert.All(response, e => Assert.IsType<AlerteCapteur>(e));
        var alerteCapteurs = Assert.IsAssignableFrom<AlerteCapteur[]>(response);

        var capteur = alerteCapteurs[0].Capteur;
        Assert.NotNull(capteur);
        Assert.NotNull(capteur.Symbole);
        Assert.NotEmpty(capteur.Symbole);
    }

    [Theory, AutoApiData]
    public async Task TestPutAlerteCapteur(
        AlerteCapteursController controller, ErabliereDbContext context)
    {
        var alerteCapteur = context.AlerteCapteurs.GetRandom();

        Assert.NotNull(alerteCapteur.IdCapteur);
        Assert.NotNull(alerteCapteur.EnvoyerA);
        Assert.NotNull(alerteCapteur.TexterA);
        Assert.NotNull(alerteCapteur.MaxValue);
        Assert.NotNull(alerteCapteur.MinValue);
        Assert.Single(alerteCapteur.EnvoyerA.Split(';'));
        Assert.Single(alerteCapteur.TexterA.Split(';'));

        var response = await controller.Modifier(
            alerteCapteur.IdCapteur.Value,
            new Donnees.Action.Put.PutAlerteCapteur
            {
                EnvoyerA = "test@test.com;test1@test.com",
                TexterA = "+10123456789;+11234567890",
                Id = alerteCapteur.Id,
                IdCapteur = alerteCapteur.IdCapteur,
                IsEnable = alerteCapteur.IsEnable,
                MaxValue = (short)(alerteCapteur.MaxValue * 10),
                MinValue = (short)(alerteCapteur.MinValue * 10),
                Nom = alerteCapteur.Nom
            },
            System.Threading.CancellationToken.None);

        var result = Assert.IsType<OkObjectResult>(response);

        var alerteCapteurResponse = Assert.IsType<AlerteCapteur>(result.Value);
    }
}

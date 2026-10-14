using ErabliereApi.Controllers;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Test.Autofixture;
using ErabliereApi.Donnees.Action.Delete;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
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

    [Fact]
    public async Task Supprimer_AlerteDUnAutreCapteur_RetourneNotFoundEtNeSupprimeRien()
    {
        var nomBD = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ErabliereDbContext>().UseInMemoryDatabase(nomBD).Options;
        var monCapteur = Guid.NewGuid();
        var capteurDuVoisin = Guid.NewGuid();
        var alerteDuVoisin = Guid.NewGuid();

        using (var depot = new ErabliereDbContext(options))
        {
            depot.Capteurs.Add(new Capteur { Id = monCapteur, IdErabliere = Guid.NewGuid(), Nom = "Secteur" });
            depot.Capteurs.Add(new Capteur { Id = capteurDuVoisin, IdErabliere = Guid.NewGuid(), Nom = "Secteur" });
            depot.AlerteCapteurs.Add(new AlerteCapteur { Id = alerteDuVoisin, IdCapteur = capteurDuVoisin, Nom = "Panne" });
            await depot.SaveChangesAsync();
        }

        // La route vise mon capteur (ValiderOwnership passe), le corps vise l'alerte du voisin
        var response = await new AlerteCapteursController(new ErabliereDbContext(options)).Supprimer(
            monCapteur,
            new DeleteAlerteCapteur { Id = alerteDuVoisin, IdCapteur = monCapteur },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(response);
        Assert.True(await new ErabliereDbContext(options).AlerteCapteurs.AnyAsync(a => a.Id == alerteDuVoisin));
    }

    [Fact]
    public async Task Supprimer_AlerteDuCapteur_LaSupprime()
    {
        var options = new DbContextOptionsBuilder<ErabliereDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var idCapteur = Guid.NewGuid();
        var idAlerte = Guid.NewGuid();

        using (var depot = new ErabliereDbContext(options))
        {
            depot.Capteurs.Add(new Capteur { Id = idCapteur, IdErabliere = Guid.NewGuid(), Nom = "Secteur" });
            depot.AlerteCapteurs.Add(new AlerteCapteur { Id = idAlerte, IdCapteur = idCapteur, Nom = "Panne" });
            await depot.SaveChangesAsync();
        }

        var response = await new AlerteCapteursController(new ErabliereDbContext(options)).Supprimer(
            idCapteur,
            new DeleteAlerteCapteur { Id = idAlerte, IdCapteur = idCapteur },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(response);
        Assert.False(await new ErabliereDbContext(options).AlerteCapteurs.AnyAsync());
    }
}

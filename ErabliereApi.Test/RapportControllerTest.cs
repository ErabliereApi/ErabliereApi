using ErabliereApi.Controllers;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Test.Autofixture;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Test;
public class RapportControllerTest
{
    [Theory, AutoApiData]
    public void TestGetRapport(
        RapportsController controller, ErabliereDbContext context)
    {
        var erabliere = context.Erabliere.GetRandom();

        var response = controller.GetSaveReports(
            erabliere.Id.Value
        );

        var okResponse = Assert.IsType<OkObjectResult>(response);
        var rapportQuery = Assert.IsAssignableFrom<IQueryable<Rapport>>(okResponse.Value);
        var rapports = rapportQuery.ToList();
        Assert.Empty(rapports);
    }

    [Theory, AutoApiData]
    public async Task CreateRapportSurCapteur(RapportsController controller, ErabliereDbContext context)
    {
        var erabliere = context.Erabliere.Include(e => e.Capteurs).Where(e => e.Capteurs.Count > 0 && e.Capteurs.Any(c => c.DonneesCapteur.Count > 0)).GetRandom();
        
        var response = await controller.RapportDegreeJour(
            erabliere.Id.Value,
            new Donnees.Action.Post.PostRapportDegreeJourRequest
            {
                AfficherDansDashboard = true,
                DateDebut = new DateTime(2023, 1, 1),
                DateFin = new DateTime(2023, 12, 31),
                IdCapteur = erabliere.Capteurs.First().Id,
                IdErabliere = erabliere.Id.Value,
                SeuilTemperature = 0,
                UtiliserTemperatureTrioDonnee = false,
            },
            sauvegarder: true,
            System.Threading.CancellationToken.None
        );
        
        var okResponse = Assert.IsType<OkObjectResult>(response);
        var rapport = Assert.IsAssignableFrom<Rapport>(okResponse.Value);
        Assert.True(rapport.Id != Guid.Empty, "Le rapport doit avoir un identifiant valide.");
        Assert.Equal(erabliere.Id.Value, rapport.IdErabliere);
        Assert.Equal(0, rapport.SeuilTemperature);
        Assert.Equal(new DateTime(2023, 1, 1), rapport.DateDebut);
        Assert.Equal(new DateTime(2023, 12, 31), rapport.DateFin);
        Assert.True(rapport.AfficherDansDashboard, "Le rapport doit être affiché dans le tableau de bord.");
    }

    [Theory, AutoApiData]
    public async Task CreateRapportSurTrioDonnees(RapportsController controller, ErabliereDbContext context)
    {
        var erabliere = context.Erabliere.Include(e => e.Capteurs).Where(e => e.Capteurs.Count > 0 && e.Capteurs.Any(c => c.DonneesCapteur.Count > 0)).GetRandom();

        var response = await controller.RapportDegreeJour(
            erabliere.Id.Value,
            new Donnees.Action.Post.PostRapportDegreeJourRequest
            {
                AfficherDansDashboard = false,
                DateDebut = new DateTime(2023, 1, 1),
                DateFin = new DateTime(2023, 12, 31),
                IdCapteur = null,
                IdErabliere = erabliere.Id.Value,
                SeuilTemperature = 0,
                UtiliserTemperatureTrioDonnee = true,
            },
            sauvegarder: true,
            System.Threading.CancellationToken.None
        );

        var okResponse = Assert.IsType<OkObjectResult>(response);
        var rapport = Assert.IsAssignableFrom<Rapport>(okResponse.Value);
        Assert.True(rapport.Id != Guid.Empty, "Le rapport doit avoir un identifiant valide.");
        Assert.Equal(erabliere.Id.Value, rapport.IdErabliere);
        Assert.Equal(0, rapport.SeuilTemperature);
        Assert.Equal(new DateTime(2023, 1, 1), rapport.DateDebut);
        Assert.Equal(new DateTime(2023, 12, 31), rapport.DateFin);
        Assert.False(rapport.AfficherDansDashboard, "Le rapport doit être affiché dans le tableau de bord.");
    }
}

using ErabliereApi.Controllers;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Delete;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Test;

/// <summary>
/// Les DELETE qui reçoivent un corps : ValiderOwnership ne protège que la ressource de la route.
/// L'id du corps ne doit jamais atteindre la ressource d'un autre client.
/// </summary>
public class SupprimerParCorpsTest
{
    private readonly DbContextOptions<ErabliereDbContext> _options =
        new DbContextOptionsBuilder<ErabliereDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private readonly Guid _monErabliere = Guid.NewGuid();
    private readonly Guid _erabliereDuVoisin = Guid.NewGuid();

    private ErabliereDbContext CreerDepot() => new(_options);

    private async Task Ajouter(params object[] entites)
    {
        using var depot = CreerDepot();
        depot.AddRange(entites);
        await depot.SaveChangesAsync();
    }

    [Fact]
    public async Task Baril_DUneAutreErabliere_RetourneNotFoundEtNeSupprimeRien()
    {
        var barilDuVoisin = Guid.NewGuid();
        await Ajouter(new Baril { Id = barilDuVoisin, IdErabliere = _erabliereDuVoisin });

        var response = await new BarilController(CreerDepot()).Supprimer(
            _monErabliere,
            new DeleteBaril { Id = barilDuVoisin, IdErabliere = _monErabliere },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(response);
        Assert.True(await CreerDepot().Barils.AnyAsync(b => b.Id == barilDuVoisin));
    }

    [Fact]
    public async Task Baril_DeLErabliere_EstSupprime()
    {
        var monBaril = Guid.NewGuid();
        await Ajouter(new Baril { Id = monBaril, IdErabliere = _monErabliere });

        var response = await new BarilController(CreerDepot()).Supprimer(
            _monErabliere,
            new DeleteBaril { Id = monBaril, IdErabliere = _monErabliere },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(response);
        Assert.False(await CreerDepot().Barils.AnyAsync());
    }

    [Fact]
    public async Task Dompeux_DUneAutreErabliere_RetourneNotFoundEtNeSupprimeRien()
    {
        var dompeuxDuVoisin = Guid.NewGuid();
        await Ajouter(new Dompeux { Id = dompeuxDuVoisin, IdErabliere = _erabliereDuVoisin });

        var response = await new DompeuxController(CreerDepot()).Supprimer(
            _monErabliere,
            dompeuxDuVoisin,
            new DeleteDompeux { Id = dompeuxDuVoisin, IdErabliere = _monErabliere });

        Assert.IsType<NotFoundResult>(response);
        Assert.True(await CreerDepot().Dompeux.AnyAsync(d => d.Id == dompeuxDuVoisin));
    }

    [Fact]
    public async Task Dompeux_DeLErabliere_EstSupprime()
    {
        var monDompeux = Guid.NewGuid();
        await Ajouter(new Dompeux { Id = monDompeux, IdErabliere = _monErabliere });

        var response = await new DompeuxController(CreerDepot()).Supprimer(
            _monErabliere,
            monDompeux,
            new DeleteDompeux { Id = monDompeux, IdErabliere = _monErabliere });

        Assert.IsType<NoContentResult>(response);
        Assert.False(await CreerDepot().Dompeux.AnyAsync());
    }

    [Fact]
    public async Task DonneeCapteur_DUnAutreCapteur_RetourneNotFoundEtNeSupprimeRien()
    {
        var monCapteur = Guid.NewGuid();
        var capteurDuVoisin = Guid.NewGuid();
        var donneeDuVoisin = Guid.NewGuid();
        await Ajouter(
            new Capteur { Id = monCapteur, IdErabliere = _monErabliere, Nom = "Secteur" },
            new Capteur { Id = capteurDuVoisin, IdErabliere = _erabliereDuVoisin, Nom = "Secteur" },
            new DonneeCapteur { Id = donneeDuVoisin, IdCapteur = capteurDuVoisin, Valeur = 1, D = DateTimeOffset.Now });

        var response = await new DonneesCapteurController(CreerDepot()).Supprimer(
            monCapteur,
            new DeleteDonneeCapteur { Id = donneeDuVoisin, IdCapteur = monCapteur },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(response);
        Assert.True(await CreerDepot().DonneesCapteur.AnyAsync(d => d.Id == donneeDuVoisin));
    }

    [Fact]
    public async Task DonneeCapteur_DuCapteur_EstSupprimee()
    {
        var monCapteur = Guid.NewGuid();
        var maDonnee = Guid.NewGuid();
        await Ajouter(
            new Capteur { Id = monCapteur, IdErabliere = _monErabliere, Nom = "Secteur" },
            new DonneeCapteur { Id = maDonnee, IdCapteur = monCapteur, Valeur = 1, D = DateTimeOffset.Now });

        var response = await new DonneesCapteurController(CreerDepot()).Supprimer(
            monCapteur,
            new DeleteDonneeCapteur { Id = maDonnee, IdCapteur = monCapteur },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(response);
        Assert.False(await CreerDepot().DonneesCapteur.AnyAsync());
    }
}

using ErabliereApi.Controllers;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Test.Autofixture;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Test;
public class DonnesCapteurV2Test
{
    [Theory, AutoApiData]
    public async Task GetDonnesCapteurs(
        DonneesCapteurV2Controller controller, ErabliereDbContext context)
    {
#nullable disable
        var erabliere = context.Erabliere.Include(e => e.Capteurs).ThenInclude(c => c.DonneesCapteur).Where(e => e.Capteurs.Count > 0 && e.Capteurs.Any(c => c.DonneesCapteur.Count > 0)).GetRandom();
        var capteur = erabliere.Capteurs.First(c => c.DonneesCapteur.Count > 0);
#nullable enable

        Assert.NotNull(capteur.Id);
        var response = await controller.Lister(
            capteur.Id.Value,
            ddr: null,
            dd: new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero),
            df: new DateTimeOffset(2023, 12, 31, 23, 59, 59, TimeSpan.Zero),
            order: null,
            top: 10,
            System.Threading.CancellationToken.None
        );

        var responseEnumerable = Assert.IsAssignableFrom<IEnumerable<GetDonneesCapteurV2>>(response);
        var donnesCapteur = responseEnumerable.ToList();
        Assert.NotNull(donnesCapteur);
    }
}

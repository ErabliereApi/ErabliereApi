using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoMapper;
using ErabliereApi.Depot.Sql;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Donnees;
using Microsoft.EntityFrameworkCore;
using ErabliereApi.Donnees.AutoMapper;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using ErabliereApi.Controllers;

namespace ErabliereApi.Test.Autofixture;
public static class ErabliereFixture
{
    public static readonly Random random = new();

    /// <summary>
    /// Create and instance of <see cref="IFixture"/> with ErabliereAPI configuration.
    /// </summary>
    /// <param name="modelOnly">If set to false, DBContext will be register and populate with fixture data</param>
    /// <returns></returns>
    public static IFixture CreerFixture(bool modelOnly = true)
    {
        var fixture = new Fixture();

        fixture.Customize<ActionDescriptor>(c => c.OmitAutoProperties());
        fixture.Customize<ControllerContext>(c => {
            return c.OmitAutoProperties().With(c => c.HttpContext, new DefaultHttpContext());
        });
        fixture.Customize(new AutoNSubstituteCustomization());

        fixture.Customize<PostErabliere>(c => c.With(e => e.IpRules, () => fixture.CreateRandomIPAddress().ToString())
                                               .With(e => e.IsPublic, () => true));
        
        fixture.Customize<Erabliere>(c => c.With(e => e.IpRule, () => fixture.CreateRandomIPAddress().ToString())
                                           .Without(e => e.Donnees)
                                           .Without(e => e.Dompeux)
                                           .Without(e => e.Barils)
                                           .Without(e => e.Documentations)
                                           .Without(e => e.Notes)
                                           .Without(e => e.Alertes)
                                           .Without(e => e.Rapports)
                                           .Without(e => e.Capteurs)
                                           .Without(e => e.Inspections)
                                           .Without(e => e.CustomerErablieres));

        fixture.Customize<Donnee>(c => c.With(d => d.D, RandomDate(from: new DateTime(2023, 1, 1), to: new DateTime(2023, 12, 31)))
                                        .Without(d => d.Erabliere)
                                        .Without(d => d.IdErabliere));

        fixture.Customize<Capteur>(c => c.Without(cc => cc.IdErabliere)
                                         .Without(cc => cc.Erabliere)
                                         .Without(cc => cc.Appareil)
                                         .Without(cc => cc.CapteurStyle));

        fixture.Customize<DonneeCapteur>(c => c.With(cc => cc.D, RandomDate(from: new DateTime(2023, 1, 1), to: new DateTime(2023, 12, 31)))
                                               .Without(cc => cc.Capteur)             
                                               .Without(cc => cc.IdCapteur));

        fixture.Customize<CapteurImage>(ci => ci.Without(cc => cc.Erabliere));

        fixture.Customize<DonneeCapteur>(c => c.Without(cc => cc.Capteur)
                                               .Without(cc => cc.Owner));

        fixture.Customize<AlerteCapteur>(c => c.Without(cc => cc.Capteur)
                                               .Without(cc => cc.Owner)
                                               .Without(cc => cc.Id));

        fixture.Customize<Customer>(c =>
            c.With(c => c.Email, RandomEmail)
             .Without(c => c.ApiKeys)
             .Without(c => c.CustomerErablieres));
        
        if (!modelOnly)
        {
            var builder = GetServicesProvider();

            fixture.Register(() => builder.GetRequiredService<ErabliereDbContext>().PopulatesDbSets(fixture));
            fixture.Register(() => builder.GetRequiredService<IDistributedCache>());
            fixture.Register(() => builder.GetRequiredService<IMapper>());
            fixture.Register(() => builder.GetRequiredService<IStringLocalizer<ErablieresController>>());
            fixture.Register(() => fixture.CreateRandomIPAddress());

            fixture.Freeze<ErabliereDbContext>();
            var httpContext = fixture.Freeze<HttpContext>();

            httpContext.RequestServices = builder;
        }

        return fixture;
    }

    private static DateTimeOffset? RandomDate(DateTime from, DateTime to)
    {
        if (from > to)
            throw new ArgumentException("La date 'from' doit être antérieure ou égale à la date 'to'.");

        var range = (to - from).Days;
        if (range < 0)
            return null;

        var randomDays = random.Next(0, range + 1);
        var randomDate = from.AddDays(randomDays);

        // Pour conserver la cohérence avec DateTimeOffset, on utilise l'heure locale
        return new DateTimeOffset(randomDate, TimeZoneInfo.Local.GetUtcOffset(randomDate));
    }

    private static string RandomEmail()
    {
        var domain = random.Next(0, 2) == 0 ? "gmail.com" : "hotmail.com";
        var name = Guid.NewGuid().ToString();
        var email = $"{name}@{domain}";
        return email;
    }

    private static IServiceProvider GetServicesProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddLocalization();

        services.AddDbContext<ErabliereDbContext>(options =>
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString());
        });

        services.AjouterAutoMapperErabliereApiDonnee();

        services.AddDistributedMemoryCache();

        return services.BuildServiceProvider();
    }
}

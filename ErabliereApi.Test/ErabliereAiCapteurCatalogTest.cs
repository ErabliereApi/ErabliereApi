using ErabliereAPI.Proxy;
using ErabliereApi.Services.AI.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Test;

/// <summary>
/// Behaviour of the pre-read that puts the real sensor names in the system prompt.
/// </summary>
public class ErabliereAiCapteurCatalogTest
{
    private static readonly Guid IdErabliere = Guid.NewGuid();

    [Fact]
    public async Task ReadAsync_RetourneLesNomsEtLesIdentifiantsDesCapteurs()
    {
        var idBrix = Guid.NewGuid();
        var proxy = CreateProxy(
            new Capteur { Id = idBrix, IdErabliere = IdErabliere, Nom = "Brix", Symbole = "°Bx", Online = true });

        var capteurs = await CreateCatalog(proxy).ReadAsync(IdErabliere, CancellationToken.None);

        var capteur = Assert.Single(capteurs);
        Assert.Equal(idBrix, capteur.Id);
        Assert.Equal("Brix", capteur.Nom);
        Assert.Equal("°Bx", capteur.Symbole);
        Assert.True(capteur.Online);
    }

    [Fact]
    public async Task ReadAsync_NeFiltreJamaisParNom()
    {
        // Tout l'intérêt de cette lecture est de donner la liste complète au modèle :
        // un filtre ici ramènerait exactement le trou qu'elle est censée boucher.
        var proxy = CreateProxy(new Capteur { Id = Guid.NewGuid(), Nom = "Brix" });

        await CreateCatalog(proxy).ReadAsync(IdErabliere, CancellationToken.None);

        await proxy.Received(1).CapteursAllAsync(
            Arg.Is(IdErabliere),
            Arg.Is<string?>(filtreNom => filtreNom == null),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<int?>(),
            Arg.Any<int?>(),
            Arg.Any<bool?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReadAsync_CapteurSansNomOuSansIdentifiant_EstIgnore()
    {
        // Le modèle ne peut ni reconnaître l'un ni lire l'autre; les nommer dans la
        // phrase système ne ferait que dépenser du contexte.
        var proxy = CreateProxy(
            new Capteur { Id = Guid.NewGuid(), Nom = "  " },
            new Capteur { Id = null, Nom = "Vacuum" },
            new Capteur { Id = Guid.NewGuid(), Nom = "Brix" });

        var capteurs = await CreateCatalog(proxy).ReadAsync(IdErabliere, CancellationToken.None);

        Assert.Equal("Brix", Assert.Single(capteurs).Nom);
    }

    [Fact]
    public async Task ReadAsync_ErabliereVide_NAppellePasLApi()
    {
        var proxy = CreateProxy();

        var capteurs = await CreateCatalog(proxy).ReadAsync(Guid.Empty, CancellationToken.None);

        Assert.Empty(capteurs);

        await proxy.DidNotReceiveWithAnyArgs().CapteursAllAsync(
            Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReadAsync_LectureRefusee_RetourneUneListeVideSansJeter()
    {
        // Une érablière que l'appelant ne possède pas atterrit ici, et c'est le bon
        // résultat : la phrase système ne nomme aucun capteur, le modèle listera
        // lui-même, et la question n'échoue pas pour autant.
        var proxy = Substitute.For<IErabliereAPIProxy>();

        proxy.CapteursAllAsync(
                 Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                 Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                 Arg.Any<CancellationToken>())
             .Returns<ICollection<Capteur>>(_ => throw new ApiException("Interdit.", 403, null, null, null));

        var capteurs = await CreateCatalog(proxy).ReadAsync(IdErabliere, CancellationToken.None);

        Assert.Empty(capteurs);
    }

    [Fact]
    public async Task ReadAsync_AnnulationDeLaRequete_RemonteLAnnulation()
    {
        // Personne n'attend plus la réponse : avaler l'annulation ferait continuer le
        // prompt pour rien.
        var proxy = Substitute.For<IErabliereAPIProxy>();

        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        proxy.CapteursAllAsync(
                 Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                 Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                 Arg.Any<CancellationToken>())
             .Returns<ICollection<Capteur>>(_ => throw new OperationCanceledException());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateCatalog(proxy).ReadAsync(IdErabliere, cancelled.Token));
    }

    private static ErabliereAiCapteurCatalog CreateCatalog(IErabliereAPIProxy proxy)
    {
        return new ErabliereAiCapteurCatalog(
            proxy,
            Options.Create(new ErabliereAiToolOptions()),
            NullLogger<ErabliereAiCapteurCatalog>.Instance);
    }

    private static IErabliereAPIProxy CreateProxy(params Capteur[] capteurs)
    {
        var proxy = Substitute.For<IErabliereAPIProxy>();

        proxy.CapteursAllAsync(
                 Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                 Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                 Arg.Any<CancellationToken>())
             .Returns(capteurs.ToList());

        return proxy;
    }
}

using AutoFixture;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Integration.Test.ApplicationFactory;
using ErabliereApi.Test.Autofixture;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Integration.Test;

public class ChirpStackControllerTest : IClassFixture<ErabliereApiApplicationFactory<Startup>>
{
    private readonly ErabliereApiApplicationFactory<Startup> _factory;
    private readonly IFixture _fixture;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ChirpStackControllerTest(ErabliereApiApplicationFactory<Startup> factory)
    {
        _factory = factory;
        _fixture = ErabliereFixture.CreerFixture();
    }

    [Fact]
    public async Task SimpleCall_RandomInformtion_BadRequest()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true,
            MaxAutomaticRedirections = 7
        });

        var content = new StringContent(Constants.ChirpStackExBadRequest, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/chirpstack/events?event=up", content);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SimpleCall_ServerExist_BadRequestOnErabliereId()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true,
            MaxAutomaticRedirections = 7
        });

        var payload = JsonSerializer.Deserialize<PostChirpstackEvent>(Constants.ChirpStackExNoErabliereId, _serializerOptions);
        Assert.NotNull(payload);
        var configC = new StringContent(JsonSerializer.Serialize(payload.deviceInfo), Encoding.UTF8, "application/json");
        var responseC = await client.PostAsync("/chirpstack/configs", configC);
        responseC.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);

        var content = new StringContent(Constants.ChirpStackExNoErabliereId, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/chirpstack/events?event=up", content);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SimpleCall_ServerAndErabliereExistButNoCapteur_BadRequest()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true,
            MaxAutomaticRedirections = 7
        });

        var cr = await client.PostAsJsonAsync("/erablieres", _fixture.Create<PostErabliere>());
        Assert.Equal(System.Net.HttpStatusCode.OK, cr.StatusCode);
        var erablieres = await client.GetFromJsonAsync<Erabliere[]>("/erablieres?$top=1&?$select=id");
        Assert.NotNull(erablieres);
        var erabliere = Assert.Single(erablieres);
        Assert.NotNull(erabliere.Id);
        var payloadStr = Constants.ChirpStackExOk.Replace("<replace-guid-erabliere>", erabliere.Id.ToString());
        Assert.Contains(erabliere.Id.Value.ToString(), payloadStr);
        var payload = JsonSerializer.Deserialize<PostChirpstackEvent>(payloadStr, _serializerOptions);
        Assert.NotNull(payload);
        var configC = new StringContent(JsonSerializer.Serialize(payload.deviceInfo), Encoding.UTF8, "application/json");
        var responseC = await client.PostAsync("/chirpstack/configs", configC);
        responseC.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);

        var content = new StringContent(payloadStr, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/chirpstack/events?event=up", content);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SimpleCall_ServerErabliereAndCapteurExist_Ok()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true,
            MaxAutomaticRedirections = 7
        });

        var cr = await client.PostAsJsonAsync("/erablieres", _fixture.Create<PostErabliere>());
        Assert.Equal(System.Net.HttpStatusCode.OK, cr.StatusCode);
        var erablieres = await client.GetFromJsonAsync<Erabliere[]>("/erablieres?$top=1&?$select=id");
        Assert.NotNull(erablieres);
        var erabliere = Assert.Single(erablieres);
        var idErabliere = Assert.NotNull(erabliere.Id);
        var payloadStr = Constants.ChirpStackExOk.Replace("<replace-guid-erabliere>", idErabliere.ToString());
        Assert.Contains(erabliere.Id.Value.ToString(), payloadStr);
        var payload = JsonSerializer.Deserialize<PostChirpstackEvent>(payloadStr, _serializerOptions);
        Assert.NotNull(payload);
        var capt1 = await client.PostAsJsonAsync($"/erablieres/{idErabliere}/capteurs", new PostCapteur
        {
            Nom = "Temperature du sol arrière",
            Symbole = "C",
            ExternalId = payload.deviceInfo.devEui,
            IdErabliere = idErabliere,
        });
        var capt2 = await client.PostAsJsonAsync($"/erablieres/{idErabliere}/capteurs", new PostCapteur
        {
            Nom = "Humidité du sol arrière",
            Symbole = "%",
            ExternalId = payload.deviceInfo.devEui,
            IdErabliere = idErabliere,
        });
        var configC = new StringContent(JsonSerializer.Serialize(payload.deviceInfo), Encoding.UTF8, "application/json");
        var responseC = await client.PostAsync("/chirpstack/configs", configC);
        responseC.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);

        var content = new StringContent(payloadStr, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/chirpstack/events?event=up", content);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
    }
}
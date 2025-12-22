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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Integration.Test;

public class TriggerAlerteTest : IClassFixture<ErabliereApiApplicationFactory<Startup>>
{
    private readonly ErabliereApiApplicationFactory<Startup> _factory;
    private readonly IFixture _fixture;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TriggerAlerteTest(ErabliereApiApplicationFactory<Startup> factory)
    {
        _factory = factory;
        _fixture = ErabliereFixture.CreerFixture();
    }

    [Fact]
    public async Task TestNoAlerte()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true,
            MaxAutomaticRedirections = 7
        });

        var id = await CreateErabiere(client);

        await SendTrioDonneeData(client, id);
    }

    private async Task SendTrioDonneeData(HttpClient client, Guid erabliereId)
    {
        var postData = _fixture.Create<Donnee>();
        postData.IdErabliere = erabliereId;
        var postResponse = await client.PostAsJsonAsync($"/Erablieres/{erabliereId}/Donnees", postData);
        Assert.NotNull(postResponse);
        var message = await postResponse.Content.ReadAsStringAsync();
        postResponse.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK, message);
    }

    private async Task<Guid> CreateErabiere(HttpClient client)
    {
        var initialResponse = await client.GetFromJsonAsync<Erabliere[]>("/Erablieres");

        Assert.NotNull(initialResponse);

        var newErabliere = _fixture.Create<PostErabliere>();

        newErabliere.AfficherTrioDonnees = true;
        newErabliere.IpRules = "-";

        var postResponse = await client.PostAsJsonAsync("/Erablieres", newErabliere);

        Assert.NotNull(postResponse);

        var newErabliereResponse = await postResponse.Content.ReadFromJsonAsync<Erabliere>(_serializerOptions);

        Assert.NotNull(newErabliereResponse);
        Assert.NotNull(newErabliere.Id);

        return newErabliere.Id.Value;
    }
}

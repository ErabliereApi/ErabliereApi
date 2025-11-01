using AutoFixture;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Integration.Test.ApplicationFactory;
using ErabliereApi.Test.Autofixture;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Integration.Test;

public class NMapControllerTest : IClassFixture<ErabliereApiApplicationFactory<Startup>>
{
    private readonly ErabliereApiApplicationFactory<Startup> _factory;
    private readonly IFixture _fixture = ErabliereFixture.CreerFixture();

    public NMapControllerTest(ErabliereApiApplicationFactory<Startup> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EmptyBody_BadRequest()
    {
        var client = _factory.CreateClient();
        var httpContent = new StringContent("", Encoding.UTF8, "application/xml");
        
        var response = await client.PutAsync($"Erablieres/{Guid.NewGuid()}/Appareil/nmapscan", httpContent);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Basic()
    {
        var client = _factory.CreateClient();
        var db = _factory.Services.GetRequiredService<ErabliereDbContext>();

        var erabliere = _fixture.Create<PostErabliere>();
        var postResponse = await client.PostAsJsonAsync("/Erablieres", erabliere);
        if (postResponse.IsSuccessStatusCode == false)
        {
            var error = await postResponse.Content.ReadAsStringAsync();
            throw new Exception($"Erreur lors de la création de l'érablière: {error}");
        }
        erabliere = await postResponse.Content.ReadFromJsonAsync<PostErabliere>() ?? throw new System.InvalidOperationException("Deserializing erabliere result in null reference.");

        var xml = GetXml("Basic");
        var httpContent = new StringContent(xml, Encoding.UTF8, "application/xml");
        var response = await client.PutAsync($"Erablieres/{erabliere.Id}/Appareil/nmapscan", httpContent);
        
        if (response.IsSuccessStatusCode == false)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new System.Exception($"Erreur lors de la mise à jour des appareils par scan nmap: {error}");
        }
        db.Appareils.Where(a => a.IdErabliere == erabliere.Id).ToList().Count.ShouldBe(2);
    }

    private static string GetXml(string step)
    {
        return File.ReadAllText(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "NmapXml",
            step + ".xml"));
    }
}

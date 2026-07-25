using System.Text.Json;
using System.Text.Json.Nodes;
using ErabliereAPI.Proxy;
using ErabliereApi.Mcp.Models;
using Shouldly;

namespace ErabliereApi.Mcp.Test;

/// <summary>
/// The MCP server serializes the value returned by a tool and sends it to the
/// model, so the shape of that JSON is part of the tool contract.
/// </summary>
public class SummarySerializationTest
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ErabliereSummary_SerializesWithTheApiPropertyNames()
    {
        var id = Guid.NewGuid();

        var summary = ErabliereSummary.From(new Erabliere
        {
            Id = id,
            Nom = "Sucrerie du Nord",
            Description = "Une érablière de test",
            Addresse = "1 rue des Érables",
            CodePostal = "G0A 1A0",
            RegionAdministrative = "Capitale-Nationale",
            IsPublic = true,
            IndiceOrdre = 3
        });

        var json = JsonNode.Parse(JsonSerializer.Serialize(summary, Options))!.AsObject();

        json["id"]!.GetValue<Guid>().ShouldBe(id);
        json["nom"]!.GetValue<string>().ShouldBe("Sucrerie du Nord");
        json["codePostal"]!.GetValue<string>().ShouldBe("G0A 1A0");
        json["regionAdministrative"]!.GetValue<string>().ShouldBe("Capitale-Nationale");
        json["isPublic"]!.GetValue<bool>().ShouldBeTrue();
        json["indiceOrdre"]!.GetValue<int>().ShouldBe(3);
    }

    [Fact]
    public void ErabliereSummary_DoesNotLeakTheNavigationProperties()
    {
        // The proxy DTO carries a dozen collections that would blow up the size
        // of the tool result. None of them must reach the model.
        var summary = ErabliereSummary.From(new Erabliere
        {
            Id = Guid.NewGuid(),
            Nom = "Sucrerie du Nord",
            Capteurs = [new Capteur { Id = Guid.NewGuid(), Nom = "Capteur" }],
            Alertes = [new Alerte { Id = Guid.NewGuid(), Nom = "Alerte" }],
            Notes = [new Note { Id = Guid.NewGuid(), Title = "Note" }]
        });

        var json = JsonNode.Parse(JsonSerializer.Serialize(summary, Options))!.AsObject();

        json.ContainsKey("capteurs").ShouldBeFalse();
        json.ContainsKey("alertes").ShouldBeFalse();
        json.ContainsKey("notes").ShouldBeFalse();
        json.Count.ShouldBe(8);
    }

    [Fact]
    public void AlerteSummary_SerializesWithTheApiPropertyNames()
    {
        var idErabliere = Guid.NewGuid();
        var lastOccurence = new DateTimeOffset(2026, 3, 12, 6, 30, 0, TimeSpan.FromHours(-4));

        var summary = AlerteSummary.From(new Alerte
        {
            Id = Guid.NewGuid(),
            IdErabliere = idErabliere,
            Nom = "Température trop basse",
            IsEnable = true,
            EnvoyerA = "producteur@example.com",
            TemperatureThresholdLow = "-5",
            NiveauBassinThresholdHight = "90",
            LastOccurence = lastOccurence
        });

        var json = JsonNode.Parse(JsonSerializer.Serialize(summary, Options))!.AsObject();

        json["idErabliere"]!.GetValue<Guid>().ShouldBe(idErabliere);
        json["nom"]!.GetValue<string>().ShouldBe("Température trop basse");
        json["isEnable"]!.GetValue<bool>().ShouldBeTrue();
        json["envoyerA"]!.GetValue<string>().ShouldBe("producteur@example.com");
        json["temperatureThresholdLow"]!.GetValue<string>().ShouldBe("-5");
        json["niveauBassinThresholdHight"]!.GetValue<string>().ShouldBe("90");
        json["lastOccurence"]!.GetValue<DateTimeOffset>().ShouldBe(lastOccurence);
    }

    [Fact]
    public void AlerteSummary_DoesNotLeakTheErabliereNavigationProperty()
    {
        var summary = AlerteSummary.From(new Alerte
        {
            Id = Guid.NewGuid(),
            IdErabliere = Guid.NewGuid(),
            Erabliere = new Erabliere { Id = Guid.NewGuid(), Nom = "Sucrerie du Nord" }
        });

        var json = JsonNode.Parse(JsonSerializer.Serialize(summary, Options))!.AsObject();

        json.ContainsKey("erabliere").ShouldBeFalse();
    }

    [Fact]
    public void Summaries_KeepTheNullValues_SoTheModelSeesTheAbsentFields()
    {
        var summary = ErabliereSummary.From(new Erabliere { Id = Guid.NewGuid(), Nom = "Sucrerie du Nord" });

        var json = JsonNode.Parse(JsonSerializer.Serialize(summary, Options))!.AsObject();

        json.ContainsKey("description").ShouldBeTrue();
        json["description"].ShouldBeNull();
    }
}

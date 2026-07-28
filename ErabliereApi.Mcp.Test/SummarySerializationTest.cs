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
    public void AlerteCapteurSummary_FlattensTheSensorAndDoesNotLeakIt()
    {
        // The sensor is asked for with include=Capteur, but only its name and its
        // unit are meaningful here: the rest of the DTO carries the readings.
        var idCapteur = Guid.NewGuid();

        var summary = AlerteCapteurSummary.From(new AlerteCapteur
        {
            Id = Guid.NewGuid(),
            IdCapteur = idCapteur,
            Nom = "Gel",
            IsEnable = true,
            MinValue = -2.5,
            MaxValue = 24,
            Capteur = new Capteur
            {
                Id = idCapteur,
                Nom = "Température extérieure",
                Symbole = "°C",
                DonneesCapteur = [new DonneeCapteur()]
            }
        });

        var json = JsonNode.Parse(JsonSerializer.Serialize(summary, Options))!.AsObject();

        json.ContainsKey("capteur").ShouldBeFalse();
        json["idCapteur"]!.GetValue<Guid>().ShouldBe(idCapteur);
        json["capteurNom"]!.GetValue<string>().ShouldBe("Température extérieure");
        json["capteurSymbole"]!.GetValue<string>().ShouldBe("°C");
        json["nom"]!.GetValue<string>().ShouldBe("Gel");
        json["isEnable"]!.GetValue<bool>().ShouldBeTrue();
        json["minValue"]!.GetValue<double>().ShouldBe(-2.5);
        json["maxValue"]!.GetValue<double>().ShouldBe(24);
    }

    [Fact]
    public void NoteSummary_NeverLeaksTheAttachedFile()
    {
        // A single photograph serialized in base 64 is worth more tokens than a
        // whole response is allowed to be, so the bytes must not reach the model
        // even though the proxy DTO carries them.
        var summary = NoteSummary.From(new Note
        {
            Id = Guid.NewGuid(),
            Title = "Entaille bouchée",
            Text = "Remplacée ce matin.",
            FileName = "photo.jpg",
            FileExtension = ".jpg",
            FileSize = 2_400_000,
            File = new byte[4096]
        });

        var json = JsonNode.Parse(JsonSerializer.Serialize(summary, Options))!.AsObject();

        json.ContainsKey("file").ShouldBeFalse();
        json["fileName"]!.GetValue<string>().ShouldBe("photo.jpg");
        json["fileSize"]!.GetValue<int>().ShouldBe(2_400_000);
    }

    [Fact]
    public void NoteSummary_CutsALongBodyAndSaysSo()
    {
        var summary = NoteSummary.From(new Note { Id = Guid.NewGuid(), Text = new string('a', 2000) });

        summary.Text!.Length.ShouldBe(NoteSummary.MaxTextLength + "… (truncated)".Length);
        summary.Text.ShouldEndWith("… (truncated)");
    }

    [Fact]
    public void NoteSummary_WhenTheBodyIsShort_LeavesItUntouched()
    {
        var summary = NoteSummary.From(new Note { Id = Guid.NewGuid(), Text = "Coulée forte." });

        summary.Text.ShouldBe("Coulée forte.");
    }

    [Fact]
    public void RapportSummary_OnAListing_LeavesOutTheRows()
    {
        var rapport = new Rapport
        {
            Id = Guid.NewGuid(),
            Nom = "Degré-jour 2026",
            Donnees = [new RapportDonnees { Date = DateTimeOffset.Now, Somme = 12 }]
        };

        RapportSummary.From(rapport, includeDonnees: false).Donnees.ShouldBeNull();
        RapportSummary.From(rapport, includeDonnees: true).Donnees!.Count.ShouldBe(1);
    }

    [Fact]
    public void RapportSummary_DoesNotLeakTheCapteurNavigationProperty()
    {
        var idCapteur = Guid.NewGuid();

        var summary = RapportSummary.From(new Rapport
        {
            Id = Guid.NewGuid(),
            Capteur = new Capteur { Id = idCapteur, Nom = "Température extérieure", DonneesCapteur = [new DonneeCapteur()] }
        }, includeDonnees: false);

        var json = JsonNode.Parse(JsonSerializer.Serialize(summary, Options))!.AsObject();

        json.ContainsKey("capteur").ShouldBeFalse();
        json["idCapteur"]!.GetValue<Guid>().ShouldBe(idCapteur);
        json["capteurNom"]!.GetValue<string>().ShouldBe("Température extérieure");
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

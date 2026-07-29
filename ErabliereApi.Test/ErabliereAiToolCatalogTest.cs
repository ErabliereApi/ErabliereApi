using ErabliereApi.Services.AI.Tools;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Test;

/// <summary>
/// Ce que le modèle voit du serveur MCP quand il discute dans ErabliereAI.
///
/// La liste des outils exposés est un choix de sécurité autant que de produit :
/// un outil d'écriture qui se glisserait ici donnerait à une conversation le droit
/// de modifier une érablière. Le test le fait échouer à la compilation du CI plutôt
/// qu'en production.
/// </summary>
public class ErabliereAiToolCatalogTest
{
    private static ErabliereAiToolCatalog CreateCatalog(Action<ErabliereAiToolOptions>? configure = null)
    {
        var options = new ErabliereAiToolOptions();
        configure?.Invoke(options);

        return new ErabliereAiToolCatalog(Options.Create(options));
    }

    [Fact]
    public void LeCatalogueExposeLesOutilsDeConsultationAttendus()
    {
        var noms = CreateCatalog().ToolNames.OrderBy(n => n, StringComparer.Ordinal).ToArray();

        Assert.Equal([
            "get_alertes",
            "get_alertes_capteur",
            "get_barils",
            "get_dompeux",
            "get_donnees_capteur",
            "get_erabliere",
            "get_horaire",
            "get_notes",
            "get_rapport",
            "list_capteurs",
            "list_erablieres",
            "list_rapports"
        ], noms);
    }

    [Fact]
    public void GetMyPlanNEstPasOffertALaConversation()
    {
        // Il résout le forfait à partir d'une clé d'api, que l'appelant du chat n'a
        // généralement pas, et il répond sur le client MCP plutôt que sur l'érablière.
        Assert.DoesNotContain("get_my_plan", CreateCatalog().ToolNames);
    }

    [Fact]
    public void UnOutilExcluParConfigurationNEstPasExpose()
    {
        var catalogue = CreateCatalog(options => options.ExcludedTools = ["get_my_plan", "get_barils"]);

        Assert.DoesNotContain("get_barils", catalogue.ToolNames);
        Assert.Contains("list_capteurs", catalogue.ToolNames);
    }

    [Fact]
    public void ChaqueOutilExposePorteUneDescriptionEtUnSchemaObjet()
    {
        foreach (var tool in CreateCatalog().ChatTools)
        {
            Assert.False(string.IsNullOrWhiteSpace(tool.FunctionDescription),
                $"L'outil {tool.FunctionName} doit décrire au modèle quand s'en servir.");

            var schema = JsonDocument.Parse(tool.FunctionParameters.ToString()).RootElement;

            Assert.Equal(JsonValueKind.Object, schema.ValueKind);
            Assert.Equal("object", schema.GetProperty("type").GetString());

            // Les api de complétion valident les paramètres de fonction contre un
            // dialecte plus étroit et refusent ce mot clé à la racine.
            Assert.False(schema.TryGetProperty("$schema", out _),
                $"Le schéma de {tool.FunctionName} ne doit pas porter le mot clé $schema.");
        }
    }

    [Fact]
    public void LeSchemaNExposePasLesDependancesDuServeur()
    {
        // Un IErabliereAPIProxy dans le schéma, et le modèle croirait pouvoir choisir
        // avec quoi l'outil lit les données.
        var listeCapteurs = CreateCatalog().ChatTools.Single(t => t.FunctionName == "list_capteurs");

        var proprietes = JsonDocument.Parse(listeCapteurs.FunctionParameters.ToString())
                                     .RootElement.GetProperty("properties");

        Assert.False(proprietes.TryGetProperty("proxy", out _));
        Assert.False(proprietes.TryGetProperty("cancellationToken", out _));
        Assert.True(proprietes.TryGetProperty("erabliereId", out _));
    }

    [Fact]
    public void LeSchemaNExposePasLesDependancesMemeConstruitEnParallele()
    {
        // AIFunctionFactory mémorise le schéma d'une méthode dans un état partagé par
        // le processus, et cette mémorisation n'est pas sûre à froid : construits
        // concurremment, les premiers catalogues revenaient avec le 'proxy' que
        // BindServiceParameters devait cacher. Un verrou dans le constructeur ferme
        // la course; ce test est ce qui empêche de le retirer par mégarde.
        var fautifs = new ConcurrentBag<string>();

        Parallel.For(0, 64, _ =>
        {
            var proprietes = JsonDocument
                .Parse(CreateCatalog().ChatTools.Single(t => t.FunctionName == "list_capteurs").FunctionParameters.ToString())
                .RootElement.GetProperty("properties");

            if (proprietes.TryGetProperty("proxy", out JsonElement _))
            {
                fautifs.Add(proprietes.ToString());
            }
        });

        Assert.True(fautifs.IsEmpty, $"{fautifs.Count} catalogues sur 64 exposent 'proxy'. Exemple : {fautifs.FirstOrDefault()}");
    }

    [Fact]
    public void LeSchemaDeGetDonneesCapteurRendLesDatesObligatoires()
    {
        // C'est ce qui empêche le modèle de demander une saison entière de relevés.
        var outil = CreateCatalog().ChatTools.Single(t => t.FunctionName == "get_donnees_capteur");

        var requis = JsonDocument.Parse(outil.FunctionParameters.ToString())
                                 .RootElement.GetProperty("required")
                                 .EnumerateArray()
                                 .Select(e => e.GetString())
                                 .ToArray();

        Assert.Contains("startDate", requis);
        Assert.Contains("endDate", requis);
    }

    [Fact]
    public void UnNomInconnuNeTrouveAucuneFonction()
    {
        Assert.Null(CreateCatalog().Find("supprimer_erabliere"));
    }
}

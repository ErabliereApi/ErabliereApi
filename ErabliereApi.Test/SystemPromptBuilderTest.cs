using ErabliereApi.Services.AI;
using System;
using System.Globalization;
using Xunit;

namespace ErabliereApi.Test;

/// <summary>
/// Locks the system prompt now that it is built in a single place.
/// </summary>
public class SystemPromptBuilderTest
{
    private const string DefaultSystemPrompt = "Vous êtes un acériculteur expérimenté avec des connaissance scientifique et pratique.";

    private readonly ISystemPromptBuilder _builder = new SystemPromptBuilder();

    [Fact]
    public void DefaultSystemPrompt_EstLaPhraseDAcericulteur()
    {
        Assert.Equal(DefaultSystemPrompt, _builder.DefaultSystemPrompt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildForNewConversation_SansPhrase_RetourneLaPhraseParDefaut(string? requested)
    {
        Assert.Equal(DefaultSystemPrompt, _builder.BuildForNewConversation(requested));
    }

    [Fact]
    public void BuildForNewConversation_AvecPhrase_RetourneLaPhraseDemandee()
    {
        Assert.Equal("Vous êtes un botaniste.", _builder.BuildForNewConversation("Vous êtes un botaniste."));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildForCompletion_SansPhrase_RetourneLaPhraseParDefaut(string? conversationSystemMessage)
    {
        Assert.Equal(DefaultSystemPrompt, _builder.BuildForCompletion(conversationSystemMessage));
    }

    [Fact]
    public void BuildForCompletion_AvecPhrase_RetourneLaPhraseDeLaConversation()
    {
        Assert.Equal("Vous êtes un botaniste.", _builder.BuildForCompletion("Vous êtes un botaniste."));
    }

    [Fact]
    public void BuildForCompletion_SansOutil_NeParlePasDOutils()
    {
        var prompt = _builder.BuildForCompletion(null, new ErabliereAiPromptContext(false));

        Assert.Equal(DefaultSystemPrompt, prompt);
    }

    [Fact]
    public void BuildForCompletion_AvecOutils_ExpliqueQuandSEnServir()
    {
        var prompt = _builder.BuildForCompletion(null, new ErabliereAiPromptContext(true));

        Assert.StartsWith(DefaultSystemPrompt, prompt);
        Assert.Contains("lecture seule", prompt);
        Assert.Contains("n'inventez jamais de données", prompt);

        // La date du jour : sans elle, « hier » et « la semaine dernière » n'ont
        // aucun sens pour un modèle dont les poids sont figés.
        Assert.Contains(DateTimeOffset.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), prompt);
    }

    [Fact]
    public void BuildForCompletion_AvecErabliereCourante_NommeLIdentifiantAuModele()
    {
        var id = Guid.NewGuid();

        var prompt = _builder.BuildForCompletion(null, new ErabliereAiPromptContext(true, id, "Sucrerie du Nord"));

        Assert.Contains(id.ToString(), prompt);
        Assert.Contains("Sucrerie du Nord", prompt);
    }

    [Fact]
    public void BuildForCompletion_AvecErabliereSansNom_NommeQuandMemeLIdentifiant()
    {
        var id = Guid.NewGuid();

        var prompt = _builder.BuildForCompletion(null, new ErabliereAiPromptContext(true, id));

        Assert.Contains(id.ToString(), prompt);
    }

    [Fact]
    public void BuildForCompletion_SansErabliereCourante_NAjouteAucunIdentifiant()
    {
        var prompt = _builder.BuildForCompletion(null, new ErabliereAiPromptContext(true, Guid.Empty));

        Assert.DoesNotContain("identifiant est", prompt);
    }

    [Fact]
    public void BuildForCompletion_ContexteErabliereSansOutil_NommeQuandMemeLErabliere()
    {
        // Le forfait peut fermer les outils sans que la question « de quelle érablière
        // parle-t-on » disparaisse.
        var id = Guid.NewGuid();

        var prompt = _builder.BuildForCompletion(null, new ErabliereAiPromptContext(false, id, "Sucrerie du Nord"));

        Assert.Contains(id.ToString(), prompt);
        Assert.DoesNotContain("lecture seule", prompt);
    }
}

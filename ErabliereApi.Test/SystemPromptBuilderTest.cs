using ErabliereApi.Services.AI;
using System;
using System.Globalization;
using Xunit;

namespace ErabliereApi.Test;

/// <summary>
/// Locks what the system prompt has to say, not how it says it.
/// </summary>
/// <remarks>
/// The wording of a prompt is tuned against a real model and changes often; an
/// assertion on the exact sentence would turn every improvement into a failing
/// test for no gain. What is worth locking is the handful of instructions the
/// chat measurably misbehaves without.
/// </remarks>
public class SystemPromptBuilderTest
{
    private readonly ISystemPromptBuilder _builder = new SystemPromptBuilder();

    [Fact]
    public void DefaultSystemPrompt_DecritLAcericulteurEtSonInterlocuteur()
    {
        var prompt = _builder.DefaultSystemPrompt;

        Assert.Contains("acériculteur", prompt);
        Assert.Contains("ÉrablièreAI", prompt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildForNewConversation_SansPhrase_RetourneLaPhraseParDefaut(string? requested)
    {
        Assert.Equal(_builder.DefaultSystemPrompt, _builder.BuildForNewConversation(requested));
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
    public void BuildForCompletion_SansPhrase_PartDeLaPhraseParDefaut(string? conversationSystemMessage)
    {
        Assert.StartsWith(_builder.DefaultSystemPrompt, _builder.BuildForCompletion(conversationSystemMessage));
    }

    [Fact]
    public void BuildForCompletion_AvecPhrase_PartDeLaPhraseDeLaConversation()
    {
        Assert.StartsWith("Vous êtes un botaniste.", _builder.BuildForCompletion("Vous êtes un botaniste."));
    }

    [Fact]
    public void BuildForCompletion_ToujoursLesConsignesDeReponse()
    {
        // Les consignes de réponse ne sont pas persistées avec la personnalité : les
        // améliorer doit rejoindre les conversations déjà ouvertes.
        var prompt = _builder.BuildForCompletion("Vous êtes un botaniste.");

        Assert.Contains(SystemPromptBuilder.AnswerInstructions, prompt);
    }

    [Fact]
    public void BuildForCompletion_SansOutil_NeParlePasDOutils()
    {
        var prompt = _builder.BuildForCompletion(null, new ErabliereAiPromptContext(false));

        Assert.DoesNotContain("lecture seule", prompt);
        Assert.DoesNotContain("list_capteurs", prompt);
    }

    [Fact]
    public void BuildForCompletion_AvecOutils_ExpliqueQuandSEnServir()
    {
        var prompt = _builder.BuildForCompletion(null, new ErabliereAiPromptContext(true));

        Assert.StartsWith(_builder.DefaultSystemPrompt, prompt);
        Assert.Contains("lecture seule", prompt);
        Assert.Contains("N'inventez jamais un chiffre", prompt);

        // La date du jour : sans elle, « hier » et « la semaine dernière » n'ont
        // aucun sens pour un modèle dont les poids sont figés.
        Assert.Contains(DateTimeOffset.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), prompt);
    }

    [Fact]
    public void BuildForCompletion_AvecOutils_DitDeNePasFiltrerUneListeAvecLesMotsDeLaQuestion()
    {
        // La régression qui a motivé ce prompt : le modèle cherchait « degré Brix »
        // dans les noms de capteurs, ne trouvait rien, et concluait que le capteur
        // « Brix » n'existait pas.
        var prompt = _builder.BuildForCompletion(null, new ErabliereAiPromptContext(true));

        Assert.Contains("Ne filtrez pas une liste avec les mots de la question", prompt);
        Assert.Contains("Une liste vide après un filtre veut dire que le filtre était mauvais", prompt);
    }

    [Fact]
    public void BuildForCompletion_AvecOutils_DemandeDeGrouperLesAppels()
    {
        // Le nombre de tours est borné : un appel par tour épuise le budget avant que
        // la question soit répondue.
        var prompt = _builder.BuildForCompletion(null, new ErabliereAiPromptContext(true));

        Assert.Contains("Groupez vos appels", prompt);
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

    [Fact]
    public void BuildForCompletion_AvecCapteurs_LesNommeAvecLeurIdentifiant()
    {
        var idErabliere = Guid.NewGuid();
        var idBrix = Guid.NewGuid();
        var idVacuum = Guid.NewGuid();

        var prompt = _builder.BuildForCompletion(null, new ErabliereAiPromptContext(
            true,
            idErabliere,
            "Sucrerie du Nord",
            [
                new ErabliereAiCapteur(idBrix, "Brix", "°Bx", Online: true),
                new ErabliereAiCapteur(idVacuum, "Vac. principale", "Hg", Online: false)
            ]));

        // Le nom écrit par l'exploitant, pour que le modèle n'ait pas à le deviner.
        Assert.Contains("« Brix »", prompt);
        Assert.Contains("« Vac. principale »", prompt);

        // L'identifiant, pour qu'il puisse lire sans repasser par list_capteurs.
        Assert.Contains(idBrix.ToString(), prompt);
        Assert.Contains(idVacuum.ToString(), prompt);

        Assert.Contains("en °Bx", prompt);
        Assert.Contains("en ligne", prompt);
        Assert.Contains("hors ligne", prompt);
    }

    [Fact]
    public void BuildForCompletion_AvecCapteurs_DemandeDeReconnaitreLeCapteurSoiMeme()
    {
        var prompt = _builder.BuildForCompletion(null, new ErabliereAiPromptContext(
            true,
            Guid.NewGuid(),
            "Sucrerie du Nord",
            [new ErabliereAiCapteur(Guid.NewGuid(), "Brix")]));

        Assert.Contains("même si l'utilisateur le désigne avec d'autres mots que son nom", prompt);
    }

    [Fact]
    public void BuildForCompletion_SansCapteurLu_NAnnonceAucuneListe()
    {
        // La lecture préalable peut échouer; le modèle doit alors lister lui-même
        // plutôt que de croire à une liste vide.
        var prompt = _builder.BuildForCompletion(null, new ErabliereAiPromptContext(
            true,
            Guid.NewGuid(),
            "Sucrerie du Nord",
            []));

        Assert.DoesNotContain("Les capteurs de cette érablière", prompt);
    }
}

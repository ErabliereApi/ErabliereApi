using ErabliereApi.Services.AI;
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
}

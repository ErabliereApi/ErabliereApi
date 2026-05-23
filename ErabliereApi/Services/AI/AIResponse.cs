namespace ErabliereApi.Services.AI;

public class AIResponse
{
    public string? Text { get; set; }

    public string? Refusal { get; set; }

    public string? Kind { get; set; }

    public Uri? ImageUri { get; set; }
}

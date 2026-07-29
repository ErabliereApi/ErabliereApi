namespace ErabliereApi.Services.AI.Tools;

/// <summary>
/// How ErabliereAI is allowed to call the tools of the MCP tool set.
/// </summary>
/// <remarks>
/// Every bound in here exists because the caller of a tool loop is a language
/// model: it decides on its own how many calls to make and how much data to ask
/// for, so the process hosting it must be the one saying when to stop.
/// <para>
/// Section <c>ErabliereAI:Tools</c>, overridable with the usual environment
/// variables (<c>ErabliereAI__Tools__Enabled=false</c>).
/// </para>
/// </remarks>
public class ErabliereAiToolOptions
{
    /// <summary>
    /// Configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "ErabliereAI:Tools";

    /// <summary>
    /// Name of the named <see cref="HttpClient" /> the tools reach ErabliereAPI with.
    /// </summary>
    public const string HttpClientName = "ErabliereAIToolProxy";

    /// <summary>
    /// Master switch. Turning it off brings the chat back to the phase 7 behaviour:
    /// no tool definition is sent and no tool is ever executed.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of round trips model → tools → model for a single prompt.
    /// The round that hits the bound is answered without tools, so the user gets an
    /// answer built on what was gathered instead of an error.
    /// </summary>
    /// <remarks>
    /// A real question costs more rounds than it looks: naming the maple grove,
    /// listing its sensors, reading two of them and noticing one came back truncated
    /// is already five. A model that spends them one call at a time — which is what a
    /// small model does unless the prompt tells it to batch — runs out before it can
    /// answer, and the user reads an evasive answer rather than an error. The token
    /// budget below is the bound that actually protects the context; this one only
    /// stops a loop that goes nowhere.
    /// </remarks>
    public int MaxRounds { get; set; } = 8;

    /// <summary>
    /// Temperature used on the completions of a tool driven exchange, overriding
    /// <c>LLMDefaultTemperature</c>. Left null, the default temperature applies.
    /// </summary>
    /// <remarks>
    /// Reading data is not writing prose. At a high temperature the model invents
    /// plausible arguments — a search term the user never wrote, a date range nobody
    /// asked for — and the answer is then built on whatever those calls happened to
    /// return. The default of the platform is 1, which is a reasonable setting for a
    /// conversation and a poor one for a tool loop, so the loop lowers it for itself
    /// instead of forcing a single value on both.
    /// </remarks>
    public float? Temperature { get; set; } = 0.2f;

    /// <summary>
    /// Time a single tool call is given before it is abandoned. The loop carries on
    /// with an error result for that call rather than failing the whole prompt: one
    /// slow sensor query must not cost the user their answer.
    /// </summary>
    public TimeSpan ToolTimeout { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Ceiling on the estimated number of tokens all the tool results of one prompt
    /// may add up to. Once crossed, no further tool is called.
    /// </summary>
    public int TokenBudget { get; set; } = 12000;

    /// <summary>
    /// Tools of the MCP assembly that are deliberately not offered to the chat.
    /// </summary>
    /// <remarks>
    /// <c>get_my_plan</c> resolves the plan from an ErabliereAPI api key, which a
    /// chat caller authenticated with a bearer token does not have, and it answers a
    /// question about the MCP client rather than about the maple grove.
    /// </remarks>
    public string[] ExcludedTools { get; set; } = ["get_my_plan"];

    /// <summary>
    /// Base url the tools call ErabliereAPI back on. Left empty, the url of the
    /// request being served is used, which is what a single deployment wants.
    /// Set it when the API is reachable internally on another address than the one
    /// the browser used, for example behind a TLS terminating ingress.
    /// </summary>
    public string? ApiBaseUrl { get; set; }

    /// <summary>
    /// How long the tool activity of a prompt stays readable by the status endpoint
    /// after the prompt is done.
    /// </summary>
    public TimeSpan ActivityRetention { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether a tool is offered to the chat.
    /// </summary>
    public bool IsExposed(string toolName)
    {
        return !Array.Exists(ExcludedTools, excluded => string.Equals(excluded, toolName, StringComparison.OrdinalIgnoreCase));
    }
}

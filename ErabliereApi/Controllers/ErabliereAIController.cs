using Azure;
using Azure.AI.OpenAI;
using ErabliereApi.Attributes;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Get;
using ErabliereApi.Donnees.Action.Patch;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Extensions;
using ErabliereApi.Services.AI;
using ErabliereApi.Services.AI.Tools;
using ErabliereApi.Services.Users;
using ErabliereModel.Action.Post;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using OpenAI.Images;
using System.ClientModel;
using System.Text;
using System.Text.Json;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler représentant les données des dompeux
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(Roles = "ErabliereAIUser", Policy = "TenantIdPrincipal")]
[ValiderAbonnement(ForfaitsAbonnement.Base)]
public class ErabliereAIController : ControllerBase
{
    private readonly ErabliereDbContext _depot;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConversationAIService _conversationAIService;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    /// <param name="depot"></param>
    /// <param name="configuration"></param>
    /// <param name="httpClientFactory"></param>
    /// <param name="conversationAIService"></param>
    public ErabliereAIController(
        ErabliereDbContext depot,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IConversationAIService conversationAIService)
    {
        _depot = depot;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _conversationAIService = conversationAIService;
    }

    /// <summary>
    /// Liste les conversation
    /// </summary>
    [HttpGet("Conversations")]
    [EnableQuery]
    [ProducesResponseType(200, Type = typeof(List<Conversation>))]
    public IActionResult GetConversation()
    {
        using var scope = HttpContext.RequestServices.CreateScope();

        var userId = UsersUtils.GetUniqueName(scope, HttpContext.User);

        return Ok(_depot.Conversations.Where(c => c.UserId == userId));
    }

    /// <summary>
    /// Liste les messages
    /// </summary>
    [HttpGet("Conversations/{id}/Messages")]
    [EnableQuery]
    [ProducesResponseType(200, Type = typeof(List<Message>))]
    public IActionResult GetMessages(Guid id)
    {
        // conversation should be filtered by the user
        using var scope = HttpContext.RequestServices.CreateScope();

        var userId = UsersUtils.GetUniqueName(scope, HttpContext.User);

#nullable disable
        return Ok(_depot.Messages.Where(m => m.ConversationId == id &&
                                             m.Conversation.UserId == userId));
#nullable enable
    }


    /// <summary>
    /// Récupérer une conversation publique
    /// </summary>
    [HttpGet("Conversations/Public/{id}")]
    [AllowAnonymous]
    [ProducesResponseType(200, Type = typeof(List<Message>))]
    public async Task<IActionResult> GetPublicConversation(Guid id, CancellationToken token)
    {
        var conversation = await _depot.Conversations
            .Include(c => c.Messages)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.SystemMessage,
                c.IsPublic,
                c.CreatedOn,
                c.LastMessageDate,
#nullable disable
                Messages = c.Messages.Select(m => new
#nullable enable
                {
                    m.Id,
                    m.Content,
                    m.IsUser,
                    m.CreatedAt
                })
            })
            .FirstOrDefaultAsync(c => c.Id == id && c.IsPublic, token);

        if (conversation == null)
        {
            return NoContent();
        }

        return Ok(conversation);
    }

    /// <summary>
    /// Envoyer un prompt à l'IA
    /// </summary>
    [HttpPost("Prompt")]
    [ProducesResponseType(200, Type = typeof(PostPromptResponse))]
    public async Task<IActionResult> EnvoyerPrompt([FromBody] PostPrompt prompt, CancellationToken cancellationToken)
    {
        // RequestServices plutôt qu'une portée enfant : ApiKeyAuthorizationContext est
        // enregistré en Scoped et rempli par ApiKeyMiddleware dans la portée de la
        // requête, donc une portée enfant en donnerait une instance vide et la
        // conversation d'un appelant par clé d'api serait créée sans propriétaire.
        var userId = UsersUtils.GetUniqueName(HttpContext.RequestServices, HttpContext.User);

        try
        {
            var response = await _conversationAIService.SendPromptAsync(prompt, userId, cancellationToken);

            return Ok(response);
        }
        catch (AIChatCompletionException e)
        {
            return BadRequest(ToValidationProblemDetails(e.ClientResult));
        }
    }

    /// <summary>
    /// Indique ce qu'ErabliereAI peut faire pour l'utilisateur authentifié :
    /// notamment si l'assistant a le droit de consulter ses données réelles.
    /// </summary>
    /// <remarks>
    /// L'interface appelle cette ressource à l'ouverture de la conversation. Lorsque
    /// les outils sont fermés, la conversation fonctionne exactement comme avant et
    /// une invitation discrète à s'abonner est affichée.
    /// </remarks>
    [HttpGet("Capabilities")]
    [ProducesResponseType(200, Type = typeof(GetErabliereAICapabilities))]
    public async Task<IActionResult> Capabilities(
        [FromServices] IErabliereAiCapabilityService capabilityService,
        CancellationToken token)
    {
        var capabilities = await capabilityService.GetCapabilitiesAsync(token);

        return Ok(new GetErabliereAICapabilities
        {
            ToolsEnabled = capabilities.ToolsEnabled,
            Plan = capabilities.Plan,
            PlanGateEnabled = capabilities.PlanGateEnabled,
            PlansGrantingAccess = capabilities.PlansGrantingAccess,
            SubscriptionUrl = capabilities.SubscriptionUrl
        });
    }

    /// <summary>
    /// Retourne l'avancement d'un prompt en cours de traitement.
    /// </summary>
    /// <remarks>
    /// L'identifiant est généré par le client et envoyé avec le prompt
    /// (<see cref="PostPrompt.ActivityId" />). Il ne donne accès à aucune donnée :
    /// la réponse ne contient que des libellés d'étapes.
    /// </remarks>
    [HttpGet("Prompt/Status/{activityId}")]
    [ProducesResponseType(200, Type = typeof(GetErabliereAIToolActivity))]
    public IActionResult PromptStatus(Guid activityId, [FromServices] IToolActivityTracker activityTracker)
    {
        var activity = activityTracker.Get(activityId);

        if (activity == null)
        {
            // Rien de publié : soit le prompt n'a pas encore commencé, soit cette
            // instance n'est pas celle qui le traite. Dans les deux cas le client
            // continue d'attendre sa réponse, il affiche simplement le libellé
            // générique.
            return Ok(new GetErabliereAIToolActivity());
        }

        return Ok(new GetErabliereAIToolActivity
        {
            Completed = activity.Completed,
            Steps = [.. activity.Steps.Select(s => new GetErabliereAIToolActivityStep
            {
                Round = s.Round,
                ToolName = s.ToolName,
                Label = s.Label
            })]
        });
    }

    private static ValidationProblemDetails ToValidationProblemDetails(ClientResultException e)
    {
        var error = new ValidationProblemDetails();

        error.Status = e.Status;
        foreach (var d in e.Data.Keys)
        {
            error.Errors[d.ToString() ?? ""] = [e.Data[d]?.ToString() ?? ""];
        }
        error.Detail = e.Message;

        return error;
    }

    /// <summary>
    /// Traduire un texte
    /// </summary>
    [HttpPost("Traduction")]
    public async Task<IActionResult> Traduire(
        [FromQuery] string from, [FromQuery] string to, [FromBody] PostTraduction traduction, CancellationToken token)
    {
        string key = _configuration["AzureTranslatorKey"] ?? "";
        string endpoint = "https://api.cognitive.microsofttranslator.com";
        string location = "eastus";

        // Input and output languages are defined as parameters.
        string route = $"/translate?api-version=3.0&from={from}&to={to}";
        string textToTranslate = traduction.Text ?? "";
        object[] body = [new { Text = textToTranslate }];
        var requestBody = JsonSerializer.Serialize(body);

        var client = _httpClientFactory.CreateClient("AITranslator");
        using var request = new HttpRequestMessage();

        // Build the request.
        request.Method = HttpMethod.Post;
        request.RequestUri = new Uri(endpoint + route);
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        request.Headers.Add("Ocp-Apim-Subscription-Key", key);
        // location required if you're using a multi-service or regional (not global) resource.
        request.Headers.Add("Ocp-Apim-Subscription-Region", location);

        // Send the request and get response.
        HttpResponseMessage response = await client.SendAsync(request, token).ConfigureAwait(false);

        // Read response as a object
        var responseBody = await response.Content.ReadAsStringAsync();

        var obj = JsonSerializer.Deserialize<List<object?>>(responseBody);

        return Ok(obj);
    }

    /// <summary>
    /// Post a image generation request
    /// </summary>
    /// <param name="request"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpPost("Images")]
    [ProducesResponseType(200, Type = typeof(PostImageGenerationResponse))]
    public async Task<IActionResult> Images([FromBody] PostImagesGenerationModel request, CancellationToken token)
    {
        var _client = new AzureOpenAIClient(
            new Uri(_configuration["AzureOpenAIImagesUri"] ?? _configuration["AzureOpenAIUri"] ?? ""),
            new AzureKeyCredential(_configuration["AzureOpenAIImagesKey"] ?? _configuration["AzureOpenAIKey"] ?? "")
        );

        var client = _client.GetImageClient(_configuration["AzureOpenAIDeploymentImageModelName"] ?? "Dalle3");

        var imagesResult = new List<GeneratedImage>();

        for (int i = 0; i < (request.ImageCount ?? 1); i++)
        {
            if (i >= 10)
            {
                break;
            }

            try
            {
                var images = await client.GenerateImageAsync(
                request.Prompt,
                GetImageGenerationOptions(request), token);

                imagesResult.Add(images.Value);
            }
            catch (ClientResultException ex)
            {
                if (ex.Message.Contains("Your request was rejected as a result of our safety system. Your prompt may contain text that is not allowed by our safety system."))
                {
                    return BadRequest("Le système de sécurité a rejeté votre demande. Votre prompt peut contenir du texte qui n'est pas autorisé par notre système de sécurité.");
                }

                throw;
            }
        }

        return Ok(new PostImageGenerationResponse
        {
            Images = [.. imagesResult.Select(ir => new PostImageGenerationResponseImage
            {
                Url = ir.ImageUri.ToString()
            })]
        });
    }

    private static ImageGenerationOptions GetImageGenerationOptions(PostImagesGenerationModel request)
    {
        return new ImageGenerationOptions
        {
            Quality = request.Quality == null ? GeneratedImageQuality.Standard : request.Quality switch
            {
                "Standard" => GeneratedImageQuality.Standard,
                "Hd" => GeneratedImageQuality.High,
                "High" => GeneratedImageQuality.High,
                _ => throw new ArgumentException("Invalid quality value")
            },
            Size = request.Size?.ToGeneratedImageSize(),
            Style = request.Style == null ? GeneratedImageStyle.Natural : request.Style switch
            {
                "Natural" => GeneratedImageStyle.Natural,
                "Vivid" => GeneratedImageStyle.Vivid,
                _ => throw new ArgumentException("Invalid style value")
            }
        };
    }

    /// <summary>
    /// Liste les messages
    /// </summary>
    [HttpPatch("Conversations/{id}")]
    [EnableQuery]
    [ProducesResponseType(200, Type = typeof(List<Message>))]
    public async Task<IActionResult> PatchConversation(Guid id, PatchConversation patch)
    {
        if (patch.UserId != null)
        {
            return BadRequest("Seulement les administrateurs peuvent modifier l'id de l'utilisateur.");
        }

        // conversation should be filtered by the user
        using var scope = HttpContext.RequestServices.CreateScope();

        var userId = UsersUtils.GetUniqueName(scope, HttpContext.User);

        var conversation = await _depot.Conversations.FindAsync([id], HttpContext.RequestAborted);

        if (conversation == null || conversation.UserId != userId)
        {
            return NotFound();
        }

        if (patch.IsPublic != null)
        {
            conversation.IsPublic = patch.IsPublic.Value;
        }

        await _depot.SaveChangesAsync(HttpContext.RequestAborted);

        return NoContent();
    }


    /// <summary>
    /// Delete a conversation
    /// </summary>
    [HttpDelete("Conversations/{id}")]
    public async Task<IActionResult> DeleteConversation(Guid id, CancellationToken cancellationToken)
    {
        using var scope = HttpContext.RequestServices.CreateScope();

        var userId = UsersUtils.GetUniqueName(scope, HttpContext.User);

        var conversation = await _depot.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);

        if (conversation == null)
        {
            return NoContent();
        }

        _depot.Conversations.Remove(conversation);
        await _depot.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    /// <summary>
    /// Liste les conversation en tant qu'administrteur
    /// </summary>
    [HttpGet("Admin/Conversations")]
    [EnableQuery]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    [ProducesResponseType(200, Type = typeof(List<Conversation>))]
    public IActionResult GetConversationAsAdmin()
    {
        return Ok(_depot.Conversations);
    }

    /// <summary>
    /// Permet à un administrateur de modifier le userId d'une conversation
    /// </summary>
    /// <param name="id"></param>
    /// <param name="patch"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpPatch("Conversations/{id}/UserId")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    public async Task<IActionResult> PatchConversationAsAdmin(Guid id, PatchConversation patch, CancellationToken token)
    {
        var conversation = await _depot.Conversations
            .FirstOrDefaultAsync(c => c.Id == id, token);

        if (conversation == null)
        {
            return NoContent();
        }

        if (patch.UserId != null)
        {
            conversation.UserId = patch.UserId;
        }

        if (patch.IsPublic != null)
        {
            conversation.IsPublic = patch.IsPublic.Value;
        }

        await _depot.SaveChangesAsync(token);

        return Ok(conversation);
    }
}
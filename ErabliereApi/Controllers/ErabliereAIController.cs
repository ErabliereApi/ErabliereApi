using Azure;
using OpenAI;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Patch;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Services.Users;
using ErabliereModel.Action.Post;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using OpenAI.Chat;
using OpenAI.Images;
using ErabliereApi.Extensions;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler représentant les données des dompeux
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(Roles = "ErabliereAIUser", Policy = "TenantIdPrincipal")]
public class ErabliereAIController : ControllerBase
{
    private readonly ErabliereDbContext _depot;
    private readonly IConfiguration _configuration;
    private readonly OpenAIClient _client;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    /// <param name="depot"></param>
    /// <param name="configuration"></param>
    public ErabliereAIController(ErabliereDbContext depot, IConfiguration configuration)
    {
        _depot = depot;
        _configuration = configuration;
        _client = new OpenAIClient(
            //new Uri(_configuration["AzureOpenAIUri"] ?? ""),
            new AzureKeyCredential(_configuration["AzureOpenAIKey"] ?? ""),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(_configuration["AzureOpenAIUri"] ?? ""),
            }
        );
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
        string defaultSystemPhrase = "Vous êtes un acériculteur expérimenté avec des connaissance scientifique et pratique.";
        // Premièrement ont obtient la conversation
        // if the convesation id is null, create a new conversation
        Conversation? conversation = await GetOrCreateConversation(prompt, defaultSystemPhrase, cancellationToken);

        string aiResponse = "Aucune réponse";

        var client = _client.GetChatClient(_configuration["AzureOpenAIDeploymentChatModelName"]);

        switch (prompt.PromptType)
        {
            case "Chat":
                // Dans le prompt de type Chat, on obtient l'historique de la conversation
                var messages = await _depot.Messages
                    .Where(m => m.ConversationId == prompt.ConversationId)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync(cancellationToken);

                var chatCompletionsOptions = new ChatCompletionOptions()
                {
                    //DeploymentName = _configuration["AzureOpenAIDeploymentChatModelName"],
                    Temperature = (float)0.7,
                    //MaxTokens = 800,
                    //NucleusSamplingFactor = (float)0.95,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0
                };

                var messagesPrompt = new List<ChatMessage>();

                messagesPrompt.Add(
                    new SystemChatMessage(
                        !string.IsNullOrWhiteSpace(conversation?.SystemMessage) ?
                            conversation.SystemMessage :
                            defaultSystemPhrase));

                foreach (var message in messages)
                {
                    messagesPrompt.Add(message.IsUser ?
                        new UserChatMessage(message.Content) :
                        new AssistantChatMessage(message.Content));
                }

                messagesPrompt.Add(new UserChatMessage(prompt.Prompt));

                var responseWithoutStream = await client.CompleteChatAsync(
                    messagesPrompt,
                    chatCompletionsOptions,
                    cancellationToken
                );

                var responseChat = responseWithoutStream.Value;
                aiResponse = responseChat?.Content?.FirstOrDefault()?.Text ?? "Aucune réponse";
                break;
            default:
                var completionResponse = await client.CompleteChatAsync(
                    [prompt.Prompt],
                    new ChatCompletionOptions
                    {
                        Temperature = (float)0.7,
                        //MaxTokens = 800,
                        //NucleusSamplingFactor = (float)0.95,
                        FrequencyPenalty = 0,
                        PresencePenalty = 0
                    },
                    cancellationToken
                );
                var completion = completionResponse.Value;

                var localText = completion?.Content?.FirstOrDefault()?.Text ?? "Aucune réponse";
                if (localText != null)
                {
                    aiResponse = localText;
                }
                break;
        }

        // create the messages for the database
        var query = new Message
        {
            ConversationId = prompt.ConversationId,
            Content = prompt.Prompt ?? "",
            IsUser = true,
            CreatedAt = DateTime.Now,
        };

        var response = new Message
        {
            ConversationId = prompt.ConversationId,
            Content = aiResponse,
            IsUser = false,
            CreatedAt = DateTime.Now,
        };

        await _depot.Messages.AddAsync(query, cancellationToken);
        await _depot.Messages.AddAsync(response, cancellationToken);
        await _depot.SaveChangesAsync(cancellationToken);

        return Ok(new PostPromptResponse
        {

            Prompt = prompt,
            Conversation = conversation,
            Response = response,
        });
    }

    private async Task<Conversation?> GetOrCreateConversation(PostPrompt prompt, string defaultSystemPhrase, CancellationToken cancellationToken)
    {
        Conversation? conversation = null;

        if (prompt.ConversationId == null)
        {
            using var scope = HttpContext.RequestServices.CreateScope();

            conversation = new Conversation
            {
                UserId = UsersUtils.GetUniqueName(scope, HttpContext.User),
                CreatedOn = DateTime.Now,
                LastMessageDate = DateTime.Now,
                Name = prompt.Prompt,
                SystemMessage = !string.IsNullOrWhiteSpace(prompt.SystemMessage) ? prompt.SystemMessage : defaultSystemPhrase,
            };
            _depot.Conversations.Add(conversation);
            await _depot.SaveChangesAsync(cancellationToken);
            prompt.ConversationId = conversation.Id;
        }
        else
        {
            conversation = await _depot.Conversations.FindAsync([prompt.ConversationId], cancellationToken);

            if (conversation != null)
            {
                conversation.LastMessageDate = DateTime.Now;
            }
        }

        return conversation;
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

        using (var client = new HttpClient())
        using (var request = new HttpRequestMessage())
        {
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
    }

    /// <summary>
    /// Post a image generation request
    /// </summary>
    /// <param name="request"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpPost("Images")]
    [ProducesResponseType(200, Type = typeof(PostImageGenerationResponse))]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Images([FromBody] PostImagesGenerationModel request, CancellationToken token)
    {
        var client = _client.GetImageClient(_configuration["AzureOpenAIDeploymentImageModelName"] ?? "Dalle3");

        var imagesResult = new List<GeneratedImage>();

        for (int i = 0; i < (request.ImageCount ?? 1); i++)
        {
            if (i >= 10)
            {
                break;
            }

            var images = await client.GenerateImageAsync(
                request.Prompt,
                new ImageGenerationOptions
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
                }, token);

            imagesResult.Add(images.Value);
        }

        return Ok(new PostImageGenerationResponse
        {
            Images = imagesResult.Select(ir => new PostImageGenerationResponseImage
            {
                Url = ir.ImageUri.ToString()
            }).ToArray()
        });
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
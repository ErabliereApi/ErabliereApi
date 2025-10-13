using Azure;
using Azure.AI.OpenAI;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Patch;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Extensions;
using ErabliereApi.Services.Users;
using ErabliereModel.Action.Post;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
using OpenAI.Images;
using System.ClientModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;

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
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    /// <param name="depot"></param>
    /// <param name="configuration"></param>
    /// <param name="httpClientFactory"></param>
    public ErabliereAIController(ErabliereDbContext depot, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _depot = depot;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
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

        ChatMessageContentPart? aiResponse;

        var _client = new AzureOpenAIClient(
            new Uri(_configuration["AzureOpenAIUri"] ?? ""),
            new AzureKeyCredential(_configuration["AzureOpenAIKey"] ?? "")
        );

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
                    Temperature = _configuration.GetRequiredValue<float>("LLMDefaultTemperature"),
                    FrequencyPenalty = 0,
                    PresencePenalty = 0,
                    EndUserId = MD5Hash(conversation.UserId)
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

                messagesPrompt.Add(GetNewPrompt(prompt));

                try
                {
                    var responseWithoutStream = await client.CompleteChatAsync(
                        messagesPrompt,
                        chatCompletionsOptions,
                        cancellationToken
                    );

                    var responseChat = responseWithoutStream.Value;
                    aiResponse = responseChat?.Content?.FirstOrDefault();
                }
                catch (ClientResultException e)
                {
                    var error = new ValidationProblemDetails();

                    error.Status = e.Status;
                    foreach (var d in e.Data.Keys)
                    {
                        error.Errors[d.ToString() ?? ""] = [e.Data[d]?.ToString() ?? ""];
                    }
                    error.Detail = e.Message;

                    return BadRequest(error);
                }
                
                break;
            default:
                var completionResponse = await client.CompleteChatAsync(
                    [prompt.Prompt],
                    new ChatCompletionOptions
                    {
                        Temperature = _configuration.GetRequiredValue<float>("LLMDefaultTemperature"),
                        FrequencyPenalty = 0,
                        PresencePenalty = 0,
                        EndUserId = MD5Hash(conversation.UserId)
                    },
                    cancellationToken
                );
                var completion = completionResponse.Value;

                aiResponse = completion?.Content?.FirstOrDefault();
                break;
        }

        // create the messages for the database
        var query = new Message
        {
            ConversationId = prompt.ConversationId,
            Content = prompt.Prompt ?? "",
            IsUser = true,
            CreatedAt = DateTime.Now,
            MessageParts = GetMessagesParts(prompt.Attachments)
        };

        var response = new Message
        {
            ConversationId = prompt.ConversationId,
            Content = aiResponse?.Text ?? "Aucune réponse",
            IsUser = false,
            CreatedAt = DateTime.Now,
            Refusal = aiResponse?.Refusal,
            ImageUri = aiResponse?.Kind == ChatMessageContentPartKind.Image ? aiResponse?.ImageUri.ToString() : null
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

    private List<MessagePart> GetMessagesParts(PromptAttachment[]? attachments)
    {
        return [];
    }

    private string? MD5Hash(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        using var md5HashAlgo = MD5.Create();

        var hashBytes = md5HashAlgo.ComputeHash(Encoding.UTF8.GetBytes(userId));

        return BitConverter.ToString(hashBytes);
    }

    private static UserChatMessage GetNewPrompt(PostPrompt prompt)
    {
        List<ChatMessageContentPart> attachments = new List<ChatMessageContentPart>();

        attachments.Add(ChatMessageContentPart.CreateTextPart(prompt.Prompt ?? ""));

        if (prompt.Attachments != null && prompt.Attachments.Length > 0)
        {
            foreach (var attachment in prompt.Attachments)
            {
                if (IsImage(attachment.ContentType))
                {
                    if (!string.IsNullOrWhiteSpace(attachment.PublicUri) && Uri.IsWellFormedUriString(attachment.PublicUri, UriKind.Absolute))
                    {
                        attachments.Add(
                            ChatMessageContentPart.CreateImagePart(
                                new Uri(attachment.PublicUri)));
                    }
                    else
                    {
                        using var memStream = new MemoryStream();

                        var b64 = Convert.FromBase64String(attachment.ContentBase64);
                        memStream.Write(b64, 0, b64.Length);

                        attachments.Add(
                            ChatMessageContentPart.CreateImagePart(
                                BinaryData.FromStream(memStream),
                                attachment.ContentType)
                            );
                    }
                }
                else if (attachment.ContentType.ToLower() == "text/plain")
                {
                    attachments.Add(
                        ChatMessageContentPart.CreateTextPart(
                            attachment.TextContent)
                    );
                }
                else if (attachment.ContentType.ToLower() == "text/pdf")
                {
                    using var pdfDoc = PdfDocument.Open(Convert.FromBase64String(attachment.ContentBase64));

                    var sb = new StringBuilder();

                    foreach (var page in pdfDoc.GetPages())
                    {
                        var text = page.Text;

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            sb.AppendLine(text);
                        }
                    }

                    attachments.Add(
                            ChatMessageContentPart.CreateTextPart(
                                sb.ToString())
                        );
                }
                else
                {
                    throw new NotImplementedException($"Le type de contenu {attachment.ContentType} n'est pas supporté pour les pièces jointes.");
                }
            }
        }

        return new UserChatMessage(attachments);
    }

    private static bool IsImage(string contentType)
    {
        switch (contentType.ToLower())
        {
            case "image/png":
            case "image/jpeg":
            case "image/jpg":
            case "image/gif":
            case "image/bmp":
            case "image/tiff":
            case "image/webp":
                return true;
            default:
                return false;
        }
    }

    private async Task<Conversation> GetOrCreateConversation(PostPrompt prompt, string defaultSystemPhrase, CancellationToken cancellationToken)
    {
        Conversation? conversation = null;

        if (prompt.ConversationId != null)
        {
            conversation = await _depot.Conversations.FindAsync([prompt.ConversationId], cancellationToken);

            if (conversation != null)
            {
                conversation.LastMessageDate = DateTime.Now;
            }
        }
        
        if (conversation == null)
        {
            using var scope = HttpContext.RequestServices.CreateScope();

            conversation = new Conversation
            {
                Id = prompt.ConversationId,
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
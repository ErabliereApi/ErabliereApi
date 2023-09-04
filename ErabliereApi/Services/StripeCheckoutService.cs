﻿using Stripe.Checkout;
using Stripe;
using Microsoft.Extensions.Options;
using AutoMapper;
using ErabliereApi.Donnees;
using ErabliereApi.StripeIntegration;
using ErabliereApi.Services.Users;

namespace ErabliereApi.Services;

/// <summary>
/// Implémentation de ICheckoutService permettan d'initialiser une session avec Stripe
/// </summary>
public class StripeCheckoutService : ICheckoutService
{
    private readonly IOptions<StripeOptions> _options;
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<StripeCheckoutService> _logger;
    private readonly IUserService _userService;
    private readonly IMapper _mapper;
    private readonly IApiKeyService _apiKeyService;
    private readonly UsageContext _usageContext;

    /// <summary>
    /// Constructeur
    /// </summary>
    /// <param name="options"></param>
    /// <param name="accessor"></param>
    /// <param name="logger"></param>
    /// <param name="userService"></param>
    /// <param name="mapper"></param>
    /// <param name="apiKeyService"></param>
    /// <param name="usageContext"></param>
    public StripeCheckoutService(IOptions<StripeOptions> options,
                                 IHttpContextAccessor accessor,
                                 ILogger<StripeCheckoutService> logger,
                                 IUserService userService,
                                 IMapper mapper,
                                 IApiKeyService apiKeyService,
                                 UsageContext usageContext)
    {
        _options = options;
        _accessor = accessor;
        _logger = logger;
        _userService = userService;
        _mapper = mapper;
        _apiKeyService = apiKeyService;
        _usageContext = usageContext;
    }

    /// <summary>
    /// Implémentation de ICheckoutService permettan d'initialiser une session avec Stripe
    /// </summary>
    /// <returns></returns>
    public async Task<object> CreateSessionAsync(CancellationToken token)
    {
        StripeConfiguration.ApiKey = _options.Value.ApiKey;

        var options = new SessionCreateOptions
        {
            SuccessUrl = _options.Value.SuccessUrl,
            CancelUrl = _options.Value.CancelUrl,
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = _options.Value.BasePlanPriceId
                }
            },
            Mode = "subscription",
            PaymentMethodTypes = new List<string>() { "card" }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: token);

        return session;
    }

    /// <summary>
    /// Implementation of a webhook needed for stripe
    /// </summary>
    /// <param name="json">The request body json</param>
    /// <returns></returns>
    public async Task Webhook(string json)
    {
        var signature = _accessor.HttpContext?.Request.Headers["Stripe-Signature"];

        var stripeEvent = EventUtility.ConstructEvent(json, signature, _options.Value.WebhookSiginSecret);

        await WebHookSwitchCaseLogic(
            stripeEvent, 
            _logger,
            _mapper, 
            _userService,
            _apiKeyService, 
            _accessor.HttpContext?.RequestAborted ?? CancellationToken.None);
    }

    /// <summary>
    /// Logic des differents event du webhook stripe
    /// </summary>
    /// <param name="stripeEvent"></param>
    /// <param name="logger"></param>
    /// <param name="mapper"></param>
    /// <param name="userService"></param>
    /// <param name="apiKeyService"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static async Task WebHookSwitchCaseLogic(Event stripeEvent,
        ILogger logger, IMapper mapper, IUserService userService, IApiKeyService apiKeyService, CancellationToken token)
    {
        switch (stripeEvent.Type)
        {
            case "customer.created":
                logger.LogInformation("Begin of customer.created");
                var customer = mapper.Map<Donnees.Customer>
                    (stripeEvent.Data.Object as Stripe.Customer);

                await userService.CreateCustomerAsync(customer, token);
                logger.LogInformation("End of customer.created");
                break;

            case "invoice.paid":
                //logger.LogInformation("Begin of invoice.paid");
                //var data = stripeEvent.Data.Object as Invoice;

                //if (data is null)
                //{
                //    throw new ArgumentNullException(nameof(data));
                //}

                //await apiKeyService.CreateApiKeyAsync(data.CustomerEmail, token);
                //logger.LogInformation("End of invoice.paid");
                break;

            case "customer.subscription.created":
                logger.LogInformation("Begin create API Key");
                var subscription = stripeEvent.Data.Object as Subscription;

                if (subscription is null)
                {
                    throw new ArgumentNullException(nameof(subscription));
                }

                await apiKeyService.CreateApiKeyAsync(subscription.CustomerId, token);
                logger.LogInformation("End of create API Key");

                logger.LogInformation("Begin of customer.subscription.created");
                await apiKeyService.SetSubscriptionKeyAsync(
                    subscription.CustomerId, subscription.Items.First().Id, token);
                logger.LogInformation("End of customer.subscription.created");
                break;

            default:
                logger.LogWarning("Unknow stripe event: {event}", stripeEvent);
                break;
        }
    }

    /// <inheritdoc />
    public Task ReccordUsageAsync(ApiKey apiKeyEntity)
    {
        if (apiKeyEntity.SubscriptionId is null)
        {
            throw new ArgumentNullException(nameof(apiKeyEntity.SubscriptionId));
        }

        _usageContext.Usages.Enqueue(new Usage
        {
            SubscriptionId = apiKeyEntity.SubscriptionId,
            Quantite = 1
        });

        return Task.CompletedTask;
    }
}

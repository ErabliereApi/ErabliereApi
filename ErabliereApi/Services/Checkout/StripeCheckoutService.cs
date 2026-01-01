using Stripe.Checkout;
using Stripe;
using Microsoft.Extensions.Options;
using ErabliereApi.Donnees;
using ErabliereApi.Services.StripeIntegration;
using ErabliereApi.Services.Users;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Extensions;

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
    private readonly IApiKeyService _apiKeyService;
    private readonly UsageContext _usageContext;

    /// <summary>
    /// Constructeur
    /// </summary>
    /// <param name="options"></param>
    /// <param name="accessor"></param>
    /// <param name="logger"></param>
    /// <param name="userService"></param>
    /// <param name="apiKeyService"></param>
    /// <param name="usageContext"></param>
    public StripeCheckoutService(IOptions<StripeOptions> options,
                                 IHttpContextAccessor accessor,
                                 ILogger<StripeCheckoutService> logger,
                                 IUserService userService,
                                 IApiKeyService apiKeyService,
                                 UsageContext usageContext)
    {
        _options = options;
        _accessor = accessor;
        _logger = logger;
        _userService = userService;
        _apiKeyService = apiKeyService;
        _usageContext = usageContext;
    }

    /// <summary>
    /// Implémentation de ICheckoutService permettan d'initialiser une session avec Stripe
    /// </summary>
    /// <returns></returns>
    public async Task<PostCheckoutObjResponse> CreateSessionAsync(CancellationToken token)
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
                    Price = _options.Value.BasePlanPriceId,
                }
            },
            Mode = "subscription",
            PaymentMethodTypes = new List<string>() { "card" }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: token);

        return new PostCheckoutObjResponse
        {
            Url = session.Url,
        };
    }

    /// <inheritdoc />
    public async Task<object?> GetCustomerSubscriptionStatusAsync(CancellationToken token)
    {
        var httpContext = _accessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is null");

        using var scope = httpContext.RequestServices.CreateScope();

        var uniqueName = UsersUtils.GetUniqueName(
            scope,
            httpContext.User);

        if (uniqueName == null)
        {
            return null;
        }

        var customer = await _userService.GetCustomerByUniqueNameAsync(uniqueName, token);

        if (customer == null || string.IsNullOrEmpty(customer.StripeId))
        {
            return null;
        }

        StripeConfiguration.ApiKey = _options.Value.ApiKey;

        var service = new SubscriptionService();
        var subscriptions = await service.ListAsync(new SubscriptionListOptions
        {
            Customer = customer.StripeId,
            Status = "all"
        }, cancellationToken: token);

        return subscriptions.Data;
    }

    /// <inheritdoc />
    public Task<object> GetCustomerUpcomingInvoiceAsync(string subscriptionId, CancellationToken token)
    {
        var httpContext = _accessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is null");

        using var scope = httpContext.RequestServices.CreateScope();

        var uniqueName = UsersUtils.GetUniqueName(
            scope,
            httpContext.User);

        if (uniqueName == null)
        {
            return Task.FromResult<object>(null!);
        }

        var customerTask = _userService.GetCustomerByUniqueNameAsync(uniqueName, token);

        return customerTask.ContinueWith<object>(t =>
        {
            var customer = t.Result;

            if (customer == null || string.IsNullOrEmpty(customer.StripeId))
            {
                return null!;
            }

            StripeConfiguration.ApiKey = _options.Value.ApiKey;

            var options = new InvoiceCreatePreviewOptions 
            { 
                Customer = customer.StripeId,
                Subscription = subscriptionId
            };
            var service = new InvoiceService();
            Invoice invoice = service.CreatePreview(options);

            return invoice;
        }, token);
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
            _userService,
            _apiKeyService,
            _accessor.HttpContext?.RequestAborted ?? CancellationToken.None);
    }

    /// <summary>
    /// Logic des differents event du webhook stripe
    /// </summary>
    /// <param name="stripeEvent"></param>
    /// <param name="logger"></param>
    /// <param name="userService"></param>
    /// <param name="apiKeyService"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static async Task WebHookSwitchCaseLogic(Event stripeEvent,
        ILogger logger,
        IUserService userService,
        IApiKeyService apiKeyService,
        CancellationToken token)
    {
        switch (stripeEvent.Type)
        {
            case "customer.subscription.created":
                logger.LogInformation("Begin create customer.subscription.created");

                var subscription = stripeEvent.Data.Object as Subscription
                    ?? throw new ArgumentNullException(nameof(stripeEvent), "Stripe.Data.Object event is null");

                logger.LogInformation("Begin of create customer");
                var stripeCustomer = await userService.StripeGetAsync(subscription.CustomerId, token);
                var customerMapped = stripeCustomer.MapTo<Stripe.Customer, Donnees.Customer>(customerMappingOption);
                customerMapped.UniqueName = customerMapped.Email;
                customerMapped.AccountType = "Stripe.Customer";
                customerMapped.StripeId = stripeCustomer.Id;
                var customer = await userService.GetOrCreateCustomerAsync(customerMapped, token);
                await userService.UpdateEnsureStripeInfoAsync(customer, customerMapped.StripeId, token);
                logger.LogInformation("End of create customer");

                logger.LogInformation("Begin of create API Key");
                await apiKeyService.CreateApiKeyAsync(new CreateApiKeyParameters { Customer = customer }, token);
                logger.LogInformation("End of create API Key");

                logger.LogInformation("Begin of customer.subscription.created");
                await apiKeyService.SetSubscriptionKeyAsync(
                    customer, subscription.Items.First().Id, token);
                logger.LogInformation("End of customer.subscription.created");
                break;

            default:
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace("Unknown stripe event: {Event}", stripeEvent);
                }
                break;
        }
    }

    /// <inheritdoc />
    public Task ReccordUsageAsync(ApiKey apiKey)
    {
        if (apiKey.SubscriptionId is null)
        {
            throw new ArgumentNullException(nameof(apiKey), "apiKeyEntity.SubscriptionId is null");
        }

        if (apiKey.Customer?.StripeId is null)
        {
            throw new ArgumentNullException(nameof(apiKey), "apiKeyEntity.Customer.StripeId is null");
        }

        _usageContext.Usages.Enqueue(new Usage
        {
            CustomerId = apiKey.Customer.StripeId,
            SubscriptionId = apiKey.SubscriptionId,
            Quantite = 1
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<object> GetProjectBalanceAsync(CancellationToken token)
    {
        StripeConfiguration.ApiKey = _options.Value.ApiKey;

        var service = new BalanceService();
        var balance = service.Get();

        return Task.FromResult<object>(balance);
    }

    /// <inheritdoc />
    public IEnumerable<Usage> GetUsageRecords()
    {
        return _usageContext.Usages.ToArray();
    }

    private static readonly Dictionary<string, Func<Stripe.Customer?, object?>> customerMappingOption = new()
    {
        { "Id", _ => null }
    };
}

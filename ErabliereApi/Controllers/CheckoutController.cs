using ErabliereApi.Extensions;
using ErabliereApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler de checkout
/// </summary>
[ApiController]
[Route("/[controller]")]
public class CheckoutController : ControllerBase
{
    private readonly ICheckoutService _checkoutService;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    public CheckoutController(
        ICheckoutService checkoutService,
        IConfiguration configuration)
    {
        _checkoutService = checkoutService;
        _configuration = configuration;
    }

    /// <summary>
    /// Create a checkout session
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> Checkout(CancellationToken token)
    {
        if (!_configuration.StripeIsEnabled())
        {
            return NotFound();
        }

        try
        {
            var session = await _checkoutService.CreateSessionAsync(token);

            return Ok(session);
        }
        catch (StripeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Webhook utilisé pour recevoir des appels d'un fournisseur de paiement
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[action]")]
    public async Task<IActionResult> Webhook()
    {
        if (!_configuration.StripeIsEnabled())
        {
            return NotFound();
        }

        using var reader = new StreamReader(HttpContext.Request.Body);

        var json = await reader.ReadToEndAsync();

        await _checkoutService.Webhook(json);

        return Ok();
    }

    /// <summary>
    /// Get the customer's subscription status
    /// </summary>
    [HttpGet]
    [Route("[action]")]
    [Authorize]
    public async Task<IActionResult> Subscriptions(CancellationToken token)
    {
        if (!_configuration.StripeIsEnabled())
        {
            return NotFound();
        }

        var status = await _checkoutService.GetCustomerSubscriptionStatusAsync(token);

        return Ok(status);
    }

    /// <summary>
    /// Get the customer's upcoming invoice
    /// </summary>
    [HttpGet]
    [Route("[action]")]
    [Authorize]
    public async Task<IActionResult> UpcomingInvoice(CancellationToken token)
    {
        if (!_configuration.StripeIsEnabled())
        {
            return NotFound();
        }

        var upcomingInvoice = await _checkoutService.GetCustomerUpcomingInvoiceAsync(token);

        return Ok(upcomingInvoice);
    }

    /// <summary>
    /// Get current balance of the project stripe account
    /// </summary>
    [HttpGet]
    [Route("[action]")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    public async Task<IActionResult> GetBalance(CancellationToken token)
    {
        if (!_configuration.StripeIsEnabled())
        {
            return NotFound();
        }

        var balance = await _checkoutService.GetProjectBalanceAsync(token);

        return Ok(balance);
    }

    /// <summary>
    /// Permet d'obtenir la liste des enregistrements d'utilisation en attente d'envoi à Stripe
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("[action]")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    [ProducesResponseType(typeof(IEnumerable<Services.StripeIntegration.Usage>), 200)]
    public IActionResult GetUsagesQueue()
    {
        if (!_configuration.StripeIsEnabled())
        {
            return NotFound();
        }
        var invoices = _checkoutService.GetUsageRecords();
        return Ok(invoices);
    }
}

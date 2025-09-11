using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Extensions;
using ErabliereApi.Services.Notifications;
using ErabliereApi.Services.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Caching.Distributed;

namespace ErabliereApi.Authorization.Customers;

/// <summary>
/// Lorsqu'un utilisateur est authentifier avec un jeton Bearer, il faut assurer
/// que l'utilisateur existe en BD afin de faire fonctionner le modèle de propriété
/// (ownership) des données.
/// </summary>
public class EnsureCustomerExist : IMiddleware
{
    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<EnsureCustomerExist>>();

        if (context.User?.Identity?.IsAuthenticated == true)
        {
            using var scope = context.RequestServices.CreateScope();

            var uniqueName = UsersUtils.GetUniqueName(scope, context.User);

            if (uniqueName == null)
            {
                throw new InvalidOperationException("User is authenticated but no unique name was found");
            }

            if (uniqueName == "")
            {
                throw new InvalidOperationException("User is authenticated but unique name is empty");
            }

            var cache = context.RequestServices.GetRequiredService<IDistributedCache>();

            var customer = await cache.GetAsync<Customer>($"Customer_{uniqueName}", context.RequestAborted);

            if (customer == null)
            {
                await HandleCaseCustomerNotInCache(context, uniqueName, cache, logger);
            }
            else
            {
                await HandleCaseCustomerIsInCache(context, uniqueName, cache, customer, logger);
            }

            var customerAcceptTerms = await EnsureCustomerAcceptTermes(context, uniqueName, cache, logger);

            if (!customerAcceptTerms)
            {
                // Write an unauthorized response if the customer has not accepted terms
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("You must accept the terms of service before proceeding.");
                return;
            }
        }
        else
        {
            logger.LogInformation("User is not authenticated");
        }

        await next(context);
    }

    private readonly string[] termeRestrictionVerbes = ["POST", "PUT", "DELETE", "PATCH"];

    private async ValueTask<bool> EnsureCustomerAcceptTermes(HttpContext context, string uniqueName, IDistributedCache cache, ILogger<EnsureCustomerExist> logger)
    {
        if (context.Request.Path.Equals("/Customers/me/accept-terms", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("User is accepting termes, skipping terms check for customer {Customer}", uniqueName);
            return true;
        }
        else if (!termeRestrictionVerbes.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogDebug("Request method is not GET, skipping terms check for customer {Customer}", uniqueName);
            return true;
        }

        var customer = await cache.GetAsync<Customer>($"Customer_{uniqueName}", context.RequestAborted);

        if (customer == null)
        {
            logger.LogWarning("Customer {Customer} was not found in cache", uniqueName);
        }

        if (customer != null && customer.AcceptTermsAt == null)
        {
            logger.LogInformation("Customer {Customer} has not accepted terms", uniqueName);

            return false;
        }

        return true;
    }

    private static async Task HandleCaseCustomerIsInCache(HttpContext context, string uniqueName, IDistributedCache cache, Customer customer, ILogger<EnsureCustomerExist> logger)
    {
        logger.LogDebug("Customer {Customer} was found in cache", customer);

        if (ShouldUpdateLastAccessTime(customer, logger, out var userTime))
        {
            var dbContext = context.RequestServices.GetRequiredService<ErabliereDbContext>();

            var dbCustomer = await dbContext.Customers.FirstOrDefaultAsync(c => c.UniqueName == uniqueName, context.RequestAborted);

            if (dbCustomer != null)
            {
                dbCustomer.LastAccessTime = userTime;

                await dbContext.TrySaveChangesAsync(context.RequestAborted, logger);

                await cache.SetAsync($"Customer_{uniqueName}", dbCustomer, context.RequestAborted);
            }
            else
            {
                logger.LogCritical("Customer {Customer} was not found in database after it was found in cache", uniqueName);
            }
        }
        else
        {
            logger.LogInformation("Customer {Customer} lastAccessTime is today ({Date}) and was in cache", customer.UniqueName, customer.LastAccessTime);
        }
    }

    private static async Task HandleCaseCustomerNotInCache(HttpContext context, string uniqueName, IDistributedCache cache, ILogger<EnsureCustomerExist> logger)
    {
        var dbContext = context.RequestServices.GetRequiredService<ErabliereDbContext>();

        if (!await dbContext.Customers.AnyAsync(c => c.UniqueName == uniqueName, context.RequestAborted))
        {
            // Cas spécial ou l'utilisateur aurait été créé précédement
            // et le uniqueName est vide.
            if (await dbContext.Customers.AnyAsync(c => c.UniqueName == "", context.RequestAborted))
            {
                await HandleSpecialCase(context, uniqueName, cache, dbContext, logger);
            }
            else
            {
                var customerEntity = await dbContext.Customers.AddAsync(new Customer
                {
                    Email = uniqueName,
                    UniqueName = uniqueName,
                    Name = context.User.FindFirst("name")?.Value ?? "",
                    AccountType = "AzureAD"
                }, context.RequestAborted);

                customerEntity.Entity.CreationTime = GetUserDateTimeOffSetNow(customerEntity.Entity, logger);
                customerEntity.Entity.LastAccessTime = customerEntity.Entity.CreationTime;

                await dbContext.SaveChangesAsync(context.RequestAborted);

                var customer = customerEntity.Entity;

                await cache.SetAsync($"Customer_{uniqueName}", customer, context.RequestAborted);

                await TrySendWelcomeNotification(context, customer);
            }
        }
        else
        {
            var customer = await dbContext.Customers.FirstAsync(c => c.UniqueName == uniqueName, context.RequestAborted);

            if (ShouldUpdateLastAccessTime(customer, logger, out var userTime))
            {
                customer.LastAccessTime = userTime;

                await dbContext.TrySaveChangesAsync(context.RequestAborted, context.RequestServices.GetRequiredService<ILogger<EnsureCustomerExist>>());
            }
            else
            {
                logger.LogInformation("User {Customer} was not in cache lastAccessTime is today ({Date})", customer.UniqueName, customer.LastAccessTime);
            }

            await cache.SetAsync($"Customer_{uniqueName}", customer, context.RequestAborted);
        }
    }

    private static async Task TrySendWelcomeNotification(HttpContext context, Customer customer)
    {
        try
        {
            var _notificationService = context.RequestServices.GetRequiredService<NotificationService>();
            var _emailConfig = context.RequestServices.GetRequiredService<EmailConfig>();

            await _notificationService.SendNotificationAsync(
                $"{customer.Name} Bienvenue dans l'application ErabliereApi !",
                customer.Email,
                NotificationType.Email,
                context.RequestAborted
            );

            await _notificationService.SendNotificationAsync(
                $"Un nouveau client a été créé avec succès. {customer.UniqueName}",
                _emailConfig.Sender ?? throw new InvalidOperationException("Email sender is not configured"),
                NotificationType.Email,
                context.RequestAborted
            );
        }
        catch (Exception ex)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<EnsureCustomerExist>>();

            logger.LogError(ex, "Failed to send welcome notification to {Customer}", customer.UniqueName);
        }
    }

    private static DateTimeOffset GetUserDateTimeOffSetNow(Customer customer, ILogger<EnsureCustomerExist> logger)
    {
        if (!string.IsNullOrWhiteSpace(customer.TimeZone))
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(customer.TimeZone);

                var userTime = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, timeZone);

                return userTime;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while converting time zone {TimeZone}", customer.TimeZone);
            }
        }

        return DateTimeOffset.Now;
    }

    private static async Task HandleSpecialCase(HttpContext context, string uniqueName, IDistributedCache cache, ErabliereDbContext dbContext, ILogger<EnsureCustomerExist> logger)
    {
        var cust = await dbContext.Customers.SingleAsync(c => c.UniqueName == "", context.RequestAborted);

        cust.UniqueName = uniqueName;

        if (!cust.AccountType.Contains("AzureAD", StringComparison.OrdinalIgnoreCase))
        {
            cust.AccountType = string.Concat(cust.AccountType, ',', "AzureAD");
        }

        var userTime = GetUserDateTimeOffSetNow(cust, logger);

        if (cust.LastAccessTime == null || cust.LastAccessTime.Value.Date < userTime.Date)
        {
            cust.LastAccessTime = userTime;
        }

        await dbContext.SaveChangesAsync(context.RequestAborted);

        await cache.SetAsync($"Customer_{uniqueName}", cust, context.RequestAborted);
    }

    private static bool ShouldUpdateLastAccessTime(Customer customer, ILogger<EnsureCustomerExist> logger, out DateTimeOffset userTime)
    {
        userTime = GetUserDateTimeOffSetNow(customer, logger);

        if (customer.LastAccessTime == null)
        {
            return true;
        }

        if (customer.LastAccessTime.Value < userTime &&
            userTime - customer.LastAccessTime.Value > TimeSpan.FromMinutes(5))
        {
            return true;
        }

        return false;
    }
}

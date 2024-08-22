using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Extensions;
using Microsoft.EntityFrameworkCore;
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
                await HandleCaseCustomerNotInCache(context, uniqueName, cache);
            }
            else
            {
                await HandleCaseCustomerIsInCache(context, uniqueName, cache, customer);
            }
        }
        else 
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<EnsureCustomerExist>>();

            logger.LogWarning("User is not authenticated");
        }

        await next(context);
    }

    private static async Task HandleCaseCustomerIsInCache(HttpContext context, string uniqueName, IDistributedCache cache, Customer customer)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<EnsureCustomerExist>>();

        logger.LogDebug("Customer {Customer} was found in cache", customer);

        if (customer.LastAccessTime == null || customer.LastAccessTime.Value.Date < DateTimeOffset.Now.Date)
        {
            var dbContext = context.RequestServices.GetRequiredService<ErabliereDbContext>();

            var dbCustomer = await dbContext.Customers.FirstOrDefaultAsync(c => c.UniqueName == uniqueName, context.RequestAborted);

            if (dbCustomer != null)
            {
                dbCustomer.LastAccessTime = DateTimeOffset.Now;

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

    private static async Task HandleCaseCustomerNotInCache(HttpContext context, string uniqueName, IDistributedCache cache)
    {
        var dbContext = context.RequestServices.GetRequiredService<ErabliereDbContext>();

        if (!await dbContext.Customers.AnyAsync(c => c.UniqueName == uniqueName, context.RequestAborted))
        {
            // Cas spécial ou l'utilisateur aurait été créé précédement
            // et le uniqueName est vide.
            if (await dbContext.Customers.AnyAsync(c => c.UniqueName == "", context.RequestAborted))
            {
                await HandleSpecialCase(context, uniqueName, cache, dbContext);
            }
            else
            {
                var customerEntity = await dbContext.Customers.AddAsync(new Customer
                {
                    Email = uniqueName,
                    UniqueName = uniqueName,
                    Name = context.User.FindFirst("name")?.Value ?? "",
                    AccountType = "AzureAD",
                    CreationTime = DateTimeOffset.Now,
                    LastAccessTime = DateTimeOffset.Now
                }, context.RequestAborted);

                await dbContext.SaveChangesAsync(context.RequestAborted);

                var customer = customerEntity.Entity;

                await cache.SetAsync($"Customer_{uniqueName}", customer, context.RequestAborted);
            }
        }
        else
        {
            var customer = await dbContext.Customers.FirstAsync(c => c.UniqueName == uniqueName, context.RequestAborted);

            if (customer.LastAccessTime == null || customer.LastAccessTime.Value.Date < DateTimeOffset.Now.Date)
            {
                customer.LastAccessTime = DateTimeOffset.Now;

                await dbContext.TrySaveChangesAsync(context.RequestAborted, context.RequestServices.GetRequiredService<ILogger<EnsureCustomerExist>>());
            }
            else 
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<EnsureCustomerExist>>();

                logger.LogInformation("User {Customer} was not in cache lastAccessTime is today ({Date})", customer.UniqueName, customer.LastAccessTime);
            }

            await cache.SetAsync($"Customer_{uniqueName}", customer, context.RequestAborted);
        }
    }

    private static async Task HandleSpecialCase(HttpContext context, string uniqueName, IDistributedCache cache, ErabliereDbContext dbContext)
    {
        var cust = await dbContext.Customers.SingleAsync(c => c.UniqueName == "", context.RequestAborted);

        cust.UniqueName = uniqueName;

        if (!cust.AccountType.Contains("AzureAD", StringComparison.OrdinalIgnoreCase))
        {
            cust.AccountType = string.Concat(cust.AccountType, ',', "AzureAD");
        }

        if (cust.LastAccessTime == null || cust.LastAccessTime.Value.Date < DateTimeOffset.Now.Date)
        {
            cust.LastAccessTime = DateTimeOffset.Now;
        }

        await dbContext.SaveChangesAsync(context.RequestAborted);

        await cache.SetAsync($"Customer_{uniqueName}", cust, context.RequestAborted);
    }
}

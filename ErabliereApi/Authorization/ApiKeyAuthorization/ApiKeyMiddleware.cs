﻿using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Services;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace ErabliereApi.Authorization;

/// <summary>
/// Middleware pour la gestion d'autorisation des clé d'api
/// </summary>
public class ApiKeyMiddleware : IMiddleware
{
    const string XApiKeyHeader = "X-ErabliereApi-ApiKey";

    /// <summary>
    /// Execute the logic needed to track usage of api keys
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        bool authorizeRequest = true;

        if (context.Request.Headers.TryGetValue(XApiKeyHeader, out var apiKey))
        {
            var apiKeyService = context.RequestServices.GetRequiredService<IApiKeyService>();

            authorizeRequest = apiKeyService.TryHashApiKey(apiKey.ToString(), out var hashkey);

            var dbContext = context.RequestServices.GetRequiredService<ErabliereDbContext>();

            var apiKeyEntity = await dbContext.ApiKeys.AsNoTracking().FirstOrDefaultAsync(k => k.Key == hashkey);

            if (apiKeyEntity != null && apiKeyEntity.IsActive())
            {
                // SubscriptionId is null when the key is not linked to a subscription, o need to record usage in checkoutservice
                if (apiKeyEntity.SubscriptionId != null)
                {
                    var checkoutService = context.RequestServices.GetRequiredService<ICheckoutService>();

                    await checkoutService.ReccordUsageAsync(apiKeyEntity);
                }

                await AuthorizeRequestAsync(context, dbContext, apiKeyEntity);
            }
            else
            {
                authorizeRequest = false;
            }
        }

        if (authorizeRequest)
        {
            await next(context);
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.Headers.Append("X-ErabliereApi-ApiKey-Reason", "Something is wrong with the token");
        }
    }

    private static async Task AuthorizeRequestAsync(HttpContext context, ErabliereDbContext dbContext, ApiKey apiKeyEntity)
    {
        var apiKeyAuthContext = context.RequestServices.GetRequiredService<ApiKeyAuthorizationContext>();

        apiKeyAuthContext.Authorize = true;
        apiKeyAuthContext.Customer = await dbContext.Customers.FindAsync([apiKeyEntity.CustomerId], cancellationToken: context.RequestAborted);
        apiKeyAuthContext.ApiKey = apiKeyEntity;
    }
}

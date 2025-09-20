using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using System.Globalization;
using System.Threading.Tasks;

namespace ErabliereApi.Middlewares;

/// <summary>
/// Middleware that adds an "X-ODataCount" header to the HTTP response if the OData total count feature is present.
/// </summary>
/// <remarks>This middleware inspects the OData feature of the current HTTP request to determine if a total count
/// value is available. If the total count is present, it adds the value to the response headers under the key
/// "X-ODataCount". This is typically used in OData APIs to expose the total number of items in a collection when
/// pagination is applied.</remarks>
public class ODataCountHeaderMiddleware : IMiddleware
{
    private readonly string _defaultMaxTop;

    /// <summary>
    /// Constructeur par défaut
    /// </summary>
    /// <param name="config"></param>
    public ODataCountHeaderMiddleware(IConfiguration config)
    {
        _defaultMaxTop = config.GetValue<int>("OData:MaxTop", 200)
            .ToString();
    }

    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Method == "GET")
        {
            // Si l'attribut EnableQuery a été utilisé sans $top, ajouter une valeur par défaut
            var enableQueryAttributes = context.GetEndpoint()?.Metadata.GetOrderedMetadata<EnableQueryAttribute>();
            if (enableQueryAttributes != null &&
                !context.Request.Query.ContainsKey("$top"))
            {
                context.Request.QueryString = context.Request.QueryString.Add("$top", _defaultMaxTop);
            }

            // Enregistrer OnStarting avant d'appeler next
            context.Response.OnStarting(state =>
            {
                var httpContext = state as HttpContext;
                if (httpContext == null)
                {
                    return Task.CompletedTask;
                }

                var totalCount = httpContext.Request.ODataFeature()?.TotalCount;
                if (totalCount.HasValue)
                {
                    // Utiliser InvariantCulture pour sérialiser proprement le nombre
                    httpContext.Response.Headers["X-ODataCount"] = totalCount.Value.ToString(CultureInfo.InvariantCulture);
                }

                var nextLink = httpContext.Request.ODataFeature()?.NextLink;
                if (nextLink != null)
                {
                    httpContext.Response.Headers["X-ODataNextLink"] = nextLink.ToString();
                }

                return Task.CompletedTask;
            }, context);
        }

        await next(context);
    }
}

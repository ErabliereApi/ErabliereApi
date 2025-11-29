using ErabliereApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ErabliereApi.OperationFilter;

/// <summary>
/// Ajout des indication pour les actions possédant l'attribut Authorize.
/// L'attribue sera déduit soit de la méthode ou de la classe.
/// </summary>
public class AuthorizeCheckOperationFilter : IOperationFilter
{
    private readonly IConfiguration _config;

    /// <summary>
    /// Constructeur
    /// </summary>
    /// <param name="config"></param>
    public AuthorizeCheckOperationFilter(IConfiguration config)
    {
        _config = config;
    }

    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAuthorize = 
            context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() == true ||
            context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();

        var oneAuthMethodEnabled =
            _config.IsAuthEnabled() ||
            _config.StripeIsEnabled();

        if (hasAuthorize && oneAuthMethodEnabled)
        {
            if (operation.Responses == null)
            {
                operation.Responses = new();
            }

            operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
            operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });

            operation.Security = new List<OpenApiSecurityRequirement>();

            var scopes = _config[ErabliereApi.Swagger.OIDC_SCOPES]
                ?? throw new InvalidOperationException("OIDC_SCOPES non initialisé");

            if (_config.IsAuthEnabled())
            {
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [
                        new OpenApiSecuritySchemeReference("oauth2", context.Document)
                    ] = new List<string> { scopes }
                });
            }

            if (_config.StripeIsEnabled())
            {
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [
                        new OpenApiSecuritySchemeReference("ApiKey", context.Document)
                    ] = new List<string> { "" }
                });
            }
        }
    }
}

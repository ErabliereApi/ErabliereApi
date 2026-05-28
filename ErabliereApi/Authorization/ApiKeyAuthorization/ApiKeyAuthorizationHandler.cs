using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace ErabliereApi.Authorization;

/// <summary>
/// Authorise les clients qui ont une clé d'api valide en regardant l'état de ApiKeyAuthrozationContext.
/// </summary>
public class ApiKeyAuthrizationHandler : IAuthorizationHandler
{
    private readonly IHttpContextAccessor _accessor;

    /// <summary>
    /// Constructeur par défaut de cette classe singleton
    /// </summary>
    public ApiKeyAuthrizationHandler(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    /// <inheritdoc />
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        var httpContext = _accessor.HttpContext;

        if (httpContext != null)
        {
            var authContext = httpContext.RequestServices.GetRequiredService<ApiKeyAuthorizationContext>();

            if (authContext.Authorize)
            {
                foreach (var requirement in context.PendingRequirements)
                {
                    if (requirement.GetType() == typeof(RolesAuthorizationRequirement))
                    {
                        context.Fail();
                    }
                    else
                    {
                        context.Succeed(requirement);
                    }
                }
            }
        }
        
        return Task.CompletedTask;
    }
}

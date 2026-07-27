using System.Security.Claims;
using ErabliereApi.Authorization;
using ErabliereApi.Extensions;

namespace ErabliereApi.Services.Users;

internal static class UsersUtils
{
    /// <summary>
    /// Permet d'obtenir le nom unique de l'utilisateur.
    /// 
    /// Si l'utilisateur est authentifié, on utilise les claims pour obtenir le nom unique.
    /// Par ordre d'importance, la claim unique_name est cherché, puis preferred_username.
    /// 
    /// Si l'utilisateur n'est pas authentifié, mais on utilise un clé d'api, va chercher
    /// le nom unique de l'utilisateur dans le contexte d'autorisation.
    /// </summary>
    /// <param name="scope"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    internal static string? GetUniqueName(IServiceScope scope, ClaimsPrincipal? user)
    {
        return GetUniqueName(scope.ServiceProvider, user);
    }

    /// <summary>
    /// Même chose que <see cref="GetUniqueName(IServiceScope, ClaimsPrincipal)" />, mais
    /// à partir d'un fournisseur de services plutôt que d'une portée.
    ///
    /// La distinction compte pour une requête authentifiée par clé d'api :
    /// <see cref="ApiKeyAuthorizationContext" /> est enregistré en <c>Scoped</c> et
    /// c'est <c>ApiKeyMiddleware</c> qui le remplit dans la portée de la requête.
    /// Une portée enfant créée avec <c>CreateScope()</c> en reçoit une instance
    /// neuve et vide, donc un appelant qui doit reconnaître un utilisateur par clé
    /// d'api doit passer <c>HttpContext.RequestServices</c> ici.
    /// </summary>
    internal static string? GetUniqueName(IServiceProvider serviceProvider, ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated == true)
        {
            var uniqueName = user.FindFirst("unique_name")?.Value ?? "";

            if (string.IsNullOrWhiteSpace(uniqueName))
            {
                uniqueName = user.FindFirst("preferred_username")?.Value ?? "";
            }

            if (string.IsNullOrWhiteSpace(uniqueName))
            {
                uniqueName = user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ?? "";
            }

            return uniqueName;
        }

        var config = serviceProvider.GetRequiredService<IConfiguration>();

        if (config.StripeIsEnabled())
        {
            var apiKeyAuthContext = serviceProvider.GetRequiredService<ApiKeyAuthorizationContext>();

            return apiKeyAuthContext?.Customer?.UniqueName;
        }

        return null;
    }
}
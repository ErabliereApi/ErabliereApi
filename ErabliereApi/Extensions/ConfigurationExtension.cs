using static System.Boolean;
using static System.StringComparison;

namespace ErabliereApi.Extensions;

/// <summary>
/// M�thode d'extension de <see cref="IConfiguration"/>
/// </summary>
public static class ConfigurationExtension
{
    /// <summary>
    /// Indique si Stripe est activ�
    /// </summary>
    /// <returns></returns>
    public static bool StripeIsEnabled(this IConfiguration config)
    {
        return !string.IsNullOrWhiteSpace(config["Stripe.ApiKey"]);
    }

    /// <summary>
    /// Indique si l'authentification est activ�
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public static bool IsAuthEnabled(this IConfiguration config)
    {
        return string.Equals(config["USE_AUTHENTICATION"], TrueString, OrdinalIgnoreCase);
    }
}

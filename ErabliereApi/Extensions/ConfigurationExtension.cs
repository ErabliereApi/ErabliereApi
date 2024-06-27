using static System.Boolean;
using static System.StringComparison;

namespace ErabliereApi.Extensions;

/// <summary>
/// M�thode d'extension de <see cref="IConfiguration"/>
/// </summary>
public static class ConfigurationExtension
{
    /// <summary>
    /// Indique si Stripe est activé
    /// </summary>
    /// <returns></returns>
    public static bool StripeIsEnabled(this IConfiguration config)
    {
        return !string.IsNullOrWhiteSpace(config["Stripe.ApiKey"]);
    }

    /// <summary>
    /// Indique si l'authentification est activée
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public static bool IsAuthEnabled(this IConfiguration config)
    {
        return string.Equals(config["USE_AUTHENTICATION"], TrueString, OrdinalIgnoreCase);
    }

    /// <summary>
    /// Indique si le mode ChaosEngineering est activé
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public static bool IsChaosEngineeringEnabled(this IConfiguration config)
    {
        return string.Equals(config["USE_CHAOS_ENGINEERING"], TrueString, OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if the variable ASPNETCORE_ENVIRONMENT is equal to Development
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public static bool IsDevelopment(this IConfiguration config)
    {
        return string.Equals(config["ASPNETCORE_ENVIRONMENT"], Environments.Development, OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if the variable USEMQTT is equal to true
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    /// <remarks>USEMQTT is used to enable or disable the MQTT service</remarks>
    public static bool UseMQTT(this IConfiguration config)
    {
        return string.Equals(config["USEMQTT"]?.Trim(), TrueString, OrdinalIgnoreCase);
    }
}

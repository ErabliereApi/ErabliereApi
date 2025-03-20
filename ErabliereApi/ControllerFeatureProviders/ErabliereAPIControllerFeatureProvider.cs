using System.Reflection;
using ErabliereApi.Controllers;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace ErabliereApi.ControllerFeatureProviders;

/// <summary>
/// Classe de filtre pour le contrôleur de checkout
/// </summary>
public class ErabliereApiControllerFeatureProvider : ControllerFeatureProvider
{
    private readonly IConfiguration _config;

    /// <summary>
    /// Build a ControllerFeatureProvider that will filter out Stripe integration controllers
    /// base on if there is an api key in the configuration. The key checked is 'Stripe.ApiKey'.
    /// </summary>
    public ErabliereApiControllerFeatureProvider(IConfiguration config)
    {
        _config = config;
    }
    
    /// <inheritdoc />
    protected override bool IsController(TypeInfo typeInfo) 
    {
        if (typeInfo.Name == nameof(CheckoutController)) 
        {
            var stripeEnabled = !string.IsNullOrWhiteSpace(_config["Stripe.ApiKey"]);

            return stripeEnabled;
        }

        if (typeInfo.Name == nameof(ErabliereAIController))
        {
            var aiEnable = !string.IsNullOrWhiteSpace(_config["AzureOpenAIUri"]);

            return aiEnable;
        }

        if (typeInfo.Name == nameof(ImagesCapteurController))
        {
            var enableImages = !string.IsNullOrWhiteSpace(_config["EmailImageObserverUrl"]);

            return enableImages;
        }

        if (typeInfo.Name == nameof(MapController))
        {
            var enableMap = !string.IsNullOrWhiteSpace(_config["Mapbox_AccessToken"]);

            return enableMap;
        }

        if (typeInfo.Name == nameof(QuantumController))
        {
            var enableQuantum = !string.IsNullOrWhiteSpace(_config["IQP_API_TOKEN"]);
            return enableQuantum;
        }

        if (typeInfo.Name == nameof(HologramController))
        {
            var enableHologram = !string.IsNullOrWhiteSpace(_config["Hologram_Token"]);
            return enableHologram;
        }

        return base.IsController(typeInfo);
    }
}
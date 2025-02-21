using ErabliereApi.Authorization;
using ErabliereApi.Authorization.Customers;
using ErabliereApi.Authorization.Policies.Handlers;
using ErabliereApi.Authorization.Policies.Requirements;
using ErabliereApi.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;

namespace ErabliereApi.Extensions;

/// <summary>
/// Méthode d'extension afin d'ajouter l'authentification sur l'api
/// </summary>
public static class AuthenticationExtension
{
    /// <summary>
    /// Ajoute l'authentification à l'API
    /// </summary>
    public static IServiceCollection AddErabliereAPIAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IUserService, UserService>()
                .Decorate<IUserService, UserCacheDecorator>()
                .AddHttpContextAccessor();
        
        if (configuration.IsAuthEnabled())
        {
            Console.WriteLine("Authentication enabled.");
            
            SetAzureADVariables(configuration);

            if (!string.IsNullOrWhiteSpace(configuration["AzureAD:ClientId"]))
            {
                services.AddSingleton<IAuthorizationHandler, TenantIdHandler>();
                services.AddAuthorization(options =>
                {
                    options.AddPolicy("TenantIdPrincipal", policy =>
                    {
                        policy.Requirements.Add(new TenantIdRequirement(configuration["AzureAD:TenantIdPrincipal"] ?? ""));
                    });
                });

                services.AddMicrosoftIdentityWebApiAuthentication(configuration);
            }
            else
            {
                throw new NotImplementedException($"Authentification method not implemented. You must disable auth or use AzureAD by initializing client id. Use this configuration key:  AzureAD:ClientId");
            }

            services.AddTransient<EnsureCustomerExist>();
        }
        else
        {
            services.AddSingleton<IAuthorizationHandler, AllowAnonymous>();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("TenantIdPrincipal", policy =>
                {
                    policy.Requirements.Add(new TenantIdRequirement(""));
                });
            });
        }

        return services;
    }

    /// <summary>
    /// Ensure the AzureAD variables are set. This is for a special case when running in k8s.
    /// </summary>
    private static void SetAzureADVariables(IConfiguration configuration)
    {
        if (configuration["AzureAD__ClientId"] != null && configuration["AzureAD:ClientId"] == null)
        {
            configuration["AzureAD:ClientId"] = configuration["AzureAD__ClientId"];
        }

        if (configuration["AzureAD__TenantId"] != null && configuration["AzureAD:TenantId"] == null)
        {
            configuration["AzureAD:TenantId"] = configuration["AzureAD__TenantId"];
        }

        if (configuration["AzureAD__ClientSecret"] != null && configuration["AzureAD:ClientSecret"] == null)
        {
            configuration["AzureAD:ClientSecret"] = configuration["AzureAD__ClientSecret"];
        }

        if (configuration["AzureAD__TenantIdPrincipal"] != null && configuration["AzureAD:TenantIdPrincipal"] == null)
        {
            configuration["AzureAD:TenantIdPrincipal"] = configuration["AzureAD__TenantIdPrincipal"];
        }
    }
}
namespace ErabliereApi.Extensions;

/// <summary>
/// Méthode d'extension afin d'ajouter l'authentification sur l'api
/// </summary>
public static class AuthenticationExtension
{
    /// <summary>
    /// Ajoute l'authentification à l'API
    /// </summary>
    public static IServiceCollection AddErabliereAPIAuthentication(this IServiceCollection services, IConfiguration config)
    {
        services.AddTransient<IUserService, UserService>()
                .Decorate<IUserService, UserCacheDecorator>()
                .AddHttpContextAccessor();
        
        if (Configuration.IsAuthEnabled())
        {
            Console.WriteLine("Authentication enabled.");
            
            SetAzureADVariables();

            if (!string.IsNullOrWhiteSpace(Configuration["AzureAD:ClientId"]))
            {
                services.AddSingleton<IAuthorizationHandler, TenantIdHandler>();
                services.AddAuthorization(options =>
                {
                    options.AddPolicy("TenantIdPrincipal", policy =>
                    {
                        policy.Requirements.Add(new TenantIdRequirement(Configuration["AzureAD:TenantIdPrincipal"] ?? ""));
                    });
                });

                services.AddMicrosoftIdentityWebApiAuthentication(Configuration);
            }
            else
            {
                services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                        .AddIdentityServerAuthentication(options =>
                        {
                            options.Authority = Configuration["OIDC_AUTHORITY"];

                            options.ApiName = "erabliereapi";
                        });
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
    private static void SetAzureADVariables()
    {
        if (Configuration["AzureAD__ClientId"] != null && Configuration["AzureAD:ClientId"] == null)
        {
            Configuration["AzureAD:ClientId"] = Configuration["AzureAD__ClientId"];
        }

        if (Configuration["AzureAD__TenantId"] != null && Configuration["AzureAD:TenantId"] == null)
        {
            Configuration["AzureAD:TenantId"] = Configuration["AzureAD__TenantId"];
        }

        if (Configuration["AzureAD__ClientSecret"] != null && Configuration["AzureAD:ClientSecret"] == null)
        {
            Configuration["AzureAD:ClientSecret"] = Configuration["AzureAD__ClientSecret"];
        }

        if (Configuration["AzureAD__TenantIdPrincipal"] != null && Configuration["AzureAD:TenantIdPrincipal"] == null)
        {
            Configuration["AzureAD:TenantIdPrincipal"] = Configuration["AzureAD__TenantIdPrincipal"];
        }
    }
}
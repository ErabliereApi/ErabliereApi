using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using ErabliereApi.Donnees.AutoMapper;
using ErabliereApi.Depot.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging;
using static System.Boolean;
using static System.Environment;
using static System.StringComparison;
using ErabliereApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Logging;
using IdentityServer4.AccessTokenValidation;
using Microsoft.OData.Edm;
using ErabliereApi.Donnees;
using Microsoft.AspNet.OData.Extensions;
using Newtonsoft.Json.Serialization;

namespace ErabliereApi
{
    /// <summary>
    /// Classe Startup de l'api
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Méthodes ConfigureServices
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            // contrôleur
            services.AddControllers(o =>
            {
                o.EnableEndpointRouting = false;
            })
            .AddNewtonsoftJson(o =>
            {
                o.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                o.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });

            services.AddOData();

            // Forwarded headers
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            // Authentication
            if (string.Equals(GetEnvironmentVariable("USE_AUTHENTICATION"), TrueString, OrdinalIgnoreCase))
            {
                services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                        .AddIdentityServerAuthentication(options =>
                        {
                            options.Authority = GetEnvironmentVariable("OIDC_AUTHORITY");
                            
                            options.ApiName = "erabliereapi";

                            if (string.Equals(GetEnvironmentVariable("VALIDATE_ATUDIENCE"), FalseString, OrdinalIgnoreCase)) 
                            {
                                options.TokenValidationParameters = new TokenValidationParameters
                                {
                                    ValidateAudience = false
                                };
                            }
                        });
            }
            else
            {
                services.AddSingleton<IAuthorizationHandler, AllowAnonymous>();
            }

            // Swagger
            services.AjouterSwagger();

            // Cors
            if (string.Equals(GetEnvironmentVariable("USE_CORS"), TrueString, OrdinalIgnoreCase))
            {
                services.AddCors();
            }

            // Automapper
            services.AjouterAutoMapperErabliereApiDonnee();

            // Database
            if (string.Equals(GetEnvironmentVariable("USE_SQL"), FalseString, OrdinalIgnoreCase))
            {
                services.AddDbContext<ErabliereDbContext>(options =>
                {
                    options.UseInMemoryDatabase(nameof(ErabliereDbContext));
                });
            }
            else
            {
                services.AddDbContext<ErabliereDbContext>(options =>
                {
                    options.UseSqlServer(GetEnvironmentVariable("SQL_CONNEXION_STRING") ?? throw new InvalidOperationException("La variable d'environnement 'SQL_CONNEXION_STRING' � une valeur null."));

                    if (string.Equals(GetEnvironmentVariable("LOG_SQL"), "Console", OrdinalIgnoreCase))
                    {
                        options.LogTo(Console.WriteLine, LogLevel.Information);
                    }
                });
            }
        }

        /// <summary>
        /// Configure
        /// </summary>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            if (string.Equals(GetEnvironmentVariable("USE_SQL"), TrueString, OrdinalIgnoreCase) &&
                string.Equals(GetEnvironmentVariable("SQL_USE_STARTUP_MIGRATION"), TrueString, OrdinalIgnoreCase))
            {
                var database = serviceProvider.GetRequiredService<ErabliereDbContext>();

                database.Database.Migrate();
            }

            if (env.IsDevelopment())
            {
                IdentityModelEventSource.ShowPII = true;
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseForwardedHeaders();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UtiliserSwagger();

            app.UseRouting();

            if (string.Equals(GetEnvironmentVariable("USE_CORS"), TrueString, OrdinalIgnoreCase))
            {
                app.UseCors(option =>
                {
                    option.WithHeaders(GetEnvironmentVariable("CORS_HEADERS")?.Split(','));
                    option.WithMethods(GetEnvironmentVariable("CORS_METHODS")?.Split(','));
                    option.WithOrigins(GetEnvironmentVariable("CORS_ORIGINS")?.Split(','));
                });
            }

            if (string.Equals(GetEnvironmentVariable("USE_AUTHENTICATION"), TrueString, OrdinalIgnoreCase))
            {
                app.UseAuthentication();
            }
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.EnableDependencyInjection();
                endpoints.Select().Expand().Filter().Count().MaxTop(100).OrderBy();
                endpoints.MapControllers();
            });

            app.UseSpa(spa =>
            {
                
            });
        }
    }
}

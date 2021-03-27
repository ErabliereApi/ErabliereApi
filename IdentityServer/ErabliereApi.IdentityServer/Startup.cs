﻿using IdentityServerHost.Quickstart.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;

namespace ErabliereApi.IdentityServer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            // Forwarded headers
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            Config config;
            AppUsers users;

            var deserializerOptions = new JsonSerializerSettings();
            deserializerOptions.Converters.Add(new ClaimConverter());

            try
            {
                var prefixPath = Environment.GetEnvironmentVariable("SECRETS_FOLDER");

                var configFileName = "ErabliereApi.IdentityServer.Config.json";
                var usersFileName = "ErabliereApi.IdentityServer.Users.json";

                if (string.IsNullOrWhiteSpace(prefixPath) == false)
                {
                    configFileName = Path.Combine(prefixPath, configFileName);
                    usersFileName = Path.Combine(prefixPath, usersFileName);
                }

                Console.WriteLine("Deserialize : " + configFileName);
                var configFile = File.ReadAllText(configFileName);
                Console.WriteLine(configFile);
                config = JsonConvert.DeserializeObject<Config>(configFile, deserializerOptions);

                Console.WriteLine("Deserialize : " + usersFileName);
                var usersFile = File.ReadAllText(usersFileName);
                Console.WriteLine(usersFile);
                users = JsonConvert.DeserializeObject<AppUsers>(usersFile, deserializerOptions);
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Config or Users file cannor be deserialized.");

                throw;
            }

            var builder = services.AddIdentityServer(options =>
            {
                // see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
                options.EmitStaticAudienceClaim = true;
            })
                .AddInMemoryIdentityResources(config.Ids)
                .AddInMemoryApiResources(config.Apis)
                .AddInMemoryApiScopes(config.Scopes)
                .AddInMemoryClients(config.Clients)
                .AddTestUsers(users.Users);

            // not recommended for production - you need to store your key material somewhere secure
            builder.AddDeveloperSigningCredential();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment environment)
        {
            if (environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseForwardedHeaders();
            }

            app.UseStaticFiles();
            app.UseRouting();
            
            app.UseIdentityServer();

            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}

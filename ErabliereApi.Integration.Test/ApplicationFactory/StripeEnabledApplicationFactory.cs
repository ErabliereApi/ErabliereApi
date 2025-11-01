﻿using AutoFixture;
using AutoMapper;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Services;
using ErabliereApi.Services.Users;
using ErabliereApi.Test.Autofixture;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Stripe;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ErabliereApi.Integration.Test.ApplicationFactory;

public class StripeEnabledApplicationFactory<TStartup> : ErabliereApiApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        var config = new Dictionary<string, string?>
        {
            { "Stripe.ApiKey", "abcd" },
            { "StripeUsageReccord.SkipRecord", "true" },
            { "ErabliereApiUserService.TestMode", "true" },
            { "USE_SQL", "false" },
            { "IpInfoApi:DBFilePath", "" },
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();

        builder.UseConfiguration(configuration)
               .ConfigureAppConfiguration(app =>
               {
                   app.AddInMemoryCollection(config);
               });

        builder.ConfigureServices((webContext, services) =>
        {
            services.Configure<EmailConfig>(email =>
            {
                email.Sender = "Sender";
                email.SmtpServer = "SmtpServer";
                email.Email = "Email";
                email.Password = "Password";
                email.SmtpPort = 597;
            });

            services.RemoveAll<IEmailService>();
            services.TryAddSingleton(sp =>
            {
                return Substitute.For<IEmailService>();
            });

            services.RemoveAll<ICheckoutService>();
            services.TryAddTransient(sp =>
            {
                var checkoutService = Substitute.For<ICheckoutService>();

                checkoutService.CreateSessionAsync(Arg.Any<CancellationToken>()).Returns(session =>
                {
                    return new PostCheckoutObjResponse
                    {
                        Url = "https://example.com/checkout/session",
                    };
                });

                checkoutService.Webhook(Arg.Any<string>()).Returns(callInfo =>
                {
                    return Task.Run(async () =>
                    {
                        var json = JsonDocument.Parse(callInfo.Args()[0] as string ?? "");

                        var eventDeserialized = EventUtility.ParseEvent(json.RootElement.ToString())
                            ?? throw new InvalidOperationException("Event deserialization failed");

                        await StripeCheckoutService.WebHookSwitchCaseLogic(
                            eventDeserialized,
                            sp.GetRequiredService<ILogger<StripeCheckoutService>>(),
                            sp.GetRequiredService<IMapper>(),
                            sp.GetRequiredService<IUserService>(),
                            sp.GetRequiredService<IApiKeyService>(),
                            CancellationToken.None);
                    });
                });

                return checkoutService;
            });
        });
    }

    /// <summary>
    /// Fonction utilitaire permettant de ne pas dupliquer la logique d'ajout de clé d'api
    /// lors de test d'intégration
    /// </summary>
    /// <returns>La clé d'api créer pouvant être ajouter en entête lors des appels http</returns>
    public async Task<(Donnees.Customer, string)> CreateValidApiKeyAsync()
    {
        using var scope = Services.CreateScope();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var context = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();

        var customer = ErabliereFixture.CreerFixture().Create<Donnees.Customer>();

        await userService.GetOrCreateCustomerAsync(customer, CancellationToken.None);

        if (customer.Id == null)
        {
            throw new InvalidOperationException("Customer not created");
        }

        var key = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        var subId = Guid.NewGuid().ToString();

        context.ApiKeys.Add(new ApiKey
        {
            CreationTime = DateTimeOffset.Now,
            Key = apiKeyService.HashApiKey(key),
            SubscriptionId = subId,
            CustomerId = customer.Id.Value
        });

        await context.SaveChangesAsync();

        return (customer, key);
    }
}

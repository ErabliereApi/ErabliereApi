﻿using ErabliereApi.Depot.Sql;
using ErabliereApi.Integration.Test.ApplicationFactory;
using ErabliereApi.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Integration.Test;

public class StripeWebhookTest : IClassFixture<StripeEnabledApplicationFactory<Startup>>
{
    private readonly StripeEnabledApplicationFactory<Startup> _factory;

    public StripeWebhookTest(StripeEnabledApplicationFactory<Startup> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NominalFlow()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true,
            MaxAutomaticRedirections = 7
        });

        AssertCustomerDoesntExist();

        await Step("setup_intent.succeeded", client);
        await Step("setup_intent.created", client);
        await Step("checkout.session.completed", client);
        await Step("customer.created", client);
        AssertCustomerExist();
        await Step("payment_method.attached", client);
        await Step("customer.updated", client);
        await Step("invoice.created", client);
        await Step("invoice.finalized", client);
        await Step("invoice.paid", client);
        AssertCustomerApiKey();
        await Step("invoice.payment_succeeded", client);
        await Step("customer.subscription.created", client);
    }

    private void AssertCustomerApiKey()
    {
        var database = _factory.Services.GetRequiredService<ErabliereDbContext>();

        var customer = database.Customers.Single(c => c.Email == "john@doe.com");

        var apiKeys = database.ApiKeys.Where(a => a.CustomerId == customer.Id).ToArray();

        var apiKey = Assert.Single(apiKeys);

        Assert.NotNull(apiKey.Key);
        Assert.Null(apiKey.RevocationTime);
        Assert.Null(apiKey.DeletionTime);

        var emailService = _factory.Services.GetRequiredService<IEmailService>();

        emailService.ReceivedWithAnyArgs(1);
    }

    private void AssertCustomerDoesntExist()
    {
        var database = _factory.Services.GetRequiredService<ErabliereDbContext>();

        var customer = database.Customers.SingleOrDefault(c => c.Email == "john@doe.com");

        Assert.Null(customer);
    }

    private void AssertCustomerExist()
    {
        var database = _factory.Services.GetRequiredService<ErabliereDbContext>();

        var customer = database.Customers.Single(c => c.Email == "john@doe.com");

        Assert.NotNull(customer.Id);
        Assert.Equal("John Doe", customer.Name);
        Assert.Equal("Stripe.Customer", customer.AccountType);
        Assert.Equal("cus_2PXvRa6ztL96bV", customer.StripeId);
    }

    private async Task Step(string step, HttpClient client)
    {
        using var content = new StringContent(GetBody(step));

        var response = await client.PostAsync("/Checkout/webhook", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public static string GetBody(string step)
    {
        return File.ReadAllText(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "StripeWebhookJson",
            "StripeWebhookTest",
            "NominalFlow",
            step + ".body.json"));
    }

    public static string GetEvent(string step)
    {
        return File.ReadAllText(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "StripeWebhookJson",
            "StripeWebhookTest",
            "NominalFlow",
            step + ".json"));
    }
}
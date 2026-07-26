using ErabliereApi.Mcp.Configuration;
using ErabliereApi.Mcp.Models;
using ErabliereApi.Mcp.Services;
using ErabliereApi.Mcp.Tools;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using NSubstitute;
using Shouldly;

namespace ErabliereApi.Mcp.Test;

/// <summary>
/// Behaviour of the get_my_plan tool: a user should be able to find out what they
/// have access to from inside their MCP client, rather than discover it when a call
/// is refused.
/// </summary>
public class PlanToolsTest
{
    private static IOptions<McpPlanGatingOptions> Gating(bool enabled, string? subscriptionUrl = null)
    {
        return Options.Create(new McpPlanGatingOptions
        {
            Enabled = enabled,
            RequiredCapability = "mcp",
            SubscriptionUrl = subscriptionUrl,
            PlanCapabilities = new Dictionary<string, string[]>
            {
                ["gratuit"] = [],
                ["base"] = ["mcp", "ai"]
            }
        });
    }

    private static ISubscriptionPlanResolver Resolver(SubscriptionPlan plan)
    {
        var resolver = Substitute.For<ISubscriptionPlanResolver>();

        resolver.GetCurrentPlanAsync(Arg.Any<CancellationToken>()).Returns(plan);

        return resolver;
    }

    [Fact]
    public async Task APaidPlanIsReportedWithItsCapabilitiesAndItsAccess()
    {
        var resolver = Resolver(new SubscriptionPlan
        {
            Plan = "base",
            AbonnementActif = true,
            FrequenceFacturation = "mensuelle",
            DateDebut = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(-5))
        });

        var result = await PlanTools.GetMyPlanAsync(resolver, Gating(enabled: true));

        result.Data.Plan.ShouldBe("base");
        result.Data.ActiveSubscription.ShouldBeTrue();
        result.Data.BillingFrequency.ShouldBe("mensuelle");
        result.Data.Capabilities.ShouldBe(["mcp", "ai"]);
        result.Data.McpAccess.ShouldBeTrue();
        result.Data.PlanGateEnabled.ShouldBeTrue();

        result.Summary.ShouldContain("base");
        result.Summary.ShouldContain("grants access");
    }

    [Fact]
    public async Task AFreePlanIsToldWhatToSubscribeTo()
    {
        var resolver = Resolver(new SubscriptionPlan { Plan = "gratuit", AbonnementActif = false });

        var result = await PlanTools.GetMyPlanAsync(resolver, Gating(enabled: true, "https://erabliereapi.test/abonnement"));

        result.Data.McpAccess.ShouldBeFalse();
        result.Data.Capabilities.ShouldBeEmpty();

        result.Summary.ShouldContain("no active subscription");
        result.Summary.ShouldContain("'base' plan");
        result.Summary.ShouldContain("https://erabliereapi.test/abonnement");
    }

    [Fact]
    public async Task AnUnknownPlanGetsNoCapabilityAndNoAccess()
    {
        var resolver = Resolver(new SubscriptionPlan { Plan = "un-forfait-inconnu", AbonnementActif = true });

        var result = await PlanTools.GetMyPlanAsync(resolver, Gating(enabled: true));

        result.Data.Capabilities.ShouldBeEmpty();
        result.Data.McpAccess.ShouldBeFalse();
    }

    [Fact]
    public async Task WithTheGateOffEveryPlanIsReportedAsHavingAccess()
    {
        // What a self-hosted stdio server answers: it gates nothing, so saying the
        // free plan has no access would be a lie.
        var resolver = Resolver(new SubscriptionPlan { Plan = "gratuit", AbonnementActif = false });

        var result = await PlanTools.GetMyPlanAsync(resolver, Gating(enabled: false));

        result.Data.McpAccess.ShouldBeTrue();
        result.Data.PlanGateEnabled.ShouldBeFalse();
        result.Summary.ShouldContain("does not restrict access by plan");
    }

    [Fact]
    public async Task AnUnreadablePlanBecomesAReadableToolError()
    {
        var resolver = Substitute.For<ISubscriptionPlanResolver>();

        resolver.GetCurrentPlanAsync(Arg.Any<CancellationToken>())
                .Returns<SubscriptionPlan>(_ => throw new SubscriptionPlanUnavailableException("ErabliereAPI refused this api key."));

        var exception = await Should.ThrowAsync<McpException>(
            async () => await PlanTools.GetMyPlanAsync(resolver, Gating(enabled: true)));

        exception.Message.ShouldBe("ErabliereAPI refused this api key.");
    }
}

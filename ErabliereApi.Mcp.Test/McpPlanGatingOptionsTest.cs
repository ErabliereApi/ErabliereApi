using ErabliereApi.Mcp.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ErabliereApi.Mcp.Test;

/// <summary>
/// The plan to capability mapping is configuration, so the binding itself is part
/// of the contract: a section that silently failed to bind would leave the gate
/// granting nothing and locking everyone out.
/// </summary>
public class McpPlanGatingOptionsTest
{
    private static McpPlanGatingOptions Bind(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var services = new ServiceCollection();

        services.AddOptions<McpPlanGatingOptions>()
                .Bind(configuration.GetSection(McpPlanGatingOptions.SectionName));

        return services.BuildServiceProvider()
                       .GetRequiredService<IOptions<McpPlanGatingOptions>>()
                       .Value;
    }

    /// <summary>
    /// The shape shipped in appsettings.json, expressed the way the configuration
    /// providers flatten it.
    /// </summary>
    private static Dictionary<string, string?> DefaultSection(bool enabled = true) => new()
    {
        ["Mcp:PlanGating:Enabled"] = enabled ? "true" : "false",
        ["Mcp:PlanGating:RequiredCapability"] = "mcp",
        ["Mcp:PlanGating:DefaultPlan"] = "gratuit",
        ["Mcp:PlanGating:CacheDuration"] = "00:05:00",
        ["Mcp:PlanGating:SubscriptionUrl"] = "https://erabliereapi.freddycoder.com/abonnement",
        ["Mcp:PlanGating:PlanCapabilities:base:0"] = "mcp"
    };

    #region Binding

    [Fact]
    public void TheSectionOfAppsettingsBindsToTheOptions()
    {
        var options = Bind(DefaultSection());

        options.Enabled.ShouldBeTrue();
        options.RequiredCapability.ShouldBe("mcp");
        options.DefaultPlan.ShouldBe("gratuit");
        options.CacheDuration.ShouldBe(TimeSpan.FromMinutes(5));
        options.SubscriptionUrl.ShouldBe("https://erabliereapi.freddycoder.com/abonnement");
        options.PlanCapabilities["base"].ShouldBe(["mcp"]);
    }

    [Fact]
    public void AnAbsentSectionLeavesTheGateOff()
    {
        // What an existing deployment gets when it upgrades without touching its
        // configuration: nothing changes for its users.
        var options = Bind([]);

        options.Enabled.ShouldBeFalse();
        options.RequiredCapability.ShouldBe(McpPlanGatingOptions.McpCapability);
        options.CacheDuration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void EnvironmentStyleKeysOverrideTheFile()
    {
        // Mcp__PlanGating__Enabled=true is how an operator turns the gate on in a
        // container, and the double underscore reaches the binder as a colon.
        var values = DefaultSection(enabled: false);
        values["Mcp:PlanGating:Enabled"] = "true";

        Bind(values).Enabled.ShouldBeTrue();
    }

    [Fact]
    public void SeveralCapabilitiesOnSeveralPlansAllBind()
    {
        var options = Bind(new Dictionary<string, string?>
        {
            ["Mcp:PlanGating:PlanCapabilities:base:0"] = "mcp",
            ["Mcp:PlanGating:PlanCapabilities:base:1"] = "ai",
            ["Mcp:PlanGating:PlanCapabilities:entreprise:0"] = "mcp"
        });

        options.PlanCapabilities.Count.ShouldBe(2);
        options.CapabilitiesFor("base").ShouldBe(["mcp", "ai"]);
        options.CapabilitiesFor("entreprise").ShouldBe(["mcp"]);
    }

    #endregion

    #region Capability lookup

    [Fact]
    public void APlanHoldingTheCapabilityIsGrantedAccess()
    {
        Bind(DefaultSection()).GrantsAccess("base").ShouldBeTrue();
    }

    [Theory]
    [InlineData("BASE")]
    [InlineData("Base")]
    public void ThePlanLookupIsCaseInsensitive(string plan)
    {
        // The configuration binder builds a dictionary with the ordinal comparer
        // whatever the property is initialized with, so the case-insensitivity has
        // to come from the lookup itself.
        Bind(DefaultSection()).GrantsAccess(plan).ShouldBeTrue();
    }

    [Fact]
    public void TheCapabilityLookupIsCaseInsensitiveToo()
    {
        var values = DefaultSection();
        values["Mcp:PlanGating:PlanCapabilities:base:0"] = "MCP";

        Bind(values).GrantsAccess("base").ShouldBeTrue();
    }

    [Fact]
    public void APlanWithAnEmptyCapabilityListIsRefused()
    {
        var values = DefaultSection();
        values["Mcp:PlanGating:PlanCapabilities:gratuit:0"] = "";

        var options = Bind(values);

        options.GrantsAccess("gratuit").ShouldBeFalse();
    }

    [Fact]
    public void APlanAbsentFromTheMappingIsRefused()
    {
        var options = Bind(DefaultSection());

        options.GrantsAccess("gratuit").ShouldBeFalse();
        options.GrantsAccess("un-forfait-inconnu").ShouldBeFalse();
        options.CapabilitiesFor("un-forfait-inconnu").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ANullOrBlankPlanIsRefused(string? plan)
    {
        Bind(DefaultSection()).GrantsAccess(plan).ShouldBeFalse();
    }

    [Fact]
    public void ADifferentRequiredCapabilityChangesWhoGetsIn()
    {
        var values = DefaultSection();
        values["Mcp:PlanGating:RequiredCapability"] = "ai";

        var options = Bind(values);

        options.GrantsAccess("base").ShouldBeFalse();
        options.PlansGrantingAccess().ShouldBeEmpty();
    }

    [Fact]
    public void PlansGrantingAccessNamesWhatToSubscribeTo()
    {
        var values = DefaultSection();
        values["Mcp:PlanGating:PlanCapabilities:entreprise:0"] = "mcp";

        Bind(values).PlansGrantingAccess().ShouldBe(["base", "entreprise"], ignoreOrder: true);
    }

    #endregion

    #region Validation

    [Fact]
    public void AValidSectionProducesNoError()
    {
        Bind(DefaultSection()).Validate().ShouldBeEmpty();
    }

    [Fact]
    public void ADisabledGateIsNeverValidated()
    {
        // Nothing of the section is read when the gate is off, so an incomplete one
        // must not keep the server from starting.
        var options = Bind(new Dictionary<string, string?>
        {
            ["Mcp:PlanGating:Enabled"] = "false"
        });

        options.Validate().ShouldBeEmpty();
    }

    [Fact]
    public void AGateNoPlanCanPassRefusesToStart()
    {
        // Enabling the gate without granting the capability to anyone would lock out
        // every caller, operator included; saying so beats answering the same denial
        // to everyone.
        var options = Bind(new Dictionary<string, string?>
        {
            ["Mcp:PlanGating:Enabled"] = "true"
        });

        var errors = options.Validate();

        errors.Count.ShouldBe(1);
        errors[0].ShouldContain("PlanCapabilities");
    }

    [Fact]
    public void ANegativeCacheDurationIsAnError()
    {
        var values = DefaultSection();
        values["Mcp:PlanGating:CacheDuration"] = "-00:05:00";

        Bind(values).Validate().ShouldContain(error => error.Contains("CacheDuration"));
    }

    [Fact]
    public void AnEmptyRequiredCapabilityIsAnError()
    {
        var values = DefaultSection();
        values["Mcp:PlanGating:RequiredCapability"] = "";

        Bind(values).Validate().ShouldContain(error => error.Contains("RequiredCapability"));
    }

    #endregion
}

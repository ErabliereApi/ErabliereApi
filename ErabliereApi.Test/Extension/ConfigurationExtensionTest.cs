using ErabliereApi.Extensions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

namespace ErabliereApi.Test.Extension;
public class ConfigurationExtensionTest
{
    [Fact]
    public void GetRequiredValue_ShouldReturnValidTimeStamp_WheneyValueValid()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "SomeTimeSpan", "00:30:00" }
            })
            .Build();

        // Act
        var result = config.GetRequiredValue<TimeSpan>("SomeTimeSpan");

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(30), result);
    }
}

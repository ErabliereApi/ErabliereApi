using ErabliereAPI.Proxy;
using ErabliereApi.Mcp.Models;
using ErabliereApi.Mcp.Services;
using Shouldly;

namespace ErabliereApi.Mcp.Test;

/// <summary>
/// The summarizer is what stands between a season of sensor readings and the
/// model context, so its arithmetic and its bounds are pinned here.
/// </summary>
public class DonneesCapteurSummarizerTest
{
    private static readonly DateTimeOffset Origin = new(2026, 3, 12, 6, 0, 0, TimeSpan.FromHours(-4));

    private static GetDonneesCapteurV2 Reading(int minutes, double? valeur, string? text = null) => new()
    {
        Id = Guid.NewGuid(),
        D = Origin.AddMinutes(minutes),
        Valeur = valeur,
        Text = text
    };

    private static GetDonneesCapteurV2[] Series(int count, Func<int, double> value)
    {
        return Enumerable.Range(0, count)
                         .Select(index => Reading(index * 5, value(index)))
                         .ToArray();
    }

    [Fact]
    public void Summarize_WhenTheRangeIsEmpty_ReturnsAnEmptySummaryWithoutThrowing()
    {
        var summary = DonneesCapteurSummarizer.Summarize([], "°C");

        summary.Count.ShouldBe(0);
        summary.Unit.ShouldBe("°C");
        summary.Min.ShouldBeNull();
        summary.Max.ShouldBeNull();
        summary.Avg.ShouldBeNull();
        summary.Latest.ShouldBeNull();
        summary.First.ShouldBeNull();
        summary.Last.ShouldBeNull();
        summary.Serie.ShouldBeEmpty();
        summary.SerieIsDownsampled.ShouldBeFalse();
    }

    [Fact]
    public void Summarize_WhenTheRangeIsEmpty_SaysSoInPlainWords()
    {
        var summary = DonneesCapteurSummarizer.Summarize([], "°C");

        DonneesCapteurSummarizer.Describe(summary, "Température extérieure", truncated: false)
                                .ShouldBe("Sensor 'Température extérieure' has no reading in the requested range.");
    }

    [Fact]
    public void Summarize_WhenThereIsASinglePoint_ReportsItAsMinMaxAverageAndLatest()
    {
        var summary = DonneesCapteurSummarizer.Summarize([Reading(0, -3.5)], "°C");

        summary.Count.ShouldBe(1);
        summary.Min.ShouldBe(-3.5);
        summary.Max.ShouldBe(-3.5);
        summary.Avg.ShouldBe(-3.5);
        summary.Latest.ShouldBe(-3.5);
        summary.First.ShouldBe(Origin);
        summary.Last.ShouldBe(Origin);
        summary.Serie.Count.ShouldBe(1);
        summary.Serie[0].V.ShouldBe(-3.5);
        summary.SerieIsDownsampled.ShouldBeFalse();
    }

    [Fact]
    public void Summarize_ComputesTheStatisticsOverTheWholeRange()
    {
        var summary = DonneesCapteurSummarizer.Summarize([Reading(0, 2), Reading(5, -6), Reading(10, 10), Reading(15, 4)], "°C");

        summary.Count.ShouldBe(4);
        summary.Min.ShouldBe(-6);
        summary.Max.ShouldBe(10);
        summary.Avg.ShouldBe(2.5);
        // The latest is the chronologically last one, not the last of the input.
        summary.Latest.ShouldBe(4);
    }

    [Fact]
    public void Summarize_WhenTheReadingsAreOutOfOrder_SortsThemByTimestamp()
    {
        var summary = DonneesCapteurSummarizer.Summarize([Reading(30, 9), Reading(0, 1), Reading(15, 5)], "°C");

        summary.First.ShouldBe(Origin);
        summary.Last.ShouldBe(Origin.AddMinutes(30));
        summary.Latest.ShouldBe(9);
        summary.Serie.Select(point => point.V).ShouldBe([1, 5, 9]);
    }

    [Fact]
    public void Summarize_WhenTheSerieFitsUnderTheLimit_KeepsEveryPoint()
    {
        var summary = DonneesCapteurSummarizer.Summarize(Series(100, index => index), "°C", maxSeriePoints: 100);

        summary.Serie.Count.ShouldBe(100);
        summary.SerieIsDownsampled.ShouldBeFalse();
    }

    [Fact]
    public void Summarize_WhenTheSerieIsLarge_DownsamplesItToTheRequestedNumberOfPoints()
    {
        // Eight thousand readings is roughly a month of a sensor reporting every
        // five minutes.
        var summary = DonneesCapteurSummarizer.Summarize(Series(8000, index => index), "Hg", maxSeriePoints: 100);

        summary.Count.ShouldBe(8000);
        summary.Serie.Count.ShouldBe(100);
        summary.SerieIsDownsampled.ShouldBeTrue();

        // The extremes survive the downsampling because they are statistics, not
        // series points.
        summary.Min.ShouldBe(0);
        summary.Max.ShouldBe(7999);
        summary.Latest.ShouldBe(7999);
    }

    [Fact]
    public void Summarize_WhenDownsampling_AveragesEachBucketAndTimestampsItWithItsFirstReading()
    {
        // Four readings into two buckets: (0, 10) and (20, 30).
        var summary = DonneesCapteurSummarizer.Summarize(
            [Reading(0, 0), Reading(5, 10), Reading(10, 20), Reading(15, 30)],
            "°C",
            maxSeriePoints: 2);

        summary.Serie.Count.ShouldBe(2);
        summary.Serie[0].V.ShouldBe(5);
        summary.Serie[0].T.ShouldBe(Origin);
        summary.Serie[1].V.ShouldBe(25);
        summary.Serie[1].T.ShouldBe(Origin.AddMinutes(10));
    }

    [Fact]
    public void Summarize_WhenDownsampling_KeepsTheSerieChronological()
    {
        var summary = DonneesCapteurSummarizer.Summarize(Series(1234, index => Math.Sin(index)), "°C", maxSeriePoints: 37);

        summary.Serie.Count.ShouldBe(37);
        summary.Serie.Zip(summary.Serie.Skip(1))
                     .ShouldAllBe(pair => pair.First.T < pair.Second.T);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(DonneesCapteurSummarizer.MaxSeriePointsLimit)]
    public void Summarize_NeverReturnsMorePointsThanAsked(int maxPoints)
    {
        var summary = DonneesCapteurSummarizer.Summarize(Series(5000, index => index), "°C", maxPoints);

        summary.Serie.Count.ShouldBe(maxPoints);
    }

    [Fact]
    public void Summarize_IgnoresTheReadingsWithoutATimestamp()
    {
        var orphan = new GetDonneesCapteurV2 { Id = Guid.NewGuid(), Valeur = 999, D = null };

        var summary = DonneesCapteurSummarizer.Summarize([Reading(0, 1), orphan, Reading(5, 3)], "°C");

        summary.Count.ShouldBe(2);
        summary.Max.ShouldBe(3);
    }

    [Fact]
    public void Summarize_WhenTheSensorReportsText_SurfacesTheLatestLabel()
    {
        var summary = DonneesCapteurSummarizer.Summarize(
            [Reading(0, null, "ouvert"), Reading(10, null, "fermé")],
            unit: null);

        summary.Count.ShouldBe(0);
        summary.LatestText.ShouldBe("fermé");
        summary.Serie.ShouldBeEmpty();

        DonneesCapteurSummarizer.Describe(summary, "Porte", truncated: false)
                                .ShouldContain("'fermé'");
    }

    [Fact]
    public void Summarize_RoundsTheStatisticsToTwoDecimals()
    {
        var summary = DonneesCapteurSummarizer.Summarize([Reading(0, 1d / 3), Reading(5, 2d / 3)], "°C");

        summary.Avg.ShouldBe(0.5);
        summary.Min.ShouldBe(0.33);
        summary.Max.ShouldBe(0.67);
    }

    [Fact]
    public void Describe_WhenTheApiCappedTheQuery_TellsTheModelToNarrowTheRange()
    {
        var summary = DonneesCapteurSummarizer.Summarize(Series(200, index => index), "°C");

        var sentence = DonneesCapteurSummarizer.Describe(summary, "Vacuum", truncated: true);

        sentence.ShouldContain("narrow startDate and endDate");
        sentence.ShouldContain("downsampled to 100 averaged points");
    }

    [Fact]
    public void Summarize_WhenMaxPointsIsNotPositive_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => DonneesCapteurSummarizer.Summarize(Series(10, index => index), "°C", maxSeriePoints: 0));
    }

    [Fact]
    public void Summarize_OfAFullMonth_StaysFarUnderTheResponseBudget()
    {
        // The whole point of the summarizer: a month of readings must come back as
        // something a model can actually read.
        var summary = DonneesCapteurSummarizer.Summarize(Series(8640, index => 12.345 + index), "°C");

        var response = ToolResponse.ForItem(
            DonneesCapteurSummarizer.Describe(summary, "Température extérieure", truncated: false),
            summary);

        ToolResponse.EstimateTokens(response).ShouldBeLessThan(ToolResponse.MaxResponseTokens);
    }
}

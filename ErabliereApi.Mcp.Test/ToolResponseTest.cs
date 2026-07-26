using System.Text.Json.Nodes;
using ErabliereApi.Mcp.Models;
using ErabliereApi.Mcp.Serialization;
using Shouldly;

namespace ErabliereApi.Mcp.Test;

/// <summary>
/// The envelope is the contract shared by every tool, and the response budget is
/// the promise that a tool result never floods the model context.
/// </summary>
public class ToolResponseTest
{
    private record Item(string Id, string Payload);

    private static Item[] Items(int count, int payloadLength)
    {
        return Enumerable.Range(0, count)
                         .Select(index => new Item($"item-{index}", new string('x', payloadLength)))
                         .ToArray();
    }

    [Fact]
    public void ForList_SerializesTheThreeFieldsOfTheEnvelope()
    {
        var response = ToolResponse.ForList("Two items.", Items(2, 4));

        var json = JsonNode.Parse(ToolJson.Serialize(response))!.AsObject();

        json["summary"]!.GetValue<string>().ShouldBe("Two items.");
        json["data"]!.AsArray().Count.ShouldBe(2);
        json["truncated"]!.GetValue<bool>().ShouldBeFalse();
        json.Count.ShouldBe(3);
    }

    [Fact]
    public void ForList_WhenTheListFits_KeepsEveryItem()
    {
        var items = Items(25, 40);

        var response = ToolResponse.ForList("Twenty five items.", items);

        response.Data.Count.ShouldBe(25);
        response.Truncated.ShouldBeFalse();
        response.Summary.ShouldBe("Twenty five items.");
    }

    [Fact]
    public void ForList_WhenTheApiAlreadyCapped_PropagatesTheFlagWithoutTrimming()
    {
        var response = ToolResponse.ForList("Capped.", Items(5, 10), truncated: true);

        response.Data.Count.ShouldBe(5);
        response.Truncated.ShouldBeTrue();
    }

    [Fact]
    public void ForList_WhenTheListIsTooLarge_TrimsTheTailAndFlagsTheResponse()
    {
        var items = Items(500, 200);

        var response = ToolResponse.ForList("Five hundred items.", items);

        response.Data.Count.ShouldBeLessThan(items.Length);
        response.Data.Count.ShouldBeGreaterThan(0);
        response.Truncated.ShouldBeTrue();
        ToolResponse.EstimateTokens(response).ShouldBeLessThanOrEqualTo(ToolResponse.MaxResponseTokens);
    }

    [Fact]
    public void ForList_WhenItTrims_KeepsTheHeadOfTheListAndSaysHowManyWereDropped()
    {
        var items = Items(500, 200);

        var response = ToolResponse.ForList("Five hundred items.", items);

        // The tools order their lists most relevant first, so the head is what
        // must survive.
        response.Data[0].Id.ShouldBe("item-0");
        response.Data.ShouldBe(items.Take(response.Data.Count));
        response.Summary.ShouldStartWith("Five hundred items.");
        response.Summary.ShouldContain($"Only the first {response.Data.Count} of 500 items");
    }

    [Fact]
    public void ForList_WhenASingleItemBlowsTheBudget_ReturnsAnEmptyListRatherThanAnOversizedOne()
    {
        var response = ToolResponse.ForList("One enormous item.", Items(1, ToolResponse.MaxResponseTokens * ToolResponse.CharactersPerToken * 2));

        response.Data.ShouldBeEmpty();
        response.Truncated.ShouldBeTrue();
        ToolResponse.EstimateTokens(response).ShouldBeLessThanOrEqualTo(ToolResponse.MaxResponseTokens);
    }

    [Fact]
    public void ForList_WhenTheListIsEmpty_ReturnsItUntouched()
    {
        var response = ToolResponse.ForList("Nothing found.", Array.Empty<Item>());

        response.Data.ShouldBeEmpty();
        response.Truncated.ShouldBeFalse();
        response.Summary.ShouldBe("Nothing found.");
    }

    [Fact]
    public void ForItem_DoesNotTrimTheObject()
    {
        var item = new Item("only", "value");

        var response = ToolResponse.ForItem("One item.", item);

        response.Data.ShouldBe(item);
        response.Truncated.ShouldBeFalse();
    }

    [Fact]
    public void TheSerializerIsCompactAndDoesNotEscapeTheFrenchAccents()
    {
        var json = ToolJson.Serialize(ToolResponse.ForItem("Érablière", new Item("é", "à")));

        json.ShouldNotContain("\\u");
        json.ShouldNotContain("\n");
        json.ShouldContain("Érablière");
    }

    [Fact]
    public void TheSerializerWritesDatesAsIso8601TruncatedToTheSecond()
    {
        // Seven fractional digits come out of the database on every reading; they
        // are noise a model never needs and pays for.
        var date = new DateTimeOffset(2026, 3, 12, 6, 30, 0, TimeSpan.FromHours(-4)).AddTicks(1234567);

        var json = ToolJson.Serialize(new SeriePoint(date, 1.5));

        json.ShouldBe("""{"t":"2026-03-12T06:30:00-04:00","v":1.5}""");
    }
}

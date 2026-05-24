using ErabliereApi.Controllers;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Services.LoRaWAN;
using ErabliereApi.Test.Autofixture;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Test;

public class ChirpstackControllerTest
{
    [Fact]
    public void CanDeserialize()
    {
        var guidId = Guid.NewGuid();

        var payload = Constants.ChirpStackExOk.Replace("<replace-guid-erabliere>", guidId.ToString());

        var eventInfo = JsonSerializer.Deserialize<PostChirpstackEvent>(payload);

        Assert.NotNull(eventInfo);
        Assert.NotNull(eventInfo.deviceInfo?.tags?.idErabliere);
        Assert.Equal(guidId, eventInfo.deviceInfo.tags.idErabliere.Value);
    }

    [Fact]
    public void DecodePacket()
    {
        var data = "AQYQrCYAAAEHENRiAAAABwBkAAEATcw=";

        var mesurements = LoRaWANPacketDecoder.TryDecodeData(data);

        Assert.NotNull(mesurements.Mesurements);
        Assert.Equal(4, mesurements.Mesurements.Length);
        var soilTemperature = mesurements.Mesurements[0];
        var soilHumidity = mesurements.Mesurements[1];
        var battery = mesurements.Mesurements[2];
        var frequency = mesurements.Mesurements[3];

        Assert.Equal(4102, soilTemperature.Mesure);
        Assert.Equal(9.9m, soilTemperature.Value);
        Assert.Equal(4103, soilHumidity.Mesure);
        Assert.Equal(25.3m, soilHumidity.Value);
        Assert.Equal(7, battery.Mesure);
        Assert.Equal(100m, battery.Value);
        Assert.Equal(8, frequency.Mesure);
    }

    [Fact]
    public void DecodeWeaterStationPacket()
    {
        string packet = "AQAwYQAAAAAAABACAEgAAAAAJfo=";

        var mesurements = LoRaWANPacketDecoder.TryDecodeData(packet);

        Assert.NotNull(mesurements.Mesurements);
        Assert.NotEmpty(mesurements.Mesurements);
        Assert.Equal(4097, mesurements.Mesurements[0].Mesure);
        Assert.Equal(4.8m, mesurements.Mesurements[0].Value);
    }

    [Theory, AutoApiData]
    public async Task RandomCall(
        ChirpstackController controller, PostChirpstackEvent rootobject)
    {
        var resp = await controller.EventListener("up", rootobject, CancellationToken.None);

        Assert.NotNull(resp);
    }
}

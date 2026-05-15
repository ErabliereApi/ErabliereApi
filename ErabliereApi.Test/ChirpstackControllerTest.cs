using ErabliereApi.Controllers;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Services.LoRaWAN;
using ErabliereApi.Test.Autofixture;
using Microsoft.Extensions.Logging;
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
        Assert.Equal(3, mesurements.Mesurements.Length);
        var soilTemperature = mesurements.Mesurements[0];
        var soilHumidity = mesurements.Mesurements[1];

        Assert.Equal(4102, soilTemperature.Mesure);
        Assert.Equal(4103, soilHumidity.Mesure);
    }

    [Fact]
    public void DecodeWeaterStationPacket()
    {
        string packet = "AQAwYQAAAAAAABACAEgAAAAAJfo=";

        var mesurements = LoRaWANPacketDecoder.TryDecodeData(packet);

        Assert.NotNull(mesurements.Mesurements);
        Assert.NotEmpty(mesurements.Mesurements);
    }

    [Theory, AutoApiData]
    public async Task CreerErabliereAnonyme(
        ChirpstackController controller, PostChirpstackEvent rootobject)
    {
        var resp = await controller.EventListener("up", rootobject, CancellationToken.None);

        Assert.NotNull(resp);
    }
}

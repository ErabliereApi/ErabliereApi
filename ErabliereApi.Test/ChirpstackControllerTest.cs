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

        var (decodedData, crc) = LoRaWANPacketDecoder.TryDecodeData(data);

        Assert.Equal(3, decodedData.Length);
        var soilTemperature = decodedData[0];
        var soilHumidity = decodedData[1];

        Assert.Equal(4102, soilTemperature.Mesure);
        Assert.Equal(4103, soilHumidity.Mesure);
    }

    [Fact]
    public void DecodeWeaterStationPacket()
    {
        string packet = "AQAwYQAAAAAAABACAEgAAAAAJfo=";

        var (decodedData, crc) = LoRaWANPacketDecoder.TryDecodeData(packet);

        Assert.NotEmpty(decodedData);
    }

    [Theory, AutoApiData]
    public async Task CreerErabliereAnonyme(
        ChirpstackController controller, PostChirpstackEvent rootobject)
    {
        var resp = await controller.EventListener("up", rootobject, CancellationToken.None);

        Assert.NotNull(resp);
    }
}
